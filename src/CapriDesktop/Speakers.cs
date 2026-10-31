using System;
using System.Runtime.InteropServices;

namespace CapriDesktop;

internal static class Speakers
{
	internal static bool? Muted()
	{
		bool muted = false;
		if (!Use((ISpeakerVolume volume) => volume.GetMute(out muted)))
		{
			return null;
		}
		return muted;
	}

	internal static bool Mute(bool muted)
	{
		return Use((ISpeakerVolume volume) => volume.SetMute(muted, IntPtr.Zero));
	}

	internal static double? Level()
	{
		float level = 0f;
		if (!Use((ISpeakerVolume volume) => volume.GetMasterVolumeLevelScalar(out level)))
		{
			return null;
		}
		return level;
	}

	internal static bool SetLevel(double level)
	{
		return Use((ISpeakerVolume volume) => volume.SetMasterVolumeLevelScalar((float)Math.Max(0.0, Math.Min(1.0, level)), IntPtr.Zero));
	}

	private static bool Use(Func<ISpeakerVolume, int> act)
	{
		ISoundDevices soundDevices = null;
		ISoundDevice device = null;
		object found = null;
		try
		{
			soundDevices = (ISoundDevices)new SoundDevices();
			if (soundDevices.GetDefaultAudioEndpoint(0, 0, out device) != 0 || device == null)
			{
				return false;
			}
			Guid iid = typeof(ISpeakerVolume).GUID;
			if (device.Activate(ref iid, 23, IntPtr.Zero, out found) != 0 || found == null)
			{
				return false;
			}
			return act((ISpeakerVolume)found) == 0;
		}
		catch (Exception)
		{
			return false;
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
