using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;

namespace CapriDesktop;

internal sealed class LocalVoice : IDisposable
{
	internal struct Vits
	{
		internal IntPtr Model;

		internal IntPtr Lexicon;

		internal IntPtr Tokens;

		internal IntPtr Data;

		internal float Noise;

		internal float NoiseW;

		internal float Length;

		internal IntPtr Dict;
	}

	internal struct Matcha
	{
		internal IntPtr Acoustic;

		internal IntPtr Vocoder;

		internal IntPtr Lexicon;

		internal IntPtr Tokens;

		internal IntPtr Data;

		internal float Noise;

		internal float Length;

		internal IntPtr Dict;
	}

	internal struct Kokoro
	{
		internal IntPtr Model;

		internal IntPtr Voices;

		internal IntPtr Tokens;

		internal IntPtr Data;

		internal float Length;

		internal IntPtr Dict;

		internal IntPtr Lexicon;

		internal IntPtr Lang;
	}

	internal struct Kitten
	{
		internal IntPtr Model;

		internal IntPtr Voices;

		internal IntPtr Tokens;

		internal IntPtr Data;

		internal float Length;
	}

	internal struct Zipvoice
	{
		internal IntPtr Tokens;

		internal IntPtr Encoder;

		internal IntPtr Decoder;

		internal IntPtr Vocoder;

		internal IntPtr Data;

		internal IntPtr Lexicon;

		internal float Feat;

		internal float Shift;

		internal float Rms;

		internal float Guidance;
	}

	internal struct Pocket
	{
		internal IntPtr Flow;

		internal IntPtr Main;

		internal IntPtr Encoder;

		internal IntPtr Decoder;

		internal IntPtr Conditioner;

		internal IntPtr Vocab;

		internal IntPtr Scores;

		internal int Cache;
	}

	internal struct Supertonic
	{
		internal IntPtr Duration;

		internal IntPtr Encoder;

		internal IntPtr Estimator;

		internal IntPtr Vocoder;

		internal IntPtr Json;

		internal IntPtr Indexer;

		internal IntPtr Style;
	}

	internal struct ModelConfig
	{
		internal Vits Vits;

		internal int Threads;

		internal int Debug;

		internal IntPtr Provider;

		internal Matcha Matcha;

		internal Kokoro Kokoro;

		internal Kitten Kitten;

		internal Zipvoice Zipvoice;

		internal Pocket Pocket;

		internal Supertonic Supertonic;
	}

	internal struct Config
	{
		internal ModelConfig Model;

		internal IntPtr Rules;

		internal int MaxSentences;

		internal IntPtr Fars;

		internal float Silence;
	}

	internal struct Generated
	{
		internal IntPtr Samples;

		internal int Count;

		internal int Rate;
	}

	internal struct Asking
	{
		internal float Silence;

		internal float Speed;

		internal int Speaker;

		internal IntPtr Clip;

		internal int ClipLength;

		internal int ClipRate;

		internal IntPtr ClipText;

		internal int Steps;

		internal IntPtr Extra;
	}

	internal const string Pack = "capri-voice-1";

	internal static string PackUrl = "https://github.com/lachydotmcg/capri-desktop/releases/download/voice-pack-1/capri-voice-1.zip";

	internal static string PackSha = "9fae085696d68a580d6b1a953d7b253129bfe506a2c016421b306b7237d3214c";

	internal static long PackSize = 316041808L;

	internal static readonly FreeStyle[] Styles = new FreeStyle[4]
	{
		new FreeStyle("Her own", 53, 1.1f, british: false),
		new FreeStyle("Bright", 2, 1.1f, british: false),
		new FreeStyle("Sly", 6, 1.08f, british: false),
		new FreeStyle("Sharp", 21, 1.1f, british: true)
	};

	internal static readonly string[] Needed = VoicePack.Needed;

	private static readonly object loading = new object();

	private static bool loaded;

	private readonly string folder;

	private readonly object gate = new object();

	private IntPtr tts;

	private bool british;

	internal volatile FreeStyle Style = Styles[0];

	internal volatile bool Cancel;

	internal bool Installed => Complete(folder);

	internal LocalVoice(string data)
	{
		folder = Folder(data);
	}

	internal static string Folder(string data)
	{
		return VoicePack.Folder(data);
	}

	internal static FreeStyle Find(string name)
	{
		FreeStyle[] styles = Styles;
		foreach (FreeStyle freeStyle in styles)
		{
			if (freeStyle.Name == name)
			{
				return freeStyle;
			}
		}
		return Styles[0];
	}

	private static bool Complete(string where)
	{
		return VoicePack.Complete(where);
	}

	internal void Install(Action<string, long, long> progress)
	{
		if (IntPtr.Size != 8)
		{
			throw new IOException("Her free voice needs 64-bit Windows.");
		}
		string directoryName = Path.GetDirectoryName(folder);
		Directory.CreateDirectory(directoryName);
		long num = PackSize * 3 + 67108864;
		string pathRoot = Path.GetPathRoot(directoryName);
		if (new DriveInfo(pathRoot).AvailableFreeSpace < num)
		{
			throw new IOException("Her free voice needs " + Engine.Amount(num) + " free on " + pathRoot.TrimEnd('\\') + ".");
		}
		string text = folder + ".zip";
		Engine.Fetch(PackUrl, text, PackSize, PackSha, "her free voice", progress, () => Cancel);
		progress("Unpacking her free voice", PackSize, PackSize);
		string text2 = folder + ".new";
		if (Directory.Exists(text2))
		{
			Directory.Delete(text2, recursive: true);
		}
		ZipFile.ExtractToDirectory(text, text2);
		if (!Complete(text2))
		{
			throw new IOException("The voice download is missing files.");
		}
		lock (gate)
		{
			Close();
			if (Directory.Exists(folder))
			{
				Directory.Delete(folder, recursive: true);
			}
			Directory.Move(text2, folder);
		}
		File.Delete(text);
	}

	internal void Warm()
	{
		FreeStyle style = Style;
		lock (gate)
		{
			Open(style.British);
		}
	}

	internal byte[] Speak(string text)
	{
		FreeStyle style = Style;
		int pitch = VoiceFx.Pitch;
		int speed = VoiceFx.Speed;
		int robot = VoiceFx.Robot;
		int glitch = VoiceFx.Glitch;
		VoiceFx.Layering layers = VoiceFx.Layers;
		short[] array2;
		int rate;
		lock (gate)
		{
			Open(style.British);
			IntPtr intPtr = Utf8(text);
			IntPtr intPtr2 = IntPtr.Zero;
			try
			{
				intPtr2 = SherpaOnnxOfflineTtsGenerate(tts, intPtr, style.Speaker, VoiceFx.Tempo(style.Speed, pitch, speed));
				if (intPtr2 == IntPtr.Zero)
				{
					throw new VoiceException("Her free voice glitched.");
				}
				Generated generated = (Generated)Marshal.PtrToStructure(intPtr2, typeof(Generated));
				float[] array = new float[Math.Max(0, generated.Count)];
				if (array.Length > 0)
				{
					Marshal.Copy(generated.Samples, array, 0, array.Length);
				}
				array2 = new short[array.Length];
				for (int i = 0; i < array.Length; i++)
				{
					array2[i] = (short)Math.Max(-32767.0, Math.Min(32767.0, Math.Round(array[i] * 32767f)));
				}
				rate = generated.Rate;
			}
			finally
			{
				if (intPtr2 != IntPtr.Zero)
				{
					SherpaOnnxDestroyOfflineTtsGeneratedAudio(intPtr2);
				}
				Marshal.FreeHGlobal(intPtr);
			}
		}
		return Voice.Wave(VoiceFx.Kokoro(array2, rate, pitch, robot, glitch, VoiceFx.Seed(text), layers), rate);
	}

	private void Open(bool gb)
	{
		if (tts != IntPtr.Zero && british == gb)
		{
			return;
		}
		if (IntPtr.Size != 8)
		{
			throw new VoiceException("Her free voice needs 64-bit Windows.");
		}
		if (!Installed)
		{
			throw new VoiceException("Set up her free voice in Capri Mainframe.");
		}
		Load(folder);
		Close();
		List<IntPtr> held = new List<IntPtr>();
		Func<string, IntPtr> func = (string value) =>
		{
			IntPtr intPtr = Utf8(value);
			held.Add(intPtr);
			return intPtr;
		};
		try
		{
			Config config = new Config
			{
				Model = 
				{
					Threads = Math.Max(1, Math.Min(6, Environment.ProcessorCount / 2)),
					Provider = func("cpu"),
					Kokoro = 
					{
						Model = func(Path.Combine(folder, "model.onnx")),
						Voices = func(Path.Combine(folder, "voices.bin")),
						Tokens = func(Path.Combine(folder, "tokens.txt")),
						Data = func(Path.Combine(folder, "espeak-ng-data")),
						Lexicon = func(Path.Combine(folder, gb ? "lexicon-gb-en.txt" : "lexicon-us-en.txt")),
						Lang = func(gb ? "en" : "en-us")
					}
				},
				MaxSentences = 1
			};
			tts = SherpaOnnxCreateOfflineTts(ref config);
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
			throw new VoiceException("Her free voice wouldn't start.");
		}
		british = gb;
	}

	internal static void Load(string where)
	{
		lock (loading)
		{
			if (loaded)
			{
				return;
			}
			string[] array = new string[2] { "onnxruntime.dll", "sherpa-onnx-c-api.dll" };
			foreach (string path in array)
			{
				if (LoadLibraryEx(Path.Combine(where, path), IntPtr.Zero, 8) == IntPtr.Zero)
				{
					throw new VoiceException("Her free voice wouldn't load (error " + Marshal.GetLastWin32Error() + ").");
				}
			}
			loaded = true;
		}
	}

	private void Close()
	{
		if (!(tts == IntPtr.Zero))
		{
			SherpaOnnxDestroyOfflineTts(tts);
			tts = IntPtr.Zero;
		}
	}

	public void Dispose()
	{
		lock (gate)
		{
			Close();
		}
	}

	internal static IntPtr Utf8(string value)
	{
		byte[] bytes = Encoding.UTF8.GetBytes(value ?? "");
		IntPtr intPtr = Marshal.AllocHGlobal(bytes.Length + 1);
		Marshal.Copy(bytes, 0, intPtr, bytes.Length);
		Marshal.WriteByte(intPtr, bytes.Length, 0);
		return intPtr;
	}

	[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	private static extern IntPtr LoadLibraryEx(string path, IntPtr file, int flags);

	[DllImport("sherpa-onnx-c-api.dll", CallingConvention = CallingConvention.Cdecl)]
	internal static extern IntPtr SherpaOnnxCreateOfflineTts(ref Config config);

	[DllImport("sherpa-onnx-c-api.dll", CallingConvention = CallingConvention.Cdecl)]
	internal static extern void SherpaOnnxDestroyOfflineTts(IntPtr tts);

	[DllImport("sherpa-onnx-c-api.dll", CallingConvention = CallingConvention.Cdecl)]
	internal static extern IntPtr SherpaOnnxOfflineTtsGenerate(IntPtr tts, IntPtr text, int speaker, float speed);

	[DllImport("sherpa-onnx-c-api.dll", CallingConvention = CallingConvention.Cdecl)]
	internal static extern void SherpaOnnxDestroyOfflineTtsGeneratedAudio(IntPtr audio);

	[DllImport("sherpa-onnx-c-api.dll", CallingConvention = CallingConvention.Cdecl)]
	internal static extern int SherpaOnnxOfflineTtsNumSpeakers(IntPtr tts);

	[DllImport("sherpa-onnx-c-api.dll", CallingConvention = CallingConvention.Cdecl)]
	internal static extern IntPtr SherpaOnnxOfflineTtsGenerateWithConfig(IntPtr tts, IntPtr text, ref Asking config, IntPtr callback, IntPtr arg);
}
