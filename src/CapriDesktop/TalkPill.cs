using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Windows.Forms;

namespace CapriDesktop;

internal sealed class TalkPill : Overlay
{
	internal const int TearFrames = 4;

	private const int WashAlpha = 40;

	internal static readonly int[] Bars = new int[5] { 2, 4, 5, 3, 2 };

	internal static readonly Color Fill = Color.FromArgb(246, 9, 11, 15);

	internal static readonly Color Unlit = Color.FromArgb(46, 52, 64);

	internal static readonly Color Caption = Color.FromArgb(168, 174, 184);

	private static readonly Random random = new Random();

	private readonly Timer ticks = new Timer();

	private int hover;

	private float scale = 1f;

	private Point centre;

	private bool chatting;

	private bool muted;

	private int level;

	private int tear;

	private int seed;

	internal bool Motion = true;

	internal bool Chatting => chatting;

	internal bool Muted => muted;

	internal bool Tearing => tear > 0;

	internal event Action Typing;

	internal event Action Voice;

	internal event Action Mute;

	internal TalkPill()
		: base("Talk to Capri")
	{
		Cursor = Look.Pointer;
		ticks.Interval = 45;
		Timer timer = ticks;
		EventHandler value = (object param0, EventArgs param1) =>
		{
			if (tear > 0)
			{
				tear--;
			}
			if (tear == 0)
			{
				ticks.Stop();
			}
			if (Visible)
			{
				Redraw();
			}
		};
		timer.Tick += value;
	}

	internal static Size Measure(float scale)
	{
		return new Size((int)Math.Round(100f * scale), (int)Math.Round(40f * scale));
	}

	internal void Place(Point topCentre, float value)
	{
		if (!Visible || !(topCentre == centre) || value != scale)
		{
			centre = topCentre;
			scale = value;
			Redraw();
		}
	}

	internal void SetChat(bool on, bool micMuted)
	{
		if (on != chatting || micMuted != muted)
		{
			chatting = on;
			muted = micMuted;
			level = 0;
			if (Visible)
			{
				Redraw();
			}
		}
	}

	internal void SetLevel(double value)
	{
		int num = ((chatting && !muted) ? Math.Min(6, (int)(Math.Sqrt(value) * 24.0)) : 0);
		if (num != level)
		{
			level = num;
			if (Visible)
			{
				Redraw();
			}
		}
	}

	private void Redraw()
	{
		if (!Visible && Motion)
		{
			tear = 4;
			seed = random.Next(1, int.MaxValue);
			ticks.Stop();
			ticks.Start();
		}
		Present(Render(scale, hover, chatting, muted, level, tear, seed), new Point(centre.X - Measure(scale).Width / 2, centre.Y));
	}

	internal bool GlitchesIn()
	{
		bool motion = Motion;
		Motion = true;
		try
		{
			Hide();
			Redraw();
			bool flag = Visible && tear == 4;
			using (Bitmap two = Render(scale, hover, chatting, muted, level))
			{
				flag = flag && Differs(Image, two);
			}
			tear = 0;
			ticks.Stop();
			Redraw();
			bool flag2 = tear == 0 && !ticks.Enabled;
			using (Bitmap two2 = Render(scale, hover, chatting, muted, level))
			{
				flag2 = flag2 && !Differs(Image, two2);
			}
			return flag && flag2;
		}
		finally
		{
			Motion = motion;
		}
	}

	private static bool Differs(Bitmap one, Bitmap two)
	{
		if (one == null || two == null || one.Size != two.Size)
		{
			return true;
		}
		for (int i = 0; i < one.Height; i++)
		{
			for (int j = 0; j < one.Width; j++)
			{
				if (one.GetPixel(j, i) != two.GetPixel(j, i))
				{
					return true;
				}
			}
		}
		return false;
	}

	internal static Rectangle Body(float s)
	{
		Size size = Measure(s);
		int num = (int)Math.Round(3f * s);
		return new Rectangle(num, num, size.Width - num * 2, size.Height - num * 2);
	}

	internal static Rectangle MicBody(float s)
	{
		Size size = Measure(s);
		int num = (int)Math.Round(3f * s);
		return new Rectangle(size.Width + (int)Math.Round(4f * s) + num, num, size.Height - num * 2, size.Height - num * 2);
	}

	internal static int Cut(float s)
	{
		return Math.Max(3, (int)Math.Round(6f * s));
	}

	internal static int EdgeWidth(float s)
	{
		return Math.Max(1, (int)Math.Round(1.5 * (double)s));
	}

	internal static string Label(int part, bool chatting, bool muted)
	{
		if (part != 1)
		{
			if (!chatting || !muted)
			{
				return "TALK";
			}
			return "MUTED";
		}
		return "TYPE";
	}

	internal static int Lit(int bar, int level)
	{
		if (level <= 0)
		{
			return 0;
		}
		return Math.Max(1, (int)Math.Round((double)(Bars[bar] * Math.Min(6, level)) / 6.0, MidpointRounding.AwayFromZero));
	}

	private static int Square(float s)
	{
		return Math.Max(2, (int)Math.Round(2f * s));
	}

	private static int Pitch(float s)
	{
		return Math.Max(3, (int)Math.Round(3f * s, MidpointRounding.AwayFromZero));
	}

	internal static Rectangle Segment(Rectangle meter, float s, int bar, int row)
	{
		int num = Square(s);
		return new Rectangle(meter.Left + bar * Pitch(s), meter.Bottom - num - row * Pitch(s), num, num);
	}

	private static Font LabelFont(float s)
	{
		return Look.Mono(8.5f * s, FontStyle.Regular);
	}

	private static Rectangle Ink(Font font, string label)
	{
		using GraphicsPath graphicsPath = new GraphicsPath();
		graphicsPath.AddString(label, font.FontFamily, (int)font.Style, font.Size, PointF.Empty, StringFormat.GenericTypographic);
		RectangleF bounds = graphicsPath.GetBounds();
		return Rectangle.FromLTRB((int)Math.Floor(bounds.Left), (int)Math.Floor(bounds.Top), (int)Math.Ceiling(bounds.Right), (int)Math.Ceiling(bounds.Bottom));
	}

	internal new static Rectangle[] Layout(float s, int part, string label)
	{
		using Font font = LabelFont(s);
		return Layout(s, part, Ink(font, label));
	}

	private new static Rectangle[] Layout(float s, int part, Rectangle ink)
	{
		Rectangle rectangle = Body(s);
		int num = Measure(s).Width / 2;
		int num2 = EdgeWidth(s);
		int num3 = ((part == 1) ? (rectangle.Left + num2) : num);
		int num4 = ((part == 1) ? num : (rectangle.Right - num2));
		int num5 = ((part == 1) ? ((int)Math.Round(12.5f * s)) : (Pitch(s) * 4 + Square(s)));
		int num6 = (int)Math.Round(4f * s);
		int num7 = num3 + (num4 - num3 - num5 - num6 - ink.Width + 1) / 2;
		int num8 = rectangle.Top + rectangle.Height / 2;
		return new Rectangle[2]
		{
			new Rectangle(num7, num8 - num5 / 2, num5, num5),
			new Rectangle(num7 + num5 + num6, num8 - ink.Height / 2, ink.Width, ink.Height)
		};
	}

	internal static Bitmap Render(float s, int hover, bool chatting = false, bool muted = false, int level = 0, int tear = 0, int seed = 0)
	{
		Size size = Measure(s);
		Bitmap bitmap = new Bitmap(chatting ? (size.Width + (int)Math.Round(4f * s) + size.Height) : size.Width, size.Height, PixelFormat.Format32bppPArgb);
		using (Graphics graphics = Graphics.FromImage(bitmap))
		{
			using Font font = LabelFont(s);
			graphics.SmoothingMode = SmoothingMode.AntiAlias;
			graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
			graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
			Rectangle box = Body(s);
			int num = size.Width / 2;
			Rectangle lit = hover switch
			{
				2 => Rectangle.FromLTRB(num, box.Top, box.Right, box.Bottom), 
				1 => Rectangle.FromLTRB(box.Left, box.Top, num, box.Bottom), 
				_ => Rectangle.Empty, 
			};
			Plate(graphics, box, s, lit, Look.Pink, Look.Cyan);
			using (Pen pen = new Pen(Color.FromArgb(66, 72, 86), Math.Max(1, (int)Math.Round(s))))
			{
				graphics.DrawLine(pen, (float)num + 1.5f * s, (float)box.Top + 10f * s, (float)num - 1.5f * s, (float)box.Bottom - 10f * s);
			}
			int num2 = Math.Max(1, (int)Math.Round(s));
			using (Brush brush = new SolidBrush(Color.FromArgb(220, Look.Cyan)))
			{
				graphics.FillRectangle(brush, box.Right - (int)Math.Round(32f * s), box.Top - (int)Math.Round(2f * s), (int)Math.Round(15f * s), num2);
			}
			using (Brush brush2 = new SolidBrush(Color.FromArgb(220, Look.Pink)))
			{
				graphics.FillRectangle(brush2, box.Left + (int)Math.Round(12f * s), box.Bottom + (int)Math.Round(2f * s) - num2, (int)Math.Round(11f * s), num2);
			}
			Color color = Mix(Fill, Look.Cyan, 40);
			string text = Label(2, chatting, muted);
			Rectangle[] array = Layout(s, 1, Ink(font, "TYPE"));
			Rectangle[] array2 = Layout(s, 2, Ink(font, text));
			DrawPencil(graphics, new PointF((float)array[0].X + (float)array[0].Width / 2f, (float)array[0].Y + (float)array[0].Height / 2f), 0.9f * s, (hover == 1) ? Look.Cyan : Look.Ink, (hover == 1) ? color : Fill);
			DrawLabel(graphics, "TYPE", font, array[1], (hover == 1) ? Look.Cyan : Caption);
			DrawMeter(graphics, array2[0], s, chatting, muted, level, (hover == 2 || chatting) ? Look.Cyan : Look.Ink);
			Graphics graphics2 = graphics;
			string label = text;
			Font font2 = font;
			Rectangle box2 = array2[1];
			Color colour;
			if (chatting && muted)
			{
				colour = Look.Red;
			}
			else
			{
				colour = ((hover == 2 || chatting) ? Look.Cyan : Caption);
			}
			DrawLabel(graphics2, label, font2, box2, colour);
			if (chatting)
			{
				Rectangle rectangle = MicBody(s);
				Color color2 = (muted ? Look.Red : Look.Cyan);
				Plate(graphics, rectangle, s, (hover == 3) ? rectangle : Rectangle.Empty, color2, color2);
				Graphics graphics3 = graphics;
				PointF pointF = new PointF((float)rectangle.X + (float)rectangle.Width / 2f, (float)rectangle.Y + (float)rectangle.Height / 2f);
				float s2 = s;
				Color colour2;
				if (hover == 3)
				{
					colour2 = Look.Cyan;
				}
				else
				{
					colour2 = (muted ? Look.Dim : Look.Ink);
				}
				DrawMic(graphics3, pointF, s2, colour2, muted, (hover == 3) ? color : Fill);
			}
		}
		if (tear > 0)
		{
			Tear(bitmap, s, Math.Min(1f, (float)tear / 4f), seed + tear);
		}
		return bitmap;
	}

	private static void Plate(Graphics graphics, Rectangle box, float s, Rectangle lit, Color from, Color to)
	{
		int num = Cut(s);
		int num2 = EdgeWidth(s);
		int num3 = (int)Math.Round(3f * s);
		PointF point = new PointF(box.Left - num3 - 1, 0f);
		PointF point2 = new PointF(box.Right + num3 + 1, 0f);
		float tipY = (float)box.Top + (float)box.Height / 2f;
		using (GraphicsPath graphicsPath = Look.Plate(box, num, -1f, tipY, 0f, both: true))
		{
			using (LinearGradientBrush brush = new LinearGradientBrush(point, point2, Color.FromArgb(34, from), Color.FromArgb(34, to)))
			{
				using Pen pen = new Pen(brush, num3 * 2);
				graphics.DrawPath(pen, graphicsPath);
			}
			using (LinearGradientBrush brush2 = new LinearGradientBrush(point, point2, Color.FromArgb(70, from), Color.FromArgb(70, to)))
			{
				using Pen pen2 = new Pen(brush2, num3);
				graphics.DrawPath(pen2, graphicsPath);
			}
			using (Brush brush3 = new SolidBrush(Fill))
			{
				graphics.FillPath(brush3, graphicsPath);
			}
			graphics.SetClip(graphicsPath);
			int num4 = Math.Max(1, (int)Math.Round(s));
			using (Pen pen3 = new Pen(Color.FromArgb(9, 255, 255, 255), num4))
			{
				for (int i = box.Top; i < box.Bottom; i += Math.Max(3, (int)Math.Round(3f * s)))
				{
					graphics.DrawLine(pen3, box.Left, (float)i + (float)num4 / 2f, box.Right, (float)i + (float)num4 / 2f);
				}
			}
			if (!lit.IsEmpty)
			{
				using Brush brush4 = new SolidBrush(Color.FromArgb(40, Look.Cyan));
				graphics.FillRectangle(brush4, lit);
			}
			graphics.ResetClip();
		}
		float num5 = (float)num2 / 2f;
		using GraphicsPath path = Look.Plate(new RectangleF((float)box.Left + num5, (float)box.Top + num5, box.Width - num2, box.Height - num2), (float)num - num5 * 0.586f, -1f, tipY, 0f, both: true);
		using LinearGradientBrush brush5 = new LinearGradientBrush(point, point2, from, to);
		using Pen pen4 = new Pen(brush5, num2);
		graphics.DrawPath(pen4, path);
	}

	private static Color Mix(Color under, Color over, int alpha)
	{
		return Color.FromArgb(under.R + (over.R - under.R) * alpha / 255, under.G + (over.G - under.G) * alpha / 255, under.B + (over.B - under.B) * alpha / 255);
	}

	private static void DrawLabel(Graphics graphics, string label, Font font, Rectangle box, Color colour)
	{
		Rectangle rectangle = Ink(font, label);
		using Brush brush = new SolidBrush(colour);
		graphics.DrawString(label, font, brush, box.Left - rectangle.Left, box.Top - rectangle.Top, StringFormat.GenericTypographic);
	}

	private static void DrawMeter(Graphics graphics, Rectangle meter, float s, bool chatting, bool muted, int level, Color colour)
	{
		float[] array = new float[5] { 5f, 10f, 14f, 9f, 4f };
		float num = ((!chatting) ? 1f : (0.45f + (float)((!muted) ? level : 0) * 0.14f));
		PointF pointF = new PointF((float)meter.X + (float)meter.Width / 2f, (float)meter.Y + (float)meter.Height / 2f);
		using Pen pen = new Pen((chatting && muted) ? Look.Dim : colour, 2.3f * s);
		pen.StartCap = LineCap.Round;
		pen.EndCap = LineCap.Round;
		for (int i = 0; i < array.Length; i++)
		{
			float num2 = pointF.X + (float)(i - 2) * 3.6f * s;
			float num3 = Math.Max(1.5f, array[i] * num) * s / 2f;
			graphics.DrawLine(pen, num2, pointF.Y - num3, num2, pointF.Y + num3);
		}
	}

	private static void DrawMic(Graphics graphics, PointF centre, float s, Color colour, bool muted, Color back)
	{
		float num = centre.X;
		float num2 = centre.Y + 0.5f * s;
		using (Brush brush = new SolidBrush(colour))
		{
			using GraphicsPath path = Look.Rounded(new RectangleF(num - 3.2f * s, num2 - 9f * s, 6.4f * s, 11f * s), 3.2f * s);
			graphics.FillPath(brush, path);
		}
		using (Pen pen = new Pen(colour, 1.8f * s))
		{
			pen.StartCap = LineCap.Round;
			pen.EndCap = LineCap.Round;
			graphics.DrawArc(pen, num - 6f * s, num2 - 8f * s, 12f * s, 12f * s, 0f, 180f);
			graphics.DrawLine(pen, num, num2 + 4f * s, num, num2 + 7f * s);
			graphics.DrawLine(pen, num - 3.5f * s, num2 + 7f * s, num + 3.5f * s, num2 + 7f * s);
		}
		if (!muted)
		{
			return;
		}
		using Pen pen2 = new Pen(back, 4.2f * s);
		using Pen pen3 = new Pen(Look.Red, 2f * s);
		pen3.StartCap = LineCap.Round;
		pen3.EndCap = LineCap.Round;
		graphics.DrawLine(pen2, num - 7.5f * s, num2 - 9.5f * s, num + 7.5f * s, num2 + 7.5f * s);
		graphics.DrawLine(pen3, num - 7.5f * s, num2 - 9.5f * s, num + 7.5f * s, num2 + 7.5f * s);
	}

	private static void DrawPencil(Graphics graphics, PointF centre, float k, Color colour, Color back)
	{
		GraphicsState gstate = graphics.Save();
		graphics.TranslateTransform(centre.X + 0.5f * k, centre.Y - 0.5f * k);
		graphics.RotateTransform(-45f);
		using (Brush brush = new SolidBrush(colour))
		{
			graphics.FillRectangle(brush, -8f * k, -2.5f * k, 12.6f * k, 5f * k);
			graphics.FillPolygon(brush, new PointF[3]
			{
				new PointF(4.6f * k, -2.5f * k),
				new PointF(9f * k, 0f),
				new PointF(4.6f * k, 2.5f * k)
			});
		}
		using (Pen pen = new Pen(back, 1.1f * k))
		{
			graphics.DrawLine(pen, -4.8f * k, -3f * k, -4.8f * k, 3f * k);
			graphics.DrawLine(pen, 4f * k, -3f * k, 4f * k, 3f * k);
		}
		graphics.Restore(gstate);
	}

	private static void Tear(Bitmap image, float s, float amount, int seed)
	{
		Random random = new Random(seed);
		bool flag = image.Width > Measure(s).Width;
		using Bitmap image2 = image.Clone(new Rectangle(0, 0, image.Width, image.Height), image.PixelFormat);
		using Graphics graphics = Graphics.FromImage(image);
		using Brush brush = new SolidBrush(Color.Transparent);
		int num = 2 + (int)Math.Round(2f * amount);
		for (int i = 0; i < num; i++)
		{
			int num2 = Math.Max(2, (int)Math.Round((float)(2 + random.Next(6)) * s));
			int num3 = random.Next(Math.Max(1, image.Height - num2));
			int num4 = ((random.Next(2) != 0) ? 1 : (-1)) * Math.Max(1, (int)Math.Round((float)(2 + random.Next(6)) * s * amount));
			Rectangle rectangle = new Rectangle(0, num3, image.Width, num2);
			graphics.SetClip(rectangle);
			graphics.CompositingMode = CompositingMode.SourceCopy;
			graphics.FillRectangle(brush, rectangle);
			graphics.DrawImage(image2, new Rectangle(num4, 0, image.Width, image.Height));
			graphics.CompositingMode = CompositingMode.SourceOver;
			graphics.SetClip(Body(s));
			if (flag)
			{
				graphics.SetClip(MicBody(s), CombineMode.Union);
			}
			graphics.IntersectClip(rectangle);
			using (Brush brush2 = new SolidBrush(Color.FromArgb((int)(96f * amount), (i % 2 == 0) ? Look.Pink : Look.Cyan)))
			{
				graphics.FillRectangle(brush2, rectangle);
			}
			graphics.ResetClip();
		}
	}

	internal static int PartAt(int x, float s, bool chatting)
	{
		int num = Measure(s).Width;
		if (chatting && x >= num)
		{
			return 3;
		}
		if (x >= num / 2)
		{
			return 2;
		}
		return 1;
	}

	private int PartAt(int x)
	{
		return PartAt(x, scale, chatting);
	}

	protected override void OnMouseMove(MouseEventArgs e)
	{
		base.OnMouseMove(e);
		int num = PartAt(e.X);
		if (num != hover)
		{
			hover = num;
			Redraw();
		}
	}

	protected override void OnMouseLeave(EventArgs e)
	{
		base.OnMouseLeave(e);
		if (hover != 0)
		{
			hover = 0;
			Redraw();
		}
	}

	protected override void OnMouseUp(MouseEventArgs e)
	{
		base.OnMouseUp(e);
		if (e.Button == MouseButtons.Left)
		{
			Press(PartAt(e.X));
		}
	}

	internal void Press(int part)
	{
		switch (part)
		{
		case 1:
			if (Typing != null)
			{
				Typing();
			}
			break;
		case 3:
			if (Mute != null)
			{
				Mute();
			}
			break;
		default:
			if (Voice != null)
			{
				Voice();
			}
			break;
		}
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			ticks.Dispose();
		}
		base.Dispose(disposing);
	}

	internal static string Check()
	{
		float[] array = new float[3] { 1f, 1.5f, 2f };
		foreach (float num in array)
		{
			string text = " at " + (int)Math.Round(num * 100f) + "%";
			Size size = Measure(num);
			Rectangle rectangle = Body(num);
			Rectangle rectangle2 = MicBody(num);
			int num2 = Cut(num);
			int num3 = EdgeWidth(num);
			int num4 = size.Width / 2;
			int num5 = size.Width + (int)Math.Round(4f * num) + size.Height;
			Bitmap[] array2 = new Bitmap[8]
			{
				Render(num, 0),
				Render(num, 1),
				Render(num, 2),
				Render(num, 0, chatting: true),
				Render(num, 0, chatting: true, muted: false, 3),
				Render(num, 0, chatting: true, muted: false, 6),
				Render(num, 0, chatting: true, muted: true),
				Render(num, 3, chatting: true)
			};
			try
			{
				Bitmap bitmap = array2[0];
				Bitmap bitmap2 = array2[1];
				Bitmap bitmap3 = array2[2];
				Bitmap bitmap4 = array2[3];
				Bitmap image = array2[4];
				Bitmap image2 = array2[5];
				Bitmap bitmap5 = array2[6];
				Bitmap image3 = array2[7];
				if (bitmap.Size != size || bitmap2.Size != size || bitmap4.Width != num5 || bitmap4.Height != size.Height)
				{
					return string.Concat("Its size changed", text, ": ", bitmap.Size, ", ", bitmap4.Size);
				}
				if (PartAt(0, num, chatting: false) != 1 || PartAt(num4 - 1, num, chatting: false) != 1 || PartAt(num4, num, chatting: false) != 2 || PartAt(size.Width - 1, num, chatting: true) != 2 || PartAt(size.Width, num, chatting: true) != 3 || PartAt(num5 - 1, num, chatting: true) != 3 || PartAt(size.Width + 2, num, chatting: false) != 2)
				{
					return "Its hit areas moved" + text;
				}
				Rectangle[] array3 = new Rectangle[2] { rectangle, rectangle2 };
				for (int j = 0; j < array3.Length; j++)
				{
					Rectangle rectangle3 = array3[j];
					Bitmap bitmap6 = ((rectangle3 == rectangle) ? bitmap : bitmap4);
					if (bitmap6.GetPixel(rectangle3.Left, rectangle3.Top).A > 140 || bitmap6.GetPixel(rectangle3.Right - 1, rectangle3.Bottom - 1).A > 140 || bitmap6.GetPixel(rectangle3.Right - 1, rectangle3.Top).A > 140 || bitmap6.GetPixel(rectangle3.Left, rectangle3.Top + num2 + 1).A < 250 || bitmap6.GetPixel(rectangle3.Right - 1, rectangle3.Bottom - num2 - 2).A < 250 || bitmap6.GetPixel(rectangle3.Left + num2 + 1, rectangle3.Top).A < 250 || bitmap6.GetPixel(rectangle3.Right - num2 - 2, rectangle3.Bottom - 1).A < 250)
					{
						return "Its corners aren't cut" + text;
					}
				}
				if (Brightest(bitmap.GetPixel(rectangle.Left + num2 + 2, rectangle.Top + num3 - 1)) < 150 || Brightest(bitmap.GetPixel(rectangle.Left + num2 + 2, rectangle.Top + num3)) > 60 || Brightest(bitmap.GetPixel(rectangle.Left + num3 - 1, rectangle.Top + rectangle.Height / 2)) < 150 || Brightest(bitmap.GetPixel(rectangle.Left + num3, rectangle.Top + rectangle.Height / 2)) > 60)
				{
					return "Its neon edge isn't crisp" + text;
				}
				if (!Red(bitmap5.GetPixel(rectangle2.Left, rectangle2.Top + num2 + 1)) || Red(bitmap4.GetPixel(rectangle2.Left, rectangle2.Top + num2 + 1)))
				{
					return "The mic button's edge didn't go red only while muted" + text;
				}
				Rectangle[] array4 = Layout(num, 1, "TYPE");
				Rectangle[] array5 = Layout(num, 2, "TALK");
				Rectangle[] array6 = Layout(num, 2, "MUTED");
				int num6 = (int)(6f * num * num);
				if (Count(bitmap, array4[1], Grey) < num6 || Count(bitmap, array5[1], Grey) < num6)
				{
					return "Its TYPE and TALK labels are missing" + text;
				}
				if (Count(bitmap2, array4[1], Cyan) < num6 || Count(bitmap2, array5[1], Cyan) > 0 || Count(bitmap3, array5[1], Cyan) < num6 || Count(bitmap3, array4[1], Cyan) > 0)
				{
					return "Hovering didn't light just its part cyan" + text;
				}
				Point point = new Point(array4[0].X + array4[0].Width / 2, array4[0].Y + array4[0].Height / 2);
				if (!Grey(bitmap.GetPixel(point.X, point.Y)) || !Cyan(bitmap2.GetPixel(point.X, point.Y)))
				{
					return "The pencil didn't light cyan on hover" + text;
				}
				if (Count(bitmap5, array6[1], Red) < num6 || Count(bitmap4, array6[1], Red) > 0 || Count(bitmap, array6[1], Red) > 0)
				{
					return "MUTED didn't show in red, or showed unmuted" + text;
				}
				Rectangle box = Rectangle.Inflate(rectangle2, -num2 - num3, -num2 - num3);
				if (Count(image3, box, Cyan) < num6 || Count(bitmap4, box, Cyan) > 0)
				{
					return "Hovering the mic didn't light it cyan" + text;
				}
				Rectangle box2 = Rectangle.Inflate(array5[0], (int)(4f * num), (int)(4f * num));
				Rectangle box3 = Rectangle.Inflate(array6[0], (int)(4f * num), (int)(4f * num));
				Point point2 = new Point(array5[0].X + array5[0].Width / 2, array5[0].Y + array5[0].Height / 2);
				if (!Grey(bitmap.GetPixel(point2.X, point2.Y)) || !Cyan(bitmap3.GetPixel(point2.X, point2.Y)))
				{
					return "The wave didn't show" + text;
				}
				int num7 = Count(bitmap4, box2, Cyan);
				int num8 = Count(image, box2, Cyan);
				int num9 = Count(image2, box2, Cyan);
				if (num7 >= num8 || num8 >= num9)
				{
					return "The wave didn't stretch with the mic level" + text + ": " + num7 + ", " + num8 + ", " + num9;
				}
				if (Count(bitmap5, box3, Cyan) > 0)
				{
					return "The wave lit while muted" + text;
				}
			}
			finally
			{
				Bitmap[] array7 = array2;
				foreach (Bitmap bitmap7 in array7)
				{
					bitmap7.Dispose();
				}
			}
		}
		return null;
	}

	private static int Brightest(Color c)
	{
		return Math.Max(c.R, Math.Max(c.G, c.B));
	}

	private static bool Grey(Color c)
	{
		if (c.A > 200 && Math.Min(c.R, Math.Min(c.G, c.B)) > 80)
		{
			return Brightest(c) - Math.Min(c.R, Math.Min(c.G, c.B)) < 40;
		}
		return false;
	}

	private static bool Cyan(Color c)
	{
		if (c.A > 200 && c.G > 140 && c.B > 150)
		{
			return c.R < 130;
		}
		return false;
	}

	private static bool Red(Color c)
	{
		if (c.A > 200 && c.R > 150 && c.G < 90)
		{
			return c.B < 120;
		}
		return false;
	}

	private static int Count(Bitmap image, Rectangle box, Func<Color, bool> test)
	{
		int num = 0;
		for (int i = Math.Max(0, box.Top); i < Math.Min(image.Height, box.Bottom); i++)
		{
			for (int j = Math.Max(0, box.Left); j < Math.Min(image.Width, box.Right); j++)
			{
				if (test(image.GetPixel(j, i)))
				{
					num++;
				}
			}
		}
		return num;
	}
}
