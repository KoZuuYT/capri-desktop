using System;
using System.Runtime.InteropServices;
using System.Security;
using Microsoft.Win32;

namespace CapriDesktop;

internal sealed class Machine
{
	[StructLayout(LayoutKind.Sequential)]
	private sealed class MemoryStatus
	{
		internal uint Length = 64u;

		internal uint Load;

		internal ulong TotalPhysical;

		internal ulong AvailablePhysical;

		internal ulong TotalPageFile;

		internal ulong AvailablePageFile;

		internal ulong TotalVirtual;

		internal ulong AvailableVirtual;

		internal ulong AvailableExtended;
	}

	internal long Ram;

	internal long Vram;

	internal string Gpu;

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern bool GlobalMemoryStatusEx([In][Out] MemoryStatus status);

	internal static Machine Read()
	{
		Machine machine = new Machine();
		MemoryStatus memoryStatus = new MemoryStatus();
		if (GlobalMemoryStatusEx(memoryStatus))
		{
			machine.Ram = (long)memoryStatus.TotalPhysical;
		}
		try
		{
			using RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Control\\Class\\{4d36e968-e325-11ce-bfc1-08002be10318}");
			if (registryKey != null)
			{
				string[] subKeyNames = registryKey.GetSubKeyNames();
				foreach (string text in subKeyNames)
				{
					if (text.Length != 4)
					{
						continue;
					}
					try
					{
						using RegistryKey registryKey2 = registryKey.OpenSubKey(text);
						if (registryKey2 != null)
						{
							long num = Bytes(registryKey2.GetValue("HardwareInformation.qwMemorySize"));
							if (num <= 0)
							{
								num = Bytes(registryKey2.GetValue("HardwareInformation.MemorySize"));
							}
							if (num > machine.Vram)
							{
								machine.Vram = num;
								machine.Gpu = registryKey2.GetValue("DriverDesc") as string;
							}
						}
					}
					catch (SecurityException)
					{
					}
					catch (UnauthorizedAccessException)
					{
					}
				}
			}
		}
		catch (SecurityException)
		{
		}
		catch (UnauthorizedAccessException)
		{
		}
		return machine;
	}

	private static long Bytes(object value)
	{
		if (value is long)
		{
			return (long)value;
		}
		if (value is int)
		{
			return (uint)(int)value;
		}
		byte[] array = value as byte[];
		if (array != null && array.Length >= 8)
		{
			return BitConverter.ToInt64(array, 0);
		}
		if (array != null && array.Length >= 4)
		{
			return BitConverter.ToUInt32(array, 0);
		}
		return 0L;
	}

	internal string Describe()
	{
		string text = Math.Round((double)Ram / 1073741824.0) + " GB RAM";
		if (string.IsNullOrEmpty(Gpu) || Vram < 1073741824)
		{
			return text;
		}
		return text + " · " + Gpu.Replace("NVIDIA GeForce ", "").Replace("AMD Radeon ", "Radeon ") + " (" + Math.Round((double)Vram / 1073741824.0) + " GB)";
	}
}
