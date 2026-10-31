using System;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Security.Principal;
using System.Threading;
using System.Windows.Forms;

namespace CapriDesktop;

internal static class Program
{
	internal const string StartMarker = "starting.on";

	internal static bool StartedFresh;

	private static string lastCrash;

	private static int lastCrashAt;

	[STAThread]
	private static int Main(string[] args)
	{
		try
		{
			Native.SetProcessDpiAwarenessContext(new IntPtr(-4));
		}
		catch (EntryPointNotFoundException)
		{
			Native.SetProcessDPIAware();
		}
		Application.EnableVisualStyles();
		Application.SetCompatibleTextRenderingDefault(defaultValue: false);
		bool flag = args.Length == 2 && args[0] == "--smoke-test";
		string value = WindowsIdentity.GetCurrent().User.Value;
		bool createdNew;
		using (new Mutex(initiallyOwned: true, "Local\\Capri.Desktop." + value + (flag ? ".Test" : ""), out createdNew))
		{
			using EventWaitHandle eventWaitHandle = new EventWaitHandle(initialState: false, EventResetMode.AutoReset, "Local\\Capri.Restore." + value + (flag ? ".Test" : ""));
			if (!createdNew)
			{
				Native.AllowSetForegroundWindow(-1);
				eventWaitHandle.Set();
				return 0;
			}
			try
			{
				string text = (flag ? Path.GetFullPath(args[1]) : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Capri"));
				Brain.LogPath = Path.Combine(text, "errors.txt");
				AppDomain.CurrentDomain.ProcessExit += (object param0, EventArgs param1) =>
				{
					HerCursor.Off();
				};
				AppDomain.CurrentDomain.UnhandledException += (object sender, UnhandledExceptionEventArgs crash) =>
				{
					HerCursor.Off();
					Crashed(crash.ExceptionObject as Exception);
				};
				if (!flag)
				{
					Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
					Application.ThreadException += (object sender, ThreadExceptionEventArgs crash) =>
					{
						Crashed(crash.Exception);
					};
					StartedFresh = Starting(text);
				}
				using Companion companion = new Companion(text, eventWaitHandle, flag);
				if (flag)
				{
					companion.StartSmokeTest(text);
				}
				Application.Run(companion);
				HerCursor.Off();
				return companion.TestFailed ? 1 : 0;
			}
			catch (Exception ex2)
			{
				HerCursor.Off();
				if (flag)
				{
					Directory.CreateDirectory(args[1]);
					File.WriteAllText(Path.Combine(args[1], "failure.txt"), ex2.ToString());
					return 1;
				}
				Crashed(ex2);
				Notice.Tell(null, "Capri could not start.\n\n" + ex2.Message, "Capri", error: true);
				return 1;
			}
		}
	}

	internal static bool Starting(string data)
	{
		string path = Path.Combine(data, "starting.on");
		string text = Path.Combine(data, "settings.json");
		bool result = false;
		try
		{
			Directory.CreateDirectory(data);
			int num = 0;
			if (File.Exists(path))
			{
				string[] array = File.ReadAllLines(path);
				num = 1 + ((array.Length > 1 && int.TryParse(array[1], out var result2) && result2 > 0) ? result2 : 0);
			}
			if (num >= 2 && File.Exists(text))
			{
				string text2 = Path.Combine(data, "settings.crashed.json");
				if (File.Exists(text2))
				{
					File.Delete(text2);
				}
				File.Move(text, text2);
				KeepKeys(text2, text);
				result = true;
				num = 0;
				Brain.Log("startup", -1L, "Capri closed while starting twice in a row, so she started with fresh settings and kept your keys. The old ones are in settings.crashed.json.");
			}
			else if (num == 1)
			{
				Brain.Log("startup", -1L, "Capri closed while starting last time. If it happens again, she starts with fresh settings and keeps your keys.");
			}
			File.WriteAllText(path, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + Environment.NewLine + num);
		}
		catch (IOException)
		{
		}
		catch (UnauthorizedAccessException)
		{
		}
		return result;
	}

	private static void KeepKeys(string crashed, string settings)
	{
		Preferences preferences;
		try
		{
			using FileStream stream = File.OpenRead(crashed);
			preferences = (Preferences)new DataContractJsonSerializer(typeof(Preferences)).ReadObject(stream);
		}
		catch (Exception)
		{
			return;
		}
		Preferences preferences2 = new Preferences();
		preferences2.Language = Brain.SystemLanguage(CultureInfo.CurrentUICulture);
		Preferences preferences3 = preferences2;
		preferences3.VoiceKey = preferences.VoiceKey;
		preferences3.VoiceId = preferences.VoiceId;
		preferences3.VoiceModel = preferences.VoiceModel;
		preferences3.AgentId = preferences.AgentId;
		preferences3.AgentVersion = preferences.AgentVersion;
		preferences3.AgentPolyglot = preferences.AgentPolyglot;
		preferences3.AgentPersonality = preferences.AgentPersonality;
		preferences3.TextKey = preferences.TextKey;
		preferences3.TextModel = preferences.TextModel;
		preferences3.NvidiaKey = preferences.NvidiaKey;
		preferences3.NvidiaModel = preferences.NvidiaModel;
		preferences3.PaidKey = preferences.PaidKey;
		preferences3.PaidModel = preferences.PaidModel;
		preferences3.ServerUrl = preferences.ServerUrl;
		preferences3.ServerApi = preferences.ServerApi;
		preferences3.ServerModel = preferences.ServerModel;
		preferences3.ServerSpeaker = preferences.ServerSpeaker;
		preferences3.ServerKey = preferences.ServerKey;
		preferences3.AsideUrl = preferences.AsideUrl;
		preferences3.AsideApi = preferences.AsideApi;
		preferences3.AsideModel = preferences.AsideModel;
		preferences3.AsideSpeaker = preferences.AsideSpeaker;
		preferences3.AsideKey = preferences.AsideKey;
		preferences3.CloudVoice = preferences.CloudVoice;
		preferences3.TalkMods = preferences.TalkMods;
		preferences3.TalkVk = preferences.TalkVk;
		preferences3.MuteMods = preferences.MuteMods;
		preferences3.MuteVk = preferences.MuteVk;
		preferences3.TypeMods = preferences.TypeMods;
		preferences3.TypeVk = preferences.TypeVk;
		preferences3.HideMods = preferences.HideMods;
		preferences3.HideVk = preferences.HideVk;
		preferences3.QuietMods = preferences.QuietMods;
		preferences3.QuietVk = preferences.QuietVk;
		try
		{
			using FileStream stream2 = File.Create(settings);
			new DataContractJsonSerializer(typeof(Preferences)).WriteObject(stream2, preferences3);
		}
		catch (Exception)
		{
			try
			{
				File.Delete(settings);
			}
			catch (Exception)
			{
			}
		}
	}

	internal static void Crashed(Exception error)
	{
		if (error == null)
		{
			return;
		}
		while (error.InnerException != null)
		{
			error = error.InnerException;
		}
		string text = error.GetType().Name + ": " + error.Message;
		lock (typeof(Program))
		{
			if (text == lastCrash && Environment.TickCount - lastCrashAt < 60000)
			{
				return;
			}
			lastCrash = text;
			lastCrashAt = Environment.TickCount;
		}
		Brain.Log("crash", -1L, text);
		int num = 0;
		string[] array = (error.StackTrace ?? "").Split('\n');
		foreach (string text2 in array)
		{
			string text3 = text2.Trim();
			if (text3.Length != 0)
			{
				Brain.Log("crash", -1L, text3);
				if (++num == 4)
				{
					break;
				}
			}
		}
	}
}
