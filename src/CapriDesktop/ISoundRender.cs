using System;
using System.Runtime.InteropServices;

namespace CapriDesktop;

[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("F294ACFC-3146-4483-A7BF-ADDCA7C260E2")]
internal interface ISoundRender
{
	[PreserveSig]
	int GetBuffer(int frames, out IntPtr data);

	[PreserveSig]
	int ReleaseBuffer(int frames, int flags);
}
