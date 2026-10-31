using System;
using System.Diagnostics;
using System.Threading;

namespace CapriDesktop;

internal sealed class Groove
{
	internal const double Latency = 0.03;

	private const double Step = 0.01;

	private const int Window = 800;

	private readonly Func<double> now;

	private readonly object gate = new object();

	private BeatLock beat = new BeatLock();

	private volatile int playing;

	private int started;

	internal Groove(Func<double> now)
	{
		this.now = now;
	}

	internal void Want(bool on)
	{
		if (on == (playing != 0))
		{
			return;
		}
		if (!on)
		{
			playing = 0;
			return;
		}
		int run = ++started;
		lock (gate)
		{
			beat = new BeatLock();
		}
		playing = run;
		Thread thread = new Thread(() =>
		{
			Listen(run);
		});
		thread.IsBackground = true;
		thread.Name = "Capri beat";
		thread.Start();
	}

	internal bool Locked(out double bpm, out double beatAt)
	{
		lock (gate)
		{
			bpm = beat.Bpm;
			beatAt = beat.BeatAt;
		}
		return bpm > 0.0;
	}

	internal void Pretend(double bpm, double beatAt)
	{
		lock (gate)
		{
			beat = new BeatLock();
			beat.Bpm = bpm;
			beat.BeatAt = beatAt;
		}
	}

	internal static string Probe()
	{
		string format = null;
		Thread thread = new Thread(() =>
		{
			SoundTap soundTap = SoundTap.Open(start: false);
			if (soundTap != null)
			{
				format = soundTap.Format;
				soundTap.Dispose();
			}
		});
		thread.IsBackground = true;
		thread.Start();
		thread.Join(5000);
		return format;
	}

	private void Listen(int run)
	{
		SoundTap soundTap = null;
		float[] ring = new float[800];
		float[] array = new float[800];
		int head = 0;
		int count = 0;
		int pushed = 0;
		Onset onset = null;
		Action<float> action = (float level) =>
		{
			ring[head] = level;
			head = (head + 1) % 800;
			if (count < 800)
			{
				count++;
			}
			pushed++;
		};
		try
		{
			double num = now();
			double num2 = num;
			double num3 = -10.0;
			double num4 = num + 1.0;
			while (playing == run)
			{
				Thread.Sleep(10);
				double num5 = now();
				if (soundTap != null && num5 - num2 > 5.0)
				{
					num2 = num5;
					if (SoundTap.DefaultDevice() != soundTap.Device)
					{
						soundTap.Dispose();
						soundTap = null;
					}
				}
				if (soundTap == null)
				{
					if (num5 - num3 < 2.0)
					{
						continue;
					}
					num3 = num5;
					soundTap = SoundTap.Open(start: true);
					count = 0;
					num = num5;
					if (soundTap == null)
					{
						continue;
					}
					onset = new Onset(soundTap.Rate);
				}
				int num6 = pushed;
				if (!soundTap.Read(action, onset, out var newest))
				{
					soundTap.Dispose();
					soundTap = null;
					continue;
				}
				double num7 = num5;
				if (newest > 0)
				{
					num7 = (double)newest * 1E-07 + num5 - (double)Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency - (double)onset.Filled / (double)soundTap.Rate;
				}
				if (Math.Abs(num7 - num5) > 0.2)
				{
					num7 = num5;
				}
				if (pushed != num6)
				{
					num = num5;
				}
				else
				{
					for (; num5 - num > 0.05; num += 0.01)
					{
						action(0f);
					}
				}
				if (num5 < num4 || count < 600)
				{
					continue;
				}
				num4 = num5 + 0.5;
				for (int num8 = 0; num8 < count; num8++)
				{
					array[num8] = ring[(head - count + num8 + 800) % 800];
				}
				bool steady = Tempo.Find(array, count, out var bpm, out var lastBeat, out var _);
				lock (gate)
				{
					if (playing == run)
					{
						beat.Take(steady, bpm, num7 - lastBeat - 0.005 + 0.03);
					}
				}
			}
		}
		catch (Exception)
		{
		}
		finally
		{
			soundTap?.Dispose();
		}
	}
}
