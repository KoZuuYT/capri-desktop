using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Windows.Forms;

namespace CapriDesktop;

// mod: little layers for feeding her files and for her night sleep. Nothing here touches the files themselves:
// only their icon is borrowed for the animation.
internal static class Snack
{
	// a layer the mouse goes straight through
	internal sealed class Ghost : Overlay
	{
		internal Ghost(string name)
			: base(name)
		{
		}

		protected override CreateParams CreateParams
		{
			get
			{
				CreateParams createParams = base.CreateParams;
				createParams.ExStyle |= 0x20;
				return createParams;
			}
		}
	}

	private static readonly Random random = new Random();

	private static double Smooth(double k)
	{
		k = Math.Clamp(k, 0.0, 1.0);
		return k * k * (3.0 - 2.0 * k);
	}

	// runs step every frame with the seconds elapsed until it returns false, then cleans the layer up
	private static void Run(Ghost layer, int interval, Func<double, bool> step)
	{
		System.Windows.Forms.Timer ticks = new System.Windows.Forms.Timer { Interval = interval };
		Stopwatch watch = Stopwatch.StartNew();
		ticks.Tick += (object sender, EventArgs e) =>
		{
			bool more;
			try
			{
				more = !layer.IsDisposed && step(watch.Elapsed.TotalSeconds);
			}
			catch (Exception)
			{
				more = false;
			}
			if (!more)
			{
				ticks.Stop();
				ticks.Dispose();
				if (!layer.IsDisposed)
				{
					layer.Close();
					layer.Dispose();
				}
			}
		};
		ticks.Start();
	}

	// the file's own icon, blown up into chunky pixels (a drawn folder when there is no icon)
	internal static Bitmap Picture(string path)
	{
		Bitmap small = null;
		try
		{
			if (File.Exists(path))
			{
				using Icon icon = Icon.ExtractAssociatedIcon(path);
				if (icon != null)
				{
					small = icon.ToBitmap();
				}
			}
		}
		catch (Exception)
		{
			small = null;
		}
		if (small == null)
		{
			small = new Bitmap(32, 32, PixelFormat.Format32bppArgb);
			using Graphics g = Graphics.FromImage(small);
			using Brush tab = new SolidBrush(Color.FromArgb(230, 170, 40));
			using Brush body = new SolidBrush(Color.FromArgb(255, 200, 70));
			g.FillRectangle(tab, 3, 6, 12, 5);
			g.FillRectangle(body, 3, 9, 26, 18);
		}
		Bitmap big = new Bitmap(64, 64, PixelFormat.Format32bppPArgb);
		using (Graphics g2 = Graphics.FromImage(big))
		{
			g2.InterpolationMode = InterpolationMode.NearestNeighbor;
			g2.PixelOffsetMode = PixelOffsetMode.Half;
			g2.DrawImage(small, 0, 0, 64, 64);
		}
		small.Dispose();
		return big;
	}

	// the icon flies in an arc from where it was dropped into her mouth, spinning and shrinking, then bursts
	// into crumbs with a "NHAC!"; chomped runs at the bite
	internal static void Eat(Bitmap picture, Point from, Point mouth, double delay, Action chomped)
	{
		Ghost layer = new Ghost("Capri snack");
		int side = 200;
		Color[] crumbs = Crumbs(picture);
		bool bitten = false;
		Font font = Neon.Display(26f, GraphicsUnit.Pixel);
		Run(layer, 16, (double t) =>
		{
			t -= delay;
			if (t < 0.0)
			{
				return true;
			}
			if (t > 1.15)
			{
				picture.Dispose();
				font.Dispose();
				return false;
			}
			double fly = Math.Min(1.0, t / 0.55);
			double eased = fly * fly;
			float mx = (from.X + mouth.X) / 2f;
			float my = Math.Min(from.Y, mouth.Y) - 140f;
			float u = (float)eased;
			float x = (1 - u) * (1 - u) * from.X + 2 * (1 - u) * u * mx + u * u * mouth.X;
			float y = (1 - u) * (1 - u) * from.Y + 2 * (1 - u) * u * my + u * u * mouth.Y;
			if (fly >= 1.0 && !bitten)
			{
				bitten = true;
				chomped?.Invoke();
			}
			Bitmap frame = new Bitmap(side, side, PixelFormat.Format32bppPArgb);
			using (Graphics g = Graphics.FromImage(frame))
			{
				g.SmoothingMode = SmoothingMode.AntiAlias;
				g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
				float c = side / 2f;
				if (fly < 1.0)
				{
					float scale = (float)(1.0 - 0.65 * eased);
					// a neon trail behind it
					using (Pen trail = new Pen(Color.FromArgb(90, Toast.Mint), 10f * scale))
					{
						trail.StartCap = LineCap.Round;
						trail.EndCap = LineCap.Round;
						g.DrawLine(trail, c, c, c - (x - from.X) * 0.08f, c - (y - from.Y) * 0.08f + 6f);
					}
					GraphicsState state = g.Save();
					g.TranslateTransform(c, c);
					g.RotateTransform((float)(fly * 540.0));
					g.ScaleTransform(scale, scale);
					g.InterpolationMode = InterpolationMode.NearestNeighbor;
					g.DrawImage(picture, -32, -32, 64, 64);
					g.Restore(state);
				}
				else
				{
					double k = (t - 0.55) / 0.6;
					Random spray = new Random(picture.GetHashCode());
					for (int i = 0; i < 26; i++)
					{
						double angle = -Math.PI * spray.NextDouble();
						double speed = 40.0 + spray.NextDouble() * 70.0;
						float px = c + (float)(Math.Cos(angle) * speed * k);
						float py = c + (float)(Math.Sin(angle) * speed * k + 160.0 * k * k);
						float cell = 4f + spray.Next(3);
						using Brush crumb = new SolidBrush(Color.FromArgb((int)(255.0 * (1.0 - k)), crumbs[spray.Next(crumbs.Length)]));
						g.FillRectangle(crumb, (float)Math.Round(px / 3.0) * 3f, (float)Math.Round(py / 3.0) * 3f, cell, cell);
					}
					double pop = Smooth(k / 0.25);
					float lift = (float)(k * 40.0);
					string word = "NHAC!";
					GraphicsState state2 = g.Save();
					g.TranslateTransform(c, c - 34f - lift);
					float grow = (float)(0.6 + 0.5 * pop - 0.1 * Smooth((k - 0.3) / 0.3));
					g.ScaleTransform(grow, grow);
					SizeF size = g.MeasureString(word, font);
					int alpha = (int)(255.0 * (1.0 - Smooth((k - 0.6) / 0.4)));
					using (Brush shadow = new SolidBrush(Color.FromArgb(alpha, 12, 10, 16)))
					{
						g.DrawString(word, font, shadow, -size.Width / 2f + 2f, -size.Height / 2f + 2f);
					}
					using (Brush ink = new SolidBrush(Color.FromArgb(alpha, Toast.Mint)))
					{
						g.DrawString(word, font, ink, -size.Width / 2f, -size.Height / 2f);
					}
					g.Restore(state2);
				}
			}
			layer.Present(frame, new Point((int)x - side / 2, (int)y - side / 2));
			return true;
		});
	}

	// the main colours in the icon, for the crumbs
	private static Color[] Crumbs(Bitmap picture)
	{
		Color[] colours = new Color[6];
		int found = 0;
		for (int attempt = 0; attempt < 200 && found < colours.Length; attempt++)
		{
			Color pixel = picture.GetPixel(random.Next(picture.Width), random.Next(picture.Height));
			if (pixel.A > 160)
			{
				colours[found++] = Color.FromArgb(255, pixel);
			}
		}
		for (int i = found; i < colours.Length; i++)
		{
			colours[i] = (i % 2 == 0) ? Toast.Mint : Color.White;
		}
		return colours;
	}

	// mod: a wide invisible net around her that catches files dragged anywhere near her, not only right over her
	// pixels. It only comes up while some window holds the mouse with a button down close to her (a drag in
	// progress), so it never takes a normal click. While files hover it shows them being sucked into her mouth.
	internal sealed class Catcher : Overlay
	{
		private const int Reach = 120;

		private readonly System.Windows.Forms.Timer ticks = new System.Windows.Forms.Timer { Interval = 30 };

		private readonly Stopwatch watch = Stopwatch.StartNew();

		private readonly Font font = Neon.Display(22f, GraphicsUnit.Pixel);

		private readonly Func<bool> allowed;

		private readonly Func<Rectangle> body;

		private readonly Func<Point> mouth;

		// when files started hovering (-1 while none), when they went away and when she swallowed them
		private double since = -1.0;

		private double leftAt = -1.0;

		private double ateAt = -1.0;

		private double heldAt = -1.0;

		private double nextSniff;

		private double last;

		private bool sulked = true;

		private float fade;

		internal bool Hungry => since >= 0.0;

		internal Catcher(Func<bool> allowed, Func<Rectangle> body, Func<Point> mouth)
			: base("Capri catcher")
		{
			this.allowed = allowed;
			this.body = body;
			this.mouth = mouth;
			AllowDrop = true;
			ticks.Tick += (object sender, EventArgs e) =>
			{
				try
				{
					Step();
				}
				catch (Exception)
				{
				}
			};
			ticks.Start();
		}

		// files came over her; false when they only hopped between her and the net, so the hello isn't repeated
		internal void Begin()
		{
			double t = watch.Elapsed.TotalSeconds;
			bool fresh = sulked && !Hungry;
			if (!Hungry)
			{
				since = t;
			}
			leftAt = -1.0;
			sulked = false;
			if (fresh)
			{
				nextSniff = t + 0.3;
				Sfx.Play(Cue.Hungry);
			}
		}

		internal void End(bool ate)
		{
			double t = watch.Elapsed.TotalSeconds;
			since = -1.0;
			if (ate)
			{
				ateAt = t;
				sulked = true;
			}
			else
			{
				leftAt = t;
			}
		}

		protected override void WndProc(ref Message message)
		{
			// WM_NCHITTEST: the whole net counts, even where it's see-through
			if (message.Msg == 132)
			{
				message.Result = new IntPtr(1);
				return;
			}
			// WM_MOUSEACTIVATE: never take focus
			if (message.Msg == 33)
			{
				message.Result = new IntPtr(3);
				return;
			}
			base.WndProc(ref message);
		}

		private static bool ButtonDown()
		{
			return (Native.GetAsyncKeyState(1) & 0x8000) != 0 || (Native.GetAsyncKeyState(2) & 0x8000) != 0;
		}

		private void Step()
		{
			double t = watch.Elapsed.TotalSeconds;
			double dt = Math.Clamp(t - last, 0.0, 0.1);
			last = t;
			if (leftAt >= 0.0 && !Hungry && !sulked && t - leftAt > 0.18)
			{
				// they took the files away again
				sulked = true;
				Sfx.Play(Cue.Dismiss);
			}
			bool held = ButtonDown();
			if (held)
			{
				heldAt = t;
			}
			else if (Hungry && t - heldAt > 1.0)
			{
				// the drag ended without telling us
				End(ate: false);
			}
			Rectangle zone = body();
			zone.Inflate(Reach, Reach);
			Point cursor = System.Windows.Forms.Cursor.Position;
			bool busy = Hungry || fade > 0.0f || (ateAt >= 0.0 && t - ateAt < 0.45);
			bool want = allowed() && (busy || (Visible && t - heldAt < 0.6) || (held && zone.Contains(cursor) && Native.MouseCaptured()));
			if (!want)
			{
				since = -1.0;
				fade = 0.0f;
				if (Visible)
				{
					Hide();
				}
				ticks.Interval = 60;
				return;
			}
			ticks.Interval = 30;
			fade = Math.Clamp(fade + (float)(dt / 0.18) * (Hungry ? 1f : -1f), 0f, 1f);
			if (!busy && Visible && Bounds == zone)
			{
				return;
			}
			Point m = mouth();
			m.Offset(-zone.Left, -zone.Top);
			Point p = new Point(cursor.X - zone.Left, cursor.Y - zone.Top);
			double reach = Reach + Math.Max(zone.Width, zone.Height) / 4.0;
			double close = 1.0 - Math.Clamp(Math.Sqrt((p.X - m.X) * (p.X - m.X) + (p.Y - m.Y) * (p.Y - m.Y)) / reach, 0.0, 1.0);
			if (Hungry && t >= nextSniff)
			{
				Sfx.Sniff(close);
				nextSniff = t + 0.34 - 0.24 * close;
			}
			Bitmap frame = new Bitmap(Math.Max(1, zone.Width), Math.Max(1, zone.Height), PixelFormat.Format32bppPArgb);
			using (Graphics g = Graphics.FromImage(frame))
			{
				// barely there everywhere, so the whole net catches the drop
				g.Clear(Color.FromArgb(1, 0, 0, 0));
				g.SmoothingMode = SmoothingMode.AntiAlias;
				g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
				if (fade > 0.0f)
				{
					Draw(g, zone.Size, m, p, t, close);
				}
				if (ateAt >= 0.0 && t - ateAt < 0.45)
				{
					Gulp(g, m, (t - ateAt) / 0.45);
				}
			}
			Put(frame, zone.Location);
		}

		private void Put(Bitmap frame, Point at)
		{
			bool showing = Visible;
			Present(frame, at);
			if (!showing)
			{
				// above her, so the drop lands here
				Native.SetWindowPos(Handle, new IntPtr(-1), 0, 0, 0, 0, 19);
			}
		}

		private void Draw(Graphics g, Size size, Point m, Point p, double t, double close)
		{
			float f = fade;
			double into = since >= 0.0 ? t - since : 1.0;
			// the net itself: a dashed neon oval that marches round, tighter and brighter as the file gets close
			using (Pen net = new Pen(Color.FromArgb((int)((60 + 110 * close) * f), Toast.Mint), 2.5f))
			{
				net.DashPattern = new float[2] { 3f, 3f };
				net.DashOffset = (float)(t * (6.0 + 14.0 * close));
				float inset = 8f + (float)(18.0 * close);
				g.DrawEllipse(net, inset, inset, size.Width - inset * 2f, size.Height - inset * 2f);
			}
			// a hello burst when the files first come over
			if (into < 0.35)
			{
				double k = into / 0.35;
				float r = (float)(10.0 + 110.0 * Smooth(k));
				using Pen burst = new Pen(Color.FromArgb((int)(230.0 * (1.0 - k) * f), Color.White), 4f);
				g.DrawEllipse(burst, m.X - r, m.Y - r, r * 2f, r * 2f);
			}
			// a dotted beam from the file to her mouth, its dots flowing in
			double speed = 1.0 + 3.0 * close;
			for (int i = 0; i < 14; i++)
			{
				double k = (i / 14.0 + t * speed) % 1.0;
				float x = (float)(p.X + (m.X - p.X) * k);
				float y = (float)(p.Y + (m.Y - p.Y) * k - Math.Sin(k * Math.PI) * 18.0);
				float cell = (float)(3.0 + 3.0 * k + 2.0 * close);
				using Brush dot = new SolidBrush(Color.FromArgb((int)(220.0 * Math.Sin(k * Math.PI) * f), (i % 3 == 0) ? Color.White : Toast.Mint));
				g.FillRectangle(dot, (float)Math.Round(x / 2.0) * 2f - cell / 2f, (float)Math.Round(y / 2.0) * 2f - cell / 2f, cell, cell);
			}
			// pixel crumbs spiralling into her mouth, faster the hungrier she gets
			for (int i = 0; i < 18; i++)
			{
				double k = (t * (0.7 + 1.8 * close) + i / 18.0) % 1.0;
				double angle = i * 2.399 + k * Math.PI * 3.0;
				double r = (70.0 + 40.0 * (1.0 - close)) * (1.0 - k);
				float x = m.X + (float)(Math.Cos(angle) * r);
				float y = m.Y + (float)(Math.Sin(angle) * r);
				float cell = (float)(2.0 + 3.0 * (1.0 - k));
				using Brush crumb = new SolidBrush(Color.FromArgb((int)(255.0 * Math.Min(1.0, k * 4.0) * (1.0 - k) * f), (i % 2 == 0) ? Toast.Mint : Color.White));
				g.FillRectangle(crumb, x - cell / 2f, y - cell / 2f, cell, cell);
			}
			// rings closing in on her mouth, like she's breathing in
			for (int i = 0; i < 3; i++)
			{
				double wave = (t * (1.1 + 2.2 * close) + i / 3.0) % 1.0;
				float r = (float)(12.0 + (1.0 - wave) * (40.0 + 20.0 * close));
				using Pen ring = new Pen(Color.FromArgb((int)(210.0 * wave * f), Toast.Mint), 2f + 2f * (float)close);
				g.DrawEllipse(ring, m.X - r, m.Y - r, r * 2f, r * 2f);
			}
			// what she says: curious far away, starving up close (and shaking with it)
			string word = close > 0.6 ? "NHAM!" : (close > 0.3 ? "nhac nhac" : "nhac?");
			float pop = (float)(0.5 + 0.5 * Smooth(into / 0.2));
			float scale = pop * (float)(1.0 + 0.35 * close + 0.06 * Math.Sin(t * (8.0 + 10.0 * close)));
			float shake = close > 0.6 ? (float)(Math.Sin(t * 70.0) * 2.5) : 0f;
			GraphicsState state = g.Save();
			g.TranslateTransform(m.X + shake, m.Y - 46f - (float)(Math.Sin(t * 6.0) * 3.0));
			g.ScaleTransform(scale, scale);
			SizeF measured = g.MeasureString(word, font);
			using (Brush shadow = new SolidBrush(Color.FromArgb((int)(220f * f), 12, 10, 16)))
			{
				g.DrawString(word, font, shadow, -measured.Width / 2f + 2f, -measured.Height / 2f + 2f);
			}
			using (Brush ink = new SolidBrush(Color.FromArgb((int)(255f * f), close > 0.6 ? Color.White : Toast.Mint)))
			{
				g.DrawString(word, font, ink, -measured.Width / 2f, -measured.Height / 2f);
			}
			g.Restore(state);
		}

		// the moment she swallows: a bright flash that snaps shut on her mouth
		private static void Gulp(Graphics g, Point m, double k)
		{
			float r = (float)(60.0 * (1.0 - Smooth(k)) + 6.0);
			using (Brush flash = new SolidBrush(Color.FromArgb((int)(150.0 * (1.0 - k)), Toast.Mint)))
			{
				g.FillEllipse(flash, m.X - r, m.Y - r, r * 2f, r * 2f);
			}
			using Pen edge = new Pen(Color.FromArgb((int)(255.0 * (1.0 - k)), Color.White), 3f);
			g.DrawEllipse(edge, m.X - r, m.Y - r, r * 2f, r * 2f);
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				ticks.Stop();
				ticks.Dispose();
				font.Dispose();
			}
			base.Dispose(disposing);
		}
	}

	// pixel "Z"s that float up from her head while she sleeps at night
	internal sealed class Snooze : IDisposable
	{
		private readonly Ghost layer = new Ghost("Capri snoring");

		private readonly Stopwatch watch = Stopwatch.StartNew();

		private readonly System.Windows.Forms.Timer ticks = new System.Windows.Forms.Timer { Interval = 40 };

		private readonly Func<Point?> head;

		// a 5x5 pixel Z
		private static readonly string[] Letter = { "#####", "...#.", "..#..", ".#...", "#####" };

		internal Snooze(Func<Point?> head)
		{
			this.head = head;
			ticks.Tick += (object sender, EventArgs e) => Step();
			ticks.Start();
		}

		private void Step()
		{
			Point? at = head();
			if (!at.HasValue)
			{
				if (layer.Visible)
				{
					layer.Hide();
				}
				return;
			}
			double t = watch.Elapsed.TotalSeconds;
			int w = 120;
			int h = 140;
			Bitmap frame = new Bitmap(w, h, PixelFormat.Format32bppPArgb);
			using (Graphics g = Graphics.FromImage(frame))
			{
				for (int i = 0; i < 3; i++)
				{
					double k = (t / 2.4 + i / 3.0) % 1.0;
					float cell = (float)(2.0 + 2.5 * k);
					float x = 20f + (float)(k * 50.0) + (float)(Math.Sin((t + i) * 2.5) * 8.0);
					float y = h - 20f - (float)(k * (h - 40));
					int alpha = (int)(255.0 * Math.Min(1.0, k / 0.15) * (1.0 - Smooth((k - 0.7) / 0.3)));
					using Brush glow = new SolidBrush(Color.FromArgb(alpha / 4, Toast.Moon));
					using Brush ink = new SolidBrush(Color.FromArgb(alpha, Toast.Moon));
					for (int row = 0; row < 5; row++)
					{
						for (int col = 0; col < 5; col++)
						{
							if (Letter[row][col] == '#')
							{
								g.FillRectangle(glow, x + col * cell - 1f, y + row * cell - 1f, cell + 2f, cell + 2f);
								g.FillRectangle(ink, x + col * cell, y + row * cell, cell - 0.5f, cell - 0.5f);
							}
						}
					}
				}
			}
			layer.Present(frame, new Point(at.Value.X - 10, at.Value.Y - h + 20));
		}

		public void Dispose()
		{
			ticks.Stop();
			ticks.Dispose();
			if (!layer.IsDisposed)
			{
				layer.Close();
				layer.Dispose();
			}
		}
	}
}
