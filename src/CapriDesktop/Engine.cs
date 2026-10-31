using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace CapriDesktop;

internal sealed class Engine
{
	private struct BasicLimits
	{
		internal long ProcessTime;

		internal long JobTime;

		internal uint Flags;

		internal UIntPtr MinimumWorkingSet;

		internal UIntPtr MaximumWorkingSet;

		internal uint ActiveProcesses;

		internal UIntPtr Affinity;

		internal uint Priority;

		internal uint Scheduling;
	}

	private struct ExtendedLimits
	{
		internal BasicLimits Basic;

		internal ulong ReadOperations;

		internal ulong WriteOperations;

		internal ulong OtherOperations;

		internal ulong ReadBytes;

		internal ulong WriteBytes;

		internal ulong OtherBytes;

		internal UIntPtr ProcessMemory;

		internal UIntPtr JobMemory;

		internal UIntPtr PeakProcessMemory;

		internal UIntPtr PeakJobMemory;
	}

	internal const string Build = "b11669";

	private const string ZipUrl = "https://github.com/ggml-org/llama.cpp/releases/download/b11669/llama-b11669-bin-win-vulkan-x64.zip";

	private const string ZipSha = "2c07574e7bde71321cb785282b20cc821e951132149eeceeea32ddebdbf0598e";

	private const long ZipSize = 32182350L;

	internal static readonly Pick[] Picks = new Pick[3]
	{
		new Pick("Gemma 4 E4B", "gemma4:e4b-it-qat", "ggml-org/gemma-4-E4B-it-GGUF", "a69f018d7678a2b9f5e705f8903d6fb29b9b25dc", "gemma-4-E4B-it-Q4_0.gguf", "a555b900214b477d8880e7832e0b8925e139b0159640036b09fe472b6f2097f2", 4590807392L).Eyes("ggml-org/gemma-4-E4B-it-GGUF", "a69f018d7678a2b9f5e705f8903d6fb29b9b25dc", "mmproj-gemma-4-E4B-it-Q8_0.gguf", "197f49a93027f9843772bd24a6a9e0be2a32a788de5a3def330e9c585d86edd1", 559874816L),
		new Pick("Gemma 4 E2B", "gemma4:e2b-it-qat", "unsloth/gemma-4-E2B-it-GGUF", "0314792d7f1f7e229411f620751375812bb9faf2", "gemma-4-E2B-it-Q4_K_M.gguf", "740185b21d22ceb83a11c3aa62ad5842ef32c70f6096d756bbee85a1e4ec34b8", 3106738272L).Eyes("ggml-org/gemma-4-E2B-it-GGUF", "6938c58e698502037401a5f19f6905f2fb2f7dee", "mmproj-gemma-4-E2B-it-Q8_0.gguf", "9406f99c16d68cda4f1f0552192dcc99021ea1fc6d2fd50b1dc3ccf30d04b292", 557368064L),
		new Pick("Qwen3 4B", "qwen3:4b-instruct-2507-q4_K_M", "unsloth/Qwen3-4B-Instruct-2507-GGUF", "a06e946bb6b655725eafa393f4a9745d460374c9", "Qwen3-4B-Instruct-2507-Q4_K_M.gguf", "3605803b982cb64aead44f6c1b2ae36e3acdb41d8e46c8a94c6533bc4c67e597", 2497281120L)
	};

	private readonly string root;

	private readonly object gate = new object();

	private Process server;

	private static IntPtr job;

	private static readonly object jobs = new object();

	private int port;

	private bool serverEyes;

	private bool serverCpu;

	private int serverWindow;

	internal volatile bool Cancel;

	internal volatile bool CpuOnly;

	internal static Func<ProcessStartInfo, Process> Spawn = Process.Start;

	private string CpuMark => Path.Combine(Folder, "cpu-only.txt");

	private string Folder => Path.Combine(root, "engine-b11669");

	private string Server => Path.Combine(Folder, "llama-server.exe");

	internal string Address
	{
		get
		{
			lock (gate)
			{
				return "http://127.0.0.1:" + port;
			}
		}
	}

	internal bool Crashed
	{
		get
		{
			lock (gate)
			{
				return server != null && server.HasExited;
			}
		}
	}

	internal Engine(string data)
	{
		root = Path.Combine(data, "brain");
		CpuOnly = File.Exists(CpuMark);
	}

	internal void KeepCpu()
	{
		try
		{
			if (Directory.Exists(Folder))
			{
				File.WriteAllText(CpuMark, "The graphics card garbled replies, so the brain runs on the CPU. Delete this file to try the graphics card again.\n");
			}
		}
		catch (IOException)
		{
		}
		catch (UnauthorizedAccessException)
		{
		}
	}

	internal static Pick Fit(Machine machine)
	{
		if (machine.Vram >= 7516192768L && machine.Ram >= 12884901888L)
		{
			return Picks[0];
		}
		if (machine.Ram >= 7516192768L || machine.Vram >= 4294967296L)
		{
			return Picks[1];
		}
		return Picks[2];
	}

	internal static string Amount(long size)
	{
		if (!((double)size < 1000000000.0))
		{
			return ((double)size / 1000000000.0).ToString("0.#") + " GB";
		}
		return Math.Round((double)size / 1000000.0) + " MB";
	}

	private string ModelPath(Pick pick)
	{
		return Path.Combine(Path.Combine(root, "models"), pick.File);
	}

	private string EyesPath(Pick pick)
	{
		if (pick.EyesFile != null)
		{
			return Path.Combine(Path.Combine(root, "models"), pick.EyesFile);
		}
		return null;
	}

	internal bool HasEyes(Pick pick)
	{
		string text = EyesPath(pick);
		if (text != null)
		{
			return File.Exists(text);
		}
		return false;
	}

	internal void InstallEyes(Pick pick, Action<string, long, long> progress)
	{
		if (pick.EyesFile != null && !HasEyes(pick))
		{
			Cancel = false;
			string pathRoot = Path.GetPathRoot(root);
			if (new DriveInfo(pathRoot).AvailableFreeSpace < pick.EyesSize + 67108864)
			{
				throw new IOException("Storage too full: her eyes need " + Amount(pick.EyesSize) + " free on " + pathRoot.TrimEnd('\\') + ".");
			}
			Fetch(pick.EyesUrl, EyesPath(pick), pick.EyesSize, pick.EyesSha256, "her eyes", progress, () => Cancel);
		}
	}

	internal Pick Installed()
	{
		if (!File.Exists(Server))
		{
			return null;
		}
		Pick[] picks = Picks;
		foreach (Pick pick in picks)
		{
			if (File.Exists(ModelPath(pick)))
			{
				return pick;
			}
		}
		return null;
	}

	internal void Install(Pick pick, Action<string, long, long> progress)
	{
		Directory.CreateDirectory(root);
		long num = (File.Exists(Server) ? 0 : 128729400) + (File.Exists(ModelPath(pick)) ? 0 : pick.Size) + 268435456;
		string pathRoot = Path.GetPathRoot(root);
		if (new DriveInfo(pathRoot).AvailableFreeSpace < num)
		{
			throw new IOException("Storage too full: her brain needs " + Amount(num) + " free on " + pathRoot.TrimEnd('\\') + ".");
		}
		if (!File.Exists(Server))
		{
			string text = Path.Combine(root, "engine-b11669.zip");
			Fetch("https://github.com/ggml-org/llama.cpp/releases/download/b11669/llama-b11669-bin-win-vulkan-x64.zip", text, 32182350L, "2c07574e7bde71321cb785282b20cc821e951132149eeceeea32ddebdbf0598e", "the engine", progress, () => Cancel);
			string text2 = Folder + ".new";
			if (Directory.Exists(text2))
			{
				Directory.Delete(text2, recursive: true);
			}
			ZipFile.ExtractToDirectory(text, text2);
			if (!File.Exists(Path.Combine(text2, "llama-server.exe")))
			{
				throw new IOException("The engine download is missing llama-server.exe.");
			}
			if (Directory.Exists(Folder))
			{
				Directory.Delete(Folder, recursive: true);
			}
			Directory.Move(text2, Folder);
			File.Delete(text);
		}
		if (!File.Exists(ModelPath(pick)))
		{
			Directory.CreateDirectory(Path.GetDirectoryName(ModelPath(pick)));
			Fetch(pick.Url, ModelPath(pick), pick.Size, pick.Sha256, pick.Name, progress, () => Cancel);
		}
	}

	internal static void Fetch(string url, string target, long size, string sha256, string label, Action<string, long, long> progress, Func<bool> stopped)
	{
		ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
		string text = target + ".part";
		long num = (File.Exists(text) ? new FileInfo(text).Length : 0);
		if (num > size)
		{
			File.Delete(text);
			num = 0L;
		}
		int num2 = 0;
		while (num < size)
		{
			long num3 = num;
			try
			{
				HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
				httpWebRequest.UserAgent = "Capri-Desktop";
				httpWebRequest.Timeout = 30000;
				httpWebRequest.ReadWriteTimeout = 60000;
				if (num > 0)
				{
					httpWebRequest.AddRange(num);
				}
				using HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
				if (num > 0 && httpWebResponse.StatusCode != HttpStatusCode.PartialContent)
				{
					num = 0L;
				}
				using Stream stream = httpWebResponse.GetResponseStream();
				using FileStream fileStream = new FileStream(text, (num > 0) ? FileMode.Append : FileMode.Create, FileAccess.Write);
				byte[] array = new byte[65536];
				long num4 = 0L;
				int num5;
				while ((num5 = stream.Read(array, 0, array.Length)) > 0)
				{
					if (stopped())
					{
						throw new OperationCanceledException();
					}
					fileStream.Write(array, 0, num5);
					num += num5;
					if (num - num4 >= 4194304)
					{
						num4 = num;
						progress("Downloading " + label, num, size);
					}
				}
			}
			catch (WebException)
			{
				if (stopped())
				{
					throw new OperationCanceledException();
				}
			}
			catch (IOException)
			{
				if (stopped())
				{
					throw new OperationCanceledException();
				}
			}
			if (num < size)
			{
				num2 = ((num <= num3) ? (num2 + 1) : 0);
				if (num2 >= 4)
				{
					throw new IOException("The download of " + label + " keeps stopping. Check your connection and try again.");
				}
				Thread.Sleep(1500 * num2);
			}
		}
		if (num != size)
		{
			File.Delete(text);
			throw new IOException(label + " came down the wrong size. Try again.");
		}
		progress("Checking " + label, size, size);
		if (!string.Equals(Hash(text), sha256, StringComparison.OrdinalIgnoreCase))
		{
			File.Delete(text);
			throw new IOException(label + " failed its checksum. Try again.");
		}
		if (File.Exists(target))
		{
			File.Delete(target);
		}
		File.Move(text, target);
	}

	internal static string Hash(string path)
	{
		using SHA256 sHA = SHA256.Create();
		using FileStream inputStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 1048576);
		StringBuilder stringBuilder = new StringBuilder();
		byte[] array = sHA.ComputeHash(inputStream);
		foreach (byte b in array)
		{
			stringBuilder.Append(b.ToString("x2"));
		}
		return stringBuilder.ToString();
	}

	internal static int Threads(int processors)
	{
		return Math.Max(1, Math.Min(4, processors / 2));
	}

	internal ProcessStartInfo StartInfo(Pick pick, bool eyes, bool cpu, int number, int window = 4096)
	{
		int num = Threads(Environment.ProcessorCount);
		ProcessStartInfo processStartInfo = new ProcessStartInfo(Server, "-m \"" + ModelPath(pick) + "\"" + (eyes ? (" --mmproj \"" + EyesPath(pick) + "\"") : "") + " --host 127.0.0.1 --port " + number + " -c " + window + " -np 1 -a capri --jinja --reasoning off --no-webui --sleep-idle-seconds 900 --log-disable -t " + num + " -tb " + num + " -b 256 -ub 128 --poll 0 --poll-batch 0" + (cpu ? " --device none -ngl 0 --no-mmproj-offload" : ""));
		processStartInfo.UseShellExecute = false;
		processStartInfo.CreateNoWindow = true;
		processStartInfo.WorkingDirectory = Folder;
		return processStartInfo;
	}

	internal static int ExitCode(Process process)
	{
		try
		{
			return process.ExitCode;
		}
		catch (InvalidOperationException)
		{
			return -1;
		}
	}

	internal static void Lower(Process child)
	{
		try
		{
			child.PriorityClass = ProcessPriorityClass.BelowNormal;
		}
		catch (InvalidOperationException)
		{
		}
		catch (Win32Exception)
		{
		}
	}

	internal void Ensure(Pick pick)
	{
		lock (gate)
		{
			bool flag = HasEyes(pick);
			int window = Brain.Window;
			if (server != null && !server.HasExited && serverEyes == flag && serverCpu == CpuOnly && serverWindow == window)
			{
				return;
			}
			if (server != null)
			{
				Stop();
			}
			if (!File.Exists(Server) || !File.Exists(ModelPath(pick)))
			{
				throw new BrainException("Brain not downloaded. Press Set up her brain.");
			}
			port = FreePort();
			serverEyes = flag;
			serverCpu = CpuOnly;
			serverWindow = window;
			server = Spawn(StartInfo(pick, flag, serverCpu, port, window));
			Lower(server);
			Bind(server);
		}
		Stopwatch stopwatch = Stopwatch.StartNew();
		while (stopwatch.Elapsed.TotalSeconds < 180.0)
		{
			Process process;
			bool flag2;
			lock (gate)
			{
				process = server;
				flag2 = !serverCpu;
			}
			if (process == null || process.HasExited)
			{
				bool flag3 = process != null && flag2;
				Brain.Log("brain", -1L, "llama-server stopped while loading " + pick.Name + (flag2 ? "" : " on the CPU") + ((process == null) ? "" : (" with exit code 0x" + ((uint)ExitCode(process)).ToString("X8"))) + (flag3 ? ", restarting it on the CPU" : ""));
				if (!flag3)
				{
					throw new BrainException("Brain crashed loading. Close games, press Refresh.");
				}
				CpuOnly = true;
				Ensure(pick);
				return;
			}
			try
			{
				HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(Address + "/health");
				httpWebRequest.Proxy = null;
				httpWebRequest.Timeout = 2000;
				using HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
				if (httpWebResponse.StatusCode == HttpStatusCode.OK)
				{
					return;
				}
			}
			catch (WebException)
			{
			}
			Thread.Sleep(400);
		}
		throw new BrainException("Brain loaded too slowly. Close games, press Refresh.");
	}

	internal static bool Lost(string problem)
	{
		return Regex.IsMatch(problem ?? "", "ErrorDeviceLost|DEVICE_LOST|device lost|vk::", RegexOptions.IgnoreCase);
	}

	internal void Stop()
	{
		lock (gate)
		{
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
	}

	internal static int FreePort()
	{
		TcpListener tcpListener = new TcpListener(IPAddress.Loopback, 0);
		tcpListener.Start();
		int result = ((IPEndPoint)tcpListener.LocalEndpoint).Port;
		tcpListener.Stop();
		return result;
	}

	[DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
	private static extern IntPtr CreateJobObject(IntPtr attributes, string name);

	[DllImport("kernel32.dll")]
	private static extern bool SetInformationJobObject(IntPtr job, int kind, ref ExtendedLimits limits, int size);

	[DllImport("kernel32.dll")]
	private static extern bool AssignProcessToJobObject(IntPtr job, IntPtr process);

	internal static void Bind(Process child)
	{
		try
		{
			lock (jobs)
			{
				if (job == IntPtr.Zero)
				{
					IntPtr intPtr = CreateJobObject(IntPtr.Zero, null);
					ExtendedLimits limits = new ExtendedLimits
					{
						Basic = 
						{
							Flags = 8192u
						}
					};
					if (intPtr != IntPtr.Zero && SetInformationJobObject(intPtr, 9, ref limits, Marshal.SizeOf(typeof(ExtendedLimits))))
					{
						job = intPtr;
					}
				}
				if (job != IntPtr.Zero)
				{
					AssignProcessToJobObject(job, child.Handle);
				}
			}
		}
		catch (InvalidOperationException)
		{
		}
		catch (Win32Exception)
		{
		}
	}
}
