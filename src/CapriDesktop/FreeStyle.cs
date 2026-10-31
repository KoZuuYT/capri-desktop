namespace CapriDesktop;

internal sealed class FreeStyle
{
	internal readonly string Name;

	internal readonly int Speaker;

	internal readonly float Speed;

	internal readonly bool British;

	internal FreeStyle(string name, int speaker, float speed, bool british)
	{
		Name = name;
		Speaker = speaker;
		Speed = speed;
		British = british;
	}
}
