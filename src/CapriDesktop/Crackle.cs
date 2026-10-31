using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace CapriDesktop;

// Her little lightning, in the anime's look: thin angular red electric lines that re-trace every couple of frames,
// red spikes with near-black outlines bursting out of the floor, and red/black glitch blocks.
// One click-through layered window that grows to cover whatever zaps are alive, in screen coordinates.
internal sealed class Crackle : Overlay
{
	private enum Kind
	{
		Arc,
		Spike,
		Block
	}

	private sealed class Zap
	{
		internal Kind Kind;

		internal float X;

		internal float Y;

		internal float Angle;

		internal float Length;

		internal float Width;

		internal double Born;

		internal double Life;

		internal int Seed;

		internal Color Colour;
	}

	// her flat solid palette: one red, one green, no gradients
	internal static readonly Color Hot = Color.FromArgb(247, 6, 63);

	internal static readonly Color Ember = Hot;

	internal static readonly Color Flame = Color.FromArgb(0, 217, 93);

	internal static readonly Color Ice = Hot;

	private static readonly Color Soot = Color.FromArgb(22, 2, 6);

	private readonly Timer ticks = new Timer();

	private readonly Stopwatch watch = Stopwatch.StartNew();

	private readonly Random random = new Random();

	private readonly List<Zap> zaps = new List<Zap>();

	protected override CreateParams CreateParams
	{
		get
		{
			CreateParams createParams = base.CreateParams;
			createParams.ExStyle |= 32;
			return createParams;
		}
	}

	internal Crackle()
		: base("Capri crackle")
	{
		ticks.Interval = 33;
		ticks.Tick += (object sender, EventArgs e) => Step();
	}

	// a strong hit: arcs leap out of her body, glitch blocks flicker and, on the floor, flame-spikes shoot up
	internal void Burst(Rectangle body, float scale, double power, bool ground)
	{
		power = Math.Max(0.15, Math.Min(1.0, power));
		double now = watch.Elapsed.TotalSeconds;
		float cx = body.Left + body.Width / 2f;
		float cy = body.Top + body.Height * 0.5f;
		int arcs = 3 + (int)Math.Round(power * 6.0);
		for (int i = 0; i < arcs; i++)
		{
			double way = random.NextDouble() * Math.PI * 2.0;
			float r = body.Width * (0.15f + (float)random.NextDouble() * 0.25f);
			Add(Kind.Arc, cx + (float)Math.Cos(way) * r, cy + (float)Math.Sin(way) * r * 1.5f, (float)way,
				(float)((40.0 + random.NextDouble() * 90.0) * (0.6 + power) * scale), Math.Max(1.5f, 2.6f * scale), now + random.NextDouble() * 0.12, 0.16 + random.NextDouble() * 0.2, Pick(Hot, Hot, Ember, Ice));
		}
		if (ground)
		{
			int spikes = 4 + (int)Math.Round(power * 7.0);
			float floor = body.Bottom;
			for (int i = 0; i < spikes; i++)
			{
				float x = cx + (float)((random.NextDouble() * 2.0 - 1.0) * body.Width * (0.5 + power * 0.6));
				float lean = (float)((x - cx) / Math.Max(1f, body.Width) * 0.9 + (random.NextDouble() - 0.5) * 0.3);
				Add(Kind.Spike, x, floor, lean, (float)((30.0 + random.NextDouble() * 70.0) * (0.5 + power) * scale), (float)((7.0 + random.NextDouble() * 9.0) * scale),
					now + Math.Abs(x - cx) / Math.Max(1f, body.Width) * 0.08, 0.3 + random.NextDouble() * 0.2, Pick(Ember, Ember, Flame, Hot));
			}
		}
		int blocks = 2 + (int)Math.Round(power * 5.0);
		for (int i = 0; i < blocks; i++)
		{
			Add(Kind.Block, body.Left + (float)random.NextDouble() * body.Width, body.Top + (float)random.NextDouble() * body.Height, 0f,
				(float)((6.0 + random.NextDouble() * 22.0) * scale), (float)((2.0 + random.NextDouble() * 5.0) * scale), now + random.NextDouble() * 0.2, 0.08 + random.NextDouble() * 0.12, Pick(Hot, Hot, Soot, Flame));
		}
		// glitch scanlines: long thin bars torn sideways across her
		int tears = 1 + (int)Math.Round(power * 3.0);
		for (int i = 0; i < tears; i++)
		{
			Add(Kind.Block, cx, body.Top + (float)random.NextDouble() * body.Height, 0f,
				(float)(body.Width * (0.8 + random.NextDouble() * 0.9)), Math.Max(1f, (float)((1.0 + random.NextDouble() * 2.0) * scale)), now + random.NextDouble() * 0.25, 0.05 + random.NextDouble() * 0.08, Pick(Hot, Flame, Soot));
		}
		Run();
	}

	// one or two short arcs crackling off her edges, for flight and fast swings
	internal void Trail(Rectangle body, float scale, double trailing)
	{
		double now = watch.Elapsed.TotalSeconds;
		int count = 1 + random.Next(2);
		for (int i = 0; i < count; i++)
		{
			float x = body.Left + (float)random.NextDouble() * body.Width;
			float y = body.Top + body.Height * (0.2f + (float)random.NextDouble() * 0.7f);
			double way = trailing + (random.NextDouble() - 0.5) * 1.6;
			Add(Kind.Arc, x, y, (float)way, (float)((25.0 + random.NextDouble() * 45.0) * scale), Math.Max(1.2f, 1.8f * scale), now, 0.1 + random.NextDouble() * 0.12, Pick(Hot, Ember, Ice));
		}
		Run();
	}

	// a tiny static fizz at her feet while she walks
	internal void Fizz(Point feet, float scale)
	{
		double now = watch.Elapsed.TotalSeconds;
		double way = -Math.PI / 2.0 + (random.NextDouble() - 0.5) * 2.2;
		Add(Kind.Arc, feet.X + (float)((random.NextDouble() - 0.5) * 30.0 * scale), feet.Y - 2f, (float)way, (float)((12.0 + random.NextDouble() * 20.0) * scale), Math.Max(1f, 1.4f * scale), now, 0.09 + random.NextDouble() * 0.08, Pick(Hot, Ice));
		if (random.Next(3) == 0)
		{
			Add(Kind.Spike, feet.X + (float)((random.NextDouble() - 0.5) * 40.0 * scale), feet.Y, (float)((random.NextDouble() - 0.5) * 0.6), (float)((10.0 + random.NextDouble() * 14.0) * scale), (float)(5.0 * scale), now, 0.2, Ember);
		}
		Run();
	}

	internal void Clear()
	{
		zaps.Clear();
		ticks.Stop();
		if (Visible)
		{
			Hide();
		}
	}

	private Color Pick(params Color[] colours)
	{
		return colours[random.Next(colours.Length)];
	}

	private void Add(Kind kind, float x, float y, float angle, float length, float width, double born, double life, Color colour)
	{
		if (zaps.Count > 160)
		{
			zaps.RemoveAt(0);
		}
		zaps.Add(new Zap
		{
			Kind = kind,
			X = x,
			Y = y,
			Angle = angle,
			Length = length,
			Width = width,
			Born = born,
			Life = life,
			Seed = random.Next(),
			Colour = colour
		});
	}

	private void Run()
	{
		if (!ticks.Enabled)
		{
			ticks.Start();
		}
		Step();
	}

	private void Step()
	{
		double now = watch.Elapsed.TotalSeconds;
		zaps.RemoveAll((Zap z) => now >= z.Born + z.Life);
		if (zaps.Count == 0)
		{
			Clear();
			return;
		}
		RectangleF reach = RectangleF.Empty;
		foreach (Zap zap in zaps)
		{
			float r = zap.Length * 1.4f + zap.Width * 4f + 12f;
			RectangleF one = new RectangleF(zap.X - r, zap.Y - r, r * 2f, r * 2f);
			reach = reach.IsEmpty ? one : RectangleF.Union(reach, one);
		}
		Rectangle area = Rectangle.Intersect(Rectangle.Round(reach), SystemInformation.VirtualScreen);
		if (area.Width < 2 || area.Height < 2)
		{
			return;
		}
		int flicker = (int)(now * 30.0) / 2;
		Bitmap bitmap = new Bitmap(area.Width, area.Height, PixelFormat.Format32bppPArgb);
		using (Graphics graphics = Graphics.FromImage(bitmap))
		{
			graphics.SmoothingMode = SmoothingMode.None;
			graphics.TranslateTransform(-area.Left, -area.Top);
			foreach (Zap zap in zaps)
			{
				double u = (now - zap.Born) / zap.Life;
				if (!(u < 0.0))
				{
					Draw(graphics, zap, u, flicker);
				}
			}
		}
		Present(bitmap, area.Location);
	}

	private static void Draw(Graphics graphics, Zap zap, double u, int flicker)
	{
		Random pick = new Random(zap.Seed + flicker * 7919);
		switch (zap.Kind)
		{
		case Kind.Arc:
		{
			// shoots out fast, holds, then the tail catches up to the tip
			float grow = (float)Math.Min(1.0, u / 0.25);
			float tail = (float)Math.Max(0.0, (u - 0.55) / 0.45);
			if (pick.Next(6) == 0)
			{
				break;
			}
			float alpha = 1f;
			PointF from = new PointF(zap.X + (float)Math.Cos(zap.Angle) * zap.Length * tail, zap.Y + (float)Math.Sin(zap.Angle) * zap.Length * tail);
			PointF to = new PointF(zap.X + (float)Math.Cos(zap.Angle) * zap.Length * grow, zap.Y + (float)Math.Sin(zap.Angle) * zap.Length * grow);
			Bolt(graphics, from, to, zap.Width, zap.Colour, alpha, pick, 0.35f, 1);
			break;
		}
		case Kind.Spike:
			Spike(graphics, zap, u, pick);
			break;
		default:
		{
			if (pick.Next(3) != 0)
			{
				float jitter = (float)((pick.NextDouble() - 0.5) * zap.Length);
				using Brush brush = new SolidBrush(zap.Colour);
				graphics.FillRectangle(brush, zap.X + jitter - zap.Length / 2f, zap.Y, zap.Length, zap.Width);
			}
			break;
		}
		}
	}

	// a flame tongue from the floor: grows up fast with a kink, then sinks back, flat-shaded with a dark outline
	private static void Spike(Graphics graphics, Zap zap, double u, Random pick)
	{
		double up = u < 0.2 ? Smooth(u / 0.2) : 1.0 - Smooth((u - 0.45) / 0.55);
		float h = zap.Length * (float)up;
		if (h < 2f)
		{
			return;
		}
		float w = zap.Width * (0.7f + 0.3f * (float)up);
		float lean = zap.Angle;
		float x = zap.X;
		float y = zap.Y;
		float kink = (float)((pick.NextDouble() - 0.5) * w * 0.8);
		PointF[] shape = new PointF[]
		{
			new PointF(x - w, y),
			new PointF(x - w * 0.35f + lean * h * 0.35f + kink, y - h * 0.45f),
			new PointF(x - w * 0.55f + lean * h * 0.55f, y - h * 0.5f),
			new PointF(x + lean * h, y - h),
			new PointF(x + w * 0.45f + lean * h * 0.5f, y - h * 0.4f),
			new PointF(x + w * 0.2f + lean * h * 0.3f - kink, y - h * 0.35f),
			new PointF(x + w, y)
		};
		using (Pen pen = new Pen(Soot, Math.Max(2f, w * 0.28f)))
		{
			pen.LineJoin = LineJoin.Miter;
			graphics.DrawPolygon(pen, shape);
		}
		using (Brush brush = new SolidBrush(zap.Colour))
		{
			graphics.FillPolygon(brush, shape);
		}
		PointF[] inner = new PointF[]
		{
			new PointF(x - w * 0.4f, y),
			new PointF(x + lean * h * 0.6f, y - h * 0.6f),
			new PointF(x + w * 0.4f, y)
		};
		if (pick.Next(4) == 0)
		{
			using Brush core = new SolidBrush(Soot);
			graphics.FillPolygon(core, inner);
		}
	}

	// her red electric line from a to b, the anime way: thin and angular, a walk of short straight runs that snap to
	// 45-degree turns like a circuit trace, a faint dark-red haze, a crimson body and a pale red hot line inside;
	// little side twigs break off at some corners
	internal static void Bolt(Graphics graphics, PointF a, PointF b, float width, Color colour, float alpha, Random pick, float jag, int forks)
	{
		float dx = b.X - a.X;
		float dy = b.Y - a.Y;
		float length = (float)Math.Sqrt(dx * dx + dy * dy);
		if (length < 2f || alpha <= 0.01f)
		{
			return;
		}
		List<PointF> points = new List<PointF> { a };
		PointF at = a;
		float run = Math.Max(4f, length / (5f + jag * 18f));
		for (int guard = 0; guard < 40; guard++)
		{
			float gx = b.X - at.X;
			float gy = b.Y - at.Y;
			float left = (float)Math.Sqrt(gx * gx + gy * gy);
			if (left <= run * 1.2f)
			{
				break;
			}
			double toward = Math.Atan2(gy, gx);
			double turn = (pick.NextDouble() < 0.55) ? 0.0 : ((pick.Next(2) == 0 ? 1 : -1) * Math.PI / 4.0 * (1 + pick.Next(2)));
			double way = Math.Round((toward + turn) / (Math.PI / 4.0)) * (Math.PI / 4.0);
			float step = run * (0.5f + (float)pick.NextDouble() * 0.9f);
			at = new PointF(at.X + (float)Math.Cos(way) * step, at.Y + (float)Math.Sin(way) * step);
			points.Add(at);
		}
		points.Add(b);
		PointF[] line = points.ToArray();
		// glitch: sometimes a green copy split off to the side, and sometimes a slice of the line jumps sideways
		if (pick.Next(3) == 0)
		{
			float off = (pick.Next(2) == 0 ? 1 : -1) * (2f + width * 1.5f);
			PointF[] ghost = Array.ConvertAll(line, (PointF p) => new PointF(p.X + off, p.Y));
			Stroke(graphics, ghost, width * 1.3f, Color.FromArgb((int)(255f * alpha), Flame));
		}
		if (line.Length > 3 && pick.Next(3) == 0)
		{
			int cut = 1 + pick.Next(line.Length - 2);
			float shove = (float)((pick.NextDouble() - 0.5) * width * 10f);
			for (int i = cut; i < line.Length; i++)
			{
				line[i].X += shove;
			}
		}
		Stroke(graphics, line, width * 1.3f, Color.FromArgb((int)(255f * alpha), colour));
		for (int i = 0; i < forks && line.Length > 3; i++)
		{
			PointF start = line[1 + pick.Next(line.Length - 2)];
			double way = Math.Round((Math.Atan2(dy, dx) + (pick.Next(2) == 0 ? 1 : -1) * Math.PI / 2.0) / (Math.PI / 4.0)) * (Math.PI / 4.0);
			float reach = length * (0.12f + (float)pick.NextDouble() * 0.2f);
			Bolt(graphics, start, new PointF(start.X + (float)Math.Cos(way) * reach, start.Y + (float)Math.Sin(way) * reach), width * 0.6f, colour, alpha * 0.8f, pick, jag, 0);
		}
	}

	private static void Stroke(Graphics graphics, PointF[] line, float width, Color colour)
	{
		using Pen pen = new Pen(colour, Math.Max(0.8f, width));
		pen.LineJoin = LineJoin.Miter;
		pen.StartCap = LineCap.Flat;
		pen.EndCap = LineCap.Flat;
		graphics.DrawLines(pen, line);
	}

	private static double Smooth(double x)
	{
		x = Math.Max(0.0, Math.Min(1.0, x));
		return x * x * (3.0 - 2.0 * x);
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
