using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace CapriSetup;

// installs the modded Capri where the official setup puts her (%LOCALAPPDATA%\Capri\Capri.exe), so she counts as
// installed, starts with Windows and keeps her memories and models in the same folder
internal static class Program
{
	internal const string Title = "Capri (mod)";

	internal const string UninstallKey = "Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\Capri.Desktop";

	internal static string Folder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Capri");

	internal static string Exe => Path.Combine(Folder, "Capri.exe");

	internal static string Version => Application.ProductVersion.Split('+')[0];

	internal static string StartMenuLink => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "Capri.lnk");

	internal static string DesktopLink => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "Capri.lnk");

	[STAThread]
	private static void Main()
	{
		Application.EnableVisualStyles();
		Application.SetCompatibleTextRenderingDefault(false);
#if UNINSTALLER
		Uninstall();
#else
		Application.Run(new SetupForm());
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
			using RegistryKey run = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run", writable: true);
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
	internal static void Install(bool desktop, Action<string> say)
	{
		say("Fechando a Capri, se estiver aberta...");
		CloseCapri();
		Directory.CreateDirectory(Folder);
		say("Copiando a Capri...");
		Unpack("Capri.exe", Exe);
		Unpack("Uninstall.exe", Path.Combine(Folder, "Uninstall.exe"));
		File.WriteAllText(Path.Combine(Folder, ".capri-install"), "Capri desktop companion (mod " + Version + ")");
		say("Criando atalhos...");
		Link(StartMenuLink);
		if (desktop)
		{
			Link(DesktopLink);
		}
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
		// she adds herself to Windows startup on her first run, as the official build does
	}

	private static void Unpack(string name, string path)
	{
		string partial = path + ".new";
		using (Stream from = Assembly.GetExecutingAssembly().GetManifestResourceStream(name))
		using (FileStream to = File.Create(partial))
		{
			from.CopyTo(to);
		}
		if (File.Exists(path))
		{
			File.Delete(path);
		}
		File.Move(partial, path);
	}

	private static void Link(string path)
	{
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
#endif
}

#if !UNINSTALLER
internal sealed class SetupForm : Form
{
	private readonly Label note;

	private readonly CheckBox desktop;

	private readonly CheckBox open;

	private readonly Button go;

	private readonly ProgressBar bar;

	private bool finished;

	internal SetupForm()
	{
		Text = "Instalar " + Program.Title;
		Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
		FormBorderStyle = FormBorderStyle.FixedDialog;
		MaximizeBox = false;
		StartPosition = FormStartPosition.CenterScreen;
		AutoScaleMode = AutoScaleMode.Dpi;
		AutoScaleDimensions = new SizeF(96f, 96f);
		ClientSize = new Size(440, 230);
		Font = new Font("Segoe UI", 9.5f);
		Controls.Add(new Label
		{
			Text = "Capri — versão modificada " + Program.Version,
			Font = new Font("Segoe UI", 13f, FontStyle.Bold),
			AutoSize = true,
			Location = new Point(20, 18)
		});
		note = new Label
		{
			Text = "Ela vai ser instalada em:\n" + Program.Folder + "\n\nNa primeira vez ela baixa a voz e os ouvidos dela (alguns GB).",
			Location = new Point(20, 56),
			Size = new Size(400, 76)
		};
		Controls.Add(note);
		desktop = new CheckBox { Text = "Criar atalho na Área de Trabalho", Checked = true, AutoSize = true, Location = new Point(20, 136) };
		open = new CheckBox { Text = "Abrir a Capri no final", Checked = true, AutoSize = true, Location = new Point(20, 160) };
		Controls.Add(desktop);
		Controls.Add(open);
		bar = new ProgressBar { Style = ProgressBarStyle.Marquee, Location = new Point(20, 194), Size = new Size(280, 22), Visible = false };
		Controls.Add(bar);
		go = new Button { Text = "Instalar", Location = new Point(320, 190), Size = new Size(100, 30) };
		go.Click += (_, _) =>
		{
			if (finished)
			{
				Close();
			}
			else
			{
				Run();
			}
		};
		Controls.Add(go);
		AcceptButton = go;
	}

	private void Run()
	{
		go.Enabled = desktop.Enabled = open.Enabled = false;
		bar.Visible = true;
		bool wantDesktop = desktop.Checked;
		Thread worker = new Thread(() =>
		{
			string problem = null;
			try
			{
				Program.Install(wantDesktop, text => BeginInvoke((Action)(() => note.Text = text)));
			}
			catch (Exception ex)
			{
				problem = ex.Message;
			}
			BeginInvoke((Action)(() => Done(problem)));
		});
		worker.IsBackground = true;
		worker.SetApartmentState(ApartmentState.STA);
		worker.Start();
	}

	private void Done(string problem)
	{
		bar.Visible = false;
		if (problem != null)
		{
			note.Text = "Não deu para instalar:\n" + problem;
			go.Text = "Tentar de novo";
			go.Enabled = desktop.Enabled = open.Enabled = true;
			return;
		}
		note.Text = "Pronto! A Capri está instalada.\nPara remover: Configurações > Aplicativos > Capri (mod).";
		if (open.Checked)
		{
			try
			{
				Process.Start(new ProcessStartInfo(Program.Exe) { WorkingDirectory = Program.Folder, UseShellExecute = true });
			}
			catch (Exception)
			{
			}
		}
		finished = true;
		go.Text = "Fechar";
		go.Enabled = true;
	}
}
#endif
