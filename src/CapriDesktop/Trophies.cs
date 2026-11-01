using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace CapriDesktop;

// mod: achievements, worked out from the counters in her settings; unlocked ids are kept in Preferences.Trophies
internal sealed class Trophy
{
	internal readonly string Id;

	internal readonly string Name;

	internal readonly string Text;

	internal readonly int Need;

	private readonly Func<Preferences, double> have;

	private Trophy(string id, string name, string text, int need, Func<Preferences, double> have)
	{
		Id = id;
		Name = name;
		Text = text;
		Need = need;
		this.have = have;
	}

	internal double Have(Preferences preferences)
	{
		try
		{
			return Math.Max(0.0, have(preferences));
		}
		catch (Exception)
		{
			return 0.0;
		}
	}

	internal static int Days(Preferences preferences)
	{
		if (preferences.FirstMet == 0)
		{
			return 0;
		}
		return (int)(DateTime.UtcNow - new DateTime(preferences.FirstMet, DateTimeKind.Utc)).TotalDays;
	}

	private static int Bits(int mask)
	{
		int count = 0;
		for (; mask != 0; mask &= mask - 1)
		{
			count++;
		}
		return count;
	}

	internal static readonly Trophy[] All =
	{
		new Trophy("pet1", "Primeiro carinho", "Faça carinho nela pela primeira vez.", 1, (Preferences p) => p.Pets),
		new Trophy("pet100", "Mão de seda", "Faça 100 carinhos nela.", 100, (Preferences p) => p.Pets),
		new Trophy("purr25", "Ronronômetro", "Faça ela ronronar 25 vezes.", 25, (Preferences p) => p.Purrs),
		new Trophy("throw10", "Arremesso livre", "Arremesse a Capri 10 vezes.", 10, (Preferences p) => p.Throws),
		new Trophy("throw100", "Catapulta humana", "Arremesse a Capri 100 vezes.", 100, (Preferences p) => p.Throws),
		new Trophy("days7", "Uma semana juntos", "Fique 7 dias com ela.", 7, (Preferences p) => Days(p)),
		new Trophy("days30", "Um mês juntos", "Fique 30 dias com ela.", 30, (Preferences p) => Days(p)),
		new Trophy("charm60", "Apegada", "Chegue ao nível Apegada no carinho.", 60, (Preferences p) => p.Charm),
		new Trophy("charm100", "Inseparáveis", "Chegue a 100% de carinho.", 100, (Preferences p) => Math.Round(p.Charm)),
		new Trophy("snack1", "Primeira mordida", "Dê um arquivo pra ela comer.", 1, (Preferences p) => p.Snacks),
		new Trophy("snack50", "Glutona digital", "Dê 50 arquivos pra ela comer.", 50, (Preferences p) => p.Snacks),
		new Trophy("eye10", "Olho vivo", "Faça 10 pontos no Pega o olho.", 10, (Preferences p) => p.EyeBest),
		new Trophy("eye40", "Olho de águia", "Faça 40 pontos no Pega o olho.", 40, (Preferences p) => p.EyeBest),
		new Trophy("found1", "Te achei!", "Encontre a Capri no esconde-esconde.", 1, (Preferences p) => p.Found),
		new Trophy("found10", "Detetive", "Encontre a Capri 10 vezes.", 10, (Preferences p) => p.Found),
		new Trophy("prank10", "Vítima favorita", "Sofra 10 travessuras dela.", 10, (Preferences p) => p.Pranks),
		new Trophy("owl", "Coruja", "Esteja com ela às 3 da manhã.", 1, (Preferences p) => p.Owl),
		new Trophy("moods", "Todas as caras", "Veja os 4 humores do dia dela.", 4, (Preferences p) => Bits(p.MoodsSeen))
	};

	internal static HashSet<string> Unlocked(Preferences preferences)
	{
		return new HashSet<string>((preferences.Trophies ?? "").Split(new char[1] { ',' }, StringSplitOptions.RemoveEmptyEntries));
	}

	// the ones newly earned since last time, which are also marked as unlocked
	internal static List<Trophy> Earn(Preferences preferences)
	{
		HashSet<string> got = Unlocked(preferences);
		List<Trophy> fresh = new List<Trophy>();
		foreach (Trophy trophy in All)
		{
			if (!got.Contains(trophy.Id) && trophy.Have(preferences) >= trophy.Need)
			{
				got.Add(trophy.Id);
				fresh.Add(trophy);
			}
		}
		if (fresh.Count > 0)
		{
			preferences.Trophies = string.Join(",", got);
		}
		return fresh;
	}
}

// mod: every achievement as a neon card, gold when earned and with a progress bar when not
internal sealed class TrophyView : Form
{
	private const int Columns = 2;

	private static TrophyView open;

	private readonly Preferences preferences;

	private readonly HashSet<string> got;

	private readonly float s;

	private readonly System.Windows.Forms.Timer frames = new System.Windows.Forms.Timer { Interval = 30 };

	private readonly Stopwatch clock = Stopwatch.StartNew();

	private readonly Font head;

	private readonly Font name;

	private readonly Font body;

	private readonly Font tag;

	private float scroll;

	private float scrollTo;

	private int hover = -1;

	private bool closeHot;

	private bool closing;

	private double closingAt;

	private Point dragFrom;

	private bool dragging;

	private int CardWide => (ClientSize.Width - S(28) * 2 - S(12)) / Columns;

	private int CardTall => S(84);

	private int ContentTall => S(84) + (Trophy.All.Length + Columns - 1) / Columns * (CardTall + S(12)) + S(16);

	private int MaxScroll => Math.Max(0, ContentTall - ClientSize.Height);

	internal static void Open(IWin32Window owner, Preferences preferences, Icon icon, float scale)
	{
		if (open != null && !open.IsDisposed)
		{
			open.Activate();
			return;
		}
		open = new TrophyView(preferences, icon, scale);
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

	private TrophyView(Preferences preferences, Icon icon, float scale)
	{
		this.preferences = preferences;
		got = Trophy.Unlocked(preferences);
		s = scale;
		Text = "Conquistas";
		Icon = icon;
		FormBorderStyle = FormBorderStyle.None;
		StartPosition = FormStartPosition.CenterParent;
		ShowInTaskbar = false;
		DoubleBuffered = true;
		BackColor = Neon.Ground;
		ClientSize = new Size(S(600), S(640));
		head = Look.Mono(20f * s, FontStyle.Bold, GraphicsUnit.Pixel);
		name = Neon.Display(17f * s, GraphicsUnit.Pixel);
		body = Neon.Type(12f * s, bold: false, GraphicsUnit.Pixel);
		tag = Look.Mono(10f * s, FontStyle.Bold, GraphicsUnit.Pixel);
		Opacity = 0.0;
		frames.Tick += (object sender, EventArgs e) => Step();
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

	private void Step()
	{
		double t = clock.Elapsed.TotalSeconds;
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
		scroll = (Math.Abs(scrollTo - scroll) < 0.5f) ? scrollTo : (scroll + (scrollTo - scroll) * 0.35f);
		Invalidate();
	}

	private static float Ease(double k)
	{
		k = Math.Clamp(k, 0.0, 1.0);
		return (float)(1.0 - Math.Pow(1.0 - k, 3.0));
	}

	private RectangleF Card(int i)
	{
		int col = i % Columns;
		int row = i / Columns;
		return new RectangleF(S(28) + col * (CardWide + S(12)), S(84) + row * (CardTall + S(12)) - scroll, CardWide, CardTall);
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		Graphics g = e.Graphics;
		g.SmoothingMode = SmoothingMode.AntiAlias;
		g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
		double t = clock.Elapsed.TotalSeconds;
		for (int i = 0; i < Trophy.All.Length; i++)
		{
			Trophy trophy = Trophy.All[i];
			float k = Ease((t - 0.12 - i * 0.035) / 0.3);
			if (k <= 0f)
			{
				continue;
			}
			RectangleF card = Card(i);
			if (card.Bottom < S(70) || card.Top > ClientSize.Height)
			{
				continue;
			}
			// cards drop in with a small bounce
			card.Offset(0f, (1f - k) * S(30));
			int alpha = (int)(255f * k);
			bool won = got.Contains(trophy.Id);
			bool hot = i == hover;
			Color tone = won ? Toast.Gold : Neon.Faint;
			using (GraphicsPath path = Neon.Cut(card, 12f * s, 3f * s, 12f * s, 3f * s))
			{
				using (Brush fill = new SolidBrush(Color.FromArgb(alpha * (won ? (hot ? 46 : 30) : (hot ? 26 : 14)) / 255, tone)))
				{
					g.FillPath(fill, path);
				}
				using Pen edge = new Pen(Color.FromArgb(alpha * (hot ? 255 : (won ? 170 : 80)) / 255, tone), (hot ? 1.8f : 1.1f) * s);
				g.DrawPath(edge, path);
			}
			DrawCup(g, new RectangleF(card.Left + S(12), card.Top + S(16), S(52), S(52)), won, alpha, t + i * 0.37);
			float x = card.Left + S(74);
			using (Brush title = new SolidBrush(Color.FromArgb(alpha, won ? Toast.Gold : Neon.White)))
			{
				g.DrawString(trophy.Name, name, title, x, card.Top + S(10));
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
			float fill2 = (float)(won ? 1.0 : (have / trophy.Need)) * Ease((t - 0.3 - i * 0.035) / 0.6);
			using (Brush bar = new SolidBrush(Color.FromArgb(alpha, won ? Toast.Gold : Neon.Rose)))
			{
				g.FillRectangle(bar, x, barY, barW * fill2, 3f * s);
			}
			string count = won ? "OK" : ((int)have + "/" + trophy.Need);
			using Brush faint = new SolidBrush(Color.FromArgb(alpha, won ? Toast.Gold : Neon.Faint));
			g.DrawString(count, tag, faint, x + barW + S(8), barY - S(6));
		}
		using (Brush ground = new SolidBrush(Neon.Ground))
		{
			g.FillRectangle(ground, 0, 0, ClientSize.Width, S(70));
		}
		int earned = 0;
		foreach (Trophy trophy in Trophy.All)
		{
			if (got.Contains(trophy.Id))
			{
				earned++;
			}
		}
		float wipe = Ease(t / 0.5);
		using (Brush gold = new SolidBrush(Toast.Gold))
		{
			g.DrawString("CONQUISTAS", head, gold, S(26), S(16));
			g.FillRectangle(gold, S(28), S(54), (ClientSize.Width - S(56)) * wipe * earned / Trophy.All.Length, 2f * s);
		}
		using (Brush dim = new SolidBrush(Color.FromArgb(60, Toast.Gold)))
		{
			g.FillRectangle(dim, S(28) + (ClientSize.Width - S(56)) * wipe * earned / Trophy.All.Length, S(54), (ClientSize.Width - S(56)) * wipe * (Trophy.All.Length - earned) / Trophy.All.Length, 2f * s);
		}
		using (Brush faint2 = new SolidBrush(Neon.Faint))
		{
			g.DrawString(earned + " / " + Trophy.All.Length + " desbloqueadas", tag, faint2, S(200), S(26));
		}
		RectangleF box = CloseBox();
		using (Pen cross = new Pen(closeHot ? Neon.Rose : Neon.Faint, 2f * s))
		{
			g.DrawLine(cross, box.Left + S(7), box.Top + S(7), box.Right - S(7), box.Bottom - S(7));
			g.DrawLine(cross, box.Right - S(7), box.Top + S(7), box.Left + S(7), box.Bottom - S(7));
		}
		if (MaxScroll > 0)
		{
			float track2 = ClientSize.Height - S(84);
			float thumb = Math.Max(S(30), track2 * ClientSize.Height / ContentTall);
			float at = S(76) + (track2 - thumb) * scroll / MaxScroll;
			using Brush b = new SolidBrush(Color.FromArgb(160, Toast.Gold));
			g.FillRectangle(b, ClientSize.Width - S(12), at, 3f * s, thumb);
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

	protected override void OnMouseMove(MouseEventArgs e)
	{
		base.OnMouseMove(e);
		if (dragging)
		{
			Location = new Point(Location.X + e.X - dragFrom.X, Location.Y + e.Y - dragFrom.Y);
			return;
		}
		bool hotNow = CloseBox().Contains(e.Location);
		int over = -1;
		if (e.Y >= S(70))
		{
			for (int i = 0; i < Trophy.All.Length; i++)
			{
				if (Card(i).Contains(e.Location))
				{
					over = i;
				}
			}
		}
		if ((over != hover && over >= 0) || (hotNow && !closeHot))
		{
			Sfx.Play(Cue.Click);
		}
		hover = over;
		closeHot = hotNow;
		Cursor = hotNow ? Cursors.Hand : Cursors.Default;
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
		if (CloseBox().Contains(e.Location))
		{
			Quit();
		}
		else if (e.Y < S(70))
		{
			dragging = true;
			dragFrom = e.Location;
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
			name.Dispose();
			body.Dispose();
			tag.Dispose();
			if (open == this)
			{
				open = null;
			}
		}
		base.Dispose(disposing);
	}
}
