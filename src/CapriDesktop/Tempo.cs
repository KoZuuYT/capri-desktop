using System;

namespace CapriDesktop;

internal static class Tempo
{
	internal const int Rate = 100;

	internal const double Slowest = 70.0;

	internal const double Fastest = 140.0;

	internal const double Clear = 2.3;

	internal static double Fold(double bpm)
	{
		if (bpm <= 0.0)
		{
			return 0.0;
		}
		while (bpm >= 140.0)
		{
			bpm /= 2.0;
		}
		while (bpm < 70.0)
		{
			bpm *= 2.0;
		}
		return bpm;
	}

	internal static double Toward(double bpm, double near)
	{
		if (bpm <= 0.0 || near <= 0.0)
		{
			return Fold(bpm);
		}
		while (bpm >= near * 1.414)
		{
			bpm /= 2.0;
		}
		while (bpm < near / 1.414)
		{
			bpm *= 2.0;
		}
		return bpm;
	}

	internal static bool Find(float[] onsets, int count, out double bpm, out double lastBeat, out double strength)
	{
		bpm = 0.0;
		lastBeat = 0.0;
		strength = 0.0;
		if (count < 400)
		{
			return false;
		}
		double[] array = new double[count];
		double num = 0.0;
		for (int i = 0; i < count; i++)
		{
			array[i] = onsets[i];
			num += array[i];
		}
		double num2 = 0.0;
		for (int j = 0; j < count; j++)
		{
			array[j] -= num / (double)count;
			num2 += array[j] * array[j];
		}
		num2 = Math.Sqrt(num2 / (double)count);
		if (num2 <= 1E-09)
		{
			return false;
		}
		double num3 = 0.0;
		double num4 = double.MinValue;
		double phase;
		for (double num5 = 30.0; num5 <= 120.0; num5 += 0.5)
		{
			double num6 = Math.Log(6000.0 / num5 / 120.0, 2.0);
			double num7 = Pulses(array, num5, 1.0, 0.5, out phase) * Math.Exp(-0.5 * num6 * num6);
			if (num7 > num4)
			{
				num4 = num7;
				num3 = num5;
			}
		}
		double num8 = num3;
		double num9 = double.MinValue;
		for (double num10 = num3 - 0.5; num10 <= num3 + 0.5; num10 += 0.05)
		{
			double num11 = Pulses(array, num10, 1.0, 0.5, out phase);
			if (num11 > num9)
			{
				num9 = num11;
				num8 = num10;
			}
		}
		Pulses(array, num8, 0.0, 0.0, out phase);
		bpm = 6000.0 / num8;
		lastBeat = phase / 100.0;
		strength = num9 / num2;
		return strength >= 2.3;
	}

	private static double Pulses(double[] onset, double period, double half, double quarter, out double phase)
	{
		int num = onset.Length;
		double num2 = double.MinValue;
		phase = 0.0;
		for (int i = 0; (double)i < period; i++)
		{
			double num3 = 0.0;
			int num4 = 0;
			for (double num5 = num - 1 - i; num5 >= 0.0; num5 -= period)
			{
				num3 += At(onset, num5) + half * At(onset, num5 - period / 2.0);
				if (quarter > 0.0)
				{
					num3 += quarter * (At(onset, num5 - period / 4.0) + At(onset, num5 - period * 3.0 / 4.0));
				}
				num4++;
			}
			num3 /= (double)num4;
			if (num3 > num2)
			{
				num2 = num3;
				phase = i;
			}
		}
		return num2;
	}

	private static double At(double[] onset, double x)
	{
		int num = (int)Math.Floor(x);
		if (num < 0 || num >= onset.Length - 1)
		{
			return 0.0;
		}
		double num2 = x - (double)num;
		return onset[num] * (1.0 - num2) + onset[num + 1] * num2;
	}
}
