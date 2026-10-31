using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace CapriDesktop;

internal sealed class WakeWord : IDisposable
{
	internal const string Pack = "kws-zipformer-gigaspeech-3.3M-2024-01-01";

	private const string Inside = "sherpa-onnx-kws-zipformer-gigaspeech-3.3M-2024-01-01";

	private const string Encoder = "encoder-epoch-12-avg-2-chunk-16-left-64.int8.onnx";

	private const string Decoder = "decoder-epoch-12-avg-2-chunk-16-left-64.onnx";

	private const string Joiner = "joiner-epoch-12-avg-2-chunk-16-left-64.int8.onnx";

	private const int Before = 5;

	private const int After = 20;

	private const double Hiss = 0.0005;

	private const int ConfigBytes = 1024;

	internal static string PackUrl = "https://github.com/k2-fsa/sherpa-onnx/releases/download/kws-models/sherpa-onnx-kws-zipformer-gigaspeech-3.3M-2024-01-01.tar.bz2";

	internal static string PackSha = "f170013b4716e41b62b9bfd809687c207cef798ef9bc6534d524e17af9b6561a";

	internal static long PackSize = 17626723L;

	internal static readonly string[] Needed = new string[4] { "encoder-epoch-12-avg-2-chunk-16-left-64.int8.onnx", "decoder-epoch-12-avg-2-chunk-16-left-64.onnx", "joiner-epoch-12-avg-2-chunk-16-left-64.int8.onnx", "tokens.txt" };

	internal static string[] Phrases = new string[1] { "▁HE Y ▁CA P RI" };

	internal static float Boost = 2f;

	internal static float Threshold = 0.1f;

	private readonly string root;

	private readonly string engine;

	private readonly object gate = new object();

	private readonly Ears mic = new Ears();

	private IntPtr spotter;

	private IntPtr stream;

	private volatile bool held;

	private bool heldWas;

	internal volatile bool Cancel;

	private volatile int lastChunk;

	internal static double Louder = 3.0;

	internal int Chunks;

	internal int Decoded;

	private double room = -1.0;

	private int quietFor = 21;

	private readonly Queue<short[]> before = new Queue<short[]>();

	private string Folder => Path.Combine(root, "kws-zipformer-gigaspeech-3.3M-2024-01-01");

	internal bool Installed => Complete(Folder);

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

	internal bool VoiceIn
	{
		get
		{
			if (File.Exists(Path.Combine(engine, "sherpa-onnx-c-api.dll")))
			{
				return File.Exists(Path.Combine(engine, "onnxruntime.dll"));
			}
			return false;
		}
	}

	internal bool Listening => mic.Listening;

	internal bool Held
	{
		get
		{
			return held;
		}
		set
		{
			held = value;
		}
	}

	internal bool Warmed => spotter != IntPtr.Zero;

	internal bool Stalled
	{
		get
		{
			if (mic.Listening)
			{
				return Environment.TickCount - lastChunk > 5000;
			}
			return false;
		}
	}

	internal double Room => room;

	internal event Action Heard;

	internal WakeWord(string data)
		: this(Path.Combine(data, "wake"), LocalVoice.Folder(data))
	{
	}

	internal WakeWord(string models, string voice)
	{
		root = models;
		engine = voice;
		mic.Heard += Hear;
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
			throw new IOException("Listening for \"Hey Capri\" needs 64-bit Windows.");
		}
		string folder = Folder;
		Directory.CreateDirectory(root);
		long num = PackSize * 3 + 16777216;
		string pathRoot = Path.GetPathRoot(Path.GetFullPath(root));
		if (new DriveInfo(pathRoot).AvailableFreeSpace < num)
		{
			throw new IOException("Listening for \"Hey Capri\" needs " + Engine.Amount(num) + " free on " + pathRoot.TrimEnd('\\') + ".");
		}
		string text = folder + ".tar.bz2";
		Engine.Fetch(PackUrl, text, PackSize, PackSha, "her wake word", progress, () => Cancel);
		progress("Unpacking her wake word", PackSize, PackSize);
		string text2 = folder + ".new";
		if (Directory.Exists(text2))
		{
			Directory.Delete(text2, recursive: true);
		}
		Directory.CreateDirectory(text2);
		LocalEars.Untar(text, text2);
		string text3 = Path.Combine(text2, "sherpa-onnx-kws-zipformer-gigaspeech-3.3M-2024-01-01");
		if (!Complete(text3))
		{
			throw new IOException("The \"Hey Capri\" download is missing files.");
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

	internal void Listen(string chosen)
	{
		lock (gate)
		{
			Open();
			Fresh();
		}
		if (!mic.Listening)
		{
			mic.Chosen = chosen;
			lastChunk = Environment.TickCount;
			mic.Start();
		}
	}

	internal void Warm()
	{
		lock (gate)
		{
			Open();
			if (stream == IntPtr.Zero)
			{
				Fresh();
			}
		}
	}

	internal void Feed(short[] chunk)
	{
		Hear(chunk);
	}

	internal void Pause()
	{
		mic.Stop();
		lock (gate)
		{
			Fresh();
		}
	}

	public void Dispose()
	{
		mic.Stop();
		lock (gate)
		{
			Close();
		}
	}

	private void Hear(short[] chunk)
	{
		lastChunk = Environment.TickCount;
		bool flag = false;
		lock (gate)
		{
			if (spotter == IntPtr.Zero || stream == IntPtr.Zero)
			{
				return;
			}
			if (held)
			{
				heldWas = true;
				return;
			}
			if (heldWas)
			{
				heldWas = false;
				Fresh();
			}
			double num = Speaker.Loudness(chunk, 0, chunk.Length);
			if (!(num < 1E-05))
			{
				if (room < 0.0)
				{
					room = num;
				}
				else if (num < room)
				{
					room += (num - room) * 0.3;
				}
				else if (num < room * Louder)
				{
					room += (num - room) * 0.01;
				}
				else
				{
					room *= 1.002;
				}
			}
			Chunks++;
			if (num > Math.Max(room * Louder, 0.0005))
			{
				if (quietFor > 20)
				{
					foreach (short[] item in before)
					{
						flag |= Decode(stream, Wave(item));
					}
				}
				before.Clear();
				quietFor = 0;
			}
			else if (++quietFor > 20)
			{
				before.Enqueue(chunk);
				if (before.Count > 5)
				{
					before.Dequeue();
				}
				return;
			}
			Decoded++;
			flag |= Decode(stream, Wave(chunk));
		}
		Action heard = Heard;
		if (flag)
		{
			heard?.Invoke();
		}
	}

	internal int Spot(short[] clip)
	{
		lock (gate)
		{
			Open();
			IntPtr intPtr = SherpaOnnxCreateKeywordStream(spotter);
			if (intPtr == IntPtr.Zero)
			{
				throw new IOException("Her wake word glitched.");
			}
			try
			{
				int num = 0;
				for (int i = 0; i < clip.Length; i += 1600)
				{
					short[] array = new short[Math.Min(1600, clip.Length - i)];
					Array.Copy(clip, i, array, 0, array.Length);
					if (Decode(intPtr, Wave(array)))
					{
						num++;
					}
				}
				for (int j = 0; j < 10; j++)
				{
					if (Decode(intPtr, new float[1600]))
					{
						num++;
					}
				}
				return num;
			}
			finally
			{
				SherpaOnnxDestroyOnlineStream(intPtr);
			}
		}
	}

	private static float[] Wave(short[] chunk)
	{
		float[] array = new float[chunk.Length];
		for (int i = 0; i < chunk.Length; i++)
		{
			array[i] = (float)chunk[i] / 32768f;
		}
		return array;
	}

	private bool Decode(IntPtr on, float[] wave)
	{
		SherpaOnnxOnlineStreamAcceptWaveform(on, 16000, wave, wave.Length);
		bool result = false;
		while (SherpaOnnxIsKeywordStreamReady(spotter, on) == 1)
		{
			SherpaOnnxDecodeKeywordStream(spotter, on);
			IntPtr intPtr = SherpaOnnxGetKeywordResult(spotter, on);
			if (intPtr == IntPtr.Zero)
			{
				continue;
			}
			try
			{
				IntPtr intPtr2 = Marshal.ReadIntPtr(intPtr);
				if (!(intPtr2 == IntPtr.Zero) && Marshal.ReadByte(intPtr2) != 0)
				{
					result = true;
					SherpaOnnxResetKeywordStream(spotter, on);
				}
			}
			finally
			{
				SherpaOnnxDestroyKeywordResult(intPtr);
			}
		}
		return result;
	}

	private void Fresh()
	{
		heldWas = false;
		before.Clear();
		quietFor = 21;
		if (!(spotter == IntPtr.Zero))
		{
			if (stream != IntPtr.Zero)
			{
				SherpaOnnxDestroyOnlineStream(stream);
			}
			stream = SherpaOnnxCreateKeywordStream(spotter);
		}
	}

	private void Open()
	{
		if (spotter != IntPtr.Zero)
		{
			return;
		}
		if (!Ready)
		{
			throw new IOException("Her wake word isn't in yet.");
		}
		LocalVoice.Load(engine);
		List<IntPtr> kept = new List<IntPtr>();
		Func<string, IntPtr> func = (string value) =>
		{
			IntPtr intPtr3 = LocalVoice.Utf8(value);
			kept.Add(intPtr3);
			return intPtr3;
		};
		string tuning = " :" + Boost.ToString("0.###", CultureInfo.InvariantCulture) + " #" + Threshold.ToString("0.###", CultureInfo.InvariantCulture) + " @HEY_CAPRI";
		byte[] bytes = Encoding.UTF8.GetBytes(string.Join("\n", Array.ConvertAll(Phrases, (string phrase) => phrase + tuning)));
		string folder = Folder;
		IntPtr intPtr = Marshal.AllocHGlobal(1024);
		try
		{
			for (int num = 0; num < 1024; num += 8)
			{
				Marshal.WriteInt64(intPtr, num, 0L);
			}
			Marshal.WriteInt32(intPtr, 0, 16000);
			Marshal.WriteInt32(intPtr, 4, 80);
			Marshal.WriteIntPtr(intPtr, 8, func(Path.Combine(folder, "encoder-epoch-12-avg-2-chunk-16-left-64.int8.onnx")));
			Marshal.WriteIntPtr(intPtr, 16, func(Path.Combine(folder, "decoder-epoch-12-avg-2-chunk-16-left-64.onnx")));
			Marshal.WriteIntPtr(intPtr, 24, func(Path.Combine(folder, "joiner-epoch-12-avg-2-chunk-16-left-64.int8.onnx")));
			Marshal.WriteIntPtr(intPtr, 56, func(Path.Combine(folder, "tokens.txt")));
			Marshal.WriteInt32(intPtr, 64, 1);
			Marshal.WriteIntPtr(intPtr, 72, func("cpu"));
			Marshal.WriteInt32(intPtr, 144, 4);
			Marshal.WriteInt32(intPtr, 148, 1);
			Marshal.WriteInt32(intPtr, 152, BitConverter.ToInt32(BitConverter.GetBytes(Boost), 0));
			Marshal.WriteInt32(intPtr, 156, BitConverter.ToInt32(BitConverter.GetBytes(Threshold), 0));
			IntPtr intPtr2 = Marshal.AllocHGlobal(bytes.Length + 1);
			kept.Add(intPtr2);
			Marshal.Copy(bytes, 0, intPtr2, bytes.Length);
			Marshal.WriteByte(intPtr2, bytes.Length, 0);
			Marshal.WriteIntPtr(intPtr, 168, intPtr2);
			Marshal.WriteInt32(intPtr, 176, bytes.Length);
			spotter = SherpaOnnxCreateKeywordSpotter(intPtr);
		}
		finally
		{
			Marshal.FreeHGlobal(intPtr);
			foreach (IntPtr item in kept)
			{
				Marshal.FreeHGlobal(item);
			}
		}
		if (!(spotter == IntPtr.Zero))
		{
			return;
		}
		throw new IOException("Her wake word wouldn't start.");
	}

	private void Close()
	{
		if (stream != IntPtr.Zero)
		{
			SherpaOnnxDestroyOnlineStream(stream);
		}
		stream = IntPtr.Zero;
		if (spotter != IntPtr.Zero)
		{
			SherpaOnnxDestroyKeywordSpotter(spotter);
		}
		spotter = IntPtr.Zero;
	}

	[DllImport("sherpa-onnx-c-api.dll", CallingConvention = CallingConvention.Cdecl)]
	private static extern IntPtr SherpaOnnxCreateKeywordSpotter(IntPtr config);

	[DllImport("sherpa-onnx-c-api.dll", CallingConvention = CallingConvention.Cdecl)]
	private static extern void SherpaOnnxDestroyKeywordSpotter(IntPtr spotter);

	[DllImport("sherpa-onnx-c-api.dll", CallingConvention = CallingConvention.Cdecl)]
	private static extern IntPtr SherpaOnnxCreateKeywordStream(IntPtr spotter);

	[DllImport("sherpa-onnx-c-api.dll", CallingConvention = CallingConvention.Cdecl)]
	private static extern void SherpaOnnxDestroyOnlineStream(IntPtr stream);

	[DllImport("sherpa-onnx-c-api.dll", CallingConvention = CallingConvention.Cdecl)]
	private static extern void SherpaOnnxOnlineStreamAcceptWaveform(IntPtr stream, int sampleRate, float[] samples, int count);

	[DllImport("sherpa-onnx-c-api.dll", CallingConvention = CallingConvention.Cdecl)]
	private static extern int SherpaOnnxIsKeywordStreamReady(IntPtr spotter, IntPtr stream);

	[DllImport("sherpa-onnx-c-api.dll", CallingConvention = CallingConvention.Cdecl)]
	private static extern void SherpaOnnxDecodeKeywordStream(IntPtr spotter, IntPtr stream);

	[DllImport("sherpa-onnx-c-api.dll", CallingConvention = CallingConvention.Cdecl)]
	private static extern IntPtr SherpaOnnxGetKeywordResult(IntPtr spotter, IntPtr stream);

	[DllImport("sherpa-onnx-c-api.dll", CallingConvention = CallingConvention.Cdecl)]
	private static extern void SherpaOnnxDestroyKeywordResult(IntPtr result);

	[DllImport("sherpa-onnx-c-api.dll", CallingConvention = CallingConvention.Cdecl)]
	private static extern void SherpaOnnxResetKeywordStream(IntPtr spotter, IntPtr stream);
}
