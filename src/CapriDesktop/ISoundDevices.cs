using System;
using System.Runtime.InteropServices;

namespace CapriDesktop;

[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("A95664D2-9614-4F35-A746-DE8DB63617E6")]
internal interface ISoundDevices
{
	[PreserveSig]
	int EnumAudioEndpoints(int flow, int states, out IntPtr devices);

	[PreserveSig]
	int GetDefaultAudioEndpoint(int flow, int role, out ISoundDevice device);

	[PreserveSig]
	int GetDevice([MarshalAs(UnmanagedType.LPWStr)] string id, out ISoundDevice device);
}
