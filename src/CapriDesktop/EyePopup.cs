using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CapriDesktop;

// Capri's eye peeking at the screen now and then. Each one is drawn on the fly with its own size, openness, tilt and colours,
// boots in with a neon glitch, follows the cursor, blinks, and shuts down again. The window is per-pixel transparent,
// click-through and never takes focus, so the mouse and keyboard keep going to whatever is underneath.
internal sealed class EyePopup : Form
{
	private const int IntroMs = 700;

	private const int OutroMs = 560;

	private static readonly Random rolls = new Random();

	// accent (lids, ring, HUD) and iris colours, after the green/blue look of her eye art
	private static readonly (Color Accent, Color Iris)[] Palettes =
	{
		(Color.FromArgb(24, 255, 120), Color.FromArgb(30, 120, 255)),
		(Color.FromArgb(0, 229, 255), Color.FromArgb(255, 46, 136)),
		(Color.FromArgb(255, 46, 136), Color.FromArgb(0, 229, 255)),
		(Color.FromArgb(255, 196, 40), Color.FromArgb(255, 70, 40)),
		(Color.FromArgb(24, 255, 120), Color.FromArgb(0, 229, 255))
	};

	private readonly Stopwatch watch = Stopwatch.StartNew();

	private readonly System.Windows.Forms.Timer clock = new System.Windows.Forms.Timer { Interval = 15 };

	private readonly int delayMs;

	private readonly int holdMs;

	private readonly float eyeWidth;

	private readonly float openUpper;

	private readonly float openLower;

	private readonly float tilt;

	private readonly float irisScale;

	private readonly float coreScale;

	private readonly Color accent;

	private readonly Color iris;

	private readonly string tag;

	private Bitmap canvas;

	private PointF look;

	private PointF lookTarget;

	private double nextSaccade;

	private double blinkAt;

	private bool opened;

	private bool closing;

	internal EyePopup(IList<Rectangle> taken, int delayMs)
	{
		this.delayMs = delayMs;
		// moods: squinting, sleepy, wide awake, or plain watching
		switch (rolls.Next(4))
		{
		case 0:
			eyeWidth = Roll(100f, 170f);
			openUpper = Roll(0.28f, 0.45f);
			openLower = openUpper * 0.8f;
			irisScale = Roll(0.85f, 1f);
			break;
		case 1:
			eyeWidth = Roll(130f, 210f);
			openUpper = Roll(0.4f, 0.55f);
			openLower = Roll(0.75f, 0.9f);
			irisScale = Roll(0.95f, 1.1f);
			break;
		case 2:
			eyeWidth = Roll(200f, 290f);
			openUpper = Roll(0.95f, 1.15f);
			openLower = Roll(0.8f, 0.95f);
			irisScale = Roll(0.75f, 0.9f);
			break;
		default:
			eyeWidth = Roll(140f, 240f);
			openUpper = Roll(0.65f, 0.85f);
			openLower = Roll(0.6f, 0.75f);
			irisScale = Roll(0.85f, 1f);
			break;
		}
		tilt = Roll(-12f, 12f);
		coreScale = Roll(0.32f, 0.5f);
		(accent, iris) = Palettes[rolls.Next(Palettes.Length)];
		holdMs = rolls.Next(2800, 6500);
		tag = "OPTIC." + rolls.Next(0x1000, 0xFFFF).ToString("X4");
		FormBorderStyle = FormBorderStyle.None;
		ShowInTaskbar = false;
		StartPosition = FormStartPosition.Manual;
		AutoScaleMode = AutoScaleMode.None;
		Size = new Size((int)(eyeWidth + 150f), (int)(eyeWidth * 1.05f + 70f));
		Location = Spot(taken);
		canvas = new Bitmap(Width, Height, PixelFormat.Format32bppArgb);
		clock.Tick += (sender, args) => Step();
	}

	protected override bool ShowWithoutActivation => true;

	protected override CreateParams CreateParams
	{
		get
		{
			CreateParams createParams = base.CreateParams;
			// layered | click-through | tool window (no taskbar/alt-tab) | no-activate | topmost
			createParams.ExStyle |= 0x80000 | 0x20 | 0x80 | 0x8000000 | 0x8;
			return createParams;
		}
	}

	protected override void WndProc(ref Message m)
	{
		switch (m.Msg)
		{
		case 0x84: // WM_NCHITTEST: let every click fall through
			m.Result = (IntPtr)(-1);
			return;
		case 0x21: // WM_MOUSEACTIVATE: never activate
			m.Result = (IntPtr)3;
			return;
		}
		base.WndProc(ref m);
	}

	protected override void OnShown(EventArgs e)
	{
		base.OnShown(e);
		Present(-1.0);
		clock.Start();
	}

	private static float Roll(float min, float max)
	{
		return min + (float)rolls.NextDouble() * (max - min);
	}

	private static double Ease(double t)
	{
		t = Math.Clamp(t, 0.0, 1.0);
		return 1.0 - Math.Pow(1.0 - t, 3.0);
	}

	// a random place on a random screen, away from the cursor and from eyes already showing
	private Point Spot(IList<Rectangle> taken)
	{
		Screen[] screens = Screen.AllScreens;
		Point cursor = Cursor.Position;
		Point best = Point.Empty;
		for (int i = 0; i < 30; i++)
		{
			Rectangle area = screens[rolls.Next(screens.Length)].WorkingArea;
			int x = rolls.Next(area.Left + 10, Math.Max(area.Left + 11, area.Right - Width - 10));
			int y = rolls.Next(area.Top + 10, Math.Max(area.Top + 11, area.Bottom - Height - 10));
			Rectangle spot = new Rectangle(x, y, Width, Height);
			best = spot.Location;
			Rectangle near = spot;
			near.Inflate(140, 140);
			if (near.Contains(cursor))
			{
				continue;
			}
			bool clash = false;
			foreach (Rectangle other in taken)
			{
				clash |= other.IntersectsWith(spot);
			}
			if (!clash)
			{
				return best;
			}
		}
		return best;
	}

	private void Step()
	{
		double ms = watch.Elapsed.TotalMilliseconds - delayMs;
		if (ms >= 0.0 && !opened)
		{
			opened = true;
			Sfx.Play(Cue.EyeOpen);
		}
		if (ms >= IntroMs + holdMs && !closing)
		{
			closing = true;
			Sfx.Play(Cue.EyeClose);
		}
		if (ms >= IntroMs + holdMs + OutroMs)
		{
			clock.Stop();
			Close();
			return;
		}
		Present(ms);
	}

	private void Present(double ms)
	{
		if (ms < 0.0)
		{
			Layer(canvas, 0);
			return;
		}
		double outroMs = ms - IntroMs - holdMs;
		double lid;
		double line;
		double glitch;
		double alpha = 1.0;
		double hud;
		if (ms < IntroMs)
		{
			// boot: a neon line strikes across, then the lids snap open with a slight overshoot
			line = Ease(ms / (IntroMs * 0.4));
			double open = Math.Clamp((ms - IntroMs * 0.4) / (IntroMs * 0.6), 0.0, 1.0);
			lid = open <= 0.0 ? 0.0 : 1.0 + 0.12 * Math.Sin(open * Math.PI) - Math.Pow(1.0 - open, 3.0);
			glitch = 1.0 - ms / IntroMs;
			hud = Ease(open);
			if (ms < 220.0)
			{
				alpha = rolls.NextDouble() < 0.35 ? 0.25 : 1.0; // neon tube flicker
			}
		}
		else if (outroMs < 0.0)
		{
			double held = ms - IntroMs;
			line = 1.0;
			lid = 1.0;
			hud = 1.0;
			glitch = rolls.NextDouble() < 0.012 ? 0.6 : 0.0;
			if (blinkAt <= 0.0)
			{
				blinkAt = held + rolls.Next(900, 2600);
			}
			if (held >= blinkAt)
			{
				double blink = (held - blinkAt) / 170.0;
				if (blink >= 1.0)
				{
					blinkAt = held + rolls.Next(1200, 3600);
				}
				else
				{
					lid = Math.Abs(Math.Cos(blink * Math.PI));
				}
			}
		}
		else
		{
			// shutdown: lids slam shut, the line flashes and collapses to a dot, then the dot pops
			double t = outroMs / OutroMs;
			lid = 1.0 - Ease(t / 0.35);
			line = t < 0.35 ? 1.0 : 1.0 - Ease((t - 0.35) / 0.45);
			glitch = Math.Min(1.0, t * 1.6);
			hud = 1.0 - Math.Clamp(t * 2.5, 0.0, 1.0);
			alpha = t < 0.85 ? 1.0 : 1.0 - (t - 0.85) / 0.15;
		}
		Track();
		Render(Math.Max(0.0, lid), line, glitch, hud, ms);
		Layer(canvas, (byte)(Math.Clamp(alpha, 0.0, 1.0) * 255.0));
	}

	// the iris drifts toward the cursor, with small random saccades
	private void Track()
	{
		double now = watch.Elapsed.TotalMilliseconds;
		Point cursor = Cursor.Position;
		float dx = cursor.X - (Left + Width / 2f);
		float dy = cursor.Y - (Top + Height / 2f);
		float length = (float)Math.Sqrt(dx * dx + dy * dy);
		float reach = Math.Min(1f, length / 500f);
		PointF aim = length < 1f ? PointF.Empty : new PointF(dx / length * reach, dy / length * reach);
		if (now >= nextSaccade)
		{
			nextSaccade = now + rolls.Next(350, 1300);
			lookTarget = new PointF(Roll(-0.25f, 0.25f), Roll(-0.2f, 0.2f));
		}
		aim = new PointF(Math.Clamp(aim.X + lookTarget.X, -1f, 1f), Math.Clamp(aim.Y + lookTarget.Y, -1f, 1f));
		look = new PointF(look.X + (aim.X - look.X) * 0.16f, look.Y + (aim.Y - look.Y) * 0.16f);
	}

	private static GraphicsPath Lens(float half, float upper, float lower)
	{
		GraphicsPath path = new GraphicsPath();
		path.AddBezier(0f - half, 0f, (0f - half) * 0.45f, (0f - upper) * 1.333f, half * 0.45f, (0f - upper) * 1.333f, half, 0f);
		path.AddBezier(half, 0f, half * 0.45f, lower * 1.333f, (0f - half) * 0.45f, lower * 1.333f, 0f - half, 0f);
		path.CloseFigure();
		return path;
	}

	private static Color Fade(Color color, double alpha)
	{
		return Color.FromArgb((int)Math.Clamp(alpha * 255.0, 0.0, 255.0), color);
	}

	private void Render(double lid, double line, double glitch, double hud, double ms)
	{
		using (Graphics g = Graphics.FromImage(canvas))
		{
			g.Clear(Color.Transparent);
			g.SmoothingMode = SmoothingMode.AntiAlias;
			g.CompositingQuality = CompositingQuality.HighQuality;
			float half = eyeWidth / 2f;
			float upper = (float)(eyeWidth * 0.3 * openUpper * lid);
			float lower = (float)(eyeWidth * 0.3 * openLower * lid);
			g.TranslateTransform(Width / 2f, Height / 2f);
			g.RotateTransform(tilt);
			if (upper + lower < 2.5f)
			{
				DrawLine(g, half * (float)line, glitch);
			}
			else
			{
				DrawEye(g, half, upper, lower, glitch, ms);
			}
			g.ResetTransform();
			if (hud > 0.01)
			{
				DrawHud(g, hud, ms);
			}
		}
		if (glitch > 0.05 && rolls.NextDouble() < glitch)
		{
			Slice(glitch);
		}
	}

	private void DrawLine(Graphics g, float reach, double glitch)
	{
		if (reach < 1f)
		{
			// collapsed to a dot: a last bright pop
			using SolidBrush pop = new SolidBrush(Fade(Color.White, 0.9));
			g.FillEllipse(pop, -4f, -4f, 8f, 8f);
			return;
		}
		for (int i = 4; i >= 1; i--)
		{
			using Pen glow = new Pen(Fade(accent, 0.12), i * 5f);
			glow.StartCap = LineCap.Round;
			glow.EndCap = LineCap.Round;
			g.DrawLine(glow, 0f - reach, 0f, reach, 0f);
		}
		float shift = (float)(glitch * 7.0);
		using (Pen cyan = new Pen(Fade(Color.FromArgb(0, 229, 255), 0.7), 2f))
		using (Pen magenta = new Pen(Fade(Color.FromArgb(255, 46, 136), 0.7), 2f))
		{
			g.DrawLine(cyan, 0f - reach - shift, 0f - shift / 2f, reach - shift, 0f - shift / 2f);
			g.DrawLine(magenta, 0f - reach + shift, shift / 2f, reach + shift, shift / 2f);
		}
		using Pen core = new Pen(Color.White, 2.5f);
		g.DrawLine(core, 0f - reach, 0f, reach, 0f);
	}

	private void DrawEye(Graphics g, float half, float upper, float lower, double glitch, double ms)
	{
		using GraphicsPath lens = Lens(half, upper, lower);
		// outer neon glow
		for (int i = 4; i >= 1; i--)
		{
			using Pen glow = new Pen(Fade(accent, 0.1), 3f + i * 5f);
			glow.LineJoin = LineJoin.Round;
			g.DrawPath(glow, lens);
		}
		// spikes from her eye art, riding the lids
		using (GraphicsPath spikes = new GraphicsPath())
		{
			spikes.AddPolygon(new PointF[]
			{
				new PointF((0f - half) * 0.62f, (0f - upper) * 0.55f),
				new PointF((0f - half) * 0.98f, (0f - upper) * 1.75f - 8f),
				new PointF((0f - half) * 0.3f, (0f - upper) * 0.95f)
			});
			spikes.AddPolygon(new PointF[]
			{
				new PointF(half * 0.62f, lower * 0.55f),
				new PointF(half * 0.92f, lower * 1.7f + 8f),
				new PointF(half * 0.3f, lower * 0.95f)
			});
			using SolidBrush dark = new SolidBrush(Color.FromArgb(240, 12, 10, 18));
			using Pen rim = new Pen(accent, 2f);
			g.FillPath(dark, spikes);
			g.DrawPath(rim, spikes);
		}
		using (LinearGradientBrush sclera = new LinearGradientBrush(new PointF(0f, 0f - upper - 1f), new PointF(0f, lower + 1f), Color.FromArgb(245, 26, 18, 36), Color.FromArgb(245, 6, 4, 12)))
		{
			g.FillPath(sclera, lens);
		}
		GraphicsState state = g.Save();
		g.SetClip(lens, CombineMode.Intersect);
		float radius = eyeWidth * 0.2f * irisScale;
		float cx = look.X * half * 0.38f;
		float cy = look.Y * Math.Max(upper, lower) * 0.4f;
		// thin outer ring like the art, then the iris and its glowing core
		using (Pen ring = new Pen(accent, Math.Max(2f, radius * 0.07f)))
		{
			g.DrawEllipse(ring, cx - radius * 1.22f, cy - radius * 1.22f, radius * 2.44f, radius * 2.44f);
		}
		using (GraphicsPath disc = new GraphicsPath())
		{
			disc.AddEllipse(cx - radius, cy - radius, radius * 2f, radius * 2f);
			using PathGradientBrush shade = new PathGradientBrush(disc);
			shade.CenterPoint = new PointF(cx, cy);
			shade.CenterColor = ControlPaint.Light(iris, 0.6f);
			shade.SurroundColors = new Color[] { ControlPaint.Dark(iris, 0.35f) };
			g.FillPath(shade, disc);
		}
		// rotating HUD segments on the iris
		using (Pen segments = new Pen(Fade(Color.White, 0.55), Math.Max(1.5f, radius * 0.06f)))
		{
			float r = radius * 0.78f;
			float spin = (float)(ms * 0.09 % 360.0);
			for (int i = 0; i < 6; i++)
			{
				g.DrawArc(segments, cx - r, cy - r, r * 2f, r * 2f, spin + i * 60f, 28f);
			}
		}
		float core = radius * coreScale * (1f + 0.06f * (float)Math.Sin(ms / 260.0));
		using (GraphicsPath glow = new GraphicsPath())
		{
			glow.AddEllipse(cx - core * 1.6f, cy - core * 1.6f, core * 3.2f, core * 3.2f);
			using PathGradientBrush halo = new PathGradientBrush(glow);
			halo.CenterColor = Fade(Color.White, 0.8);
			halo.SurroundColors = new Color[] { Fade(Color.White, 0.0) };
			g.FillPath(halo, glow);
		}
		using (SolidBrush white = new SolidBrush(Color.White))
		{
			g.FillEllipse(white, cx - core, cy - core, core * 2f, core * 2f);
		}
		// shadow of the upper lid and scanlines
		using (LinearGradientBrush shadow = new LinearGradientBrush(new PointF(0f, 0f - upper - 2f), new PointF(0f, 0f - upper * 0.2f), Color.FromArgb(190, 0, 0, 0), Color.FromArgb(0, 0, 0, 0)))
		{
			g.FillRectangle(shadow, 0f - half, 0f - upper - 1f, half * 2f, upper * 0.8f + 1f);
		}
		using (Pen scan = new Pen(Color.FromArgb(45, 0, 0, 0)))
		{
			for (float y = 0f - upper; y < lower; y += 3f)
			{
				g.DrawLine(scan, 0f - half, y, half, y);
			}
		}
		g.Restore(state);
		// chromatic split on the lids while glitching
		if (glitch > 0.02)
		{
			float shift = (float)(glitch * 6.0);
			using Pen cyan = new Pen(Fade(Color.FromArgb(0, 229, 255), 0.75), 2f);
			using Pen magenta = new Pen(Fade(Color.FromArgb(255, 46, 136), 0.75), 2f);
			g.TranslateTransform(0f - shift, 0f);
			g.DrawPath(cyan, lens);
			g.TranslateTransform(shift * 2f, 0f);
			g.DrawPath(magenta, lens);
			g.TranslateTransform(0f - shift, 0f);
		}
		using Pen lid = new Pen(accent, 3f);
		lid.LineJoin = LineJoin.Round;
		g.DrawPath(lid, lens);
	}

	private void DrawHud(Graphics g, double hud, double ms)
	{
		float margin = 14f;
		float arm = 16f;
		RectangleF box = new RectangleF(margin, margin, Width - margin * 2f, Height - margin * 2f - 14f);
		using Pen bracket = new Pen(Fade(accent, 0.75 * hud), 2f);
		g.DrawLines(bracket, new PointF[] { new PointF(box.Left, box.Top + arm), new PointF(box.Left, box.Top), new PointF(box.Left + arm, box.Top) });
		g.DrawLines(bracket, new PointF[] { new PointF(box.Right - arm, box.Top), new PointF(box.Right, box.Top), new PointF(box.Right, box.Top + arm) });
		g.DrawLines(bracket, new PointF[] { new PointF(box.Left, box.Bottom - arm), new PointF(box.Left, box.Bottom), new PointF(box.Left + arm, box.Bottom) });
		g.DrawLines(bracket, new PointF[] { new PointF(box.Right - arm, box.Bottom), new PointF(box.Right, box.Bottom), new PointF(box.Right, box.Bottom - arm) });
		// label types itself in, with a blinking cursor
		string label = "> " + tag + " // LINK " + (look.X >= 0f ? "R" : "L");
		int shown = (int)(label.Length * Math.Clamp(hud * 1.4, 0.0, 1.0));
		string text = label.Substring(0, shown) + (ms % 500.0 < 250.0 ? "_" : " ");
		using Font mono = new Font("Consolas", 8f, FontStyle.Bold);
		using SolidBrush ink = new SolidBrush(Fade(accent, 0.9 * hud));
		g.DrawString(text, mono, ink, box.Left + 2f, box.Bottom + 2f);
	}

	// tear a few horizontal bands sideways
	private void Slice(double glitch)
	{
		using Bitmap copy = (Bitmap)canvas.Clone();
		using Graphics g = Graphics.FromImage(canvas);
		g.CompositingMode = CompositingMode.SourceCopy;
		int bands = rolls.Next(2, 6);
		for (int i = 0; i < bands; i++)
		{
			int y = rolls.Next(0, Height - 4);
			int h = rolls.Next(2, Math.Max(3, Height / 12));
			int dx = (int)((rolls.NextDouble() * 2.0 - 1.0) * 22.0 * glitch);
			g.DrawImage(copy, new Rectangle(dx, y, Width, h), new Rectangle(0, y, Width, h), GraphicsUnit.Pixel);
		}
	}

	private void Layer(Bitmap bitmap, byte alpha)
	{
		if (!IsHandleCreated || bitmap == null)
		{
			return;
		}
		IntPtr screen = GetDC(IntPtr.Zero);
		IntPtr memory = CreateCompatibleDC(screen);
		IntPtr bits = IntPtr.Zero;
		IntPtr previous = IntPtr.Zero;
		try
		{
			bits = bitmap.GetHbitmap(Color.FromArgb(0));
			previous = SelectObject(memory, bits);
			Size size = new Size(bitmap.Width, bitmap.Height);
			Point source = Point.Empty;
			Point topLeft = Location;
			Blend blend = new Blend { BlendOp = 0, BlendFlags = 0, SourceConstantAlpha = alpha, AlphaFormat = 1 };
			UpdateLayeredWindow(Handle, screen, ref topLeft, ref size, memory, ref source, 0, ref blend, 2);
		}
		finally
		{
			ReleaseDC(IntPtr.Zero, screen);
			if (bits != IntPtr.Zero)
			{
				SelectObject(memory, previous);
				DeleteObject(bits);
			}
			DeleteDC(memory);
		}
	}

	protected override void OnFormClosed(FormClosedEventArgs e)
	{
		clock.Dispose();
		canvas?.Dispose();
		canvas = null;
		base.OnFormClosed(e);
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct Blend
	{
		public byte BlendOp;

		public byte BlendFlags;

		public byte SourceConstantAlpha;

		public byte AlphaFormat;
	}

	[DllImport("user32.dll", SetLastError = true)]
	private static extern bool UpdateLayeredWindow(IntPtr hwnd, IntPtr hdcDst, ref Point pptDst, ref Size psize, IntPtr hdcSrc, ref Point pprSrc, int crKey, ref Blend pblend, int dwFlags);

	[DllImport("user32.dll")]
	private static extern IntPtr GetDC(IntPtr hWnd);

	[DllImport("user32.dll")]
	private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

	[DllImport("gdi32.dll")]
	private static extern IntPtr CreateCompatibleDC(IntPtr hDC);

	[DllImport("gdi32.dll")]
	private static extern bool DeleteDC(IntPtr hdc);

	[DllImport("gdi32.dll")]
	private static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObject);

	[DllImport("gdi32.dll")]
	private static extern bool DeleteObject(IntPtr hObject);
}
