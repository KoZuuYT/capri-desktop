using System;
using System.Collections.Generic;
using System.Globalization;

namespace CapriDesktop;

internal static class VoiceFx
{
	internal sealed class Live
	{
		internal readonly int Rate;

		internal readonly int Pitch;

		internal readonly int Speed;

		internal readonly int Robot;

		internal readonly int Glitch;

		internal readonly Layering Layers;

		private readonly Stretcher stretcher;

		private readonly Resampler resampler;

		private readonly Vocoder vocoder;

		private readonly Glitcher glitcher;

		private readonly Layerer layerer;

		internal Live(int rate, int semitones, int speed, int robot, int glitch, int seed, bool timed)
			: this(rate, semitones, speed, robot, glitch, seed, timed, Layering.Off)
		{
		}

		internal Live(int rate, int semitones, int speed, int robot, int glitch, int seed, bool timed, Layering layers)
		{
			Rate = rate;
			Pitch = semitones;
			Speed = speed;
			Robot = robot;
			Glitch = glitch;
			Layers = layers ?? Layering.Off;
			double num = Lift(semitones);
			double num2 = (timed ? Stretch(num, speed) : 1.0);
			if (Math.Abs(num2 - 1.0) > 0.001)
			{
				stretcher = new Stretcher(rate, num2);
			}
			if (semitones != 0)
			{
				resampler = new Resampler(num);
			}
			if (robot > 0)
			{
				vocoder = new Vocoder(rate, (double)robot / 100.0, seed);
			}
			if (glitch > 0)
			{
				glitcher = new Glitcher(rate, (double)glitch / 100.0, seed);
			}
			if (Layers.On)
			{
				layerer = new Layerer(rate, Layers);
			}
		}

		internal short[] Push(short[] chunk)
		{
			if (stretcher != null)
			{
				chunk = stretcher.Push(chunk);
			}
			if (resampler != null)
			{
				chunk = resampler.Push(chunk);
			}
			if (vocoder != null)
			{
				chunk = vocoder.Push(chunk);
			}
			if (glitcher != null)
			{
				chunk = glitcher.Push(chunk);
			}
			if (layerer != null)
			{
				chunk = layerer.Push(chunk);
			}
			return chunk;
		}

		internal short[] Flush()
		{
			short[] array = ((stretcher != null) ? stretcher.Flush() : new short[0]);
			if (resampler != null)
			{
				array = Join(resampler.Push(array), resampler.Flush());
			}
			if (vocoder != null)
			{
				array = Join(vocoder.Push(array), vocoder.Flush());
			}
			if (glitcher != null)
			{
				array = Join(glitcher.Push(array), glitcher.Flush());
			}
			if (layerer != null)
			{
				array = Join(layerer.Push(array), layerer.Flush());
			}
			return array;
		}

		internal short[] All(short[] pcm)
		{
			return Join(Push(pcm), Flush());
		}
	}

	internal sealed class Layering
	{
		internal const double MostPitch = 12.0;

		internal const int MostDelay = 250;

		internal const double HigherPitch = 2.0;

		internal const double LowerPitch = -2.5;

		internal const int Behind = 25;

		internal static readonly Layering Off = new Layering(on: false, 2.0, 25, -2.5, 25);

		internal readonly bool On;

		internal readonly double Pitch1;

		internal readonly int Delay1;

		internal readonly double Pitch2;

		internal readonly int Delay2;

		internal string Shown => (On ? "On" : "Off") + " | " + PitchName(Pitch1) + ", " + DelayName(Delay1) + " | " + PitchName(Pitch2) + ", " + DelayName(Delay2);

		internal Layering(bool on, double pitch1, int delay1, double pitch2, int delay2)
		{
			On = on;
			Pitch1 = Step(pitch1);
			Delay1 = Wait(delay1);
			Pitch2 = Step(pitch2);
			Delay2 = Wait(delay2);
		}

		internal static double Step(double semitones)
		{
			if (double.IsNaN(semitones) || double.IsInfinity(semitones))
			{
				return 0.0;
			}
			return Math.Max(-12.0, Math.Min(12.0, Math.Round(semitones * 2.0, MidpointRounding.AwayFromZero) / 2.0));
		}

		internal static int Wait(int ms)
		{
			return Math.Max(0, Math.Min(250, ms));
		}

		internal Layering Turned(bool on)
		{
			return new Layering(on, Pitch1, Delay1, Pitch2, Delay2);
		}

		internal Layering With(int layer, double pitch, int delay)
		{
			if (layer != 1)
			{
				return new Layering(On, Pitch1, Delay1, pitch, delay);
			}
			return new Layering(On, pitch, delay, Pitch2, Delay2);
		}

		internal double PitchOf(int layer)
		{
			if (layer != 1)
			{
				return Pitch2;
			}
			return Pitch1;
		}

		internal int DelayOf(int layer)
		{
			if (layer != 1)
			{
				return Delay2;
			}
			return Delay1;
		}

		internal bool Same(Layering other)
		{
			if (other != null && other.On == On && other.Pitch1 == Pitch1 && other.Delay1 == Delay1 && other.Pitch2 == Pitch2)
			{
				return other.Delay2 == Delay2;
			}
			return false;
		}

		internal static string PitchName(double semitones)
		{
			if (semitones == 0.0)
			{
				return "Same";
			}
			return ((semitones > 0.0) ? "+" : "-") + Math.Abs(semitones).ToString("0.#", CultureInfo.InvariantCulture);
		}

		internal static string DelayName(int ms)
		{
			return ms + " ms";
		}
	}

	private sealed class Layerer
	{
		private sealed class Copy
		{
			internal readonly int Delay;

			private readonly Stretcher stretcher;

			private readonly Resampler resampler;

			private readonly List<short> made = new List<short>();

			private long first;

			internal long Made => first + made.Count;

			internal Copy(int rate, double semitones, int ms)
			{
				Delay = (int)Math.Round((double)ms * (double)rate / 1000.0);
				if (semitones != 0.0)
				{
					double num = Math.Pow(2.0, semitones / 12.0);
					stretcher = new Stretcher(rate, Stretch(num, 100));
					resampler = new Resampler(num);
				}
			}

			internal void Push(short[] chunk)
			{
				made.AddRange((stretcher == null) ? chunk : resampler.Push(stretcher.Push(chunk)));
			}

			internal void Finish()
			{
				if (stretcher != null)
				{
					made.AddRange(resampler.Push(stretcher.Flush()));
					made.AddRange(resampler.Flush());
				}
			}

			internal double At(long index)
			{
				long num = index - first;
				return (num >= 0 && num < made.Count) ? made[(int)num] : 0;
			}

			internal void Drop(long before)
			{
				int num = (int)Math.Max(0L, Math.Min(made.Count, before - first));
				made.RemoveRange(0, num);
				first += num;
			}

			internal void Restart()
			{
				made.Clear();
				first = 0L;
			}
		}

		private readonly Copy[] copies;

		private readonly Limiter limiter;

		private readonly int longest;

		private readonly List<short> held = new List<short>();

		private long first;

		private long mixed;

		private long Received => first + held.Count;

		internal Layerer(int rate, Layering layers)
		{
			copies = new Copy[2]
			{
				new Copy(rate, layers.Pitch1, layers.Delay1),
				new Copy(rate, layers.Pitch2, layers.Delay2)
			};
			longest = Math.Max(copies[0].Delay, copies[1].Delay);
			limiter = new Limiter(rate);
		}

		internal short[] Push(short[] chunk)
		{
			held.AddRange(chunk);
			long num = Received;
			Copy[] array = copies;
			foreach (Copy copy in array)
			{
				copy.Push(chunk);
				num = Math.Min(num, copy.Made + copy.Delay);
			}
			return limiter.Push(Mix(num));
		}

		internal short[] Flush()
		{
			Copy[] array = copies;
			foreach (Copy copy in array)
			{
				copy.Finish();
			}
			short[] result = Join(limiter.Push(Mix(Received + longest)), limiter.Flush());
			held.Clear();
			first = (mixed = 0L);
			Copy[] array2 = copies;
			foreach (Copy copy2 in array2)
			{
				copy2.Restart();
			}
			return result;
		}

		private double[] Mix(long upTo)
		{
			double[] array = new double[Math.Max(0L, upTo - mixed)];
			int num = 0;
			while (num < array.Length)
			{
				long num2 = mixed - first;
				double num3 = ((num2 < held.Count) ? held[(int)num2] : 0);
				Copy[] array2 = copies;
				foreach (Copy copy in array2)
				{
					num3 += 0.4 * copy.At(mixed - copy.Delay);
				}
				array[num] = num3;
				num++;
				mixed++;
			}
			int num4 = (int)Math.Max(0L, Math.Min(held.Count, mixed - first));
			held.RemoveRange(0, num4);
			first += num4;
			Copy[] array3 = copies;
			foreach (Copy copy2 in array3)
			{
				copy2.Drop(mixed - copy2.Delay);
			}
			return array;
		}
	}

	private sealed class Limiter
	{
		internal const double Ceiling = 31000.0;

		private readonly int ahead;

		private readonly double letUp;

		private readonly double[] recent;

		private readonly List<double> held = new List<double>();

		private readonly List<double> lows = new List<double>();

		private readonly List<long> lowAt = new List<long>();

		private long taken;

		private double gain;

		private int under;

		internal Limiter(int rate)
		{
			ahead = Math.Max(1, rate / 400);
			letUp = Math.Exp(-1.0 / (0.06 * (double)rate));
			recent = new double[ahead + 1];
			Restart();
		}

		private void Restart()
		{
			held.Clear();
			lows.Clear();
			lowAt.Clear();
			taken = 0L;
			gain = 1.0;
			under = 0;
			for (int i = 0; i < recent.Length; i++)
			{
				recent[i] = 1.0;
			}
			for (int j = 0; j < ahead; j++)
			{
				Take(0.0, 0L, null);
			}
		}

		internal short[] Push(double[] chunk)
		{
			List<short> list = new List<short>(chunk.Length);
			foreach (double value in chunk)
			{
				Take(value, long.MaxValue, list);
			}
			return list.ToArray();
		}

		internal short[] Flush()
		{
			List<short> list = new List<short>();
			long end = taken;
			for (int i = 0; i < ahead; i++)
			{
				Take(0.0, end, list);
			}
			Restart();
			return list.ToArray();
		}

		private void Take(double value, long end, List<short> output)
		{
			double num = Math.Abs(value);
			double num2 = ((num > 31000.0) ? (31000.0 / num) : 1.0);
			while (lows.Count > 0 && lows[lows.Count - 1] >= num2)
			{
				lows.RemoveAt(lows.Count - 1);
				lowAt.RemoveAt(lowAt.Count - 1);
			}
			lows.Add(num2);
			lowAt.Add(taken);
			held.Add(value);
			long num3 = taken++ - ahead;
			if (num3 < 0)
			{
				return;
			}
			while (lowAt[0] < num3)
			{
				lowAt.RemoveAt(0);
				lows.RemoveAt(0);
			}
			gain = Math.Min(lows[0], 1.0 - (1.0 - gain) * letUp);
			if (gain > 0.99999 && lows[0] >= 1.0)
			{
				gain = 1.0;
			}
			int num4 = (int)(num3 % recent.Length);
			if (recent[num4] < 1.0)
			{
				under--;
			}
			recent[num4] = gain;
			if (gain < 1.0)
			{
				under++;
			}
			double num5 = 1.0;
			if (under > 0)
			{
				num5 = 0.0;
				double[] array = recent;
				foreach (double num6 in array)
				{
					num5 += num6;
				}
				num5 /= (double)recent.Length;
			}
			double num7 = held[0];
			held.RemoveAt(0);
			if (num3 >= ahead && num3 < end)
			{
				output.Add(Clip(num7 * num5));
			}
		}
	}

	private sealed class Resampler
	{
		private const int Phases = 256;

		private const int Reach = 16;

		private readonly double factor;

		private readonly int reach;

		private readonly int taps;

		private readonly double[] kernel;

		private readonly List<short> held = new List<short>();

		private long first;

		private long made;

		internal Resampler(double factor)
		{
			this.factor = factor;
			double num = Math.Min(1.0, 1.0 / factor);
			reach = (int)Math.Ceiling(16.0 / num);
			taps = 2 * reach;
			kernel = new double[257 * taps];
			for (int i = 0; i <= 256; i++)
			{
				double num2 = (double)i / 256.0;
				double num3 = 0.0;
				for (int j = 0; j < taps; j++)
				{
					double num4 = (double)(j - reach + 1) - num2;
					double num5 = num4 * num;
					double num6 = ((Math.Abs(num5) < 1E-09) ? 1.0 : (Math.Sin(Math.PI * num5) / (Math.PI * num5))) * ((Math.Abs(num4) >= (double)reach) ? 0.0 : (0.5 + 0.5 * Math.Cos(Math.PI * num4 / (double)reach)));
					kernel[i * taps + j] = num6;
					num3 += num6;
				}
				for (int k = 0; k < taps; k++)
				{
					kernel[i * taps + k] /= num3;
				}
			}
		}

		internal short[] Push(short[] chunk)
		{
			held.AddRange(chunk);
			List<short> list = new List<short>();
			while ((long)Math.Floor((double)made * factor) + reach < first + held.Count)
			{
				list.Add(Next());
			}
			int num = (int)Math.Max(0L, Math.Min(held.Count, (long)Math.Floor((double)made * factor) - reach + 1 - first));
			held.RemoveRange(0, num);
			first += num;
			return list.ToArray();
		}

		internal short[] Flush()
		{
			List<short> list = new List<short>();
			long num = (long)Math.Round((double)(first + held.Count) / factor);
			while (made < num)
			{
				list.Add(Next());
			}
			held.Clear();
			first = (made = 0L);
			return list.ToArray();
		}

		private short Next()
		{
			double num = (double)made * factor;
			long num2 = (long)Math.Floor(num);
			int num3 = (int)Math.Round((num - (double)num2) * 256.0) * taps;
			long num4 = num2 - reach + 1 - first;
			double num5 = 0.0;
			for (int i = 0; i < taps; i++)
			{
				long num6 = num4 + i;
				if (num6 >= 0 && num6 < held.Count)
				{
					num5 += (double)held[(int)num6] * kernel[num3 + i];
				}
			}
			made++;
			return Clip(num5);
		}
	}

	private sealed class Stretcher
	{
		private readonly double stretch;

		private readonly int frame;

		private readonly int hop;

		private readonly int tolerance;

		private readonly double[] window;

		private readonly double[] near;

		private readonly double[] span;

		private readonly List<short> held = new List<short>();

		private readonly List<double> sum = new List<double>();

		private readonly List<double> weight = new List<double>();

		private long first;

		private long emitted;

		private long placed;

		private long previous = -1L;

		private long Received => first + held.Count;

		private long Nominal => (long)Math.Round((double)(placed * hop) / stretch);

		internal Stretcher(int rate, double stretch)
		{
			this.stretch = stretch;
			frame = Math.Max(32, rate / 50) & -2;
			hop = frame / 2;
			tolerance = Math.Max(1, rate / 200);
			near = new double[(frame + 3) / 4];
			span = new double[2 * tolerance + frame];
			window = new double[frame];
			for (int i = 0; i < frame; i++)
			{
				window[i] = 0.5 - 0.5 * Math.Cos(Math.PI * 2.0 * (double)i / (double)frame);
			}
		}

		internal short[] Push(short[] chunk)
		{
			held.AddRange(chunk);
			while (Nominal + tolerance + frame <= Received && (previous < 0 || previous + hop + frame <= Received))
			{
				Place(Nominal, long.MaxValue);
			}
			return Emit(placed * hop);
		}

		internal short[] Flush()
		{
			long received = Received;
			long num = (long)Math.Round((double)received * stretch);
			long num2 = received - frame;
			short[] result;
			if (placed == 0 && (num2 < 2 * tolerance || num < frame))
			{
				result = held.ToArray();
			}
			else
			{
				while (placed * hop < num)
				{
					Place(Math.Min(num2, Nominal), num2);
				}
				result = Emit(num);
			}
			held.Clear();
			sum.Clear();
			weight.Clear();
			first = (emitted = (placed = 0L));
			previous = -1L;
			return result;
		}

		private void Place(long nominal, long last)
		{
			long num = nominal;
			if (previous >= 0)
			{
				long num2 = Math.Min(last, previous + hop);
				double num3 = double.MinValue;
				int num4 = 0;
				int num5 = 0;
				while (num4 < frame)
				{
					near[num5] = At(num2 + num4);
					num4 += 4;
					num5++;
				}
				for (int i = 0; i < span.Length; i++)
				{
					span[i] = At(nominal - tolerance + i);
				}
				for (int j = -tolerance; j <= tolerance; j++)
				{
					long num6 = nominal + j;
					if (num6 >= 0 && num6 <= last)
					{
						double num7 = 0.0;
						double num8 = 1.0;
						int num9 = 0;
						int num10 = 0;
						int num11 = j + tolerance;
						while (num9 < frame)
						{
							double num12 = span[num11 + num9];
							num7 += near[num10] * num12;
							num8 += num12 * num12;
							num9 += 4;
							num10++;
						}
						double num13 = num7 / Math.Sqrt(num8);
						if (num13 > num3)
						{
							num3 = num13;
							num = num6;
						}
					}
				}
			}
			long num14 = placed * hop;
			while (emitted + sum.Count < num14 + frame)
			{
				sum.Add(0.0);
				weight.Add(0.0);
			}
			for (int k = 0; k < frame; k++)
			{
				int index = (int)(num14 + k - emitted);
				sum[index] += window[k] * At(num + k);
				weight[index] += window[k];
			}
			previous = num;
			placed++;
		}

		private double At(long index)
		{
			long num = index - first;
			return (num >= 0 && num < held.Count) ? held[(int)num] : 0;
		}

		private short[] Emit(long upTo)
		{
			int num = (int)Math.Max(0L, upTo - emitted);
			short[] array = new short[num];
			for (int i = 0; i < num && i < sum.Count; i++)
			{
				array[i] = (short)((weight[i] > 0.001) ? Clip(sum[i] / weight[i]) : 0);
			}
			int count = Math.Min(num, sum.Count);
			sum.RemoveRange(0, count);
			weight.RemoveRange(0, count);
			emitted += num;
			long num2 = Math.Min(Math.Min((previous >= 0) ? (previous + hop) : Nominal, Nominal - tolerance), Received - frame - tolerance);
			int num3 = (int)Math.Max(0L, Math.Min(held.Count, num2 - first));
			held.RemoveRange(0, num3);
			first += num3;
			return array;
		}
	}

	private sealed class Glitcher
	{
		private readonly int rate;

		private readonly int block;

		private readonly int fade;

		private readonly int reach;

		private readonly double amount;

		private readonly double perSecond;

		private readonly Random random;

		private readonly List<short> held = new List<short>();

		private readonly List<double> peaks = new List<double>();

		private long first;

		private long peakBase;

		private long peaked;

		private long at;

		private long next;

		private double loudest;

		internal int Events;

		private long Received => first + held.Count;

		internal Glitcher(int rate, double amount, int seed)
		{
			this.rate = rate;
			this.amount = amount;
			block = Math.Max(1, rate / 100);
			fade = Math.Max(1, rate / 400);
			reach = (int)((double)rate * 0.16) + fade;
			perSecond = amount * (2.0 + amount);
			random = new Random(seed);
			Restart();
		}

		private void Restart()
		{
			held.Clear();
			peaks.Clear();
			first = (peakBase = (peaked = (at = 0L)));
			loudest = 0.0;
			next = (long)((double)rate * (0.1 + random.NextDouble() / perSecond));
		}

		internal short[] Push(short[] chunk)
		{
			held.AddRange(chunk);
			List<short> list = new List<short>();
			Run(list, -1L);
			return list.ToArray();
		}

		internal short[] Flush()
		{
			List<short> list = new List<short>();
			long received = Received;
			if (received / block >= 20)
			{
				Run(list, received);
			}
			else
			{
				Copy(list, received);
			}
			Restart();
			return list.ToArray();
		}

		private void Run(List<short> output, long length)
		{
			bool flag = length >= 0;
			long num = (flag ? length : Received) / block;
			long num2 = (flag ? (length - rate / 10) : long.MaxValue);
			while (peaked < num)
			{
				loudest = Math.Max(loudest, Level(peaked));
				peaks.Add(loudest);
				peaked++;
			}
			while (next < num2)
			{
				long num3 = next / block;
				long num4 = Math.Min(num, num3 + 12);
				if (!flag && num3 + 12 > num)
				{
					break;
				}
				long num5 = -1L;
				if (num4 > num3)
				{
					double num6 = 0.15 * peaks[(int)(num4 - 1 - peakBase)];
					for (long num7 = num3; num7 < num4; num7++)
					{
						if (Level(num7) > num6)
						{
							num5 = num7;
							break;
						}
					}
				}
				if (num5 < 0)
				{
					next += block * 12;
					continue;
				}
				long num8 = Math.Max(at, num5 * block);
				if (flag ? (num8 >= num2) : (num8 + reach >= Received))
				{
					break;
				}
				Copy(output, num8);
				Event(output, num8, flag ? length : long.MaxValue);
			}
			Copy(output, flag ? length : Math.Max(at, Math.Min(Received, next / block * block)));
			int num9 = (int)Math.Max(0L, Math.Min(held.Count, Math.Min(Math.Min(at, next / block * block), peaked * block) - first));
			held.RemoveRange(0, num9);
			first += num9;
			int num10 = (int)Math.Max(0L, Math.Min(peaks.Count, next / block - peakBase));
			peaks.RemoveRange(0, num10);
			peakBase += num10;
		}

		private void Event(List<short> output, long start, long length)
		{
			double num = random.NextDouble();
			if (num < 0.55)
			{
				int num2 = (int)((double)rate * (0.03 + (0.02 + 0.04 * amount) * random.NextDouble()));
				int num3 = 1 + ((random.NextDouble() < amount) ? 1 : 0) + ((random.NextDouble() < amount / 2.0) ? 1 : 0);
				for (int i = 0; i < num3; i++)
				{
					for (int j = 0; j < num2; j++)
					{
						output.Add((short)((double)At(start + j) * Math.Min(1.0, (double)Math.Min((i == 0) ? fade : j, num2 - 1 - j) / (double)fade)));
					}
				}
				for (int k = 0; k < fade; k++)
				{
					output.Add((short)((double)(At(start + k) * k) / (double)fade));
				}
				at = start + fade;
			}
			else if (num < 0.85)
			{
				int num4 = (int)Math.Min(length - start, (long)((double)rate * (0.04 + (0.04 + 0.08 * amount) * random.NextDouble())));
				int num5 = 3 + (int)(random.NextDouble() * (3.0 + 9.0 * amount));
				int num6 = 1 << 8 + (int)Math.Round(4.0 * amount);
				int num7 = 0;
				for (int l = 0; l < num4; l++)
				{
					if (l % num5 == 0)
					{
						num7 = At(start + l) / num6 * num6;
					}
					double num8 = Math.Min(1.0, (double)Math.Min(l, num4 - 1 - l) / (double)fade);
					output.Add((short)((double)At(start + l) * (1.0 - num8) + (double)num7 * num8));
				}
				at = start + num4;
			}
			else
			{
				int num9 = (int)((double)rate * (0.02 + (0.01 + 0.02 * amount) * random.NextDouble()));
				for (int m = 0; m < fade; m++)
				{
					output.Add((short)((double)At(start + m) * (1.0 - (double)m / (double)fade) + (double)(At(start + num9 + m) * m) / (double)fade));
				}
				at = start + num9 + fade;
			}
			Events++;
			next = at + (long)((double)rate * (0.6 + 0.8 * random.NextDouble()) / perSecond);
		}

		private double Level(long b)
		{
			double num = 0.0;
			for (long num2 = b * block; num2 < (b + 1) * block; num2++)
			{
				double num3 = At(num2);
				num += num3 * num3;
			}
			return Math.Sqrt(num / (double)block);
		}

		private short At(long index)
		{
			long num = index - first;
			if (num < 0 || num >= held.Count)
			{
				return 0;
			}
			return held[(int)num];
		}

		private void Copy(List<short> output, long upTo)
		{
			for (long num = at; num < upTo; num++)
			{
				output.Add(At(num));
			}
			at = Math.Max(at, upTo);
		}
	}

	private sealed class Vocoder
	{
		private readonly int rate;

		private readonly int size;

		private readonly int hop;

		private readonly int lifter;

		private readonly int reach;

		private readonly int least;

		private readonly int most;

		private readonly int lead;

		private readonly int seed;

		private readonly double dry;

		private readonly double wet;

		private readonly double snap;

		private readonly double glideStep;

		private readonly double voiceStep;

		private readonly double[] window;

		private readonly double[] tilt;

		private readonly double[] cosines;

		private readonly double[] sines;

		private readonly double[] frame;

		private readonly double[] smooth;

		private readonly double[] difference;

		private readonly double[] voiceRe;

		private readonly double[] voiceIm;

		private readonly double[] shapeRe;

		private readonly double[] shapeIm;

		private readonly double[] envelope;

		private readonly double[] synthRe;

		private readonly double[] synthIm;

		private readonly double[] around = new double[5];

		private readonly List<short> held = new List<short>();

		private readonly List<double> pitches = new List<double>();

		private readonly List<double> clarities = new List<double>();

		private readonly List<double> targets = new List<double>();

		private readonly List<double> voicings = new List<double>();

		private readonly List<double> carrier = new List<double>();

		private readonly List<double> centres = new List<double>();

		private readonly List<double> sum = new List<double>();

		private readonly List<double> weight = new List<double>();

		private long first;

		private long analyzed;

		private long pitchBase;

		private long targeted;

		private long targetBase;

		private long made;

		private long carrierFirst;

		private long centreBase;

		private long spoken;

		private long sumFirst;

		private long emitted;

		private double loudest;

		private double note;

		private double glide;

		private double voiced;

		private double phase;

		private bool sung;

		private long Received => first + held.Count;

		internal Vocoder(int rate, double amount, int seed)
		{
			this.rate = rate;
			this.seed = seed;
			size = ((rate >= 32000) ? 2048 : ((rate >= 20000) ? 1024 : 512));
			hop = size / 4;
			lifter = Math.Max(8, (int)Math.Round((double)rate * 0.002));
			reach = Math.Max(1, (int)Math.Round(150.0 * (double)size / (double)rate));
			least = Math.Max(2, rate / 900);
			most = Math.Min(size / 2 - 1, rate / 60);
			dry = Math.Cos(amount * Math.PI / 2.0);
			wet = Math.Sin(amount * Math.PI / 2.0);
			snap = Math.Min(1.0, 0.5 + 0.5 * amount);
			glideStep = 1.0 - Math.Exp(-1.0 / (0.015 * (double)rate));
			voiceStep = 1.0 - Math.Exp(-1.0 / (0.01 * (double)rate));
			lead = (int)Math.Round(0.015 * (double)rate);
			window = new double[size];
			tilt = new double[size / 2 + 1];
			cosines = new double[size / 2];
			sines = new double[size / 2];
			for (int i = 0; i < size; i++)
			{
				window[i] = 0.5 - 0.5 * Math.Cos(Math.PI * 2.0 * (double)i / (double)size);
			}
			for (int j = 0; j <= size / 2; j++)
			{
				double num = (double)j * (double)rate / (double)size;
				tilt[j] = (double)j / Math.Sqrt(1.0 + num * num / 25000000.0);
			}
			for (int k = 0; k < size / 2; k++)
			{
				cosines[k] = Math.Cos(Math.PI * 2.0 * (double)k / (double)size);
				sines[k] = Math.Sin(Math.PI * 2.0 * (double)k / (double)size);
			}
			frame = new double[size];
			smooth = new double[size];
			difference = new double[most + 2];
			voiceRe = new double[size];
			voiceIm = new double[size];
			shapeRe = new double[size];
			shapeIm = new double[size];
			envelope = new double[size / 2 + 1];
			synthRe = new double[size];
			synthIm = new double[size];
			Restart();
		}

		private void Restart()
		{
			held.Clear();
			pitches.Clear();
			clarities.Clear();
			targets.Clear();
			voicings.Clear();
			carrier.Clear();
			centres.Clear();
			sum.Clear();
			weight.Clear();
			long num = -hop - size / 2;
			first = (analyzed = (pitchBase = (targeted = (targetBase = (emitted = 0L)))));
			made = (carrierFirst = (sumFirst = num));
			centreBase = num / hop;
			spoken = -1L;
			loudest = (glide = (voiced = (phase = 0.0)));
			note = double.NaN;
			sung = false;
		}

		internal short[] Push(short[] chunk)
		{
			held.AddRange(chunk);
			List<short> list = new List<short>();
			Run(list, ending: false);
			return list.ToArray();
		}

		internal short[] Flush()
		{
			List<short> list = new List<short>();
			if (Received > 0)
			{
				Run(list, ending: true);
			}
			Restart();
			return list.ToArray();
		}

		private void Run(List<short> output, bool ending)
		{
			long received = Received;
			long num = (ending ? FloorDiv(received - 1 + size / 2, hop) : long.MaxValue);
			long num2 = (ending ? (num * hop + size / 2) : long.MaxValue);
			long num3 = (ending ? (NoteFor(num2 - 1) + 1) : long.MaxValue);
			long num4 = (ending ? (num3 + 2) : long.MaxValue);
			while (analyzed < num4 && (ending || analyzed * hop + size / 2 <= received))
			{
				Analyze(analyzed++);
			}
			while (targeted < num3 && (ending || targeted + 2 < analyzed))
			{
				Target(targeted++);
			}
			while (made < num2 && (ending || NoteFor(made) < targeted))
			{
				Sing(made++);
			}
			while (spoken <= num && (ending || (spoken * hop + size / 2 <= received && spoken * hop + size / 2 <= made)))
			{
				Speak(spoken++);
			}
			long num5 = (ending ? received : Math.Min(received, spoken * hop - size / 2));
			for (long num6 = emitted; num6 < num5; num6++)
			{
				int num7 = (int)(num6 - sumFirst);
				double num8 = ((num7 < weight.Count && weight[num7] > 1E-09) ? (sum[num7] / weight[num7]) : 0.0);
				output.Add(Soft(In(num6) * dry + num8 * wet));
			}
			emitted = Math.Max(emitted, num5);
			long num9 = spoken * hop - size / 2;
			sumFirst += Drop(Math.Min(emitted, num9) - sumFirst, sum, weight);
			first += Drop(Math.Min(emitted, Math.Min(num9, analyzed * hop - size / 2)) - first, held);
			carrierFirst += Drop(num9 - carrierFirst, carrier, null);
			centreBase += Drop(spoken - centreBase, centres, null);
			pitchBase += Drop(targeted - 2 - pitchBase, pitches, clarities);
			targetBase += Drop(NoteFor(made) - targetBase, targets, voicings);
		}

		private long NoteFor(long sample)
		{
			return FloorDiv(sample + lead + hop / 2, hop);
		}

		private static int Drop(long count, List<double> list, List<double> beside)
		{
			int num = (int)Math.Max(0L, Math.Min(list.Count, count));
			list.RemoveRange(0, num);
			beside?.RemoveRange(0, num);
			return num;
		}

		private static int Drop(long count, List<short> list)
		{
			int num = (int)Math.Max(0L, Math.Min(list.Count, count));
			list.RemoveRange(0, num);
			return num;
		}

		private static long FloorDiv(long value, long by)
		{
			if (value < 0)
			{
				return -((-value + by - 1) / by);
			}
			return value / by;
		}

		private double In(long index)
		{
			long num = index - first;
			return (index >= 0 && num >= 0 && num < held.Count) ? held[(int)num] : 0;
		}

		private void Analyze(long u)
		{
			long num = u * hop - size / 2;
			for (int i = 0; i < size; i++)
			{
				frame[i] = In(num + i);
			}
			double num2 = 0.0;
			for (int j = 0; j < size; j++)
			{
				double num3 = frame[j];
				smooth[j] = 0.5 * num3 + 0.25 * ((j > 0) ? frame[j - 1] : num3) + 0.25 * ((j + 1 < size) ? frame[j + 1] : num3);
				if (j >= size / 4 && j < size * 3 / 4)
				{
					num2 += num3 * num3;
				}
			}
			double num4 = Math.Sqrt(num2 / (double)(size / 2));
			double item = 0.0;
			double item2 = 0.0;
			loudest = Math.Max(loudest, num4);
			if (num4 > 20.0 && num4 > 0.03 * loudest)
			{
				int num5 = size / 2;
				double num6 = 0.0;
				difference[0] = 1.0;
				for (int k = 1; k <= most + 1; k++)
				{
					int num7 = (size - num5 - k) / 2;
					double num8 = 0.0;
					for (int l = num7; l < num7 + num5; l++)
					{
						double num9 = smooth[l] - smooth[l + k];
						num8 += num9 * num9;
					}
					num6 += num8;
					difference[k] = ((num6 > 0.0) ? (num8 * (double)k / num6) : 1.0);
				}
				int num10 = -1;
				for (int m = least; m <= most; m++)
				{
					if (num10 >= 0)
					{
						break;
					}
					if (difference[m] < 0.15)
					{
						for (; m < most && difference[m + 1] < difference[m]; m++)
						{
						}
						num10 = m;
					}
				}
				if (num10 < 0)
				{
					num10 = least;
					for (int n = least + 1; n <= most; n++)
					{
						if (difference[n] < difference[num10])
						{
							num10 = n;
						}
					}
				}
				double num11 = difference[num10];
				if (num11 < 0.35)
				{
					double num12 = difference[num10 - 1];
					double num13 = difference[num10 + 1];
					double num14 = num12 - 2.0 * num11 + num13;
					double val = ((num14 > 1E-12) ? (0.5 * (num12 - num13) / num14) : 0.0);
					item = (double)rate / ((double)num10 + Math.Max(-1.0, Math.Min(1.0, val)));
					item2 = Math.Max(0.0, Math.Min(1.0, (0.35 - num11) / 0.2));
				}
			}
			pitches.Add(item);
			clarities.Add(item2);
		}

		private double PitchAt(long u)
		{
			if (u < pitchBase || u >= analyzed)
			{
				return 0.0;
			}
			return pitches[(int)(u - pitchBase)];
		}

		private void Target(long u)
		{
			double num = PitchAt(u);
			double item = double.NaN;
			double item2 = 0.0;
			if (num > 0.0)
			{
				int num2 = 0;
				for (long num3 = u - 2; num3 <= u + 2; num3++)
				{
					double num4 = PitchAt(num3);
					if (num4 > 0.0)
					{
						around[num2++] = num4;
					}
				}
				if (num2 >= 3)
				{
					Array.Sort(around, 0, num2);
					num = around[num2 / 2];
				}
				double num5 = 69.0 + 12.0 * Math.Log(num / 440.0) / Math.Log(2.0);
				if (double.IsNaN(note) || Math.Abs(num5 - note) > 0.65)
				{
					note = Math.Round(num5);
				}
				item = num5 + snap * (note - num5);
				item2 = clarities[(int)(u - pitchBase)];
			}
			targets.Add(item);
			voicings.Add(item2);
		}

		private void Sing(long j)
		{
			long num = NoteFor(j);
			bool flag = num >= targetBase && num < targeted;
			double num2 = (flag ? targets[(int)(num - targetBase)] : double.NaN);
			double num3 = (flag ? voicings[(int)(num - targetBase)] : 0.0);
			if (!double.IsNaN(num2))
			{
				if (!sung)
				{
					glide = num2;
					sung = true;
				}
				glide += (num2 - glide) * glideStep;
			}
			voiced += (num3 - voiced) * voiceStep;
			double num4 = Math.Min(0.45, (sung ? (440.0 * Math.Pow(2.0, (glide - 69.0) / 12.0)) : 220.0) / (double)rate);
			phase += num4;
			if (phase >= 1.0)
			{
				phase--;
			}
			double num5 = 0.0;
			if (phase < num4)
			{
				double num6 = phase / num4;
				num5 = num6 + num6 - num6 * num6 - 1.0;
			}
			else if (phase > 1.0 - num4)
			{
				double num7 = (phase - 1.0) / num4;
				num5 = num7 * num7 + num7 + num7 + 1.0;
			}
			carrier.Add(2.0 * phase - 1.0 - num5);
			if (j % hop == 0)
			{
				centres.Add(voiced);
			}
		}

		private void Speak(long t)
		{
			long num = t * hop - size / 2;
			int num2 = size / 2;
			for (int i = 0; i < size; i++)
			{
				voiceRe[i] = In(num + i) * window[i];
				voiceIm[i] = 0.0;
			}
			Transform(voiceRe, voiceIm, inverse: false);
			double num3 = 0.0;
			for (int j = 0; j <= num2; j++)
			{
				envelope[j] = voiceRe[j] * voiceRe[j] + voiceIm[j] * voiceIm[j];
				num3 += ((j == 0 || j == num2) ? envelope[j] : (2.0 * envelope[j]));
			}
			for (int k = 0; k <= num2; k++)
			{
				int num4 = Math.Max(0, k - reach);
				int num5 = Math.Min(num2, k + reach);
				double num6 = 0.0;
				for (int l = num4; l <= num5; l++)
				{
					num6 += envelope[l];
				}
				shapeRe[k] = 0.5 * Math.Log(num6 / (double)(num5 - num4 + 1) + 0.01);
				shapeIm[k] = 0.0;
			}
			for (int m = num2 + 1; m < size; m++)
			{
				shapeRe[m] = shapeRe[size - m];
				shapeIm[m] = 0.0;
			}
			Transform(shapeRe, shapeIm, inverse: true);
			for (int n = 0; n < size; n++)
			{
				int num7 = Math.Min(n, size - n);
				shapeRe[n] *= ((num7 <= lifter) ? (0.5 + 0.5 * Math.Cos(Math.PI * (double)num7 / (double)(lifter + 1))) : 0.0);
				shapeIm[n] = 0.0;
			}
			Transform(shapeRe, shapeIm, inverse: false);
			long num8 = num - carrierFirst;
			for (int num9 = 0; num9 < size; num9++)
			{
				synthRe[num9] = carrier[(int)(num8 + num9)] * window[num9];
				synthIm[num9] = 0.0;
			}
			Transform(synthRe, synthIm, inverse: false);
			double num10 = 0.0;
			double num11 = 0.0;
			for (int num12 = 1; num12 < num2; num12++)
			{
				envelope[num12] = Math.Exp(shapeRe[num12]);
				double num13 = envelope[num12] * tilt[num12];
				synthRe[num12] *= num13;
				synthIm[num12] *= num13;
				num10 += 2.0 * (synthRe[num12] * synthRe[num12] + synthIm[num12] * synthIm[num12]);
				num11 += 2.0 * envelope[num12] * envelope[num12];
			}
			double num14 = centres[(int)(t - centreBase)];
			double num15 = ((num10 > 1E-09) ? (Math.Sin(num14 * Math.PI / 2.0) * Math.Sqrt(num3 / num10)) : 0.0);
			double num16 = ((num11 > 1E-09) ? (Math.Cos(num14 * Math.PI / 2.0) * Math.Sqrt(num3 / num11)) : 0.0);
			Random random = new Random(seed + (int)t * 7919);
			voiceRe[0] = (voiceIm[0] = (voiceRe[num2] = (voiceIm[num2] = 0.0)));
			for (int num17 = 1; num17 < num2; num17++)
			{
				double num18 = Math.PI * 2.0 * random.NextDouble();
				double num19 = envelope[num17] * num16;
				voiceRe[num17] = num15 * synthRe[num17] + num19 * Math.Cos(num18);
				voiceIm[num17] = num15 * synthIm[num17] + num19 * Math.Sin(num18);
				voiceRe[size - num17] = voiceRe[num17];
				voiceIm[size - num17] = 0.0 - voiceIm[num17];
			}
			Transform(voiceRe, voiceIm, inverse: true);
			long num20 = num - sumFirst;
			while (sum.Count < num20 + size)
			{
				sum.Add(0.0);
				weight.Add(0.0);
			}
			for (int num21 = 0; num21 < size; num21++)
			{
				sum[(int)(num20 + num21)] += voiceRe[num21] * window[num21];
				weight[(int)(num20 + num21)] += window[num21] * window[num21];
			}
		}

		private void Transform(double[] real, double[] imaginary, bool inverse)
		{
			int num = size;
			int i = 1;
			int num2 = 0;
			for (; i < num; i++)
			{
				int num3 = num >> 1;
				while ((num2 & num3) != 0)
				{
					num2 ^= num3;
					num3 >>= 1;
				}
				num2 ^= num3;
				if (i < num2)
				{
					double num4 = real[i];
					real[i] = real[num2];
					real[num2] = num4;
					num4 = imaginary[i];
					imaginary[i] = imaginary[num2];
					imaginary[num2] = num4;
				}
			}
			for (int num5 = 2; num5 <= num; num5 <<= 1)
			{
				int num6 = num5 >> 1;
				int num7 = num / num5;
				for (int j = 0; j < num; j += num5)
				{
					for (int k = 0; k < num6; k++)
					{
						double num8 = cosines[k * num7];
						double num9 = (inverse ? sines[k * num7] : (0.0 - sines[k * num7]));
						int num10 = j + k;
						int num11 = num10 + num6;
						double num12 = real[num11] * num8 - imaginary[num11] * num9;
						double num13 = real[num11] * num9 + imaginary[num11] * num8;
						real[num11] = real[num10] - num12;
						imaginary[num11] = imaginary[num10] - num13;
						real[num10] += num12;
						imaginary[num10] += num13;
					}
				}
			}
			if (inverse)
			{
				for (int l = 0; l < num; l++)
				{
					real[l] /= num;
					imaginary[l] /= num;
				}
			}
		}
	}

	internal const int MostPitch = 6;

	internal const int Slowest = 70;

	internal const int Fastest = 150;

	internal const int HdRobotDefault = 0;

	internal const double LayerLevel = 0.4;

	internal static volatile int Pitch;

	internal static volatile int Speed = 100;

	internal static volatile int Robot;

	internal static volatile int HdRobot = 0;

	internal static volatile int ServerRobot;

	internal static volatile int Glitch;

	internal static volatile int HdGlitch;

	internal static volatile int ServerGlitch;

	internal static volatile Layering Layers = Layering.Off;

	internal static double Lift(int semitones)
	{
		return Math.Pow(2.0, (double)semitones / 12.0);
	}

	internal static float Tempo(float style, int semitones, int speed)
	{
		if (semitones == 0 && speed == 100)
		{
			return style;
		}
		return (float)Math.Max(0.5, Math.Min(3.0, (double)(style * (float)speed) / 100.0 / Lift(semitones)));
	}

	internal static short[] Kokoro(short[] pcm, int rate, int semitones, int robot, int glitch, int seed)
	{
		return Kokoro(pcm, rate, semitones, robot, glitch, seed, Layering.Off);
	}

	internal static short[] Kokoro(short[] pcm, int rate, int semitones, int robot, int glitch, int seed, Layering layers)
	{
		if (Plain(semitones, 100, robot, glitch, layers))
		{
			return pcm;
		}
		return new Live(rate, semitones, 100, robot, glitch, seed, timed: false, layers).All(pcm);
	}

	internal static short[] Restyled(short[] pcm, int rate, int semitones, int speed, int robot, int glitch, int seed)
	{
		return Restyled(pcm, rate, semitones, speed, robot, glitch, seed, Layering.Off);
	}

	internal static short[] Restyled(short[] pcm, int rate, int semitones, int speed, int robot, int glitch, int seed, Layering layers)
	{
		if (Plain(semitones, speed, robot, glitch, layers))
		{
			return pcm;
		}
		return new Live(rate, semitones, speed, robot, glitch, seed, timed: true, layers).All(pcm);
	}

	internal static bool Plain(int semitones, int speed, int robot, int glitch, Layering layers)
	{
		if (semitones == 0 && speed == 100 && robot <= 0 && glitch <= 0)
		{
			if (layers != null)
			{
				return !layers.On;
			}
			return true;
		}
		return false;
	}

	internal static byte[] Styled(byte[] wav, string text)
	{
		int pitch = Pitch;
		int speed = Speed;
		int robot = Robot;
		int glitch = Glitch;
		Layering layers = Layers;
		if (wav == null || Plain(pitch, speed, robot, glitch, layers))
		{
			return wav;
		}
		short[] array = Voice.Samples(wav, out var rate);
		if (array.Length != 0)
		{
			return Voice.Wave(Restyled(array, rate, pitch, speed, robot, glitch, Seed(text), layers), rate);
		}
		return wav;
	}

	internal static int Seed(string text)
	{
		int num = -2128831035;
		string text2 = text ?? "";
		foreach (char c in text2)
		{
			num = (num ^ c) * 16777619;
		}
		return num & 0x7FFFFFFF;
	}

	internal static short[] Resample(short[] pcm, double factor)
	{
		Resampler resampler = new Resampler(factor);
		return Join(resampler.Push(pcm), resampler.Flush());
	}

	internal static short[] Glitched(short[] pcm, int rate, double amount, int seed)
	{
		int events;
		return Glitched(pcm, rate, amount, seed, out events);
	}

	internal static short[] Glitched(short[] pcm, int rate, double amount, int seed, out int events)
	{
		events = 0;
		if (amount <= 0.0)
		{
			return pcm;
		}
		Glitcher glitcher = new Glitcher(rate, amount, seed);
		short[] result = Join(glitcher.Push(pcm), glitcher.Flush());
		events = glitcher.Events;
		return result;
	}

	private static short[] Join(short[] one, short[] two)
	{
		if (two.Length == 0)
		{
			return one;
		}
		if (one.Length == 0)
		{
			return two;
		}
		short[] array = new short[one.Length + two.Length];
		Array.Copy(one, array, one.Length);
		Array.Copy(two, 0, array, one.Length, two.Length);
		return array;
	}

	private static short Clip(double value)
	{
		return (short)Math.Max(-32768.0, Math.Min(32767.0, Math.Round(value)));
	}

	private static short Soft(double value)
	{
		double num = Math.Abs(value);
		if (num > 28000.0)
		{
			value = (double)Math.Sign(value) * (28000.0 + 4767.0 * Math.Tanh((num - 28000.0) / 4767.0));
		}
		return Clip(value);
	}

	private static double Stretch(double lift, int speed)
	{
		return lift * 100.0 / (double)Math.Max(1, speed);
	}

	internal static string LayersCheck(out string found)
	{
		List<string> list = new List<string>();
		Random hiss = new Random(11);
		short[] array = new short[24000];
		short[] array2 = Line(24000, 2, hiss);
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = (short)(6000.0 * Math.Sin(Math.PI * 400.0 * (double)i / 24000.0) + 3000.0 * Math.Sin(Math.PI * 800.0 * (double)i / 24000.0) + 1500.0 * Math.Sin(Math.PI * 1200.0 * (double)i / 24000.0));
		}
		Layering layering = new Layering(on: false, 7.0, 200, -9.0, 90);
		if (Layering.Off.On || Kokoro(array, 24000, 0, 0, 0, 1, layering) != array || Restyled(array, 24000, 0, 100, 0, 0, 1, layering) != array || !Plain(0, 100, 0, 0, layering) || Plain(0, 100, 0, 0, layering.Turned(on: true)))
		{
			list.Add("off isn't untouched");
		}
		if (!Same(Restyled(array2, 24000, 3, 120, 30, 40, 5, layering), Restyled(array2, 24000, 3, 120, 30, 40, 5)) || !Same(Kokoro(array2, 24000, -2, 20, 10, 5, layering), Kokoro(array2, 24000, -2, 20, 10, 5)) || !Same(new Live(24000, 2, 90, 0, 20, 5, timed: true, layering).All(array2), new Live(24000, 2, 90, 0, 20, 5, timed: true).All(array2)))
		{
			list.Add("off changed a styled voice");
		}
		Layering layers = new Layering(on: true, 5.0, 100, -7.0, 200);
		short[] array3 = Restyled(array2, 24000, 0, 100, 0, 0, 5, layers);
		short[] sound = Restyled(array2, 24000, 5, 100, 0, 0, 5);
		short[] sound2 = Restyled(array2, 24000, -7, 100, 0, 0, 5);
		int num = 0;
		int num2 = 2400;
		int num3 = 4800;
		for (int j = 0; j < array3.Length; j++)
		{
			num = Math.Max(num, (int)Math.Abs((double)array3[j] - Math.Round(At(array2, j) + 0.4 * (At(sound, j - num2) + At(sound2, j - num3)))));
		}
		if (array3.Length != array2.Length + num3 || num > 1)
		{
			list.Add("on isn't her voice plus each layer at its delay (" + array3.Length + " samples, " + num + " off)");
		}
		int[] array4 = Voice.Mouths(Voice.Wave(array3, 24000), 12.0);
		bool flag = false;
		for (int k = array2.Length / 2000; k < array4.Length; k++)
		{
			flag |= array4[k] >= 0;
		}
		if (!flag)
		{
			list.Add("her mouth doesn't follow the layers' tail");
		}
		short[] array5 = new short[24000];
		for (int l = 0; l < array5.Length; l++)
		{
			array5[l] = (short)(8000.0 * Math.Sin(Math.PI * 440.0 * (double)l / 24000.0));
		}
		List<string> list2 = new List<string>();
		double[][] array6 = new double[2][]
		{
			new double[2] { 5.0, -7.0 },
			new double[2] { 1.5, -2.5 }
		};
		foreach (double[] array7 in array6)
		{
			short[] sound3 = Restyled(array5, 24000, 0, 100, 0, 0, 5, new Layering(on: true, array7[0], 25, array7[1], 25));
			double num4 = Peak(sound3, 24000, 220.0, out var strength);
			double num5 = Peak(sound3, 24000, 220.0 * Math.Pow(2.0, array7[0] / 12.0), out var strength2);
			double num6 = Peak(sound3, 24000, 220.0 * Math.Pow(2.0, array7[1] / 12.0), out var strength3);
			double num7 = num5 / num4;
			double num8 = num6 / num4;
			list2.Add(Layering.PitchName(array7[0]) + " x" + num7.ToString("0.000") + " at " + (20.0 * Math.Log10(strength2 / strength)).ToString("0.0") + " dB, " + Layering.PitchName(array7[1]) + " x" + num8.ToString("0.000") + " at " + (20.0 * Math.Log10(strength3 / strength)).ToString("0.0") + " dB");
			if (Math.Abs(num4 - 220.0) > 1.0 || Math.Abs(num7 / Math.Pow(2.0, array7[0] / 12.0) - 1.0) > 0.005 || Math.Abs(num8 / Math.Pow(2.0, array7[1] / 12.0) - 1.0) > 0.005 || strength2 / strength < 0.24 || strength2 / strength > 0.5599999999999999 || strength3 / strength < 0.24 || strength3 / strength > 0.5599999999999999)
			{
				list.Add("a layer's pitch is off (" + list2[list2.Count - 1] + ")");
			}
		}
		Random random = new Random(4);
		bool flag2 = true;
		int[][] array8 = new int[4][]
		{
			new int[5] { 24000, 0, 100, 0, 0 },
			new int[5] { 16000, 4, 120, 10, 50 },
			new int[5] { 22050, -3, 80, 30, 0 },
			new int[5] { 44100, 0, 100, 0, 0 }
		};
		foreach (int[] array9 in array8)
		{
			Layering layers2 = ((array9[1] == 0) ? new Layering(on: true, 2.0, 25, -2.5, 25) : new Layering(on: true, -12.0, 250, 12.0, 0));
			short[] array10 = Line(array9[0], 2, hiss);
			short[] other = new Live(array9[0], array9[1], array9[2], array9[3], array9[4], 77, timed: true, layers2).All(array10);
			Live live = new Live(array9[0], array9[1], array9[2], array9[3], array9[4], 77, timed: true, layers2);
			List<short> list3 = new List<short>();
			short[] array11;
			for (int n = 0; n < array10.Length; n += array11.Length)
			{
				array11 = new short[Math.Min(array10.Length - n, 1 + random.Next((random.Next(3) == 0) ? 20 : 4000))];
				Array.Copy(array10, n, array11, 0, array11.Length);
				list3.AddRange(live.Push(array11));
			}
			list3.AddRange(live.Flush());
			flag2 &= Same(list3.ToArray(), other) && (array9[4] > 0 || Same(live.All(array10), other));
		}
		if (!flag2)
		{
			list.Add("streamed in chunks, a layered voice came out different");
		}
		short[] array12 = new short[24000];
		for (int num9 = 0; num9 < array12.Length; num9++)
		{
			array12[num9] = (short)(31100.0 * Math.Sin(Math.PI * 400.0 * (double)num9 / 24000.0));
		}
		short[] array13 = Restyled(array12, 24000, 0, 100, 0, 0, 5, new Layering(on: true, 0.0, 0, 0.0, 0));
		short[] array14 = Restyled(array12, 24000, 0, 100, 0, 0, 5, new Layering(on: true, 3.0, 10, -5.0, 30));
		int num10 = 0;
		double num11 = 0.0;
		short[] array15 = array13;
		foreach (short num12 in array15)
		{
			num10 = Math.Max(num10, Math.Abs((int)num12));
			num11 += (double)num12 * (double)num12;
		}
		array15 = array14;
		foreach (short value in array15)
		{
			num10 = Math.Max(num10, Math.Abs((int)value));
		}
		double num13 = Math.Sqrt(num11 / (double)Math.Max(1, array13.Length));
		if (array13.Length != array12.Length || (double)num10 > 31000.0 || num13 < 20000.0)
		{
			list.Add("too loud isn't held under full scale (peak " + num10 + ", level " + num13.ToString("0") + ")");
		}
		found = "delays of 100 and 200 ms land sample for sample and ring out " + (array3.Length - array2.Length) * 1000 / 24000 + " ms past her; " + string.Join("; ", list2.ToArray()) + "; a 1.8x full-scale stack peaks at " + num10 + " of 32767";
		if (list.Count != 0)
		{
			return string.Join(" / ", list.ToArray());
		}
		return null;
	}

	private static short[] Line(int rate, int seconds, Random hiss)
	{
		short[] array = new short[rate * seconds];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = ((i % (rate / 4) < rate / 6) ? ((short)(5000.0 * Math.Sin(Math.PI * 340.0 * (double)i / (double)rate) + 1500.0 * Math.Sin(Math.PI * 1020.0 * (double)i / (double)rate))) : ((short)(hiss.Next(200) - 100)));
		}
		return array;
	}

	private static double At(short[] sound, long index)
	{
		return (index >= 0 && index < sound.Length) ? sound[index] : 0;
	}

	private static bool Same(short[] one, short[] other)
	{
		if (one.Length != other.Length)
		{
			return false;
		}
		for (int i = 0; i < one.Length; i++)
		{
			if (one[i] != other[i])
			{
				return false;
			}
		}
		return true;
	}

	private static double Peak(short[] sound, int rate, double near, out double strength)
	{
		int num = rate * 3 / 10;
		int num2 = rate * 6 / 10;
		double result = near;
		strength = 0.0;
		for (double num3 = near * 0.96; num3 <= near * 1.04; num3 += 0.25)
		{
			double num4 = 2.0 * Math.Cos(Math.PI * 2.0 * num3 / (double)rate);
			double num5 = 0.0;
			double num6 = 0.0;
			for (int i = 0; i < num2; i++)
			{
				double num7 = At(sound, num + i) * (0.5 - 0.5 * Math.Cos(Math.PI * 2.0 * (double)i / (double)(num2 - 1))) + num4 * num5 - num6;
				num6 = num5;
				num5 = num7;
			}
			double num8 = Math.Sqrt(Math.Max(0.0, num5 * num5 + num6 * num6 - num4 * num5 * num6));
			if (num8 > strength)
			{
				strength = num8;
				result = num3;
			}
		}
		return result;
	}
}
