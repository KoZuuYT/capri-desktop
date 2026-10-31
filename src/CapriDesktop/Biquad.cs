using System;

namespace CapriDesktop;

internal sealed class Biquad
{
	private readonly double b0;

	private readonly double b1;

	private readonly double b2;

	private readonly double a1;

	private readonly double a2;

	private double x1;

	private double x2;

	private double y1;

	private double y2;

	private Biquad(double b0, double b1, double b2, double a0, double a1, double a2)
	{
		this.b0 = b0 / a0;
		this.b1 = b1 / a0;
		this.b2 = b2 / a0;
		this.a1 = a1 / a0;
		this.a2 = a2 / a0;
	}

	internal static Biquad Low(double rate, double cut)
	{
		double num = Math.PI * 2.0 * cut / rate;
		double num2 = Math.Cos(num);
		double num3 = Math.Sin(num) / 1.4142;
		return new Biquad((1.0 - num2) / 2.0, 1.0 - num2, (1.0 - num2) / 2.0, 1.0 + num3, -2.0 * num2, 1.0 - num3);
	}

	internal static Biquad High(double rate, double cut)
	{
		double num = Math.PI * 2.0 * cut / rate;
		double num2 = Math.Cos(num);
		double num3 = Math.Sin(num) / 1.4142;
		return new Biquad((1.0 + num2) / 2.0, 0.0 - (1.0 + num2), (1.0 + num2) / 2.0, 1.0 + num3, -2.0 * num2, 1.0 - num3);
	}

	internal double Run(double x)
	{
		double result = b0 * x + b1 * x1 + b2 * x2 - a1 * y1 - a2 * y2;
		x2 = x1;
		x1 = x;
		y2 = y1;
		y1 = result;
		return result;
	}
}
