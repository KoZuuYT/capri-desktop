using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace CapriDesktop;

internal sealed class Shatter : Overlay
{
	internal sealed class Shard
	{
		internal float X;

		internal float Y;

		internal float Size;

		internal float Angle;

		internal float Spin;

		internal float Way;

		internal float Speed;

		internal float Rise;

		internal float Delay;

		internal Color Colour;
	}

	internal const double LeaveLength = 0.95;

	internal const double ArriveLength = 0.72;

	internal const double Handoff = 0.42;

	internal static readonly Color Violet = Color.FromArgb(122, 60, 255);

	internal static readonly Color Mint = Color.FromArgb(30, 240, 138);

	private readonly Timer ticks = new Timer();

	private readonly Stopwatch watch = new Stopwatch();

	private readonly Random random = new Random();

	private Bitmap picture;

	private List<Shard> shards;

	private Rectangle area;

	private Point at;

	private bool leaving;

	private float scale;

	private int seed;

	protected override CreateParams CreateParams
	{
		get
		{
			CreateParams createParams = base.CreateParams;
			createParams.ExStyle |= 32;
			return createParams;
		}
	}

	internal Shatter()
		: base("Capri shatter")
	{
		ticks.Interval = 33;
		Timer timer = ticks;
		EventHandler value = (object param0, EventArgs param1) =>
		{
			Step();
		};
		timer.Tick += value;
	}

	internal static Rectangle Room(Rectangle canvas)
	{
		return Rectangle.FromLTRB(canvas.Left - canvas.Width * 3 / 4, canvas.Top - canvas.Height, canvas.Right + canvas.Width * 3 / 4, canvas.Bottom + canvas.Height / 5);
	}

	internal void Play(Bitmap self, Point canvas, Rectangle screen, bool leave, int size)
	{
		Stop();
		area = Rectangle.Intersect(Room(new Rectangle(canvas, self.Size)), screen);
		if (area.Width < 8 || area.Height < 8)
		{
			self.Dispose();
			return;
		}
		picture = self;
		scale = (float)size / 240f;
		at = new Point(canvas.X - area.Left, canvas.Y - area.Top);
		leaving = leave;
		seed = random.Next();
		shards = Cut(self, scale, leave, seed);
		watch.Reset();
		watch.Start();
		Step();
		ticks.Start();
	}

	internal void Stop()
	{
		ticks.Stop();
		if (Visible)
		{
			Hide();
		}
		if (picture != null)
		{
			picture.Dispose();
			picture = null;
		}
	}

	private void Step()
	{
		double totalSeconds = watch.Elapsed.TotalSeconds;
		if (picture == null || totalSeconds >= (leaving ? 0.95 : 0.72))
		{
			Stop();
		}
		else
		{
			Present(Frame(area.Size, picture, at, shards, totalSeconds, leaving, scale, seed, (int)(totalSeconds * 30.0)), area.Location);
		}
	}

	private static double Smooth(double x)
	{
		x = Math.Max(0.0, Math.Min(1.0, x));
		return x * x * (3.0 - 2.0 * x);
	}

	internal static List<Shard> Cut(Bitmap self, float scale, bool leave, int seed)
	{
		Random random = new Random(seed);
		List<Shard> list = new List<Shard>();
		int num = Math.Max(6, (int)Math.Round(9f * scale));
		float num2 = (float)self.Width / 2f;
		float num3 = (float)self.Height / 2f;
		Color[] array = new Color[5]
		{
			Violet,
			Mint,
			Color.White,
			Look.Red,
			Look.Pink
		};
		for (int i = num / 2; i < self.Height; i += num)
		{
			for (int j = num / 2; j < self.Width; j += num)
			{
				Color pixel = self.GetPixel(j, i);
				if (pixel.A >= 140)
				{
					Shard shard = new Shard();
					shard.X = (float)j + (float)(random.NextDouble() - 0.5) * (float)num * 0.6f;
					shard.Y = (float)i + (float)(random.NextDouble() - 0.5) * (float)num * 0.6f;
					shard.Size = (float)num * (0.55f + 0.6f * (float)random.NextDouble());
					shard.Angle = (float)(random.NextDouble() * Math.PI);
					shard.Spin = (float)((random.NextDouble() * 2.0 - 1.0) * 9.0);
					shard.Way = (float)(Math.Atan2((float)i - num3, (float)j - num2) + (random.NextDouble() * 2.0 - 1.0) * 0.9);
					shard.Speed = (float)((170.0 + random.NextDouble() * 430.0) * (double)scale);
					shard.Rise = (float)((40.0 + random.NextDouble() * 200.0) * (double)scale);
					shard.Delay = (float)(leave ? (0.1 + 0.12 * (1.0 - (double)i / (double)self.Height) + random.NextDouble() * 0.14) : (0.04 + random.NextDouble() * 0.12));
					bool flag = random.Next(100) < 9;
					if (flag)
					{
						shard.Size *= 2.2f;
					}
					shard.Colour = ((flag || random.Next(10) < 3) ? array[random.Next(array.Length)] : Color.FromArgb(255, pixel.R, pixel.G, pixel.B));
					list.Add(shard);
				}
			}
		}
		return list;
	}

	internal static Bitmap Frame(Size size, Bitmap self, Point at, List<Shard> shards, double t, bool leave, float scale, int seed, int flicker)
	{
		Bitmap bitmap = new Bitmap(size.Width, size.Height, PixelFormat.Format32bppPArgb);
		Random pick = new Random(seed + flicker * 7919);
		double num = (leave ? 0.95 : 0.72);
		float num2 = (float)at.X + (float)self.Width / 2f;
		float middleY = (float)at.Y + (float)self.Height * 0.45f;
		float bottom = (float)at.Y + (float)self.Height * 0.9f;
		using (Graphics graphics = Graphics.FromImage(bitmap))
		{
			graphics.SmoothingMode = SmoothingMode.AntiAlias;
			graphics.InterpolationMode = InterpolationMode.HighQualityBilinear;
			if (leave)
			{
				Beam(graphics, num2, 0f, bottom, t - 0.45, 0.06, 0.12, 0.33, up: true, scale, pick);
			}
			else
			{
				Beam(graphics, num2, 0f, bottom, t, 0.08, 0.12, 0.26, up: false, scale, pick);
			}
			double num3;
			if (leave)
			{
				num3 = 1.0 - Smooth((t - 0.14) / 0.2);
			}
			else
			{
				num3 = ((t < 0.47) ? Smooth((t - 0.28) / 0.12) : 0.0);
			}
			double glitch = (leave ? Math.Min(1.0, 0.35 + t / 0.12) : (1.0 - Smooth((t - 0.34) / 0.1) * 0.6));
			if (num3 > 0.01)
			{
				double warp = (leave ? Smooth(t / 0.34) : 1.0 - Smooth((t - 0.26) / 0.2));
				Sliced(graphics, self, at, num3, glitch, scale, pick, warp, t);
			}
			PointF[] array = new PointF[4];
			foreach (Shard shard in shards)
			{
				float num6;
				float num7;
				float num8;
				float num9;
				if (leave)
				{
					double num4 = t - (double)shard.Delay;
					if (num4 < 0.0)
					{
						continue;
					}
					double num5 = (1.0 - Math.Exp(-3.0 * num4)) / 3.0;
					num6 = shard.X + (float)(Math.Cos(shard.Way) * (double)shard.Speed * num5);
					num7 = shard.Y + (float)(Math.Sin(shard.Way) * (double)shard.Speed * num5 * 0.6 - (double)shard.Rise * num4);
					num8 = shard.Size * (float)(1.0 - 0.55 * Smooth(num4 / 0.7));
					num9 = (float)(1.0 - Smooth((t - (num - 0.32)) / 0.32));
				}
				else
				{
					double num10 = 0.2 + (double)shard.Delay;
					double num11 = num10 - 0.24;
					if (t < num11)
					{
						continue;
					}
					double num12 = 1.0 - Smooth((t - num11) / 0.24);
					num6 = shard.X + (float)(Math.Cos(shard.Way) * (double)shard.Speed * 0.38 * num12);
					num7 = shard.Y + (float)(Math.Sin(shard.Way) * (double)shard.Speed * 0.38 * num12 * 0.6 - (double)shard.Rise * 0.5 * num12);
					num8 = shard.Size * (float)(0.6 + 0.4 * (1.0 - num12));
					num9 = (float)(Smooth((t - num11) / 0.1) * (1.0 - Smooth((t - 0.42) / 0.06)));
				}
				if (!((double)num9 <= 0.01) && !(num8 <= 0.5f))
				{
					double num13 = (double)shard.Angle + (double)shard.Spin * t;
					float num14 = (float)Math.Cos(num13);
					float num15 = (float)Math.Sin(num13);
					float num16 = num8 * 0.5f;
					float num17 = (float)at.X + num6;
					float num18 = (float)at.Y + num7;
					ref PointF reference = ref array[0];
					reference = new PointF(num17 + num8 * num15, num18 - num8 * num14);
					ref PointF reference2 = ref array[1];
					reference2 = new PointF(num17 + num16 * num14, num18 + num16 * num15);
					ref PointF reference3 = ref array[2];
					reference3 = new PointF(num17 - num8 * num15, num18 + num8 * num14);
					ref PointF reference4 = ref array[3];
					reference4 = new PointF(num17 - num16 * num14, num18 - num16 * num15);
					using Brush brush = new SolidBrush(Color.FromArgb((int)(255f * num9), shard.Colour));
					graphics.FillPolygon(brush, array);
				}
			}
			if (!leave)
			{
				Lines(graphics, num2, middleY, (t - 0.3) / 0.42, scale, seed);
			}
		}
		return bitmap;
	}

	private static void Beam(Graphics graphics, float middle, float top, float bottom, double b, double rise, double hold, double fall, bool up, float scale, Random pick)
	{
		if (b < 0.0 || b > rise + hold + fall)
		{
			return;
		}
		double num = Smooth(b / rise);
		double num2 = (double)(30f * scale) * ((b < rise) ? num : (1.0 - Smooth((b - rise - hold) / fall)));
		if (num2 < 0.6)
		{
			return;
		}
		float num3 = (up ? ((float)((double)bottom - (double)(bottom - top) * num)) : top);
		float num4 = (up ? bottom : ((float)((double)top + (double)(bottom - top) * num)));
		if (num4 - num3 < 2f)
		{
			return;
		}
		float num5 = (float)num2;
		float glow = (float)Math.Min(1.0, num2 / (double)(30f * scale));
		RectangleF rect = new RectangleF(middle - num5 * 1.6f, num3, num5 * 3.2f, num4 - num3);
		using (LinearGradientBrush linearGradientBrush = new LinearGradientBrush(rect, Color.Transparent, Color.Transparent, LinearGradientMode.Horizontal))
		{
			ColorBlend colorBlend = new ColorBlend();
			colorBlend.Colors = new Color[3]
			{
				Color.FromArgb(0, Crackle.Hot),
				Color.FromArgb((int)(70f * glow), Crackle.Hot),
				Color.FromArgb(0, Crackle.Hot)
			};
			colorBlend.Positions = new float[3] { 0f, 0.5f, 1f };
			linearGradientBrush.InterpolationColors = colorBlend;
			graphics.FillRectangle(linearGradientBrush, rect);
		}
		// the strike itself: two jagged bolts twisting around each other, re-jagged every frame, with forks
		float bolt = Math.Max(1.5f, num5 * 0.22f);
		Crackle.Bolt(graphics, new PointF(middle, num3), new PointF(middle, num4), bolt, Crackle.Hot, 1f, pick, 0.12f, 4);
		Crackle.Bolt(graphics, new PointF(middle + (float)(pick.NextDouble() - 0.5) * num5, num3), new PointF(middle, num4), bolt * 0.6f, Crackle.Ember, 0.8f, pick, 0.16f, 2);
		using Brush brush3 = new SolidBrush(Color.FromArgb((int)(150f * glow), 255, 60, 70));
		graphics.FillEllipse(brush3, middle - num5 * 1.6f, num4 - num5 * 0.35f, num5 * 3.2f, num5 * 0.7f);
	}

	// warp is her lightning form taking over: the body stretches up into the bolt, thins, and each slice
	// waves sideways like a flame, so she looks melted and misshapen at the start and end of the jump
	private static void Sliced(Graphics graphics, Bitmap self, Point at, double shown, double glitch, float scale, Random pick, double warp, double t)
	{
		using (ImageAttributes key = Tint(Color.Empty, shown))
		{
			using ImageAttributes key2 = Tint(Mint, shown * 0.6 * glitch);
			using ImageAttributes key3 = Tint(Violet, shown * 0.6 * glitch);
			float num = (float)((double)(4f * scale) * glitch);
			int num2;
			for (int i = 0; i < self.Height; i += num2)
			{
				num2 = Math.Min(self.Height - i, Math.Max(3, (int)((double)self.Height * (0.03 + pick.NextDouble() * 0.13))));
				float num3 = ((pick.Next(3) == 0) ? ((float)((pick.NextDouble() * 2.0 - 1.0) * 18.0 * (double)scale * glitch)) : 0f);
				double rise = 1.0 - (double)(i + num2 / 2) / (double)self.Height;
				num3 += (float)(Math.Sin(rise * 9.0 + t * 38.0) * 22.0 * (double)scale * warp * (0.3 + rise));
				float stretch = (float)(1.0 + warp * 0.9);
				float thin = (float)(1.0 - warp * 0.45 * (0.4 + rise));
				int destW = Math.Max(1, (int)((float)self.Width * thin));
				int destX = at.X + (self.Width - destW) / 2;
				int destY = at.Y + self.Height - (int)((float)(self.Height - i) * stretch);
				int destH = Math.Max(1, (int)Math.Ceiling((float)num2 * stretch));
				KeyValuePair<ImageAttributes, float>[] array = new KeyValuePair<ImageAttributes, float>[3]
				{
					new KeyValuePair<ImageAttributes, float>(key2, num3 - num),
					new KeyValuePair<ImageAttributes, float>(key3, num3 + num),
					new KeyValuePair<ImageAttributes, float>(key, num3)
				};
				for (int j = 0; j < array.Length; j++)
				{
					KeyValuePair<ImageAttributes, float> keyValuePair = array[j];
					graphics.DrawImage(self, new Rectangle(destX + (int)Math.Round(keyValuePair.Value), destY, destW, destH), 0, i, self.Width, num2, GraphicsUnit.Pixel, keyValuePair.Key);
				}
			}
		}
		Color[] array2 = new Color[4]
		{
			Color.White,
			Mint,
			Look.Red,
			Violet
		};
		int k = 0;
		for (int num4 = (int)Math.Round(5.0 * glitch); k < num4; k++)
		{
			int num5 = at.Y + (int)((double)self.Height * (0.1 + pick.NextDouble() * 0.8));
			int num6 = (int)((double)self.Width * (0.25 + pick.NextDouble() * 0.5));
			int num7 = at.X + (int)((double)self.Width * (0.5 + (pick.NextDouble() - 0.5) * 0.5)) - num6 / 2;
			using Brush brush = new SolidBrush(Color.FromArgb((int)(210.0 * shown), array2[pick.Next(array2.Length)]));
			graphics.FillRectangle(brush, num7, num5, num6, Math.Max(2, (int)((float)(1 + pick.Next(3)) * scale)));
		}
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
		ColorMatrix colorMatrix2 = colorMatrix;
		imageAttributes.SetColorMatrix(colorMatrix2);
		return imageAttributes;
	}

	private static void Lines(Graphics graphics, float middleX, float middleY, double u, float scale, int seed)
	{
		if (u <= 0.0 || u >= 1.0)
		{
			return;
		}
		Random random = new Random(seed);
		Color[] array = new Color[5]
		{
			Color.White,
			Color.White,
			Color.White,
			Look.Red,
			Violet
		};
		float num = 120f * scale * (float)(0.7 + 0.9 * Smooth(u));
		int alpha = (int)(235.0 * (1.0 - Smooth((u - 0.35) / 0.65)));
		for (int i = 0; i < 28; i++)
		{
			double num2 = ((double)i + random.NextDouble() * 0.7) / 28.0 * 2.0 * Math.PI;
			float num3 = num * (float)(0.85 + random.NextDouble() * 0.5);
			float num4 = num3 + 150f * scale * (float)(0.5 + random.NextDouble());
			float num5 = (float)((2.0 + random.NextDouble() * 6.0) * (double)scale);
			Color baseColor = array[random.Next(array.Length)];
			float num6 = (float)Math.Cos(num2);
			float num7 = (float)Math.Sin(num2);
			PointF[] points = new PointF[3]
			{
				new PointF(middleX + num6 * num3, middleY + num7 * num3 * 0.8f),
				new PointF(middleX + num6 * num4 - num7 * num5, middleY + (num7 * num4 + num6 * num5) * 0.8f),
				new PointF(middleX + num6 * num4 + num7 * num5, middleY + (num7 * num4 - num6 * num5) * 0.8f)
			};
			using Brush brush = new SolidBrush(Color.FromArgb(alpha, baseColor));
			graphics.FillPolygon(brush, points);
		}
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			ticks.Dispose();
			if (picture != null)
			{
				picture.Dispose();
				picture = null;
			}
		}
		base.Dispose(disposing);
	}
}
