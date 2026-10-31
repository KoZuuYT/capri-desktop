using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.Win32;

namespace CapriDesktop;

// the mod: voice broadcast. Her voice (and, if wanted, your mic) also plays into an audio device you pick,
// like a virtual cable, so apps that record from its other side (Discord, OBS, Audacity) hear her as your mic
internal static class VoiceCast
{
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	private struct OutCaps
	{
		internal short Maker;

		internal short Product;

		internal int Driver;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
		internal string Name;

		internal int Formats;

		internal short Channels;

		internal short Reserved;

		internal int Support;
	}

	private const int MicRate = 48000;

	private const int MicChunk = 960;

	private const int MicBuffers = 6;

	private static readonly Regex Virtual = new Regex("cable|vb-audio|voicemeeter|virtual", RegexOptions.IgnoreCase);

	private static readonly object gate = new object();

	private static readonly List<IntPtr> voicePending = new List<IntPtr>();

	private static readonly List<IntPtr> micPending = new List<IntPtr>();

	private static bool on;

	private static bool passMic;

	private static string target;

	private static string micId;

	private static IntPtr voiceOut;

	private static int voiceRate;

	private static IntPtr micIn;

	private static IntPtr micOut;

	private static IntPtr[] micHeaders;

	private static AutoResetEvent micSignal;

	private static Thread micPump;

	private static volatile bool micRunning;

	internal static bool On => on;

	internal static bool MicPassing => micRunning;

	internal static string Problem { get; private set; }

	[DllImport("winmm.dll")]
	private static extern int waveOutGetNumDevs();

	[DllImport("winmm.dll", CharSet = CharSet.Unicode)]
	private static extern int waveOutGetDevCaps(IntPtr id, ref OutCaps caps, int size);

	[DllImport("winmm.dll")]
	private static extern int waveOutMessage(IntPtr device, int message, out int size, IntPtr unused);

	[DllImport("winmm.dll", CharSet = CharSet.Unicode)]
	private static extern int waveOutMessage(IntPtr device, int message, StringBuilder text, IntPtr size);

	[DllImport("winmm.dll")]
	private static extern int waveOutOpen(out IntPtr device, int id, ref WaveFormat format, IntPtr callback, IntPtr instance, int flags);

	[DllImport("winmm.dll")]
	private static extern int waveOutPrepareHeader(IntPtr device, IntPtr header, int size);

	[DllImport("winmm.dll")]
	private static extern int waveOutUnprepareHeader(IntPtr device, IntPtr header, int size);

	[DllImport("winmm.dll")]
	private static extern int waveOutWrite(IntPtr device, IntPtr header, int size);

	[DllImport("winmm.dll")]
	private static extern int waveOutReset(IntPtr device);

	[DllImport("winmm.dll")]
	private static extern int waveOutClose(IntPtr device);

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

	// every playback device Windows has, by its endpoint id so the choice survives plugging things in and out
	internal static List<Mic> List()
	{
		List<Mic> list = new List<Mic>();
		int count = waveOutGetNumDevs();
		for (int i = 0; i < count; i++)
		{
			OutCaps caps = default;
			if (waveOutGetDevCaps(new IntPtr(i), ref caps, Marshal.SizeOf(typeof(OutCaps))) == 0)
			{
				string id = Endpoint(i);
				list.Add(new Mic(id ?? caps.Name, FullName(id) ?? caps.Name));
			}
		}
		return list;
	}

	private static string Endpoint(int index)
	{
		if (waveOutMessage(new IntPtr(index), 2066, out var size, IntPtr.Zero) != 0 || size <= 0)
		{
			return null;
		}
		StringBuilder text = new StringBuilder(size / 2 + 1);
		if (waveOutMessage(new IntPtr(index), 2065, text, new IntPtr(size)) != 0 || text.Length == 0)
		{
			return null;
		}
		return text.ToString();
	}

	private static string FullName(string id)
	{
		int at = id?.LastIndexOf("}.{", StringComparison.Ordinal) ?? (-1);
		if (at < 0)
		{
			return null;
		}
		try
		{
			using RegistryKey key = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\MMDevices\\Audio\\Render\\" + id.Substring(at + 2) + "\\Properties");
			if (key == null)
			{
				return null;
			}
			string name = key.GetValue("{a45c254e-df1c-4efd-8020-67d146a850e0},2") as string;
			string card = key.GetValue("{b3f8fa53-0004-438e-9003-51a46e139bfc},6") as string;
			if (string.IsNullOrEmpty(name))
			{
				return string.IsNullOrEmpty(card) ? null : card;
			}
			return string.IsNullOrEmpty(card) ? name : (name + " (" + card + ")");
		}
		catch (Exception)
		{
			return null;
		}
	}

	internal static int Index(string id)
	{
		if (string.IsNullOrEmpty(id))
		{
			return -1;
		}
		List<Mic> list = List();
		for (int i = 0; i < list.Count; i++)
		{
			if (list[i].Id == id)
			{
				return i;
			}
		}
		return -1;
	}

	internal static string NameOf(string id)
	{
		int index = Index(id);
		return (index < 0) ? null : List()[index].Name;
	}

	// a virtual cable if there is one: the usual place to send her voice
	internal static string Guess()
	{
		foreach (Mic item in List())
		{
			if (Virtual.IsMatch(item.Name))
			{
				return item.Id;
			}
		}
		return null;
	}

	// what to pick as the mic in Discord or a recorder: the other side of the cable
	internal static string OtherSide(string name)
	{
		if (string.IsNullOrEmpty(name))
		{
			return null;
		}
		if (name.IndexOf("CABLE", StringComparison.OrdinalIgnoreCase) >= 0 && name.IndexOf("Input", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return Regex.Replace(Regex.Replace(name, "\\s*\\(.*$", ""), "Input", "Output", RegexOptions.IgnoreCase);
		}
		if (name.IndexOf("Voicemod", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return "Voicemod Virtual Audio Device";
		}
		if (name.IndexOf("Voicemeeter", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return Regex.Replace(Regex.Replace(name, "\\s*\\(.*$", ""), "Input", "Output", RegexOptions.IgnoreCase);
		}
		return null;
	}

	internal static void Apply(bool wanted, string device, bool withMic, string mic)
	{
		lock (gate)
		{
			bool moved = device != target || mic != micId;
			on = wanted;
			target = device;
			passMic = withMic;
			micId = mic;
			Problem = null;
			if (!on || moved)
			{
				CloseVoice();
				StopMic();
			}
			if (!on)
			{
				return;
			}
			if (Index(target) < 0)
			{
				Problem = string.IsNullOrEmpty(target) ? "Pick a device to send her voice into." : "That device is gone. Pick another.";
				return;
			}
			if (passMic && !micRunning)
			{
				StartMic();
			}
			else if (!passMic)
			{
				StopMic();
			}
		}
	}

	// her voice, right as it reaches your speakers
	internal static void Feed(short[] samples, int rate)
	{
		if (!on || samples == null || samples.Length == 0 || rate <= 0)
		{
			return;
		}
		lock (gate)
		{
			if (!on)
			{
				return;
			}
			if (voiceOut != IntPtr.Zero && rate != voiceRate)
			{
				CloseVoice();
			}
			if (voiceOut == IntPtr.Zero)
			{
				int index = Index(target);
				WaveFormat format = WaveFormat.Mono(rate);
				if (index < 0 || waveOutOpen(out voiceOut, index, ref format, IntPtr.Zero, IntPtr.Zero, 0) != 0)
				{
					voiceOut = IntPtr.Zero;
					Problem = (index < 0) ? "That device is gone. Pick another." : "Windows wouldn't open that device.";
					return;
				}
				voiceRate = rate;
			}
			Write(voiceOut, voicePending, samples, samples.Length);
		}
	}

	internal static void Feed(byte[] wav)
	{
		if (!on || wav == null)
		{
			return;
		}
		try
		{
			short[] samples = Voice.Samples(wav, out var rate);
			Feed(samples, rate);
		}
		catch (Exception)
		{
		}
	}

	// she was cut off: stop her voice in the mic too
	internal static void Clear()
	{
		lock (gate)
		{
			if (voiceOut != IntPtr.Zero)
			{
				waveOutReset(voiceOut);
				Sweep(voiceOut, voicePending);
			}
		}
	}

	private static void Write(IntPtr device, List<IntPtr> pending, short[] samples, int count)
	{
		Sweep(device, pending);
		int size = Marshal.SizeOf(typeof(WaveHeader));
		WaveHeader header = new WaveHeader
		{
			Data = Marshal.AllocHGlobal(count * 2),
			Length = count * 2
		};
		Marshal.Copy(samples, 0, header.Data, count);
		IntPtr at = Marshal.AllocHGlobal(size);
		Marshal.StructureToPtr(header, at, fDeleteOld: false);
		waveOutPrepareHeader(device, at, size);
		waveOutWrite(device, at, size);
		pending.Add(at);
	}

	private static void Sweep(IntPtr device, List<IntPtr> pending)
	{
		int size = Marshal.SizeOf(typeof(WaveHeader));
		for (int i = pending.Count - 1; i >= 0; i--)
		{
			WaveHeader header = (WaveHeader)Marshal.PtrToStructure(pending[i], typeof(WaveHeader));
			if ((header.Flags & 1) != 0)
			{
				waveOutUnprepareHeader(device, pending[i], size);
				Marshal.FreeHGlobal(header.Data);
				Marshal.FreeHGlobal(pending[i]);
				pending.RemoveAt(i);
			}
		}
	}

	private static void CloseVoice()
	{
		if (voiceOut == IntPtr.Zero)
		{
			return;
		}
		waveOutReset(voiceOut);
		Sweep(voiceOut, voicePending);
		waveOutClose(voiceOut);
		voiceOut = IntPtr.Zero;
		voiceRate = 0;
	}

	// the mic Capri hears you with, unless it is a virtual cable itself (that would loop back forever)
	private static int MicIndex(out string name)
	{
		name = null;
		string id = micId;
		if (string.IsNullOrEmpty(id))
		{
			ISoundDevices devices = null;
			ISoundDevice device = null;
			try
			{
				devices = (ISoundDevices)new SoundDevices();
				if (devices.GetDefaultAudioEndpoint(1, 0, out device) == 0 && device != null)
				{
					device.GetId(out id);
				}
			}
			catch (Exception)
			{
			}
			finally
			{
				if (device != null)
				{
					Marshal.ReleaseComObject(device);
				}
				if (devices != null)
				{
					Marshal.ReleaseComObject(devices);
				}
			}
		}
		List<Mic> list = Ears.List();
		for (int i = 0; i < list.Count; i++)
		{
			if (list[i].Id == id)
			{
				name = list[i].Name;
				return i;
			}
		}
		return -1;
	}

	private static void StartMic()
	{
		int index = MicIndex(out var name);
		if (name != null && Virtual.IsMatch(name))
		{
			Problem = "Your mic is a virtual cable itself, so only her voice goes in.";
			return;
		}
		int outIndex = Index(target);
		WaveFormat format = WaveFormat.Mono(MicRate);
		if (waveOutOpen(out micOut, outIndex, ref format, IntPtr.Zero, IntPtr.Zero, 0) != 0)
		{
			micOut = IntPtr.Zero;
			Problem = "Windows wouldn't open that device for your mic.";
			return;
		}
		micSignal = new AutoResetEvent(initialState: false);
		IntPtr callback = micSignal.SafeWaitHandle.DangerousGetHandle();
		if (waveInOpen(out micIn, index, ref format, callback, IntPtr.Zero, 327680) != 0 && (index == -1 || waveInOpen(out micIn, -1, ref format, callback, IntPtr.Zero, 327680) != 0))
		{
			micIn = IntPtr.Zero;
			waveOutClose(micOut);
			micOut = IntPtr.Zero;
			micSignal.Dispose();
			micSignal = null;
			Problem = "Couldn't open your mic, so only her voice goes in.";
			return;
		}
		int size = Marshal.SizeOf(typeof(WaveHeader));
		micHeaders = new IntPtr[MicBuffers];
		for (int i = 0; i < MicBuffers; i++)
		{
			WaveHeader header = new WaveHeader
			{
				Data = Marshal.AllocHGlobal(MicChunk * 2),
				Length = MicChunk * 2
			};
			micHeaders[i] = Marshal.AllocHGlobal(size);
			Marshal.StructureToPtr(header, micHeaders[i], fDeleteOld: false);
			waveInPrepareHeader(micIn, micHeaders[i], size);
			waveInAddBuffer(micIn, micHeaders[i], size);
		}
		micRunning = true;
		waveInStart(micIn);
		micPump = new Thread(PumpMic)
		{
			IsBackground = true,
			Name = "Capri broadcast mic"
		};
		micPump.Start();
	}

	private static void PumpMic()
	{
		int size = Marshal.SizeOf(typeof(WaveHeader));
		int flags = (int)Marshal.OffsetOf(typeof(WaveHeader), "Flags");
		int next = 0;
		short[] chunk = new short[MicChunk];
		try
		{
			while (micRunning)
			{
				micSignal.WaitOne(250);
				while (micRunning)
				{
					IntPtr at = micHeaders[next];
					WaveHeader header = (WaveHeader)Marshal.PtrToStructure(at, typeof(WaveHeader));
					if ((header.Flags & 1) == 0)
					{
						break;
					}
					int count = Math.Min(MicChunk, header.Recorded / 2);
					Marshal.Copy(header.Data, chunk, 0, count);
					Marshal.WriteInt32(at, flags, header.Flags & -2);
					waveInAddBuffer(micIn, at, size);
					next = (next + 1) % MicBuffers;
					if (count > 0)
					{
						Write(micOut, micPending, chunk, count);
					}
				}
			}
		}
		catch (Exception)
		{
		}
	}

	private static void StopMic()
	{
		if (!micRunning)
		{
			return;
		}
		micRunning = false;
		micSignal.Set();
		micPump.Join(1000);
		waveInReset(micIn);
		int size = Marshal.SizeOf(typeof(WaveHeader));
		foreach (IntPtr at in micHeaders)
		{
			waveInUnprepareHeader(micIn, at, size);
			Marshal.FreeHGlobal(((WaveHeader)Marshal.PtrToStructure(at, typeof(WaveHeader))).Data);
			Marshal.FreeHGlobal(at);
		}
		waveInClose(micIn);
		micIn = IntPtr.Zero;
		micHeaders = null;
		waveOutReset(micOut);
		Sweep(micOut, micPending);
		waveOutClose(micOut);
		micOut = IntPtr.Zero;
		micSignal.Dispose();
		micSignal = null;
	}
}
