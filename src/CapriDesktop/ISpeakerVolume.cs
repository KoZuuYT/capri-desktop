using System;
using System.Runtime.InteropServices;

namespace CapriDesktop;

[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("5CDF2C82-841E-4546-9722-0CF74078229A")]
internal interface ISpeakerVolume
{
	[PreserveSig]
	int RegisterControlChangeNotify(IntPtr notify);

	[PreserveSig]
	int UnregisterControlChangeNotify(IntPtr notify);

	[PreserveSig]
	int GetChannelCount(out int count);

	[PreserveSig]
	int SetMasterVolumeLevel(float decibels, IntPtr context);

	[PreserveSig]
	int SetMasterVolumeLevelScalar(float level, IntPtr context);

	[PreserveSig]
	int GetMasterVolumeLevel(out float decibels);

	[PreserveSig]
	int GetMasterVolumeLevelScalar(out float level);

	[PreserveSig]
	int SetChannelVolumeLevel(int channel, float decibels, IntPtr context);

	[PreserveSig]
	int SetChannelVolumeLevelScalar(int channel, float level, IntPtr context);

	[PreserveSig]
	int GetChannelVolumeLevel(int channel, out float decibels);

	[PreserveSig]
	int GetChannelVolumeLevelScalar(int channel, out float level);

	[PreserveSig]
	int SetMute([MarshalAs(UnmanagedType.Bool)] bool mute, IntPtr context);

	[PreserveSig]
	int GetMute([MarshalAs(UnmanagedType.Bool)] out bool mute);
}
