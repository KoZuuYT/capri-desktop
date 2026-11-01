using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;

namespace CapriDesktop;

internal static class Native
{
	internal struct Point
	{
		internal int X;

		internal int Y;

		internal Point(int x, int y)
		{
			X = x;
			Y = y;
		}
	}

	internal struct Size
	{
		internal int Width;

		internal int Height;

		internal Size(int width, int height)
		{
			Width = width;
			Height = height;
		}
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	internal struct Blend
	{
		internal byte Operation;

		internal byte Flags;

		internal byte Alpha;

		internal byte Format;
	}

	private struct InputInfo
	{
		internal int Size;

		internal uint Time;
	}

	internal static bool? FakedGaming;

	// the mod: on Linux she runs under Wine, which has no Windows extras (media sessions, Windows speech, tar.exe)
	// and hears what plays through the Linux sound server instead
	private static bool? wine;

	internal static bool Wine
	{
		get
		{
			if (!wine.HasValue)
			{
				try
				{
					IntPtr ntdll = GetModuleHandle("ntdll.dll");
					wine = ntdll != IntPtr.Zero && GetProcAddress(ntdll, "wine_get_version") != IntPtr.Zero;
				}
				catch (Exception)
				{
					wine = false;
				}
			}
			return wine.Value;
		}
	}

	[DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
	private static extern IntPtr GetModuleHandle(string name);

	[DllImport("kernel32.dll", CharSet = CharSet.Ansi)]
	private static extern IntPtr GetProcAddress(IntPtr module, string name);

	[DllImport("user32.dll")]
	private static extern bool GetLastInputInfo(ref InputInfo info);

	[DllImport("user32.dll")]
	internal static extern IntPtr GetForegroundWindow();

	[DllImport("user32.dll")]
	internal static extern int GetWindowThreadProcessId(IntPtr window, out int process);

	[DllImport("user32.dll")]
	internal static extern IntPtr GetWindow(IntPtr window, int command);

	[DllImport("user32.dll")]
	internal static extern bool SetWindowPos(IntPtr window, IntPtr after, int x, int y, int width, int height, int flags);

	[DllImport("user32.dll")]
	internal static extern bool SetProcessDPIAware();

	[DllImport("user32.dll")]
	internal static extern bool SetProcessDpiAwarenessContext(IntPtr context);

	[DllImport("user32.dll")]
	internal static extern int GetWindowLong(IntPtr window, int index);

	[DllImport("user32.dll")]
	internal static extern int SetWindowLong(IntPtr window, int index, int value);

	[DllImport("user32.dll", SetLastError = true)]
	internal static extern bool RegisterHotKey(IntPtr window, int id, int modifiers, int key);

	[DllImport("user32.dll")]
	internal static extern bool UnregisterHotKey(IntPtr window, int id);

	[DllImport("user32.dll")]
	internal static extern short GetAsyncKeyState(int key);

	[DllImport("user32.dll")]
	internal static extern short GetKeyState(int key);

	private struct GuiThreadInfo
	{
		internal int Size;

		internal int Flags;

		internal IntPtr Active;

		internal IntPtr Focus;

		internal IntPtr Capture;

		internal IntPtr MenuOwner;

		internal IntPtr MoveSize;

		internal IntPtr Caret;

		internal int CaretLeft;

		internal int CaretTop;

		internal int CaretRight;

		internal int CaretBottom;
	}

	[DllImport("user32.dll")]
	private static extern bool GetGUIThreadInfo(int thread, ref GuiThreadInfo info);

	// mod: true while a window of the app in front holds the mouse, as it does all through a drag
	internal static bool MouseCaptured()
	{
		GuiThreadInfo info = new GuiThreadInfo
		{
			Size = Marshal.SizeOf(typeof(GuiThreadInfo))
		};
		return GetGUIThreadInfo(0, ref info) && info.Capture != IntPtr.Zero;
	}

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	internal static extern int MapVirtualKey(int code, int type);

	[DllImport("user32.dll")]
	internal static extern IntPtr WindowFromPoint(Point point);

	[DllImport("user32.dll")]
	internal static extern IntPtr GetAncestor(IntPtr window, int flags);

	[DllImport("user32.dll")]
	internal static extern void keybd_event(byte key, byte scan, int flags, UIntPtr extra);

	[DllImport("user32.dll")]
	internal static extern int GetDpiForWindow(IntPtr window);

	[DllImport("user32.dll")]
	internal static extern bool AllowSetForegroundWindow(int process);

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	internal static extern IntPtr SendMessage(IntPtr window, int message, IntPtr wParam, string lParam);

	[DllImport("user32.dll")]
	internal static extern IntPtr SendMessage(IntPtr window, int message, IntPtr wParam, IntPtr lParam);

	[DllImport("user32.dll")]
	internal static extern int MsgWaitForMultipleObjectsEx(int count, IntPtr[] handles, int milliseconds, int wake, int flags);

	[DllImport("user32.dll")]
	private static extern IntPtr GetDC(IntPtr window);

	[DllImport("user32.dll")]
	private static extern int ReleaseDC(IntPtr window, IntPtr dc);

	[DllImport("gdi32.dll")]
	private static extern IntPtr CreateCompatibleDC(IntPtr dc);

	[DllImport("gdi32.dll")]
	private static extern bool DeleteDC(IntPtr dc);

	[DllImport("gdi32.dll")]
	private static extern IntPtr SelectObject(IntPtr dc, IntPtr value);

	[DllImport("gdi32.dll")]
	private static extern bool DeleteObject(IntPtr value);

	[DllImport("user32.dll", SetLastError = true)]
	private static extern bool UpdateLayeredWindow(IntPtr window, IntPtr screen, ref Point destination, ref Size size, IntPtr source, ref Point origin, int colorKey, ref Blend blend, int flags);

	[DllImport("shell32.dll")]
	private static extern int SHQueryUserNotificationState(out int state);

	[DllImport("kernel32.dll")]
	private static extern bool GetSystemTimes(out long idle, out long kernel, out long user);

	internal static bool CpuTimes(out long idle, out long total)
	{
		bool systemTimes = GetSystemTimes(out idle, out var kernel, out var user);
		total = kernel + user;
		return systemTimes;
	}

	internal static long IdleMilliseconds()
	{
		InputInfo info = new InputInfo
		{
			Size = Marshal.SizeOf(typeof(InputInfo))
		};
		return (uint)(GetLastInputInfo(ref info) ? (Environment.TickCount - (int)info.Time) : 0);
	}

	internal static bool Gaming()
	{
		if (FakedGaming.HasValue)
		{
			return FakedGaming.Value;
		}
		try
		{
			int state;
			return SHQueryUserNotificationState(out state) == 0 && (state == 2 || state == 3);
		}
		catch (EntryPointNotFoundException)
		{
			return false;
		}
	}

	internal static void DrawLayered(IntPtr window, Bitmap bitmap, System.Drawing.Point location)
	{
		IntPtr dC = GetDC(IntPtr.Zero);
		IntPtr intPtr = CreateCompatibleDC(dC);
		IntPtr hbitmap = bitmap.GetHbitmap(Color.FromArgb(0));
		IntPtr value = SelectObject(intPtr, hbitmap);
		try
		{
			Point destination = new Point(location.X, location.Y);
			Point origin = new Point(0, 0);
			Size size = new Size(bitmap.Width, bitmap.Height);
			Blend blend = new Blend
			{
				Operation = 0,
				Flags = 0,
				Alpha = byte.MaxValue,
				Format = 1
			};
			if (!UpdateLayeredWindow(window, dC, ref destination, ref size, intPtr, ref origin, 0, ref blend, 2))
			{
				throw new Win32Exception(Marshal.GetLastWin32Error());
			}
		}
		finally
		{
			SelectObject(intPtr, value);
			DeleteObject(hbitmap);
			DeleteDC(intPtr);
			ReleaseDC(IntPtr.Zero, dC);
		}
	}
}
