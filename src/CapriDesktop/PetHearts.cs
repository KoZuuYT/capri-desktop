using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CapriDesktop;

// Petting effects, drawn per-pixel transparent, click-through and never taking focus.
// Stroke: a little spark burst under the cursor while you rub her. React: pixel hearts pop out of her with green rings,
// rise and wobble (with a purr wave when she purrs). The end: the hearts pull back into one big pixel heart that pops
// into pixels with a fading ring.
internal sealed class PetHearts : Form
{
	internal enum Kind
	{
		Stroke,
		React
	}

	private static readonly Random rolls = new Random();

	private static readonly Color Pink = Color.FromArgb(255, 92, 170);

	private static readonly Color Green = Color.FromArgb(24, 255, 115);

	private static readonly Color Blue = Color.FromArgb(40, 140, 255);

	private static readonly Color Ink = Color.FromArgb(12, 10, 16);

	// 7x6 pixel heart
	private static readonly string[] HeartRows = { ".XX.XX.", "XXXXXXX", "XXXXXXX", ".XXXXX.", "..XXX..", "...X..." };

	private readonly Stopwatch watch = Stopwatch.StartNew();

	private readonly System.Windows.Forms.Timer clock = new System.Windows.Forms.Timer { Interval = 15 };

	private readonly Kind kind;

	private readonly bool purr;

	private readonly int lifeMs;

	private readonly int endMs;

	private readonly List<Bit> bits = new List<Bit>();

	private bool endCued;

	private Bitmap canvas;

	// fired once when the closing animation starts
	internal event EventHandler Ending;

	private sealed class Bit
	{
		public float X;

		public float Vx;

		public float Vy;

		public float Size;

		public float Phase;

		public int DelayMs;

		public bool Heart;

		public Color Color;
	}

	internal PetHearts(Kind kind = Kind.React, bool purr = false)
	{
		this.kind = kind;
		this.purr = purr;
		FormBorderStyle = FormBorderStyle.None;
		ShowInTaskbar = false;
		TopMost = true;
		StartPosition = FormStartPosition.Manual;
		if (kind == Kind.Stroke)
		{
			Size = new Size(110, 110);
			lifeMs = 520;
			endMs = lifeMs;
			for (int i = 0; i < 7; i++)
			{
				double angle = rolls.NextDouble() * Math.PI * 2.0;
				float speed = 50f + (float)rolls.NextDouble() * 70f;
				bits.Add(new Bit
				{
					Vx = (float)Math.Cos(angle) * speed,
					Vy = (float)Math.Sin(angle) * speed - 30f,
					Size = 2f + (float)rolls.NextDouble() * 2.5f,
					Heart = i == 0,
					Color = i == 0 ? Pink : (rolls.Next(3) == 0 ? Blue : Green)
				});
			}
		}
		else
		{
			Size = new Size(280, 260);
			lifeMs = purr ? 3300 : 2700;
			endMs = lifeMs - 700;
			int count = purr ? 9 : 7;
			for (int i = 0; i < count; i++)
			{
				bits.Add(new Bit
				{
					X = (float)(rolls.NextDouble() * 2.0 - 1.0) * 70f,
					Vx = (float)(rolls.NextDouble() * 2.0 - 1.0) * 40f,
					Vy = -(40f + (float)rolls.NextDouble() * 35f),
					Size = 3f + (float)rolls.NextDouble() * 2.5f,
					Phase = (float)(rolls.NextDouble() * Math.PI * 2.0),
					DelayMs = i * 70 + rolls.Next(60),
					Heart = true,
					Color = rolls.Next(4) == 0 ? Green : Pink
				});
			}
		}
		clock.Tick += (sender, args) => Step();
	}

	protected override bool ShowWithoutActivation => true;

	protected override CreateParams CreateParams
	{
		get
		{
			CreateParams createParams = base.CreateParams;
			// layered, click-through, tool window, no activate, topmost
			createParams.ExStyle |= 0x80000 | 0x20 | 0x80 | 0x8000000 | 0x8;
			return createParams;
		}
	}

	// centres a stroke burst on the cursor (a 1x1 rect), or a reaction over her head
	internal void Place(Rectangle near)
	{
		if (kind == Kind.Stroke)
		{
			Location = new Point(near.X - Width / 2, near.Y - Height / 2);
		}
		else
		{
			Location = new Point(near.Left + near.Width / 2 - Width / 2, near.Top + Math.Min(near.Height / 3, 120) - Height / 2 - 40);
		}
	}

	protected override void OnShown(EventArgs e)
	{
		base.OnShown(e);
		canvas = new Bitmap(Width, Height, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
		Step();
		clock.Start();
	}

	private void Step()
	{
		double ms = watch.Elapsed.TotalMilliseconds;
		if (ms >= lifeMs)
		{
			clock.Stop();
			Close();
			return;
		}
		if (kind == Kind.React && !endCued && ms >= endMs)
		{
			endCued = true;
			Ending?.Invoke(this, EventArgs.Empty);
		}
		using (Graphics g = Graphics.FromImage(canvas))
		{
			g.Clear(Color.Transparent);
			g.SmoothingMode = SmoothingMode.AntiAlias;
			g.TranslateTransform(Width / 2f, Height / 2f);
			if (kind == Kind.Stroke)
			{
				DrawStroke(g, ms);
			}
			else
			{
				DrawReact(g, ms);
			}
		}
		Layer(canvas);
	}

	private void DrawStroke(Graphics g, double ms)
	{
		float p = (float)(ms / lifeMs);
		float fade = 1f - p * p;
		using (Pen ring = new Pen(Color.FromArgb((int)(170 * fade), Green), 2f * (1f - p) + 0.5f))
		{
			float r = 6f + 38f * Ease(p);
			g.DrawEllipse(ring, -r, -r, r * 2f, r * 2f);
		}
		float t = (float)(ms / 1000.0);
		foreach (Bit bit in bits)
		{
			float x = bit.Vx * t;
			float y = bit.Vy * t + 90f * t * t;
			if (bit.Heart)
			{
				float pop = p < 0.25f ? Ease(p / 0.25f) * 1.3f : 1.3f - 0.3f * (p - 0.25f);
				Heart(g, x * 0.3f, y * 0.5f - 6f, 2.6f * pop, Color.FromArgb((int)(255 * fade), bit.Color), outline: true);
			}
			else
			{
				using Brush brush = new SolidBrush(Color.FromArgb((int)(255 * fade), bit.Color));
				float s = bit.Size * (1f - p * 0.6f);
				g.FillRectangle(brush, x - s / 2f, y - s / 2f, s, s);
			}
		}
	}

	private void DrawReact(Graphics g, double ms)
	{
		float center = 30f;
		// burst: a green ring and a dashed blue one
		if (ms < 700)
		{
			for (int k = 0; k < 2; k++)
			{
				float q = (float)Math.Max(0.0, (ms - k * 140) / 560.0);
				if (q > 0f && q < 1f)
				{
					float r = 12f + 110f * Ease(q);
					using Pen pen = new Pen(Color.FromArgb((int)(200 * (1f - q)), k == 0 ? Green : Blue), 3f * (1f - q) + 0.6f);
					pen.DashStyle = k == 1 ? DashStyle.Dash : DashStyle.Solid;
					g.DrawEllipse(pen, -r, center - r, r * 2f, r * 2f);
				}
			}
		}
		double ending = Math.Max(0.0, (ms - endMs) / (lifeMs - endMs));
		float pull = ending > 0.0 ? Ease((float)Math.Min(1.0, ending / 0.4)) : 0f;
		float t = (float)(ms / 1000.0);
		if (ending < 0.4)
		{
			foreach (Bit bit in bits)
			{
				double age = ms - bit.DelayMs;
				if (age < 0.0)
				{
					continue;
				}
				float a = (float)(age / 1000.0);
				float x = bit.X + bit.Vx * a + (float)Math.Sin(t * 5.0 + bit.Phase) * 10f;
				float y = center + bit.Vy * a - 12f * a * a + (float)Math.Cos(t * 7.0 + bit.Phase) * 3f;
				y = Math.Max(y, -Height / 2f + 22f);
				x += (0f - x) * pull;
				y += (-40f - y) * pull;
				float pop = age < 260.0 ? Overshoot((float)(age / 260.0)) : 1f + 0.08f * (float)Math.Sin(t * 9.0 + bit.Phase);
				// chromatic glitch ghost now and then
				if (rolls.Next(14) == 0)
				{
					Heart(g, x + 3f, y, bit.Size * pop, Color.FromArgb(120, Blue), outline: false);
				}
				Heart(g, x, y, bit.Size * pop, bit.Color, outline: true);
			}
			float fade = ending > 0.0 ? 1f - (float)(ending / 0.4) : 1f;
			if (purr)
			{
				DrawPurrWave(g, ms, center + 66f, fade * Math.Min(1f, (float)(ms / 300.0)));
			}
			else if (ms > 150)
			{
				float q = (float)Math.Min(1.0, (ms - 150) / 400.0);
				using Font font = new Font("Consolas", 12f, FontStyle.Bold);
				using Brush brush = new SolidBrush(Color.FromArgb((int)(230 * q * fade), Green));
				g.DrawString(Typed("+AFFECTION", q), font, brush, -48f, center + 60f);
			}
		}
		// end: the hearts meet in one big heart that pops into pixels with a ring
		if (ending > 0.35)
		{
			float q = (float)((ending - 0.35) / 0.65);
			if (q < 0.35f)
			{
				Heart(g, 0f, -40f, 6f * Overshoot(q / 0.35f), Pink, outline: true);
			}
			else
			{
				float r = (q - 0.35f) / 0.65f;
				using (Pen pen = new Pen(Color.FromArgb((int)(220 * (1f - r)), Green), 2.5f * (1f - r) + 0.5f))
				{
					float rad = 20f + 80f * Ease(r);
					g.DrawEllipse(pen, -rad, -40f - rad, rad * 2f, rad * 2f);
				}
				Random scatter = new Random(3);
				for (int row = 0; row < HeartRows.Length; row++)
				{
					for (int col = 0; col < 7; col++)
					{
						if (HeartRows[row][col] != 'X')
						{
							continue;
						}
						float px = (col - 3f) * 6f;
						float py = -40f + (row - 2.5f) * 6f;
						float dx = px * 3f + (float)(scatter.NextDouble() - 0.5) * 60f;
						float dy = (row - 2.5f) * 18f + (float)(scatter.NextDouble() - 0.5) * 60f - 20f;
						using Brush brush = new SolidBrush(Color.FromArgb((int)(255 * (1f - r)), scatter.Next(3) == 0 ? Green : Pink));
						float s = 6f * (1f - r * 0.7f);
						g.FillRectangle(brush, px + dx * Ease(r), py + dy * Ease(r) + 30f * r * r, s, s);
					}
				}
			}
		}
	}

	// a little oscilloscope purr: a green line that buzzes in pulses, with a typed "prrr" tag
	private static void DrawPurrWave(Graphics g, double ms, float y, float fade)
	{
		int alpha = (int)(240 * Math.Max(0f, Math.Min(1f, fade)));
		float t = (float)(ms / 1000.0);
		float width = 160f;
		float pulse = (float)Math.Pow(0.5 + 0.5 * Math.Sin(t * 24.0), 2.0);
		PointF[] line = new PointF[48];
		for (int i = 0; i < line.Length; i++)
		{
			float u = i / (float)(line.Length - 1);
			float edge = (float)Math.Sin(Math.PI * u);
			float buzz = (float)Math.Sin(u * 38.0 - t * 30.0) * (0.25f + 0.75f * pulse);
			line[i] = new PointF(-width / 2f + width * u, y + buzz * 9f * edge);
		}
		using (Pen glow = new Pen(Color.FromArgb(alpha / 4, Green), 6f))
		{
			g.DrawLines(glow, line);
		}
		using (Pen pen = new Pen(Color.FromArgb(alpha, Green), 2f))
		{
			g.DrawLines(pen, line);
		}
		using Font font = new Font("Consolas", 11f, FontStyle.Bold);
		using Brush brush = new SolidBrush(Color.FromArgb(alpha, Green));
		using Brush shadow = new SolidBrush(Color.FromArgb(alpha / 2, Ink));
		string text = Typed("~prrrr.exe~", (float)Math.Min(1.0, ms / 700.0));
		g.DrawString(text, font, shadow, -45f, y + 15f);
		g.DrawString(text, font, brush, -46f, y + 14f);
	}

	private static string Typed(string text, float progress)
	{
		int n = (int)Math.Ceiling(text.Length * Math.Max(0f, Math.Min(1f, progress)));
		return text.Substring(0, n) + (n < text.Length ? "_" : "");
	}

	// pixel heart centred on (x, y), each pixel `size` wide, with a dark outline like her art
	private static void Heart(Graphics g, float x, float y, float size, Color color, bool outline)
	{
		if (size <= 0.2f)
		{
			return;
		}
		SmoothingMode mode = g.SmoothingMode;
		g.SmoothingMode = SmoothingMode.None;
		float left = x - 3.5f * size;
		float top = y - 3f * size;
		if (outline)
		{
			using Brush ink = new SolidBrush(Color.FromArgb(color.A * 3 / 4, Ink));
			for (int row = 0; row < HeartRows.Length; row++)
			{
				for (int col = 0; col < 7; col++)
				{
					if (HeartRows[row][col] == 'X')
					{
						g.FillRectangle(ink, left + col * size - size * 0.5f, top + row * size - size * 0.5f, size * 2f, size * 2f);
					}
				}
			}
		}
		using Brush fill = new SolidBrush(color);
		using Brush shine = new SolidBrush(Color.FromArgb(color.A, 255, 220, 240));
		for (int row = 0; row < HeartRows.Length; row++)
		{
			for (int col = 0; col < 7; col++)
			{
				if (HeartRows[row][col] == 'X')
				{
					g.FillRectangle(row == 1 && col == 1 ? shine : fill, left + col * size, top + row * size, size + 0.5f, size + 0.5f);
				}
			}
		}
		g.SmoothingMode = mode;
	}

	private static float Ease(float p)
	{
		p = Math.Max(0f, Math.Min(1f, p));
		return 1f - (1f - p) * (1f - p) * (1f - p);
	}

	// back-out ease: overshoots a little then settles at 1
	private static float Overshoot(float p)
	{
		p = Math.Max(0f, Math.Min(1f, p));
		const float c = 1.9f;
		return 1f + (c + 1f) * (float)Math.Pow(p - 1f, 3.0) + c * (float)Math.Pow(p - 1f, 2.0);
	}

	private void Layer(Bitmap bitmap)
	{
		if (!IsHandleCreated || bitmap == null)
		{
			return;
		}
		IntPtr screen = GetDC(IntPtr.Zero);
		IntPtr memory = CreateCompatibleDC(screen);
		IntPtr hbits = IntPtr.Zero;
		IntPtr previous = IntPtr.Zero;
		try
		{
			hbits = bitmap.GetHbitmap(Color.FromArgb(0));
			previous = SelectObject(memory, hbits);
			Size size = new Size(bitmap.Width, bitmap.Height);
			Point source = Point.Empty;
			Point topLeft = Location;
			Blend blend = new Blend { BlendOp = 0, BlendFlags = 0, SourceConstantAlpha = 255, AlphaFormat = 1 };
			UpdateLayeredWindow(Handle, screen, ref topLeft, ref size, memory, ref source, 0, ref blend, 2);
		}
		finally
		{
			ReleaseDC(IntPtr.Zero, screen);
			if (hbits != IntPtr.Zero)
			{
				SelectObject(memory, previous);
				DeleteObject(hbits);
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
