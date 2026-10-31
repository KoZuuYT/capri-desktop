namespace CapriDesktop;

internal struct WaveFormat
{
	internal short Tag;

	internal short Channels;

	internal int SampleRate;

	internal int BytesPerSecond;

	internal short Align;

	internal short Bits;

	internal short Extra;

	internal static WaveFormat Mono(int rate)
	{
		return new WaveFormat
		{
			Tag = 1,
			Channels = 1,
			SampleRate = rate,
			BytesPerSecond = rate * 2,
			Align = 2,
			Bits = 16
		};
	}
}
