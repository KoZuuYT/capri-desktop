using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Windows.Forms;

namespace CapriDesktop;

// mod: the Mainframe boots like an old terminal each time it opens: the CRT switches on as a bright line, a K.mod
// BIOS types its checks with a blip per line and a progress bar fills, then the screen breaks into pixels that
// fall away and reveal the real panel underneath. Clicks go straight through, so it never gets in the way.
internal sealed class BootScreen : Overlay
{
	private const double Length = 1.75;

	private static BootScreen running;

	private readonly System.Windows.Forms.Timer ticks = new System.Windows.Forms.Timer { Interval = 16 };

	private readonly Stopwatch watch = Stopwatch.StartNew();

	private readonly Form over;

	private readonly string[] lines;

	private readonly float s;

	private readonly int seed = new Random().Next();

	private readonly Font big;

	private readonly Font mono;

	private readonly Font small;

	protected override CreateParams CreateParams
	{
		get
		{
			CreateParams createParams = base.CreateParams;
			createParams.ExStyle |= 0x20;
			return createParams;
		}
	}

	private BootScreen(Form over, string version, string mood, int charm)
		: base("Capri mainframe boot")
	{
		this.over = over;
		s = Math.Max(0.75f, over.DeviceDpi / 96f);
		lines = new string[]
		{
			"K.MOD BIOS v" + version + "  //  (c) choom industries",
			"> núcleo neural ................ OK",
			"> olhos e visor ................ OK",
			"> medidor de carinho ...... " + charm + "%  OK",
			"> humor do dia ......... " + mood.ToUpperInvariant() + "  OK",
			"> conectando ao mainframe ...... OK"
		};
		big = Neon.Display(40f * s, GraphicsUnit.Pixel);
		mono = Look.Mono(12f * s, FontStyle.Regular, GraphicsUnit.Pixel);
		small = Look.Mono(10f * s, FontStyle.Bold, GraphicsUnit.Pixel);
		ticks.Tick += (object sender, EventArgs e) => Step();
	}

	internal static void Play(Form over, string version, string mood, int charm)
	{
		if (running != null || over.WindowState == FormWindowState.Minimized || over.Width < 50 || over.Height < 50)
		{
			return;
		}
		running = new BootScreen(over, version, mood, charm);
		// one premixed track: the hum, then a blip as each line lands, then the ready chime
		List<(Cue Cue, int Ms)> hits = new List<(Cue Cue, int Ms)> { (Cue.BootUp, 0) };
		for (int i = 0; i < running.lines.Length; i++)
		{
			hits.Add((Cue.Beep, (int)(1000.0 * running.LineAt(i))));
		}
		hits.Add((Cue.Notify, 1250));
		Sfx.Play(hits);
		running.Step();
		running.ticks.Start();
	}

	private double LineAt(int i)
	{
		return 0.22 + i * 0.13;
	}

	private void Step()
	{
		double t = watch.Elapsed.TotalSeconds;
		if (t >= Length || over.IsDisposed || !over.Visible || over.WindowState == FormWindowState.Minimized)
		{
			ticks.Stop();
			running = null;
			Close();
			Dispose();
			return;
		}
		Rectangle bounds = over.Bounds;
		Present(Frame(bounds.Size, t), bounds.Location);
	}

	private static double Smooth(double k)
	{
		k = Math.Clamp(k, 0.0, 1.0);
		return k * k * (3.0 - 2.0 * k);
	}

	private Bitmap Frame(Size size, double t)
	{
		Bitmap screen = new Bitmap(size.Width, size.Height, PixelFormat.Format32bppPArgb);
		using (Graphics g = Graphics.FromImage(screen))
		{
			g.SmoothingMode = SmoothingMode.AntiAlias;
			g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
			if (t < 0.18)
			{
				// the tube warming up: a line that widens, then opens into the screen
				g.Clear(Color.FromArgb(255, 4, 4, 8));
				double open = Smooth(t / 0.1);
				double tall = Smooth((t - 0.09) / 0.09);
				float w = (float)(size.Width * open);
				float h = Math.Max(2f * s, (float)(size.Height * tall));
				using Brush glow = new SolidBrush(Color.FromArgb(90, Neon.Rose));
				g.FillRectangle(glow, (size.Width - w) / 2f, (size.Height - h) / 2f - 6f * s, w, h + 12f * s);
				using Brush core = new SolidBrush(Color.FromArgb((int)(255.0 * (1.0 - tall * 0.7)), Color.White));
				g.FillRectangle(core, (size.Width - w) / 2f, (size.Height - h) / 2f, w, h);
				return screen;
			}
			Terminal(g, size, t);
		}
		if (t < 1.25)
		{
			return screen;
		}
		// the screen breaks into blocks that drop away, bottom-up, with a bit of random order
		double k = (t - 1.25) / (Length - 1.25);
		Bitmap broken = new Bitmap(size.Width, size.Height, PixelFormat.Format32bppPArgb);
		using (Graphics g2 = Graphics.FromImage(broken))
		{
			int block = (int)(24f * s);
			Random order = new Random(seed);
			for (int y = 0; y < size.Height; y += block)
			{
				for (int x = 0; x < size.Width; x += block)
				{
					double start = 0.55 * (1.0 - (double)y / size.Height) + order.NextDouble() * 0.35;
					double fall = Smooth((k - start) / 0.25);
					if (fall >= 1.0)
					{
						continue;
					}
					int side = (int)(block * (1.0 - fall * 0.6));
					int dy = (int)(fall * fall * 140.0 * s);
					Rectangle from = new Rectangle(x, y, Math.Min(block, size.Width - x), Math.Min(block, size.Height - y));
					Rectangle to = new Rectangle(x + (block - side) / 2, y + dy + (block - side) / 2, from.Width * side / block, from.Height * side / block);
					using ImageAttributes fade = new ImageAttributes();
					fade.SetColorMatrix(new ColorMatrix { Matrix33 = (float)(1.0 - fall) });
					g2.DrawImage(screen, to, from.X, from.Y, from.Width, from.Height, GraphicsUnit.Pixel, fade);
				}
			}
		}
		screen.Dispose();
		return broken;
	}

	private void Terminal(Graphics g, Size size, double t)
	{
		Neon.Backdrop(g, size, 220f * s);
		Random flicker = new Random(seed + (int)(t * 30.0));
		// grid floor and scanlines
		using (Pen grid = new Pen(Color.FromArgb(18, Neon.Rose), 1f))
		{
			for (float x = 0f; x < size.Width; x += 32f * s)
			{
				g.DrawLine(grid, x, 0f, x, size.Height);
			}
			for (float y = 0f; y < size.Height; y += 32f * s)
			{
				g.DrawLine(grid, 0f, y, size.Width, y);
			}
		}
		float left = 34f * s;
		float top = Math.Max(30f * s, size.Height * 0.16f);
		// logo, with a colour split that settles
		double settle = 1.0 - Smooth((t - 0.2) / 0.5);
		float split = (float)(6.0 * s * settle) + ((flicker.Next(18) == 0) ? 4f * s : 0f);
		string logo = "CAPRI  /  K.mod";
		using (Brush cyan = new SolidBrush(Color.FromArgb(150, Look.Cyan)))
		{
			g.DrawString(logo, big, cyan, left - split, top);
		}
		using (Brush rose = new SolidBrush(Neon.Rose))
		{
			g.DrawString(logo, big, rose, left + split * 0.5f, top);
		}
		float y2 = top + big.Height + 18f * s;
		using (Brush rule = new SolidBrush(Neon.Hot))
		{
			g.FillRectangle(rule, left, y2 - 8f * s, (float)((size.Width - left * 2f) * Smooth((t - 0.15) / 0.4)), 2f * s);
		}
		using Brush white = new SolidBrush(Neon.White);
		using Brush ok = new SolidBrush(Toast.Mint);
		float row = 20f * s;
		for (int i = 0; i < lines.Length; i++)
		{
			double typed = (t - LineAt(i)) / 0.1;
			if (typed <= 0.0)
			{
				break;
			}
			string line = lines[i];
			int count = Math.Min(line.Length, (int)(line.Length * typed));
			string shown = line.Substring(0, count);
			bool done = count == line.Length && line.EndsWith("OK");
			string head = done ? line.Substring(0, line.Length - 2) : shown;
			g.DrawString(head, mono, (i == 0) ? ok : white, left, y2 + i * row);
			if (done)
			{
				float w = g.MeasureString(head, mono, PointF.Empty, StringFormat.GenericTypographic).Width;
				g.DrawString("OK", mono, ok, left + w + 2f * s, y2 + i * row);
			}
		}
		// progress bar in blocks
		float barY = y2 + lines.Length * row + 16f * s;
		float barW = Math.Min(size.Width - left * 2f, 360f * s);
		double fill = Smooth((t - 0.25) / 0.95);
		int blocks = (int)(barW / (10f * s));
		using (Brush on = new SolidBrush(Neon.Rose))
		using (Brush off = new SolidBrush(Color.FromArgb(40, Neon.Rose)))
		{
			for (int i = 0; i < blocks; i++)
			{
				g.FillRectangle((i < blocks * fill) ? on : off, left + i * 10f * s, barY, 7f * s, 10f * s);
			}
		}
		using (Brush faint = new SolidBrush(Neon.Faint))
		{
			g.DrawString((int)(fill * 100.0) + "%", small, faint, left + barW + 10f * s, barY - 1f * s);
		}
		bool cursor = (int)(t * 4.0) % 2 == 0;
		if (cursor)
		{
			g.FillRectangle(white, left, barY + 26f * s, 9f * s, 14f * s);
		}
		using (Pen scan = new Pen(Color.FromArgb(40, 0, 0, 0), Math.Max(1f, s)))
		{
			for (float y = 0f; y < size.Height; y += 3f * s)
			{
				g.DrawLine(scan, 0f, y, size.Width, y);
			}
		}
		// a soft roll bar running down the tube
		float roll = (float)((t * 0.9 % 1.0) * (size.Height + 80f * s)) - 40f * s;
		using LinearGradientBrush band = new LinearGradientBrush(new RectangleF(0f, roll - 40f * s, 10f, 80f * s), Color.FromArgb(0, 255, 255, 255), Color.FromArgb(18, 255, 255, 255), LinearGradientMode.Vertical);
		band.SetBlendTriangularShape(0.5f);
		g.FillRectangle(band, 0f, roll - 40f * s, size.Width, 80f * s);
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			ticks.Dispose();
			big.Dispose();
			mono.Dispose();
			small.Dispose();
		}
		base.Dispose(disposing);
	}
}
