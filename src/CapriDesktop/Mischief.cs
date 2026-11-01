using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace CapriDesktop;

// Mischief mode pranks: a screen glitch, a short cursor theft, a fake "you've been hacked" note, pulling or shaking a
// window, flipping the screen upside down and a graffiti tag, each with its own sound and a little smirk from her.
// All harmless: nothing is clicked, typed or changed, each one cleans up after itself in a few seconds, windows go
// back exactly where they were, and grabbing the mouse or the window back ends the prank at once.
internal sealed class Mischief : IDisposable
{
	private const double GlitchSeconds = 1.1;

	private const double NoteSeconds = 8.0;

	private const double FlipHold = 2.0;

	private const double TagSeconds = 6.0;

	private static readonly Color Pink = Color.FromArgb(255, 40, 120);

	private static readonly Color Mint = Color.FromArgb(30, 240, 138);

	private static readonly Color Cyan = Color.FromArgb(40, 230, 255);

	private static readonly Color Violet = Color.FromArgb(150, 70, 255);

	private static readonly Color Ink = Color.FromArgb(8, 6, 14);

	private readonly Random random = new Random();

	// every running animation's stop, so closing the app can end (and undo) them all
	private readonly List<Action> endings = new List<Action>();

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

	private struct Rect
	{
		public int Left;

		public int Top;

		public int Right;

		public int Bottom;
	}

	// runs one animation: step gets the seconds elapsed every tick and returns false when it is done;
	// finish always runs exactly once, also when the app closes mid-prank. The returned action stops it early.
	private Action Animate(int interval, Func<double, bool> step, Action finish)
	{
		System.Windows.Forms.Timer ticks = new System.Windows.Forms.Timer { Interval = interval };
		Stopwatch watch = Stopwatch.StartNew();
		bool over = false;
		Action end = null;
		end = () =>
		{
			if (!over)
			{
				over = true;
				ticks.Stop();
				ticks.Dispose();
				endings.Remove(end);
				try
				{
					finish?.Invoke();
				}
				catch (Exception)
				{
				}
			}
		};
		endings.Add(end);
		bool Run()
		{
			try
			{
				return step(watch.Elapsed.TotalSeconds);
			}
			catch (Exception)
			{
				return false;
			}
		}
		ticks.Tick += (object param0, EventArgs param1) =>
		{
			if (!over && !Run())
			{
				end();
			}
		};
		if (Run())
		{
			ticks.Start();
		}
		else
		{
			end();
		}
		return end;
	}

	private static void Drop(Form layer)
	{
		layer.Close();
		layer.Dispose();
	}

	private static double Ease(double k)
	{
		k = Math.Clamp(k, 0.0, 1.0);
		return k * k * (3.0 - 2.0 * k);
	}

	// overshoots a little and settles, like something springing into place
	private static double Back(double k)
	{
		k = Math.Clamp(k, 0.0, 1.0) - 1.0;
		return 1.0 + k * k * (2.7 * k + 1.7);
	}

	private static Color Fade(Color color, double alpha)
	{
		return Color.FromArgb((int)Math.Clamp(alpha * 255.0, 0.0, 255.0), color);
	}

	private Color Tint(Random rolls)
	{
		return rolls.Next(4) switch
		{
			0 => Pink,
			1 => Cyan,
			2 => Violet,
			_ => Mint,
		};
	}

	private static Bitmap Capture(Rectangle area)
	{
		Bitmap shot = null;
		try
		{
			shot = new Bitmap(area.Width, area.Height, PixelFormat.Format32bppArgb);
			using Graphics graphics = Graphics.FromImage(shot);
			graphics.CopyFromScreen(area.Location, Point.Empty, area.Size);
			return shot;
		}
		catch (Exception)
		{
			shot?.Dispose();
			return null;
		}
	}

	// her reaction to each prank: little neon horns pop up over her head, "hehe" bounces in and sparks fly
	internal void Smirk(Rectangle her)
	{
		Ghost badge = new Ghost("Capri mischief smirk");
		Size size = new Size(260, 150);
		Point at = new Point(her.Left + her.Width / 2 - size.Width / 2, her.Top - size.Height + 46);
		Rectangle work = Screen.FromRectangle(her).WorkingArea;
		at = new Point(Math.Clamp(at.X, work.Left, Math.Max(work.Left, work.Right - size.Width)), Math.Clamp(at.Y, work.Top, Math.Max(work.Top, work.Bottom - size.Height)));
		int seed = random.Next();
		Sfx.Play(Cue.Giggle);
		Animate(30, (double t) =>
		{
			if (t >= 1.9)
			{
				return false;
			}
			badge.Present(SmirkFrame(size, t, seed), at);
			return true;
		}, () => Drop(badge));
	}

	private Bitmap SmirkFrame(Size size, double t, int seed)
	{
		Bitmap frame = new Bitmap(size.Width, size.Height, PixelFormat.Format32bppArgb);
		using Graphics graphics = Graphics.FromImage(frame);
		graphics.SmoothingMode = SmoothingMode.AntiAlias;
		graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
		double fade = t < 1.5 ? 1.0 : 1.0 - (t - 1.5) / 0.4;
		float cx = size.Width / 2f;
		float baseY = size.Height - 40f;
		// sparks burst out of her head and fall
		Random rolls = new Random(seed);
		for (int i = 0; i < 14; i++)
		{
			double angle = -Math.PI * (0.1 + 0.8 * rolls.NextDouble());
			double speed = 70.0 + rolls.NextDouble() * 90.0;
			double age = t - 0.08 - rolls.NextDouble() * 0.25;
			if (age <= 0.0 || age > 1.2)
			{
				continue;
			}
			float x = cx + (float)(Math.Cos(angle) * speed * age);
			float y = baseY + (float)(Math.Sin(angle) * speed * age + 90.0 * age * age);
			float r = (float)(3.5 * (1.0 - age / 1.2)) + 1f;
			using SolidBrush spark = new SolidBrush(Fade(Tint(rolls), fade * (1.0 - age / 1.2)));
			graphics.FillEllipse(spark, x - r, y - r, r * 2f, r * 2f);
		}
		// two curved horns pop up with a springy overshoot and wiggle
		double pop = Back(t / 0.3);
		float wiggle = (float)(Math.Sin(t * 18.0) * 6.0 * Math.Exp(-t * 2.5));
		for (int side = -1; side <= 1; side += 2)
		{
			GraphicsState state = graphics.Save();
			graphics.TranslateTransform(cx + side * 34f, baseY + 4f);
			graphics.RotateTransform(side * (14f + wiggle));
			graphics.ScaleTransform((float)pop, (float)pop);
			using GraphicsPath horn = new GraphicsPath();
			horn.AddBezier(-11f, 0f, -12f, -18f, -2f, -32f, side * 8f, -40f);
			horn.AddBezier(side * 8f, -40f, 2f, -26f, 9f, -12f, 11f, 0f);
			horn.CloseFigure();
			using (Pen glow = new Pen(Fade(Pink, 0.3 * fade), 7f))
			{
				glow.LineJoin = LineJoin.Round;
				graphics.DrawPath(glow, horn);
			}
			using (SolidBrush fill = new SolidBrush(Fade(Pink, fade)))
			{
				graphics.FillPath(fill, horn);
			}
			using (Pen edge = new Pen(Fade(Ink, fade), 2f))
			{
				graphics.DrawPath(edge, horn);
			}
			graphics.Restore(state);
		}
		// "hehe" letters hop in one after another
		string word = "hehe";
		using Font font = Neon.Display(34f, GraphicsUnit.Pixel);
		float width = 0f;
		float[] widths = new float[word.Length];
		for (int i = 0; i < word.Length; i++)
		{
			widths[i] = graphics.MeasureString(word[i].ToString(), font, PointF.Empty, StringFormat.GenericTypographic).Width + 2f;
			width += widths[i];
		}
		float x0 = cx - width / 2f;
		for (int i = 0; i < word.Length; i++)
		{
			double local = t - 0.18 - i * 0.07;
			if (local > 0.0)
			{
				float hop = (float)(-Math.Abs(Math.Sin(local * 9.0)) * 14.0 * Math.Exp(-local * 2.2));
				float grow = (float)Back(local / 0.22);
				float y = 6f + hop;
				GraphicsState state = graphics.Save();
				graphics.TranslateTransform(x0 + widths[i] / 2f, y + 20f);
				graphics.ScaleTransform(grow, grow);
				string letter = word[i].ToString();
				using (SolidBrush cyan = new SolidBrush(Fade(Cyan, 0.7 * fade)))
				{
					graphics.DrawString(letter, font, cyan, -widths[i] / 2f - 2f, -20f, StringFormat.GenericTypographic);
				}
				using (SolidBrush pink = new SolidBrush(Fade(Pink, 0.8 * fade)))
				{
					graphics.DrawString(letter, font, pink, -widths[i] / 2f + 2f, -20f, StringFormat.GenericTypographic);
				}
				using (SolidBrush white = new SolidBrush(Fade(Color.White, fade)))
				{
					graphics.DrawString(letter, font, white, -widths[i] / 2f, -20f, StringFormat.GenericTypographic);
				}
				graphics.Restore(state);
			}
			x0 += widths[i];
		}
		return frame;
	}

	// tears the screen into shifted, tinted slices with an error flash in the middle
	internal void Glitch(Rectangle screen)
	{
		Bitmap shot = Capture(screen);
		if (shot == null)
		{
			return;
		}
		Ghost layer = new Ghost("Capri mischief glitch");
		int seed = random.Next();
		Sfx.Play(Cue.Crunch);
		Animate(33, (double t) =>
		{
			if (t >= GlitchSeconds)
			{
				return false;
			}
			layer.Present(GlitchFrame(shot, screen.Size, t, seed), screen.Location);
			return true;
		}, () =>
		{
			Drop(layer);
			shot.Dispose();
		});
	}

	private Bitmap GlitchFrame(Bitmap shot, Size area, double t, int seed)
	{
		Random rolls = new Random(seed + (int)(t * 30.0));
		double strength = 0.35 + 0.65 * Math.Sin(Math.PI * t / GlitchSeconds);
		Bitmap frame = new Bitmap(area.Width, area.Height, PixelFormat.Format32bppArgb);
		using Graphics graphics = Graphics.FromImage(frame);
		graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
		int slices = 5 + (int)(strength * 16.0);
		for (int i = 0; i < slices; i++)
		{
			int height = 4 + rolls.Next(Math.Max(5, area.Height / 14));
			int y = rolls.Next(Math.Max(1, area.Height - height));
			int shift = (int)((rolls.NextDouble() * 2.0 - 1.0) * 110.0 * strength);
			Rectangle source = new Rectangle(0, y, area.Width, height);
			if (rolls.Next(2) == 0)
			{
				// colour split: a pink and a cyan ghost of the slice on either side
				int split = 4 + (int)(10.0 * strength);
				graphics.DrawImage(shot, new Rectangle(shift - split, y, area.Width, height), source, GraphicsUnit.Pixel);
				using (SolidBrush pink = new SolidBrush(Color.FromArgb(120, Pink)))
				{
					graphics.FillRectangle(pink, shift - split, y, area.Width, height);
				}
				graphics.DrawImage(shot, new Rectangle(shift + split, y, area.Width, height), source, GraphicsUnit.Pixel);
				using SolidBrush cyan = new SolidBrush(Color.FromArgb(90, Cyan));
				graphics.FillRectangle(cyan, shift + split, y, area.Width, height);
			}
			graphics.DrawImage(shot, new Rectangle(shift, y, area.Width, height), source, GraphicsUnit.Pixel);
			if (rolls.Next(3) == 0)
			{
				using SolidBrush tint = new SolidBrush(Color.FromArgb(50 + rolls.Next(70), Tint(rolls)));
				graphics.FillRectangle(tint, shift, y, area.Width, height);
			}
		}
		// dead pixel blocks
		int blocks = (int)(strength * 22.0);
		for (int j = 0; j < blocks; j++)
		{
			int side = 6 + rolls.Next(28);
			using SolidBrush block = new SolidBrush(Color.FromArgb(200, rolls.Next(4) == 0 ? Ink : Tint(rolls)));
			graphics.FillRectangle(block, rolls.Next(area.Width), rolls.Next(area.Height), side * (1 + rolls.Next(4)), side);
		}
		int bars = 2 + (int)(strength * 6.0);
		for (int k = 0; k < bars; k++)
		{
			using SolidBrush bar = new SolidBrush(Color.FromArgb(210, Tint(rolls)));
			graphics.FillRectangle(bar, rolls.Next(area.Width / 2), rolls.Next(area.Height), area.Width / 4 + rolls.Next(area.Width / 2), 1 + rolls.Next(3));
		}
		// a flickering error banner while the glitch peaks
		if (t > 0.25 && t < 0.85 && rolls.Next(5) != 0)
		{
			graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
			using Font font = Neon.Display(Math.Max(28f, area.Width / 22f), GraphicsUnit.Pixel);
			string text = rolls.Next(3) == 0 ? "C4PR1.EXE PAROU" : "ERR0R // CAPRI.EXE";
			SizeF measured = graphics.MeasureString(text, font);
			float x = (area.Width - measured.Width) / 2f + rolls.Next(-12, 13);
			float y = (area.Height - measured.Height) / 2f + rolls.Next(-8, 9);
			using (SolidBrush back = new SolidBrush(Color.FromArgb(220, Ink)))
			{
				graphics.FillRectangle(back, x - 24f, y - 10f, measured.Width + 48f, measured.Height + 20f);
			}
			using (SolidBrush cyan = new SolidBrush(Color.FromArgb(200, Cyan)))
			{
				graphics.DrawString(text, font, cyan, x - 5f, y);
			}
			using (SolidBrush pink = new SolidBrush(Color.FromArgb(200, Pink)))
			{
				graphics.DrawString(text, font, pink, x + 5f, y);
			}
			using SolidBrush white = new SolidBrush(Color.White);
			graphics.DrawString(text, font, white, x, y);
		}
		return frame;
	}

	// drags the cursor over to her, spins it around her and hands it back, leaving a neon trail
	internal void Steal(Point to)
	{
		Point from = Cursor.Position;
		Rectangle bounds = Screen.FromPoint(to).Bounds;
		to = new Point(Math.Clamp(to.X, bounds.Left + 60, bounds.Right - 60), Math.Clamp(to.Y, bounds.Top + 40, bounds.Bottom - 40));
		Point lastSet = from;
		List<Point> trail = new List<Point>();
		Ghost layer = new Ghost("Capri mischief trail");
		bool giggled = false;
		bool back = false;
		Sfx.Play(Cue.Whoosh);
		Animate(15, (double t) =>
		{
			Point at = Cursor.Position;
			if (Math.Abs(at.X - lastSet.X) + Math.Abs(at.Y - lastSet.Y) > 60)
			{
				// they grabbed it back
				return false;
			}
			double x;
			double y;
			if (t < 0.8)
			{
				double k = Ease(t / 0.8);
				double wiggle = Math.Sin(t * 28.0) * 14.0 * (1.0 - k);
				x = from.X + (to.X - from.X) * k;
				y = from.Y + (to.Y - from.Y) * k + wiggle;
			}
			else if (t < 1.7)
			{
				if (!giggled)
				{
					giggled = true;
					Sfx.Play(Cue.Spark);
				}
				double angle = (t - 0.8) / 0.9 * Math.PI * 4.0;
				x = to.X + Math.Sin(angle) * 50.0;
				y = to.Y - (1.0 - Math.Cos(angle)) * 30.0;
			}
			else if (t < 2.3)
			{
				if (!back)
				{
					back = true;
					Sfx.Play(Cue.Whoosh);
				}
				double k = Ease((t - 1.7) / 0.6);
				x = to.X + (from.X - to.X) * k;
				y = to.Y + (from.Y - to.Y) * k;
			}
			else
			{
				Cursor.Position = from;
				return false;
			}
			lastSet = new Point((int)Math.Round(x), (int)Math.Round(y));
			Cursor.Position = lastSet;
			lastSet = Cursor.Position;
			trail.Add(lastSet);
			if (trail.Count > 22)
			{
				trail.RemoveAt(0);
			}
			Trail(layer, trail, t);
			return true;
		}, () => Drop(layer));
	}

	private void Trail(Ghost layer, List<Point> trail, double t)
	{
		int left = int.MaxValue;
		int top = int.MaxValue;
		int right = int.MinValue;
		int bottom = int.MinValue;
		foreach (Point point in trail)
		{
			left = Math.Min(left, point.X);
			top = Math.Min(top, point.Y);
			right = Math.Max(right, point.X);
			bottom = Math.Max(bottom, point.Y);
		}
		Rectangle box = Rectangle.FromLTRB(left - 24, top - 24, right + 24, bottom + 24);
		Bitmap frame = new Bitmap(box.Width, box.Height, PixelFormat.Format32bppArgb);
		using (Graphics graphics = Graphics.FromImage(frame))
		{
			graphics.SmoothingMode = SmoothingMode.AntiAlias;
			for (int i = 0; i < trail.Count; i++)
			{
				double age = (double)(i + 1) / trail.Count;
				float r = (float)(2.0 + 9.0 * age);
				float x = trail[i].X - box.Left;
				float y = trail[i].Y - box.Top;
				Color color = i % 2 == 0 ? Pink : Cyan;
				using (SolidBrush glow = new SolidBrush(Fade(color, 0.18 * age)))
				{
					graphics.FillEllipse(glow, x - r * 1.8f, y - r * 1.8f, r * 3.6f, r * 3.6f);
				}
				using SolidBrush dot = new SolidBrush(Fade(color, 0.75 * age));
				graphics.FillEllipse(dot, x - r / 2f, y - r / 2f, r, r);
			}
			// a tiny pink heart riding along with the cursor
			Point head = trail[trail.Count - 1];
			float hx = head.X - box.Left + 14f;
			float hy = head.Y - box.Top - 14f + (float)Math.Sin(t * 12.0) * 3f;
			using GraphicsPath heart = new GraphicsPath();
			heart.AddBezier(hx, hy + 6f, hx - 9f, hy - 2f, hx - 4f, hy - 9f, hx, hy - 3f);
			heart.AddBezier(hx, hy - 3f, hx + 4f, hy - 9f, hx + 9f, hy - 2f, hx, hy + 6f);
			using SolidBrush fill = new SolidBrush(Pink);
			graphics.FillPath(fill, heart);
		}
		layer.Present(frame, box.Location);
	}

	// pops a fake security alert somewhere on the screen; it switches on like an old monitor, types its message,
	// "copies your memes" and switches off. A click or a few seconds closes it.
	internal void Hacked(Rectangle work)
	{
		Overlay note = new Overlay("Capri hacked note");
		Size size = new Size(440, 196);
		Point at = new Point(work.Left + random.Next(Math.Max(1, work.Width - size.Width)), work.Top + random.Next(Math.Max(1, work.Height - size.Height)));
		double leaveAt = NoteSeconds - 0.4;
		double now = 0.0;
		note.MouseDown += (object param0, MouseEventArgs param1) =>
		{
			leaveAt = Math.Min(leaveAt, now);
		};
		Sfx.Play(Cue.Alarm);
		Animate(33, (double t) =>
		{
			now = t;
			if (t >= leaveAt + 0.4)
			{
				return false;
			}
			// switching on/off: a bright line that opens up to the full card, or closes back down to a dot
			double open = t < 0.3 ? Back(t / 0.3) : (t > leaveAt ? 1.0 - Ease((t - leaveAt) / 0.4) : 1.0);
			bool glitchy = t < 0.45 || t > leaveAt || random.Next(40) == 0;
			using Bitmap card = Card(size, t, glitchy);
			int height = Math.Max(3, (int)(size.Height * Math.Min(1.15, open)));
			int width = t > leaveAt + 0.25 ? Math.Max(6, (int)(size.Width * (1.0 - (t - leaveAt - 0.25) / 0.15))) : size.Width;
			Bitmap frame = new Bitmap(size.Width, Math.Max(size.Height, height), PixelFormat.Format32bppArgb);
			using (Graphics graphics = Graphics.FromImage(frame))
			{
				graphics.InterpolationMode = InterpolationMode.HighQualityBilinear;
				Rectangle target = new Rectangle((size.Width - width) / 2, (frame.Height - height) / 2, width, height);
				graphics.DrawImage(card, target);
				if (open < 0.98)
				{
					using SolidBrush flash = new SolidBrush(Fade(Color.White, (1.0 - open) * 0.8));
					graphics.FillRectangle(flash, target);
				}
			}
			Point jitter = glitchy ? new Point(random.Next(-8, 9), random.Next(-5, 6)) : Point.Empty;
			note.Present(frame, new Point(at.X + jitter.X, at.Y + jitter.Y - (frame.Height - size.Height) / 2));
			return true;
		}, () => Drop(note));
	}

	private Bitmap Card(Size size, double t, bool glitchy)
	{
		Bitmap card = new Bitmap(size.Width, size.Height, PixelFormat.Format32bppArgb);
		using Graphics graphics = Graphics.FromImage(card);
		graphics.SmoothingMode = SmoothingMode.AntiAlias;
		graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
		using (SolidBrush back = new SolidBrush(Color.FromArgb(244, Ink)))
		{
			graphics.FillRectangle(back, 0, 0, size.Width, size.Height);
		}
		// the header flashes like an alarm light
		bool flash = (int)(t * 4.0) % 2 == 0;
		using (SolidBrush header = new SolidBrush(flash ? Pink : Color.FromArgb(200, 20, 70)))
		{
			graphics.FillRectangle(header, 0, 0, size.Width, 30);
		}
		using (Pen edge = new Pen(Fade(Mint, 0.6 + 0.4 * Math.Sin(t * 8.0)), 2f))
		{
			graphics.DrawRectangle(edge, 1, 1, size.Width - 3, size.Height - 3);
		}
		using (Font font = Neon.Display(15f, GraphicsUnit.Pixel))
		using (SolidBrush ink = new SolidBrush(Ink))
		{
			graphics.DrawString(flash ? "!! ALERTA DE SEGURANÇA !!" : "   ALERTA DE SEGURANÇA", font, ink, 12f, 6f);
		}
		using (Font font2 = Neon.Display(34f, GraphicsUnit.Pixel))
		{
			float shake = glitchy ? random.Next(-3, 4) : 0f;
			using (SolidBrush pink = new SolidBrush(Fade(Pink, 0.6)))
			{
				graphics.DrawString("VOCÊ FOI HACKEADO", font2, pink, 14f + shake, 40f);
			}
			using SolidBrush mint = new SolidBrush(Mint);
			graphics.DrawString("VOCÊ FOI HACKEADO", font2, mint, 12f - shake, 40f);
		}
		using (Font font3 = Neon.Type(10f))
		using (SolidBrush light = new SolidBrush(Color.FromArgb(225, 230, 240)))
		using (SolidBrush dim = new SolidBrush(Color.FromArgb(140, 150, 170)))
		{
			// the message types itself in with a blinking block cursor
			string message = "Todos os seus arquivos agora pertencem à Capri.";
			int shown = (int)Math.Clamp((t - 0.4) / 1.3 * message.Length, 0.0, message.Length);
			string typed = message.Substring(0, shown) + (t % 0.5 < 0.25 ? "█" : "");
			graphics.DrawString(typed, font3, light, 14f, 90f);
			// fake progress bar
			double progress = Math.Clamp((t - 1.4) / 4.0, 0.0, 1.0);
			int percent = (int)(Math.Pow(progress, 0.8) * 100.0);
			string status = percent < 100 ? ("Copiando seus memes... " + percent + "%") : "Upload concluído. Brincadeira, choom ;)";
			graphics.DrawString(status, font3, dim, 14f, 116f);
			Rectangle bar = new Rectangle(16, 142, size.Width - 32, 12);
			using (Pen frame = new Pen(Fade(Mint, 0.7), 1f))
			{
				graphics.DrawRectangle(frame, bar);
			}
			int filled = (int)((bar.Width - 4) * (percent / 100.0));
			if (filled > 0)
			{
				using LinearGradientBrush fill = new LinearGradientBrush(new Rectangle(bar.X + 2, bar.Y + 2, bar.Width - 4, bar.Height - 4), Pink, Mint, LinearGradientMode.Horizontal);
				graphics.FillRectangle(fill, bar.X + 2, bar.Y + 2, filled, bar.Height - 3);
			}
			graphics.DrawString("clique para fechar", font3, dim, size.Width - 150f, 166f);
		}
		// scanlines rolling down
		using (Pen scan = new Pen(Color.FromArgb(26, 255, 255, 255)))
		{
			int offset = (int)(t * 40.0) % 4;
			for (int y = 30 + offset; y < size.Height; y += 4)
			{
				graphics.DrawLine(scan, 0, y, size.Width, y);
			}
		}
		if (glitchy)
		{
			for (int i = 0; i < 5; i++)
			{
				int height = 3 + random.Next(12);
				int y = random.Next(size.Height - height);
				int shift = random.Next(-24, 25);
				using Bitmap slice = card.Clone(new Rectangle(0, y, size.Width, height), card.PixelFormat);
				graphics.DrawImage(slice, shift, y);
				using SolidBrush tint = new SolidBrush(Color.FromArgb(90, Tint(random)));
				graphics.FillRectangle(tint, 0, y, size.Width, height);
			}
		}
		return card;
	}

	// the frontmost ordinary window that isn't hers: visible, titled, not minimized, maximized, full screen or a shell surface
	private static IntPtr Victim()
	{
		int self = Environment.ProcessId;
		for (IntPtr window = GetTopWindow(IntPtr.Zero); window != IntPtr.Zero; window = Native.GetWindow(window, 2))
		{
			if (!IsWindowVisible(window) || IsIconic(window) || IsZoomed(window) || GetWindowTextLength(window) == 0)
			{
				continue;
			}
			Native.GetWindowThreadProcessId(window, out var process);
			if (process == self || (Native.GetWindowLong(window, -20) & 0x80) != 0)
			{
				continue;
			}
			if (DwmGetWindowAttribute(window, 14, out var cloaked, 4) == 0 && cloaked != 0)
			{
				continue;
			}
			StringBuilder name = new StringBuilder(64);
			GetClassName(window, name, name.Capacity);
			string kind = name.ToString();
			if (kind == "Progman" || kind == "WorkerW" || kind == "Shell_TrayWnd" || kind == "Shell_SecondaryTrayWnd" || kind == "Windows.UI.Core.CoreWindow")
			{
				continue;
			}
			if (!GetWindowRect(window, out var rect))
			{
				continue;
			}
			Rectangle bounds = Rectangle.FromLTRB(rect.Left, rect.Top, rect.Right, rect.Bottom);
			if (bounds.Width < 160 || bounds.Height < 100 || bounds.Contains(Screen.FromHandle(window).Bounds))
			{
				continue;
			}
			return window;
		}
		return IntPtr.Zero;
	}

	private static bool Where(IntPtr window, out Point at, out Size size)
	{
		at = Point.Empty;
		size = Size.Empty;
		if (!IsWindow(window) || !GetWindowRect(window, out var rect))
		{
			return false;
		}
		at = new Point(rect.Left, rect.Top);
		size = new Size(rect.Right - rect.Left, rect.Bottom - rect.Top);
		return true;
	}

	private static Point Move(IntPtr window, Point to)
	{
		// no size, no z-order, no activation
		Native.SetWindowPos(window, IntPtr.Zero, to.X, to.Y, 0, 0, 0x15);
		Where(window, out var at, out _);
		return at;
	}

	// she lassoes the frontmost window with a neon cord, tugs it toward her, wobbles it and lets it spring back.
	// False when there is no window she can grab.
	internal bool Yank(Point her)
	{
		IntPtr window = Victim();
		if (window == IntPtr.Zero || !Where(window, out var from, out var size))
		{
			return false;
		}
		Rectangle work = Screen.FromHandle(window).WorkingArea;
		double dx = her.X - (from.X + size.Width / 2.0);
		double dy = her.Y - (from.Y + size.Height / 2.0);
		double length = Math.Sqrt(dx * dx + dy * dy);
		if (length < 1.0)
		{
			dx = 1.0;
			length = 1.0;
		}
		double reach = Math.Clamp(length * 0.5, 120.0, 340.0);
		Point to = new Point((int)(from.X + dx / length * reach), (int)(from.Y + dy / length * reach));
		to = new Point(Math.Clamp(to.X, work.Left - size.Width / 2, Math.Max(work.Left, work.Right - size.Width / 2)), Math.Clamp(to.Y, work.Top, Math.Max(work.Top, work.Bottom - 80)));
		Point last = from;
		bool lost = false;
		bool sprung = false;
		Ghost rope = new Ghost("Capri mischief rope");
		Sfx.Play(Cue.Zap);
		Animate(15, (double t) =>
		{
			if (!Where(window, out var now, out _))
			{
				lost = true;
				return false;
			}
			if (Math.Abs(now.X - last.X) + Math.Abs(now.Y - last.Y) > 40)
			{
				// they grabbed the window themselves: let go and leave it where they put it
				lost = true;
				return false;
			}
			double x;
			double y;
			if (t < 0.9)
			{
				// three tugs, then it slides over
				double p = t / 0.9;
				double k = Ease(p) + Math.Sin(p * Math.PI * 6.0) * 0.07 * (1.0 - p);
				x = from.X + (to.X - from.X) * k;
				y = from.Y + (to.Y - from.Y) * k;
			}
			else if (t < 1.5)
			{
				double w = (t - 0.9) / 0.6;
				x = to.X + Math.Sin(w * 40.0) * 12.0 * (1.0 - w);
				y = to.Y + Math.Cos(w * 31.0) * 5.0 * (1.0 - w);
			}
			else if (t < 2.3)
			{
				if (!sprung)
				{
					sprung = true;
					Sfx.Play(Cue.Boing);
				}
				double k = Back((t - 1.5) / 0.8);
				x = to.X + (from.X - to.X) * k;
				y = to.Y + (from.Y - to.Y) * k;
			}
			else
			{
				return false;
			}
			Point want = new Point((int)Math.Round(x), (int)Math.Round(y));
			last = Move(window, want);
			if (Math.Abs(last.X - want.X) + Math.Abs(last.Y - want.Y) > 20)
			{
				// it won't move (an admin window, say): give up quietly
				lost = true;
				return false;
			}
			if (t < 1.75)
			{
				Rope(rope, her, new Rectangle(last, size), t);
			}
			else if (rope.Visible)
			{
				rope.Hide();
			}
			return true;
		}, () =>
		{
			Drop(rope);
			if (!lost && IsWindow(window))
			{
				Move(window, from);
			}
		});
		return true;
	}

	private void Rope(Ghost layer, Point her, Rectangle window, double t)
	{
		Point hook = new Point(Math.Clamp(her.X, window.Left + 12, window.Right - 12), Math.Clamp(her.Y, window.Top + 12, window.Bottom - 12));
		Rectangle box = Rectangle.FromLTRB(Math.Min(her.X, hook.X) - 40, Math.Min(her.Y, hook.Y) - 60, Math.Max(her.X, hook.X) + 40, Math.Max(her.Y, hook.Y) + 60);
		if (box.Width > 4000 || box.Height > 4000)
		{
			return;
		}
		double fade = t < 1.5 ? 1.0 : 1.0 - (t - 1.5) / 0.25;
		Bitmap frame = new Bitmap(box.Width, box.Height, PixelFormat.Format32bppArgb);
		using (Graphics graphics = Graphics.FromImage(frame))
		{
			graphics.SmoothingMode = SmoothingMode.AntiAlias;
			PointF a = new PointF(her.X - box.Left, her.Y - box.Top);
			PointF b = new PointF(hook.X - box.Left, hook.Y - box.Top);
			// the cord sags while slack and wobbles while she pulls
			float sag = (float)(28.0 * (1.0 - Math.Min(1.0, t / 0.4)) + Math.Sin(t * 30.0) * 6.0);
			PointF c1 = new PointF(a.X + (b.X - a.X) / 3f, a.Y + (b.Y - a.Y) / 3f + sag);
			PointF c2 = new PointF(a.X + (b.X - a.X) * 2f / 3f, a.Y + (b.Y - a.Y) * 2f / 3f - sag * 0.6f);
			for (int i = 3; i >= 1; i--)
			{
				using Pen glow = new Pen(Fade(Pink, 0.14 * fade), i * 5f);
				glow.StartCap = LineCap.Round;
				glow.EndCap = LineCap.Round;
				graphics.DrawBezier(glow, a, c1, c2, b);
			}
			using (Pen core = new Pen(Fade(Mint, fade), 3f))
			{
				core.DashPattern = new float[2] { 3f, 1.2f };
				core.DashOffset = (float)(t * 30.0);
				graphics.DrawBezier(core, a, c1, c2, b);
			}
			using SolidBrush hookFill = new SolidBrush(Fade(Pink, fade));
			graphics.FillEllipse(hookFill, b.X - 7f, b.Y - 7f, 14f, 14f);
			using Pen ring = new Pen(Fade(Color.White, fade), 2f);
			graphics.DrawEllipse(ring, b.X - 10f, b.Y - 10f, 20f, 20f);
		}
		layer.Present(frame, box.Location);
	}

	// shakes the frontmost window like an earthquake and puts it back. False when there is no window to shake.
	internal bool Shake()
	{
		IntPtr window = Victim();
		if (window == IntPtr.Zero || !Where(window, out var from, out _))
		{
			return false;
		}
		Point last = from;
		bool lost = false;
		Sfx.Play(Cue.Rumble);
		Animate(15, (double t) =>
		{
			if (t >= 1.3)
			{
				return false;
			}
			if (!Where(window, out var now, out _) || Math.Abs(now.X - last.X) + Math.Abs(now.Y - last.Y) > 40)
			{
				lost = true;
				return false;
			}
			double fall = 1.0 - t / 1.3;
			Point want = new Point(from.X + (int)(Math.Sin(t * 55.0) * 18.0 * fall), from.Y + (int)(Math.Cos(t * 43.0) * 7.0 * fall));
			last = Move(window, want);
			if (Math.Abs(last.X - want.X) + Math.Abs(last.Y - want.Y) > 20)
			{
				lost = true;
				return false;
			}
			return true;
		}, () =>
		{
			if (!lost && IsWindow(window))
			{
				Move(window, from);
			}
		});
		return true;
	}

	// flips the whole screen upside down like a card, holds it a moment and flips it back; a click flips back sooner
	internal void Flip(Rectangle screen)
	{
		Bitmap shot = Capture(screen);
		if (shot == null)
		{
			return;
		}
		Bitmap flipped = (Bitmap)shot.Clone();
		flipped.RotateFlip(RotateFlipType.Rotate180FlipNone);
		Overlay layer = new Overlay("Capri mischief flip");
		double holdUntil = 0.56 + FlipHold;
		double now = 0.0;
		bool steady = false;
		bool turned = false;
		layer.MouseDown += (object param0, MouseEventArgs param1) =>
		{
			holdUntil = Math.Min(holdUntil, Math.Max(0.56, now));
		};
		Sfx.Play(Cue.Whoosh);
		Animate(30, (double t) =>
		{
			now = t;
			if (t >= holdUntil + 0.56)
			{
				return false;
			}
			if (t >= 0.56 && t < holdUntil)
			{
				if (!steady)
				{
					steady = true;
					layer.Present(FlipFrame(flipped, screen.Size, 1.0), screen.Location);
				}
				return true;
			}
			if (t >= holdUntil && !turned)
			{
				turned = true;
				Sfx.Play(Cue.Whoosh);
			}
			double local = t < holdUntil ? t : t - holdUntil;
			Bitmap face = (local < 0.28) == (t < holdUntil) ? shot : flipped;
			double squeeze = local < 0.28 ? 1.0 - Ease(local / 0.28) : Back((local - 0.28) / 0.28);
			layer.Present(FlipFrame(face, screen.Size, squeeze), screen.Location);
			return true;
		}, () =>
		{
			Drop(layer);
			shot.Dispose();
			flipped.Dispose();
		});
	}

	private static Bitmap FlipFrame(Bitmap face, Size area, double squeeze)
	{
		Bitmap frame = new Bitmap(area.Width, area.Height, PixelFormat.Format32bppArgb);
		using Graphics graphics = Graphics.FromImage(frame);
		graphics.Clear(Ink);
		graphics.InterpolationMode = squeeze >= 0.999 && squeeze <= 1.001 ? InterpolationMode.NearestNeighbor : InterpolationMode.Bilinear;
		int height = Math.Max(2, (int)(area.Height * squeeze));
		// a touch of perspective: the card narrows as it turns edge-on
		int width = (int)(area.Width * (1.0 - 0.1 * Math.Max(0.0, 1.0 - squeeze)));
		Rectangle target = new Rectangle((area.Width - width) / 2, (area.Height - height) / 2, width, height);
		graphics.DrawImage(face, target);
		if (squeeze < 0.999 || squeeze > 1.001)
		{
			using Pen edge = new Pen(Fade(Mint, 0.9), 3f);
			graphics.DrawLine(edge, target.Left, target.Top, target.Right, target.Top);
			graphics.DrawLine(edge, target.Left, target.Bottom, target.Right, target.Bottom);
		}
		return frame;
	}

	// sprays "CAPRI ESTEVE AQUI" across the screen, letter by letter with drips; it fades after a few seconds or on a click
	internal void Graffiti(Rectangle work)
	{
		Overlay layer = new Overlay("Capri mischief graffiti");
		Size size = new Size(Math.Min(720, work.Width - 40), 260);
		Point at = new Point(work.Left + 20 + random.Next(Math.Max(1, work.Width - size.Width - 40)), work.Top + 20 + random.Next(Math.Max(1, work.Height - size.Height - 40)));
		float tilt = (float)(random.NextDouble() * 12.0 - 6.0);
		int seed = random.Next();
		double leaveAt = TagSeconds - 0.6;
		double now = 0.0;
		layer.MouseDown += (object param0, MouseEventArgs param1) =>
		{
			leaveAt = Math.Min(leaveAt, now);
		};
		Sfx.Play(Cue.Spray);
		Animate(33, (double t) =>
		{
			now = t;
			if (t >= leaveAt + 0.6)
			{
				return false;
			}
			double fade = t < leaveAt ? 1.0 : 1.0 - (t - leaveAt) / 0.6;
			layer.Present(Tag(size, t, fade, tilt, seed), at);
			return true;
		}, () => Drop(layer));
	}

	private static Bitmap Tag(Size size, double t, double fade, float tilt, int seed)
	{
		Bitmap frame = new Bitmap(size.Width, size.Height, PixelFormat.Format32bppArgb);
		using Graphics graphics = Graphics.FromImage(frame);
		graphics.SmoothingMode = SmoothingMode.AntiAlias;
		graphics.TextRenderingHint = TextRenderingHint.AntiAlias;
		graphics.TranslateTransform(size.Width / 2f, size.Height / 2f);
		graphics.RotateTransform(tilt);
		string text = "CAPRI ESTEVE AQUI";
		using Font font = Neon.Display(Math.Min(72f, size.Width / 9.5f), GraphicsUnit.Pixel);
		SizeF measured = graphics.MeasureString(text, font);
		float left = -measured.Width / 2f;
		float top = -measured.Height / 2f - 14f;
		// the can sweeps left to right over 1.4 s
		double reveal = Math.Clamp(t / 1.4, 0.0, 1.0);
		float edge = left + measured.Width * (float)reveal;
		GraphicsState state = graphics.Save();
		graphics.SetClip(new RectangleF(left - 10f, top - 40f, edge - left + 10f, measured.Height + 140f));
		// soft overspray, a dark outline and the pink fill
		for (int i = 0; i < 6; i++)
		{
			float ox = (float)Math.Cos(i * 1.047) * 5f;
			float oy = (float)Math.Sin(i * 1.047) * 5f;
			using SolidBrush halo = new SolidBrush(Fade(Pink, 0.12 * fade));
			graphics.DrawString(text, font, halo, left + ox, top + oy);
		}
		using (SolidBrush outline = new SolidBrush(Fade(Ink, 0.85 * fade)))
		{
			graphics.DrawString(text, font, outline, left + 3f, top + 3f);
		}
		using (SolidBrush fill = new SolidBrush(Fade(Pink, fade)))
		{
			graphics.DrawString(text, font, fill, left, top);
		}
		using (SolidBrush shine = new SolidBrush(Fade(Color.White, 0.35 * fade)))
		{
			graphics.DrawString(text, font, shine, left - 1.5f, top - 1.5f);
		}
		// drips run down from under the letters once the can has passed them
		Random rolls = new Random(seed);
		float bottom = top + measured.Height * 0.8f;
		for (int j = 0; j < 12; j++)
		{
			float x = left + (float)rolls.NextDouble() * measured.Width;
			float most = 14f + (float)rolls.NextDouble() * 50f;
			double since = t - (x - left) / measured.Width * 1.4 - 0.1;
			if (since <= 0.0)
			{
				continue;
			}
			float length = most * (float)Ease(since / (0.8 + rolls.NextDouble()));
			float thick = 3f + (float)rolls.NextDouble() * 3f;
			using SolidBrush drip = new SolidBrush(Fade(Pink, 0.95 * fade));
			graphics.FillRectangle(drip, x - thick / 2f, bottom - 4f, thick, length + 4f);
			graphics.FillEllipse(drip, x - thick * 0.8f, bottom + length - thick * 0.6f, thick * 1.6f, thick * 1.8f);
		}
		graphics.Restore(state);
		// a mint underline swoosh and a little heart once the words are done
		if (t > 1.4)
		{
			double grow = Ease((t - 1.4) / 0.4);
			using Pen swoosh = new Pen(Fade(Mint, fade), 6f);
			swoosh.StartCap = LineCap.Round;
			swoosh.EndCap = LineCap.Triangle;
			float y = top + measured.Height + 26f;
			float reach = left + measured.Width * (float)grow;
			graphics.DrawBezier(swoosh, left, y, left + measured.Width * 0.3f * (float)grow, y + 16f, reach - measured.Width * 0.3f * (float)grow, y - 14f, reach, y);
			double pop = Back((t - 1.6) / 0.3);
			if (pop > 0.0)
			{
				float hx = left + measured.Width + 26f;
				float hy = top + 10f;
				GraphicsState heartState = graphics.Save();
				graphics.TranslateTransform(hx, hy);
				graphics.ScaleTransform((float)pop * 1.6f, (float)pop * 1.6f);
				using GraphicsPath heart = new GraphicsPath();
				heart.AddBezier(0f, 8f, -12f, -2f, -6f, -12f, 0f, -4f);
				heart.AddBezier(0f, -4f, 6f, -12f, 12f, -2f, 0f, 8f);
				using SolidBrush red = new SolidBrush(Fade(Mint, fade));
				graphics.FillPath(red, heart);
				graphics.Restore(heartState);
			}
		}
		// paint mist puffing around the nozzle while spraying
		if (reveal < 1.0)
		{
			Random mist = new Random(seed + (int)(t * 60.0));
			float ny = top + measured.Height / 2f + (float)Math.Sin(t * 14.0) * measured.Height * 0.3f;
			for (int k = 0; k < 40; k++)
			{
				double angle = mist.NextDouble() * Math.PI * 2.0;
				float distance = (float)(mist.NextDouble() * mist.NextDouble() * 34.0);
				float r = 1f + (float)mist.NextDouble() * 2.2f;
				using SolidBrush dot = new SolidBrush(Fade(mist.Next(4) == 0 ? Color.White : Pink, (0.4 + 0.5 * mist.NextDouble()) * fade));
				graphics.FillEllipse(dot, edge + (float)Math.Cos(angle) * distance - r, ny + (float)Math.Sin(angle) * distance - r, r * 2f, r * 2f);
			}
		}
		return frame;
	}

	public void Dispose()
	{
		foreach (Action item in endings.ToArray())
		{
			item();
		}
	}

	[DllImport("user32.dll")]
	private static extern bool GetWindowRect(IntPtr window, out Rect rect);

	[DllImport("user32.dll")]
	private static extern bool IsWindow(IntPtr window);

	[DllImport("user32.dll")]
	private static extern bool IsWindowVisible(IntPtr window);

	[DllImport("user32.dll")]
	private static extern bool IsIconic(IntPtr window);

	[DllImport("user32.dll")]
	private static extern bool IsZoomed(IntPtr window);

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	private static extern int GetWindowTextLength(IntPtr window);

	[DllImport("user32.dll")]
	private static extern IntPtr GetTopWindow(IntPtr window);

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	private static extern int GetClassName(IntPtr window, StringBuilder name, int most);

	[DllImport("dwmapi.dll")]
	private static extern int DwmGetWindowAttribute(IntPtr window, int attribute, out int value, int size);
}
