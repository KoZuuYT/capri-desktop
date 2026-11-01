using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace CapriDesktop;

// mod: a live mixer for her sound effects. The plain speaker plays sounds one after another, so in a burst (the eye
// game, the eye swarm) they pile up in a queue and fall behind what's on screen. Here every sound starts the moment
// it's played and is mixed over the ones still ringing, written to the speakers a few milliseconds at a time.
internal sealed class SfxMixer : IDisposable
{
	private const int Blocks = 4;

	private const int BlockMs = 15;

	private sealed class Voice
	{
		internal short[] Samples;

		internal int At;
	}

	private readonly object gate = new object();

	private readonly List<Voice> voices = new List<Voice>();

	private readonly int rate;

	private readonly int block;

	private readonly IntPtr device;

	private readonly IntPtr[] headers = new IntPtr[Blocks];

	private readonly AutoResetEvent done;

	private readonly Thread feeder;

	private readonly int flagsAt = (int)Marshal.OffsetOf(typeof(WaveHeader), "Flags");

	private volatile bool stopped;

	private volatile bool broken;

	// false once the feeder thread has died; sounds then go to the plain speaker instead
	internal bool Alive => !stopped && !broken;

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

	private SfxMixer(int rate, IntPtr device, AutoResetEvent done)
	{
		this.rate = rate;
		this.device = device;
		this.done = done;
		block = rate * BlockMs / 1000;
		int size = Marshal.SizeOf(typeof(WaveHeader));
		for (int i = 0; i < Blocks; i++)
		{
			WaveHeader header = new WaveHeader
			{
				Data = Marshal.AllocHGlobal(block * 2),
				Length = block * 2
			};
			headers[i] = Marshal.AllocHGlobal(size);
			Marshal.StructureToPtr(header, headers[i], fDeleteOld: false);
			waveOutPrepareHeader(device, headers[i], size);
		}
		feeder = new Thread(Run)
		{
			IsBackground = true,
			Name = "Capri sound effects",
			Priority = ThreadPriority.AboveNormal
		};
		feeder.Start();
	}

	// null when the speakers won't open this way; the caller then falls back to the plain speaker
	internal static SfxMixer Open(int rate)
	{
		try
		{
			AutoResetEvent signal = new AutoResetEvent(initialState: false);
			WaveFormat format = WaveFormat.Mono(rate);
			// CALLBACK_EVENT: the event is set each time a block finishes playing
			if (waveOutOpen(out IntPtr device, -1, ref format, signal.SafeWaitHandle.DangerousGetHandle(), IntPtr.Zero, 0x50000) != 0)
			{
				signal.Dispose();
				return null;
			}
			return new SfxMixer(rate, device, signal);
		}
		catch (Exception)
		{
			return null;
		}
	}

	internal void Play(short[] samples)
	{
		if (samples == null || samples.Length == 0 || !Alive)
		{
			return;
		}
		lock (gate)
		{
			voices.Add(new Voice { Samples = samples });
		}
	}

	internal void Clear()
	{
		lock (gate)
		{
			voices.Clear();
		}
	}

	private void Fill(IntPtr header)
	{
		WaveHeader wave = (WaveHeader)Marshal.PtrToStructure(header, typeof(WaveHeader));
		int[] sum = new int[block];
		lock (gate)
		{
			for (int v = voices.Count - 1; v >= 0; v--)
			{
				Voice voice = voices[v];
				int count = Math.Min(block, voice.Samples.Length - voice.At);
				for (int i = 0; i < count; i++)
				{
					sum[i] += voice.Samples[voice.At + i];
				}
				voice.At += count;
				if (voice.At >= voice.Samples.Length)
				{
					voices.RemoveAt(v);
				}
			}
		}
		short[] mixed = new short[block];
		for (int i = 0; i < block; i++)
		{
			mixed[i] = (short)Math.Clamp(sum[i], -32768, 32767);
		}
		Marshal.Copy(mixed, 0, wave.Data, block);
		waveOutWrite(device, header, Marshal.SizeOf(typeof(WaveHeader)));
	}

	private void Run()
	{
		try
		{
			// all blocks start out in the queue; each one is refilled with the newest mix as soon as it has played
			foreach (IntPtr header in headers)
			{
				Fill(header);
			}
			while (!stopped)
			{
				done.WaitOne(BlockMs * 2);
				if (stopped)
				{
					break;
				}
				foreach (IntPtr header in headers)
				{
					// WHDR_DONE
					if ((Marshal.ReadInt32(header, flagsAt) & 1) != 0)
					{
						Fill(header);
					}
				}
			}
		}
		catch (Exception)
		{
			broken = true;
		}
	}

	public void Dispose()
	{
		if (stopped)
		{
			return;
		}
		stopped = true;
		done.Set();
		if (Thread.CurrentThread != feeder)
		{
			feeder.Join(500);
		}
		waveOutReset(device);
		int size = Marshal.SizeOf(typeof(WaveHeader));
		foreach (IntPtr header in headers)
		{
			WaveHeader wave = (WaveHeader)Marshal.PtrToStructure(header, typeof(WaveHeader));
			waveOutUnprepareHeader(device, header, size);
			Marshal.FreeHGlobal(wave.Data);
			Marshal.FreeHGlobal(header);
		}
		waveOutClose(device);
		done.Dispose();
	}
}
