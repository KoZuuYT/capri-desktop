using System;
using System.Drawing;
using System.Windows.Forms;

namespace CapriDesktop;

internal class Overlay : Form
{
	private Bitmap shown;

	internal bool Above = true;

	protected override bool ShowWithoutActivation => true;

	protected override CreateParams CreateParams
	{
		get
		{
			CreateParams createParams = base.CreateParams;
			createParams.ExStyle |= 0x8080080 | (Above ? 8 : 0);
			return createParams;
		}
	}

	internal Bitmap Image => shown;

	internal Overlay(string name)
	{
		Text = name;
		FormBorderStyle = FormBorderStyle.None;
		ShowInTaskbar = false;
		StartPosition = FormStartPosition.Manual;
		AutoScaleMode = AutoScaleMode.None;
	}

	internal void Present(Bitmap image, Point at)
	{
		Rectangle rectangle = new Rectangle(at, image.Size);
		if (Bounds != rectangle)
		{
			Bounds = rectangle;
		}
		if (!Visible)
		{
			Show();
		}
		Native.DrawLayered(Handle, image, at);
		Bitmap bitmap = shown;
		shown = image;
		bitmap?.Dispose();
	}

	protected override void WndProc(ref Message message)
	{
		if (message.Msg == 132 && shown != null)
		{
			long num = message.LParam.ToInt64();
			Point point = PointToClient(new Point((short)(num & 0xFFFF), (short)((num >> 16) & 0xFFFF)));
			if (point.X < 0 || point.Y < 0 || point.X >= shown.Width || point.Y >= shown.Height || shown.GetPixel(point.X, point.Y).A < 12)
			{
				message.Result = new IntPtr(-1);
				return;
			}
		}
		base.WndProc(ref message);
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing && shown != null)
		{
			shown.Dispose();
			shown = null;
		}
		base.Dispose(disposing);
	}
}
