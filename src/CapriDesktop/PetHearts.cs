using System;
using System.Drawing;
using System.Windows.Forms;

namespace CapriDesktop;

// A few little hearts that float up from Capri when she gets petted. Transparent, never takes focus.
internal sealed class PetHearts : Form
{
	private const int Frames = 45;

	private static readonly Random rolls = new Random();

	private static readonly Color Pink = Color.FromArgb(255, 110, 180);

	private readonly System.Windows.Forms.Timer clock = new System.Windows.Forms.Timer { Interval = 30 };

	private readonly float[] drift = new float[5];

	private readonly float[] sizes = new float[5];

	private int frame;

	internal PetHearts()
	{
		FormBorderStyle = FormBorderStyle.None;
		ShowInTaskbar = false;
		TopMost = true;
		StartPosition = FormStartPosition.Manual;
		BackColor = Color.Magenta;
		TransparencyKey = Color.Magenta;
		Size = new Size(160, 130);
		DoubleBuffered = true;
		for (int i = 0; i < drift.Length; i++)
		{
			drift[i] = (float)(rolls.NextDouble() * 2.0 - 1.0);
			sizes[i] = 12f + (float)rolls.NextDouble() * 8f;
		}
		clock.Tick += (sender, args) => Step();
		clock.Start();
	}

	protected override bool ShowWithoutActivation => true;

	internal void Place(Rectangle near)
	{
		Location = new Point(near.Left + near.Width / 2 - Width / 2, near.Top - 20);
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
		Invalidate();
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		Graphics g = e.Graphics;
		g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
		g.Clear(BackColor);
		float t = frame / (float)Frames;
		for (int i = 0; i < drift.Length; i++)
		{
			float rise = t * (Height - 30);
			float x = Width / 2f - 20f + drift[i] * 40f + (float)Math.Sin((frame + i * 6) * 0.25) * 8f;
			float y = Height - 30f - rise - i * 6f;
			int alpha = (int)(255 * (1.0 - t));
			using Font font = new Font("Segoe UI Symbol", sizes[i], FontStyle.Bold);
			using Brush brush = new SolidBrush(Color.FromArgb(alpha, Pink));
			g.DrawString("♥", font, brush, x, y);
		}
	}

	protected override void OnFormClosed(FormClosedEventArgs e)
	{
		clock.Dispose();
		base.OnFormClosed(e);
	}
}
