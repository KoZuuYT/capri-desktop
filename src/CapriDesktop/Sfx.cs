using System;

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
			Cue.Arrive => Sweep(0.42, 330.0, 990.0, 0.03, level), 
			Cue.Leave => Sweep(0.36, 880.0, 250.0, 0.012, level), 
			Cue.Hop => Sweep(0.16, 520.0, 1240.0, 0.008, level),
			Cue.EyeOpen => Glitch(open: true, level * 3 / 4),
			Cue.EyeClose => Glitch(open: false, level * 3 / 4),
			Cue.PetStroke => Blip(0.07, 1300.0 + strokes.NextDouble() * 500.0, 1.25, level / 2),
			Cue.PetHappy => Chime(level * 3 / 4),
			Cue.PetPurr => Mix(Chime(level * 3 / 5), Purr(level), 2400),
			Cue.PetEnd => Mix(Blip(0.12, 1500.0, 0.6, level / 2), Blip(0.16, 900.0, 0.55, level / 2), 2600),
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
