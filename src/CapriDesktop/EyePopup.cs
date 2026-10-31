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
		(Color.FromArgb(24, 255, 115), Color.FromArgb(28, 112, 255)),
		(Color.FromArgb(24, 255, 115), Color.FromArgb(28, 112, 255)),
		(Color.FromArgb(24, 255, 115), Color.FromArgb(28, 112, 255)),
		(Color.FromArgb(60, 255, 150), Color.FromArgb(20, 90, 230)),
		(Color.FromArgb(24, 240, 100), Color.FromArgb(40, 140, 255))
	};

	private static readonly Color Ink = Color.FromArgb(12, 10, 16);

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

	// the green visor panel behind the eye is a slightly crooked quad, like her face art
	private readonly PointF[] skew = new PointF[4];

	private Bitmap canvas;

	private PointF look;

	private PointF lookTarget;

	private double nextSaccade;

	private double blinkAt;

	private bool opened;

	private bool closing;

	private float peak;

	internal EyePopup(IList<Rectangle> taken, int delayMs)
	{
		this.delayMs = delayMs;
		// moods: squinting, sleepy, wide awake, or plain watching
		switch (rolls.Next(4))
		{
		case 0:
			eyeWidth = Roll(100f, 170f);
			openUpper = Roll(0.5f, 0.65f);
			openLower = openUpper * 0.8f;
			irisScale = Roll(0.85f, 1f);
			break;
		case 1:
			eyeWidth = Roll(130f, 210f);
			openUpper = Roll(0.6f, 0.75f);
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
			openUpper = Roll(0.8f, 0.95f);
			openLower = Roll(0.6f, 0.75f);
			irisScale = Roll(0.85f, 1f);
			break;
		}
		tilt = Roll(-12f, 12f);
		coreScale = Roll(0.32f, 0.5f);
		(accent, iris) = Palettes[rolls.Next(Palettes.Length)];
		holdMs = rolls.Next(2800, 6500);
		// box shapes: crooked quad, trapezoid wide on top, trapezoid wide below, slanted parallelogram
		int box = rolls.Next(4);
		float slant = Roll(0.12f, 0.22f) * (rolls.Next(2) == 0 ? 1f : -1f);
		for (int i = 0; i < 4; i++)
		{
			bool topCorner = i < 2;
			bool leftCorner = i == 0 || i == 3;
			float x = Roll(-0.05f, 0.05f);
			float y = Roll(-0.1f, 0.1f);
			switch (box)
			{
			case 0:
				x += Roll(-0.06f, 0.06f);
				y += Roll(-0.14f, 0.14f);
				break;
			case 1:
				x += topCorner ? 0.08f : -0.14f;
				break;
			case 2:
				x += topCorner ? -0.14f : 0.08f;
				break;
			default:
				x += (topCorner == leftCorner ? -1f : 1f) * slant;
				break;
			}
			skew[i] = new PointF(x, y);
		}
		tag = "OPTIC." + rolls.Next(0x1000, 0xFFFF).ToString("X4");
		FormBorderStyle = FormBorderStyle.None;
		ShowInTaskbar = false;
		StartPosition = FormStartPosition.Manual;
		AutoScaleMode = AutoScaleMode.None;
		Size = new Size((int)(eyeWidth * 1.55f + 70f), (int)(eyeWidth * 1.2f + 80f));
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
			if (line < 0.999 && upper + lower < 2.5f)
			{
				DrawLine(g, half * 1.25f * (float)line, glitch);
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
		// green visor panel: grows out of the boot line, stays put while she blinks
		float panelX = half * 1.3f;
		float panelY = eyeWidth * 0.3f * Math.Max(openUpper, openLower) * 1.15f + eyeWidth * 0.12f;
		float grow = (float)Math.Clamp(Math.Max(upper + lower, 0f) / Math.Max(1f, eyeWidth * 0.3f * (openUpper + openLower)) * 1.6f, 0.08, 1.0);
		if (!closing)
		{
			grow = peak = Math.Max(grow, peak);
		}
		PointF[] panel =
		{
			new PointF(-panelX * (1f + skew[0].X), -panelY * grow * (1f + skew[0].Y)),
			new PointF(panelX * (1f + skew[1].X), -panelY * grow * (1f + skew[1].Y)),
			new PointF(panelX * (1f + skew[2].X), panelY * grow * (1f + skew[2].Y)),
			new PointF(-panelX * (1f + skew[3].X), panelY * grow * (1f + skew[3].Y))
		};
		for (int i = 3; i >= 1; i--)
		{
			using Pen glow = new Pen(Fade(accent, 0.1), i * 6f);
			glow.LineJoin = LineJoin.Round;
			g.DrawPolygon(glow, panel);
		}
		using (SolidBrush plate = new SolidBrush(accent))
		{
			g.FillPolygon(plate, panel);
		}
		using (Pen scan = new Pen(Color.FromArgb(28, 0, 0, 0)))
		{
			float top = -panelY * grow * 1.15f;
			for (float y = top; y < -top; y += 3f)
			{
				g.DrawLine(scan, -panelX * 1.1f, y, panelX * 1.1f, y);
			}
		}
		using SolidBrush ink = new SolidBrush(Ink);
		if (upper + lower < 3f)
		{
			// shut: a black curved lash with a crossing thorn, like her wink
			using Pen lash = new Pen(Ink, Math.Max(3f, eyeWidth * 0.035f));
			lash.StartCap = LineCap.Triangle;
			lash.EndCap = LineCap.Triangle;
			g.DrawBezier(lash, -half, 0f, -half * 0.4f, -eyeWidth * 0.07f, half * 0.4f, -eyeWidth * 0.07f, half * 1.05f, eyeWidth * 0.02f);
			using Pen thorn = new Pen(Ink, Math.Max(2f, eyeWidth * 0.02f));
			g.DrawLine(thorn, -half * 0.2f, -eyeWidth * 0.16f, half * 0.35f, eyeWidth * 0.08f);
			return;
		}
		using GraphicsPath lens = Lens(half, upper, lower);
		// thorns from her eye art: one up off the outer lid, one down off the inner lid, plus a small tick
		using (GraphicsPath thorns = new GraphicsPath())
		{
			float reachUp = Math.Max(upper, 4f);
			float reachDown = Math.Max(lower, 4f);
			thorns.AddPolygon(new PointF[]
			{
				new PointF(half * 0.18f, -reachUp * 0.9f),
				new PointF(half * 0.62f, -reachUp * 1.65f - eyeWidth * 0.05f),
				new PointF(half * 0.42f, -reachUp * 0.7f)
			});
			thorns.AddPolygon(new PointF[]
			{
				new PointF(-half * 0.62f, reachDown * 0.55f),
				new PointF(-half * 0.92f, reachDown * 1.6f + eyeWidth * 0.05f),
				new PointF(-half * 0.38f, reachDown * 0.85f)
			});
			thorns.AddPolygon(new PointF[]
			{
				new PointF(half * 0.55f, reachDown * 0.6f),
				new PointF(half * 0.78f, reachDown * 1.25f + eyeWidth * 0.02f),
				new PointF(half * 0.68f, reachDown * 0.45f)
			});
			g.FillPath(ink, thorns);
		}
		g.FillPath(ink, lens);
		GraphicsState state = g.Save();
		g.SetClip(lens, CombineMode.Intersect);
		float radius = eyeWidth * 0.2f * irisScale;
		float cx = look.X * half * 0.32f;
		float cy = look.Y * Math.Max(upper, lower) * 0.35f;
		// flat target: green ring, black gap, blue iris, white pupil
		using (Pen ring = new Pen(accent, Math.Max(2.5f, radius * 0.1f)))
		{
			g.DrawEllipse(ring, cx - radius * 1.2f, cy - radius * 1.2f, radius * 2.4f, radius * 2.4f);
		}
		using (SolidBrush blue = new SolidBrush(iris))
		{
			g.FillEllipse(blue, cx - radius * 0.9f, cy - radius * 0.9f, radius * 1.8f, radius * 1.8f);
		}
		float core = radius * coreScale * (1f + 0.05f * (float)Math.Sin(ms / 260.0));
		float px = cx + look.X * radius * 0.15f;
		float py = cy + look.Y * radius * 0.15f;
		using (SolidBrush white = new SolidBrush(Color.White))
		{
			g.FillEllipse(white, px - core, py - core, core * 2f, core * 2f);
		}
		g.Restore(state);
		// chromatic split on the lids while glitching
		if (glitch > 0.02)
		{
			float shift = (float)(glitch * 6.0);
			using Pen cyan = new Pen(Fade(Color.FromArgb(0, 229, 255), 0.75), 2f);
			using Pen magenta = new Pen(Fade(Color.FromArgb(255, 46, 136), 0.75), 2f);
			g.TranslateTransform(-shift, 0f);
			g.DrawPath(cyan, lens);
			g.TranslateTransform(shift * 2f, 0f);
			g.DrawPath(magenta, lens);
			g.TranslateTransform(-shift, 0f);
		}
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
