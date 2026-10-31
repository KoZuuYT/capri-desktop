using System;
using System.Runtime.InteropServices;

namespace CapriDesktop;

internal sealed class SoundTap : IDisposable
{
	internal readonly string Device;

	internal readonly int Rate;

	internal readonly int Channels;

	internal readonly int Bits;

	internal readonly bool Float;

	private readonly ISoundClient client;

	private readonly ISoundCapture capture;

	private float[] floats = new float[0];

	private short[] shorts = new short[0];

	private int[] ints = new int[0];

	internal string Format => Rate + " Hz, " + Channels + " channels, " + Bits + "-bit " + (Float ? "float" : "PCM");

	private SoundTap(string device, ISoundClient client, ISoundCapture capture, int rate, int channels, int bits, bool isFloat)
	{
		Device = device;
		this.client = client;
		this.capture = capture;
		Rate = rate;
		Channels = channels;
		Bits = bits;
		Float = isFloat;
	}

	internal static string DefaultDevice()
	{
		ISoundDevices soundDevices = null;
		ISoundDevice device = null;
		try
		{
			soundDevices = (ISoundDevices)new SoundDevices();
			if (soundDevices.GetDefaultAudioEndpoint(0, 1, out device) != 0 || device == null || device.GetId(out var id) != 0)
			{
				return null;
			}
			return id;
		}
		catch (Exception)
		{
			return null;
		}
		finally
		{
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

	internal static SoundTap Open(bool start)
	{
		ISoundDevices soundDevices = null;
		ISoundDevice device = null;
		ISoundClient soundClient = null;
		IntPtr format = IntPtr.Zero;
		try
		{
			soundDevices = (ISoundDevices)new SoundDevices();
			if (soundDevices.GetDefaultAudioEndpoint(0, 1, out device) != 0 || device == null || device.GetId(out var id) != 0)
			{
				return null;
			}
			Guid iid = typeof(ISoundClient).GUID;
			if (device.Activate(ref iid, 23, IntPtr.Zero, out var found) != 0)
			{
				return null;
			}
			soundClient = (ISoundClient)found;
			if (soundClient.GetMixFormat(out format) != 0 || format == IntPtr.Zero)
			{
				return null;
			}
			int num = Marshal.ReadInt16(format, 0) & 0xFFFF;
			int num2 = Marshal.ReadInt16(format, 2);
			int num3 = Marshal.ReadInt32(format, 4);
			int num4 = Marshal.ReadInt16(format, 14);
			if (num == 65534)
			{
				num = Marshal.ReadInt32(format, 24);
			}
			bool flag = num == 3 && num4 == 32;
			if ((!flag && (num != 1 || (num4 != 16 && num4 != 32))) || num2 < 1 || num3 < 8000)
			{
				return null;
			}
			if (soundClient.Initialize(0, 131072, 1000000L, 0L, format, IntPtr.Zero) != 0)
			{
				return null;
			}
			Guid iid2 = typeof(ISoundCapture).GUID;
			if (soundClient.GetService(ref iid2, out var service) != 0)
			{
				return null;
			}
			if (start && soundClient.Start() != 0)
			{
				Marshal.ReleaseComObject(service);
				return null;
			}
			SoundTap result = new SoundTap(id, soundClient, (ISoundCapture)service, num3, num2, num4, flag);
			soundClient = null;
			return result;
		}
		catch (Exception)
		{
			return null;
		}
		finally
		{
			if (format != IntPtr.Zero)
			{
				Marshal.FreeCoTaskMem(format);
			}
			if (soundClient != null)
			{
				Marshal.ReleaseComObject(soundClient);
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

	internal bool Read(Action<float> push, Onset onset, out long newest)
	{
		newest = 0L;
		while (true)
		{
			if (capture.GetNextPacketSize(out var frames) != 0)
			{
				return false;
			}
			if (frames == 0)
			{
				return true;
			}
			if (capture.GetBuffer(out var data, out var frames2, out var flags, out var _, out var time) != 0)
			{
				break;
			}
			int num = frames2 * Channels;
			bool flag = (flags & 2) != 0 || data == IntPtr.Zero;
			if (!flag && Float)
			{
				if (floats.Length < num)
				{
					floats = new float[num];
				}
				Marshal.Copy(data, floats, 0, num);
			}
			else if (!flag && Bits == 16)
			{
				if (shorts.Length < num)
				{
					shorts = new short[num];
				}
				Marshal.Copy(data, shorts, 0, num);
			}
			else if (!flag)
			{
				if (ints.Length < num)
				{
					ints = new int[num];
				}
				Marshal.Copy(data, ints, 0, num);
			}
			for (int i = 0; i < frames2; i++)
			{
				double num2 = 0.0;
				if (!flag)
				{
					for (int j = i * Channels; j < (i + 1) * Channels; j++)
					{
						num2 += (Float ? ((double)floats[j]) : ((Bits == 16) ? ((double)shorts[j] / 32768.0) : ((double)ints[j] / 2147483648.0)));
					}
				}
				if (onset.Add(num2 / (double)Channels, out var strength))
				{
					push(strength);
				}
			}
			capture.ReleaseBuffer(frames2);
			if (time > 0)
			{
				newest = time + (long)((double)frames2 * 10000000.0 / (double)Rate);
			}
		}
		return false;
	}

	public void Dispose()
	{
		try
		{
			client.Stop();
		}
		catch (Exception)
		{
		}
		try
		{
			Marshal.ReleaseComObject(capture);
		}
		catch (Exception)
		{
		}
		try
		{
			Marshal.ReleaseComObject(client);
		}
		catch (Exception)
		{
		}
	}
}
