using System;
using System.Runtime.InteropServices;

namespace CapriDesktop;

[ComImport]
[Guid("1CB9AD4C-DBFA-4c32-B178-C2F568A703B2")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface ISoundClient
{
	[PreserveSig]
	int Initialize(int shareMode, int streamFlags, long bufferDuration, long periodicity, IntPtr format, IntPtr session);

	[PreserveSig]
	int GetBufferSize(out int frames);

	[PreserveSig]
	int GetStreamLatency(out long latency);

	[PreserveSig]
	int GetCurrentPadding(out int frames);

	[PreserveSig]
	int IsFormatSupported(int shareMode, IntPtr format, out IntPtr closest);

	[PreserveSig]
	int GetMixFormat(out IntPtr format);

	[PreserveSig]
	int GetDevicePeriod(out long normal, out long shortest);

	[PreserveSig]
	int Start();

	[PreserveSig]
	int Stop();

	[PreserveSig]
	int Reset();

	[PreserveSig]
	int SetEventHandle(IntPtr handle);

	[PreserveSig]
	int GetService(ref Guid iid, [MarshalAs(UnmanagedType.IUnknown)] out object service);
}
