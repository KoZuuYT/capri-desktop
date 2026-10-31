using System;
using System.Drawing;

namespace CapriDesktop;

internal sealed class Picture
{
	internal byte[] Jpeg;

	internal Size Size;

	internal Rectangle Area;

	internal Point ToScreen(double x, double y)
	{
		x = Math.Max(0.0, Math.Min(Size.Width - 1, x));
		y = Math.Max(0.0, Math.Min(Size.Height - 1, y));
		return new Point(Area.X + (int)Math.Round(x * (double)Area.Width / (double)Size.Width), Area.Y + (int)Math.Round(y * (double)Area.Height / (double)Size.Height));
	}

	internal Point FromScreen(Point at)
	{
		return new Point((int)Math.Round((double)(at.X - Area.X) * (double)Size.Width / (double)Area.Width), (int)Math.Round((double)(at.Y - Area.Y) * (double)Size.Height / (double)Area.Height));
	}

	internal static Picture Of(Bitmap shot, Rectangle area)
	{
		double num = Math.Min(1.0, 1280.0 / (double)Math.Max(shot.Width, shot.Height));
		Picture picture = new Picture();
		picture.Jpeg = Companion.Jpeg(shot, 1280);
		picture.Size = new Size(Math.Max(1, (int)Math.Round((double)shot.Width * num)), Math.Max(1, (int)Math.Round((double)shot.Height * num)));
		picture.Area = area;
		return picture;
	}
}
