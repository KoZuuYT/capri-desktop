using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace CapriDesktop;

// mod: her stats as one neon page: level ring and XP bar, a speech bubble where she comments on your numbers, a tile
// per counter and every achievement. It reads her settings every frame, so a pet, a throw or a new achievement shows
// up here the moment it happens, with the number popping, "+XP" floating off it and the level bar filling.
internal sealed class StatsView : Form
{
	private const int Columns = 2;

	private const int Tiles = 3;

	private const int TileTall = 76;

	private const int TileGap = 10;

	private const int Top = 84;

	private const int BubbleY = 250;

	private const int NumbersY = 344;

	private const int TrophiesY = NumbersY + 22 + 4 * (TileTall + TileGap) + 14;

	private const int CardsY = TrophiesY + 34;

	private static StatsView open;

	private static readonly string[] Face =
	{
		"..#####..",
		".#######.",
		"#########",
		"##.###.##",
		"#########",
		"###...###",
		".#######.",
		"..#####.."
	};

	private sealed class Float
	{
		internal PointF At;

		internal string Text;

		internal Color Tone;

		internal double Born;
	}

	private sealed class Bit
	{
		internal float X;

		internal float Y;

		internal float Vx;

		internal float Vy;

		internal Color Tone;

		internal double Born;
	}

	private readonly Preferences preferences;

	private readonly Func<int, string> comment;

	private readonly Action<string> say;

	private readonly float s;

	private readonly System.Windows.Forms.Timer frames = new System.Windows.Forms.Timer { Interval = 30 };

	private readonly Stopwatch clock = Stopwatch.StartNew();

	private readonly Random random = new Random();

	private readonly Font head;

	private readonly Font title;

	private readonly Font big;

	private readonly Font number;

	private readonly Font name;

	private readonly Font body;

	private readonly Font talk;

	private readonly Font tag;

	private readonly Font small;

	private readonly int[] values = new int[Progress.All.Length];

	private readonly double[] shown = new double[Progress.All.Length];

	private readonly double[] bumped = new double[Progress.All.Length];

	private readonly double[] wonAt = new double[Trophy.All.Length];

	private readonly List<Float> floats = new List<Float>();

	private readonly List<Bit> bits = new List<Bit>();

	private HashSet<string> got;

	private int xp;

	private double shownXp;

	private int shownLevel = 1;

	private double levelAt = -10.0;

	private bool counting = true;

	private double nextTally;

	private string line = "";

	private double lineAt = -10.0;

	private bool lineDue = true;

	private double blinkAt = 2.0;

	private float scroll;

	private float scrollTo;

	private int hover = -1;

	private bool closeHot;

	private bool closing;

	private double closingAt;

	private double last;

	private Point dragFrom;

	private bool dragging;

	private int CardWide => (ClientSize.Width - S(28) * 2 - S(12)) / Columns;

	private int TileWide => (ClientSize.Width - S(28) * 2 - S(TileGap) * (Tiles - 1)) / Tiles;

	private int ContentTall => S(CardsY) + (Trophy.All.Length + Columns - 1) / Columns * S(96) + S(16);

	private int MaxScroll => Math.Max(0, ContentTall - ClientSize.Height);

	internal static void Open(IWin32Window owner, Preferences preferences, Icon icon, float scale, Func<int, string> comment, Action<string> say, bool trophies)
	{
		if (open != null && !open.IsDisposed)
		{
			open.Activate();
			if (trophies)
			{
				open.scrollTo = Math.Min(open.MaxScroll, open.S(TrophiesY - Top));
			}
			return;
		}
		open = new StatsView(preferences, icon, scale, comment, say);
		if (trophies)
		{
			open.scrollTo = open.scroll = Math.Min(open.MaxScroll, open.S(TrophiesY - Top));
		}
		if (owner != null)
		{
			open.Show(owner);
		}
		else
		{
			open.StartPosition = FormStartPosition.CenterScreen;
			open.Show();
		}
	}

	private StatsView(Preferences preferences, Icon icon, float scale, Func<int, string> comment, Action<string> say)
	{
		this.preferences = preferences;
		this.comment = comment;
		this.say = say;
		s = scale;
		Text = "Stats da Capri";
		Icon = icon;
		FormBorderStyle = FormBorderStyle.None;
		StartPosition = FormStartPosition.CenterParent;
		ShowInTaskbar = false;
		DoubleBuffered = true;
		BackColor = Neon.Ground;
		ClientSize = new Size(S(660), S(720));
		head = Look.Mono(20f * s, FontStyle.Bold, GraphicsUnit.Pixel);
		title = Neon.Display(24f * s, GraphicsUnit.Pixel);
		big = Neon.Display(46f * s, GraphicsUnit.Pixel);
		number = Neon.Display(28f * s, GraphicsUnit.Pixel);
		name = Neon.Display(17f * s, GraphicsUnit.Pixel);
		body = Neon.Type(12f * s, bold: false, GraphicsUnit.Pixel);
		talk = Neon.Type(14f * s, bold: false, GraphicsUnit.Pixel);
		tag = Look.Mono(10f * s, FontStyle.Bold, GraphicsUnit.Pixel);
		small = Look.Mono(9f * s, FontStyle.Regular, GraphicsUnit.Pixel);
		got = Trophy.Unlocked(preferences);
		for (int i = 0; i < wonAt.Length; i++)
		{
			wonAt[i] = -10.0;
		}
		for (int i = 0; i < values.Length; i++)
		{
			values[i] = Progress.All[i].Value(preferences);
			bumped[i] = -10.0;
		}
		xp = Progress.Xp(preferences);
		Opacity = 0.0;
		frames.Tick += (object sender, EventArgs e) =>
		{
			try
			{
				Step();
			}
			catch (Exception)
			{
			}
		};
		Neon.Clip(this, 16f * s, 6f * s);
	}

	private int S(float v)
	{
		return (int)Math.Round(v * s);
	}

	protected override void OnShown(EventArgs e)
	{
		base.OnShown(e);
		Sfx.Play(Cue.Arrive);
		clock.Restart();
		frames.Start();
	}

	private static float Ease(double k)
	{
		k = Math.Clamp(k, 0.0, 1.0);
		return (float)(1.0 - Math.Pow(1.0 - k, 3.0));
	}

	private void Step()
	{
		double t = clock.Elapsed.TotalSeconds;
		double dt = Math.Clamp(t - last, 0.0, 0.1);
		last = t;
		if (closing)
		{
			double k = (t - closingAt) / 0.18;
			Opacity = Math.Max(0.0, 1.0 - k);
			if (k >= 1.0)
			{
				frames.Stop();
				Close();
				return;
			}
		}
		else
		{
			Opacity = Math.Min(1.0, t / 0.22);
		}
		Watch(t);
		Count(t, dt);
		if (lineDue && t > 0.9)
		{
			lineDue = false;
			Comment(-1, t, speak: true);
		}
		if (t >= blinkAt + 0.14)
		{
			blinkAt = t + 2.0 + random.NextDouble() * 3.0;
		}
		floats.RemoveAll((Float f) => t - f.Born > 1.1);
		foreach (Bit bit in bits)
		{
			bit.X += bit.Vx * (float)dt;
			bit.Y += bit.Vy * (float)dt;
			bit.Vy += S(520) * (float)dt;
		}
		bits.RemoveAll((Bit b) => t - b.Born > 1.4);
		scroll = (Math.Abs(scrollTo - scroll) < 0.5f) ? scrollTo : (scroll + (scrollTo - scroll) * 0.35f);
		Invalidate();
	}

	// what changed in her settings since the last frame
	private void Watch(double t)
	{
		int gained = 0;
		int first = -1;
		for (int i = 0; i < values.Length; i++)
		{
			int now = Progress.All[i].Value(preferences);
			if (now == values[i])
			{
				continue;
			}
			int diff = now - values[i];
			values[i] = now;
			if (diff > 0)
			{
				bumped[i] = t;
				gained += diff * Progress.All[i].Xp;
				RectangleF tile = Tile(i);
				floats.Add(new Float
				{
					At = new PointF(tile.Left + tile.Width / 2f, tile.Top + S(10)),
					Text = "+" + diff * Progress.All[i].Xp + " XP",
					Tone = Toast.Mint,
					Born = t
				});
				if (first < 0)
				{
					first = i;
				}
			}
		}
		HashSet<string> now2 = Trophy.Unlocked(preferences);
		for (int i = 0; i < Trophy.All.Length; i++)
		{
			if (now2.Contains(Trophy.All[i].Id) && !got.Contains(Trophy.All[i].Id))
			{
				wonAt[i] = t;
			}
		}
		got = now2;
		xp = Progress.Xp(preferences);
		if (gained > 0 && !counting)
		{
			Sfx.Play(Cue.XpGain);
		}
	}

	private void Count(double t, double dt)
	{
		if (counting)
		{
			// on opening everything counts up from zero, ticking as it goes
			float k = Ease((t - 0.3) / 1.3);
			shownXp = xp * k;
			for (int i = 0; i < shown.Length; i++)
			{
				shown[i] = values[i] * Ease((t - 0.25 - i * 0.04) / 0.9);
			}
			if (t >= nextTally && t > 0.3 && k < 1f)
			{
				nextTally = t + 0.055;
				Sfx.Play(Cue.Tally);
			}
			if (t > 1.7)
			{
				counting = false;
			}
		}
		else
		{
			shownXp += (xp - shownXp) * Math.Min(1.0, dt * 5.0);
			if (Math.Abs(xp - shownXp) < 0.5)
			{
				shownXp = xp;
			}
			for (int i = 0; i < shown.Length; i++)
			{
				shown[i] += (values[i] - shown[i]) * Math.Min(1.0, dt * 8.0);
				if (Math.Abs(values[i] - shown[i]) < 0.05)
				{
					shown[i] = values[i];
				}
			}
		}
		int level = Progress.At((int)Math.Round(shownXp)).Number;
		if (level > shownLevel)
		{
			levelAt = t;
			if (counting)
			{
				Sfx.Play(Cue.Count);
			}
			else
			{
				// the fanfare is hers (Companion plays it with the notice); here it's confetti
				Burst(t);
			}
		}
		shownLevel = level;
	}

	private void Burst(double t)
	{
		PointF at = Ring();
		Color[] tones = { Toast.Mint, Toast.Gold, Neon.Cyan, Neon.Rose, Color.White };
		for (int i = 0; i < 46; i++)
		{
			double angle = random.NextDouble() * Math.PI * 2.0;
			double speed = S(140) + random.NextDouble() * S(260);
			bits.Add(new Bit
			{
				X = at.X,
				Y = at.Y,
				Vx = (float)(Math.Cos(angle) * speed),
				Vy = (float)(Math.Sin(angle) * speed) - S(160),
				Tone = tones[i % tones.Length],
				Born = t
			});
		}
	}

	private void Comment(int which, double t, bool speak)
	{
		string next = null;
		try
		{
			next = comment?.Invoke(which);
		}
		catch (Exception)
		{
		}
		if (string.IsNullOrEmpty(next))
		{
			return;
		}
		line = next;
		lineAt = t;
		Sfx.Play(Cue.Peek);
		if (speak)
		{
			try
			{
				say?.Invoke(next);
			}
			catch (Exception)
			{
			}
		}
	}

	private PointF Ring()
	{
		return new PointF(S(28 + 70), S(Top + 75) - scroll);
	}

	private RectangleF Tile(int i)
	{
		int col = i % Tiles;
		int row = i / Tiles;
		return new RectangleF(S(28) + col * (TileWide + S(TileGap)), S(NumbersY + 22) + row * S(TileTall + TileGap) - scroll, TileWide, S(TileTall));
	}

	private RectangleF Card(int i)
	{
		int col = i % Columns;
		int row = i / Columns;
		return new RectangleF(S(28) + col * (CardWide + S(12)), S(CardsY) + row * S(96) - scroll, CardWide, S(84));
	}

	private RectangleF Bubble()
	{
		return new RectangleF(S(28 + 70), S(BubbleY) - scroll, ClientSize.Width - S(28 + 70 + 28), S(76));
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		Graphics g = e.Graphics;
		g.SmoothingMode = SmoothingMode.AntiAlias;
		g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
		double t = clock.Elapsed.TotalSeconds;
		DrawLevel(g, t);
		DrawBubble(g, t);
		Label(g, "NÚMEROS", S(NumbersY) - scroll, Neon.Cyan, Ease((t - 0.2) / 0.4));
		for (int i = 0; i < Progress.All.Length; i++)
		{
			DrawTile(g, i, t);
		}
		DrawTrophies(g, t);
		foreach (Float f in floats)
		{
			double k = (t - f.Born) / 1.1;
			using Brush ink = new SolidBrush(Color.FromArgb((int)(255.0 * (1.0 - k * k)), f.Tone));
			SizeF size = g.MeasureString(f.Text, tag);
			g.DrawString(f.Text, tag, ink, f.At.X - size.Width / 2f, f.At.Y - (float)(Ease(k) * S(34)));
		}
		foreach (Bit bit in bits)
		{
			double k = (t - bit.Born) / 1.4;
			using Brush ink = new SolidBrush(Color.FromArgb((int)(255.0 * (1.0 - k)), bit.Tone));
			float cell = S(4);
			g.FillRectangle(ink, (float)Math.Round(bit.X / cell) * cell, (float)Math.Round(bit.Y / cell) * cell, cell, cell);
		}
		DrawHeader(g, t);
	}

	private void Label(Graphics g, string text, float y, Color tone, float k)
	{
		if (k <= 0f)
		{
			return;
		}
		using Brush ink = new SolidBrush(Color.FromArgb((int)(255f * k), tone));
		g.DrawString(text, tag, ink, S(26), y);
		float x = S(32) + g.MeasureString(text, tag).Width;
		using Brush rule = new SolidBrush(Color.FromArgb((int)(70f * k), tone));
		g.FillRectangle(rule, x, y + S(6), (ClientSize.Width - S(28) - x) * k, Math.Max(1f, s));
	}

	private void DrawHeader(Graphics g, double t)
	{
		using (Brush ground = new SolidBrush(Neon.Ground))
		{
			g.FillRectangle(ground, 0, 0, ClientSize.Width, S(70));
		}
		Level level = Progress.At((int)Math.Round(shownXp));
		using (Brush cyan = new SolidBrush(Neon.Cyan))
		{
			g.DrawString("STATS", head, cyan, S(26), S(16));
		}
		using (Brush faint = new SolidBrush(Neon.Faint))
		{
			g.DrawString("nível " + level.Number + " · " + level.Title.ToLowerInvariant(), tag, faint, S(118), S(26));
		}
		// a scanline running along the bottom of the header
		float width = ClientSize.Width - S(56);
		using (Brush rule = new SolidBrush(Color.FromArgb(60, Neon.Cyan)))
		{
			g.FillRectangle(rule, S(28), S(54), width * Ease(t / 0.5), Math.Max(1f, 2f * s));
		}
		float scan = (float)((t * 0.45) % 1.0);
		using (Brush lit = new SolidBrush(Color.FromArgb(200, Neon.Cyan)))
		{
			g.FillRectangle(lit, S(28) + (width - S(60)) * scan, S(54), S(60), Math.Max(1f, 2f * s));
		}
		RectangleF box = CloseBox();
		using (Pen cross = new Pen(closeHot ? Neon.Rose : Neon.Faint, 2f * s))
		{
			g.DrawLine(cross, box.Left + S(7), box.Top + S(7), box.Right - S(7), box.Bottom - S(7));
			g.DrawLine(cross, box.Right - S(7), box.Top + S(7), box.Left + S(7), box.Bottom - S(7));
		}
		if (MaxScroll > 0)
		{
			float track = ClientSize.Height - S(84);
			float thumb = Math.Max(S(30), track * ClientSize.Height / ContentTall);
			float at = S(76) + (track - thumb) * scroll / MaxScroll;
			using Brush b = new SolidBrush(Color.FromArgb(160, Neon.Cyan));
			g.FillRectangle(b, ClientSize.Width - S(12), at, 3f * s, thumb);
		}
	}

	private void DrawLevel(Graphics g, double t)
	{
		float k = Ease((t - 0.05) / 0.35);
		if (k <= 0f)
		{
			return;
		}
		RectangleF panel = new RectangleF(S(28), S(Top) - scroll + (1f - k) * S(20), ClientSize.Width - S(56), S(150));
		if (panel.Bottom < S(70))
		{
			return;
		}
		int alpha = (int)(255f * k);
		double since = t - levelAt;
		bool fresh = since < 1.2 && !counting;
		using (GraphicsPath path = Neon.Cut(panel, 14f * s, 3f * s, 14f * s, 3f * s))
		{
			int glow = fresh ? (int)(60.0 * (1.0 - since / 1.2)) : 0;
			using (Brush fill = new SolidBrush(Color.FromArgb(alpha * (12 + glow) / 255, Toast.Mint)))
			{
				g.FillPath(fill, path);
			}
			using Pen edge = new Pen(Color.FromArgb(alpha * (fresh ? 255 : 90) / 255, Toast.Mint), 1.2f * s);
			g.DrawPath(edge, path);
		}
		Level level = Progress.At((int)Math.Round(shownXp));
		PointF c = Ring();
		c.Y += (1f - k) * S(20);
		float r = S(52);
		// the ring: 24 segments that light up as the level fills, the next one blinking
		int segments = 24;
		double part = level.Part * segments;
		for (int i = 0; i < segments; i++)
		{
			float start = -90f + i * 15f + 1.8f;
			int a;
			if (i < (int)part)
			{
				a = 255;
			}
			else if (i == (int)part)
			{
				a = 60 + (int)(140.0 * (part - i) * (0.6 + 0.4 * Math.Sin(t * 8.0)));
			}
			else
			{
				a = 34;
			}
			using Pen seg = new Pen(Color.FromArgb(alpha * a / 255, Toast.Mint), 8f * s);
			g.DrawArc(seg, c.X - r, c.Y - r, r * 2f, r * 2f, start, 11.4f);
		}
		using (Pen dash = new Pen(Color.FromArgb(alpha * 110 / 255, Neon.Cyan), 1.5f * s))
		{
			dash.DashPattern = new float[2] { 2f, 4f };
			dash.DashOffset = (float)(t * 6.0);
			float r2 = r + S(11);
			g.DrawEllipse(dash, c.X - r2, c.Y - r2, r2 * 2f, r2 * 2f);
		}
		if (fresh)
		{
			// a shockwave off the ring on a level up
			float r3 = r + (float)(Ease(since / 0.8) * S(70));
			using Pen wave = new Pen(Color.FromArgb((int)(220.0 * Math.Max(0.0, 1.0 - since / 0.8)), Color.White), 3f * s);
			g.DrawEllipse(wave, c.X - r3, c.Y - r3, r3 * 2f, r3 * 2f);
		}
		using (Brush faint = new SolidBrush(Color.FromArgb(alpha, Neon.Faint)))
		{
			SizeF size = g.MeasureString("NÍVEL", tag);
			g.DrawString("NÍVEL", tag, faint, c.X - size.Width / 2f, c.Y - S(34));
		}
		string text = level.Number.ToString();
		float pop = since < 0.5 ? 1f + 0.45f * (1f - Ease(since / 0.5)) : 1f;
		GraphicsState state = g.Save();
		g.TranslateTransform(c.X, c.Y + S(6));
		g.ScaleTransform(pop, pop);
		SizeF measured = g.MeasureString(text, big);
		using (Brush ink = new SolidBrush(Color.FromArgb(alpha, fresh ? Toast.Mint : Color.White)))
		{
			g.DrawString(text, big, ink, -measured.Width / 2f, -measured.Height / 2f);
		}
		g.Restore(state);
		float x = S(28 + 150);
		float right = panel.Right - S(18);
		using (Brush gold = new SolidBrush(Color.FromArgb(alpha, Toast.Gold)))
		{
			g.DrawString(level.Title, title, gold, x, panel.Top + S(12));
		}
		using (Brush faint = new SolidBrush(Color.FromArgb(alpha, Neon.Faint)))
		{
			g.DrawString((int)Math.Round(shownXp) + " XP no total", tag, faint, x + S(2), panel.Top + S(46));
		}
		// a block bar like her notices, with a shine sweeping over the filled part
		int blocks = 24;
		float gap = S(3);
		float bw = (right - x - gap * (blocks - 1)) / blocks;
		float by = panel.Top + S(70);
		double filled = level.Part * blocks;
		double shine = (t * 14.0) % (filled + 10.0);
		for (int i = 0; i < blocks; i++)
		{
			Color tone;
			int a;
			if (i < (int)filled)
			{
				tone = Math.Abs(i - shine) < 1.2 ? Color.White : Toast.Mint;
				a = 255;
			}
			else if (i == (int)filled)
			{
				tone = Toast.Mint;
				a = 50 + (int)(170.0 * (filled - i) * (0.6 + 0.4 * Math.Sin(t * 8.0)));
			}
			else
			{
				tone = Toast.Mint;
				a = 32;
			}
			using Brush block = new SolidBrush(Color.FromArgb(alpha * a / 255, tone));
			g.FillRectangle(block, x + i * (bw + gap), by, bw, S(16));
		}
		using (Brush white = new SolidBrush(Color.FromArgb(alpha, Neon.White)))
		{
			g.DrawString(level.Into + " / " + level.Need + " XP", tag, white, x, by + S(24));
		}
		using (Brush faint = new SolidBrush(Color.FromArgb(alpha, Neon.Faint)))
		{
			string togo = "faltam " + (level.Need - level.Into) + " pro nível " + (level.Number + 1);
			SizeF size = g.MeasureString(togo, tag);
			g.DrawString(togo, tag, faint, right - size.Width, by + S(24));
			(int Level, string Name)? next = Progress.NextTitle(level.Number);
			string ahead = next.HasValue ? ("Próximo título: " + next.Value.Name + " (nível " + next.Value.Level + ")") : "Título máximo. Você é uma lenda.";
			g.DrawString(ahead, small, faint, x, by + S(44));
		}
		if (fresh)
		{
			float b = Ease(since / 0.25) * (float)Math.Min(1.0, (1.2 - since) / 0.3);
			using Brush up = new SolidBrush(Color.FromArgb((int)(255f * b), Toast.Mint));
			string banner = "SUBIU DE NÍVEL!";
			SizeF size = g.MeasureString(banner, tag);
			g.DrawString(banner, tag, up, right - size.Width, panel.Top + S(20) - (1f - b) * S(10));
		}
	}

	private void DrawBubble(Graphics g, double t)
	{
		float k = Ease((t - 0.45) / 0.35);
		if (k <= 0f)
		{
			return;
		}
		RectangleF box = Bubble();
		box.Offset((1f - k) * S(20), 0f);
		if (box.Bottom < S(70))
		{
			return;
		}
		int alpha = (int)(255f * k);
		bool hot = hover == 200;
		// her little face: blinks now and then, and her mouth moves while the line is typing
		int typed = Math.Min(line.Length, (int)((t - lineAt) * 42.0));
		bool talking = typed < line.Length && ((int)(t * 10.0) % 2 == 0);
		bool blink = t >= blinkAt && t < blinkAt + 0.14;
		float cell = S(5);
		float fx = S(28) + (S(60) - cell * 9f) / 2f;
		float fy = box.Top + (box.Height - cell * 8f) / 2f + (float)(Math.Sin(t * 3.0) * S(2));
		using (Brush pixel = new SolidBrush(Color.FromArgb(alpha, Neon.Rose)))
		{
			for (int row = 0; row < Face.Length; row++)
			{
				for (int col = 0; col < 9; col++)
				{
					bool on = Face[row][col] == '#' || (blink && row == 3) || (!talking && row == 5);
					if (on)
					{
						g.FillRectangle(pixel, fx + col * cell, fy + row * cell, cell - 1f, cell - 1f);
					}
				}
			}
		}
		using (GraphicsPath path = Neon.Cut(box, 10f * s, 3f * s, 10f * s, 3f * s))
		{
			path.AddPolygon(new PointF[3]
			{
				new PointF(box.Left, box.Top + S(26)),
				new PointF(box.Left - S(10), box.Top + S(34)),
				new PointF(box.Left, box.Top + S(42))
			});
			using (Brush fill = new SolidBrush(Color.FromArgb(alpha * (hot ? 30 : 16) / 255, Neon.Rose)))
			{
				g.FillPath(fill, path);
			}
			using Pen edge = new Pen(Color.FromArgb(alpha * (hot ? 255 : 130) / 255, Neon.Rose), (hot ? 1.6f : 1.1f) * s);
			g.DrawPath(edge, path);
		}
		string shownText = line.Substring(0, Math.Max(0, typed));
		if ((int)(t * 3.0) % 2 == 0 || typed < line.Length)
		{
			shownText += "▌";
		}
		using (Brush ink = new SolidBrush(Color.FromArgb(alpha, Neon.White)))
		{
			g.DrawString(shownText, talk, ink, new RectangleF(box.Left + S(14), box.Top + S(10), box.Width - S(28), box.Height - S(26)));
		}
		using Brush faint = new SolidBrush(Color.FromArgb(alpha * (hot ? 255 : 120) / 255, Neon.Faint));
		string hint = "clique aqui ou num número pra ela comentar";
		SizeF size = g.MeasureString(hint, small);
		g.DrawString(hint, small, faint, box.Right - size.Width - S(8), box.Bottom - size.Height - S(4));
	}

	private void DrawTile(Graphics g, int i, double t)
	{
		float k = Ease((t - 0.2 - i * 0.04) / 0.35);
		if (k <= 0f)
		{
			return;
		}
		RectangleF tile = Tile(i);
		if (tile.Bottom < S(70) || tile.Top > ClientSize.Height)
		{
			return;
		}
		tile.Offset(0f, (1f - k) * S(24));
		Stat stat = Progress.All[i];
		int alpha = (int)(255f * k);
		bool hot = hover == i;
		double since = t - bumped[i];
		float flash = since < 0.6 ? (float)(1.0 - since / 0.6) : 0f;
		using (GraphicsPath path = Neon.Cut(tile, 10f * s, 3f * s, 10f * s, 3f * s))
		{
			int fillA = (hot ? 26 : 12) + (int)(70f * flash);
			using (Brush fill = new SolidBrush(Color.FromArgb(alpha * fillA / 255, stat.Tone)))
			{
				g.FillPath(fill, path);
			}
			int edgeA = Math.Min(255, (hot ? 230 : 80) + (int)(175f * flash));
			using Pen edge = new Pen(Color.FromArgb(alpha * edgeA / 255, flash > 0f ? Color.White : stat.Tone), (hot ? 1.6f : 1.1f) * s);
			g.DrawPath(edge, path);
		}
		float cell = S(3);
		float ix = tile.Left + S(12);
		float iy = tile.Top + S(12) - (since < 0.4 ? (float)(Math.Sin(since / 0.4 * Math.PI) * S(5)) : 0f);
		using (Brush pixel = new SolidBrush(Color.FromArgb(alpha, stat.Tone)))
		{
			for (int row = 0; row < 7; row++)
			{
				for (int col = 0; col < 7; col++)
				{
					if (stat.Icon[row][col] == '#')
					{
						g.FillRectangle(pixel, ix + col * cell, iy + row * cell, cell - 0.5f, cell - 0.5f);
					}
				}
			}
		}
		using (Brush faint = new SolidBrush(Color.FromArgb(alpha, hot ? stat.Tone : Neon.Faint)))
		{
			g.DrawString(stat.Name.ToUpperInvariant(), tag, faint, tile.Left + S(40), tile.Top + S(12));
		}
		string text = ((int)Math.Round(shown[i])).ToString("N0", System.Globalization.CultureInfo.GetCultureInfo("pt-BR"));
		float pop = since < 0.45 ? 1f + 0.35f * (1f - Ease(since / 0.45)) : 1f;
		GraphicsState state = g.Save();
		g.TranslateTransform(tile.Left + S(12), tile.Top + S(46));
		g.ScaleTransform(pop, pop);
		SizeF size = g.MeasureString(text, number);
		using (Brush ink = new SolidBrush(Color.FromArgb(alpha, flash > 0f ? stat.Tone : Neon.White)))
		{
			g.DrawString(text, number, ink, -S(2), -size.Height / 2f);
		}
		g.Restore(state);
		string sub = hot ? ("rendeu " + (values[i] * stat.Xp).ToString("N0", System.Globalization.CultureInfo.GetCultureInfo("pt-BR")) + " XP") : ("+" + stat.Xp + " XP cada");
		using Brush tagInk = new SolidBrush(Color.FromArgb(alpha * (hot ? 255 : 150) / 255, hot ? Toast.Mint : Neon.Faint));
		SizeF subSize = g.MeasureString(sub, small);
		g.DrawString(sub, small, tagInk, tile.Right - subSize.Width - S(8), tile.Bottom - subSize.Height - S(6));
	}

	private void DrawTrophies(Graphics g, double t)
	{
		int earned = 0;
		foreach (Trophy trophy in Trophy.All)
		{
			if (got.Contains(trophy.Id))
			{
				earned++;
			}
		}
		float y = S(TrophiesY) - scroll;
		float k0 = Ease((t - 0.5) / 0.4);
		Label(g, "CONQUISTAS  " + earned + " / " + Trophy.All.Length, y, Toast.Gold, k0);
		float width = ClientSize.Width - S(56);
		float wipe = Ease((t - 0.6) / 0.6);
		using (Brush gold = new SolidBrush(Toast.Gold))
		{
			g.FillRectangle(gold, S(28), y + S(20), width * wipe * earned / Trophy.All.Length, 3f * s);
		}
		using (Brush dim = new SolidBrush(Color.FromArgb(50, Toast.Gold)))
		{
			float at = S(28) + width * wipe * earned / Trophy.All.Length;
			g.FillRectangle(dim, at, y + S(20), width * wipe * (Trophy.All.Length - earned) / Trophy.All.Length, 3f * s);
		}
		for (int i = 0; i < Trophy.All.Length; i++)
		{
			Trophy trophy = Trophy.All[i];
			float k = Ease((t - 0.55 - i * 0.03) / 0.3);
			if (k <= 0f)
			{
				continue;
			}
			RectangleF card = Card(i);
			if (card.Bottom < S(70) || card.Top > ClientSize.Height)
			{
				continue;
			}
			card.Offset(0f, (1f - k) * S(30));
			int alpha = (int)(255f * k);
			bool won = got.Contains(trophy.Id);
			bool hot = hover == 100 + i;
			double since = t - wonAt[i];
			float flash = since < 1.5 ? (float)(1.0 - since / 1.5) : 0f;
			Color tone = won ? Toast.Gold : Neon.Faint;
			using (GraphicsPath path = Neon.Cut(card, 12f * s, 3f * s, 12f * s, 3f * s))
			{
				int fillA = (won ? (hot ? 46 : 30) : (hot ? 26 : 14)) + (int)(110f * flash);
				using (Brush fill = new SolidBrush(Color.FromArgb(alpha * Math.Min(255, fillA) / 255, tone)))
				{
					g.FillPath(fill, path);
				}
				using Pen edge = new Pen(Color.FromArgb(alpha * (hot || flash > 0f ? 255 : (won ? 170 : 80)) / 255, flash > 0.5f ? Color.White : tone), (hot ? 1.8f : 1.1f) * s);
				g.DrawPath(edge, path);
			}
			DrawCup(g, new RectangleF(card.Left + S(12), card.Top + S(16), S(52), S(52)), won, alpha, t + i * 0.37);
			float x = card.Left + S(74);
			using (Brush titleInk = new SolidBrush(Color.FromArgb(alpha, won ? Toast.Gold : Neon.White)))
			{
				g.DrawString(trophy.Name, name, titleInk, x, card.Top + S(10));
			}
			using (Brush text = new SolidBrush(Color.FromArgb(alpha, won ? Neon.White : Neon.Faint)))
			{
				g.DrawString(trophy.Text, body, text, new RectangleF(x, card.Top + S(32), card.Right - x - S(10), S(32)));
			}
			double have = Math.Min(trophy.Need, trophy.Have(preferences));
			float barW = card.Right - x - S(60);
			float barY = card.Bottom - S(14);
			using (Brush track = new SolidBrush(Color.FromArgb(alpha * 50 / 255, tone)))
			{
				g.FillRectangle(track, x, barY, barW, 3f * s);
			}
			float fill2 = (float)(won ? 1.0 : (have / trophy.Need)) * Ease((t - 0.7 - i * 0.03) / 0.6);
			using (Brush bar = new SolidBrush(Color.FromArgb(alpha, won ? Toast.Gold : Neon.Rose)))
			{
				g.FillRectangle(bar, x, barY, barW * fill2, 3f * s);
			}
			string count = won ? "+150 XP" : ((int)have + "/" + trophy.Need);
			using Brush faint = new SolidBrush(Color.FromArgb(alpha, won ? Toast.Gold : Neon.Faint));
			g.DrawString(count, tag, faint, x + barW + S(6), barY - S(6));
		}
	}

	// a pixel trophy (with a glint) when earned, a pixel padlock when not
	private void DrawCup(Graphics g, RectangleF box, bool won, int alpha, double t)
	{
		string[] cup = { "#########", "#.#####.#", "#.#####.#", ".#######.", "..#####..", "...###...", "....#....", "...###...", "..#####.." };
		string[] padlock = { "...###...", "..#...#..", "..#...#..", "..#...#..", ".#######.", ".#######.", ".###.###.", ".#######.", ".#######." };
		string[] art = won ? cup : padlock;
		float cell = (float)Math.Floor(box.Width / 10f);
		float ox = box.Left + (box.Width - cell * 9f) / 2f;
		float oy = box.Top + (box.Height - cell * 9f) / 2f;
		using Brush pixel = new SolidBrush(Color.FromArgb(alpha, won ? Toast.Gold : Color.FromArgb(90, 80, 96)));
		using Brush shine = new SolidBrush(Color.FromArgb(alpha, Color.White));
		double glint = (t % 2.6) / 0.5;
		for (int row = 0; row < 9; row++)
		{
			for (int col = 0; col < 9; col++)
			{
				if (art[row][col] != '#')
				{
					continue;
				}
				bool lit = won && glint < 1.0 && Math.Abs(col + row * 0.5 - glint * 12.0) < 1.0;
				g.FillRectangle(lit ? shine : pixel, ox + col * cell, oy + row * cell, cell - 1f, cell - 1f);
			}
		}
	}

	private RectangleF CloseBox()
	{
		return new RectangleF(ClientSize.Width - S(52), S(14), S(28), S(28));
	}

	protected override void OnPaintBackground(PaintEventArgs e)
	{
		e.Graphics.Clear(Neon.Ground);
	}

	protected override void OnMouseWheel(MouseEventArgs e)
	{
		base.OnMouseWheel(e);
		scrollTo = Math.Max(0, Math.Min(MaxScroll, scrollTo - e.Delta * S(90) / 120));
	}

	private int Over(Point p)
	{
		if (p.Y < S(70))
		{
			return -1;
		}
		if (Bubble().Contains(p))
		{
			return 200;
		}
		for (int i = 0; i < Progress.All.Length; i++)
		{
			if (Tile(i).Contains(p))
			{
				return i;
			}
		}
		for (int i = 0; i < Trophy.All.Length; i++)
		{
			if (Card(i).Contains(p))
			{
				return 100 + i;
			}
		}
		return -1;
	}

	protected override void OnMouseMove(MouseEventArgs e)
	{
		base.OnMouseMove(e);
		if (dragging)
		{
			Location = new Point(Location.X + e.X - dragFrom.X, Location.Y + e.Y - dragFrom.Y);
			return;
		}
		bool hotNow = CloseBox().Contains(e.Location);
		int over = Over(e.Location);
		if ((over != hover && over >= 0) || (hotNow && !closeHot))
		{
			Sfx.Play(Cue.Click);
		}
		hover = over;
		closeHot = hotNow;
		Cursor = (hotNow || over == 200 || (over >= 0 && over < 100)) ? Cursors.Hand : Cursors.Default;
	}

	protected override void OnMouseLeave(EventArgs e)
	{
		base.OnMouseLeave(e);
		hover = -1;
		closeHot = false;
	}

	protected override void OnMouseDown(MouseEventArgs e)
	{
		base.OnMouseDown(e);
		double t = clock.Elapsed.TotalSeconds;
		int over = Over(e.Location);
		if (CloseBox().Contains(e.Location))
		{
			Quit();
		}
		else if (e.Y < S(70))
		{
			dragging = true;
			dragFrom = e.Location;
		}
		else if (over == 200)
		{
			Comment(-1, t, speak: true);
		}
		else if (over >= 0 && over < 100)
		{
			bumped[over] = t;
			Comment(over, t, speak: true);
		}
	}

	protected override void OnMouseUp(MouseEventArgs e)
	{
		base.OnMouseUp(e);
		dragging = false;
	}

	protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
	{
		switch (keyData)
		{
		case Keys.Escape:
			Quit();
			return true;
		case Keys.Down:
			scrollTo = Math.Min(MaxScroll, scrollTo + S(60));
			return true;
		case Keys.Up:
			scrollTo = Math.Max(0, scrollTo - S(60));
			return true;
		case Keys.Next:
			scrollTo = Math.Min(MaxScroll, scrollTo + ClientSize.Height - S(120));
			return true;
		case Keys.Prior:
			scrollTo = Math.Max(0, scrollTo - ClientSize.Height + S(120));
			return true;
		case Keys.Home:
			scrollTo = 0;
			return true;
		case Keys.End:
			scrollTo = MaxScroll;
			return true;
		default:
			return base.ProcessCmdKey(ref msg, keyData);
		}
	}

	private void Quit()
	{
		if (!closing)
		{
			closing = true;
			closingAt = clock.Elapsed.TotalSeconds;
			Sfx.Play(Cue.Leave);
		}
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			frames.Dispose();
			head.Dispose();
			title.Dispose();
			big.Dispose();
			number.Dispose();
			name.Dispose();
			body.Dispose();
			talk.Dispose();
			tag.Dispose();
			small.Dispose();
			if (open == this)
			{
				open = null;
			}
		}
		base.Dispose(disposing);
	}
}
