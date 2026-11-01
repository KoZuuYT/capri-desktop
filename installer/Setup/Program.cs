using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;
#if !UNINSTALLER
using System.Web.Script.Serialization;
#endif

namespace CapriSetup;

// installs the modded Capri where the official setup puts her (%LOCALAPPDATA%\Capri\Capri.exe), so she counts as
// installed, starts with Windows and keeps her memories and models in the same folder
internal static class Program
{
	internal const string Title = "Capri (mod)";

	internal const string UninstallKey = "Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\Capri.Desktop";

	internal const string RunKey = "Software\\Microsoft\\Windows\\CurrentVersion\\Run";

	internal const string ApprovedKey = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\StartupApproved\\Run";

	internal static string Folder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Capri");

	internal static string Exe => Path.Combine(Folder, "Capri.exe");

	internal static string Settings => Path.Combine(Folder, "settings.json");

	internal static string Version => Application.ProductVersion.Split('+')[0];

	internal static string StartMenuLink => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "Capri.lnk");

	internal static string DesktopLink => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "Capri.lnk");

	[STAThread]
	private static void Main(string[] args)
	{
		Application.EnableVisualStyles();
		Application.SetCompatibleTextRenderingDefault(false);
#if UNINSTALLER
		Uninstall();
#else
		// --preview walks through every step without touching the PC; --snap <folder> saves a picture of each page
		bool preview = Array.IndexOf(args, "--preview") >= 0;
		int snap = Array.IndexOf(args, "--snap");
		if (snap >= 0 && snap + 1 < args.Length)
		{
			SetupForm.Snap(args[snap + 1]);
			return;
		}
		Application.Run(new SetupForm(preview));
#endif
	}

	internal static void CloseCapri()
	{
		foreach (Process process in Process.GetProcessesByName("Capri"))
		{
			try
			{
				process.Kill();
				process.WaitForExit(5000);
			}
			catch (Exception)
			{
			}
			finally
			{
				process.Dispose();
			}
		}
		Thread.Sleep(500);
	}

#if UNINSTALLER
	private static void Uninstall()
	{
		if (MessageBox.Show("Remover a Capri deste computador?", Title, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
		{
			return;
		}
		bool everything = MessageBox.Show("Apagar também as memórias, configurações e modelos baixados dela?\n\nSim: apaga tudo.\nNão: deixa esses arquivos, caso você instale de novo.", Title, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
		CloseCapri();
		try
		{
			using RegistryKey run = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
			run?.DeleteValue("Capri", throwOnMissingValue: false);
			Registry.CurrentUser.DeleteSubKeyTree(UninstallKey, throwOnMissingSubKey: false);
		}
		catch (Exception)
		{
		}
		foreach (string link in new[] { StartMenuLink, DesktopLink })
		{
			try
			{
				File.Delete(link);
			}
			catch (Exception)
			{
			}
		}
		try
		{
			File.Delete(Exe);
		}
		catch (Exception)
		{
		}
		// this program runs from the folder, so a short-lived cmd removes it after it closes
		string clean = everything ? "rmdir /s /q \"" + Folder + "\"" : "del /f /q \"" + Path.Combine(Folder, "Uninstall.exe") + "\"";
		Process.Start(new ProcessStartInfo("cmd.exe", "/c ping 127.0.0.1 -n 3 >nul & " + clean)
		{
			CreateNoWindow = true,
			UseShellExecute = false,
			WorkingDirectory = Path.GetTempPath()
		});
		MessageBox.Show("A Capri foi removida.", Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
	}
#else
	internal static bool Existing => File.Exists(Exe);

	internal static bool HasSettings => File.Exists(Settings);

	// say(percent, line): the window turns these into the progress bar and the log
	internal static void Install(Options options, Action<int, string> say)
	{
		say(2, "Fechando a Capri, se estiver aberta...");
		CloseCapri();
		Directory.CreateDirectory(Folder);
		say(8, "Copiando o núcleo da Capri (Capri.exe)...");
		Unpack("Capri.exe", Exe, percent => say(8 + percent * 62 / 100, null));
		say(72, "Copiando o desinstalador...");
		Unpack("Uninstall.exe", Path.Combine(Folder, "Uninstall.exe"), null);
		File.WriteAllText(Path.Combine(Folder, ".capri-install"), "Capri desktop companion (mod " + Version + ")");
		say(78, "Criando atalhos...");
		Link(StartMenuLink, options.StartMenu);
		Link(DesktopLink, options.Desktop);
		say(84, "Registrando em Configurações > Aplicativos...");
		using (RegistryKey key = Registry.CurrentUser.CreateSubKey(UninstallKey))
		{
			key.SetValue("DisplayName", "Capri (mod)");
			key.SetValue("DisplayVersion", Version);
			key.SetValue("Publisher", "Capri fan project (mod pessoal)");
			key.SetValue("DisplayIcon", Exe);
			key.SetValue("InstallLocation", Folder);
			key.SetValue("UninstallString", "\"" + Path.Combine(Folder, "Uninstall.exe") + "\"");
			key.SetValue("NoModify", 1, RegistryValueKind.DWord);
			key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
			key.SetValue("EstimatedSize", (int)(new FileInfo(Exe).Length / 1024), RegistryValueKind.DWord);
		}
		say(90, options.Startup ? "Ligando a Capri junto com o Windows..." : "Tirando a Capri da inicialização do Windows...");
		Startup(options.Startup);
		say(95, options.Apply ? "Gravando as configurações escolhidas..." : "Mantendo as configurações atuais...");
		Configure(options);
		say(100, "Tudo pronto.");
	}

	// the same steps, slowed down and without touching anything, for trying the window out
	internal static void Pretend(Options options, Action<int, string> say)
	{
		string[] lines = { "Fechando a Capri, se estiver aberta...", "Copiando o núcleo da Capri (Capri.exe)...", "Copiando o desinstalador...", "Criando atalhos...", "Registrando em Configurações > Aplicativos...", "Ligando a Capri junto com o Windows...", "Gravando as configurações escolhidas...", "Tudo pronto." };
		int[] marks = { 2, 8, 72, 78, 84, 90, 95, 100 };
		for (int i = 0; i < lines.Length; i++)
		{
			say(marks[i], lines[i]);
			if (i == 1)
			{
				for (int p = 0; p <= 100; p += 4)
				{
					Thread.Sleep(70);
					say(8 + p * 62 / 100, null);
				}
			}
			Thread.Sleep(350);
		}
	}

	private static void Unpack(string name, string path, Action<int> progress)
	{
		string partial = path + ".new";
		using (Stream from = Assembly.GetExecutingAssembly().GetManifestResourceStream(name))
		using (FileStream to = File.Create(partial))
		{
			byte[] chunk = new byte[1 << 18];
			long done = 0;
			int read;
			int shown = -1;
			while ((read = from.Read(chunk, 0, chunk.Length)) > 0)
			{
				to.Write(chunk, 0, read);
				done += read;
				int percent = (int)(done * 100 / Math.Max(1, from.Length));
				if (progress != null && percent != shown)
				{
					shown = percent;
					progress(percent);
				}
			}
		}
		if (File.Exists(path))
		{
			File.Delete(path);
		}
		File.Move(partial, path);
	}

	private static void Link(string path, bool wanted)
	{
		if (!wanted)
		{
			try
			{
				File.Delete(path);
			}
			catch (Exception)
			{
			}
			return;
		}
		Type shellType = Type.GetTypeFromProgID("WScript.Shell");
		object shell = Activator.CreateInstance(shellType);
		object link = shellType.InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, shell, new object[] { path });
		Type linkType = link.GetType();
		linkType.InvokeMember("TargetPath", BindingFlags.SetProperty, null, link, new object[] { Exe });
		linkType.InvokeMember("WorkingDirectory", BindingFlags.SetProperty, null, link, new object[] { Folder });
		linkType.InvokeMember("IconLocation", BindingFlags.SetProperty, null, link, new object[] { Exe + ",0" });
		linkType.InvokeMember("Description", BindingFlags.SetProperty, null, link, new object[] { "Capri (mod)" });
		linkType.InvokeMember("Save", BindingFlags.InvokeMethod, null, link, null);
	}

	// the same Run entry Capri writes for herself (CapriDesktop.Startup.Set)
	private static void Startup(bool on)
	{
		using (RegistryKey run = Registry.CurrentUser.CreateSubKey(RunKey))
		{
			if (on)
			{
				run.SetValue("Capri", "\"" + Exe + "\"");
			}
			else
			{
				run.DeleteValue("Capri", throwOnMissingValue: false);
			}
		}
		using RegistryKey approved = Registry.CurrentUser.OpenSubKey(ApprovedKey, writable: true);
		approved?.DeleteValue("Capri", throwOnMissingValue: false);
	}

	// writes the chosen options into her settings.json, keeping every other value (keys, memories, voice) as it is;
	// StartupSet always goes in so she doesn't put herself back in Windows startup after the user said no
	private static void Configure(Options options)
	{
		JavaScriptSerializer json = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
		Dictionary<string, object> settings;
		if (File.Exists(Settings))
		{
			try
			{
				settings = json.Deserialize<Dictionary<string, object>>(File.ReadAllText(Settings));
			}
			catch (Exception)
			{
				return; // she has her own recovery for a broken file, so leave it alone
			}
			if (settings == null)
			{
				return;
			}
			File.Copy(Settings, Path.Combine(Folder, "settings.before-setup.json"), overwrite: true);
		}
		else
		{
			settings = new Dictionary<string, object>();
		}
		bool fresh = !settings.ContainsKey("Size");
		settings["StartupSet"] = true;
		if (options.Apply)
		{
			string language = options.Language switch
			{
				1 => "Portuguese",
				2 => "English",
				_ => fresh ? SystemLanguage() : null
			};
			if (language != null)
			{
				settings["Language"] = language;
			}
			settings["Size"] = new[] { 170, 240, 320 }[options.Size];
			settings["Sounds"] = new[] { 30, 60, 90 }[options.Sounds];
			settings["Mischief"] = options.Mischief;
			settings["EyePopups"] = options.Eyes;
			settings["Petting"] = options.Petting;
			settings["VoiceOrders"] = options.VoiceOrders;
			settings["OnTop"] = options.OnTop;
		}
		File.WriteAllText(Settings, json.Serialize(settings));
	}

	private static string SystemLanguage()
	{
		return CultureInfo.CurrentUICulture.TwoLetterISOLanguageName switch
		{
			"pt" => "Portuguese",
			"es" => "Spanish",
			"fr" => "French",
			"de" => "German",
			"it" => "Italian",
			_ => null
		};
	}
#endif
}

#if !UNINSTALLER
// what the options page sets; defaults match a first install
internal sealed class Options
{
	public bool Desktop = true;

	public bool StartMenu = true;

	public bool Startup = true;

	public bool OpenAfter = true;

	// false keeps her current settings.json untouched (apart from the startup choice)
	public bool Apply = true;

	public int Language; // 0 system, 1 Portuguese, 2 English

	public int Size = 1; // small, medium, large

	public int Sounds = 1; // low, medium, high

	public bool Mischief;

	public bool Eyes = true;

	public bool Petting = true;

	public bool VoiceOrders = true;

	public bool OnTop = true;
}
#endif
