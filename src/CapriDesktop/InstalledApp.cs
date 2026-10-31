namespace CapriDesktop;

internal sealed class InstalledApp
{
	internal readonly string Name;

	internal readonly string Id;

	internal InstalledApp(string name, string id)
	{
		Name = name;
		Id = id;
	}
}
