using System;
using System.Collections.Generic;

namespace CapriDesktop;

internal static class Sfx
{
	internal const int Rate = 24000;

	internal const double TypeLimit = 6.0;

	internal static volatile int Level = 60;

	internal static volatile bool Typing = true;

	internal static Action<short[]> Out;

	internal static Action<short[]> TypeOut;

	internal static Action Untyped;

	internal static void Play(Cue cue)
	{
		Action<short[]> action = Out;
		int level = Level;
		if (action != null && level > 0)
		{
			action(Make(cue, level));
		}
	}

	// several cues laid over each other as one sound, each starting `Ms` in, so they don't queue up one after another
	internal static void Play(IList<(Cue Cue, int Ms)> hits)
	{
		Action<short[]> action = Out;
		int level = Level;
		if (action == null || level <= 0 || hits.Count == 0)
		{
			return;
		}
		// a little softer per cue, since many of them overlap
		int each = Math.Max(1, level * 4 / 5);
		short[] samples = new short[0];
		foreach ((Cue cue, int ms) in hits)
		{
			samples = Mix(samples, Make(cue, each), Math.Max(0, ms) * Rate / 1000);
		}
		action(samples);
	}

	// mod: a quick sniff while files hover near her, higher the closer they get
	internal static void Sniff(double close)
	{
		Action<short[]> action = Out;
		int level = Level;
		if (action != null && level > 0)
		{
			action(Blip(0.045, 620.0 + 900.0 * Math.Clamp(close, 0.0, 1.0), 1.5, level / 4));
		}
	}

	internal static void Type(string text, double pace, int seed)
	{
		Untype();
		Action<short[]> typeOut = TypeOut;
		int level = Level;
		if (typeOut != null && Typing && level > 0 && !string.IsNullOrEmpty(text))
		{
			typeOut(Typed(text, pace, level, seed));
		}
	}

	internal static void Untype()
	{
		Untyped?.Invoke();
	}

	internal static short[] Typed(string text, double pace, int level, int seed)
	{
		if (string.IsNullOrEmpty(text) || pace <= 0.0 || level <= 0)
		{
			return new short[0];
		}
		double num = Math.Min(6.0, (double)text.Length / pace) + 0.05;
		int num2 = (int)(num * 24000.0);
		short[] array = new short[num2];
		Random random = new Random(seed);
		double num3 = 0.09 * (double)Math.Min(100, level) / 100.0;
		int num4 = 432;
		for (int i = 0; i < text.Length; i += 2)
		{
			if (char.IsLetterOrDigit(text[i]))
			{
				double num5 = (double)i / pace;
				if (num5 >= 6.0)
				{
					break;
				}
				double num6 = ((num5 < 5.0) ? 1.0 : (6.0 - num5));
				int num7 = (int)(num5 * 24000.0) + random.Next(0, 120);
				double num8 = 1900.0 * (0.9 + random.NextDouble() * 0.2);
				double num9 = num3 * num6 * (0.7 + random.NextDouble() * 0.3);
				double num10 = 0.0;
				double num11 = 0.0;
				for (int j = 0; j < num4 && num7 + j < num2; j++)
				{
					double num12 = (double)j / 24000.0;
					num10 += Math.PI * 2.0 * num8 / 24000.0;
					double num13 = Math.Min(1.0, num12 / 0.0015) * Math.Exp((0.0 - num12) / 0.004);
					double num14 = Math.Sin(num10) + 0.3 * Math.Sin(num10 * 2.01);
					num11 += (num14 - num11) * 0.45;
					int val = array[num7 + j] + (int)(num11 * num13 / 1.3 * num9 * 32767.0);
					array[num7 + j] = (short)Math.Max(-32768, Math.Min(32767, val));
				}
			}
		}
		return array;
	}

	internal static short[] Make(Cue cue, int level)
	{
		return cue switch
		{
			Cue.Arrive => Mix(Strike(level * 4 / 5, 3), Sweep(0.42, 330.0, 990.0, 0.03, level * 3 / 5), 1800),
			Cue.Leave => Mix(Sweep(0.36, 880.0, 250.0, 0.012, level * 3 / 5), Strike(level * 4 / 5, 5), 4000),
			Cue.Hop => Mix(Crackle(0.3, level * 3 / 4, 13), Sweep(0.16, 520.0, 1240.0, 0.008, level / 2), 0),
			Cue.Zap => Crackle(0.32, level * 3 / 4, strokes.Next()),
			Cue.Spark => Crackle(0.12, level / 3, strokes.Next()),
			Cue.EyeOpen => Glitch(open: true, level * 3 / 4),
			Cue.EyeClose => Glitch(open: false, level * 3 / 4),
			Cue.PetStroke => Blip(0.07, 1300.0 + strokes.NextDouble() * 500.0, 1.25, level / 2),
			Cue.PetHappy => Chime(level * 3 / 4),
			Cue.PetPurr => Mix(Chime(level * 3 / 5), Purr(level), 2400),
			Cue.PetEnd => Mix(Blip(0.12, 1500.0, 0.6, level / 2), Blip(0.16, 900.0, 0.55, level / 2), 2600),
			Cue.Giggle => Giggle(level),
			Cue.Crunch => Mix(Crackle(0.45, level * 4 / 5, strokes.Next()), Glitch(open: false, level / 2), 300),
			Cue.Whoosh => Mix(Whoosh(0.45, level, strokes.Next()), Sweep(0.3, 260.0, 900.0, 0.02, level / 4), 1200),
			Cue.Alarm => Alarm(level * 3 / 4),
			Cue.Boing => Boing(level * 3 / 4),
			Cue.Rumble => Rumble(1.2, level * 3 / 4, strokes.Next()),
			Cue.Spray => Spray(level * 3 / 4, strokes.Next()),
			Cue.Notify => Mix(Notes(level * 3 / 5, 0.09, 1600, 1046.5, 1568.0), Sweep(0.18, 1400.0, 2400.0, 0.004, level / 6), 3000),
			Cue.Trophy => Mix(Notes(level * 3 / 4, 0.16, 2000, 523.25, 659.25, 783.99, 1046.5, 1318.5), Crackle(0.35, level / 4, strokes.Next()), 9000),
			Cue.Chomp => Mix(Mix(Bite(level), Bite(level * 3 / 4), 3400), Blip(0.12, 700.0, 1.6, level / 3), 8200),
			Cue.Pop => Mix(Blip(0.08, 760.0, 2.4, level * 3 / 4), Crackle(0.07, level / 3, strokes.Next()), 0),
			Cue.RedPop => Mix(Mix(Blip(0.1, 520.0, 3.2, level * 3 / 4), Blip(0.1, 1040.0, 1.6, level / 2), 1200), Crackle(0.14, level / 2, strokes.Next()), 0),
			Cue.Miss => Blip(0.14, 320.0, 0.55, level / 3),
			Cue.Count => Blip(0.11, 880.0, 1.0, level * 3 / 5),
			Cue.Go => Mix(Blip(0.3, 1760.0, 1.0, level * 3 / 5), Sweep(0.3, 600.0, 1800.0, 0.01, level / 4), 0),
			Cue.BootUp => Mix(Mix(Sweep(0.7, 70.0, 520.0, 0.08, level / 2), Notes(level / 3, 0.05, 1500, 1200.0, 1800.0, 1500.0, 2400.0), 6000), Crackle(0.2, level / 5, strokes.Next()), 0),
			Cue.Beep => Blip(0.035, 2000.0 + strokes.Next(400), 1.0, level / 4),
			Cue.Yawn => Mix(Sweep(0.5, 420.0, 640.0, 0.12, level / 3), Sweep(0.7, 600.0, 230.0, 0.05, level / 3), 9000),
			Cue.Peek => Mix(Blip(0.06, 1300.0, 1.5, level / 3), Blip(0.06, 1700.0, 1.3, level / 4), 1500),
			Cue.Found => Mix(Mix(Chime(level * 3 / 4), Sweep(0.35, 500.0, 1600.0, 0.01, level / 3), 0), Giggle(level * 3 / 5), 7000),
			Cue.Dismiss => Sweep(0.14, 1300.0, 480.0, 0.004, level / 3),
			Cue.Hungry => Mix(Sweep(0.2, 480.0, 1150.0, 0.01, level / 3), Mix(Blip(0.06, 1400.0, 1.3, level / 3), Blip(0.07, 1800.0, 1.3, level / 3), 1700), 3200),
			Cue.Slurp => Mix(Whoosh(0.32, level * 3 / 4, strokes.Next()), Sweep(0.28, 1500.0, 260.0, 0.005, level / 3), 0),
			_ => Sweep(0.05, 1600.0, 1350.0, 0.003, level * 3 / 4),
		};
	}

	private static readonly Random strokes = new Random();

	// lays b over a, starting `at` samples in
	private static short[] Mix(short[] a, short[] b, int at)
	{
		short[] samples = new short[Math.Max(a.Length, at + b.Length)];
		for (int i = 0; i < samples.Length; i++)
		{
			int value = (i < a.Length ? a[i] : 0) + (i >= at && i - at < b.Length ? b[i - at] : 0);
			samples[i] = (short)Math.Max(-32768, Math.Min(32767, value));
		}
		return samples;
	}

	// electric crackle: sharp random snaps that thin out, over a buzzing 120 Hz hum with a jittery pitch, bit-crushed
	private static short[] Crackle(double seconds, int level, int seed)
	{
		int count = (int)(seconds * 24000.0);
		short[] samples = new short[count];
		Random noise = new Random(seed);
		double gain = 0.3 * (double)Math.Max(0, Math.Min(100, level)) / 100.0;
		double phase = 0.0;
		double snap = 0.0;
		double last = 0.0;
		double held = 0.0;
		for (int i = 0; i < count; i++)
		{
			double p = (double)i / (double)count;
			if (noise.NextDouble() < 0.004 * (1.0 - p) + 0.0006)
			{
				snap = 0.8 + noise.NextDouble() * 0.6;
			}
			snap *= 0.93;
			double white = noise.NextDouble() * 2.0 - 1.0;
			double bright = white - last;
			last = white;
			phase += Math.PI * 2.0 * (120.0 + noise.NextDouble() * 90.0) / 24000.0;
			double hum = 0.5 * Math.Sign(Math.Sin(phase)) * Math.Pow(1.0 - p, 1.5);
			double sample = bright * snap + hum * 0.45;
			if (i % 3 == 0)
			{
				held = sample;
			}
			double envelope = Math.Min(1.0, (double)i / 40.0) * Math.Pow(1.0 - p, 0.8);
			samples[i] = (short)(Math.Max(-1.0, Math.Min(1.0, held * envelope)) * gain * 32767.0);
		}
		return samples;
	}

	// a lightning strike: a white-hot crack, a falling electric buzz and a short low rumble
	private static short[] Strike(int level, int seed)
	{
		double seconds = 0.7;
		int count = (int)(seconds * 24000.0);
		short[] samples = new short[count];
		Random noise = new Random(seed);
		double gain = 0.34 * (double)Math.Max(0, Math.Min(100, level)) / 100.0;
		double phase = 0.0;
		double low = 0.0;
		double last = 0.0;
		for (int i = 0; i < count; i++)
		{
			double t = (double)i / 24000.0;
			double p = (double)i / (double)count;
			double white = noise.NextDouble() * 2.0 - 1.0;
			double crack = (white - last) * Math.Exp(-t / 0.035) * 1.2;
			last = white;
			low += (white - low) * 0.02;
			double rumble = low * 4.0 * Math.Min(1.0, t / 0.03) * Math.Exp(-t / 0.22);
			phase += Math.PI * 2.0 * (900.0 * Math.Pow(70.0 / 900.0, Math.Sqrt(p)) + noise.NextDouble() * 60.0) / 24000.0;
			double buzz = Math.Sign(Math.Sin(phase)) * 0.35 * Math.Exp(-t / 0.18) * (noise.Next(9) == 0 ? 0.2 : 1.0);
			double sample = crack + rumble + buzz;
			samples[i] = (short)(Math.Max(-1.0, Math.Min(1.0, sample / 1.4)) * gain * 32767.0);
		}
		return samples;
	}

	// a short soft square-ish blip that glides by `bend`, for strokes and the closing pair
	private static short[] Blip(double seconds, double freq, double bend, int level)
	{
		int count = (int)(seconds * 24000.0);
		short[] samples = new short[count];
		double gain = 0.2 * (double)Math.Max(0, Math.Min(100, level)) / 100.0;
		double phase = 0.0;
		double smooth = 0.0;
		for (int i = 0; i < count; i++)
		{
			double p = (double)i / (double)count;
			phase += Math.PI * 2.0 * freq * Math.Pow(bend, p) / 24000.0;
			double tone = 0.5 * Math.Sign(Math.Sin(phase)) + 0.5 * Math.Sin(phase);
			smooth += (tone - smooth) * 0.35;
			double envelope = Math.Min(1.0, (double)i / 60.0) * Math.Pow(1.0 - p, 2.0);
			samples[i] = (short)(smooth * envelope * gain * 32767.0);
		}
		return samples;
	}

	// happy rising three-note arpeggio, each note a quick bit-crushed blip
	private static short[] Chime(int level)
	{
		short[] samples = new short[(int)(0.42 * 24000.0)];
		double[] notes = { 784.0, 988.0, 1319.0 };
		for (int n = 0; n < notes.Length; n++)
		{
			short[] note = Blip(0.2, notes[n], 1.04, level);
			int at = n * 2400;
			for (int i = 0; i < note.Length && at + i < samples.Length; i++)
			{
				if (i % 3 == 0 || i == 0)
				{
					samples[at + i] = (short)Math.Max(-32768, Math.Min(32767, samples[at + i] + note[i]));
				}
				else
				{
					samples[at + i] = samples[at + i - 1];
				}
			}
		}
		return samples;
	}

	// a small digital purr: low buzzy pulses at ~24 Hz, breathing in and out twice, bit-crushed with a faint hiss
	private static short[] Purr(int level)
	{
		double seconds = 1.7;
		int count = (int)(seconds * 24000.0);
		short[] samples = new short[count];
		Random noise = new Random(5);
		double gain = 0.3 * (double)Math.Max(0, Math.Min(100, level)) / 100.0;
		double phase = 0.0;
		double pulse = 0.0;
		double held = 0.0;
		double smooth = 0.0;
		for (int i = 0; i < count; i++)
		{
			double t = (double)i / 24000.0;
			double p = (double)i / (double)count;
			double breath = Math.Pow(Math.Sin(Math.PI * Math.Min(1.0, p * 2.0 % 1.0 + 0.02)), 0.7);
			double rate = 22.0 + 4.0 * Math.Sin(p * 12.0);
			pulse += Math.PI * 2.0 * rate / 24000.0;
			double flutter = Math.Pow(0.5 + 0.5 * Math.Sin(pulse), 3.0);
			phase += Math.PI * 2.0 * (95.0 + 10.0 * Math.Sin(t * 5.0)) / 24000.0;
			double tone = 0.6 * Math.Sign(Math.Sin(phase)) + 0.4 * Math.Sin(phase * 2.0);
			double sample = tone * flutter + (noise.NextDouble() * 2.0 - 1.0) * 0.25 * flutter;
			if (i % 6 == 0)
			{
				held = Math.Round(sample * 6.0) / 6.0;
			}
			smooth += (held - smooth) * 0.25;
			double envelope = breath * Math.Min(1.0, t / 0.05) * Math.Min(1.0, (seconds - t) / 0.15);
			samples[i] = (short)(Math.Max(-1.0, Math.Min(1.0, smooth * envelope)) * gain * 32767.0);
		}
		return samples;
	}

	// digital boot/shutdown for the eye pop-ups: a buzzy sweep chopped by a random gate, bit-crushed, with noise
	private static short[] Glitch(bool open, int level)
	{
		double seconds = open ? 0.6 : 0.42;
		int count = (int)(seconds * 24000.0);
		short[] samples = new short[count];
		Random noise = new Random(open ? 7 : 11);
		double gain = 0.24 * (double)Math.Max(0, Math.Min(100, level)) / 100.0;
		double phase = 0.0;
		double held = 0.0;
		bool gate = true;
		int gateLeft = 0;
		for (int i = 0; i < count; i++)
		{
			double t = (double)i / 24000.0;
			double p = (double)i / (double)count;
			double freq = open ? 140.0 * Math.Pow(1800.0 / 140.0, p * p) : 1600.0 * Math.Pow(90.0 / 1600.0, Math.Sqrt(p));
			phase += Math.PI * 2.0 * freq / 24000.0;
			double tone = 0.35 * Math.Sign(Math.Sin(phase)) + 0.65 * Math.Sin(phase);
			if (--gateLeft <= 0)
			{
				gate = noise.NextDouble() < (open ? 0.35 + 0.65 * p : 1.0 - 0.6 * p);
				gateLeft = noise.Next(240, 900);
			}
			double hiss = (noise.NextDouble() * 2.0 - 1.0) * (open ? 0.5 * (1.0 - p) : 0.35 * p);
			double sample = (gate ? tone : tone * 0.15) + hiss;
			int crush = open ? 1 + (int)(14.0 * (1.0 - p)) : 1 + (int)(16.0 * p);
			if (i % crush == 0)
			{
				held = sample;
			}
			double envelope = Math.Min(1.0, t / 0.006) * (open ? Math.Pow(1.0 - p, 0.6) : Math.Pow(1.0 - p, 1.4));
			double value = held * envelope;
			if (open && p > 0.68 && p < 0.76)
			{
				value += Math.Sin(Math.PI * 2.0 * 2400.0 * t) * 0.5 * (1.0 - (p - 0.68) / 0.08); // lock-on blip
			}
			samples[i] = (short)(Math.Max(-1.0, Math.Min(1.0, value / 1.3)) * gain * 32767.0);
		}
		return samples;
	}

	// mischief: a cheeky "he-he-he-he", quick bouncy blips wobbling up and down
	private static short[] Giggle(int level)
	{
		double[] notes = { 1180.0, 1420.0, 1250.0, 1560.0 };
		short[] samples = new short[0];
		for (int n = 0; n < notes.Length; n++)
		{
			samples = Mix(samples, Blip(0.075, notes[n], n % 2 == 0 ? 1.18 : 0.86, level * 3 / 5), n * 2300);
		}
		return samples;
	}

	// air rushing past: noise through a low-pass that opens and closes again
	private static short[] Whoosh(double seconds, int level, int seed)
	{
		int count = (int)(seconds * 24000.0);
		short[] samples = new short[count];
		Random noise = new Random(seed);
		double gain = 0.32 * (double)Math.Max(0, Math.Min(100, level)) / 100.0;
		double low = 0.0;
		double lower = 0.0;
		for (int i = 0; i < count; i++)
		{
			double p = (double)i / (double)count;
			double open = Math.Sin(Math.PI * p);
			double cutoff = 0.02 + 0.25 * open * open;
			double white = noise.NextDouble() * 2.0 - 1.0;
			low += (white - low) * cutoff;
			lower += (low - lower) * cutoff;
			samples[i] = (short)(Math.Max(-1.0, Math.Min(1.0, lower * 3.0 * Math.Pow(open, 1.5))) * gain * 32767.0);
		}
		return samples;
	}

	// a fake security siren: two bit-crushed square tones trading places, pulsing
	private static short[] Alarm(int level)
	{
		double seconds = 1.15;
		int count = (int)(seconds * 24000.0);
		short[] samples = new short[count];
		double gain = 0.2 * (double)Math.Max(0, Math.Min(100, level)) / 100.0;
		double phase = 0.0;
		double held = 0.0;
		for (int i = 0; i < count; i++)
		{
			double t = (double)i / 24000.0;
			double freq = (int)(t / 0.19) % 2 == 0 ? 1046.0 : 784.0;
			phase += Math.PI * 2.0 * freq / 24000.0;
			double tone = 0.6 * Math.Sign(Math.Sin(phase)) + 0.4 * Math.Sin(phase);
			if (i % 4 == 0)
			{
				held = tone;
			}
			double envelope = Math.Min(1.0, t / 0.01) * Math.Min(1.0, (seconds - t) / 0.12) * (0.75 + 0.25 * Math.Sin(t * Math.PI * 24.0));
			samples[i] = (short)(Math.Max(-1.0, Math.Min(1.0, held * envelope)) * gain * 32767.0);
		}
		return samples;
	}

	// a cartoon spring: a tone wobbling fast around a rising pitch and settling
	private static short[] Boing(int level)
	{
		double seconds = 0.7;
		int count = (int)(seconds * 24000.0);
		short[] samples = new short[count];
		double gain = 0.28 * (double)Math.Max(0, Math.Min(100, level)) / 100.0;
		double phase = 0.0;
		for (int i = 0; i < count; i++)
		{
			double t = (double)i / 24000.0;
			double p = (double)i / (double)count;
			double freq = 190.0 + 140.0 * p + 120.0 * Math.Sin(t * Math.PI * 22.0) * Math.Exp(-t * 4.5);
			phase += Math.PI * 2.0 * freq / 24000.0;
			double tone = Math.Sin(phase) + 0.25 * Math.Sin(phase * 2.0);
			double envelope = Math.Min(1.0, t / 0.005) * Math.Exp(-t * 3.2);
			samples[i] = (short)(Math.Max(-1.0, Math.Min(1.0, tone * envelope / 1.25)) * gain * 32767.0);
		}
		return samples;
	}

	// a shaky low rumble with a rattle on top, for the window earthquake
	private static short[] Rumble(double seconds, int level, int seed)
	{
		int count = (int)(seconds * 24000.0);
		short[] samples = new short[count];
		Random noise = new Random(seed);
		double gain = 0.34 * (double)Math.Max(0, Math.Min(100, level)) / 100.0;
		double low = 0.0;
		double phase = 0.0;
		for (int i = 0; i < count; i++)
		{
			double t = (double)i / 24000.0;
			double p = (double)i / (double)count;
			double white = noise.NextDouble() * 2.0 - 1.0;
			low += (white - low) * 0.03;
			phase += Math.PI * 2.0 * (48.0 + 8.0 * Math.Sin(t * 7.0)) / 24000.0;
			double rattle = Math.Sign(Math.Sin(t * Math.PI * 110.0)) * 0.15 * (0.5 + 0.5 * Math.Sin(t * Math.PI * 18.0));
			double sample = low * 5.0 + Math.Sin(phase) * 0.5 + rattle;
			double envelope = Math.Min(1.0, t / 0.03) * Math.Pow(1.0 - p, 0.7);
			samples[i] = (short)(Math.Max(-1.0, Math.Min(1.0, sample * envelope / 1.4)) * gain * 32767.0);
		}
		return samples;
	}

	// a spray can: two rattle clicks, then a long hiss that pulses as the hand sweeps
	private static short[] Spray(int level, int seed)
	{
		double seconds = 1.6;
		int count = (int)(seconds * 24000.0);
		short[] samples = new short[count];
		Random noise = new Random(seed);
		double gain = 0.26 * (double)Math.Max(0, Math.Min(100, level)) / 100.0;
		double last = 0.0;
		for (int i = 0; i < count; i++)
		{
			double t = (double)i / 24000.0;
			double white = noise.NextDouble() * 2.0 - 1.0;
			double bright = white - last;
			last = white;
			double hiss = t < 0.18 ? 0.0 : bright * 0.6 * (0.7 + 0.3 * Math.Sin(t * Math.PI * 7.0)) * Math.Min(1.0, (t - 0.18) / 0.05) * Math.Min(1.0, (seconds - t) / 0.2);
			double rattle = 0.0;
			if (t < 0.16)
			{
				double c = t % 0.08;
				rattle = Math.Sin(Math.PI * 2.0 * 2600.0 * c) * Math.Exp(-c / 0.006) * 0.8;
			}
			samples[i] = (short)(Math.Max(-1.0, Math.Min(1.0, hiss + rattle)) * gain * 32767.0);
		}
		return samples;
	}

	// a quick run of square-ish notes, one every `gap` samples
	private static short[] Notes(int level, double each, int gap, params double[] freqs)
	{
		short[] samples = new short[0];
		for (int i = 0; i < freqs.Length; i++)
		{
			samples = Mix(samples, Blip(each, freqs[i], 1.01, level), i * gap);
		}
		return samples;
	}

	// one bite: a low thump with a crunchy noise burst on top
	private static short[] Bite(int level)
	{
		int count = (int)(0.13 * 24000.0);
		short[] samples = new short[count];
		double gain = 0.3 * (double)Math.Max(0, Math.Min(100, level)) / 100.0;
		double phase = 0.0;
		for (int i = 0; i < count; i++)
		{
			double t = (double)i / 24000.0;
			phase += Math.PI * 2.0 * (180.0 - 900.0 * t) / 24000.0;
			double thump = Math.Sin(phase) * Math.Exp(-t * 30.0);
			double crunch = (strokes.NextDouble() * 2.0 - 1.0) * Math.Exp(-t * 45.0) * ((i / 90) % 2 == 0 ? 1.0 : 0.35);
			samples[i] = (short)(Math.Max(-1.0, Math.Min(1.0, thump * 0.8 + crunch * 0.6)) * gain * 32767.0);
		}
		return samples;
	}

	private static short[] Sweep(double seconds, double from, double to, double attack, int level)
	{
		int num = (int)(seconds * 24000.0);
		short[] array = new short[num];
		double num2 = 0.0;
		double num3 = 0.0;
		double num4 = 0.0;
		double num5 = 0.26 * (double)Math.Max(0, Math.Min(100, level)) / 100.0;
		for (int i = 0; i < num; i++)
		{
			double num6 = (double)i / 24000.0;
			double num7 = (double)i / (double)num;
			double num8 = from * Math.Pow(to / from, 1.0 - Math.Pow(1.0 - num7, 3.0));
			num2 += Math.PI * 2.0 * num8 / 24000.0;
			num3 += Math.PI * 3.0 * num8 / 24000.0;
			double num9 = Math.Min(1.0, num6 / attack) * Math.Pow(1.0 - num7, 2.2);
			double num10 = Math.Sin(num2) + 0.28 * Math.Sin(num3) + 0.12 * Math.Sin(2.0 * num2);
			num4 += (num10 - num4) * 0.5;
			array[i] = (short)(Math.Max(-1.0, Math.Min(1.0, num4 * num9 / 1.4)) * num5 * 32767.0);
		}
		return array;
	}
}
