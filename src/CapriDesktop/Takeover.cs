using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CapriDesktop;

internal sealed class Takeover : Overlay
{
	internal const double FaceAt = 0.12;

	internal const double FaceIn = 0.3;

	internal const double FlyAt = 0.95;

	internal const double FlyLength = 0.45;

	internal const double BadgeAt = 1.3;

	internal const double Settled = 1.75;

	internal const double LeaveLength = 0.32;

	internal const double VineLength = 1.6;

	internal static readonly RectangleF FacePlate = new RectangleF(140f, 145f, 110f, 110f);

	internal static readonly RectangleF Zoom = new RectangleF(45f, 45f, 300f, 300f);

	private static readonly StringFormat Spaced = Typographic();

	internal static bool HackIn;

	internal static Bitmap Face;

	private static Bitmap painted;

	private static Bitmap minted;

	private static Bitmap bruised;

	private static RectangleF paintedFrom;

	private readonly Timer ticks = new Timer();

	private readonly Stopwatch watch = Stopwatch.StartNew();

	private readonly Random random = new Random();

	private IntPtr window;

	private string app = "";

	private Rectangle box;

	private Rectangle screen;

	private Rectangle shownBox;

	private double started = -1.0;

	private double leaving = -1.0;

	private double nextFlicker;

	private double flickerUntil;

	private bool clean;

	private int seed;

	private float scale = 1f;

	protected override CreateParams CreateParams
	{
		get
		{
			CreateParams createParams = base.CreateParams;
			createParams.ExStyle |= 32;
			return createParams;
		}
	}

	internal bool Active
	{
		get
		{
			if (started >= 0.0)
			{
				return leaving < 0.0;
			}
			return false;
		}
	}

	internal IntPtr Window => window;

	private double Now => watch.Elapsed.TotalSeconds;

	internal Takeover()
		: base("Capri control")
	{
		ticks.Interval = 33;
		Timer timer = ticks;
		EventHandler value = (object param0, EventArgs param1) =>
		{
			Step();
		};
		timer.Tick += value;
	}

	[DllImport("user32.dll")]
	private static extern bool SetWindowDisplayAffinity(IntPtr window, uint affinity);

	protected override void OnHandleCreated(EventArgs e)
	{
		base.OnHandleCreated(e);
		try
		{
			SetWindowDisplayAffinity(Handle, 17u);
		}
		catch (EntryPointNotFoundException)
		{
		}
	}

	internal void Follow(IntPtr handle, string name, Rectangle bounds, float size)
	{
		if (handle == IntPtr.Zero)
		{
			return;
		}
		scale = Math.Max(0.8f, Math.Min(1.6f, size));
		if (Ledges.Showing(handle, out var bounds2))
		{
			bounds = bounds2;
		}
		if (bounds.Width < 60 || bounds.Height < 40)
		{
			return;
		}
		app = name;
		if (!Active || !(handle == window))
		{
			bool active = Active;
			window = handle;
			box = bounds;
			screen = Screen.FromRectangle(bounds).Bounds;
			leaving = -1.0;
			clean = false;
			if (!active)
			{
				started = (HackIn ? Now : (Now - 1.75));
				seed = random.Next();
			}
			else if (Now - started > 1.75)
			{
				started = Now - (HackIn ? 1.3 : 1.75);
			}
			nextFlicker = Now + 4.0 + random.NextDouble() * 3.0;
			ticks.Start();
			Step();
		}
	}

	internal void Release()
	{
		if (Active)
		{
			leaving = Now;
			clean = false;
		}
	}

	internal void Stop()
	{
		ticks.Stop();
		started = -1.0;
		leaving = -1.0;
		window = IntPtr.Zero;
		if (Visible)
		{
			Hide();
		}
	}

	private void Step()
	{
		double now = Now;
		double num = now - started;
		if (started < 0.0)
		{
			ticks.Stop();
			return;
		}
		if (leaving >= 0.0 && now - leaving >= 0.32)
		{
			Stop();
			return;
		}
		if (Ledges.Showing(window, out var bounds))
		{
			box = bounds;
		}
		else if (leaving < 0.0)
		{
			Release();
			now = Now;
		}
		bool flag = num < 1.75 || leaving >= 0.0;
		if (!flag && now >= nextFlicker)
		{
			flickerUntil = now + 0.12;
			nextFlicker = now + 4.0 + random.NextDouble() * 3.0;
		}
		bool flag2 = now < flickerUntil;
		if (flag || flag2 || !clean || !(box == shownBox))
		{
			Rectangle rectangle = ((num < 1.75) ? screen : Rectangle.Intersect(Rectangle.Inflate(box, 8, 8), screen));
			if (rectangle.Width >= 8 && rectangle.Height >= 8)
			{
				Present(Frame(box: new Rectangle(box.X - rectangle.X, box.Y - rectangle.Y, box.Width, box.Height), size: rectangle.Size, app: app, t: num, gone: (leaving >= 0.0) ? ((now - leaving) / 0.32) : 0.0, glitch: flag2 ? (seed + (int)(now * 30.0)) : (-1), face: Face, scale: scale, seed: seed), rectangle.Location);
				clean = !flag && !flag2;
				shownBox = box;
			}
		}
	}

	private static StringFormat Typographic()
	{
		StringFormat stringFormat = (StringFormat)StringFormat.GenericTypographic.Clone();
		stringFormat.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces;
		return stringFormat;
	}

	private static double Smooth(double x)
	{
		x = Math.Max(0.0, Math.Min(1.0, x));
		return x * x * (3.0 - 2.0 * x);
	}

	private static double Back(double x)
	{
		x = Math.Max(0.0, Math.Min(1.0, x)) - 1.0;
		return x * x * (2.7 * x + 1.7) + 1.0;
	}

	internal static Bitmap Frame(Size size, Rectangle box, string app, double t, double gone, int glitch, Bitmap face, float scale, int seed)
	{
		Bitmap bitmap = new Bitmap(size.Width, size.Height, PixelFormat.Format32bppPArgb);
		double num = 1.0 - Smooth(gone);
		if (num <= 0.0)
		{
			return bitmap;
		}
		Random random = new Random((glitch >= 0) ? glitch : ((int)(t * 30.0) * 7919 + 13));
		float unit = Math.Max(0.6f, Math.Min(1.6f, (float)box.Height / 560f));
		float num2 = (float)Math.Min((double)box.Height * 0.24, (double)size.Height * 0.28);
		PointF middle = new PointF((float)box.Left + (float)box.Width / 2f, (float)box.Top + (float)box.Height / 2f);
		double val = Back((t - 0.12) / 0.24);
		double num3 = (t - 0.08) / 1.52;
		float num4 = num2 * 0.75f * (float)Math.Max(0.15, Math.Min(1.0, val));
		if (num3 > 0.0 && t < 1.6)
		{
			Tear.Tendrils(bitmap, Rectangle.Inflate(box, box.Width / 30, box.Height / 30), middle, new SizeF(num4, num4), unit, (gone > 0.0) ? Math.Max(num3, 0.8 + 0.17 * gone) : num3, seed, (int)(t * 30.0));
		}
		using (Graphics graphics = Graphics.FromImage(bitmap))
		{
			using Font font = Look.Mono(11f * scale, FontStyle.Bold);
			graphics.SmoothingMode = SmoothingMode.AntiAlias;
			graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
			graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
			graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
			string lead = "CAPRI IS CONTROLLING ";
			string name = app.ToUpperInvariant();
			float num5 = 26f * scale;
			float num6 = 20f * scale;
			float num7 = 7f * scale;
			float num8 = graphics.MeasureString(lead, font, PointF.Empty, Spaced).Width;
			float num9 = graphics.MeasureString(name, font, PointF.Empty, Spaced).Width;
			float num10 = num7 + num6 + num7 + num8 + num9 + num7 * 1.4f;
			float num11 = Math.Max(4f * scale, (float)box.Top + 6f * scale);
			float num12 = (float)box.Left + (float)box.Width / 2f;
			RectangleF iconAt = new RectangleF(num12 - num10 / 2f + num7, num11 + (num5 - num6) / 2f, num6, num6);
			if (t < 0.2)
			{
				Hits(graphics, box, t / 0.2 + gone, unit, random);
			}
			if (t < 1.5)
			{
				Border(graphics, box, Smooth((t - 0.08) / 0.2) * (1.0 - Smooth((t - 0.95 - 0.225) / 0.3)) * num, scale, random);
			}
			if (t >= 1.22)
			{
				Brackets(graphics, box, Smooth((t - 0.95 - 0.27) / 0.25) * num, scale);
			}
			if (t >= 1.3)
			{
				double num13 = Smooth((t - 1.3) / 0.18);
				float num14 = num6 + num7 * 2f + (float)((double)(num10 - num6 - num7 * 2f) * num13);
				float num15 = ((glitch >= 0) ? ((float)((random.NextDouble() * 2.0 - 1.0) * 6.0 * (double)scale)) : 0f);
				RectangleF slab = new RectangleF(num12 - num10 / 2f + num15, num11, num14, num5);
				Badge(graphics, slab, iconAt, lead, name, font, num7, num6, Smooth((t - 1.3 - 0.08) / 0.3), num, face, glitch >= 0, random, scale);
			}
			if (face != null && t >= 0.12 && t < 1.4)
			{
				double num16 = Smooth((t - 0.95) / 0.45);
				float num17 = (float)(Math.Sin(t * 7.0) * 3.0 * (double)scale * (1.0 - num16));
				PointF pointF = new PointF(middle.X, middle.Y + num17);
				PointF pointF2 = new PointF(iconAt.X + num6 / 2f, iconAt.Y + num6 / 2f);
				double num18 = ((t < 0.32 && random.Next(3) == 0) ? 0.35 : 1.0);
				for (int num19 = 2; num19 >= 0; num19--)
				{
					double num20 = Math.Max(0.0, num16 - (double)num19 * 0.09);
					if (num19 <= 0 || (!(num16 <= 0.0) && !(num16 >= 1.0)))
					{
						float side = (float)(Math.Exp(Math.Log(num2 * 2f) + (Math.Log(num6) - Math.Log(num2 * 2f)) * num20) * Math.Max(0.02, val));
						PointF at = new PointF(pointF.X + (pointF2.X - pointF.X) * (float)num20, pointF.Y + (pointF2.Y - pointF.Y) * (float)num20);
						float num21 = (float)num20;
						RectangleF source = new RectangleF(Zoom.X + (FacePlate.X - Zoom.X) * num21, Zoom.Y + (FacePlate.Y - Zoom.Y) * num21, Zoom.Width + (FacePlate.Width - Zoom.Width) * num21, Zoom.Height + (FacePlate.Height - Zoom.Height) * num21);
						double slices;
						if (num19 > 0)
						{
							slices = 0.6;
						}
						else if (t < 0.42)
						{
							slices = 1.0 - (t - 0.12) / 0.3;
						}
						else
						{
							slices = ((random.Next(9) == 0) ? 0.5 : 0.0);
						}
						Portal(graphics, face, at, side, source, ((num19 == 0) ? num18 : (0.32 / (double)num19)) * num, slices, num19 switch
						{
							2 => Shatter.Mint, 
							1 => Shatter.Violet, 
							_ => Color.Empty, 
						}, scale, unit, num20, seed, random);
					}
				}
			}
		}
		return bitmap;
	}

	private static Bitmap Baked(Bitmap face, int span, RectangleF source, Color tint)
	{
		Bitmap bitmap = new Bitmap(span, span, PixelFormat.Format32bppPArgb);
		using Graphics graphics = Graphics.FromImage(bitmap);
		using ImageAttributes imageAttrs = Tint(tint, tint.IsEmpty ? 1.0 : 0.55);
		graphics.InterpolationMode = InterpolationMode.HighQualityBilinear;
		graphics.PixelOffsetMode = PixelOffsetMode.Half;
		graphics.DrawImage(face, new Rectangle(0, 0, span, span), source.X, source.Y, source.Width, source.Height, GraphicsUnit.Pixel, imageAttrs);
		return bitmap;
	}

	private static void Portal(Graphics graphics, Bitmap face, PointF at, float side, RectangleF source, double alpha, double slices, Color tint, float scale, float unit, double square, int seed, Random pick)
	{
		if (side < 2f || alpha <= 0.01)
		{
			return;
		}
		Random random = new Random(seed ^ 0x5BD1);
		PointF[] array = new PointF[14];
		PointF[] array2 = new PointF[14];
		PointF[] array3 = new PointF[14];
		float num = Math.Max(3f * unit, side * 0.05f);
		float num2 = 3f * unit * (float)(1.0 - square);
		for (int i = 0; i < 14; i++)
		{
			double num3 = ((double)i + 0.3 * random.NextDouble()) / 14.0 * 2.0 * Math.PI - Math.PI / 2.0;
			double num4 = ((i % 2 == 0) ? (0.94 + 0.16 * random.NextDouble()) : (0.6 + 0.2 * random.NextDouble()));
			double num5 = (num4 + (1.0 / Math.Max(Math.Abs(Math.Cos(num3)), Math.Abs(Math.Sin(num3))) - num4) * square) * (double)side / 2.0;
			float num6 = (float)Math.Cos(num3);
			float num7 = (float)Math.Sin(num3);
			ref PointF reference = ref array[i];
			reference = new PointF(at.X + num6 * (float)num5, at.Y + num7 * (float)num5);
			ref PointF reference2 = ref array2[i];
			reference2 = new PointF(at.X + num6 * ((float)num5 + num), at.Y + num7 * ((float)num5 + num));
			ref PointF reference3 = ref array3[i];
			reference3 = new PointF(array2[i].X - num2, array2[i].Y + num2);
		}
		int alpha2 = (int)(255.0 * Math.Max(0.0, Math.Min(1.0, alpha)));
		if (!tint.IsEmpty)
		{
			using (Brush brush = new SolidBrush(Color.FromArgb(alpha2, tint)))
			{
				graphics.FillPolygon(brush, array2);
				return;
			}
		}
		using (Brush brush2 = new SolidBrush(Color.FromArgb(alpha2, Shatter.Violet)))
		{
			graphics.FillPolygon(brush2, array3);
		}
		using (Brush brush3 = new SolidBrush(Color.FromArgb(alpha2, 236, 6, 68)))
		{
			graphics.FillPolygon(brush3, array2);
		}
		using (Brush brush4 = new SolidBrush(Color.FromArgb(alpha2, 10, 8, 14)))
		{
			graphics.FillPolygon(brush4, array);
		}
		int num8 = Math.Max(2, (int)Math.Ceiling(side * 1.08f));
		if (painted == null || painted.Width != num8 || paintedFrom != source)
		{
			Bitmap[] array4 = new Bitmap[3] { painted, minted, bruised };
			for (int j = 0; j < array4.Length; j++)
			{
				array4[j]?.Dispose();
			}
			painted = Baked(face, num8, source, Color.Empty);
			minted = (bruised = null);
			paintedFrom = source;
		}
		Bitmap bitmap = painted;
		if (slices > 0.01 || alpha < 0.999)
		{
			if (slices > 0.25 && minted == null)
			{
				minted = Baked(face, num8, source, Shatter.Mint);
				bruised = Baked(face, num8, source, Shatter.Violet);
			}
			bitmap = new Bitmap(num8, num8, PixelFormat.Format32bppPArgb);
			using Graphics graphics2 = Graphics.FromImage(bitmap);
			using ImageAttributes imageAttr = Tint(Color.Empty, alpha);
			graphics2.InterpolationMode = InterpolationMode.NearestNeighbor;
			int num9 = (int)((double)(6f * scale) * slices);
			float num11;
			for (float num10 = 0f; num10 < 1f; num10 += num11)
			{
				num11 = ((slices <= 0.01) ? 1f : ((float)Math.Min(1f - num10, 0.05 + pick.NextDouble() * 0.15)));
				int srcY = (int)((float)num8 * num10);
				int srcHeight = Math.Max(1, (int)Math.Ceiling((float)num8 * num11));
				int num12 = ((slices > 0.01 && pick.Next(3) == 0) ? ((int)((pick.NextDouble() * 2.0 - 1.0) * 28.0 * (double)scale * slices)) : 0);
				if (slices > 0.25)
				{
					graphics2.DrawImage(minted, new Rectangle(num12 - num9, srcY, num8, srcHeight), 0, srcY, num8, srcHeight, GraphicsUnit.Pixel);
					graphics2.DrawImage(bruised, new Rectangle(num12 + num9, srcY, num8, srcHeight), 0, srcY, num8, srcHeight, GraphicsUnit.Pixel);
				}
				graphics2.DrawImage(painted, new Rectangle(num12, srcY, num8, srcHeight), 0, srcY, num8, srcHeight, GraphicsUnit.Pixel, imageAttr);
			}
		}
		InterpolationMode interpolationMode = graphics.InterpolationMode;
		PixelOffsetMode pixelOffsetMode = graphics.PixelOffsetMode;
		graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
		graphics.PixelOffsetMode = PixelOffsetMode.None;
		using (TextureBrush textureBrush = new TextureBrush(bitmap, WrapMode.Clamp))
		{
			textureBrush.TranslateTransform((float)Math.Round(at.X - (float)num8 / 2f), (float)Math.Round(at.Y - (float)num8 / 2f));
			graphics.FillPolygon(textureBrush, array);
		}
		graphics.InterpolationMode = interpolationMode;
		graphics.PixelOffsetMode = pixelOffsetMode;
		if (bitmap != painted)
		{
			bitmap.Dispose();
		}
		using Pen pen = new Pen(Color.FromArgb(alpha2, 10, 8, 14), Math.Max(1.2f, 1.6f * unit));
		graphics.DrawPolygon(pen, array);
	}

	private static void Hits(Graphics graphics, Rectangle box, double t, float unit, Random pick)
	{
		double num = 1.0 - Smooth(t);
		if (num <= 0.01)
		{
			return;
		}
		Color[] array = new Color[5]
		{
			Look.Red,
			Color.FromArgb(10, 8, 14),
			Look.Pink,
			Shatter.Mint,
			Shatter.Violet
		};
		int i = 0;
		for (int num2 = 4 + pick.Next(5); i < num2; i++)
		{
			float num3 = (float)box.Width * (float)(0.25 + pick.NextDouble() * 0.65);
			float num4 = (float)(1 + pick.Next(5)) * unit;
			using Brush brush = new SolidBrush(Color.FromArgb((int)(220.0 * num), array[pick.Next(array.Length)]));
			graphics.FillRectangle(brush, (float)box.Left + (float)pick.NextDouble() * ((float)box.Width - num3), (float)box.Top + (float)pick.NextDouble() * (float)box.Height, num3, num4);
		}
	}

	private static Rectangle Rect(RectangleF area)
	{
		return Rectangle.Round(area);
	}

	private static ImageAttributes Tint(Color colour, double alpha)
	{
		ImageAttributes imageAttributes = new ImageAttributes();
		float num = (float)Math.Max(0.0, Math.Min(1.0, alpha));
		ColorMatrix colorMatrix;
		if (!colour.IsEmpty)
		{
			float[][] array = new float[5][];
			float[] array2 = new float[5];
			array[0] = array2;
			float[] array3 = new float[5];
			array[1] = array3;
			float[] array4 = new float[5];
			array[2] = array4;
			array[3] = new float[5] { 0f, 0f, 0f, num, 0f };
			array[4] = new float[5]
			{
				(float)(int)colour.R / 255f,
				(float)(int)colour.G / 255f,
				(float)(int)colour.B / 255f,
				0f,
				1f
			};
			colorMatrix = new ColorMatrix(array);
		}
		else
		{
			colorMatrix = new ColorMatrix(new float[5][]
			{
				new float[5] { 1f, 0f, 0f, 0f, 0f },
				new float[5] { 0f, 1f, 0f, 0f, 0f },
				new float[5] { 0f, 0f, 1f, 0f, 0f },
				new float[5] { 0f, 0f, 0f, num, 0f },
				new float[5] { 0f, 0f, 0f, 0f, 1f }
			});
		}
		imageAttributes.SetColorMatrix(colorMatrix);
		return imageAttributes;
	}

	private static void Border(Graphics graphics, Rectangle box, double alpha, float scale, Random pick)
	{
		if (alpha <= 0.01)
		{
			return;
		}
		List<PointF> list = new List<PointF>();
		float num = 18f * scale;
		float num2 = 5f * scale;
		for (float num3 = box.Left; num3 < (float)box.Right; num3 += num)
		{
			list.Add(new PointF(num3, (float)box.Top + (float)(pick.NextDouble() * 2.0 - 1.0) * num2));
		}
		for (float num4 = box.Top; num4 < (float)box.Bottom; num4 += num)
		{
			list.Add(new PointF((float)box.Right + (float)(pick.NextDouble() * 2.0 - 1.0) * num2, num4));
		}
		for (float num5 = box.Right; num5 > (float)box.Left; num5 -= num)
		{
			list.Add(new PointF(num5, (float)box.Bottom + (float)(pick.NextDouble() * 2.0 - 1.0) * num2));
		}
		for (float num6 = box.Bottom; num6 > (float)box.Top; num6 -= num)
		{
			list.Add(new PointF((float)box.Left + (float)(pick.NextDouble() * 2.0 - 1.0) * num2, num6));
		}
		PointF[] array = list.ToArray();
		Color[] array2 = new Color[3]
		{
			Shatter.Violet,
			Color.FromArgb(40, 230, 255),
			Look.Pink
		};
		for (int i = 0; i < array2.Length; i++)
		{
			using Pen pen = new Pen(Color.FromArgb((int)(220.0 * alpha), array2[i]), ((i == 2) ? 2.4f : 2f) * scale);
			pen.LineJoin = LineJoin.Miter;
			using Matrix matrix = new Matrix();
			matrix.Translate((float)(i - 1) * 2.5f * scale, 0f);
			PointF[] array3 = (PointF[])array.Clone();
			matrix.TransformPoints(array3);
			graphics.DrawPolygon(pen, array3);
		}
	}

	private static void Brackets(Graphics graphics, Rectangle box, double alpha, float scale)
	{
		if (alpha <= 0.01)
		{
			return;
		}
		float num = 22f * scale;
		float num2 = 3f * scale;
		int[] array = new int[2] { 0, 1 };
		foreach (int num3 in array)
		{
			using Pen pen = new Pen(Color.FromArgb((int)(230.0 * alpha), (num3 == 0) ? Shatter.Violet : Look.Pink), num2);
			pen.StartCap = LineCap.Square;
			pen.EndCap = LineCap.Square;
			float num4 = ((num3 == 0) ? (2f * scale) : 0f);
			float num5 = (float)(box.Left + 2) + num4;
			float num6 = (float)(box.Right - 3) + num4;
			float num7 = (float)(box.Top + 2) + num4;
			float num8 = (float)(box.Bottom - 3) + num4;
			graphics.DrawLines(pen, new PointF[3]
			{
				new PointF(num5, num7 + num),
				new PointF(num5, num7),
				new PointF(num5 + num, num7)
			});
			graphics.DrawLines(pen, new PointF[3]
			{
				new PointF(num6 - num, num7),
				new PointF(num6, num7),
				new PointF(num6, num7 + num)
			});
			graphics.DrawLines(pen, new PointF[3]
			{
				new PointF(num5, num8 - num),
				new PointF(num5, num8),
				new PointF(num5 + num, num8)
			});
			graphics.DrawLines(pen, new PointF[3]
			{
				new PointF(num6 - num, num8),
				new PointF(num6, num8),
				new PointF(num6, num8 - num)
			});
		}
	}

	private static void Badge(Graphics graphics, RectangleF slab, RectangleF iconAt, string lead, string name, Font font, float pad, float icon, double typed, double fade, Bitmap face, bool glitch, Random pick, float scale)
	{
		int num = (int)(255.0 * fade);
		using (GraphicsPath graphicsPath = new GraphicsPath())
		{
			float num2 = slab.Height * 0.35f;
			graphicsPath.AddPolygon(new PointF[4]
			{
				new PointF(slab.Left + num2, slab.Top),
				new PointF(slab.Right, slab.Top),
				new PointF(slab.Right - num2, slab.Bottom),
				new PointF(slab.Left, slab.Bottom)
			});
			using (Brush brush = new SolidBrush(Color.FromArgb(num, Shatter.Violet)))
			{
				using Matrix matrix = new Matrix();
				using GraphicsPath graphicsPath2 = (GraphicsPath)graphicsPath.Clone();
				matrix.Translate(2f * scale, 3f * scale);
				graphicsPath2.Transform(matrix);
				graphics.FillPath(brush, graphicsPath2);
			}
			using (LinearGradientBrush brush2 = new LinearGradientBrush(slab, Color.FromArgb(num * 94 / 100, 34, 22, 44), Color.FromArgb(num * 96 / 100, 12, 8, 18), LinearGradientMode.Vertical))
			{
				graphics.FillPath(brush2, graphicsPath);
			}
			using (Pen pen = new Pen(Color.FromArgb(num, Look.Red), 1.6f * scale))
			{
				graphics.DrawPath(pen, graphicsPath);
			}
			using Region clip = graphics.Clip;
			graphics.SetClip(graphicsPath, CombineMode.Intersect);
			float num3 = iconAt.Left + icon + pad;
			float num4 = slab.Top + (slab.Height - font.GetHeight(graphics)) / 2f + 1f;
			int num5 = (int)Math.Round((double)(lead.Length + name.Length) * Math.Max(0.0, Math.Min(1.0, typed)));
			string s = lead.Substring(0, Math.Min(lead.Length, num5));
			string text = ((num5 > lead.Length) ? name.Substring(0, Math.Min(name.Length, num5 - lead.Length)) : "");
			using (Brush brush3 = new SolidBrush(Color.FromArgb(num, Look.Pink)))
			{
				using Brush brush4 = new SolidBrush(Color.FromArgb(num, 255, 255, 255));
				graphics.DrawString(s, font, brush3, num3, num4, Spaced);
				if (text.Length > 0)
				{
					graphics.DrawString(text, font, brush4, num3 + graphics.MeasureString(lead, font, PointF.Empty, Spaced).Width, num4, Spaced);
				}
			}
			if (glitch)
			{
				using Brush brush5 = new SolidBrush(Color.FromArgb(num * 3 / 4, (pick.Next(2) == 0) ? Shatter.Mint : Look.Pink));
				graphics.FillRectangle(brush5, slab.Left, slab.Top + (float)pick.NextDouble() * slab.Height, slab.Width * (float)(0.3 + pick.NextDouble() * 0.6), Math.Max(1f, 2f * scale));
			}
			graphics.Clip = clip;
		}
		using (Brush brush6 = new SolidBrush(Color.FromArgb(num, 10, 8, 14)))
		{
			graphics.FillRectangle(brush6, iconAt);
		}
		if (face != null)
		{
			using ImageAttributes imageAttrs = Tint(Color.Empty, fade);
			graphics.DrawImage(face, Rect(iconAt), FacePlate.X, FacePlate.Y, FacePlate.Width, FacePlate.Height, GraphicsUnit.Pixel, imageAttrs);
		}
		using Pen pen2 = new Pen(Color.FromArgb(num, Shatter.Mint), 1.4f * scale);
		graphics.DrawRectangle(pen2, iconAt.X, iconAt.Y, iconAt.Width, iconAt.Height);
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			ticks.Dispose();
		}
		base.Dispose(disposing);
	}
}
