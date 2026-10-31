using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CapriDesktop;

internal sealed class HerPointer : Overlay
{
	internal const double Ripple = 0.38;

	private readonly Timer ticks = new Timer();

	private readonly Stopwatch watch = Stopwatch.StartNew();

	private readonly object gate = new object();

	private IntPtr window;

	private Bitmap arrow;

	private Point tip;

	private int reach = 40;

	private float scale = 1f;

	private double tapped = -1.0;

	protected override CreateParams CreateParams
	{
		get
		{
			CreateParams createParams = base.CreateParams;
			createParams.ExStyle |= 32;
			return createParams;
		}
	}

	internal Point Tip
	{
		get
		{
			lock (gate)
			{
				return tip;
			}
		}
	}

	internal bool Rippling => tapped >= 0.0;

	internal HerPointer()
		: base("Capri pointer")
	{
		ticks.Interval = 16;
		Timer timer = ticks;
		EventHandler value = (object param0, EventArgs param1) =>
		{
			Step();
		};
		timer.Tick += value;
	}

	[DllImport("user32.dll")]
	private static extern bool SetWindowPos(IntPtr window, IntPtr after, int x, int y, int width, int height, uint flags);

	protected override void OnHandleCreated(EventArgs e)
	{
		base.OnHandleCreated(e);
		window = Handle;
	}

	internal void MoveTo(Point at)
	{
		int num;
		lock (gate)
		{
			tip = at;
			num = reach;
		}
		IntPtr intPtr = window;
		if (intPtr != IntPtr.Zero)
		{
			SetWindowPos(intPtr, new IntPtr(-1), at.X - num, at.Y - num, 0, 0, 16401u);
		}
	}

	internal void Begin(Point at)
	{
		using (Graphics graphics = Graphics.FromHwnd(IntPtr.Zero))
		{
			scale = Math.Max(1f, graphics.DpiX / 96f);
		}
		int num = (int)Math.Round(36f * scale);
		if (arrow == null || arrow.Width != num)
		{
			if (arrow != null)
			{
				arrow.Dispose();
			}
			arrow = Look.PointerArt(num);
		}
		lock (gate)
		{
			tip = at;
			reach = (int)Math.Round(40f * scale);
		}
		tapped = -1.0;
		Render();
	}

	internal void Tap()
	{
		if (Visible)
		{
			tapped = watch.Elapsed.TotalSeconds;
			ticks.Start();
			Render();
		}
	}

	internal void End()
	{
		ticks.Stop();
		tapped = -1.0;
		if (Visible)
		{
			Hide();
		}
	}

	private void Step()
	{
		if (tapped >= 0.0 && watch.Elapsed.TotalSeconds - tapped > 0.38)
		{
			tapped = -1.0;
			ticks.Stop();
		}
		if (Visible)
		{
			Render();
		}
	}

	internal void Render()
	{
		if (arrow == null)
		{
			return;
		}
		Point point = Tip;
		Bitmap image = new Bitmap(reach * 2, reach * 2, PixelFormat.Format32bppArgb);
		using (Graphics graphics = Graphics.FromImage(image))
		{
			graphics.SmoothingMode = SmoothingMode.AntiAlias;
			if (tapped >= 0.0)
			{
				double num = Math.Min(1.0, (watch.Elapsed.TotalSeconds - tapped) / 0.38);
				float num2 = (float)(1.0 - Math.Pow(1.0 - num, 3.0));
				float num3 = (5f + 17f * num2) * scale;
				int num4 = (int)Math.Round(230.0 * (1.0 - num));
				using (Pen pen = new Pen(Color.FromArgb(num4 / 2, Look.Cyan), 2f * scale))
				{
					graphics.DrawEllipse(pen, (float)reach - num3 + 1.5f * scale, (float)reach - num3 + 0.5f * scale, num3 * 2f, num3 * 2f);
				}
				using Pen pen2 = new Pen(Color.FromArgb(num4, Look.Pink), (2.6f - 1.2f * num2) * scale);
				graphics.DrawEllipse(pen2, (float)reach - num3, (float)reach - num3, num3 * 2f, num3 * 2f);
			}
			float num5 = (float)arrow.Width / 32f;
			graphics.DrawImage(arrow, (float)reach - 2f * num5, (float)reach - 2f * num5, arrow.Width, arrow.Height);
		}
		Present(image, new Point(point.X - reach, point.Y - reach));
		SetWindowPos(Handle, new IntPtr(-1), 0, 0, 0, 0, 19u);
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			ticks.Dispose();
			if (arrow != null)
			{
				arrow.Dispose();
				arrow = null;
			}
		}
		base.Dispose(disposing);
	}
}
