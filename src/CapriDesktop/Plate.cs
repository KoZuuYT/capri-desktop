using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace CapriDesktop;

internal sealed class Plate : Button
{
	internal Color Accent = Neon.Rose;

	internal bool Solid;

	internal int Glyph;

	private bool hot;

	private bool down;

	internal Plate()
	{
		SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
		BackColor = Neon.Ground;
		Cursor = Cursors.Hand;
		UseMnemonic = false;
	}

	protected override void OnMouseEnter(EventArgs e)
	{
		hot = true;
		Invalidate();
		base.OnMouseEnter(e);
	}

	protected override void OnMouseLeave(EventArgs e)
	{
		hot = false;
		down = false;
		Invalidate();
		base.OnMouseLeave(e);
	}

	protected override void OnMouseDown(MouseEventArgs e)
	{
		if (e.Button == MouseButtons.Left)
		{
			down = true;
			Invalidate();
		}
		base.OnMouseDown(e);
	}

	protected override void OnMouseUp(MouseEventArgs e)
	{
		down = false;
		Invalidate();
		base.OnMouseUp(e);
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		float num = (float)DeviceDpi / 96f;
		Graphics graphics = e.Graphics;
		Neon.Behind(graphics, this, 210f * num);
		graphics.SmoothingMode = SmoothingMode.AntiAlias;
		float num2 = Math.Max(1f, num);
		RectangleF rectangleF = new RectangleF(num2 / 2f, num2 / 2f, (float)Width - num2, (float)Height - num2);
		float num3 = (float)Width / 2f;
		float num4 = (float)Height / 2f;
		if (Glyph != 0)
		{
			if (hot)
			{
				using GraphicsPath path = Neon.Cut(rectangleF, 0f, 0f, 4f * num, 0f);
				using Brush brush = new SolidBrush(down ? Neon.Pressed : Neon.Hover);
				graphics.FillPath(brush, path);
			}
			Color color;
			if (!hot && (!Focused || !ShowFocusCues))
			{
				color = Neon.Faint;
			}
			else
			{
				color = ((Glyph == 2) ? Neon.Hot : Neon.Cyan);
			}
			using Pen pen = new Pen(color, 1.5f * num);
			if (Glyph == 1)
			{
				graphics.DrawLine(pen, num3 - 5f * num, num4, num3 + 5f * num, num4);
				return;
			}
			graphics.DrawLine(pen, num3 - 4.5f * num, num4 - 4.5f * num, num3 + 4.5f * num, num4 + 4.5f * num);
			graphics.DrawLine(pen, num3 - 4.5f * num, num4 + 4.5f * num, num3 + 4.5f * num, num4 - 4.5f * num);
			return;
		}
		bool flag = hot || (Focused && ShowFocusCues);
		Color color2;
		Color color3;
		Color foreColor;
		if (!Enabled)
		{
			color2 = Neon.Field;
			color3 = Color.FromArgb(52, 22, 32);
			foreColor = Color.FromArgb(112, 94, 104);
		}
		else if (Solid)
		{
			if (down)
			{
				color2 = ControlPaint.Dark(Accent, 0.1f);
			}
			else
			{
				color2 = (hot ? ControlPaint.Light(Accent, 0.35f) : Accent);
			}
			color3 = color2;
			foreColor = Neon.OnInk;
		}
		else if (Accent == Neon.Cyan)
		{
			if (down)
			{
				color2 = Color.FromArgb(18, 62, 67);
			}
			else
			{
				color2 = (hot ? Color.FromArgb(13, 45, 49) : Neon.Deep);
			}
			color3 = (flag ? Neon.Cyan : Neon.DeepEdge);
			foreColor = Neon.Cyan;
		}
		else if (Accent == Neon.Hot)
		{
			if (down)
			{
				color2 = Color.FromArgb(72, 18, 30);
			}
			else
			{
				color2 = (hot ? Color.FromArgb(54, 16, 28) : Color.FromArgb(34, 12, 22));
			}
			color3 = (flag ? Color.FromArgb(255, 124, 122) : Neon.Hot);
			foreColor = Color.FromArgb(255, 166, 162);
		}
		else
		{
			if (down)
			{
				color2 = Neon.Pressed;
			}
			else
			{
				color2 = (hot ? Neon.Hover : Neon.Field);
			}
			color3 = (flag ? Neon.Hot : Neon.Edge);
			foreColor = Accent;
		}
		using (GraphicsPath path2 = Neon.Cut(rectangleF, 0f, 0f, 6f * num, 0f))
		{
			using (Brush brush2 = new SolidBrush(color2))
			{
				graphics.FillPath(brush2, path2);
			}
			using Pen pen2 = new Pen(color3, num2);
			graphics.DrawPath(pen2, path2);
		}
		if (Solid && Enabled && Focused && ShowFocusCues)
		{
			using GraphicsPath path3 = Neon.Cut(RectangleF.Inflate(rectangleF, -3f * num, -3f * num), 0f, 0f, 4f * num, 0f);
			using Pen pen3 = new Pen(Neon.OnInk, num2);
			graphics.DrawPath(pen3, path3);
		}
		TextRenderer.DrawText(graphics, Text, Font, ClientRectangle, foreColor, TextFormatFlags.EndEllipsis | TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter);
	}
}
