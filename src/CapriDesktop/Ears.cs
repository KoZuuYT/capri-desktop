using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Microsoft.Win32;

namespace CapriDesktop;

internal sealed class Ears : IDisposable
{
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	private struct MicCaps
	{
		internal short Maker;

		internal short Product;

		internal int Driver;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
		internal string Name;

		internal int Formats;

		internal short Channels;

		internal short Reserved;
	}

	internal const int Rate = 16000;

	internal const int Chunk = 1600;

	private const int Buffers = 4;

	internal volatile bool Muted;

	internal volatile string Chosen;

	private IntPtr device;

	private IntPtr[] headers;

	private Thread worker;

	private AutoResetEvent signal;

	private volatile bool running;

	internal bool Listening => running;

	internal event Action<short[]> Heard;

	internal event Action<double> Level;

	[DllImport("winmm.dll")]
	private static extern int waveInOpen(out IntPtr device, int id, ref WaveFormat format, IntPtr callback, IntPtr instance, int flags);

	[DllImport("winmm.dll")]
	private static extern int waveInPrepareHeader(IntPtr device, IntPtr header, int size);

	[DllImport("winmm.dll")]
	private static extern int waveInUnprepareHeader(IntPtr device, IntPtr header, int size);

	[DllImport("winmm.dll")]
	private static extern int waveInAddBuffer(IntPtr device, IntPtr header, int size);

	[DllImport("winmm.dll")]
	private static extern int waveInStart(IntPtr device);

	[DllImport("winmm.dll")]
	private static extern int waveInReset(IntPtr device);

	[DllImport("winmm.dll")]
	private static extern int waveInClose(IntPtr device);

	[DllImport("winmm.dll")]
	private static extern int waveInGetNumDevs();

	[DllImport("winmm.dll", CharSet = CharSet.Unicode)]
	private static extern int waveInGetDevCaps(IntPtr id, ref MicCaps caps, int size);

	[DllImport("winmm.dll")]
	private static extern int waveInMessage(IntPtr device, int message, out int size, IntPtr unused);

	[DllImport("winmm.dll", CharSet = CharSet.Unicode)]
	private static extern int waveInMessage(IntPtr device, int message, StringBuilder text, IntPtr size);

	internal static List<Mic> List()
	{
		List<Mic> list = new List<Mic>();
		int num = waveInGetNumDevs();
		for (int i = 0; i < num; i++)
		{
			MicCaps caps = default;
			if (waveInGetDevCaps(new IntPtr(i), ref caps, Marshal.SizeOf(typeof(MicCaps))) == 0)
			{
				string text = Endpoint(i);
				list.Add(new Mic(text ?? caps.Name, FullName(text) ?? caps.Name));
			}
		}
		return list;
	}

	private static string Endpoint(int index)
	{
		if (waveInMessage(new IntPtr(index), 2066, out var size, IntPtr.Zero) != 0 || size <= 0)
		{
			return null;
		}
		StringBuilder stringBuilder = new StringBuilder(size / 2 + 1);
		if (waveInMessage(new IntPtr(index), 2065, stringBuilder, new IntPtr(size)) != 0)
		{
			return null;
		}
		if (stringBuilder.Length != 0)
		{
			return stringBuilder.ToString();
		}
		return null;
	}

	private static RegistryKey Properties(string id)
	{
		int num = id?.LastIndexOf("}.{", StringComparison.Ordinal) ?? (-1);
		if (num < 0)
		{
			return null;
		}
		return Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\MMDevices\\Audio\\Capture\\" + id.Substring(num + 2) + "\\Properties");
	}

	private static string FullName(string id)
	{
		try
		{
			using RegistryKey registryKey = Properties(id);
			if (registryKey == null)
			{
				return null;
			}
			string text = registryKey.GetValue("{a45c254e-df1c-4efd-8020-67d146a850e0},2") as string;
			string text2 = registryKey.GetValue("{b3f8fa53-0004-438e-9003-51a46e139bfc},6") as string;
			if (string.IsNullOrEmpty(text))
			{
				return string.IsNullOrEmpty(text2) ? null : text2;
			}
			string result;
			if (string.IsNullOrEmpty(text2))
			{
				result = text;
			}
			else
			{
				result = ((text == "Microphone") ? text2 : (text + " (" + text2 + ")"));
			}
			return result;
		}
		catch (Exception)
		{
			return null;
		}
	}

	private static string Opens(string chosen)
	{
		if (Pick(chosen) >= 0)
		{
			return chosen;
		}
		ISoundDevices soundDevices = null;
		ISoundDevice soundDevice = null;
		try
		{
			soundDevices = (ISoundDevices)new SoundDevices();
			if (soundDevices.GetDefaultAudioEndpoint(1, 0, out soundDevice) != 0 || soundDevice == null || soundDevice.GetId(out var id) != 0)
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
			if (soundDevice != null)
			{
				Marshal.ReleaseComObject(soundDevice);
			}
			if (soundDevices != null)
			{
				Marshal.ReleaseComObject(soundDevices);
			}
		}
	}

	internal static string HandsFree(string chosen)
	{
		string id = Opens(chosen);
		try
		{
			using RegistryKey registryKey = Properties(id);
			if (registryKey == null || !string.Equals(registryKey.GetValue("{a45c254e-df1c-4efd-8020-67d146a850e0},24") as string, "BTHHFENUM", StringComparison.OrdinalIgnoreCase))
			{
				return null;
			}
		}
		catch (Exception)
		{
			return null;
		}
		return FullName(id) ?? "Bluetooth headset";
	}

	private static int Pick(string chosen)
	{
		if (string.IsNullOrEmpty(chosen))
		{
			return -1;
		}
		List<Mic> list = List();
		for (int i = 0; i < list.Count; i++)
		{
			if (list[i].Id == chosen)
			{
				return i;
			}
		}
		return -1;
	}

	internal void Start()
	{
		if (!running)
		{
			WaveFormat format = WaveFormat.Mono(16000);
			signal = new AutoResetEvent(initialState: false);
			int num = Marshal.SizeOf(typeof(WaveHeader));
			int num2 = Pick(Chosen);
			IntPtr callback = signal.SafeWaitHandle.DangerousGetHandle();
			if (waveInOpen(out device, num2, ref format, callback, IntPtr.Zero, 327680) != 0 && (num2 == -1 || waveInOpen(out device, -1, ref format, callback, IntPtr.Zero, 327680) != 0))
			{
				signal.Dispose();
				signal = null;
				throw new VoiceException("Can't open your mic. Pick another under VOICE.");
			}
			headers = new IntPtr[4];
			for (int i = 0; i < 4; i++)
			{
				WaveHeader structure = new WaveHeader
				{
					Data = Marshal.AllocHGlobal(3200),
					Length = 3200
				};
				ref IntPtr reference = ref headers[i];
				reference = Marshal.AllocHGlobal(num);
				Marshal.StructureToPtr(structure, headers[i], fDeleteOld: false);
				waveInPrepareHeader(device, headers[i], num);
				waveInAddBuffer(device, headers[i], num);
			}
			running = true;
			waveInStart(device);
			worker = new Thread(Pump);
			worker.IsBackground = true;
			worker.Name = "Capri ears";
			worker.Start();
		}
	}

	internal void Stop()
	{
		if (running)
		{
			running = false;
			signal.Set();
			worker.Join(1000);
			waveInReset(device);
			int size = Marshal.SizeOf(typeof(WaveHeader));
			IntPtr[] array = headers;
			foreach (IntPtr intPtr in array)
			{
				waveInUnprepareHeader(device, intPtr, size);
				Marshal.FreeHGlobal(((WaveHeader)Marshal.PtrToStructure(intPtr, typeof(WaveHeader))).Data);
				Marshal.FreeHGlobal(intPtr);
			}
			waveInClose(device);
			device = IntPtr.Zero;
			headers = null;
			signal.Dispose();
			signal = null;
		}
	}

	public void Dispose()
	{
		Stop();
	}

	private void Pump()
	{
		int size = Marshal.SizeOf(typeof(WaveHeader));
		int ofs = (int)Marshal.OffsetOf(typeof(WaveHeader), "Flags");
		int num = 0;
		try
		{
			while (running)
			{
				signal.WaitOne(250);
				while (running)
				{
					IntPtr intPtr = headers[num];
					WaveHeader waveHeader = (WaveHeader)Marshal.PtrToStructure(intPtr, typeof(WaveHeader));
					if ((waveHeader.Flags & 1) == 0)
					{
						break;
					}
					short[] array = new short[waveHeader.Recorded / 2];
					Marshal.Copy(waveHeader.Data, array, 0, array.Length);
					Marshal.WriteInt32(intPtr, ofs, waveHeader.Flags & -2);
					waveInAddBuffer(device, intPtr, size);
					num = (num + 1) % 4;
					try
					{
						Feed(array);
					}
					catch (Exception)
					{
					}
				}
			}
		}
		catch (Exception)
		{
		}
	}

	internal void Feed(short[] chunk)
	{
		Level?.Invoke(Muted ? 0.0 : Speaker.Loudness(chunk, 0, chunk.Length));
		Heard?.Invoke(Muted ? new short[chunk.Length] : chunk);
	}
}
