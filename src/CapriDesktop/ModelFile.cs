using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace CapriDesktop;

internal static class ModelFile
{
	internal const string Missing = "That's no voice model Capri can run. Pick a Piper, Kokoro, Kitten or Pocket TTS model for sherpa-onnx: its .tar.bz2, or the .onnx in its folder.";

	internal const string Unopened = "Capri can't open that clip. Press the arrow in the Speaker or clip box to pick it.";

	internal static string Engine;

	internal static string Shelf;

	private static readonly object gate = new object();

	private static IntPtr tts;

	private static string opened;

	private static string openKind;

	private static string clipFrom;

	private static float[] clipSound;

	private static int clipRate;

	internal static string Kind(string folder)
	{
		string text = Model(folder);
		if (text == null)
		{
			return null;
		}
		if (Part(folder, "lm_main") != null && Part(folder, "lm_flow") != null && File.Exists(Path.Combine(folder, "vocab.json")))
		{
			return "pocket";
		}
		if (!File.Exists(Path.Combine(folder, "tokens.txt")))
		{
			return null;
		}
		if (!File.Exists(Path.Combine(folder, "voices.bin")))
		{
			return "vits";
		}
		string text2 = Path.GetFileName(folder.TrimEnd('\\', '/')) + " " + Path.GetFileName(text);
		if (text2.IndexOf("kitten", StringComparison.OrdinalIgnoreCase) < 0)
		{
			return "kokoro";
		}
		return "kitten";
	}

	private static string Model(string folder)
	{
		string result = null;
		long num = -1L;
		try
		{
			if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
			{
				return null;
			}
			FileInfo[] files = new DirectoryInfo(folder).GetFiles();
			foreach (FileInfo fileInfo in files)
			{
				if (fileInfo.Extension.Equals(".onnx", StringComparison.OrdinalIgnoreCase) && fileInfo.Length > num)
				{
					result = fileInfo.FullName;
					num = fileInfo.Length;
				}
			}
			return result;
		}
		catch (IOException)
		{
			return null;
		}
		catch (UnauthorizedAccessException)
		{
			return null;
		}
	}

	private static string Part(string folder, string name)
	{
		string[] array = new string[2]
		{
			name + ".int8.onnx",
			name + ".onnx"
		};
		foreach (string path in array)
		{
			if (File.Exists(Path.Combine(folder, path)))
			{
				return Path.Combine(folder, path);
			}
		}
		return null;
	}

	internal static string Clean(string folder)
	{
		return (folder ?? "").Trim().Trim('"').Trim();
	}

	internal static string Name(string folder)
	{
		string input;
		try
		{
			input = Path.GetFileName(Clean(folder).TrimEnd('\\', '/'));
		}
		catch (ArgumentException)
		{
			input = "";
		}
		input = Regex.Replace(input, "^(vits-(piper-)?|sherpa-onnx-)", "", RegexOptions.IgnoreCase);
		if (input.Length != 0)
		{
			return input;
		}
		return "Voice model";
	}

	internal static string Unpack(string archive)
	{
		string text = Regex.Replace(Path.GetFileName(archive), "\\.(tar\\.bz2|tar\\.gz|tgz|tar|zip)$", "", RegexOptions.IgnoreCase).Trim().TrimEnd('.', ' ');
		if (text.Length == 0 || text.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
		{
			text = "voice-model";
		}
		string text2 = Path.Combine(Shelf, text);
		string text3 = text2 + ".new";
		if (Directory.Exists(text3))
		{
			Directory.Delete(text3, recursive: true);
		}
		Directory.CreateDirectory(text3);
		try
		{
			if (archive.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
			{
				ZipFile.ExtractToDirectory(archive, text3);
			}
			else
			{
				ProcessStartInfo processStartInfo = new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "tar.exe"), "-xf \"" + archive + "\" -C \"" + text3 + "\"");
				processStartInfo.UseShellExecute = false;
				processStartInfo.CreateNoWindow = true;
				using Process process = Process.Start(processStartInfo);
				if (!process.WaitForExit(600000))
				{
					try
					{
						process.Kill();
						process.WaitForExit(5000);
					}
					catch (Exception)
					{
					}
					throw new VoiceException("Windows took too long to unpack " + Path.GetFileName(archive) + ".");
				}
				if (process.ExitCode != 0)
				{
					throw new VoiceException("Windows couldn't unpack " + Path.GetFileName(archive) + ".");
				}
			}
		}
		catch (Exception)
		{
			try
			{
				Directory.Delete(text3, recursive: true);
			}
			catch (Exception)
			{
			}
			throw;
		}
		string text4 = text3;
		string[] directories = Directory.GetDirectories(text3);
		if (Kind(text3) == null && directories.Length == 1 && Directory.GetFiles(text3).Length == 0)
		{
			text4 = directories[0];
		}
		if (Kind(text4) == null)
		{
			Directory.Delete(text3, recursive: true);
			throw new VoiceException("That's no voice model Capri can run. Pick a Piper, Kokoro, Kitten or Pocket TTS model for sherpa-onnx: its .tar.bz2, or the .onnx in its folder.");
		}
		lock (gate)
		{
			if (opened != null && opened.StartsWith(text2 + "|", StringComparison.OrdinalIgnoreCase))
			{
				Close();
			}
			if (Directory.Exists(text2))
			{
				Directory.Delete(text2, recursive: true);
			}
			Directory.Move(text4, text2);
		}
		if (text4 != text3)
		{
			Directory.Delete(text3, recursive: true);
		}
		return text2;
	}

	internal static short[] Speak(string folder, string lang, string voice, string text, out int rate)
	{
		lock (gate)
		{
			string text2 = Open(folder, lang);
			IntPtr intPtr = LocalVoice.Utf8(text);
			IntPtr intPtr2 = IntPtr.Zero;
			try
			{
				try
				{
					if (text2 == "pocket")
					{
						intPtr2 = Copy(intPtr, voice);
					}
					else
					{
						if (!int.TryParse((voice ?? "").Trim(), out var result) || result < 0 || result >= LocalVoice.SherpaOnnxOfflineTtsNumSpeakers(tts))
						{
							result = 0;
						}
						intPtr2 = LocalVoice.SherpaOnnxOfflineTtsGenerate(tts, intPtr, result, 1f);
					}
				}
				catch (SEHException)
				{
					throw new VoiceException("Your voice model glitched.");
				}
				if (intPtr2 == IntPtr.Zero)
				{
					throw new VoiceException("Your voice model glitched.");
				}
				LocalVoice.Generated generated = (LocalVoice.Generated)Marshal.PtrToStructure(intPtr2, typeof(LocalVoice.Generated));
				float[] array = new float[Math.Max(0, generated.Count)];
				if (array.Length > 0)
				{
					Marshal.Copy(generated.Samples, array, 0, array.Length);
				}
				short[] array2 = new short[array.Length];
				for (int i = 0; i < array.Length; i++)
				{
					array2[i] = (short)Math.Max(-32767.0, Math.Min(32767.0, Math.Round(array[i] * 32767f)));
				}
				rate = generated.Rate;
				return array2;
			}
			finally
			{
				if (intPtr2 != IntPtr.Zero)
				{
					LocalVoice.SherpaOnnxDestroyOfflineTtsGeneratedAudio(intPtr2);
				}
				Marshal.FreeHGlobal(intPtr);
			}
		}
	}

	private static IntPtr Copy(IntPtr words, string voice)
	{
		string text = (voice ?? "").Trim().Trim('"');
		if (int.TryParse(text, out var _))
		{
			text = "";
		}
		string text2 = "";
		if (text.Length > 0)
		{
			try
			{
				text2 = text + "|" + File.GetLastWriteTimeUtc(text).Ticks;
			}
			catch (ArgumentException)
			{
				throw new VoiceException("Capri can't open that clip. Press the arrow in the Speaker or clip box to pick it.");
			}
			catch (NotSupportedException)
			{
				throw new VoiceException("Capri can't open that clip. Press the arrow in the Speaker or clip box to pick it.");
			}
			catch (PathTooLongException)
			{
				throw new VoiceException("Capri can't open that clip. Press the arrow in the Speaker or clip box to pick it.");
			}
		}
		if (clipSound == null || clipFrom != text2)
		{
			if (text.Length == 0)
			{
				throw new VoiceException("Pocket TTS copies a voice clip. Capri ships none, so pick a clip of a voice you have the rights to use.");
			}
			short[] array = YourVoice.Read(text);
			clipSound = new float[array.Length];
			for (int i = 0; i < array.Length; i++)
			{
				clipSound[i] = (float)array[i] / 32768f;
			}
			clipRate = 24000;
			clipFrom = text2;
		}
		GCHandle gCHandle = GCHandle.Alloc(clipSound, GCHandleType.Pinned);
		IntPtr intPtr = LocalVoice.Utf8("{\"max_reference_audio_len\": 10.0, \"seed\": 42}");
		try
		{
			LocalVoice.Asking config = new LocalVoice.Asking
			{
				Silence = 0.2f,
				Speed = 1f,
				Clip = gCHandle.AddrOfPinnedObject(),
				ClipLength = clipSound.Length,
				ClipRate = clipRate,
				Extra = intPtr
			};
			return LocalVoice.SherpaOnnxOfflineTtsGenerateWithConfig(tts, words, ref config, IntPtr.Zero, IntPtr.Zero);
		}
		finally
		{
			gCHandle.Free();
			Marshal.FreeHGlobal(intPtr);
		}
	}

	private static string Speech(string folder)
	{
		string text = Path.Combine(folder, "espeak-ng-data");
		string text2 = Path.Combine(Engine, "espeak-ng-data");
		if (!Directory.Exists(text))
		{
			return "";
		}
		if (!Directory.Exists(text2))
		{
			return text;
		}
		try
		{
			string[] files = Directory.GetFiles(text, "*", SearchOption.AllDirectories);
			foreach (string text3 in files)
			{
				string text4 = text2 + text3.Substring(text.Length);
				if (!File.Exists(text4))
				{
					Directory.CreateDirectory(Path.GetDirectoryName(text4));
					File.Copy(text3, text4);
				}
			}
		}
		catch (IOException)
		{
		}
		catch (UnauthorizedAccessException)
		{
		}
		return text2;
	}

	private static string Open(string folder, string lang)
	{
		lang = (string.IsNullOrEmpty(lang) ? "en-us" : lang.Trim().ToLowerInvariant());
		string text = folder + "|" + lang;
		if (tts != IntPtr.Zero && opened == text)
		{
			return openKind;
		}
		if (IntPtr.Size != 8)
		{
			throw new VoiceException("Voice model files need 64-bit Windows.");
		}
		if (Engine == null || !File.Exists(Path.Combine(Engine, "sherpa-onnx-c-api.dll")))
		{
			throw new VoiceException("Voice model files run on her free voice. Get free voice under VOICE first.");
		}
		string text2 = Kind(folder);
		if (text2 == null)
		{
			throw new VoiceException("That's no voice model Capri can run. Pick a Piper, Kokoro, Kitten or Pocket TTS model for sherpa-onnx: its .tar.bz2, or the .onnx in its folder.");
		}
		LocalVoice.Load(Engine);
		Close();
		List<IntPtr> held = new List<IntPtr>();
		Func<string, IntPtr> func = (string value) =>
		{
			IntPtr intPtr = LocalVoice.Utf8(value);
			held.Add(intPtr);
			return intPtr;
		};
		Func<string, string> func2 = (string name) =>
		{
			string text5 = Path.Combine(folder, name);
			return (!File.Exists(text5) && !Directory.Exists(text5)) ? "" : text5;
		};
		try
		{
			LocalVoice.Config config = new LocalVoice.Config
			{
				Model = 
				{
					Threads = Math.Max(1, Math.Min(6, Environment.ProcessorCount / 2)),
					Provider = func("cpu")
				}
			};
			switch (text2)
			{
			case "vits":
				config.Model.Vits.Model = func(Model(folder));
				config.Model.Vits.Tokens = func(func2("tokens.txt"));
				config.Model.Vits.Data = func(Speech(folder));
				config.Model.Vits.Lexicon = func(func2("lexicon.txt"));
				config.Model.Vits.Dict = func(func2("dict"));
				break;
			case "kitten":
				config.Model.Kitten.Model = func(Model(folder));
				config.Model.Kitten.Voices = func(func2("voices.bin"));
				config.Model.Kitten.Tokens = func(func2("tokens.txt"));
				config.Model.Kitten.Data = func(Speech(folder));
				break;
			case "pocket":
				config.Model.Pocket.Flow = func(Part(folder, "lm_flow"));
				config.Model.Pocket.Main = func(Part(folder, "lm_main"));
				config.Model.Pocket.Encoder = func(Part(folder, "encoder"));
				config.Model.Pocket.Decoder = func(Part(folder, "decoder"));
				config.Model.Pocket.Conditioner = func(Part(folder, "text_conditioner"));
				config.Model.Pocket.Vocab = func(func2("vocab.json"));
				config.Model.Pocket.Scores = func(func2("token_scores.json"));
				config.Model.Pocket.Cache = 50;
				break;
			default:
			{
				List<string> list = new List<string>();
				string[] files = Directory.GetFiles(folder, "lexicon*.txt");
				foreach (string text3 in files)
				{
					string text4 = Path.GetFileName(text3).ToLowerInvariant();
					if (!(text4 == ((lang == "en-gb" || lang == "en") ? "lexicon-us-en.txt" : "lexicon-gb-en.txt")))
					{
						list.Add(text3);
					}
				}
				config.Model.Kokoro.Model = func(Model(folder));
				config.Model.Kokoro.Voices = func(func2("voices.bin"));
				config.Model.Kokoro.Tokens = func(func2("tokens.txt"));
				config.Model.Kokoro.Data = func(Speech(folder));
				config.Model.Kokoro.Dict = func(func2("dict"));
				config.Model.Kokoro.Lexicon = func(string.Join(",", list.ToArray()));
				config.Model.Kokoro.Lang = func(lang);
				break;
			}
			}
			config.MaxSentences = 1;
			try
			{
				tts = LocalVoice.SherpaOnnxCreateOfflineTts(ref config);
			}
			catch (SEHException)
			{
				tts = IntPtr.Zero;
			}
		}
		finally
		{
			foreach (IntPtr item in held)
			{
				Marshal.FreeHGlobal(item);
			}
		}
		if (tts == IntPtr.Zero)
		{
			throw new VoiceException("That voice model wouldn't start. Check it's a Piper, Kokoro, Kitten or Pocket TTS model for sherpa-onnx.");
		}
		opened = text;
		openKind = text2;
		return text2;
	}

	private static void Close()
	{
		if (!(tts == IntPtr.Zero))
		{
			LocalVoice.SherpaOnnxDestroyOfflineTts(tts);
			tts = IntPtr.Zero;
			opened = null;
			openKind = null;
		}
	}
}
