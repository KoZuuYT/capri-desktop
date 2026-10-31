using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace CapriDesktop;

internal sealed class MiniPlayer : Overlay
{
	internal const int Wide = 336;

	internal const int Tall = 116;

	internal const float Perch = 0.1f;

	private const int TitleY = 11;

	private const int ArtistY = 31;

	private const int SeekY = 58;

	private const int ControlsY = 88;

	private readonly Timer ticks = new Timer();

	private readonly Random random = new Random();

	private Tune tune;

	private bool liked;

	private float scale = 1f;

	private Point? corner;

	private int hover;

	private int held;

	private bool grabbing;

	private Point grabbed;

	private Point grabbedAt;

	private double scrub = -1.0;

	private int legsFrom = -1;

	private int legsTo = -1;

	private double nextGlitch;

	private double glitchUntil = -1.0;

	private int glitchSeed;

	private double sentAt = -10.0;

	private double heartAt = -10.0;

	internal bool Motion = true;

	internal static Func<Point> Mouse = () => Cursor.Position;

	internal Tune Tune => tune;

	internal bool Liked => liked;

	internal float ScaleShown => scale;

	internal double SentAt => sentAt;

	internal int LegsFrom => legsFrom;

	internal int LegsTo => legsTo;

	internal bool Busy
	{
		get
		{
			if (Visible)
			{
				if (!grabbing && held == 0)
				{
					return Bounds.Contains(Mouse());
				}
				return true;
			}
			return false;
		}
	}

	private int Glow => (int)Math.Round(12f * scale);

	internal Rectangle Plate
	{
		get
		{
			int glow = Glow;
			if (!Visible)
			{
				return Rectangle.Empty;
			}
			return new Rectangle(Left + glow, Top + glow, Width - glow * 2, Height - glow * 2);
		}
	}

	internal event Action<string> Command;

	internal event Action<Point> Moved;

	[DllImport("user32.dll")]
	private static extern bool SetWindowDisplayAffinity(IntPtr window, uint affinity);

	internal MiniPlayer()
		: base("Capri mini player")
	{
		AccessibleName = "Capri mini player";
		Cursor = Look.Pointer;
		ticks.Interval = 500;
		Timer timer = ticks;
		EventHandler value = (object param0, EventArgs param1) =>
		{
			Step();
		};
		timer.Tick += value;
	}

	private float Dpi()
	{
		int num = 96;
		try
		{
			num = Native.GetDpiForWindow(Handle);
		}
		catch (EntryPointNotFoundException)
		{
		}
		return Math.Max(1f, (float)num / 96f);
	}

	internal void ShowTune(Tune now, Point? spot)
	{
		if (now != null)
		{
			if (tune == null || now.Key != tune.Key)
			{
				liked = false;
				heartAt = -10.0;
			}
			tune = now;
			if (!Visible)
			{
				corner = spot;
				hover = 0;
				held = 0;
				scrub = -1.0;
				scale = Dpi();
				nextGlitch = Tunes.Now + 3.0 + random.NextDouble() * 5.0;
			}
			Redraw();
			if (!ticks.Enabled)
			{
				ticks.Start();
			}
		}
	}

	private void Redraw()
	{
		if (tune != null)
		{
			double now = Tunes.Now;
			Bitmap bitmap = Render(scale, tune, now, hover, liked, scrub, legsFrom, legsTo, (Motion && glitchUntil >= 0.0) ? glitchSeed : 0);
			Present(bitmap, Spot(bitmap.Size));
			float num = Dpi();
			if (num != scale)
			{
				scale = num;
				bitmap = Render(scale, tune, now, hover, liked, scrub, legsFrom, legsTo, 0);
				Present(bitmap, Spot(bitmap.Size));
			}
		}
	}

	private Point Spot(Size size)
	{
		int glow = Glow;
		Rectangle workingArea;
		Point point;
		if (corner.HasValue)
		{
			workingArea = Screen.FromPoint(corner.Value).WorkingArea;
			point = new Point(corner.Value.X - glow, corner.Value.Y + glow - size.Height);
		}
		else
		{
			workingArea = Screen.PrimaryScreen.WorkingArea;
			int num = (int)Math.Round(12f * scale);
			point = new Point(workingArea.Left + num - glow, workingArea.Bottom - num + glow - size.Height);
		}
		return new Point(Math.Max(workingArea.Left - glow, Math.Min(point.X, workingArea.Right + glow - size.Width)), Math.Max(workingArea.Top - glow, Math.Min(point.Y, workingArea.Bottom + glow - size.Height)));
	}

	private void Step()
	{
		if (!Visible || tune == null)
		{
			ticks.Stop();
			return;
		}
		double now = Tunes.Now;
		if (glitchUntil >= 0.0 && now >= glitchUntil)
		{
			glitchUntil = -1.0;
			nextGlitch = now + 8.0 + random.NextDouble() * 10.0;
			ticks.Interval = 500;
		}
		else if (glitchUntil < 0.0 && Motion && now >= nextGlitch)
		{
			glitchUntil = now + 0.36;
			ticks.Interval = 60;
		}
		if (glitchUntil >= 0.0)
		{
			glitchSeed = random.Next(1, int.MaxValue);
		}
		Redraw();
	}

	protected override void OnVisibleChanged(EventArgs e)
	{
		base.OnVisibleChanged(e);
		if (!Visible)
		{
			ticks.Stop();
			grabbing = false;
			held = 0;
			scrub = -1.0;
			hover = 0;
			glitchUntil = -1.0;
			ticks.Interval = 500;
		}
		Seat();
	}

	protected override void OnMove(EventArgs e)
	{
		base.OnMove(e);
		Seat();
	}

	protected override void OnSizeChanged(EventArgs e)
	{
		base.OnSizeChanged(e);
		Seat();
	}

	private void Seat()
	{
		if (IsHandleCreated)
		{
			Ledges.OwnSeat(Handle, Plate);
		}
	}

	internal void Sitting(int? hipX, int size, bool facingLeft)
	{
		Rectangle plate = Plate;
		int num = -1;
		int num2 = -1;
		if (hipX.HasValue && size > 0 && !plate.IsEmpty)
		{
			int num3 = hipX.Value - plate.Left;
			num = num3 - (int)((float)size * (facingLeft ? 0.36f : 0.1f));
			num2 = num3 + (int)((float)size * (facingLeft ? 0.1f : 0.36f));
		}
		if (num != legsFrom || num2 != legsTo)
		{
			legsFrom = num;
			legsTo = num2;
			if (Visible)
			{
				Redraw();
			}
		}
	}

	internal bool Unseen(bool unseen)
	{
		if (!IsHandleCreated)
		{
			return false;
		}
		try
		{
			SetWindowDisplayAffinity(Handle, unseen ? 17u : 0u);
		}
		catch (EntryPointNotFoundException)
		{
			return false;
		}
		if (unseen)
		{
			return Visible;
		}
		return false;
	}

	private PointF OnPlate(Point at)
	{
		int glow = Glow;
		return new PointF(at.X - glow, at.Y - glow);
	}

	private double Fraction(float x)
	{
		return Math.Max(0f, Math.Min(1f, (x - 18f * scale) / (300f * scale)));
	}

	protected override void OnMouseDown(MouseEventArgs e)
	{
		base.OnMouseDown(e);
		if (e.Button != MouseButtons.Left || tune == null)
		{
			return;
		}
		int num = PartAt(OnPlate(e.Location), scale, tune.Spotify);
		if (num == 0)
		{
			Grab(PointToScreen(e.Location));
			return;
		}
		held = num;
		Capture = true;
		if (num == 6 && tune.CanSeek)
		{
			scrub = Fraction(OnPlate(e.Location).X);
			Redraw();
		}
	}

	protected override void OnMouseMove(MouseEventArgs e)
	{
		base.OnMouseMove(e);
		if (grabbing)
		{
			Haul(PointToScreen(e.Location));
		}
		else
		{
			if (tune == null)
			{
				return;
			}
			if (scrub >= 0.0)
			{
				scrub = Fraction(OnPlate(e.Location).X);
				Redraw();
				return;
			}
			int num = PartAt(OnPlate(e.Location), scale, tune.Spotify);
			Cursor = ((num == 0) ? Cursors.SizeAll : Look.Pointer);
			if (num != hover)
			{
				hover = num;
				Redraw();
			}
		}
	}

	protected override void OnMouseUp(MouseEventArgs e)
	{
		base.OnMouseUp(e);
		if (grabbing)
		{
			LetGo();
			return;
		}
		int num = held;
		double num2 = scrub;
		held = 0;
		scrub = -1.0;
		if (Capture)
		{
			Capture = false;
		}
		if (tune != null)
		{
			if (e.Button == MouseButtons.Right)
			{
				Raise("menu");
			}
			else if (num2 >= 0.0)
			{
				SeekTo(num2);
			}
			else if (num != 0 && num == PartAt(OnPlate(e.Location), scale, tune.Spotify))
			{
				Press(num);
			}
		}
	}

	protected override void OnMouseLeave(EventArgs e)
	{
		base.OnMouseLeave(e);
		if (hover != 0 && held == 0)
		{
			hover = 0;
			Redraw();
		}
	}

	protected override void OnMouseCaptureChanged(EventArgs e)
	{
		base.OnMouseCaptureChanged(e);
		if (Capture)
		{
			return;
		}
		if (grabbing)
		{
			LetGo();
		}
		if (held != 0 || !(scrub < 0.0))
		{
			held = 0;
			scrub = -1.0;
			if (Visible)
			{
				Redraw();
			}
		}
	}

	internal void Grab(Point at)
	{
		grabbing = true;
		grabbed = at;
		grabbedAt = Location;
		Capture = true;
	}

	internal void Haul(Point at)
	{
		if (grabbing)
		{
			MoveTo(new Point(grabbedAt.X + at.X - grabbed.X, grabbedAt.Y + at.Y - grabbed.Y));
		}
	}

	internal void LetGo()
	{
		bool flag = grabbing;
		grabbing = false;
		if (Capture)
		{
			Capture = false;
		}
		if (flag && Visible)
		{
			if (Dpi() != scale)
			{
				Redraw();
			}
			if (corner.HasValue && Moved != null)
			{
				Moved(corner.Value);
			}
		}
	}

	internal void MoveTo(Point topLeft)
	{
		Bitmap image = Image;
		if (image != null && Visible)
		{
			int glow = Glow;
			corner = new Point(topLeft.X + glow, topLeft.Y + image.Height - glow);
			Point point = Spot(image.Size);
			corner = new Point(point.X + glow, point.Y + image.Height - glow);
			if (Location != point)
			{
				Location = point;
			}
		}
	}

	internal void Press(int part)
	{
		Tune tune = this.tune;
		switch (part)
		{
		case 1:
			Raise("menu");
			return;
		case 7:
			Raise("close");
			return;
		}
		if (tune == null)
		{
			return;
		}
		double now = Tunes.Now;
		if (part == 2 && tune.CanBack)
		{
			Raise("previous");
		}
		else if (part == 4 && tune.CanSkip)
		{
			Raise("next");
		}
		else
		{
			switch (part)
			{
			case 3:
				tune.Position = tune.At(now);
				tune.ReadAt = now;
				tune.Playing = !tune.Playing;
				Raise(tune.Playing ? "play" : "pause");
				break;
			case 5:
				if (tune.Spotify)
				{
					liked = !liked;
					heartAt = now;
					Raise(liked ? "like" : "unlike");
					break;
				}
				return;
			default:
				return;
			}
		}
		if (Visible)
		{
			Redraw();
		}
	}

	internal void Hearted(string key, bool has, double asked)
	{
		if (tune != null && !(tune.Key != key) && !(asked < heartAt + 2.0) && has != liked)
		{
			liked = has;
			if (Visible)
			{
				Redraw();
			}
		}
	}

	internal void SeekTo(double fraction)
	{
		Tune tune = this.tune;
		if (tune == null || !tune.CanSeek)
		{
			if (Visible)
			{
				Redraw();
			}
			return;
		}
		tune.Position = Math.Max(0.0, Math.Min(1.0, fraction)) * tune.Length;
		tune.ReadAt = Tunes.Now;
		Raise("seek:" + tune.Position.ToString("0.###", CultureInfo.InvariantCulture));
		if (Visible)
		{
			Redraw();
		}
	}

	internal void Sent(string what, bool ok)
	{
		if (!ok && (!(what != "like") || !(what != "unlike")))
		{
			liked = what == "unlike";
			if (Visible)
			{
				Redraw();
			}
		}
	}

	private void Raise(string what)
	{
		sentAt = Tunes.Now;
		if (Command != null)
		{
			Command(what);
		}
	}

	private static RectangleF Around(float x, float y, float width, float height, float s)
	{
		return new RectangleF((x - width / 2f) * s, (y - height / 2f) * s, width * s, height * s);
	}

	internal static RectangleF Box(int part, float s)
	{
		return part switch
		{
			1 => Around(30f, 88f, 34f, 34f, s), 
			2 => Around(110f, 88f, 36f, 34f, s), 
			3 => Around(168f, 88f, 42f, 42f, s), 
			4 => Around(226f, 88f, 36f, 34f, s), 
			5 => Around(306f, 88f, 34f, 34f, s), 
			6 => new RectangleF(10f * s, 49f * s, 316f * s, 18f * s), 
			7 => Around(318f, 17f, 26f, 26f, s), 
			_ => RectangleF.Empty, 
		};
	}

	private static PointF Middle(int part, float s)
	{
		RectangleF rectangleF = Box(part, s);
		return new PointF(rectangleF.X + rectangleF.Width / 2f, rectangleF.Y + rectangleF.Height / 2f);
	}

	internal static int PartAt(PointF spot, float s, bool heart)
	{
		for (int i = 1; i <= 7; i++)
		{
			if ((i != 5 || heart) && Box(i, s).Contains(spot))
			{
				return i;
			}
		}
		return 0;
	}

	internal static string Clock(double seconds)
	{
		int num = (int)Math.Max(0.0, Math.Floor(seconds));
		if (num < 3600)
		{
			return num / 60 + ":" + (num % 60).ToString("00");
		}
		return num / 3600 + ":" + (num / 60 % 60).ToString("00") + ":" + (num % 60).ToString("00");
	}

	internal static float[] TextSpan(float s, int legsFrom, int legsTo)
	{
		float num = 18f * s;
		float num2 = 300f * s;
		if (legsTo > legsFrom)
		{
			float num3 = (float)legsFrom - 6f * s;
			float num4 = (float)legsTo + 6f * s;
			if (num4 > num && num3 < num2)
			{
				float num5 = num3 - num;
				float num6 = num2 - num4;
				if (Math.Max(num5, num6) >= 64f * s)
				{
					if (num6 >= num5)
					{
						num = num4;
					}
					else
					{
						num2 = num3;
					}
				}
			}
		}
		return new float[2] { num, num2 };
	}

	internal static Bitmap Render(float s, Tune tune, double now, int hover, bool liked, double scrub, int legsFrom, int legsTo, int glitch)
	{
		int num = (int)Math.Round(12f * s);
		int num2 = (int)Math.Round(336f * s);
		int num3 = (int)Math.Round(116f * s);
		Bitmap bitmap = new Bitmap(num2 + num * 2, num3 + num * 2, PixelFormat.Format32bppPArgb);
		using (Graphics graphics = Graphics.FromImage(bitmap))
		{
			graphics.SmoothingMode = SmoothingMode.AntiAlias;
			graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
			graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
			graphics.TranslateTransform(num, num);
			RectangleF box = new RectangleF(0f, 0f, num2, num3);
			PointF point = new PointF(-1f, 0f);
			PointF point2 = new PointF(num2 + 1, 0f);
			using (GraphicsPath graphicsPath = Look.Rounded(box, 16f * s))
			{
				using (LinearGradientBrush brush = new LinearGradientBrush(point, point2, Color.FromArgb(30, Look.Pink), Color.FromArgb(30, Look.Cyan)))
				{
					using Pen pen = new Pen(brush, 14f * s);
					graphics.DrawPath(pen, graphicsPath);
				}
				using (LinearGradientBrush brush2 = new LinearGradientBrush(point, point2, Color.FromArgb(70, Look.Pink), Color.FromArgb(70, Look.Cyan)))
				{
					using Pen pen2 = new Pen(brush2, 6f * s);
					graphics.DrawPath(pen2, graphicsPath);
				}
				using (Brush brush3 = new SolidBrush(Color.FromArgb(250, 5, 6, 9)))
				{
					graphics.FillPath(brush3, graphicsPath);
				}
				graphics.SetClip(graphicsPath);
				using (Pen pen3 = new Pen(Color.FromArgb(9, 255, 255, 255), Math.Max(1f, s)))
				{
					for (float num4 = 0f; num4 < (float)num3; num4 += 3f * s)
					{
						graphics.DrawLine(pen3, 0f, num4, num2, num4);
					}
				}
				graphics.ResetClip();
				using LinearGradientBrush brush4 = new LinearGradientBrush(point, point2, Look.Pink, Look.Cyan);
				using Pen pen4 = new Pen(brush4, 1.6f * s);
				graphics.DrawPath(pen4, graphicsPath);
			}
			using (Brush brush5 = new SolidBrush(Color.FromArgb(210, Look.Cyan)))
			{
				graphics.FillRectangle(brush5, (float)num2 * 0.64f, -3f * s, 28f * s, 1.6f * s);
			}
			using (Brush brush6 = new SolidBrush(Color.FromArgb(210, Look.Pink)))
			{
				graphics.FillRectangle(brush6, (float)num2 * 0.18f, (float)num3 + 1.4f * s, 18f * s, 1.6f * s);
			}
			string text = Tunes.AppName(tune.App);
			string text2 = ((Plain(tune.Title).Length > 0) ? Plain(tune.Title) : text);
			string text3;
			if (Plain(tune.Title).Length != 0)
			{
				text3 = ((Plain(tune.Artist).Length > 0) ? Plain(tune.Artist) : text);
			}
			else
			{
				text3 = "";
			}
			double num5 = ((scrub >= 0.0 && tune.Length > 0.0) ? (scrub * tune.Length) : tune.At(now));
			string text4 = ((tune.Length > 0.0) ? (Clock(num5) + " / " + Clock(tune.Length)) : "");
			float[] array = TextSpan(s, legsFrom, legsTo);
			bool flag = array[1] < 300f * s;
			using (Font font = Look.Mono(13.5f * s, FontStyle.Regular, GraphicsUnit.Pixel, text2))
			{
				using Font font2 = Look.Mono(11f * s, FontStyle.Regular, GraphicsUnit.Pixel, text3 + text4);
				string text5 = Fit(graphics, text2, font, array[1] - array[0]);
				using (Brush brush7 = new SolidBrush(Color.FromArgb(140, Look.Pink)))
				{
					Look.Draw(graphics, text5, font, brush7, array[0] - 1.1f * s, 11f * s);
				}
				using (Brush brush8 = new SolidBrush(Color.FromArgb(140, Look.Cyan)))
				{
					Look.Draw(graphics, text5, font, brush8, array[0] + 1.1f * s, 11f * s);
				}
				using (Brush brush9 = new SolidBrush(Look.Ink))
				{
					Look.Draw(graphics, text5, font, brush9, array[0], 11f * s);
				}
				float num6 = ((text4.Length > 0) ? graphics.MeasureString(text4, font2, PointF.Empty, StringFormat.GenericTypographic).Width : 0f);
				float num7 = (float)num2 - 18f * s - num6;
				bool flag2 = text4.Length > 0 && (legsTo <= legsFrom || !((float)legsTo + 4f * s > num7) || !((float)legsFrom - 4f * s < (float)num2 - 18f * s));
				float num8 = (flag ? array[1] : ((float)num2 - 18f * s));
				if (flag2)
				{
					num8 = Math.Min(num8, num7 - 12f * s);
				}
				using (Brush brush10 = new SolidBrush(Color.FromArgb(225, Look.Cyan)))
				{
					Look.Draw(graphics, Fit(graphics, text3, font2, num8 - array[0]), font2, brush10, array[0], 31f * s);
				}
				if (flag2)
				{
					using Brush brush11 = new SolidBrush(Look.Dim);
					graphics.DrawString(text4, font2, brush11, num7, 31f * s, StringFormat.GenericTypographic);
				}
			}
			float num9 = 18f * s;
			float num10 = (float)num2 - 18f * s;
			float num11 = 58f * s;
			using (Pen pen5 = new Pen(Color.FromArgb(58, 63, 74), 2.2f * s))
			{
				pen5.StartCap = LineCap.Round;
				pen5.EndCap = LineCap.Round;
				graphics.DrawLine(pen5, num9, num11, num10, num11);
			}
			if (tune.Length > 0.0)
			{
				float num12 = num9 + (float)(Math.Max(0.0, Math.Min(1.0, num5 / tune.Length)) * (double)(num10 - num9));
				if (num12 > num9 + 0.5f)
				{
					using LinearGradientBrush brush12 = new LinearGradientBrush(new PointF(num9 - 1f, 0f), new PointF(num10 + 1f, 0f), Look.Pink, Look.Cyan);
					using Pen pen6 = new Pen(brush12, 2.6f * s);
					pen6.StartCap = LineCap.Round;
					pen6.EndCap = LineCap.Round;
					graphics.DrawLine(pen6, num9, num11, num12, num11);
				}
				bool flag3 = hover == 6 || scrub >= 0.0;
				float num13 = (flag3 ? 6.5f : 5.5f) * s;
				using (Brush brush13 = new SolidBrush(Color.FromArgb(flag3 ? 90 : 60, flag3 ? Look.Cyan : Look.Pink)))
				{
					graphics.FillEllipse(brush13, num12 - num13 * 1.9f, num11 - num13 * 1.9f, num13 * 3.8f, num13 * 3.8f);
				}
				using Brush brush14 = new SolidBrush(Look.Ink);
				graphics.FillEllipse(brush14, num12 - num13, num11 - num13, num13 * 2f, num13 * 2f);
			}
			int[] array2 = new int[4] { 1, 2, 4, 5 };
			foreach (int num14 in array2)
			{
				if (hover == num14 && (num14 != 5 || tune.Spotify))
				{
					PointF pointF = Middle(num14, s);
					using Brush brush15 = new SolidBrush(Color.FromArgb(30, 255, 255, 255));
					graphics.FillEllipse(brush15, pointF.X - 15f * s, pointF.Y - 15f * s, 30f * s, 30f * s);
				}
			}
			DrawMenu(graphics, Middle(1, s), s, (hover == 1) ? Look.Cyan : Look.Ink);
			Graphics graphics2 = graphics;
			PointF c = Middle(2, s);
			float s2 = s;
			Color colour;
			if (tune.CanBack)
			{
				colour = ((hover == 2) ? Look.Cyan : Look.Ink);
			}
			else
			{
				colour = Look.Dim;
			}
			DrawSkip(graphics2, c, s2, colour, next: false);
			Graphics graphics3 = graphics;
			PointF c2 = Middle(4, s);
			float s3 = s;
			Color colour2;
			if (tune.CanSkip)
			{
				colour2 = ((hover == 4) ? Look.Cyan : Look.Ink);
			}
			else
			{
				colour2 = Look.Dim;
			}
			DrawSkip(graphics3, c2, s3, colour2, next: true);
			DrawPlay(graphics, Middle(3, s), s, hover == 3, tune.Playing);
			if (tune.Spotify)
			{
				DrawHeart(graphics, Middle(5, s), s, hover == 5, liked);
			}
			DrawClose(graphics, Middle(7, s), s, hover == 7);
			if (glitch != 0)
			{
				Tear(bitmap, graphics, num, num2, num3, s, glitch);
			}
		}
		return bitmap;
	}

	private static void Tear(Bitmap image, Graphics graphics, int glow, int w, int h, float s, int seed)
	{
		Random random = new Random(seed);
		graphics.ResetTransform();
		using Bitmap image2 = image.Clone(new Rectangle(0, 0, image.Width, image.Height), image.PixelFormat);
		using GraphicsPath clip = Look.Rounded(new RectangleF(glow, glow, w, h), 16f * s);
		using Brush brush = new SolidBrush(Color.Transparent);
		for (int i = 0; i < 3; i++)
		{
			int num = Math.Max(2, (int)Math.Round((float)(2 + random.Next(7)) * s));
			int num2 = glow + random.Next(Math.Max(1, h - num));
			int num3 = ((random.Next(2) != 0) ? 1 : (-1)) * Math.Max(1, (int)Math.Round((float)(2 + random.Next(5)) * s));
			Rectangle rectangle = new Rectangle(0, num2, image.Width, num);
			graphics.SetClip(rectangle);
			graphics.CompositingMode = CompositingMode.SourceCopy;
			graphics.FillRectangle(brush, rectangle);
			graphics.DrawImage(image2, new Rectangle(num3, 0, image.Width, image.Height));
			graphics.CompositingMode = CompositingMode.SourceOver;
			graphics.SetClip(clip);
			graphics.IntersectClip(rectangle);
			using (Brush brush2 = new SolidBrush(Color.FromArgb(56, (i % 2 == 0) ? Look.Pink : Look.Cyan)))
			{
				graphics.FillRectangle(brush2, rectangle);
			}
			graphics.ResetClip();
		}
	}

	internal static string Plain(string text)
	{
		StringBuilder stringBuilder = new StringBuilder();
		string text2 = text ?? "";
		foreach (char c in text2)
		{
			if (!char.IsSurrogate(c) && c != '\ufe0e' && c != '\ufe0f' && c != '\u200d' && c != '\u20e3')
			{
				stringBuilder.Append(c);
			}
		}
		return Regex.Replace(stringBuilder.ToString(), "\\s{2,}", " ").Trim();
	}

	private static string Fit(Graphics graphics, string text, Font font, float width)
	{
		if (string.IsNullOrEmpty(text) || width <= 0f)
		{
			return "";
		}
		if (Measure(graphics, text, font) <= width)
		{
			return text;
		}
		int num = 0;
		int num2 = text.Length;
		while (num < num2)
		{
			int num3 = (num + num2 + 1) / 2;
			if (Measure(graphics, Start(text, num3) + "...", font) <= width)
			{
				num = num3;
			}
			else
			{
				num2 = num3 - 1;
			}
		}
		if (num != 0)
		{
			return Start(text, num).TrimEnd() + "...";
		}
		return "";
	}

	private static string Start(string text, int length)
	{
		if (length > 0 && length < text.Length && char.IsHighSurrogate(text[length - 1]))
		{
			length--;
		}
		return text.Substring(0, length);
	}

	private static float Measure(Graphics graphics, string text, Font font)
	{
		return graphics.MeasureString(text, font, PointF.Empty, StringFormat.GenericTypographic).Width;
	}

	private static void DrawMenu(Graphics graphics, PointF c, float s, Color colour)
	{
		using Pen pen = new Pen(colour, 2.2f * s);
		pen.StartCap = LineCap.Round;
		pen.EndCap = LineCap.Round;
		for (int i = -1; i <= 1; i++)
		{
			graphics.DrawLine(pen, c.X - 8f * s, c.Y + (float)i * 5.5f * s, c.X + 8f * s, c.Y + (float)i * 5.5f * s);
		}
	}

	private static void DrawSkip(Graphics graphics, PointF c, float s, Color colour, bool next)
	{
		float num = (next ? 1 : (-1));
		using Brush brush = new SolidBrush(colour);
		graphics.FillPolygon(brush, new PointF[3]
		{
			new PointF(c.X - num * 9f * s, c.Y - 6.5f * s),
			new PointF(c.X - num * 9f * s, c.Y + 6.5f * s),
			new PointF(c.X - num * 1.5f * s, c.Y)
		});
		graphics.FillPolygon(brush, new PointF[3]
		{
			new PointF(c.X - num * 1.5f * s, c.Y - 6.5f * s),
			new PointF(c.X - num * 1.5f * s, c.Y + 6.5f * s),
			new PointF(c.X + num * 6f * s, c.Y)
		});
		graphics.FillRectangle(brush, next ? (c.X + 6f * s) : (c.X - 8.4f * s), c.Y - 6.5f * s, 2.4f * s, 13f * s);
	}

	private static void DrawPlay(Graphics graphics, PointF c, float s, bool lit, bool playing)
	{
		float num = 17f * s;
		if (lit)
		{
			using Brush brush = new SolidBrush(Color.FromArgb(70, Look.Cyan));
			graphics.FillEllipse(brush, c.X - num - 5f * s, c.Y - num - 5f * s, (num + 5f * s) * 2f, (num + 5f * s) * 2f);
		}
		using (Brush brush2 = new SolidBrush(Color.FromArgb(170, Look.Pink)))
		{
			graphics.FillEllipse(brush2, c.X - num - 1.6f * s, c.Y - num, num * 2f, num * 2f);
		}
		using (Brush brush3 = new SolidBrush(Color.FromArgb(170, Look.Cyan)))
		{
			graphics.FillEllipse(brush3, c.X - num + 1.6f * s, c.Y - num, num * 2f, num * 2f);
		}
		using (Brush brush4 = new SolidBrush(Look.Ink))
		{
			graphics.FillEllipse(brush4, c.X - num, c.Y - num, num * 2f, num * 2f);
		}
		using Brush brush5 = new SolidBrush(Color.FromArgb(10, 11, 15));
		if (!playing)
		{
			graphics.FillPolygon(brush5, new PointF[3]
			{
				new PointF(c.X - 4.5f * s, c.Y - 7.5f * s),
				new PointF(c.X - 4.5f * s, c.Y + 7.5f * s),
				new PointF(c.X + 8f * s, c.Y)
			});
			return;
		}
		float[] array = new float[2]
		{
			c.X - 5.6f * s,
			c.X + 2f * s
		};
		foreach (float num2 in array)
		{
			using GraphicsPath path = Look.Rounded(new RectangleF(num2, c.Y - 6.5f * s, 3.6f * s, 13f * s), 1f * s);
			graphics.FillPath(brush5, path);
		}
	}

	private static void DrawHeart(Graphics graphics, PointF c, float s, bool lit, bool liked)
	{
		float num = 8.5f * s;
		float num2 = c.X;
		float num3 = c.Y + 0.5f * s;
		using GraphicsPath graphicsPath = new GraphicsPath();
		graphicsPath.AddBezier(num2, num3 + num * 0.95f, num2 - num * 0.35f, num3 + num * 0.6f, num2 - num * 1.1f, num3 + num * 0.1f, num2 - num * 1.05f, num3 - num * 0.35f);
		graphicsPath.AddBezier(num2 - num * 1.05f, num3 - num * 0.35f, num2 - num * 1f, num3 - num * 1.05f, num2 - num * 0.1f, num3 - num * 1.1f, num2, num3 - num * 0.45f);
		graphicsPath.AddBezier(num2, num3 - num * 0.45f, num2 + num * 0.1f, num3 - num * 1.1f, num2 + num * 1f, num3 - num * 1.05f, num2 + num * 1.05f, num3 - num * 0.35f);
		graphicsPath.AddBezier(num2 + num * 1.05f, num3 - num * 0.35f, num2 + num * 1.1f, num3 + num * 0.1f, num2 + num * 0.35f, num3 + num * 0.6f, num2, num3 + num * 0.95f);
		graphicsPath.CloseFigure();
		if (liked)
		{
			using Brush brush = new SolidBrush(Look.Pink);
			graphics.FillPath(brush, graphicsPath);
		}
		Color color;
		if (liked)
		{
			color = Look.Pink;
		}
		else
		{
			color = (lit ? Look.Cyan : Look.Ink);
		}
		using Pen pen = new Pen(color, 2f * s);
		pen.LineJoin = LineJoin.Round;
		graphics.DrawPath(pen, graphicsPath);
	}

	private static void DrawClose(Graphics graphics, PointF c, float s, bool lit)
	{
		float num = 4.5f * s;
		if (lit)
		{
			using Brush brush = new SolidBrush(Color.FromArgb(60, Look.Red));
			graphics.FillEllipse(brush, c.X - 10f * s, c.Y - 10f * s, 20f * s, 20f * s);
		}
		using Pen pen = new Pen(lit ? Look.Red : Look.Dim, 1.8f * s);
		pen.StartCap = LineCap.Round;
		pen.EndCap = LineCap.Round;
		graphics.DrawLine(pen, c.X - num, c.Y - num, c.X + num, c.Y + num);
		graphics.DrawLine(pen, c.X - num, c.Y + num, c.X + num, c.Y - num);
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
