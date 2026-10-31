using System.IO;

namespace CapriDesktop;

internal static class VoicePack
{
	internal const string Name = "capri-voice-1";

	internal const string Url = "https://github.com/lachydotmcg/capri-desktop/releases/download/voice-pack-1/capri-voice-1.zip";

	internal const string Sha = "9fae085696d68a580d6b1a953d7b253129bfe506a2c016421b306b7237d3214c";

	internal const long Size = 316041808L;

	internal static readonly string[] Needed = new string[8]
	{
		"sherpa-onnx-c-api.dll",
		"onnxruntime.dll",
		"model.onnx",
		"voices.bin",
		"tokens.txt",
		"lexicon-us-en.txt",
		"lexicon-gb-en.txt",
		Path.Combine("espeak-ng-data", "phontab")
	};

	internal static string Folder(string data)
	{
		return Path.Combine(Path.Combine(data, "voice"), "capri-voice-1");
	}

	internal static bool Complete(string where)
	{
		string[] needed = Needed;
		foreach (string path in needed)
		{
			if (!File.Exists(Path.Combine(where, path)))
			{
				return false;
			}
		}
		return true;
	}
}
