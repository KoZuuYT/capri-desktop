using System;

namespace CapriDesktop;

internal sealed class Onset
{
	private const double Floor = 0.0001;

	internal readonly int Block;

	private readonly Biquad bass;

	private readonly Biquad midLow;

	private readonly Biquad midHigh;

	private readonly Biquad treble;

	private readonly double[] energy = new double[3];

	private readonly double[] before = new double[3];

	internal int Filled;

	private bool primed;

	internal Onset(int rate)
	{
		Block = Math.Max(1, rate / 100);
		bass = Biquad.Low(rate, 150.0);
		midLow = Biquad.High(rate, 300.0);
		midHigh = Biquad.Low(rate, 3000.0);
		treble = Biquad.High(rate, 5000.0);
	}

	internal bool Add(double sample, out float strength)
	{
		double num = bass.Run(sample);
		double num2 = midHigh.Run(midLow.Run(sample));
		double num3 = treble.Run(sample);
		energy[0] += num * num;
		energy[1] += num2 * num2;
		energy[2] += num3 * num3;
		strength = 0f;
		if (++Filled < Block)
		{
			return false;
		}
		double num4 = 0.0;
		for (int i = 0; i < 3; i++)
		{
			double num5 = Math.Log(0.0001 + Math.Sqrt(energy[i] / (double)Block));
			if (primed)
			{
				num4 += Math.Max(0.0, num5 - before[i]);
			}
			before[i] = num5;
			energy[i] = 0.0;
		}
		strength = (float)num4;
		Filled = 0;
		primed = true;
		return true;
	}
}
