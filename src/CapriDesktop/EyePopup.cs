using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace CapriDesktop;

// A small cyberpunk pop-up with Capri's face, shown now and then. It never takes focus and fades out by itself.
internal sealed class EyePopup : Form
{
	private const int Frames = 120;

	private static readonly Random rolls = new Random();

	private readonly Bitmap face;

	private readonly System.Windows.Forms.Timer clock = new System.Windows.Forms.Timer { Interval = 40 };

	private int frame;

	internal EyePopup(Bitmap face)
	{
		this.face = face;
		FormBorderStyle = FormBorderStyle.None;
		ShowInTaskbar = false;
		TopMost = true;
		StartPosition = FormStartPosition.Manual;
		BackColor = Color.FromArgb(10, 6, 20);
		Size = new Size(240, 180);
		Location = Corner();
		DoubleBuffered = true;
		Opacity = 0.0;
		clock.Tick += (sender, args) => Step();
		Click += (sender, args) => Close();
		clock.Start();
	}

	protected override bool ShowWithoutActivation => true;

	private Point Corner()
	{
		Rectangle area = Screen.PrimaryScreen.WorkingArea;
		bool right = rolls.Next(2) == 0;
		bool bottom = rolls.Next(2) == 0;
		int x = right ? area.Right - Width - 20 : area.Left + 20;
		int y = bottom ? area.Bottom - Height - 20 : area.Top + 20;
		return new Point(x, y);
	}

	private void Step()
	{
		frame++;
		if (frame >= Frames)
		{
			clock.Stop();
			Close();
			return;
		}
		double fadeIn = Math.Min(1.0, frame / 8.0);
		double fadeOut = Math.Min(1.0, (Frames - frame) / 15.0);
		Opacity = 0.95 * Math.Min(fadeIn, fadeOut);
		Invalidate();
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		Graphics g = e.Graphics;
		g.SmoothingMode = SmoothingMode.AntiAlias;
		g.InterpolationMode = InterpolationMode.HighQualityBicubic;
		g.Clear(BackColor);

		Rectangle plate = new Rectangle(45, 6, 150, 150);
		g.DrawImage(face, plate);

		if (frame % 9 < 2)
		{
			using Brush bar = new SolidBrush(Color.FromArgb(120, 255, 46, 136));
			for (int i = 0; i < 3; i++)
			{
				int y = rolls.Next(plate.Top, plate.Bottom - 8);
				g.FillRectangle(bar, plate.Left, y, plate.Width, rolls.Next(3, 9));
			}
		}

		using (Pen scan = new Pen(Color.FromArgb(40, 0, 0, 0)))
		{
			for (int y = 0; y < Height; y += 3)
			{
				g.DrawLine(scan, 0, y, Width, y);
			}
		}

		int pulse = 170 + (int)(85 * Math.Sin(frame * 0.3));
		using (Pen cyan = new Pen(Color.FromArgb(pulse, 0, 229, 255), 2f))
		using (Pen magenta = new Pen(Color.FromArgb(255, 46, 136), 1f))
		{
			g.DrawRectangle(cyan, 1, 1, Width - 3, Height - 3);
			g.DrawRectangle(magenta, 6, 6, Width - 13, Height - 13);
		}

		using Font mono = new Font("Consolas", 8.5f, FontStyle.Bold);
		using Brush text = new SolidBrush(Color.FromArgb(0, 229, 255));
		g.DrawString("> SINAL DA CAPRI", mono, text, 14, 158);
	}

	protected override void OnFormClosed(FormClosedEventArgs e)
	{
		clock.Dispose();
		base.OnFormClosed(e);
	}
}
