using System;
using System.Runtime.InteropServices;

namespace CapriDesktop;

[ComImport]
[Guid("D666063F-1587-4E43-81F1-B948E807363F")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface ISoundDevice
{
	[PreserveSig]
	int Activate(ref Guid iid, int context, IntPtr parameters, [MarshalAs(UnmanagedType.IUnknown)] out object found);

	[PreserveSig]
	int OpenPropertyStore(int access, out IntPtr store);

	[PreserveSig]
	int GetId([MarshalAs(UnmanagedType.LPWStr)] out string id);
}
