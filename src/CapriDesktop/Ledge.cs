using System;
using System.Drawing;

namespace CapriDesktop;

internal sealed class Ledge
{
	internal IntPtr Window;

	internal Rectangle Bounds;

	internal Rectangle Icon;

	internal bool Floor;

	internal bool Free;

	internal int Left;

	internal int Right;

	internal int Y;
}
