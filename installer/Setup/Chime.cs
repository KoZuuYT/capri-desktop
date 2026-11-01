#if !UNINSTALLER
using System;
using System.IO;
using System.Media;

namespace CapriSetup;

// the setup's little synth: every sound is built once into an in-memory wav, so the exe carries no audio files
internal static class Chime
{
	private const int Rate = 22050;

	internal static bool On = true;

	private static readonly Random Noise = new Random(7);

	internal static readonly SoundPlayer Tick = Make(0.03, t => Math.Sin(2 * Math.PI * 2600 * t) * Math.Exp(-t * 160) * 0.16);

	internal static readonly SoundPlayer Click = Make(0.07, t => Square(900 - t * 7000, t) * Math.Exp(-t * 60) * 0.18);

	internal static readonly SoundPlayer Key = Make(0.014, t => (Noise.NextDouble() * 2 - 1) * Math.Exp(-t * 400) * 0.10);

	internal static readonly SoundPlayer Up = Make(0.11, t => Math.Sin(2 * Math.PI * (t < 0.05 ? 760 : 1180) * t) * Fade(t, 0.11) * 0.2);

	internal static readonly SoundPlayer Down = Make(0.11, t => Math.Sin(2 * Math.PI * (t < 0.05 ? 1180 : 620) * t) * Fade(t, 0.11) * 0.2);

	internal static readonly SoundPlayer Step = Make(0.26, t => (Crush(Math.Sin(2 * Math.PI * (280 + 5200 * t * t) * t), 6) * 0.7 + (Noise.NextDouble() * 2 - 1) * 0.3) * Fade(t, 0.26) * 0.2);

	internal static readonly SoundPlayer Boot = Make(0.9, t => (Math.Sin(2 * Math.PI * (70 + 160 * t) * t) * 0.6 + Crush(Math.Sin(2 * Math.PI * 1800 * t), 3) * (Math.Sin(t * 40) > 0.6 ? 0.25 : 0)) * Fade(t, 0.9) * 0.22);

	internal static readonly SoundPlayer Blip = Make(0.045, t => Math.Sin(2 * Math.PI * 1650 * t) * Math.Exp(-t * 90) * 0.13);

	internal static readonly SoundPlayer Done = Make(1.2, Arpeggio);

	internal static readonly SoundPlayer Error = Make(0.4, t => Square(140, t) * (0.6 + 0.4 * Math.Sin(t * 120)) * Fade(t, 0.4) * 0.16);

	internal static void Play(SoundPlayer sound)
	{
		if (!On)
		{
			return;
		}
		try
		{
			sound.Play();
		}
		catch (Exception)
		{
		}
	}

	// four rising notes and a held chord with a glitchy shimmer on top
	private static double Arpeggio(double t)
	{
		double[] notes = { 523.25, 659.25, 783.99, 1046.5 };
		double sum = 0;
		for (int i = 0; i < notes.Length; i++)
		{
			double start = i * 0.09;
			if (t < start)
			{
				continue;
			}
			double local = t - start;
			sum += (Math.Sin(2 * Math.PI * notes[i] * local) * 0.7 + Crush(Math.Sin(2 * Math.PI * notes[i] * 2 * local), 4) * 0.15) * Math.Exp(-local * 2.4);
		}
		return sum * Fade(t, 1.2) * 0.12;
	}

	private static double Square(double frequency, double t)
	{
		return Math.Sin(2 * Math.PI * frequency * t) >= 0 ? 1 : -1;
	}

	private static double Crush(double value, int steps)
	{
		return Math.Round(value * steps) / steps;
	}

	// short attack and release so nothing clicks
	private static double Fade(double t, double length)
	{
		return Math.Min(1, Math.Min(t / 0.004, (length - t) / 0.03));
	}

	private static SoundPlayer Make(double seconds, Func<double, double> wave)
	{
		int count = (int)(seconds * Rate);
		MemoryStream stream = new MemoryStream();
		BinaryWriter writer = new BinaryWriter(stream);
		writer.Write(0x46464952); // RIFF
		writer.Write(36 + count * 2);
		writer.Write(0x45564157); // WAVE
		writer.Write(0x20746D66); // fmt
		writer.Write(16);
		writer.Write((short)1);
		writer.Write((short)1);
		writer.Write(Rate);
		writer.Write(Rate * 2);
		writer.Write((short)2);
		writer.Write((short)16);
		writer.Write(0x61746164); // data
		writer.Write(count * 2);
		for (int i = 0; i < count; i++)
		{
			double value = Math.Max(-1, Math.Min(1, wave((double)i / Rate)));
			writer.Write((short)(value * 32000));
		}
		writer.Flush();
		stream.Position = 0;
		SoundPlayer player = new SoundPlayer(stream);
		player.Load();
		return player;
	}
}
#endif
