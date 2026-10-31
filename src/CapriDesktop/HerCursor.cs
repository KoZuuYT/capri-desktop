using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;

namespace CapriDesktop;

internal static class HerCursor
{
	private struct IconInfo
	{
		internal bool Icon;

		internal int X;

		internal int Y;

		internal IntPtr Mask;

		internal IntPtr Colour;
	}

	private static readonly uint[] Shapes = new uint[4] { 32512u, 32513u, 32649u, 32650u };

	internal static string Marker;

	internal static Action<bool> Apply = Swap;

	private static volatile bool worn;

	internal static bool Worn => worn;

	[DllImport("user32.dll")]
	private static extern bool SetSystemCursor(IntPtr cursor, uint id);

	[DllImport("user32.dll")]
	private static extern IntPtr CopyImage(IntPtr image, uint type, int width, int height, uint flags);

	[DllImport("user32.dll")]
	private static extern bool DestroyCursor(IntPtr cursor);

	[DllImport("user32.dll")]
	private static extern bool SystemParametersInfo(uint action, uint value, IntPtr data, uint update);

	[DllImport("user32.dll")]
	private static extern bool GetIconInfo(IntPtr icon, ref IconInfo info);

	[DllImport("gdi32.dll")]
	private static extern bool DeleteObject(IntPtr value);

	internal static void Wear()
	{
		if (worn)
		{
			return;
		}
		worn = true;
		Mark(on: true);
		try
		{
			Apply(obj: true);
		}
		catch (Exception ex)
		{
			Brain.Log("her cursor", -1L, Voice.Short(ex.Message));
		}
	}

	internal static void Off()
	{
		if (worn)
		{
			worn = false;
			try
			{
				Apply(obj: false);
			}
			catch (Exception ex)
			{
				Brain.Log("her cursor", -1L, Voice.Short(ex.Message));
			}
			Mark(on: false);
		}
	}

	internal static bool Recover()
	{
		if (worn || Marker == null || !File.Exists(Marker))
		{
			return false;
		}
		try
		{
			Apply(obj: false);
		}
		catch (Exception ex)
		{
			Brain.Log("her cursor", -1L, Voice.Short(ex.Message));
		}
		Mark(on: false);
		return true;
	}

	private static void Mark(bool on)
	{
		if (Marker == null)
		{
			return;
		}
		try
		{
			if (on)
			{
				File.WriteAllText(Marker, "Capri's cursor is on. Capri puts yours back when she starts.");
			}
			else
			{
				File.Delete(Marker);
			}
		}
		catch (IOException)
		{
		}
		catch (UnauthorizedAccessException)
		{
		}
	}

	internal static IntPtr Copy()
	{
		return CopyImage(Look.Pointer.Handle, 2u, 0, 0, 0u);
	}

	internal static Point Hotspot(IntPtr cursor)
	{
		IconInfo info = default;
		if (cursor == IntPtr.Zero || !GetIconInfo(cursor, ref info))
		{
			return new Point(-1, -1);
		}
		if (info.Mask != IntPtr.Zero)
		{
			DeleteObject(info.Mask);
		}
		if (info.Colour != IntPtr.Zero)
		{
			DeleteObject(info.Colour);
		}
		return new Point(info.X, info.Y);
	}

	internal static void Drop(IntPtr cursor)
	{
		if (cursor != IntPtr.Zero)
		{
			DestroyCursor(cursor);
		}
	}

	private static void Swap(bool on)
	{
		if (!on)
		{
			SystemParametersInfo(87u, 0u, IntPtr.Zero, 0u);
			return;
		}
		uint[] shapes = Shapes;
		foreach (uint id in shapes)
		{
			IntPtr intPtr = Copy();
			if (intPtr != IntPtr.Zero && !SetSystemCursor(intPtr, id))
			{
				DestroyCursor(intPtr);
			}
		}
	}
}
