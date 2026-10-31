using System;
using System.Runtime.InteropServices;

namespace CapriDesktop;

[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("C8ADBD64-E71E-48a0-A4DE-185C395CD317")]
internal interface ISoundCapture
{
	[PreserveSig]
	int GetBuffer(out IntPtr data, out int frames, out int flags, out long position, out long time);

	[PreserveSig]
	int ReleaseBuffer(int frames);

	[PreserveSig]
	int GetNextPacketSize(out int frames);
}
