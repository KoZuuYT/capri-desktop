using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace CapriDesktop;

internal static class Neon
{
	internal sealed class Caption : Label
	{
		internal Caption()
		{
			BackColor = Color.Transparent;
			UseMnemonic = false;
		}

		protected override void WndProc(ref Message message)
		{
			if (message.Msg == 132)
			{
				message.Result = new IntPtr(-1);
			}
			else
			{
				base.WndProc(ref message);
			}
		}
	}

	internal static readonly Color Ground = Color.FromArgb(10, 10, 16);

	internal static readonly Color Glow = Color.FromArgb(54, 14, 26);

	internal static readonly Color Band = Color.FromArgb(16, 12, 20);

	internal static readonly Color Rule = Color.FromArgb(44, 122, 134);

	internal static readonly Color Rose = Color.FromArgb(255, 93, 102);

	internal static readonly Color White = Color.FromArgb(236, 236, 241);

	internal static readonly Color Faint = Color.FromArgb(146, 130, 140);

	internal static readonly Color Field = Color.FromArgb(17, 16, 28);

	internal static readonly Color Hover = Color.FromArgb(30, 20, 36);

	internal static readonly Color Pressed = Color.FromArgb(46, 22, 38);

	internal static readonly Color Edge = Color.FromArgb(92, 24, 36);

	internal static readonly Color Hot = Color.FromArgb(232, 66, 70);

	internal static readonly Color Cyan = Color.FromArgb(94, 240, 246);

	internal static readonly Color Deep = Color.FromArgb(9, 30, 33);

	internal static readonly Color DeepEdge = Color.FromArgb(40, 120, 132);

	internal static readonly Color OnInk = Color.FromArgb(6, 14, 18);

	internal static readonly Color Dash = Color.FromArgb(78, 28, 40);

	internal static readonly Color Lit = Color.FromArgb(245, 90, 86);

	internal static readonly Color Brim = Color.FromArgb(120, 30, 44);

	internal static readonly Color Well = Color.FromArgb(12, 11, 20);

	internal static readonly Color WellEdge = Color.FromArgb(72, 28, 40);

	internal static readonly Blend Fade = Falloff();

	private static readonly FontFamily Plain = Family("Bahnschrift SemiCondensed");

	private static readonly FontFamily Heavy = Family("Bahnschrift SemiBold SemiConden");

	private static readonly FontFamily Narrow = Family("Bahnschrift SemiBold Condensed");

	private static Blend Falloff()
	{
		Blend blend = new Blend(9);
		for (int i = 0; i < 9; i++)
		{
			float num = (float)i / 8f;
			blend.Positions[i] = num;
			blend.Factors[i] = 1f - (1f - num) * (1f - num);
		}
		return blend;
	}

	private static FontFamily Family(string name)
	{
		try
		{
			return new FontFamily(name);
		}
		catch (ArgumentException)
		{
			return null;
		}
	}

	internal static Font Type(float size, bool bold = false, GraphicsUnit unit = GraphicsUnit.Point)
	{
		FontFamily fontFamily = (bold ? Heavy : Plain);
		if (fontFamily == null)
		{
			return new Font("Segoe UI", size - 0.5f, bold ? FontStyle.Bold : FontStyle.Regular, unit);
		}
		return new Font(fontFamily, size, FontStyle.Regular, unit);
	}

	internal static Font Display(float size, GraphicsUnit unit = GraphicsUnit.Point)
	{
		if (Narrow == null)
		{
			return new Font("Segoe UI Semibold", size - 2f, FontStyle.Regular, unit);
		}
		return new Font(Narrow, size, FontStyle.Regular, unit);
	}

	internal static GraphicsPath Cut(RectangleF box, float topLeft, float topRight, float bottomRight, float bottomLeft)
	{
		GraphicsPath graphicsPath = new GraphicsPath();
		graphicsPath.AddPolygon(new PointF[8]
		{
			new PointF(box.Left + topLeft, box.Top),
			new PointF(box.Right - topRight, box.Top),
			new PointF(box.Right, box.Top + topRight),
			new PointF(box.Right, box.Bottom - bottomRight),
			new PointF(box.Right - bottomRight, box.Bottom),
			new PointF(box.Left + bottomLeft, box.Bottom),
			new PointF(box.Left, box.Bottom - bottomLeft),
			new PointF(box.Left, box.Top + topLeft)
		});
		return graphicsPath;
	}

	internal static void Frame(Graphics graphics, Size size, float s, float big, float small, float along, float down)
	{
		SmoothingMode smoothingMode = graphics.SmoothingMode;
		graphics.SmoothingMode = SmoothingMode.AntiAlias;
		RectangleF rectangleF = new RectangleF(0.5f * s, 0.5f * s, (float)size.Width - s - 1f, (float)size.Height - s - 1f);
		using (GraphicsPath path = Cut(rectangleF, big, small, big, small))
		{
			using Pen pen = new Pen(Brim, Math.Max(1f, 1.2f * s));
			graphics.DrawPath(pen, path);
		}
		RectangleF rectangleF2 = RectangleF.Inflate(rectangleF, -0.6f * s, -0.6f * s);
		using (Pen pen2 = new Pen(Hot, 2f * s))
		{
			graphics.DrawLines(pen2, new PointF[4]
			{
				new PointF(rectangleF2.Left, rectangleF2.Top + big + down),
				new PointF(rectangleF2.Left, rectangleF2.Top + big),
				new PointF(rectangleF2.Left + big, rectangleF2.Top),
				new PointF(rectangleF2.Left + big + along, rectangleF2.Top)
			});
			graphics.DrawLines(pen2, new PointF[4]
			{
				new PointF(rectangleF2.Right, rectangleF2.Bottom - big - down),
				new PointF(rectangleF2.Right, rectangleF2.Bottom - big),
				new PointF(rectangleF2.Right - big, rectangleF2.Bottom),
				new PointF(rectangleF2.Right - big - along, rectangleF2.Bottom)
			});
		}
		graphics.SmoothingMode = smoothingMode;
	}

	internal static void Clip(Control window, float big, float small)
	{
		Region region = window.Region;
		using (GraphicsPath path = Cut(new RectangleF(0f, 0f, window.Width, window.Height), big, small, big, small))
		{
			window.Region = new Region(path);
		}
		region?.Dispose();
	}

	internal static void Backdrop(Graphics graphics, Size size, float reach)
	{
		using (Brush brush = new SolidBrush(Ground))
		{
			graphics.FillRectangle(brush, 0, 0, size.Width, size.Height);
		}
		float num = Math.Min(size.Height, reach);
		if (num < 1f)
		{
			return;
		}
		using LinearGradientBrush linearGradientBrush = new LinearGradientBrush(new RectangleF(0f, -1f, 16f, num + 2f), Glow, Ground, LinearGradientMode.Vertical);
		linearGradientBrush.Blend = Fade;
		graphics.FillRectangle(linearGradientBrush, 0f, 0f, size.Width, num);
	}

	internal static void Behind(Graphics graphics, Control control, float reach)
	{
		Form form = control.FindForm();
		if (form == null)
		{
			Backdrop(graphics, control.Size, reach);
			return;
		}
		int num = 0;
		int num2 = 0;
		Control control2 = control;
		while (control2 != null && control2 != form)
		{
			num += control2.Left;
			num2 += control2.Top;
			control2 = control2.Parent;
		}
		GraphicsState gstate = graphics.Save();
		graphics.TranslateTransform(-num, -num2);
		Backdrop(graphics, form.ClientSize, reach);
		graphics.Restore(gstate);
	}
}
