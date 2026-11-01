using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Windows.Forms;

namespace CapriDesktop;

internal enum ToastKind
{
	Info,
	Reminder,
	Focus,
	Pc,
	Battery,
	Trophy,
	Mood,
	Night,
	Game,
	Snack,
	Hide
}

// mod: one neon notice card for everything (reminders, pomodoro, PC alerts, achievements, games, mood, night).
// It glitches in from the right edge, builds its pixel icon, types its title, counts down on a block bar and folds
// away like an old CRT. Newer cards stack on top and push the others down; hovering holds one, a click closes it.
internal sealed class Toast : Overlay
{
	private const int MostShown = 4;

	private const double EnterSeconds = 0.38;

	private const double LeaveSeconds = 0.32;

	internal static readonly Color Gold = Color.FromArgb(255, 196, 64);

	internal static readonly Color Violet = Color.FromArgb(160, 96, 255);

	internal static readonly Color Moon = Color.FromArgb(128, 150, 255);

	internal static readonly Color Mint = Color.FromArgb(30, 240, 138);

	private static readonly List<Toast> shown = new List<Toast>();

	private static readonly System.Windows.Forms.Timer ticks = new System.Windows.Forms.Timer { Interval = 16 };

	private static readonly Random rolls = new Random();

	private static Rectangle area;

	// 9x9 pixel icons, one per kind, in ToastKind's order
	private static readonly string[][] Icons =
	{
		new[] { "....#....", "...###...", "...###...", "...###...", "....#....", "....#....", ".........", "...###...", "....#...." },
		new[] { "....#....", "...###...", "..#####..", "..#####..", "..#####..", ".#######.", "#########", ".........", "...###..." },
		new[] { "#########", ".#.....#.", "..#...#..", "...#.#...", "....#....", "...#.#...", "..#.#.#..", ".#.###.#.", "#########" },
		new[] { ".#.#.#.#.", "#########", "##.....##", ".#.###.#.", "##.###.##", ".#.###.#.", "##.....##", "#########", ".#.#.#.#." },
		new[] { ".........", "........", "########.", "#......##", "##.....##", "##.....##", "#......##", "########.", "........." },
		new[] { "#########", "#.#####.#", "#.#####.#", ".#######.", "..#####..", "...###...", "....#....", "...###...", "..#####.." },
		new[] { "..#####..", ".#.....#.", "#..#.#..#", "#.......#", "#.#...#.#", "#..###..#", ".#.....#.", "..#####..", "........." },
		new[] { "...###..#", "..##.....", ".##....#.", ".##......", ".##......", ".##......", "..##.....", "...###...", "........." },
		new[] { ".........", "..#####..", ".#.....#.", "#..###..#", "#..#.#..#", "#..###..#", ".#.....#.", "..#####..", "........." },
		new[] { ".######..", "########.", "##.###...", "#####....", "####.#...", "######.#.", "#.######.", "########.", ".######.." },
		new[] { "..###....", ".#...#...", "#.....#..", "#.....#..", "#.....#..", ".#...#...", "..####...", "......##.", ".......##" }
	};

	private readonly ToastKind kind;

	private readonly string title;

	private readonly string body;

	private readonly double life;

	private readonly Color colour;

	private readonly float s;

	private readonly int seed = rolls.Next();

	private readonly Stopwatch watch = Stopwatch.StartNew();

	private readonly Font titleFont;

	private readonly Font bodyFont;

	private readonly Font tagFont;

	private readonly string stamp = DateTime.Now.ToString("HH:mm");

	private double held;

	private double lastT;

	private double leavingAt = -1.0;

	private float y = float.NaN;

	private float yTo;

	private bool hot;

	private int Wide => (int)(360f * s);

	private int Tall => (int)(94f * s);

	private int Pad => (int)(12f * s);

	static Toast()
	{
		ticks.Tick += (object sender, EventArgs e) => StepAll();
	}

	private Toast(ToastKind kind, string title, string body, double seconds)
		: base("Capri notice")
	{
		this.kind = kind;
		this.title = (title ?? "").ToUpperInvariant();
		this.body = body ?? "";
		life = seconds;
		colour = Tint(kind);
		using (Graphics screen = Graphics.FromHwnd(IntPtr.Zero))
		{
			s = Math.Max(1f, screen.DpiX / 96f);
		}
		titleFont = Neon.Display(18f * s, GraphicsUnit.Pixel);
		bodyFont = Neon.Type(13f * s, bold: false, GraphicsUnit.Pixel);
		tagFont = Look.Mono(9f * s, FontStyle.Bold, GraphicsUnit.Pixel);
		Cursor = Cursors.Hand;
	}

	internal static Color Tint(ToastKind kind)
	{
		return kind switch
		{
			ToastKind.Reminder => Look.Cyan,
			ToastKind.Focus => Neon.Rose,
			ToastKind.Pc => Neon.Hot,
			ToastKind.Battery => Neon.Hot,
			ToastKind.Trophy => Gold,
			ToastKind.Mood => Violet,
			ToastKind.Night => Moon,
			ToastKind.Game => Mint,
			ToastKind.Snack => Mint,
			ToastKind.Hide => Violet,
			_ => Look.Cyan
		};
	}

	internal static void Show(ToastKind kind, string title, string body, double seconds = 6.5)
	{
		if (shown.TrueForAll((Toast each) => each.leavingAt >= 0.0))
		{
			area = Screen.FromPoint(Cursor.Position).WorkingArea;
		}
		Toast toast = new Toast(kind, title, body, seconds);
		shown.Insert(0, toast);
		int kept = 0;
		foreach (Toast each in shown.ToArray())
		{
			if (each.leavingAt < 0.0 && ++kept > MostShown)
			{
				each.Dismiss(quiet: true);
			}
		}
		Sfx.Play((kind == ToastKind.Trophy) ? Cue.Trophy : Cue.Notify);
		Arrange();
		toast.Step();
		ticks.Start();
	}

	// closes every card at once, for quitting
	internal static void ClearAll()
	{
		foreach (Toast each in shown.ToArray())
		{
			each.Close();
			each.Dispose();
		}
		shown.Clear();
		ticks.Stop();
	}

	private static void Arrange()
	{
		float top = area.Top + 14f;
		foreach (Toast each in shown)
		{
			if (each.leavingAt >= 0.0)
			{
				continue;
			}
			each.yTo = top - each.Pad;
			top += each.Tall + 10f * each.s;
		}
	}

	private static void StepAll()
	{
		foreach (Toast each in shown.ToArray())
		{
			each.Step();
		}
		if (shown.Count == 0)
		{
			ticks.Stop();
		}
	}

	private void Dismiss(bool quiet)
	{
		if (leavingAt < 0.0)
		{
			leavingAt = watch.Elapsed.TotalSeconds;
			if (!quiet)
			{
				Sfx.Play(Cue.Dismiss);
			}
			Arrange();
		}
	}

	protected override void OnMouseUp(MouseEventArgs e)
	{
		base.OnMouseUp(e);
		Dismiss(quiet: false);
	}

	private static double Ease(double k)
	{
		k = Math.Clamp(k, 0.0, 1.0);
		return 1.0 - Math.Pow(1.0 - k, 3.0);
	}

	private void Step()
	{
		if (IsDisposed)
		{
			shown.Remove(this);
			return;
		}
		double t = watch.Elapsed.TotalSeconds;
		hot = leavingAt < 0.0 && Visible && Bounds.Contains(Cursor.Position);
		if (hot)
		{
			held += t - lastT;
		}
		lastT = t;
		double age = t - held;
		if (leavingAt < 0.0 && age >= life)
		{
			Dismiss(quiet: true);
		}
		double gone = (leavingAt < 0.0) ? 0.0 : ((t - leavingAt) / LeaveSeconds);
		if (gone >= 1.0)
		{
			shown.Remove(this);
			Close();
			Dispose();
			return;
		}
		y = float.IsNaN(y) ? yTo : (y + (yTo - y) * 0.2f);
		double enter = Ease(t / EnterSeconds);
		int x = area.Right - Wide - Pad - (int)(14f * s) + (int)((1.0 - enter) * Wide * 0.55);
		Present(Compose(t, age, gone), new Point(x, (int)y));
	}

	private Bitmap Compose(double t, double age, double gone)
	{
		Bitmap face = Card(t, age);
		double glitch = (t < EnterSeconds) ? (1.0 - t / EnterSeconds) : 0.0;
		if (gone <= 0.0 && rolls.NextDouble() < 0.004)
		{
			// a rare flicker so the card feels alive
			glitch = 0.3;
		}
		if (gone > 0.0)
		{
			Bitmap folded = new Bitmap(face.Width, face.Height, PixelFormat.Format32bppPArgb);
			using (Graphics g = Graphics.FromImage(folded))
			{
				// first the card squashes to a bright line, then the line shrinks to a dot, like a CRT switching off
				double squash = Ease(gone / 0.55);
				double shrink = Ease((gone - 0.55) / 0.45);
				float h = Math.Max(2f * s, (float)(face.Height * (1.0 - squash * 0.97)));
				float w = (float)(face.Width * (1.0 - shrink));
				RectangleF to = new RectangleF((face.Width - w) / 2f, (face.Height - h) / 2f, w, h);
				if (w > 1f)
				{
					g.InterpolationMode = InterpolationMode.NearestNeighbor;
					g.DrawImage(face, to);
					using Brush flash = new SolidBrush(Color.FromArgb((int)(220.0 * squash * (1.0 - shrink * 0.6)), Color.White));
					g.FillRectangle(flash, to.X, face.Height / 2f - s, to.Width, 2f * s);
				}
			}
			face.Dispose();
			return folded;
		}
		if (glitch <= 0.01)
		{
			return face;
		}
		Bitmap sliced = new Bitmap(face.Width, face.Height, PixelFormat.Format32bppPArgb);
		using (Graphics g = Graphics.FromImage(sliced))
		{
			int band;
			for (int top = 0; top < face.Height; top += band)
			{
				band = Math.Min(face.Height - top, (int)((3 + rolls.Next(12)) * s));
				int shift = (rolls.Next(3) == 0) ? (int)((rolls.NextDouble() * 2.0 - 1.0) * 26.0 * s * glitch) : 0;
				g.DrawImage(face, new Rectangle(shift, top, face.Width, band), new Rectangle(0, top, face.Width, band), GraphicsUnit.Pixel);
			}
			using ImageAttributes ghost = new ImageAttributes();
			ghost.SetColorMatrix(new ColorMatrix(new float[5][]
			{
				new float[5],
				new float[5] { 0f, 1f, 0f, 0f, 0f },
				new float[5] { 0f, 0f, 1f, 0f, 0f },
				new float[5] { 0f, 0f, 0f, (float)(0.5 * glitch), 0f },
				new float[5] { 0f, 0f, 0f, 0f, 1f }
			}));
			int split = (int)(8f * s * glitch);
			g.DrawImage(face, new Rectangle(-split, 0, face.Width, face.Height), 0, 0, face.Width, face.Height, GraphicsUnit.Pixel, ghost);
		}
		face.Dispose();
		return sliced;
	}

	private Bitmap Card(double t, double age)
	{
		Bitmap bitmap = new Bitmap(Wide + Pad * 2, Tall + Pad * 2, PixelFormat.Format32bppPArgb);
		using Graphics g = Graphics.FromImage(bitmap);
		g.SmoothingMode = SmoothingMode.AntiAlias;
		g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
		RectangleF card = new RectangleF(Pad, Pad, Wide, Tall);
		using GraphicsPath path = Neon.Cut(card, 14f * s, 4f * s, 14f * s, 4f * s);
		using (Pen glow = new Pen(Color.FromArgb(hot ? 80 : 46, colour), 8f * s))
		{
			glow.LineJoin = LineJoin.Round;
			g.DrawPath(glow, path);
		}
		using (Brush fill = new SolidBrush(Color.FromArgb(242, 12, 11, 20)))
		{
			g.FillPath(fill, path);
		}
		using (LinearGradientBrush wash = new LinearGradientBrush(card, Color.FromArgb(52, colour), Color.FromArgb(0, colour), LinearGradientMode.Horizontal))
		{
			g.FillPath(wash, path);
		}
		g.SetClip(path);
		using (Pen scan = new Pen(Color.FromArgb(12, 255, 255, 255), Math.Max(1f, s)))
		{
			for (float row = card.Top; row < card.Bottom; row += 3f * s)
			{
				g.DrawLine(scan, card.Left, row, card.Right, row);
			}
		}
		// a shine sweeps across once as the card lands
		double sweep = (t - 0.15) / 0.55;
		if (sweep > 0.0 && sweep < 1.0)
		{
			float sx = card.Left + (float)(sweep * (card.Width + 120f * s)) - 60f * s;
			using GraphicsPath band = new GraphicsPath();
			band.AddPolygon(new PointF[]
			{
				new PointF(sx, card.Top),
				new PointF(sx + 26f * s, card.Top),
				new PointF(sx - 14f * s, card.Bottom),
				new PointF(sx - 40f * s, card.Bottom)
			});
			using Brush shine = new SolidBrush(Color.FromArgb(34, 255, 255, 255));
			g.FillPath(shine, band);
		}
		g.ResetClip();
		using (Pen edge = new Pen(Color.FromArgb(hot ? 255 : 200, colour), (hot ? 2f : 1.4f) * s))
		{
			g.DrawPath(edge, path);
		}
		using (Brush bar = new SolidBrush(colour))
		{
			g.FillRectangle(bar, card.Left + 7f * s, card.Top + 14f * s, 3f * s, card.Height - 28f * s);
		}
		DrawIcon(g, new RectangleF(card.Left + 20f * s, card.Top + 18f * s, 46f * s, 46f * s), t);
		float textX = card.Left + 80f * s;
		// the title types itself in, with a block cursor while it does
		int typed = Math.Clamp((int)((t - 0.12) / 0.022), 0, title.Length);
		string shownTitle = title.Substring(0, typed);
		using (Brush ink = new SolidBrush(colour))
		{
			g.DrawString(shownTitle, titleFont, ink, textX, card.Top + 10f * s);
			if (typed < title.Length || (int)(t * 2.4) % 2 == 0)
			{
				float w = g.MeasureString(shownTitle, titleFont, PointF.Empty, StringFormat.GenericTypographic).Width;
				g.FillRectangle(ink, textX + w + 4f * s, card.Top + 16f * s, 7f * s, 14f * s);
			}
		}
		string tag = "K.MOD // " + stamp;
		SizeF tagSize = g.MeasureString(tag, tagFont);
		using (Brush faint = new SolidBrush(Color.FromArgb(170, Neon.Faint)))
		{
			g.DrawString(tag, tagFont, faint, card.Right - tagSize.Width - 10f * s, card.Top + 6f * s);
		}
		double fade = Math.Clamp((t - 0.22) / 0.25, 0.0, 1.0);
		using (Brush text = new SolidBrush(Color.FromArgb((int)(235.0 * fade), Neon.White)))
		{
			using StringFormat wrap = new StringFormat { Trimming = StringTrimming.EllipsisWord };
			g.DrawString(body, bodyFont, text, new RectangleF(textX, card.Top + 38f * s, card.Right - textX - 14f * s, card.Height - 52f * s), wrap);
		}
		// time left, as a row of little blocks that run out from the right
		float left = textX;
		float right = card.Right - 16f * s;
		float barY = card.Bottom - 10f * s;
		double remain = Math.Clamp(1.0 - age / life, 0.0, 1.0);
		float block = 5f * s;
		float gap = 2f * s;
		int blocks = (int)((right - left) / (block + gap));
		int lit = (int)Math.Ceiling(blocks * remain);
		using (Brush on = new SolidBrush(colour))
		using (Brush off = new SolidBrush(Color.FromArgb(40, colour)))
		{
			for (int i = 0; i < blocks; i++)
			{
				g.FillRectangle((i < lit) ? on : off, left + i * (block + gap), barY, block, 3f * s);
			}
		}
		return bitmap;
	}

	private void DrawIcon(Graphics g, RectangleF box, double t)
	{
		using (GraphicsPath frame = Neon.Cut(box, 8f * s, 2f * s, 8f * s, 2f * s))
		{
			using Brush back = new SolidBrush(Color.FromArgb(34, colour));
			g.FillPath(back, frame);
			using Pen line = new Pen(Color.FromArgb(150, colour), s);
			g.DrawPath(line, frame);
		}
		string[] icon = Icons[Math.Clamp((int)kind, 0, Icons.Length - 1)];
		float cell = (float)Math.Floor(4f * s);
		float size = cell * 9f;
		float ox = box.Left + (box.Width - size) / 2f;
		float oy = box.Top + (box.Height - size) / 2f;
		Random order = new Random(seed);
		// pixels drop in one by one, then the icon breathes
		double pulse = 0.82 + 0.18 * Math.Sin(t * 5.0);
		using Brush pixel = new SolidBrush(Color.FromArgb((int)(255.0 * pulse), colour));
		using Brush spark = new SolidBrush(Color.White);
		for (int row = 0; row < 9; row++)
		{
			string line = icon[row];
			for (int col = 0; col < 9 && col < line.Length; col++)
			{
				double at = 0.1 + order.NextDouble() * 0.35;
				if (line[col] != '#' || t < at)
				{
					continue;
				}
				g.FillRectangle((t - at < 0.06) ? spark : pixel, ox + col * cell, oy + row * cell, cell - Math.Max(0.5f, s * 0.5f), cell - Math.Max(0.5f, s * 0.5f));
			}
		}
		if (kind == ToastKind.Trophy)
		{
			// a glint that runs over the cup every couple of seconds
			double glint = (t % 2.2) / 0.4;
			if (glint < 1.0)
			{
				using Brush shine = new SolidBrush(Color.FromArgb(200, Color.White));
				float gx = ox + (float)(glint * 9.0) * cell;
				g.FillRectangle(shine, gx, oy, cell, cell * 4f);
			}
		}
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			titleFont.Dispose();
			bodyFont.Dispose();
			tagFont.Dispose();
		}
		base.Dispose(disposing);
	}
}
