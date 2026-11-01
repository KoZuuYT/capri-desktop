using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Windows.Forms;

namespace CapriDesktop;

// Mischief mode pranks: a quick screen glitch, a short cursor theft and a fake "you've been hacked" note.
// All harmless: nothing is clicked, typed or changed, each one cleans up after itself in a few seconds,
// and grabbing the mouse back ends the cursor theft at once.
internal sealed class Mischief : IDisposable
{
	private const double GlitchSeconds = 0.75;

	private const double NoteSeconds = 8.0;

	private static readonly Color Pink = Color.FromArgb(255, 40, 120);

	private static readonly Color Mint = Color.FromArgb(30, 240, 138);

	private static readonly Color Cyan = Color.FromArgb(40, 230, 255);

	private static readonly Color Ink = Color.FromArgb(8, 6, 14);

	private readonly Random random = new Random();

	private readonly Overlay glitch = new Overlay("Capri mischief");

	private readonly System.Windows.Forms.Timer glitchTicks = new System.Windows.Forms.Timer { Interval = 40 };

	private readonly Stopwatch glitchWatch = new Stopwatch();

	private readonly System.Windows.Forms.Timer stealTicks = new System.Windows.Forms.Timer { Interval = 15 };

	private readonly Stopwatch stealWatch = new Stopwatch();

	private readonly List<Action> notes = new List<Action>();

	private Bitmap shot;

	private Rectangle area;

	private int seed;

	private Point stealFrom;

	private Point stealTo;

	private Point lastSet;

	internal Mischief()
	{
		glitchTicks.Tick += (object param0, EventArgs param1) =>
		{
			GlitchStep();
		};
		stealTicks.Tick += (object param0, EventArgs param1) =>
		{
			StealStep();
		};
	}

	// tears the screen into shifted, tinted slices for a moment
	internal void Glitch(Rectangle screen)
	{
		StopGlitch();
		area = screen;
		try
		{
			shot = new Bitmap(area.Width, area.Height, PixelFormat.Format32bppArgb);
			using Graphics graphics = Graphics.FromImage(shot);
			graphics.CopyFromScreen(area.Location, Point.Empty, area.Size);
		}
		catch (Exception)
		{
			shot?.Dispose();
			shot = null;
			return;
		}
		seed = random.Next();
		glitchWatch.Restart();
		GlitchStep();
		glitchTicks.Start();
	}

	private void GlitchStep()
	{
		double t = glitchWatch.Elapsed.TotalSeconds;
		if (t >= GlitchSeconds || shot == null)
		{
			StopGlitch();
			return;
		}
		Random rolls = new Random(seed + (int)(t * 25.0));
		double strength = 0.35 + 0.65 * Math.Sin(Math.PI * t / GlitchSeconds);
		Bitmap frame = new Bitmap(area.Width, area.Height, PixelFormat.Format32bppArgb);
		using (Graphics graphics = Graphics.FromImage(frame))
		{
			graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
			int slices = 4 + (int)(strength * 14.0);
			for (int i = 0; i < slices; i++)
			{
				int height = 4 + rolls.Next(Math.Max(5, area.Height / 16));
				int y = rolls.Next(Math.Max(1, area.Height - height));
				int shift = (int)((rolls.NextDouble() * 2.0 - 1.0) * 80.0 * strength);
				graphics.DrawImage(shot, new Rectangle(shift, y, area.Width, height), new Rectangle(0, y, area.Width, height), GraphicsUnit.Pixel);
				if (rolls.Next(3) == 0)
				{
					using SolidBrush tint = new SolidBrush(Color.FromArgb(60 + rolls.Next(70), Tint(rolls)));
					graphics.FillRectangle(tint, shift, y, area.Width, height);
				}
			}
			int bars = 2 + (int)(strength * 5.0);
			for (int j = 0; j < bars; j++)
			{
				using SolidBrush bar = new SolidBrush(Color.FromArgb(210, Tint(rolls)));
				graphics.FillRectangle(bar, rolls.Next(area.Width / 2), rolls.Next(area.Height), area.Width / 4 + rolls.Next(area.Width / 2), 1 + rolls.Next(3));
			}
		}
		glitch.Present(frame, area.Location);
	}

	private static Color Tint(Random rolls)
	{
		return rolls.Next(3) switch
		{
			0 => Pink,
			1 => Cyan,
			_ => Mint,
		};
	}

	private void StopGlitch()
	{
		glitchTicks.Stop();
		if (glitch.Visible)
		{
			glitch.Hide();
		}
		shot?.Dispose();
		shot = null;
	}

	// drags the cursor over to her, spins it around her and hands it back
	internal void Steal(Point to)
	{
		stealFrom = Cursor.Position;
		Rectangle bounds = Screen.FromPoint(to).Bounds;
		stealTo = new Point(Math.Clamp(to.X, bounds.Left + 60, bounds.Right - 60), Math.Clamp(to.Y, bounds.Top + 40, bounds.Bottom - 40));
		lastSet = stealFrom;
		stealWatch.Restart();
		stealTicks.Start();
	}

	private void StealStep()
	{
		Point at = Cursor.Position;
		if (Math.Abs(at.X - lastSet.X) + Math.Abs(at.Y - lastSet.Y) > 60)
		{
			// they grabbed it back
			stealTicks.Stop();
			return;
		}
		double t = stealWatch.Elapsed.TotalSeconds;
		double x;
		double y;
		if (t < 0.8)
		{
			double k = Ease(t / 0.8);
			double wiggle = Math.Sin(t * 28.0) * 14.0 * (1.0 - k);
			x = stealFrom.X + (stealTo.X - stealFrom.X) * k;
			y = stealFrom.Y + (stealTo.Y - stealFrom.Y) * k + wiggle;
		}
		else if (t < 1.7)
		{
			double angle = (t - 0.8) / 0.9 * Math.PI * 4.0;
			x = stealTo.X + Math.Sin(angle) * 50.0;
			y = stealTo.Y - (1.0 - Math.Cos(angle)) * 30.0;
		}
		else if (t < 2.3)
		{
			double k = Ease((t - 1.7) / 0.6);
			x = stealTo.X + (stealFrom.X - stealTo.X) * k;
			y = stealTo.Y + (stealFrom.Y - stealTo.Y) * k;
		}
		else
		{
			Cursor.Position = stealFrom;
			stealTicks.Stop();
			return;
		}
		lastSet = new Point((int)Math.Round(x), (int)Math.Round(y));
		Cursor.Position = lastSet;
		lastSet = Cursor.Position;
	}

	private static double Ease(double k)
	{
		k = Math.Clamp(k, 0.0, 1.0);
		return k * k * (3.0 - 2.0 * k);
	}

	// pops a fake security alert somewhere on the screen; a click or a few seconds closes it
	internal void Hacked(Rectangle work)
	{
		Overlay note = new Overlay("Capri hacked note");
		System.Windows.Forms.Timer life = new System.Windows.Forms.Timer { Interval = 40 };
		Stopwatch watch = Stopwatch.StartNew();
		Size size = new Size(400, 160);
		Point at = new Point(work.Left + random.Next(Math.Max(1, work.Width - size.Width)), work.Top + random.Next(Math.Max(1, work.Height - size.Height)));
		bool steady = false;
		bool closed = false;
		Action close = null;
		close = () =>
		{
			if (!closed)
			{
				closed = true;
				life.Stop();
				life.Dispose();
				notes.Remove(close);
				note.Close();
				note.Dispose();
			}
		};
		notes.Add(close);
		note.MouseDown += (object param0, MouseEventArgs param1) =>
		{
			close();
		};
		life.Tick += (object param0, EventArgs param1) =>
		{
			double t = watch.Elapsed.TotalSeconds;
			if (t >= NoteSeconds)
			{
				close();
			}
			else if (t < 0.45 || t > NoteSeconds - 0.35)
			{
				steady = false;
				note.Present(Card(size, glitchy: true), new Point(at.X + random.Next(-8, 9), at.Y + random.Next(-5, 6)));
			}
			else if (!steady)
			{
				steady = true;
				note.Present(Card(size, glitchy: false), at);
			}
		};
		note.Present(Card(size, glitchy: true), at);
		life.Start();
	}

	private Bitmap Card(Size size, bool glitchy)
	{
		Bitmap card = new Bitmap(size.Width, size.Height, PixelFormat.Format32bppArgb);
		using Graphics graphics = Graphics.FromImage(card);
		graphics.SmoothingMode = SmoothingMode.AntiAlias;
		graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
		using (SolidBrush back = new SolidBrush(Color.FromArgb(242, Ink)))
		{
			graphics.FillRectangle(back, 0, 0, size.Width, size.Height);
		}
		using (SolidBrush header = new SolidBrush(Pink))
		{
			graphics.FillRectangle(header, 0, 0, size.Width, 30);
		}
		using (Pen edge = new Pen(Mint, 2f))
		{
			graphics.DrawRectangle(edge, 1, 1, size.Width - 3, size.Height - 3);
		}
		using (Font font = Neon.Display(15f, GraphicsUnit.Pixel))
		using (SolidBrush ink = new SolidBrush(Ink))
		{
			graphics.DrawString("!! ALERTA DE SEGURANÇA !!", font, ink, 12f, 6f);
		}
		using (Font font2 = Neon.Display(34f, GraphicsUnit.Pixel))
		using (SolidBrush mint = new SolidBrush(Mint))
		{
			graphics.DrawString("VOCÊ FOI HACKEADO", font2, mint, 12f, 40f);
		}
		using (Font font3 = Neon.Type(10f))
		using (SolidBrush light = new SolidBrush(Color.FromArgb(225, 230, 240)))
		using (SolidBrush dim = new SolidBrush(Color.FromArgb(140, 150, 170)))
		{
			graphics.DrawString("Todos os seus arquivos agora pertencem à Capri.", font3, light, 14f, 92f);
			graphics.DrawString("Brincadeira, choom.  ·  clique para fechar", font3, dim, 14f, 124f);
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

	public void Dispose()
	{
		StopGlitch();
		stealTicks.Stop();
		foreach (Action item in notes.ToArray())
		{
			item();
		}
		glitchTicks.Dispose();
		stealTicks.Dispose();
		glitch.Close();
		glitch.Dispose();
	}
}
