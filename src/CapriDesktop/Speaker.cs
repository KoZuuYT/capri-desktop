using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace CapriDesktop;

internal sealed class Speaker : IDisposable
{
	internal static bool SkipWave;

	private static bool fellBack;

	private readonly bool silent;

	internal bool Plain;

	private readonly List<IntPtr> pending = new List<IntPtr>();

	private readonly List<int> shapes = new List<int>();

	private readonly List<short> leftover = new List<short>();

	private IntPtr device;

	private SoundOut modern;

	private int rate;

	private double loudest = 0.05;

	private long began;

	private long until;

	internal bool Modern => modern != null;

	internal long ModernFed
	{
		get
		{
			if (modern != null)
			{
				return Interlocked.Read(ref modern.Fed);
			}
			return -1L;
		}
	}

	internal bool Busy => Stopwatch.GetTimestamp() < Interlocked.Read(ref until) + Stopwatch.Frequency * 35 / 100;

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

	internal Speaker(bool quiet)
	{
		silent = quiet;
	}

	internal static double Loudness(short[] samples, int start, int count)
	{
		if (count <= 0)
		{
			return 0.0;
		}
		double num = 0.0;
		for (int i = start; i < start + count; i++)
		{
			num += (double)samples[i] * (double)samples[i];
		}
		return Math.Sqrt(num / (double)count) / 32768.0;
	}

	internal void Open(int sampleRate)
	{
		if (sampleRate == rate && (silent || device != IntPtr.Zero || (modern != null && modern.Alive)))
		{
			return;
		}
		Close();
		rate = sampleRate;
		if (silent)
		{
			return;
		}
		WaveFormat format = WaveFormat.Mono(rate);
		int num = (SkipWave ? 6 : waveOutOpen(out device, -1, ref format, IntPtr.Zero, IntPtr.Zero, 0));
		if (num == 0)
		{
			return;
		}
		device = IntPtr.Zero;
		try
		{
			modern = new SoundOut(rate);
			if (!fellBack && !SkipWave)
			{
				Brain.Log("Speakers", -1L, "Windows' wave API wouldn't open the speakers (error " + num + "), so she plays through WASAPI.");
			}
			fellBack = !SkipWave;
			return;
		}
		catch (Exception ex)
		{
			Brain.Log("Speakers", -1L, "Windows' wave API wouldn't open the speakers (error " + num + ") and nor would WASAPI: " + ex.Message);
		}
		throw new VoiceException(Speakers(num));
	}

	internal static string Speakers(int failed)
	{
		switch (failed)
		{
		case 2:
		case 6:
			return "Can't open your speakers: Windows has no speakers or headphones on. Pick one in Settings > System > Sound.";
		case 4:
			return "Can't open your speakers: another app has them to itself. Close it, or untick exclusive mode in your speaker's Sound settings.";
		default:
			return "Can't open your speakers (Windows error " + failed + "). Check Settings > System > Sound, then try again.";
		}
	}

	internal void Play(short[] samples)
	{
		if (rate == 0 || samples.Length == 0)
		{
			return;
		}
		Sweep();
		long timestamp = Stopwatch.GetTimestamp();
		if (timestamp >= Interlocked.Read(ref until))
		{
			began = timestamp;
			shapes.Clear();
			leftover.Clear();
			Interlocked.Exchange(ref until, timestamp);
		}
		Interlocked.Add(ref until, samples.Length * Stopwatch.Frequency / rate);
		leftover.AddRange(samples);
		int num = Math.Max(1, rate / 12);
		short[] array = leftover.ToArray();
		int i;
		for (i = 0; i + num <= array.Length; i += num)
		{
			double num2 = Loudness(array, i, num);
			loudest = Math.Max(loudest * 0.995, num2);
			int count = shapes.Count;
			List<int> list = shapes;
			int item;
			if (num2 <= Math.Max(0.004, loudest * 0.08))
			{
				item = -1;
			}
			else if (num2 < loudest * 0.3)
			{
				item = ((count % 3 != 0) ? 1 : 0);
			}
			else if (num2 < loudest * 0.55)
			{
				item = ((count % 2 != 1) ? 1 : 2);
			}
			else
			{
				item = ((num2 < loudest * 0.85) ? 3 : 4);
			}
			list.Add(item);
		}
		leftover.RemoveRange(0, i);
		if (!silent && (!(device == IntPtr.Zero) || modern != null))
		{
			if (!Plain)
			{
				samples = Voice.Louder(samples);
			}
			if (modern != null)
			{
				modern.Write(samples);
				return;
			}
			int num3 = Marshal.SizeOf(typeof(WaveHeader));
			WaveHeader structure = new WaveHeader
			{
				Data = Marshal.AllocHGlobal(samples.Length * 2),
				Length = samples.Length * 2
			};
			Marshal.Copy(samples, 0, structure.Data, samples.Length);
			IntPtr intPtr = Marshal.AllocHGlobal(num3);
			Marshal.StructureToPtr(structure, intPtr, fDeleteOld: false);
			waveOutPrepareHeader(device, intPtr, num3);
			waveOutWrite(device, intPtr, num3);
			pending.Add(intPtr);
		}
	}

	internal int Shape()
	{
		long timestamp = Stopwatch.GetTimestamp();
		if (timestamp >= Interlocked.Read(ref until) || shapes.Count == 0)
		{
			return -1;
		}
		int num = (int)((timestamp - began) * 12 / Stopwatch.Frequency);
		if (num < 0 || num >= shapes.Count)
		{
			return -1;
		}
		return shapes[num];
	}

	internal void Clear()
	{
		if (device != IntPtr.Zero)
		{
			waveOutReset(device);
		}
		if (modern != null)
		{
			modern.Clear();
		}
		Sweep();
		shapes.Clear();
		leftover.Clear();
		Interlocked.Exchange(ref until, 0L);
	}

	private void Sweep()
	{
		int size = Marshal.SizeOf(typeof(WaveHeader));
		for (int num = pending.Count - 1; num >= 0; num--)
		{
			WaveHeader waveHeader = (WaveHeader)Marshal.PtrToStructure(pending[num], typeof(WaveHeader));
			if ((waveHeader.Flags & 1) != 0)
			{
				waveOutUnprepareHeader(device, pending[num], size);
				Marshal.FreeHGlobal(waveHeader.Data);
				Marshal.FreeHGlobal(pending[num]);
				pending.RemoveAt(num);
			}
		}
	}

	internal void Close()
	{
		Clear();
		if (device != IntPtr.Zero)
		{
			waveOutClose(device);
		}
		device = IntPtr.Zero;
		if (modern != null)
		{
			modern.Dispose();
		}
		modern = null;
		rate = 0;
	}

	public void Dispose()
	{
		Close();
	}
}
