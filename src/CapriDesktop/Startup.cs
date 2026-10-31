using System;
using System.IO;
using System.Security;
using Microsoft.Win32;

namespace CapriDesktop;

internal static class Startup
{
	internal const string Name = "Capri";

	internal static string RunKey = "Software\\Microsoft\\Windows\\CurrentVersion\\Run";

	internal static string ApprovedKey = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\StartupApproved\\Run";

	internal static bool On(string exe)
	{
		try
		{
			using (RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(RunKey))
			{
				if (registryKey == null || !Starts(registryKey.GetValue("Capri") as string, exe))
				{
					return false;
				}
			}
			using RegistryKey registryKey2 = Registry.CurrentUser.OpenSubKey(ApprovedKey);
			byte[] array = ((registryKey2 == null) ? null : (registryKey2.GetValue("Capri") as byte[]));
			return array == null || array.Length == 0 || (array[0] & 1) == 0;
		}
		catch (UnauthorizedAccessException)
		{
			return false;
		}
		catch (SecurityException)
		{
			return false;
		}
		catch (IOException)
		{
			return false;
		}
	}

	internal static bool Set(string exe, bool on)
	{
		try
		{
			using (RegistryKey registryKey = Registry.CurrentUser.CreateSubKey(RunKey))
			{
				if (on)
				{
					registryKey.SetValue("Capri", "\"" + Path.GetFullPath(exe) + "\"");
				}
				else
				{
					registryKey.DeleteValue("Capri", throwOnMissingValue: false);
				}
			}
			using (RegistryKey registryKey2 = Registry.CurrentUser.OpenSubKey(ApprovedKey, writable: true))
			{
				registryKey2?.DeleteValue("Capri", throwOnMissingValue: false);
			}
			return true;
		}
		catch (UnauthorizedAccessException)
		{
			return false;
		}
		catch (SecurityException)
		{
			return false;
		}
		catch (IOException)
		{
			return false;
		}
	}

	internal static void Clear(string exe)
	{
		string command = null;
		try
		{
			using RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(RunKey);
			if (registryKey != null)
			{
				command = registryKey.GetValue("Capri") as string;
			}
		}
		catch (UnauthorizedAccessException)
		{
			return;
		}
		catch (SecurityException)
		{
			return;
		}
		catch (IOException)
		{
			return;
		}
		if (Starts(command, exe))
		{
			Set(exe, on: false);
		}
	}

	internal static bool Starts(string command, string exe)
	{
		if (string.IsNullOrWhiteSpace(command))
		{
			return false;
		}
		string text = command.Trim();
		if (text.StartsWith("\"", StringComparison.Ordinal))
		{
			int num = text.IndexOf('"', 1);
			text = ((num > 1) ? text.Substring(1, num - 1) : text.Trim('"'));
		}
		try
		{
			return string.Equals(Path.GetFullPath(text), Path.GetFullPath(exe), StringComparison.OrdinalIgnoreCase);
		}
		catch (ArgumentException)
		{
			return false;
		}
		catch (NotSupportedException)
		{
			return false;
		}
		catch (IOException)
		{
			return false;
		}
	}
}
