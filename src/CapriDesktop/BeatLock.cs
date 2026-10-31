using System;

namespace CapriDesktop;

internal sealed class BeatLock
{
	internal double Bpm;

	internal double BeatAt;

	private double pending;

	private int agree;

	private int misses;

	internal void Take(bool steady, double bpm, double beatAt)
	{
		if (!steady)
		{
			if (++misses >= 8)
			{
				Bpm = 0.0;
				pending = 0.0;
				agree = 0;
			}
			return;
		}
		misses = 0;
		double num = Tempo.Toward(bpm, Bpm);
		if (Bpm > 0.0 && Near(num, Bpm))
		{
			Bpm = Bpm * 0.8 + num * 0.2;
			BeatAt = beatAt;
			pending = 0.0;
			agree = 0;
			return;
		}
		double num2 = Tempo.Toward(bpm, pending);
		if (pending > 0.0 && Near(num2, pending))
		{
			agree++;
		}
		else
		{
			num2 = (pending = Tempo.Fold(bpm));
			agree = 1;
		}
		if (agree >= ((Bpm > 0.0) ? 3 : 2))
		{
			Bpm = num2;
			BeatAt = beatAt;
			pending = 0.0;
			agree = 0;
		}
	}

	private static bool Near(double a, double b)
	{
		return Math.Abs(a - b) / b < 0.04;
	}
}
