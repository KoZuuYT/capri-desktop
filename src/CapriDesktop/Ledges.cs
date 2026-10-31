using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Forms;

namespace CapriDesktop;

internal static class Ledges
{
	private delegate bool Visit(IntPtr window, IntPtr data);

	private struct Box
	{
		internal int Left;

		internal int Top;

		internal int Right;

		internal int Bottom;
	}

	private sealed class Shown
	{
		internal IntPtr Window;

		internal Rectangle Bounds;

		internal bool Seat;
	}

	private const int IconInset = 2;

	private static readonly string[] Shell = new string[6] { "Progman", "WorkerW", "Shell_TrayWnd", "Shell_SecondaryTrayWnd", "#32768", "tooltips_class32" };

	private static readonly object ownLock = new object();

	private static IntPtr ownSeat;

	private static Rectangle ownPlate;

	[DllImport("user32.dll")]
	private static extern bool EnumWindows(Visit visit, IntPtr data);

	[DllImport("user32.dll")]
	private static extern bool IsWindow(IntPtr window);

	[DllImport("user32.dll")]
	private static extern bool IsWindowVisible(IntPtr window);

	[DllImport("user32.dll")]
	private static extern bool IsIconic(IntPtr window);

	[DllImport("user32.dll")]
	private static extern bool IsZoomed(IntPtr window);

	[DllImport("user32.dll")]
	private static extern bool GetWindowRect(IntPtr window, out Box box);

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	private static extern int GetClassName(IntPtr window, StringBuilder name, int size);

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	private static extern IntPtr FindWindow(string type, string title);

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	private static extern IntPtr FindWindowEx(IntPtr parent, IntPtr after, string type, string title);

	[DllImport("dwmapi.dll")]
	private static extern int DwmGetWindowAttribute(IntPtr window, int attribute, out Box value, int size);

	[DllImport("dwmapi.dll")]
	private static extern int DwmGetWindowAttribute(IntPtr window, int attribute, out int value, int size);

	internal static void OwnSeat(IntPtr window, Rectangle plate)
	{
		lock (ownLock)
		{
			ownSeat = window;
			ownPlate = plate;
		}
	}

	private static bool Own(IntPtr window, out Rectangle plate)
	{
		lock (ownLock)
		{
			plate = ownPlate;
			return window != IntPtr.Zero && window == ownSeat && plate.Width > 0 && plate.Height > 0;
		}
	}

	private static string Kind(IntPtr window)
	{
		StringBuilder stringBuilder = new StringBuilder(128);
		GetClassName(window, stringBuilder, stringBuilder.Capacity);
		return stringBuilder.ToString();
	}

	internal static bool Showing(IntPtr window, out Rectangle bounds)
	{
		bounds = Rectangle.Empty;
		if (!IsWindow(window) || !IsWindowVisible(window) || IsIconic(window))
		{
			return false;
		}
		if (Own(window, out var plate))
		{
			bounds = plate;
			return true;
		}
		if (DwmGetWindowAttribute(window, 14, out int value, 4) == 0 && value != 0)
		{
			return false;
		}
		int windowLong = Native.GetWindowLong(window, -20);
		if ((windowLong & 0x20) != 0 || (windowLong & 0x8080000) == 134742016)
		{
			return false;
		}
		if (Array.IndexOf(Shell, Kind(window)) >= 0)
		{
			return false;
		}
		if (DwmGetWindowAttribute(window, 9, out Box value2, 16) != 0 && !GetWindowRect(window, out value2))
		{
			return false;
		}
		bounds = Rectangle.FromLTRB(value2.Left, value2.Top, value2.Right, value2.Bottom);
		if (bounds.Width > 0)
		{
			return bounds.Height > 0;
		}
		return false;
	}

	internal static bool Alive(IntPtr window)
	{
		return IsWindow(window);
	}

	private static bool Solid(IntPtr window, Rectangle bounds)
	{
		int windowLong = Native.GetWindowLong(window, -16);
		int windowLong2 = Native.GetWindowLong(window, -20);
		if ((windowLong2 & 0x80) != 0 && (windowLong2 & 0x40000) == 0)
		{
			return false;
		}
		if ((windowLong & 0xC00000) != 12582912)
		{
			return false;
		}
		if (bounds.Width >= 120)
		{
			return bounds.Height >= 60;
		}
		return false;
	}

	internal static bool Seatable(IntPtr window, Rectangle bounds)
	{
		if (Own(window, out var _))
		{
			return true;
		}
		int windowLong = Native.GetWindowLong(window, -20);
		if ((windowLong & 0x80) != 0 && (windowLong & 0x40000) == 0)
		{
			return false;
		}
		if (IsZoomed(window) || bounds.Width < 120 || bounds.Height < 60)
		{
			return false;
		}
		return !bounds.Contains(Screen.FromRectangle(bounds).Bounds);
	}

	private static List<Shown> Windows(ICollection<IntPtr> skip)
	{
		List<Shown> found = new List<Shown>();
		Visit visit = (IntPtr window, IntPtr data) =>
		{
			if (!skip.Contains(window) && Showing(window, out var bounds))
			{
				found.Add(new Shown
				{
					Window = window,
					Bounds = bounds,
					Seat = Seatable(window, bounds)
				});
			}
			return true;
		};
		EnumWindows(visit, IntPtr.Zero);
		GC.KeepAlive(visit);
		return found;
	}

	internal static List<int[]> Open(int left, int right, int y, IEnumerable<Rectangle> above)
	{
		List<int[]> list = new List<int[]>();
		list.Add(new int[2] { left, right });
		List<int[]> list2 = list;
		foreach (Rectangle item in above)
		{
			if (y < item.Top || y >= item.Bottom)
			{
				continue;
			}
			List<int[]> list3 = new List<int[]>();
			foreach (int[] item2 in list2)
			{
				if (item.Right <= item2[0] || item.Left >= item2[1])
				{
					list3.Add(item2);
					continue;
				}
				if (item.Left > item2[0])
				{
					list3.Add(new int[2]
					{
						item2[0],
						item.Left
					});
				}
				if (item.Right < item2[1])
				{
					list3.Add(new int[2]
					{
						item.Right,
						item2[1]
					});
				}
			}
			list2 = list3;
		}
		return list2;
	}

	internal static List<Ledge> Find(ICollection<IntPtr> skip, IEnumerable<Rectangle> icons, IntPtr under = default(IntPtr))
	{
		List<Ledge> list = new List<Ledge>();
		List<Rectangle> list2 = new List<Rectangle>();
		foreach (Shown item in Windows(skip))
		{
			if (item.Seat)
			{
				foreach (int[] item2 in Open(item.Bounds.Left, item.Bounds.Right, item.Bounds.Top, list2))
				{
					list.Add(new Ledge
					{
						Window = item.Window,
						Bounds = item.Bounds,
						Left = item2[0],
						Right = item2[1],
						Y = item.Bounds.Top
					});
				}
			}
			if (item.Window != under)
			{
				list2.Add(item.Bounds);
			}
		}
		foreach (Rectangle icon in icons)
		{
			foreach (int[] item3 in Open(icon.Left, icon.Right, icon.Top + 2, list2))
			{
				list.Add(new Ledge
				{
					Icon = icon,
					Left = item3[0],
					Right = item3[1],
					Y = icon.Top + 2
				});
			}
		}
		Screen[] allScreens = Screen.AllScreens;
		foreach (Screen screen in allScreens)
		{
			list.Add(new Ledge
			{
				Floor = true,
				Left = screen.WorkingArea.Left,
				Right = screen.WorkingArea.Right,
				Y = screen.WorkingArea.Bottom
			});
		}
		return list;
	}

	internal static Ledge Below(IEnumerable<Ledge> ledges, System.Drawing.Point hip, int over, int reach, int slack, int room)
	{
		Ledge ledge = null;
		foreach (Ledge ledge2 in ledges)
		{
			int num = ledge2.Y - hip.Y;
			if (num >= -over && num <= reach && ledge2.Right - ledge2.Left >= 24 && hip.X >= ledge2.Left - slack && hip.X <= ledge2.Right + slack && ledge2.Y - room >= Screen.FromPoint(new System.Drawing.Point(hip.X, ledge2.Y)).Bounds.Top && (ledge == null || Math.Abs(num) < Math.Abs(ledge.Y - hip.Y)))
			{
				ledge = ledge2;
			}
		}
		return ledge;
	}

	internal static bool Covered(Ledge seat, int x, ICollection<IntPtr> skip)
	{
		bool covered = false;
		Visit visit = (IntPtr window, IntPtr data) =>
		{
			if (window == seat.Window)
			{
				return false;
			}
			if (!skip.Contains(window) && Showing(window, out var bounds) && Solid(window, bounds) && x >= bounds.Left && x < bounds.Right && seat.Y >= bounds.Top && seat.Y < bounds.Bottom)
			{
				covered = true;
				return false;
			}
			return true;
		};
		EnumWindows(visit, IntPtr.Zero);
		GC.KeepAlive(visit);
		return covered;
	}

	private static IntPtr DesktopList()
	{
		IntPtr view = FindWindowEx(FindWindow("Progman", null), IntPtr.Zero, "SHELLDLL_DefView", null);
		if (view == IntPtr.Zero)
		{
			Visit visit = (IntPtr window, IntPtr data) =>
			{
				if (Kind(window) == "WorkerW")
				{
					view = FindWindowEx(window, IntPtr.Zero, "SHELLDLL_DefView", null);
				}
				return view == IntPtr.Zero;
			};
			EnumWindows(visit, IntPtr.Zero);
			GC.KeepAlive(visit);
		}
		if (!(view == IntPtr.Zero))
		{
			return FindWindowEx(view, IntPtr.Zero, "SysListView32", null);
		}
		return IntPtr.Zero;
	}

	internal static List<Rectangle> Icons()
	{
		List<Rectangle> list = new List<Rectangle>();
		IntPtr intPtr = DesktopList();
		if (intPtr == IntPtr.Zero || !IsWindowVisible(intPtr))
		{
			return list;
		}
		try
		{
			CacheRequest cacheRequest = new CacheRequest();
			cacheRequest.Add(AutomationElement.BoundingRectangleProperty);
			using (cacheRequest.Activate())
			{
				AutomationElementCollection automationElementCollection = AutomationElement.FromHandle(intPtr).FindAll(TreeScope.Children, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.ListItem));
				foreach (AutomationElement item in automationElementCollection)
				{
					Rect boundingRectangle = item.Cached.BoundingRectangle;
					if (!boundingRectangle.IsEmpty && boundingRectangle.Width >= 16.0 && boundingRectangle.Height >= 16.0)
					{
						list.Add(new Rectangle((int)boundingRectangle.X, (int)boundingRectangle.Y, (int)boundingRectangle.Width, (int)boundingRectangle.Height));
					}
				}
			}
		}
		catch (ElementNotAvailableException)
		{
		}
		catch (InvalidOperationException)
		{
		}
		catch (COMException)
		{
		}
		return list;
	}
}
