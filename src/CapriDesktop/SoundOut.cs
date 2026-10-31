using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace CapriDesktop;

internal sealed class SoundOut : IDisposable
{
	private readonly int rate;

	private readonly object gate = new object();

	private readonly Queue<short[]> chunks = new Queue<short[]>();

	private readonly Thread feeder;

	private readonly ManualResetEvent ready = new ManualResetEvent(initialState: false);

	private int headAt;

	private string failure;

	private volatile bool stopped;

	private volatile bool flush;

	private volatile bool alive;

	internal long Fed;

	internal bool Alive => alive;

	internal SoundOut(int rate)
	{
		this.rate = rate;
		feeder = new Thread(Run)
		{
			IsBackground = true,
			Name = "Capri speakers"
		};
		feeder.SetApartmentState(ApartmentState.MTA);
		feeder.Start();
		if (!ready.WaitOne(3000))
		{
			stopped = true;
			throw new VoiceException("Windows took too long to open your speakers.");
		}
		if (failure != null)
		{
			throw new VoiceException(failure);
		}
	}

	internal void Write(short[] samples)
	{
		if (samples.Length == 0)
		{
			return;
		}
		lock (gate)
		{
			chunks.Enqueue(samples);
		}
	}

	internal void Clear()
	{
		lock (gate)
		{
			chunks.Clear();
			headAt = 0;
		}
		flush = true;
	}

	private int Take(short[] into, int wanted)
	{
		int num = 0;
		lock (gate)
		{
			while (num < wanted && chunks.Count > 0)
			{
				short[] array = chunks.Peek();
				int num2 = Math.Min(wanted - num, array.Length - headAt);
				Array.Copy(array, headAt, into, num, num2);
				num += num2;
				headAt += num2;
				if (headAt >= array.Length)
				{
					chunks.Dequeue();
					headAt = 0;
				}
			}
		}
		return num;
	}

	private void Run()
	{
		ISoundDevices soundDevices = null;
		ISoundDevice device = null;
		ISoundClient soundClient = null;
		object service = null;
		IntPtr intPtr = IntPtr.Zero;
		try
		{
			int frames;
			ISoundRender soundRender;
			try
			{
				soundDevices = (ISoundDevices)new SoundDevices();
				int defaultAudioEndpoint = soundDevices.GetDefaultAudioEndpoint(0, 1, out device);
				if (defaultAudioEndpoint != 0 || device == null)
				{
					failure = "Windows found no speakers (WASAPI error 0x" + defaultAudioEndpoint.ToString("X8") + ").";
					return;
				}
				Guid iid = typeof(ISoundClient).GUID;
				defaultAudioEndpoint = device.Activate(ref iid, 23, IntPtr.Zero, out var found);
				if (defaultAudioEndpoint != 0 || found == null)
				{
					failure = "Windows wouldn't open your speakers (WASAPI error 0x" + defaultAudioEndpoint.ToString("X8") + ").";
					return;
				}
				soundClient = (ISoundClient)found;
				intPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(WaveFormat)));
				Marshal.StructureToPtr(WaveFormat.Mono(rate), intPtr, fDeleteOld: false);
				defaultAudioEndpoint = soundClient.Initialize(0, -2013265920, 2000000L, 0L, intPtr, IntPtr.Zero);
				if (defaultAudioEndpoint != 0)
				{
					failure = "Windows wouldn't play her voice on your speakers (WASAPI error 0x" + defaultAudioEndpoint.ToString("X8") + ").";
					return;
				}
				if (soundClient.GetBufferSize(out frames) != 0 || frames <= 0)
				{
					failure = "Windows gave her no room on your speakers.";
					return;
				}
				Guid iid2 = typeof(ISoundRender).GUID;
				defaultAudioEndpoint = soundClient.GetService(ref iid2, out service);
				if (defaultAudioEndpoint != 0 || service == null)
				{
					failure = "Windows wouldn't let her write to your speakers (WASAPI error 0x" + defaultAudioEndpoint.ToString("X8") + ").";
					return;
				}
				soundRender = (ISoundRender)service;
				defaultAudioEndpoint = soundClient.Start();
				if (defaultAudioEndpoint != 0)
				{
					failure = "Windows wouldn't start your speakers (WASAPI error 0x" + defaultAudioEndpoint.ToString("X8") + ").";
					return;
				}
				alive = true;
			}
			catch (Exception ex)
			{
				failure = "Windows wouldn't open your speakers: " + ex.Message;
				return;
			}
			finally
			{
				ready.Set();
			}
			short[] array = new short[frames];
			while (!stopped)
			{
				Thread.Sleep(10);
				if (flush)
				{
					flush = false;
					soundClient.Stop();
					soundClient.Reset();
					soundClient.Start();
				}
				if (soundClient.GetCurrentPadding(out var frames2) != 0)
				{
					break;
				}
				int num = frames - frames2;
				if (num <= 0)
				{
					continue;
				}
				int num2 = Take(array, num);
				if (num2 != 0)
				{
					if (soundRender.GetBuffer(num2, out var data) != 0)
					{
						break;
					}
					Marshal.Copy(array, 0, data, num2);
					if (soundRender.ReleaseBuffer(num2, 0) != 0)
					{
						break;
					}
					Interlocked.Add(ref Fed, num2);
				}
			}
		}
		catch (Exception)
		{
		}
		finally
		{
			alive = false;
			try
			{
				soundClient?.Stop();
			}
			catch (Exception)
			{
			}
			if (service != null)
			{
				Marshal.ReleaseComObject(service);
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
			if (intPtr != IntPtr.Zero)
			{
				Marshal.FreeHGlobal(intPtr);
			}
		}
	}

	public void Dispose()
	{
		stopped = true;
		if (Thread.CurrentThread != feeder)
		{
			feeder.Join(500);
		}
	}
}
