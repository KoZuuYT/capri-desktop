using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace CapriDesktop;

// mod: hide-and-seek. After a countdown she hides behind one edge of the screen with only the top of her head
// showing; every few seconds she peeks out a little further with a tiny blip, and ducks back with a giggle when the
// cursor gets close. A click on her ends the game as found; after the time runs out she wins.
internal sealed class HideSeek : IDisposable
{
	internal const double Seconds = 75.0;

	internal const double CountIn = 3.0;

	internal static readonly string[] EdgeNames = { "de baixo", "da esquerda", "da direita", "de cima" };

	private sealed class Pad : Overlay
	{
		internal Action Pressed;

		internal Pad(string name)
			: base(name)
		{
			Cursor = Cursors.Hand;
		}

		protected override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown(e);
			if (e.Button == MouseButtons.Left)
			{
				Pressed?.Invoke();
			}
		}
	}

	private readonly Random random = new Random();

	private readonly Stopwatch watch = Stopwatch.StartNew();

	private readonly System.Windows.Forms.Timer ticks = new System.Windows.Forms.Timer { Interval = 30 };

	private readonly Bitmap head;

	private readonly Rectangle work;

	private readonly Action<bool, Point> finished;

	private readonly Pad layer = new Pad("Capri hiding");

	private readonly int along;

	private double nextPeek;

	private double peekAt = -10.0;

	private double foundAt = -1.0;

	private double depth = 0.0;

	private bool near;

	private bool hinted;

	private bool over;

	// 0 bottom, 1 left, 2 right, 3 top
	internal readonly int Edge;

	internal bool Running => !over;

	internal double Elapsed => watch.Elapsed.TotalSeconds;

	// head: the top of her picture, upright; finished(found, where she was) runs once
	internal HideSeek(Bitmap picture, Rectangle work, Action<bool, Point> finished)
	{
		this.work = work;
		this.finished = finished;
		Edge = random.Next(4);
		head = new Bitmap(picture);
		switch (Edge)
		{
		case 1:
			head.RotateFlip(RotateFlipType.Rotate90FlipNone);
			break;
		case 2:
			head.RotateFlip(RotateFlipType.Rotate270FlipNone);
			break;
		case 3:
			head.RotateFlip(RotateFlipType.Rotate180FlipNone);
			break;
		}
		bool across = Edge == 0 || Edge == 3;
		int room = across ? (work.Width - head.Width - 120) : (work.Height - head.Height - 200);
		along = (across ? (work.Left + 60) : (work.Top + 140)) + random.Next(Math.Max(1, room));
		nextPeek = CountIn + 4.0 + random.NextDouble() * 3.0;
		layer.Pressed = Found;
		ticks.Tick += (object sender, EventArgs e) => Step();
		ticks.Start();
	}

	// where her head is, for putting her back there
	internal Point Spot
	{
		get
		{
			return Edge switch
			{
				1 => new Point(work.Left + head.Width / 2, along + head.Height / 2),
				2 => new Point(work.Right - head.Width / 2, along + head.Height / 2),
				3 => new Point(along + head.Width / 2, work.Top + head.Height / 2),
				_ => new Point(along + head.Width / 2, work.Bottom - head.Height / 2)
			};
		}
	}

	internal bool HintDue => !hinted && Elapsed > CountIn + 25.0;

	internal void Hinted()
	{
		hinted = true;
	}

	private void Found()
	{
		if (over || foundAt >= 0.0 || Elapsed < CountIn)
		{
			return;
		}
		foundAt = Elapsed;
		Sfx.Play(Cue.Found);
	}

	private static double Smooth(double k)
	{
		k = Math.Clamp(k, 0.0, 1.0);
		return k * k * (3.0 - 2.0 * k);
	}

	private void Step()
	{
		if (over)
		{
			return;
		}
		double t = Elapsed;
		if (t < CountIn)
		{
			return;
		}
		if (foundAt >= 0.0)
		{
			double k = (t - foundAt) / 0.35;
			depth = 0.25 + 0.75 * Smooth(k);
			Draw(t, found: true);
			if (k >= 1.6)
			{
				Finish(found: true);
			}
			return;
		}
		if (t >= CountIn + Seconds)
		{
			Sfx.Play(Cue.Giggle);
			Finish(found: false);
			return;
		}
		Point cursor = Cursor.Position;
		Point spot = Spot;
		double dx = cursor.X - spot.X;
		double dy = cursor.Y - spot.Y;
		bool close = Math.Sqrt(dx * dx + dy * dy) < 200.0;
		if (close && !near)
		{
			// ducks when you get warm, with a little giggle as the clue
			Sfx.Play(Cue.Giggle);
		}
		near = close;
		if (t >= nextPeek && !near)
		{
			peekAt = t;
			// she gets bolder as time goes by
			double late = Math.Clamp((t - CountIn) / Seconds, 0.0, 1.0);
			nextPeek = t + (6.0 - 3.0 * late) + random.NextDouble() * 3.0;
			Sfx.Play(Cue.Peek);
		}
		double peek = t - peekAt;
		double want = near ? 0.1 : ((peek < 1.3) ? (0.2 + 0.4 * Math.Sin(Math.PI * Math.Min(1.0, peek / 1.3))) : 0.2);
		depth += (want - depth) * 0.25;
		Draw(t, found: false);
	}

	private void Draw(double t, bool found)
	{
		bool across = Edge == 0 || Edge == 3;
		int deep = Math.Max(4, (int)((across ? head.Height : head.Width) * depth));
		int w = across ? head.Width : deep;
		int h = across ? deep : head.Height;
		Bitmap bitmap = new Bitmap(w, h, PixelFormat.Format32bppPArgb);
		using (Graphics g = Graphics.FromImage(bitmap))
		{
			g.InterpolationMode = InterpolationMode.HighQualityBicubic;
			// a little sway so she looks alive while hiding
			float sway = near ? (float)(Math.Sin(t * 40.0) * 2.0) : (float)(Math.Sin(t * 2.0) * 1.5);
			Point from = Edge switch
			{
				1 => new Point(head.Width - deep, 0),
				3 => new Point(0, head.Height - deep),
				_ => new Point(0, 0)
			};
			Rectangle source = new Rectangle(from, new Size(w, h));
			Rectangle target = across ? new Rectangle((int)sway, 0, w, h) : new Rectangle(0, (int)sway, w, h);
			g.DrawImage(head, target, source, GraphicsUnit.Pixel);
			if (found)
			{
				// a bright flash over her as she's caught
				double k = Math.Clamp((t - foundAt) / 0.3, 0.0, 1.0);
				using ImageAttributes white = new ImageAttributes();
				white.SetColorMatrix(new ColorMatrix(new float[5][]
				{
					new float[5],
					new float[5],
					new float[5],
					new float[5] { 0f, 0f, 0f, (float)(0.8 * (1.0 - k)), 0f },
					new float[5] { 1f, 1f, 1f, 0f, 1f }
				}));
				g.DrawImage(head, target, source.X, source.Y, source.Width, source.Height, GraphicsUnit.Pixel, white);
			}
		}
		Point at = Edge switch
		{
			1 => new Point(work.Left, along),
			2 => new Point(work.Right - w, along),
			3 => new Point(along, work.Top),
			_ => new Point(along, work.Bottom - h)
		};
		layer.Present(bitmap, at);
	}

	internal void Stop()
	{
		if (!over)
		{
			over = true;
			Dispose();
		}
	}

	private void Finish(bool found)
	{
		if (over)
		{
			return;
		}
		Point spot = Spot;
		over = true;
		Dispose();
		finished?.Invoke(found, spot);
	}

	public void Dispose()
	{
		over = true;
		ticks.Stop();
		ticks.Dispose();
		if (!layer.IsDisposed)
		{
			layer.Close();
			layer.Dispose();
		}
		head.Dispose();
	}
}
