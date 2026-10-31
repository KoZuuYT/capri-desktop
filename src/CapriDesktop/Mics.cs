using System;
using System.Runtime.InteropServices;

namespace CapriDesktop;

internal static class Mics
{
	internal static bool? Muted(string id)
	{
		ISoundDevices soundDevices = null;
		ISoundDevice device = null;
		object found = null;
		try
		{
			soundDevices = (ISoundDevices)new SoundDevices();
			if (string.IsNullOrEmpty(id) || soundDevices.GetDevice(id, out device) != 0 || device == null)
			{
				if (device != null)
				{
					Marshal.ReleaseComObject(device);
				}
				device = null;
				if (soundDevices.GetDefaultAudioEndpoint(1, 0, out device) != 0 || device == null)
				{
					return null;
				}
			}
			Guid iid = typeof(ISpeakerVolume).GUID;
			if (device.Activate(ref iid, 23, IntPtr.Zero, out found) != 0 || found == null)
			{
				return null;
			}
			bool mute;
			return (((ISpeakerVolume)found).GetMute(out mute) == 0) ? new bool?(mute) : ((bool?)null);
		}
		catch (Exception)
		{
			return null;
		}
		finally
		{
			if (found != null)
			{
				Marshal.ReleaseComObject(found);
			}
			if (device != null)
			{
				Marshal.ReleaseComObject(device);
			}
			if (soundDevices != null)
			{
				Marshal.ReleaseComObject(soundDevices);
			}
		}
	}
}
