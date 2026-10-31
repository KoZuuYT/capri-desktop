namespace CapriDesktop;

internal sealed class Pick
{
	internal readonly string Name;

	internal readonly string Ollama;

	internal readonly string File;

	internal readonly string Url;

	internal readonly string Sha256;

	internal readonly long Size;

	internal string EyesFile;

	internal string EyesUrl;

	internal string EyesSha256;

	internal long EyesSize;

	internal string Download => Engine.Amount(Size);

	internal Pick(string name, string ollama, string repository, string revision, string file, string sha256, long size)
	{
		Name = name;
		Ollama = ollama;
		File = file;
		Url = "https://huggingface.co/" + repository + "/resolve/" + revision + "/" + file;
		Sha256 = sha256;
		Size = size;
	}

	internal Pick Eyes(string repository, string revision, string file, string sha256, long size)
	{
		EyesFile = file;
		EyesUrl = "https://huggingface.co/" + repository + "/resolve/" + revision + "/" + file;
		EyesSha256 = sha256;
		EyesSize = size;
		return this;
	}
}
