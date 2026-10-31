using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace CapriDesktop;

internal sealed class QwenVoice : IDisposable
{
	internal const string Style = "HD";

	internal const string Build = "qwentts-5916bf7-ctx";

	private const string Hosted = "https://github.com/lachydotmcg/capri-desktop/releases/download/hd-voice-1/";

	internal const string ModelFile = "qwen-talker-1.7b-base-Q5_K_M.gguf";

	internal const string CodecFile = "qwen-tokenizer-12hz-Q8_0.gguf";

	internal const string OldEngine = "engine-b11781";

	internal const long CardNeeded = 6442450944L;

	internal const long RoomNeeded = 2684354560L;

	internal const int PieceLength = 300;

	internal static string EngineUrl = "https://github.com/lachydotmcg/capri-desktop/releases/download/hd-voice-1/qwentts-5916bf7-ctx-win-vulkan-x64.zip";

	internal static string EngineSha = "6d9042bc6af4b8c8e9bc31e85166a81c0baea8c83ff9e647f452ce61f7134a22";

	internal static long EngineSize = 16547183L;

	internal static string ModelUrl = "https://github.com/lachydotmcg/capri-desktop/releases/download/hd-voice-1/qwen-talker-1.7b-base-Q5_K_M.gguf";

	internal static string ModelSha = "4257253743e61e13882de0c355357446724231188fcf7a96d38bad838b5097c3";

	internal static long ModelSize = 1411265728L;

	internal static string CodecUrl = "https://huggingface.co/Serveurperso/Qwen3-TTS-GGUF/resolve/b7ee2e8c7459c3bea99da23e3d178125a7d1713c/qwen-tokenizer-12hz-Q8_0.gguf";

	internal static string CodecSha = "1883beeed99348fc35e23dd225e9082f93f6f8c109330a33d935baa8acdbfd94";

	internal static long CodecSize = 291150624L;

	internal static readonly string[] OldFiles = new string[2] { "Qwen3-TTS-12Hz-1.7B-Base-Q4_K_M.gguf", "mmproj-Qwen3-TTS-12Hz-1.7B-Base-Q8_0.gguf" };

	private static readonly string[] Tongues = new string[10] { "zh", "en", "de", "it", "pt", "es", "ja", "ko", "fr", "ru" };

	private static readonly string[] Languages = new string[10] { "chinese", "english", "german", "italian", "portuguese", "spanish", "japanese", "korean", "french", "russian" };

	internal static Func<ProcessStartInfo, Process> Spawn = Process.Start;

	private readonly string folder;

	private readonly string engine;

	private readonly object gate = new object();

	private readonly object waking = new object();

	private Process server;

	private int port;

	private bool awake;

	private byte[] voiced;

	private int voicedOn;

	internal volatile bool Cancel;

	internal static long PackSize => EngineSize + CodecSize + ModelSize;

	internal string Folder => folder;

	private string Server => Path.Combine(engine, "tts-server.exe");

	internal bool Installed
	{
		get
		{
			if (File.Exists(Server) && File.Exists(Path.Combine(folder, "qwen-talker-1.7b-base-Q5_K_M.gguf")))
			{
				return File.Exists(Path.Combine(folder, "qwen-tokenizer-12hz-Q8_0.gguf"));
			}
			return false;
		}
	}

	internal bool Awake
	{
		get
		{
			lock (gate)
			{
				return awake && (server == null || !server.HasExited);
			}
		}
	}

	internal QwenVoice(string data)
	{
		string path = Path.Combine(data, "voice");
		folder = Path.Combine(path, "qwen3-tts-1.7b");
		engine = Path.Combine(path, "engine-qwentts-5916bf7-ctx");
	}

	internal void Install(Action<string, long, long> progress)
	{
		if (IntPtr.Size != 8)
		{
			throw new IOException("Her HD voice needs 64-bit Windows.");
		}
		Directory.CreateDirectory(folder);
		string text = Path.Combine(folder, "qwen-talker-1.7b-base-Q5_K_M.gguf");
		string text2 = Path.Combine(folder, "qwen-tokenizer-12hz-Q8_0.gguf");
		long num = (File.Exists(Server) ? 0 : (EngineSize * 4)) + (File.Exists(text2) ? 0 : CodecSize) + (File.Exists(text) ? 0 : ModelSize) + 67108864;
		string pathRoot = Path.GetPathRoot(folder);
		if (new DriveInfo(pathRoot).AvailableFreeSpace < num)
		{
			throw new IOException("Her HD voice needs " + Engine.Amount(num) + " free on " + pathRoot.TrimEnd('\\') + ".");
		}
		Func<bool> stopped = () => Cancel;
		if (!File.Exists(Server))
		{
			string text3 = engine + ".zip";
			Engine.Fetch(EngineUrl, text3, EngineSize, EngineSha, "her HD voice", Along(progress, 0L), stopped);
			progress("Unpacking her HD voice", EngineSize, PackSize);
			string text4 = engine + ".new";
			if (Directory.Exists(text4))
			{
				Directory.Delete(text4, recursive: true);
			}
			ZipFile.ExtractToDirectory(text3, text4);
			if (!File.Exists(Path.Combine(text4, "tts-server.exe")))
			{
				throw new IOException("The HD voice download is missing tts-server.exe.");
			}
			lock (gate)
			{
				Close();
				if (Directory.Exists(engine))
				{
					Directory.Delete(engine, recursive: true);
				}
				Directory.Move(text4, engine);
			}
			File.Delete(text3);
		}
		if (!File.Exists(text2))
		{
			Engine.Fetch(CodecUrl, text2, CodecSize, CodecSha, "her HD voice", Along(progress, EngineSize), stopped);
		}
		if (!File.Exists(text))
		{
			Engine.Fetch(ModelUrl, text, ModelSize, ModelSha, "her HD voice", Along(progress, EngineSize + CodecSize), stopped);
		}
	}

	private static Action<string, long, long> Along(Action<string, long, long> progress, long before)
	{
		return (string text, long done, long total) =>
		{
			progress(text, before + done, PackSize);
		};
	}

	internal void Tidy()
	{
		string text = Path.Combine(Path.GetDirectoryName(engine), "engine-b11781");
		try
		{
			string[] array = new string[2]
			{
				text,
				text + ".new"
			};
			foreach (string path in array)
			{
				if (Directory.Exists(path))
				{
					Directory.Delete(path, recursive: true);
				}
			}
			string[] array2 = new string[2]
			{
				text + ".zip",
				text + ".zip.part"
			};
			foreach (string path2 in array2)
			{
				File.Delete(path2);
			}
			string[] oldFiles = OldFiles;
			foreach (string text2 in oldFiles)
			{
				string[] array3 = new string[2]
				{
					text2,
					text2 + ".part"
				};
				foreach (string path3 in array3)
				{
					File.Delete(Path.Combine(folder, path3));
				}
			}
		}
		catch (IOException)
		{
		}
		catch (UnauthorizedAccessException)
		{
		}
	}

	internal ProcessStartInfo StartInfo(int number)
	{
		ProcessStartInfo processStartInfo = new ProcessStartInfo(Server, "--model \"" + Path.Combine(folder, "qwen-talker-1.7b-base-Q5_K_M.gguf") + "\" --codec \"" + Path.Combine(folder, "qwen-tokenizer-12hz-Q8_0.gguf") + "\" --host 127.0.0.1 --port " + number + " --ctx-size 1024 --codec-chunk-dur 6");
		processStartInfo.UseShellExecute = false;
		processStartInfo.CreateNoWindow = true;
		processStartInfo.WorkingDirectory = engine;
		return processStartInfo;
	}

	internal void Warm()
	{
		lock (waking)
		{
			int num;
			Process process;
			lock (gate)
			{
				if (awake && (server == null || !server.HasExited))
				{
					return;
				}
				Close();
				if (IntPtr.Size != 8)
				{
					throw new VoiceException("Her HD voice needs 64-bit Windows.");
				}
				if (!Installed)
				{
					throw new VoiceException("Get her HD voice in Capri Mainframe first.");
				}
				num = (port = Engine.FreePort());
				try
				{
					process = (server = Spawn(StartInfo(num)));
				}
				catch (Win32Exception ex)
				{
					throw new VoiceException(Voice.Short("Her HD voice wouldn't start: " + ex.Message));
				}
				Engine.Lower(process);
				Engine.Bind(process);
			}
			Stopwatch stopwatch = Stopwatch.StartNew();
			while (stopwatch.Elapsed.TotalSeconds < 120.0)
			{
				lock (gate)
				{
					if (server != process)
					{
						throw new OperationCanceledException();
					}
					if (process.HasExited)
					{
						Brain.Log("HD voice", stopwatch.ElapsedMilliseconds, "tts-server stopped while loading with exit code 0x" + ((uint)Engine.ExitCode(process)).ToString("X8"));
						Close();
						throw new VoiceException("Her HD voice crashed loading. Close games, then try again.");
					}
				}
				bool flag = false;
				try
				{
					HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create("http://127.0.0.1:" + num + "/health");
					httpWebRequest.Proxy = null;
					httpWebRequest.Timeout = 2000;
					using HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
					flag = httpWebResponse.StatusCode == HttpStatusCode.OK;
				}
				catch (WebException)
				{
				}
				if (flag)
				{
					try
					{
						Say(num, "Hey, I'm awake.");
					}
					catch (VoiceException)
					{
						lock (gate)
						{
							if (server != process)
							{
								throw new OperationCanceledException();
							}
						}
						throw;
					}
					lock (gate)
					{
						if (server != process)
						{
							throw new OperationCanceledException();
						}
						awake = true;
						return;
					}
				}
				Thread.Sleep(300);
			}
			Stop();
			throw new VoiceException("Her HD voice loaded too slowly.");
		}
	}

	internal byte[] Speak(string text)
	{
		int number;
		lock (gate)
		{
			if (!awake || (server != null && server.HasExited))
			{
				throw new VoiceException("Her HD voice is still waking up.");
			}
			number = port;
		}
		return Say(number, text);
	}

	private byte[] Say(int number, string text)
	{
		Voiced(number);
		int num = Array.IndexOf(Tongues, Brain.LanguageCode(Brain.Language));
		string text2 = ",\"voice\":\"capri\",\"language\":\"" + ((num >= 0) ? Languages[num] : "english") + "\",\"seed\":7,\"max_new_tokens\":512,\"response_format\":\"wav\"}";
		int pitch = VoiceFx.Pitch;
		int speed = VoiceFx.Speed;
		int hdRobot = VoiceFx.HdRobot;
		int hdGlitch = VoiceFx.HdGlitch;
		VoiceFx.Layering layers = VoiceFx.Layers;
		List<short> list = new List<short>();
		int rate = 24000;
		foreach (string item in Pieces(Voice.Sayable(text)))
		{
			list.AddRange(Voice.Samples(Ask(number, "/v1/audio/speech", "{\"input\":" + Voice.Quote(item) + text2), out rate));
		}
		short[] array = list.ToArray();
		if (!VoiceFx.Plain(pitch, speed, hdRobot, hdGlitch, layers))
		{
			array = VoiceFx.Restyled(array, rate, pitch, speed, hdRobot, hdGlitch, VoiceFx.Seed(text), layers);
		}
		byte[] array2 = Voice.Wave(array, rate);
		if (Voice.Seconds(array2) < 0.2)
		{
			throw new VoiceException("Her HD voice made no sound.");
		}
		return array2;
	}

	private void Voiced(int number)
	{
		byte[] array = Clip();
		lock (gate)
		{
			if (voicedOn == number && Same(voiced, array))
			{
				return;
			}
		}
		Ask(number, "/v1/audio/voices", "{\"name\":\"capri\",\"wav_b64\":\"" + Convert.ToBase64String(array) + "\"}");
		lock (gate)
		{
			voiced = array;
			voicedOn = number;
		}
	}

	private static bool Same(byte[] one, byte[] other)
	{
		if (one == other)
		{
			return true;
		}
		if (one == null || one.Length != other.Length)
		{
			return false;
		}
		for (int i = 0; i < one.Length; i++)
		{
			if (one[i] != other[i])
			{
				return false;
			}
		}
		return true;
	}

	internal static List<string> Pieces(string text)
	{
		List<string> list = new List<string>();
		string text2 = "";
		string[] array = Regex.Split(text.Trim(), "(?<=[.!?…])\\s+|\\s*\\n\\s*");
		foreach (string text3 in array)
		{
			if (text3.Length != 0)
			{
				if (text2.Length > 0 && text2.Length + 1 + text3.Length > 300)
				{
					list.Add(text2);
					text2 = "";
				}
				text2 = ((text2.Length == 0) ? text3 : (text2 + " " + text3));
			}
		}
		if (text2.Length > 0)
		{
			list.Add(text2);
		}
		return list;
	}

	private static byte[] Ask(int number, string path, string json)
	{
		byte[] bytes = Encoding.UTF8.GetBytes(json);
		HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create("http://127.0.0.1:" + number + path);
		httpWebRequest.Method = "POST";
		httpWebRequest.Proxy = null;
		httpWebRequest.ContentType = "application/json";
		httpWebRequest.ContentLength = bytes.Length;
		httpWebRequest.Timeout = 60000;
		httpWebRequest.ReadWriteTimeout = 60000;
		try
		{
			using (Stream stream = httpWebRequest.GetRequestStream())
			{
				stream.Write(bytes, 0, bytes.Length);
			}
			using HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
			using Stream stream2 = httpWebResponse.GetResponseStream();
			using MemoryStream memoryStream = new MemoryStream();
			stream2.CopyTo(memoryStream);
			return memoryStream.ToArray();
		}
		catch (WebException ex)
		{
			if (!(ex.Response is HttpWebResponse httpWebResponse2))
			{
				throw new VoiceException("Her HD voice stopped answering.");
			}
			string text;
			using (httpWebResponse2)
			{
				using StreamReader streamReader = new StreamReader(httpWebResponse2.GetResponseStream());
				text = streamReader.ReadToEnd();
			}
			throw new VoiceException(Voice.Short("Her HD voice glitched: HTTP " + (int)httpWebResponse2.StatusCode + " " + Regex.Replace(text ?? "", "\\s+", " ").Trim()));
		}
		catch (IOException)
		{
			throw new VoiceException("Her HD voice stopped answering.");
		}
	}

	internal Func<string, byte[]> Or(Func<string, byte[]> fallback, Action<string> trouble)
	{
		return (string words) =>
		{
			Stopwatch stopwatch = Stopwatch.StartNew();
			try
			{
				byte[] array = Speak(words);
				if (stopwatch.Elapsed.TotalSeconds > Math.Max(6.0, Voice.Seconds(array) * 1.2))
				{
					trouble("Her HD voice is running slowly right now, so she's using her free voice for a bit.");
				}
				return array;
			}
			catch (VoiceException ex)
			{
				trouble(Voice.Short(ex.Message) + " She's using her free voice for a bit.");
				if (fallback == null)
				{
					throw;
				}
				return fallback(words);
			}
		};
	}

	private byte[] Clip()
	{
		string path = Path.Combine(folder, "reference.wav");
		try
		{
			if (File.Exists(path))
			{
				return File.ReadAllBytes(path);
			}
		}
		catch (IOException)
		{
		}
		throw new VoiceException("Her HD voice needs a clip: Capri ships no voice. Pick a clip of a voice you have the rights to use.");
	}

	internal static long FreeRoom()
	{
		string text = Path.Combine(Environment.SystemDirectory, "nvidia-smi.exe");
		if (!File.Exists(text))
		{
			return -1L;
		}
		try
		{
			ProcessStartInfo processStartInfo = new ProcessStartInfo(text, "--query-gpu=memory.total,memory.used --format=csv,noheader,nounits");
			processStartInfo.UseShellExecute = false;
			processStartInfo.CreateNoWindow = true;
			processStartInfo.RedirectStandardOutput = true;
			ProcessStartInfo startInfo = processStartInfo;
			using Process process = Process.Start(startInfo);
			string text2 = process.StandardOutput.ReadToEnd();
			if (!process.WaitForExit(5000))
			{
				return -1L;
			}
			long num = -1L;
			string[] array = text2.Split('\n');
			foreach (string text3 in array)
			{
				string[] array2 = text3.Split(',');
				if (array2.Length == 2 && long.TryParse(array2[0].Trim(), out var result) && long.TryParse(array2[1].Trim(), out var result2))
				{
					num = Math.Max(num, result - result2 << 20);
				}
			}
			return num;
		}
		catch (Win32Exception)
		{
			return -1L;
		}
		catch (InvalidOperationException)
		{
			return -1L;
		}
	}

	internal void Attach(int number)
	{
		lock (gate)
		{
			Close();
			port = number;
			awake = true;
		}
	}

	internal void Stop()
	{
		lock (gate)
		{
			Close();
		}
	}

	private void Close()
	{
		awake = false;
		voiced = null;
		if (server == null)
		{
			return;
		}
		try
		{
			if (!server.HasExited)
			{
				server.Kill();
				server.WaitForExit(3000);
			}
		}
		catch (InvalidOperationException)
		{
		}
		catch (Win32Exception)
		{
		}
		server.Dispose();
		server = null;
	}

	public void Dispose()
	{
		Stop();
	}
}
