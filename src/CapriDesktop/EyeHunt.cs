using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Windows.Forms;

namespace CapriDesktop;

// mod: the "Pega o olho" minigame. The screen dims into a neon arena, a 3-2-1 count starts it, and for 30 seconds
// her eyes open all over the place: a click on a green one is 1 point, a red one (faster) is 3, and every 5 hits in
// a row raise the combo multiplier. Missing (a click on the arena or an eye closing on its own) breaks the combo.
// A HUD on top shows time, score, combo and the record, with a button to quit; the end shows the score counting up.
internal sealed class EyeHunt : IDisposable
{
	internal const double Seconds = 30.0;

	private const double CountIn = 2.4;

	private static readonly Color Green = Color.FromArgb(24, 255, 115);

	private static readonly Color Blue = Color.FromArgb(28, 112, 255);

	private static readonly Color Red = Color.FromArgb(255, 36, 64);

	private static readonly Color DeepRed = Color.FromArgb(200, 0, 24);

	private static readonly Color Ink = Color.FromArgb(12, 10, 16);

	// a layer the mouse goes straight through
	private sealed class Ghost : Overlay
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

	// a layer that reports clicks
	private sealed class Pad : Overlay
	{
		internal Action<Point> Pressed;

		internal Pad(string name)
			: base(name)
		{
			Cursor = Cursors.Cross;
		}

		protected override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown(e);
			if (e.Button == MouseButtons.Left)
			{
				Pressed?.Invoke(e.Location);
			}
		}
	}

	private sealed class Eye
	{
		internal Pad Layer;

		internal PointF Middle;

		internal float Width;

		internal bool Red;

		internal double Born;

		internal double Life;

		internal double HitAt = -1.0;

		internal double ShutAt = -1.0;

		internal int Points;

		internal float Tilt;
	}

	private readonly Random random = new Random();

	private readonly Stopwatch watch = Stopwatch.StartNew();

	private readonly System.Windows.Forms.Timer ticks = new System.Windows.Forms.Timer { Interval = 16 };

	private readonly List<Eye> eyes = new List<Eye>();

	private readonly Rectangle work;

	private readonly float s;

	private readonly int best;

	private readonly Action<int, int> finished;

	private readonly Pad arena;

	private readonly Pad hud;

	private readonly Ghost banner;

	private readonly Font huge;

	private readonly Font title;

	private readonly Font label;

	private readonly Font digits;

	private double nextEye;

	private double endAt = -1.0;

	private int score;

	private int streak;

	private int hits;

	private double flashAt = -10.0;

	private double missAt = -10.0;

	private int lastCount = -1;

	private bool over;

	private Rectangle quitBox;

	internal bool Running => !over;

	private double Now => watch.Elapsed.TotalSeconds;

	private int Combo => Math.Min(3, 1 + streak / 5);

	// finished(score, hits) runs once, when the game ends or is quit
	internal EyeHunt(Rectangle work, int best, Action<int, int> finished)
	{
		this.work = work;
		this.best = best;
		this.finished = finished;
		using (Graphics screen = Graphics.FromHwnd(IntPtr.Zero))
		{
			s = Math.Max(1f, screen.DpiX / 96f);
		}
		huge = Neon.Display(96f * s, GraphicsUnit.Pixel);
		title = Neon.Display(26f * s, GraphicsUnit.Pixel);
		label = Look.Mono(10f * s, FontStyle.Bold, GraphicsUnit.Pixel);
		digits = Neon.Display(30f * s, GraphicsUnit.Pixel);
		arena = new Pad("Capri eye hunt arena");
		arena.Pressed = (Point at) =>
		{
			Missed(soft: false);
			Restack();
		};
		arena.Present(ArenaFrame(), work.Location);
		hud = new Pad("Capri eye hunt score");
		hud.Cursor = Cursors.Hand;
		hud.Pressed = (Point at) =>
		{
			if (quitBox.Contains(at))
			{
				Quit();
			}
		};
		banner = new Ghost("Capri eye hunt banner");
		ticks.Tick += (object sender, EventArgs e) => Step();
		Step();
		ticks.Start();
	}

	private Bitmap ArenaFrame()
	{
		Bitmap bitmap = new Bitmap(work.Width, work.Height, PixelFormat.Format32bppPArgb);
		using Graphics g = Graphics.FromImage(bitmap);
		using (Brush dim = new SolidBrush(Color.FromArgb(70, 6, 4, 12)))
		{
			g.FillRectangle(dim, 0, 0, work.Width, work.Height);
		}
		using (Pen grid = new Pen(Color.FromArgb(26, Green), 1f))
		{
			for (float x = 0f; x < work.Width; x += 48f * s)
			{
				g.DrawLine(grid, x, 0f, x, work.Height);
			}
			for (float y = 0f; y < work.Height; y += 48f * s)
			{
				g.DrawLine(grid, 0f, y, work.Width, y);
			}
		}
		// neon corner brackets
		float arm = 60f * s;
		float m = 16f * s;
		using Pen corner = new Pen(Color.FromArgb(200, Neon.Rose), 3f * s);
		PointF[][] corners =
		{
			new[] { new PointF(m, m + arm), new PointF(m, m), new PointF(m + arm, m) },
			new[] { new PointF(work.Width - m - arm, m), new PointF(work.Width - m, m), new PointF(work.Width - m, m + arm) },
			new[] { new PointF(m, work.Height - m - arm), new PointF(m, work.Height - m), new PointF(m + arm, work.Height - m) },
			new[] { new PointF(work.Width - m - arm, work.Height - m), new PointF(work.Width - m, work.Height - m), new PointF(work.Width - m, work.Height - m - arm) }
		};
		foreach (PointF[] each in corners)
		{
			g.DrawLines(corner, each);
		}
		return bitmap;
	}

	private void Step()
	{
		if (over)
		{
			return;
		}
		double t = Now;
		double play = t - CountIn;
		if (endAt < 0.0 && play >= Seconds)
		{
			End();
		}
		if (endAt < 0.0 && play >= 0.0)
		{
			Spawn(play);
		}
		foreach (Eye eye in eyes.ToArray())
		{
			StepEye(eye, t);
		}
		DrawHud(t, play);
		DrawBanner(t, play);
		Restack();
		if (endAt >= 0.0 && t - endAt > 3.2)
		{
			Finish();
		}
	}

	// a click on the dim arena lifts it above everything else of hers, which buried the eyes under it and made them
	// unclickable; so every frame the eyes, the HUD and the banner are put back on top of it
	private void Restack()
	{
		foreach (Eye eye in eyes)
		{
			Raise(eye.Layer);
		}
		Raise(hud);
		Raise(banner);
	}

	private static void Raise(Form layer)
	{
		if (!layer.IsDisposed && layer.Visible)
		{
			// HWND_TOPMOST, without moving, sizing or activating
			Native.SetWindowPos(layer.Handle, new IntPtr(-1), 0, 0, 0, 0, 19);
		}
	}

	private void Spawn(double play)
	{
		if (play < nextEye || eyes.FindAll((Eye each) => each.HitAt < 0.0 && each.ShutAt < 0.0).Count >= 6)
		{
			return;
		}
		double k = Math.Clamp(play / Seconds, 0.0, 1.0);
		nextEye = play + (0.8 - 0.42 * k) * (0.75 + random.NextDouble() * 0.5);
		bool red = random.NextDouble() < 0.14 + 0.12 * k;
		float width = (float)(72.0 + random.NextDouble() * 40.0) * s * (red ? 0.85f : 1f);
		Eye eye = new Eye
		{
			Width = width,
			Red = red,
			Born = Now,
			Life = (1.75 - 0.75 * k) * (red ? 0.7 : 1.0),
			Tilt = (float)(random.NextDouble() * 24.0 - 12.0)
		};
		// somewhere free, away from the HUD at the top and the other eyes
		Rectangle room = Rectangle.Inflate(work, -(int)(width * 1.2f), -(int)(width * 1.2f));
		room = Rectangle.FromLTRB(room.Left, Math.Max(room.Top, work.Top + (int)(130f * s)), room.Right, room.Bottom);
		for (int attempt = 0; attempt < 12; attempt++)
		{
			eye.Middle = new PointF(room.Left + (float)random.NextDouble() * Math.Max(1, room.Width), room.Top + (float)random.NextDouble() * Math.Max(1, room.Height));
			if (eyes.TrueForAll((Eye other) => Distance(other.Middle, eye.Middle) > (other.Width + width) * 0.9f))
			{
				break;
			}
		}
		eye.Layer = new Pad("Capri eye hunt target");
		eye.Layer.Pressed = (Point at) => Hit(eye);
		eyes.Add(eye);
		Sfx.Play(Cue.EyeOpen);
	}

	private static float Distance(PointF a, PointF b)
	{
		float dx = a.X - b.X;
		float dy = a.Y - b.Y;
		return (float)Math.Sqrt(dx * dx + dy * dy);
	}

	private void Hit(Eye eye)
	{
		if (over || endAt >= 0.0 || eye.HitAt >= 0.0 || eye.ShutAt >= 0.0)
		{
			return;
		}
		eye.HitAt = Now;
		streak++;
		hits++;
		eye.Points = (eye.Red ? 3 : 1) * Combo;
		score += eye.Points;
		flashAt = Now;
		Sfx.Play(eye.Red ? Cue.RedPop : Cue.Pop);
		if (streak % 5 == 0 && Combo > 1)
		{
			Sfx.Play(Cue.PetHappy);
		}
	}

	private void Missed(bool soft)
	{
		if (over || endAt >= 0.0 || Now < CountIn)
		{
			return;
		}
		streak = 0;
		missAt = Now;
		if (!soft)
		{
			Sfx.Play(Cue.Miss);
		}
	}

	private void StepEye(Eye eye, double t)
	{
		double age = t - eye.Born;
		if (eye.HitAt >= 0.0)
		{
			double k = (t - eye.HitAt) / 0.45;
			if (k >= 1.0)
			{
				Drop(eye);
				return;
			}
			Show(eye, Burst(eye, k));
			return;
		}
		if (eye.ShutAt < 0.0 && (age >= eye.Life || endAt >= 0.0))
		{
			eye.ShutAt = t;
			if (endAt < 0.0)
			{
				Missed(soft: true);
				Sfx.Play(Cue.EyeClose);
			}
		}
		double open;
		if (eye.ShutAt >= 0.0)
		{
			double k2 = (t - eye.ShutAt) / 0.16;
			if (k2 >= 1.0)
			{
				Drop(eye);
				return;
			}
			open = 1.0 - k2;
		}
		else
		{
			open = Math.Min(1.0, age / 0.12);
		}
		// the last stretch of its life it starts to flicker, as a warning
		double left = eye.Life - age;
		double glitch = (open < 1.0) ? (1.0 - open) : ((left < 0.35 && eye.ShutAt < 0.0) ? 0.5 : 0.0);
		Show(eye, EyeFrame(eye, open, glitch, t));
	}

	private void Show(Eye eye, Bitmap frame)
	{
		Point at = new Point((int)(eye.Middle.X - frame.Width / 2f), (int)(eye.Middle.Y - frame.Height / 2f));
		eye.Layer.Present(frame, at);
	}

	private void Drop(Eye eye)
	{
		eyes.Remove(eye);
		eye.Layer.Close();
		eye.Layer.Dispose();
	}

	private Bitmap EyeFrame(Eye eye, double open, double glitch, double t)
	{
		int side = (int)(eye.Width * 1.7f);
		Bitmap bitmap = new Bitmap(side, side, PixelFormat.Format32bppPArgb);
		using (Graphics g = Graphics.FromImage(bitmap))
		{
			g.SmoothingMode = SmoothingMode.AntiAlias;
			g.TranslateTransform(side / 2f, side / 2f);
			g.RotateTransform(eye.Tilt);
			Point mouse = Cursor.Position;
			float dx = mouse.X - eye.Middle.X;
			float dy = mouse.Y - eye.Middle.Y;
			float reach = Math.Max(1f, (float)Math.Sqrt(dx * dx + dy * dy));
			PointF look = new PointF(dx / Math.Max(reach, 200f), dy / Math.Max(reach, 200f));
			DrawEye(g, eye.Width, open, look, eye.Red ? Red : Green, eye.Red ? DeepRed : Blue, glitch, t);
		}
		return bitmap;
	}

	// her eye, after the look of EyePopup: a green visor plate, a black lens with thorns, a green ring, blue iris
	// and a white pupil that follows the cursor
	internal static void DrawEye(Graphics g, float width, double open, PointF look, Color accent, Color iris, double glitch, double t)
	{
		float half = width / 2f;
		float upper = (float)(width * 0.28 * open);
		float lower = (float)(width * 0.24 * open);
		float plateX = half * 1.28f;
		float plateY = width * 0.36f * (float)Math.Max(0.15, open);
		float wobble = width * 0.02f;
		PointF[] plate =
		{
			new PointF(-plateX + wobble * (float)Math.Sin(t * 1.7), -plateY),
			new PointF(plateX * 0.95f, -plateY * 1.12f + wobble * (float)Math.Sin(t * 2.1)),
			new PointF(plateX, plateY * 0.9f),
			new PointF(-plateX * 1.05f, plateY + wobble * (float)Math.Sin(t * 1.3))
		};
		for (int i = 3; i >= 1; i--)
		{
			using Pen glow = new Pen(Color.FromArgb(26, accent), i * 6f);
			glow.LineJoin = LineJoin.Round;
			g.DrawPolygon(glow, plate);
		}
		using (Brush fill = new SolidBrush(accent))
		{
			g.FillPolygon(fill, plate);
		}
		using (Pen scan = new Pen(Color.FromArgb(30, 0, 0, 0)))
		{
			for (float y = -plateY * 1.2f; y < plateY * 1.2f; y += 3f)
			{
				g.DrawLine(scan, -plateX * 1.1f, y, plateX * 1.1f, y);
			}
		}
		using Brush ink = new SolidBrush(Ink);
		if (upper + lower < 3f)
		{
			using Pen lash = new Pen(Ink, Math.Max(3f, width * 0.04f));
			g.DrawBezier(lash, -half, 0f, -half * 0.4f, -width * 0.07f, half * 0.4f, -width * 0.07f, half * 1.05f, width * 0.02f);
			return;
		}
		using GraphicsPath lens = new GraphicsPath();
		lens.AddBezier(-half, 0f, -half * 0.45f, -upper * 1.333f, half * 0.45f, -upper * 1.333f, half, 0f);
		lens.AddBezier(half, 0f, half * 0.45f, lower * 1.333f, -half * 0.45f, lower * 1.333f, -half, 0f);
		lens.CloseFigure();
		using (GraphicsPath thorns = new GraphicsPath())
		{
			thorns.AddPolygon(new PointF[]
			{
				new PointF(half * 0.18f, -upper * 0.9f),
				new PointF(half * 0.62f, -upper * 1.65f - width * 0.05f),
				new PointF(half * 0.42f, -upper * 0.7f)
			});
			thorns.AddPolygon(new PointF[]
			{
				new PointF(-half * 0.62f, lower * 0.55f),
				new PointF(-half * 0.92f, lower * 1.6f + width * 0.05f),
				new PointF(-half * 0.38f, lower * 0.85f)
			});
			g.FillPath(ink, thorns);
		}
		g.FillPath(ink, lens);
		GraphicsState state = g.Save();
		g.SetClip(lens, CombineMode.Intersect);
		float radius = width * 0.19f;
		float cx = look.X * half * 0.32f;
		float cy = look.Y * Math.Max(upper, lower) * 0.35f;
		using (Pen ring = new Pen(accent, Math.Max(2.5f, radius * 0.12f)))
		{
			g.DrawEllipse(ring, cx - radius * 1.2f, cy - radius * 1.2f, radius * 2.4f, radius * 2.4f);
		}
		using (Brush blue = new SolidBrush(iris))
		{
			g.FillEllipse(blue, cx - radius * 0.9f, cy - radius * 0.9f, radius * 1.8f, radius * 1.8f);
		}
		float core = radius * 0.42f * (1f + 0.06f * (float)Math.Sin(t * 4.0));
		using (Brush white = new SolidBrush(Color.White))
		{
			g.FillEllipse(white, cx + look.X * radius * 0.15f - core, cy + look.Y * radius * 0.15f - core, core * 2f, core * 2f);
		}
		g.Restore(state);
		if (glitch > 0.02)
		{
			float shift = (float)(glitch * 6.0);
			using Pen cyan = new Pen(Color.FromArgb(190, 0, 229, 255), 2f);
			using Pen magenta = new Pen(Color.FromArgb(190, 255, 46, 136), 2f);
			g.TranslateTransform(-shift, 0f);
			g.DrawPath(cyan, lens);
			g.TranslateTransform(shift * 2f, 0f);
			g.DrawPath(magenta, lens);
			g.TranslateTransform(-shift, 0f);
		}
	}

	// a caught eye: a flash ring, pixels flying out and the points floating up
	private Bitmap Burst(Eye eye, double k)
	{
		int side = (int)(eye.Width * 1.7f);
		Bitmap bitmap = new Bitmap(side, side, PixelFormat.Format32bppPArgb);
		using Graphics g = Graphics.FromImage(bitmap);
		g.SmoothingMode = SmoothingMode.AntiAlias;
		g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
		float c = side / 2f;
		Color accent = eye.Red ? Red : Green;
		double fade = 1.0 - k;
		float ring = (float)(eye.Width * (0.3 + k * 0.6));
		using (Pen flash = new Pen(Color.FromArgb((int)(220.0 * fade), Color.White), (float)(6.0 * fade + 1.0) * s))
		{
			g.DrawEllipse(flash, c - ring, c - ring, ring * 2f, ring * 2f);
		}
		Random spray = new Random(eye.GetHashCode());
		float cell = Math.Max(3f, 4f * s);
		for (int i = 0; i < 22; i++)
		{
			double angle = spray.NextDouble() * Math.PI * 2.0;
			double speed = (0.35 + spray.NextDouble() * 0.5) * eye.Width;
			float x = c + (float)(Math.Cos(angle) * speed * k);
			float y = c + (float)(Math.Sin(angle) * speed * k + 40.0 * s * k * k);
			Color tone = (spray.Next(3) == 0) ? Color.White : ((spray.Next(2) == 0) ? accent : (eye.Red ? DeepRed : Blue));
			using Brush bit = new SolidBrush(Color.FromArgb((int)(255.0 * fade), tone));
			g.FillRectangle(bit, (float)Math.Round(x / cell) * cell, (float)Math.Round(y / cell) * cell, cell, cell);
		}
		string points = "+" + eye.Points;
		SizeF size = g.MeasureString(points, title);
		float py = c - size.Height / 2f - (float)(k * 34.0 * s);
		using (Brush shadow = new SolidBrush(Color.FromArgb((int)(200.0 * fade), Ink)))
		{
			g.DrawString(points, title, shadow, c - size.Width / 2f + 2f * s, py + 2f * s);
		}
		using (Brush text = new SolidBrush(Color.FromArgb((int)(255.0 * Math.Min(1.0, fade * 1.6)), eye.Red ? Toast.Gold : Color.White)))
		{
			g.DrawString(points, title, text, c - size.Width / 2f, py);
		}
		return bitmap;
	}

	private void DrawHud(double t, double play)
	{
		int w = (int)(460f * s);
		int h = (int)(92f * s);
		Bitmap bitmap = new Bitmap(w, h, PixelFormat.Format32bppPArgb);
		using (Graphics g = Graphics.FromImage(bitmap))
		{
			g.SmoothingMode = SmoothingMode.AntiAlias;
			g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
			RectangleF card = new RectangleF(6f * s, 6f * s, w - 12f * s, h - 12f * s);
			using GraphicsPath path = Neon.Cut(card, 14f * s, 4f * s, 14f * s, 4f * s);
			bool flash = t - flashAt < 0.15;
			bool miss = t - missAt < 0.25;
			Color edge = miss ? Neon.Hot : (flash ? Color.White : Green);
			using (Pen glow = new Pen(Color.FromArgb(60, edge), 8f * s))
			{
				g.DrawPath(glow, path);
			}
			using (Brush fill = new SolidBrush(Color.FromArgb(240, 10, 12, 18)))
			{
				g.FillPath(fill, path);
			}
			using (Pen line = new Pen(edge, 1.6f * s))
			{
				g.DrawPath(line, path);
			}
			using Brush faint = new SolidBrush(Neon.Faint);
			using Brush green = new SolidBrush(Green);
			using Brush white = new SolidBrush(Neon.White);
			g.DrawString("PEGA O OLHO", label, green, card.Left + 18f * s, card.Top + 8f * s);
			double left = Math.Clamp(Seconds - Math.Max(0.0, play), 0.0, Seconds);
			string time = Math.Ceiling(left).ToString("00");
			using (Brush hurry = new SolidBrush((left <= 5.0 && (int)(t * 4.0) % 2 == 0) ? Neon.Hot : Neon.White))
			{
				g.DrawString(time, digits, hurry, card.Left + 16f * s, card.Top + 24f * s);
			}
			g.DrawString("SEG", label, faint, card.Left + 64f * s, card.Top + 40f * s);
			g.DrawString("PONTOS", label, faint, card.Left + 120f * s, card.Top + 8f * s);
			g.DrawString(score.ToString(), digits, flash ? green : white, card.Left + 118f * s, card.Top + 24f * s);
			g.DrawString("COMBO", label, faint, card.Left + 214f * s, card.Top + 8f * s);
			using (Brush combo = new SolidBrush((Combo > 1) ? Toast.Gold : Neon.Faint))
			{
				g.DrawString("x" + Combo, digits, combo, card.Left + 212f * s, card.Top + 24f * s);
			}
			// combo progress: 5 little blocks
			for (int i = 0; i < 5; i++)
			{
				bool lit = Combo < 3 && i < streak % 5;
				using Brush block = new SolidBrush(lit ? Toast.Gold : Color.FromArgb(50, Toast.Gold));
				g.FillRectangle(block, card.Left + 254f * s + i * 7f * s, card.Top + 50f * s, 5f * s, 5f * s);
			}
			g.DrawString("RECORDE", label, faint, card.Left + 300f * s, card.Top + 8f * s);
			g.DrawString(Math.Max(best, score).ToString(), digits, (score > best && best > 0) ? green : white, card.Left + 298f * s, card.Top + 24f * s);
			quitBox = new Rectangle((int)(card.Right - 54f * s), (int)(card.Top + 14f * s), (int)(42f * s), (int)(42f * s));
			bool hot = quitBox.Contains(hud.PointToClient(Cursor.Position)) && hud.Visible;
			using (Pen cross = new Pen(hot ? Neon.Rose : Neon.Faint, 2f * s))
			{
				g.DrawRectangle(cross, quitBox.X + 6f * s, quitBox.Y + 6f * s, quitBox.Width - 12f * s, quitBox.Height - 12f * s);
				g.DrawLine(cross, quitBox.X + 14f * s, quitBox.Y + 14f * s, quitBox.Right - 14f * s, quitBox.Bottom - 14f * s);
				g.DrawLine(cross, quitBox.Right - 14f * s, quitBox.Y + 14f * s, quitBox.X + 14f * s, quitBox.Bottom - 14f * s);
			}
			// time bar along the bottom
			float barW = card.Width - 36f * s;
			using (Brush track = new SolidBrush(Color.FromArgb(40, Green)))
			{
				g.FillRectangle(track, card.Left + 18f * s, card.Bottom - 10f * s, barW, 3f * s);
			}
			using Brush bar = new SolidBrush((left <= 5.0) ? Neon.Hot : Green);
			g.FillRectangle(bar, card.Left + 18f * s, card.Bottom - 10f * s, (float)(barW * left / Seconds), 3f * s);
		}
		// slides down from the top edge at the start
		int drop = (int)((1.0 - Math.Min(1.0, t / 0.3)) * h);
		hud.Present(bitmap, new Point(work.Left + (work.Width - w) / 2, work.Top + (int)(10f * s) - drop));
	}

	private void DrawBanner(double t, double play)
	{
		string big = null;
		string small = null;
		Color colour = Green;
		double pop = 0.0;
		if (play < 0.0)
		{
			int count = (int)Math.Ceiling(-play / 0.6);
			double into = (-play) % 0.6;
			if (count > 3)
			{
				big = "PREPARA";
				small = "verde = 1  •  vermelho = 3  •  5 seguidos = combo";
				pop = Math.Min(1.0, t / 0.2);
			}
			else
			{
				big = count.ToString();
				small = "pega os olhos dela!";
				pop = 1.0 - into / 0.6;
				if (count != lastCount)
				{
					lastCount = count;
					Sfx.Play(Cue.Count);
				}
			}
		}
		else if (play < 0.5)
		{
			big = "JÁ!";
			colour = Toast.Gold;
			pop = 1.0 - play / 0.5;
			if (lastCount != 0)
			{
				lastCount = 0;
				Sfx.Play(Cue.Go);
			}
		}
		else if (endAt >= 0.0)
		{
			double k = t - endAt;
			int shownScore = (int)Math.Round(score * Math.Min(1.0, k / 1.0));
			big = shownScore.ToString();
			bool record = score > best;
			small = record ? "NOVO RECORDE!" : ("fim de jogo  •  recorde " + best);
			colour = (record && (int)(t * 6.0) % 2 == 0) ? Toast.Gold : Green;
			pop = Math.Max(0.0, 1.0 - k / 0.25);
		}
		if (big == null)
		{
			if (banner.Visible)
			{
				banner.Hide();
			}
			return;
		}
		int w = (int)(560f * s);
		int h = (int)(260f * s);
		Bitmap bitmap = new Bitmap(w, h, PixelFormat.Format32bppPArgb);
		using (Graphics g = Graphics.FromImage(bitmap))
		{
			g.SmoothingMode = SmoothingMode.AntiAlias;
			g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
			float scale = (float)(1.0 + 0.35 * pop);
			SizeF size = g.MeasureString(big, huge);
			GraphicsState state = g.Save();
			g.TranslateTransform(w / 2f, h * 0.42f);
			g.ScaleTransform(scale, scale);
			using (Brush cyan = new SolidBrush(Color.FromArgb(150, 0, 229, 255)))
			{
				g.DrawString(big, huge, cyan, -size.Width / 2f - 4f * s * (float)pop, -size.Height / 2f);
			}
			using (Brush magenta = new SolidBrush(Color.FromArgb(150, 255, 46, 136)))
			{
				g.DrawString(big, huge, magenta, -size.Width / 2f + 4f * s * (float)pop, -size.Height / 2f);
			}
			using (Brush main = new SolidBrush(colour))
			{
				g.DrawString(big, huge, main, -size.Width / 2f, -size.Height / 2f);
			}
			g.Restore(state);
			if (small != null)
			{
				SizeF size2 = g.MeasureString(small, label);
				using Brush back = new SolidBrush(Color.FromArgb(200, 10, 10, 16));
				g.FillRectangle(back, (w - size2.Width) / 2f - 10f * s, h * 0.78f - 4f * s, size2.Width + 20f * s, size2.Height + 8f * s);
				using Brush text = new SolidBrush(Neon.White);
				g.DrawString(small, label, text, (w - size2.Width) / 2f, h * 0.78f);
			}
		}
		banner.Present(bitmap, new Point(work.Left + (work.Width - w) / 2, work.Top + (work.Height - h) / 2));
	}

	private void End()
	{
		if (endAt < 0.0)
		{
			endAt = Now;
			Sfx.Play((score > best) ? Cue.Trophy : Cue.PetEnd);
		}
	}

	internal void Quit()
	{
		if (!over)
		{
			Sfx.Play(Cue.Dismiss);
			Finish();
		}
	}

	private void Finish()
	{
		if (over)
		{
			return;
		}
		over = true;
		ticks.Stop();
		Dispose();
		finished?.Invoke(score, hits);
	}

	public void Dispose()
	{
		over = true;
		ticks.Stop();
		ticks.Dispose();
		foreach (Eye eye in eyes.ToArray())
		{
			Drop(eye);
		}
		foreach (Form layer in new Form[] { arena, hud, banner })
		{
			if (!layer.IsDisposed)
			{
				layer.Close();
				layer.Dispose();
			}
		}
		huge.Dispose();
		title.Dispose();
		label.Dispose();
		digits.Dispose();
	}
}
