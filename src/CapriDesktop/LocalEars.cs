using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace CapriDesktop;

internal sealed class LocalEars : IDisposable
{
	internal const string Pack = "parakeet-tdt-0.6b-v2-int8";

	private const string Inside = "sherpa-onnx-nemo-parakeet-tdt-0.6b-v2-int8";

	internal const string EuropeanPack = "parakeet-tdt-0.6b-v3-int8";

	private const string EuropeanInside = "sherpa-onnx-nemo-parakeet-tdt-0.6b-v3-int8";

	private const int ConfigBytes = 4096;

	internal static string PackUrl = "https://github.com/k2-fsa/sherpa-onnx/releases/download/asr-models/sherpa-onnx-nemo-parakeet-tdt-0.6b-v2-int8.tar.bz2";

	internal static string PackSha = "157c157bc51155e03e37d2466522a3a737dd9c72bb25f36eb18912964161e1ad";

	internal static long PackSize = 482468385L;

	internal static string EuropeanUrl = "https://github.com/k2-fsa/sherpa-onnx/releases/download/asr-models/sherpa-onnx-nemo-parakeet-tdt-0.6b-v3-int8.tar.bz2";

	internal static string EuropeanSha = "5793d0fd397c5778d2cf2126994d58e9d56b1be7c04d13c7a15bb1b4eafb16bf";

	internal static long EuropeanSize = 487170055L;

	internal static readonly string[] European = new string[25]
	{
		"bg", "hr", "cs", "da", "nl", "en", "et", "fi", "fr", "de",
		"el", "hu", "it", "lv", "lt", "mt", "pl", "pt", "ro", "sk",
		"sl", "es", "sv", "ru", "uk"
	};

	internal static readonly string[] Needed = new string[4] { "encoder.int8.onnx", "decoder.int8.onnx", "joiner.int8.onnx", "tokens.txt" };

	private readonly string root;

	private readonly string engine;

	private readonly object gate = new object();

	private IntPtr recognizer;

	private string opened;

	private volatile string language = "en";

	internal volatile bool Cancel;

	internal static bool SystemBzip = true;

	internal string Language
	{
		get
		{
			return language;
		}
		set
		{
			language = value ?? "en";
		}
	}

	internal bool Fits
	{
		get
		{
			if (IntPtr.Size == 8)
			{
				return Array.IndexOf(European, language) >= 0;
			}
			return false;
		}
	}

	internal string Model
	{
		get
		{
			if (Fits)
			{
				if (!(language == "en"))
				{
					return "parakeet-tdt-0.6b-v3-int8";
				}
				return "parakeet-tdt-0.6b-v2-int8";
			}
			return null;
		}
	}

	internal long Size
	{
		get
		{
			if (!(Model == "parakeet-tdt-0.6b-v3-int8"))
			{
				return PackSize;
			}
			return EuropeanSize;
		}
	}

	private string Folder => Path.Combine(root, Model ?? "parakeet-tdt-0.6b-v2-int8");

	internal bool Installed
	{
		get
		{
			if (Fits)
			{
				return Complete(Folder);
			}
			return false;
		}
	}

	internal bool Ready
	{
		get
		{
			if (IntPtr.Size == 8 && Installed && File.Exists(Path.Combine(engine, "sherpa-onnx-c-api.dll")))
			{
				return File.Exists(Path.Combine(engine, "onnxruntime.dll"));
			}
			return false;
		}
	}

	internal LocalEars(string data)
	{
		root = Path.Combine(data, "ears");
		engine = LocalVoice.Folder(data);
	}

	internal static string Spoken(string capri, string windows)
	{
		if (!(capri != "en") && !string.IsNullOrEmpty(windows))
		{
			return windows.ToLowerInvariant();
		}
		return capri;
	}

	private static bool Complete(string where)
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

	internal void Install(Action<string, long, long> progress)
	{
		if (IntPtr.Size != 8)
		{
			throw new IOException("Her ears need 64-bit Windows.");
		}
		if (!Fits)
		{
			throw new IOException("Her own ears don't know your language yet.");
		}
		bool flag = Model == "parakeet-tdt-0.6b-v2-int8";
		string folder = Folder;
		long num = (flag ? PackSize : EuropeanSize);
		Directory.CreateDirectory(root);
		long num2 = num * 3 + 67108864;
		string pathRoot = Path.GetPathRoot(root);
		if (new DriveInfo(pathRoot).AvailableFreeSpace < num2)
		{
			throw new IOException("Her ears need " + Engine.Amount(num2) + " free on " + pathRoot.TrimEnd('\\') + ".");
		}
		string text = folder + ".tar.bz2";
		Engine.Fetch(flag ? PackUrl : EuropeanUrl, text, num, flag ? PackSha : EuropeanSha, "her ears", progress, () => Cancel);
		progress("Unpacking her ears", num, num);
		string text2 = folder + ".new";
		if (Directory.Exists(text2))
		{
			Directory.Delete(text2, recursive: true);
		}
		Directory.CreateDirectory(text2);
		Untar(text, text2);
		string text3 = Path.Combine(text2, flag ? "sherpa-onnx-nemo-parakeet-tdt-0.6b-v2-int8" : "sherpa-onnx-nemo-parakeet-tdt-0.6b-v3-int8");
		if (!Complete(text3))
		{
			throw new IOException("The ears download is missing files.");
		}
		lock (gate)
		{
			Close();
			if (Directory.Exists(folder))
			{
				Directory.Delete(folder, recursive: true);
			}
			Directory.Move(text3, folder);
		}
		Directory.Delete(text2, recursive: true);
		File.Delete(text);
	}

	internal static void Untar(string tarball, string into)
	{
		string text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "tar.exe");
		if (!File.Exists(text))
		{
			throw new IOException("Windows is missing tar.exe, which unpacks her ears.");
		}
		if (SystemBzip && Tar(text, "-xjf \"" + tarball + "\" -C \"" + into + "\"") == null)
		{
			return;
		}
		string text2 = Path.ChangeExtension(tarball, null);
		try
		{
			using (FileStream fileStream = new FileStream(tarball, FileMode.Open, FileAccess.Read, FileShare.Read, 1048576))
			{
				using FileStream to = new FileStream(text2, FileMode.Create, FileAccess.Write, FileShare.None, 1048576);
				try
				{
					Bunzip.Copy(fileStream, to);
				}
				catch (IOException)
				{
					throw;
				}
				catch (Exception)
				{
					throw new IOException("The ears download is damaged.");
				}
			}
			File.Delete(tarball);
			Directory.Delete(into, recursive: true);
			Directory.CreateDirectory(into);
			string text3 = Tar(text, "-xf \"" + text2 + "\" -C \"" + into + "\"");
			if (text3 != null)
			{
				throw new IOException("Couldn't unpack her ears: " + Voice.Short(text3));
			}
		}
		finally
		{
			if (File.Exists(text2))
			{
				File.Delete(text2);
			}
		}
	}

	private static string Tar(string tar, string arguments)
	{
		ProcessStartInfo processStartInfo = new ProcessStartInfo(tar, arguments);
		processStartInfo.UseShellExecute = false;
		processStartInfo.CreateNoWindow = true;
		processStartInfo.RedirectStandardError = true;
		ProcessStartInfo startInfo = processStartInfo;
		using Process process = Process.Start(startInfo);
		string text = process.StandardError.ReadToEnd();
		process.WaitForExit();
		return (process.ExitCode == 0) ? null : text.Trim();
	}

	internal void Warm()
	{
		lock (gate)
		{
			Open();
		}
	}

	internal string Hear(short[] samples)
	{
		lock (gate)
		{
			Open();
			IntPtr intPtr = SherpaOnnxCreateOfflineStream(recognizer);
			if (intPtr == IntPtr.Zero)
			{
				throw new IOException("Her ears glitched.");
			}
			IntPtr intPtr2 = IntPtr.Zero;
			try
			{
				float[] array = new float[samples.Length];
				for (int i = 0; i < samples.Length; i++)
				{
					array[i] = (float)samples[i] / 32768f;
				}
				SherpaOnnxAcceptWaveformOffline(intPtr, 16000, array, array.Length);
				SherpaOnnxDecodeOfflineStream(recognizer, intPtr);
				intPtr2 = SherpaOnnxGetOfflineStreamResult(intPtr);
				return (intPtr2 == IntPtr.Zero) ? "" : Text(Marshal.ReadIntPtr(intPtr2));
			}
			finally
			{
				if (intPtr2 != IntPtr.Zero)
				{
					SherpaOnnxDestroyOfflineRecognizerResult(intPtr2);
				}
				SherpaOnnxDestroyOfflineStream(intPtr);
			}
		}
	}

	private void Open()
	{
		string folder = Folder;
		if (recognizer != IntPtr.Zero && opened == folder)
		{
			return;
		}
		Close();
		if (!Ready)
		{
			throw new IOException("Her ears aren't in yet.");
		}
		LocalVoice.Load(engine);
		List<IntPtr> held = new List<IntPtr>();
		Func<string, IntPtr> func = (string value) =>
		{
			IntPtr intPtr2 = Utf8(value);
			held.Add(intPtr2);
			return intPtr2;
		};
		IntPtr intPtr = Marshal.AllocHGlobal(4096);
		try
		{
			for (int num = 0; num < 4096; num += 8)
			{
				Marshal.WriteInt64(intPtr, num, 0L);
			}
			Marshal.WriteIntPtr(intPtr, 8, func(Path.Combine(folder, "encoder.int8.onnx")));
			Marshal.WriteIntPtr(intPtr, 16, func(Path.Combine(folder, "decoder.int8.onnx")));
			Marshal.WriteIntPtr(intPtr, 24, func(Path.Combine(folder, "joiner.int8.onnx")));
			Marshal.WriteIntPtr(intPtr, 104, func(Path.Combine(folder, "tokens.txt")));
			Marshal.WriteInt32(intPtr, 112, Math.Max(1, Math.Min(4, Environment.ProcessorCount / 2)));
			Marshal.WriteIntPtr(intPtr, 120, func("cpu"));
			Marshal.WriteIntPtr(intPtr, 128, func("nemo_transducer"));
			recognizer = SherpaOnnxCreateOfflineRecognizer(intPtr);
		}
		finally
		{
			Marshal.FreeHGlobal(intPtr);
			foreach (IntPtr item in held)
			{
				Marshal.FreeHGlobal(item);
			}
		}
		if (recognizer == IntPtr.Zero)
		{
			throw new IOException("Her ears wouldn't start.");
		}
		opened = folder;
	}

	private void Close()
	{
		if (!(recognizer == IntPtr.Zero))
		{
			SherpaOnnxDestroyOfflineRecognizer(recognizer);
			recognizer = IntPtr.Zero;
		}
	}

	public void Dispose()
	{
		lock (gate)
		{
			Close();
		}
	}

	private static IntPtr Utf8(string value)
	{
		byte[] bytes = Encoding.UTF8.GetBytes(value ?? "");
		IntPtr intPtr = Marshal.AllocHGlobal(bytes.Length + 1);
		Marshal.Copy(bytes, 0, intPtr, bytes.Length);
		Marshal.WriteByte(intPtr, bytes.Length, 0);
		return intPtr;
	}

	private static string Text(IntPtr pointer)
	{
		if (pointer == IntPtr.Zero)
		{
			return "";
		}
		int i;
		for (i = 0; Marshal.ReadByte(pointer, i) != 0; i++)
		{
		}
		byte[] array = new byte[i];
		Marshal.Copy(pointer, array, 0, i);
		return Encoding.UTF8.GetString(array);
	}

	[DllImport("sherpa-onnx-c-api.dll", CallingConvention = CallingConvention.Cdecl)]
	private static extern IntPtr SherpaOnnxCreateOfflineRecognizer(IntPtr config);

	[DllImport("sherpa-onnx-c-api.dll", CallingConvention = CallingConvention.Cdecl)]
	private static extern void SherpaOnnxDestroyOfflineRecognizer(IntPtr recognizer);

	[DllImport("sherpa-onnx-c-api.dll", CallingConvention = CallingConvention.Cdecl)]
	private static extern IntPtr SherpaOnnxCreateOfflineStream(IntPtr recognizer);

	[DllImport("sherpa-onnx-c-api.dll", CallingConvention = CallingConvention.Cdecl)]
	private static extern void SherpaOnnxDestroyOfflineStream(IntPtr stream);

	[DllImport("sherpa-onnx-c-api.dll", CallingConvention = CallingConvention.Cdecl)]
	private static extern void SherpaOnnxAcceptWaveformOffline(IntPtr stream, int sampleRate, float[] samples, int count);

	[DllImport("sherpa-onnx-c-api.dll", CallingConvention = CallingConvention.Cdecl)]
	private static extern void SherpaOnnxDecodeOfflineStream(IntPtr recognizer, IntPtr stream);

	[DllImport("sherpa-onnx-c-api.dll", CallingConvention = CallingConvention.Cdecl)]
	private static extern IntPtr SherpaOnnxGetOfflineStreamResult(IntPtr stream);

	[DllImport("sherpa-onnx-c-api.dll", CallingConvention = CallingConvention.Cdecl)]
	private static extern void SherpaOnnxDestroyOfflineRecognizerResult(IntPtr result);
}
