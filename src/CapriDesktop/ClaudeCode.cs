using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace CapriDesktop;

internal static class ClaudeCode
{
	internal const string Key = "claude-code";

	internal const string Missing = "Can't find Claude Code on your PC. Install it from claude.com/claude-code, run claude once in a terminal and log in, then try again.";

	internal const string LoggedOut = "Claude Code isn't logged in. Open a terminal, run claude and log in with your Claude plan, then try again.";

	internal const string Limited = "Your Claude plan hit its usage limit for now. Pick NORMAL under BRAIN in Capri Mainframe until it resets.";

	internal const string Late = "Claude Code didn't answer in time. Try again, or pick sonnet or haiku under BRAIN in Capri Mainframe.";

	internal static readonly string[] Models = new string[3] { "opus", "sonnet", "haiku" };

	internal static string Effort = "low";

	internal static bool Named(string key)
	{
		string text = (key ?? "").Trim().ToLowerInvariant().Replace(' ', '-')
			.Replace('_', '-');
		if (!(text == "claude-code") && !(text == "claudecode"))
		{
			return text == "claude-code-cli";
		}
		return true;
	}

	internal static string Find()
	{
		List<string> list = new List<string>();
		bool flag = Path.DirectorySeparatorChar == '\\';
		string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
		string[] array = (flag ? new string[2] { "claude.exe", "claude.cmd" } : new string[1] { "claude" });
		if (!string.IsNullOrEmpty(folderPath))
		{
			list.Add(Path.Combine(folderPath, ".local", "bin"));
		}
		if (!string.IsNullOrEmpty(folderPath) && !flag)
		{
			list.Add(Path.Combine(folderPath, ".claude", "local"));
		}
		if (!string.IsNullOrEmpty(folderPath) && !flag)
		{
			list.Add(Path.Combine(folderPath, ".npm-global", "bin"));
		}
		string folderPath2 = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
		if (flag && !string.IsNullOrEmpty(folderPath2))
		{
			list.Add(Path.Combine(folderPath2, "npm"));
		}
		string[] array2 = (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator);
		foreach (string text in array2)
		{
			if (text.Trim().Length > 0)
			{
				list.Add(text.Trim().Trim('"'));
			}
		}
		if (!flag)
		{
			list.Add("/opt/homebrew/bin");
			list.Add("/usr/local/bin");
			list.Add("/usr/bin");
		}
		string[] array3 = array;
		foreach (string path in array3)
		{
			foreach (string item in list)
			{
				try
				{
					string text2 = Path.Combine(item, path);
					if (File.Exists(text2))
					{
						return text2;
					}
				}
				catch (ArgumentException)
				{
				}
			}
		}
		return null;
	}

	private static string Argument(string value)
	{
		if (value.Length > 0 && value.IndexOfAny(new char[3] { ' ', '\t', '"' }) < 0)
		{
			return value;
		}
		return "\"" + value.Replace("\\\"", "\\\\\"").Replace("\"", "\\\"") + "\"";
	}

	private static ProcessStartInfo Start(string program, string arguments)
	{
		ProcessStartInfo processStartInfo = ((!program.EndsWith(".cmd", StringComparison.OrdinalIgnoreCase)) ? new ProcessStartInfo(program, arguments) : new ProcessStartInfo(Environment.GetEnvironmentVariable("ComSpec") ?? "cmd.exe", "/d /s /c \"" + Argument(program) + " " + arguments + "\""));
		processStartInfo.UseShellExecute = false;
		processStartInfo.CreateNoWindow = true;
		processStartInfo.RedirectStandardInput = true;
		processStartInfo.RedirectStandardOutput = true;
		processStartInfo.RedirectStandardError = true;
		processStartInfo.StandardOutputEncoding = Encoding.UTF8;
		processStartInfo.StandardErrorEncoding = Encoding.UTF8;
		string text = Path.Combine(Path.GetTempPath(), "capri-claude");
		Directory.CreateDirectory(text);
		processStartInfo.WorkingDirectory = text;
		processStartInfo.EnvironmentVariables["CLAUDE_CODE_DISABLE_NONESSENTIAL_TRAFFIC"] = "1";
		processStartInfo.EnvironmentVariables.Remove("ANTHROPIC_API_KEY");
		processStartInfo.EnvironmentVariables.Remove("CLAUDECODE");
		return processStartInfo;
	}

	internal static string Problem()
	{
		string text = Find();
		if (text == null)
		{
			return "Can't find Claude Code on your PC. Install it from claude.com/claude-code, run claude once in a terminal and log in, then try again.";
		}
		string text2 = Run(text, "auth status --json", null, 20000, null, asking: false);
		ClaudeCodeAuth claudeCodeAuth = Read<ClaudeCodeAuth>(text2.Substring(Math.Max(0, text2.IndexOf('{'))));
		if (claudeCodeAuth == null || !claudeCodeAuth.LoggedIn)
		{
			return "Claude Code isn't logged in. Open a terminal, run claude and log in with your Claude plan, then try again.";
		}
		return null;
	}

	internal static string Ask(string model, string system, string content, int wait, Action<Process> started)
	{
		string text = Find();
		if (text == null)
		{
			throw new ClaudeCodeException("Can't find Claude Code on your PC. Install it from claude.com/claude-code, run claude once in a terminal and log in, then try again.");
		}
		string text2 = Path.Combine(Path.GetTempPath(), "capri-claude");
		Directory.CreateDirectory(text2);
		string text3 = Path.Combine(text2, Guid.NewGuid().ToString("N") + ".txt");
		File.WriteAllText(text3, system ?? "", new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
		try
		{
			string arguments = "-p --input-format stream-json --output-format stream-json --verbose --model " + Argument(model ?? "opus") + (string.IsNullOrEmpty(Effort) ? "" : (" --effort " + Argument(Effort))) + " --system-prompt-file " + Argument(text3) + " --tools \"\" --strict-mcp-config --setting-sources project --no-session-persistence --disable-slash-commands --permission-prompts none --no-chrome";
			string input = "{\"type\":\"user\",\"message\":{\"role\":\"user\",\"content\":" + content + "}}\n";
			string text4 = Run(text, arguments, input, wait, started, asking: true);
			ClaudeCodeLine claudeCodeLine = null;
			StringBuilder stringBuilder = new StringBuilder();
			string[] array = text4.Split('\n');
			foreach (string text5 in array)
			{
				string text6 = text5.Trim();
				if (!text6.StartsWith("{", StringComparison.Ordinal) || text6.StartsWith("{\"type\":\"stream_event\"", StringComparison.Ordinal) || (text6.IndexOf("\"type\":\"result\"", StringComparison.Ordinal) < 0 && text6.IndexOf("\"type\":\"assistant\"", StringComparison.Ordinal) < 0))
				{
					continue;
				}
				ClaudeCodeLine claudeCodeLine2 = Read<ClaudeCodeLine>(text6);
				if (claudeCodeLine2 == null)
				{
					continue;
				}
				if (claudeCodeLine2.Type == "result")
				{
					claudeCodeLine = claudeCodeLine2;
				}
				else
				{
					if (!(claudeCodeLine2.Type == "assistant") || claudeCodeLine2.Message == null || claudeCodeLine2.Message.Content == null)
					{
						continue;
					}
					ClaudeCodeBlock[] content2 = claudeCodeLine2.Message.Content;
					foreach (ClaudeCodeBlock claudeCodeBlock in content2)
					{
						if (claudeCodeBlock != null && claudeCodeBlock.Type == "text" && !string.IsNullOrEmpty(claudeCodeBlock.Text))
						{
							stringBuilder.Append(claudeCodeBlock.Text).Append(' ');
						}
					}
				}
			}
			if (claudeCodeLine != null && (claudeCodeLine.Failed || (claudeCodeLine.Subtype != null && claudeCodeLine.Subtype != "success")))
			{
				throw new ClaudeCodeException(Explain(claudeCodeLine.Result ?? claudeCodeLine.Subtype));
			}
			if (claudeCodeLine != null && !string.IsNullOrEmpty(claudeCodeLine.Result))
			{
				return claudeCodeLine.Result;
			}
			if (stringBuilder.Length > 0)
			{
				return stringBuilder.ToString().Trim();
			}
			string text7 = Path.Combine(text2, "last-output.txt");
			try
			{
				File.WriteAllText(text7, text4, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
			}
			catch (IOException)
			{
			}
			throw new ClaudeCodeException("Claude Code answered in a way I couldn't read. What it sent is in " + text7);
		}
		finally
		{
			try
			{
				File.Delete(text3);
			}
			catch (IOException)
			{
			}
			catch (UnauthorizedAccessException)
			{
			}
		}
	}

	internal static string Explain(string said)
	{
		string text = (said ?? "").ToLowerInvariant();
		if (text.Contains("not logged in") || text.Contains("/login") || text.Contains("invalid api key") || text.Contains("oauth") || text.Contains("authentication"))
		{
			return "Claude Code isn't logged in. Open a terminal, run claude and log in with your Claude plan, then try again.";
		}
		if (text.Contains("usage limit") || text.Contains("limit reached") || text.Contains("rate limit") || text.Contains("out of extra usage"))
		{
			return "Your Claude plan hit its usage limit for now. Pick NORMAL under BRAIN in Capri Mainframe until it resets.";
		}
		if (text.Contains("model") && (text.Contains("not found") || text.Contains("not available") || text.Contains("invalid")))
		{
			return "Your Claude plan can't use that model. Pick another one under BRAIN in Capri Mainframe.";
		}
		string text2 = (said ?? "").Trim();
		if (text2.Length > 160)
		{
			text2 = text2.Substring(0, 157) + "...";
		}
		if (text2.Length != 0)
		{
			return "Claude Code: " + text2;
		}
		return "Claude Code sent nothing back. Try again.";
	}

	private static string Run(string program, string arguments, string input, int wait, Action<Process> started, bool asking)
	{
		Process process = new Process();
		process.StartInfo = Start(program, arguments);
		Process process2 = process;
		StringBuilder output = new StringBuilder();
		StringBuilder errors = new StringBuilder();
		process2.OutputDataReceived += (object sender, DataReceivedEventArgs e) =>
		{
			if (e.Data != null)
			{
				lock (output)
				{
					output.Append(e.Data).Append('\n');
				}
			}
		};
		process2.ErrorDataReceived += (object sender, DataReceivedEventArgs e) =>
		{
			if (e.Data != null)
			{
				lock (errors)
				{
					errors.Append(e.Data).Append('\n');
				}
			}
		};
		try
		{
			process2.Start();
		}
		catch (Win32Exception)
		{
			throw new ClaudeCodeException("Can't find Claude Code on your PC. Install it from claude.com/claude-code, run claude once in a terminal and log in, then try again.");
		}
		try
		{
			started?.Invoke(process2);
			process2.BeginOutputReadLine();
			process2.BeginErrorReadLine();
			if (input != null)
			{
				byte[] bytes = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetBytes(input);
				process2.StandardInput.BaseStream.Write(bytes, 0, bytes.Length);
				process2.StandardInput.BaseStream.Flush();
			}
			process2.StandardInput.Close();
			if (!process2.WaitForExit(wait))
			{
				Stop(process2);
				throw new ClaudeCodeException(asking ? "Claude Code didn't answer in time. Try again, or pick sonnet or haiku under BRAIN in Capri Mainframe." : "Can't find Claude Code on your PC. Install it from claude.com/claude-code, run claude once in a terminal and log in, then try again.");
			}
			process2.WaitForExit();
			string text;
			lock (output)
			{
				text = output.ToString();
			}
			if (text.Trim().Length == 0)
			{
				string text2;
				lock (errors)
				{
					text2 = errors.ToString();
				}
				if (asking || text2.Trim().Length > 0)
				{
					throw new ClaudeCodeException(Explain(text2));
				}
			}
			return text;
		}
		finally
		{
			process2.Dispose();
		}
	}

	internal static void Stop(Process process)
	{
		try
		{
			if (process.HasExited)
			{
				return;
			}
			ProcessStartInfo processStartInfo = new ProcessStartInfo("taskkill", "/T /F /PID " + process.Id);
			processStartInfo.UseShellExecute = false;
			processStartInfo.CreateNoWindow = true;
			using Process process2 = Process.Start(processStartInfo);
			if (!process2.WaitForExit(5000) && !process.HasExited)
			{
				process.Kill();
			}
		}
		catch (InvalidOperationException)
		{
		}
		catch (Win32Exception)
		{
		}
		catch (NotSupportedException)
		{
		}
	}

	private static T Read<T>(string json) where T : class
	{
		try
		{
			using MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
			return (T)new DataContractJsonSerializer(typeof(T)).ReadObject(stream);
		}
		catch (SerializationException)
		{
			return null;
		}
	}
}
