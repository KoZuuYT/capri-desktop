using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace CapriDesktop;

// the mod: friend ears. She taps what plays on a speaker (your friends in a Discord call) through its loopback,
// turns it into 16 kHz mono and cuts it into lines, so each line can be heard and answered
internal sealed class FriendEars : IDisposable
{
	private const double Loud = 0.012;

	private const int Chunk = 1600;

	internal volatile Func<bool> Hold;

	internal volatile string Problem;

	private volatile bool running;

	private Thread worker;

	private string device;

	// what she heard lately, written to errors.txt every 30 s so a silent call can be traced
	private long reportAt;

	private double loudest;

	private int sent;

	private int cut;

	internal bool On => running;

	internal event Action<short[]> Said;

	internal void Start(string id)
	{
		if (running && id == device)
		{
			return;
		}
		Stop();
		device = id;
		Problem = null;
		running = true;
		worker = new Thread(Pump);
		worker.IsBackground = true;
		worker.Name = "Capri friend ears";
		worker.SetApartmentState(ApartmentState.MTA);
		worker.Start();
	}

	internal void Stop()
	{
		if (running)
		{
			running = false;
			worker?.Join(1500);
			worker = null;
		}
	}

	public void Dispose()
	{
		Stop();
	}

	private void Pump()
	{
		ISoundDevices devices = null;
		ISoundDevice found = null;
		ISoundClient client = null;
		ISoundCapture capture = null;
		IntPtr format = IntPtr.Zero;
		try
		{
			devices = (ISoundDevices)new SoundDevices();
			if ((string.IsNullOrEmpty(device) || devices.GetDevice(device, out found) != 0 || found == null) && (devices.GetDefaultAudioEndpoint(0, 0, out found) != 0 || found == null))
			{
				Problem = "Can't find that speaker. Pick another.";
				return;
			}
			Guid iid = typeof(ISoundClient).GUID;
			if (found.Activate(ref iid, 23, IntPtr.Zero, out var made) != 0)
			{
				Problem = "Can't listen to that speaker.";
				return;
			}
			client = (ISoundClient)made;
			if (client.GetMixFormat(out format) != 0 || format == IntPtr.Zero)
			{
				Problem = "Can't listen to that speaker.";
				return;
			}
			int tag = Marshal.ReadInt16(format, 0) & 0xFFFF;
			int channels = Marshal.ReadInt16(format, 2);
			int rate = Marshal.ReadInt32(format, 4);
			int bits = Marshal.ReadInt16(format, 14);
			if (tag == 65534)
			{
				tag = Marshal.ReadInt32(format, 24);
			}
			bool isFloat = tag == 3 && bits == 32;
			if ((!isFloat && (tag != 1 || (bits != 16 && bits != 32))) || channels < 1 || rate < 8000)
			{
				Problem = "That speaker uses a sound format I can't read.";
				return;
			}
			if (client.Initialize(0, 131072, 2000000L, 0L, format, IntPtr.Zero) != 0)
			{
				Problem = "Can't listen to that speaker.";
				return;
			}
			Guid iid2 = typeof(ISoundCapture).GUID;
			if (client.GetService(ref iid2, out var service) != 0)
			{
				Problem = "Can't listen to that speaker.";
				return;
			}
			capture = (ISoundCapture)service;
			if (client.Start() != 0)
			{
				Problem = "Can't listen to that speaker.";
				return;
			}
			Brain.Log("friend ears", -1L, "listening: " + channels + " ch, " + rate + " Hz, " + (isFloat ? "float" : (bits + "-bit")));
			reportAt = Environment.TickCount64 + 30000;
			Listen(capture, rate, channels, bits, isFloat);
		}
		catch (Exception ex)
		{
			Problem = "Can't listen to that speaker: " + ex.Message;
		}
		finally
		{
			if (Problem != null)
			{
				Brain.Log("friend ears", -1L, Problem);
			}
			running = false;
			try
			{
				client?.Stop();
			}
			catch (Exception)
			{
			}
			if (format != IntPtr.Zero)
			{
				Marshal.FreeCoTaskMem(format);
			}
			foreach (object item in new object[4] { capture, client, found, devices })
			{
				if (item != null)
				{
					try
					{
						Marshal.ReleaseComObject(item);
					}
					catch (Exception)
					{
					}
				}
			}
		}
	}

	private void Listen(ISoundCapture capture, int rate, int channels, int bits, bool isFloat)
	{
		double step = rate / 16000.0;
		double sum = 0.0;
		double taken = 0.0;
		double edge = step;
		short[] chunk = new short[Chunk];
		int filled = 0;
		float[] floats = new float[0];
		short[] shorts = new short[0];
		int[] ints = new int[0];
		long quietSince = Environment.TickCount64;
		Line line = new Line();
		while (running)
		{
			Thread.Sleep(20);
			bool any = false;
			while (running && capture.GetNextPacketSize(out var size) == 0 && size > 0)
			{
				if (capture.GetBuffer(out var data, out var frames, out var flags, out var _, out var _) != 0)
				{
					break;
				}
				any = true;
				int count = frames * channels;
				bool silent = (flags & 2) != 0 || data == IntPtr.Zero;
				if (!silent && isFloat)
				{
					if (floats.Length < count)
					{
						floats = new float[count];
					}
					Marshal.Copy(data, floats, 0, count);
				}
				else if (!silent && bits == 16)
				{
					if (shorts.Length < count)
					{
						shorts = new short[count];
					}
					Marshal.Copy(data, shorts, 0, count);
				}
				else if (!silent)
				{
					if (ints.Length < count)
					{
						ints = new int[count];
					}
					Marshal.Copy(data, ints, 0, count);
				}
				for (int i = 0; i < frames; i++)
				{
					double value = 0.0;
					if (!silent)
					{
						for (int j = i * channels; j < (i + 1) * channels; j++)
						{
							value += isFloat ? floats[j] : ((bits == 16) ? (shorts[j] / 32768.0) : (ints[j] / 2147483648.0));
						}
						value /= channels;
					}
					sum += value;
					taken += 1.0;
					if (taken >= edge)
					{
						edge += step;
						double average = sum / Math.Max(1.0, Math.Round(step));
						sum = 0.0;
						chunk[filled++] = (short)Math.Max(-32768.0, Math.Min(32767.0, average * 32767.0));
						if (filled == Chunk)
						{
							filled = 0;
							Take(line, (short[])chunk.Clone());
						}
					}
				}
				capture.ReleaseBuffer(frames);
			}
			if (any)
			{
				quietSince = Environment.TickCount64;
			}
			else if (line.Talking && Environment.TickCount64 - quietSince >= 100)
			{
				// nothing plays at all, so the speaker went silent: count it as quiet
				quietSince = Environment.TickCount64;
				Take(line, new short[Chunk]);
			}
		}
	}

	private sealed class Line
	{
		internal readonly List<short[]> Parts = new List<short[]>();

		internal readonly List<short[]> Before = new List<short[]>();

		internal bool Talking;

		internal int LoudRun;

		internal int Quiet;

		internal int Voiced;
	}

	private void Take(Line line, short[] chunk)
	{
		double level = Speaker.Loudness(chunk, 0, chunk.Length);
		loudest = Math.Max(loudest, level);
		if (Environment.TickCount64 >= reportAt)
		{
			reportAt = Environment.TickCount64 + 30000;
			if (loudest >= 0.002 || sent > 0 || cut > 0)
			{
				Brain.Log("friend ears", -1L, "last 30 s: loudest " + loudest.ToString("0.000") + " (talk starts at " + Loud.ToString("0.000") + "), " + sent + " lines sent, " + cut + " cut while she talked");
			}
			loudest = 0.0;
			sent = cut = 0;
		}
		Func<bool> hold = Hold;
		bool held;
		try
		{
			held = hold != null && hold();
		}
		catch (Exception)
		{
			held = false;
		}
		if (held)
		{
			// she's talking: drop it, so her own voice never counts as a friend
			if (line.Talking && line.Voiced >= 5)
			{
				cut++;
			}
			line.Parts.Clear();
			line.Before.Clear();
			line.Talking = false;
			line.LoudRun = line.Quiet = line.Voiced = 0;
			return;
		}
		bool loud = level >= Loud;
		if (!line.Talking)
		{
			line.Before.Add(chunk);
			if (line.Before.Count > 3)
			{
				line.Before.RemoveAt(0);
			}
			line.LoudRun = loud ? (line.LoudRun + 1) : 0;
			if (line.LoudRun >= 2)
			{
				line.Talking = true;
				line.Parts.AddRange(line.Before);
				line.Before.Clear();
				line.Quiet = 0;
				line.Voiced = line.LoudRun;
			}
			return;
		}
		line.Parts.Add(chunk);
		if (loud)
		{
			line.Voiced++;
			line.Quiet = 0;
		}
		else
		{
			line.Quiet++;
		}
		if (line.Quiet < 8 && line.Parts.Count < 150)
		{
			return;
		}
		List<short[]> parts = new List<short[]>(line.Parts);
		int voiced = line.Voiced;
		line.Parts.Clear();
		line.Talking = false;
		line.LoudRun = line.Quiet = line.Voiced = 0;
		if (voiced < 5)
		{
			return;
		}
		short[] samples = new short[parts.Count * Chunk];
		for (int i = 0; i < parts.Count; i++)
		{
			Array.Copy(parts[i], 0, samples, i * Chunk, Chunk);
		}
		sent++;
		try
		{
			Said?.Invoke(samples);
		}
		catch (Exception)
		{
		}
	}
}
