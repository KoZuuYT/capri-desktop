namespace CapriDesktop;

internal sealed class Moment
{
	internal readonly string On;

	internal readonly string Line;

	internal readonly string[] Words;

	internal Moment(string on, string line, string[] words)
	{
		On = on;
		Line = line;
		Words = words;
	}
}
