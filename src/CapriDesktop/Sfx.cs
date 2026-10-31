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
			_ => Sweep(0.05, 1600.0, 1350.0, 0.003, level * 3 / 4),
		};
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
