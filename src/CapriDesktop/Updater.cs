using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;

namespace CapriDesktop;

internal sealed class Updater
{
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	private struct TrustFile
	{
		internal uint Size;

		[MarshalAs(UnmanagedType.LPWStr)]
		internal string Path;

		internal IntPtr File;

		internal IntPtr Subject;
	}

	private struct TrustData
	{
		internal uint Size;

		internal IntPtr Policy;

		internal IntPtr Sip;

		internal uint Ui;

		internal uint Revocation;

		internal uint Choice;

		internal IntPtr File;

		internal uint State;

		internal IntPtr StateData;

		internal IntPtr Url;

		internal uint Flags;

		internal uint Context;

		internal IntPtr Signatures;
	}

	private readonly string folder;

	private readonly string app;

	private readonly bool local;

	internal Func<Release> Latest = Companion.FetchLatestRelease;

	internal Func<string, string> Signer = SignedBy;

	internal Action<ProcessStartInfo> Run = (ProcessStartInfo command) =>
	{
		using Process process = Process.Start(command);
		if (process == null)
		{
			throw new IOException("The update installer did not start.");
		}
	};

	internal Updater(string data, string executable, bool smoke)
	{
		folder = Path.GetFullPath(Path.Combine(data, "updates"));
		app = executable;
		local = smoke;
		if (smoke)
		{
			Latest = () =>
			{
				throw new Exception("The update check needs a fake release.");
			};
			Run = (ProcessStartInfo param0) =>
			{
				throw new Exception("The update installer needs a fake process start.");
			};
		}
	}

	internal static ReleaseAsset Installer(Release release)
	{
		ReleaseAsset releaseAsset = null;
		ReleaseAsset releaseAsset2 = null;
		if (release == null || release.Assets == null)
		{
			return null;
		}
		ReleaseAsset[] assets = release.Assets;
		foreach (ReleaseAsset releaseAsset3 in assets)
		{
			if (releaseAsset3 != null && releaseAsset3.Name != null && releaseAsset3.Name.StartsWith("Capri-Setup", StringComparison.OrdinalIgnoreCase) && releaseAsset3.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) && releaseAsset3.Name.IndexOfAny(Path.GetInvalidFileNameChars()) < 0)
			{
				if (string.Equals(releaseAsset3.Name, "Capri-Setup-" + (release.Tag ?? "").TrimStart('v', 'V') + ".exe", StringComparison.OrdinalIgnoreCase))
				{
					return releaseAsset3;
				}
				if (string.Equals(releaseAsset3.Name, "Capri-Setup.exe", StringComparison.OrdinalIgnoreCase))
				{
					releaseAsset2 = releaseAsset3;
				}
				if (releaseAsset == null)
				{
					releaseAsset = releaseAsset3;
				}
			}
		}
		return releaseAsset2 ?? releaseAsset;
	}

	internal UpdateFile Download(Release release)
	{
		if (release == null || release.Draft || release.Prerelease || !Companion.ReleaseVersion(release.Tag, out var version))
		{
			throw new BrainException("This release can't be installed.");
		}
		ReleaseAsset releaseAsset = Installer(release);
		if (releaseAsset == null)
		{
			throw new BrainException("This release has no Capri installer.");
		}
		if (releaseAsset.Size <= 0 || releaseAsset.Size > 536870912)
		{
			throw new BrainException("The update size is invalid.");
		}
		Address(releaseAsset.Url);
		ReleaseAsset releaseAsset2 = null;
		ReleaseAsset[] assets = release.Assets;
		foreach (ReleaseAsset releaseAsset3 in assets)
		{
			if (releaseAsset3 != null && string.Equals(releaseAsset3.Name, "SHA256SUMS.txt", StringComparison.OrdinalIgnoreCase))
			{
				if (releaseAsset2 != null)
				{
					throw new BrainException("This release has more than one checksum file.");
				}
				releaseAsset2 = releaseAsset3;
			}
		}
		string hash = null;
		if (releaseAsset2 != null)
		{
			using MemoryStream memoryStream = new MemoryStream();
			Copy(releaseAsset2.Url, memoryStream, 1048576L, -1L);
			memoryStream.Position = 0L;
			using StreamReader streamReader = new StreamReader(memoryStream);
			hash = Checksum(streamReader.ReadToEnd(), releaseAsset.Name);
		}
		UpdateFile updateFile = new UpdateFile();
		updateFile.Version = ((version.Revision == 0) ? version.ToString(3) : version.ToString(4));
		updateFile.Hash = hash;
		updateFile.Size = releaseAsset.Size;
		UpdateFile updateFile2 = updateFile;
		updateFile2.Path = Path.Combine(folder, "Capri-Setup-" + updateFile2.Version + "-" + releaseAsset.Id + ".exe");
		Directory.CreateDirectory(folder);
		string text = updateFile2.Path + ".partial";
		if (File.Exists(text))
		{
			File.Delete(text);
		}
		if (File.Exists(updateFile2.Path))
		{
			Verify(updateFile2.Path, updateFile2, cached: true);
			return updateFile2;
		}
		try
		{
			using (FileStream target = new FileStream(text, FileMode.CreateNew, FileAccess.Write, FileShare.None))
			{
				Copy(releaseAsset.Url, target, releaseAsset.Size, releaseAsset.Size);
			}
			Verify(text, updateFile2);
			File.Move(text, updateFile2.Path);
			return updateFile2;
		}
		finally
		{
			if (File.Exists(text))
			{
				File.Delete(text);
			}
		}
	}

	internal static string Checksum(string text, string name)
	{
		string text2 = null;
		string[] array = text.Split('\n');
		foreach (string text3 in array)
		{
			Match match = Regex.Match(text3.TrimEnd('\r'), "^([a-fA-F0-9]{64})[ \\t]+\\*?(.+)$");
			if (match.Success && !(match.Groups[2].Value != name))
			{
				if (text2 != null)
				{
					throw new BrainException("The installer checksum is listed twice.");
				}
				text2 = match.Groups[1].Value;
			}
		}
		if (text2 == null)
		{
			throw new BrainException("The installer checksum is missing.");
		}
		return text2;
	}

	private Uri Address(string address)
	{
		if (!Uri.TryCreate(address, UriKind.Absolute, out var result) || result.UserInfo.Length != 0 || result.Fragment.Length != 0)
		{
			throw new BrainException("The update download address is invalid.");
		}
		if (local && result.IsLoopback && result.Scheme == Uri.UriSchemeHttp)
		{
			return result;
		}
		if (result.Scheme != Uri.UriSchemeHttps || !result.IsDefaultPort || result.Host != "github.com" || !result.AbsolutePath.StartsWith("/lachydotmcg/capri-desktop/releases/download/", StringComparison.Ordinal))
		{
			throw new BrainException("The update download address is invalid.");
		}
		return result;
	}

	private void Copy(string address, Stream target, long limit, long expected)
	{
		Uri uri = Address(address);
		ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
		Stopwatch stopwatch = Stopwatch.StartNew();
		for (int i = 0; i < 6; i++)
		{
			HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(uri);
			httpWebRequest.UserAgent = "Capri-Desktop";
			httpWebRequest.Timeout = 15000;
			httpWebRequest.ReadWriteTimeout = 15000;
			httpWebRequest.AllowAutoRedirect = false;
			using HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
			switch ((int)httpWebResponse.StatusCode)
			{
			case 301:
			case 302:
			case 303:
			case 307:
			case 308:
			{
				if (!Uri.TryCreate(uri, httpWebResponse.Headers["Location"], out var result) || result.UserInfo.Length != 0 || (result.Scheme != Uri.UriSchemeHttps && (!local || !result.IsLoopback || !(result.Scheme == Uri.UriSchemeHttp))))
				{
					throw new BrainException("The update download moved to an invalid address.");
				}
				uri = result;
				goto end_IL_0066;
			}
			case 200:
				if (httpWebResponse.ContentLength <= limit && (expected < 0 || httpWebResponse.ContentLength < 0 || httpWebResponse.ContentLength == expected))
				{
					break;
				}
				goto default;
			default:
				throw new BrainException("The update download is incomplete.");
			}
			using (Stream stream = httpWebResponse.GetResponseStream())
			{
				byte[] array = new byte[65536];
				long num = 0L;
				int num2;
				while ((num2 = stream.Read(array, 0, array.Length)) > 0)
				{
					num += num2;
					if (num > limit || stopwatch.Elapsed.TotalMinutes > 10.0)
					{
						throw new BrainException("The update download didn't finish.");
					}
					target.Write(array, 0, num2);
				}
				if ((expected >= 0 && num != expected) || (httpWebResponse.ContentLength >= 0 && num != httpWebResponse.ContentLength))
				{
					throw new BrainException("The update download is incomplete.");
				}
				return;
			}
			end_IL_0066:;
		}
		throw new BrainException("The update download moved too many times.");
	}

	private void Verify(string path, UpdateFile update, bool cached = false)
	{
		string text = null;
		using (FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
		{
			if (fileStream.Length != update.Size)
			{
				text = "The update download is incomplete.";
			}
			else if (update.Hash != null)
			{
				using SHA256 sHA = SHA256.Create();
				if (!string.Equals(BitConverter.ToString(sHA.ComputeHash(fileStream)).Replace("-", ""), update.Hash, StringComparison.OrdinalIgnoreCase))
				{
					text = "The update checksum doesn't match. It won't run.";
				}
			}
			if (text == null)
			{
				string text2 = Signer(app);
				if (text2 != null && !string.Equals(text2, Signer(path), StringComparison.Ordinal))
				{
					throw new BrainException("The update signer doesn't match Capri. It won't run.");
				}
				if (update.Hash == null && text2 == null)
				{
					throw new BrainException("This update has no checksum or matching signature. It won't run.");
				}
			}
		}
		if (text != null)
		{
			if (cached)
			{
				File.Delete(path);
			}
			throw new BrainException(text);
		}
	}

	internal void Install(UpdateFile update, int parent, long started)
	{
		if (update == null || !string.Equals(Path.GetDirectoryName(Path.GetFullPath(update.Path)), folder, StringComparison.OrdinalIgnoreCase) || !update.Path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) || parent <= 0 || started <= 0)
		{
			throw new BrainException("The update isn't ready to install.");
		}
		using (new FileStream(update.Path, FileMode.Open, FileAccess.Read, FileShare.Read))
		{
			Verify(update.Path, update);
			Run(new ProcessStartInfo(update.Path, "--update " + parent.ToString(CultureInfo.InvariantCulture) + " " + started.ToString(CultureInfo.InvariantCulture))
			{
				UseShellExecute = false,
				CreateNoWindow = true,
				WorkingDirectory = folder
			});
		}
	}

	internal static string SignedBy(string path)
	{
		TrustFile structure = new TrustFile
		{
			Size = (uint)Marshal.SizeOf(typeof(TrustFile)),
			Path = path
		};
		IntPtr intPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(TrustFile)));
		Marshal.StructureToPtr(structure, intPtr, fDeleteOld: false);
		TrustData data = new TrustData
		{
			Size = (uint)Marshal.SizeOf(typeof(TrustData)),
			Ui = 2u,
			Choice = 1u,
			File = intPtr,
			State = 1u,
			Flags = 128u
		};
		Guid action = new Guid("00AAC56B-CD44-11D0-8CC2-00C04FC295EE");
		try
		{
			switch (WinVerifyTrust(new IntPtr(-1), ref action, ref data))
			{
			case -2146762496:
				return null;
			default:
				throw new BrainException("Windows couldn't verify the update signature. It won't run.");
			case 0:
			{
				X509Certificate x509Certificate = X509Certificate.CreateFromSignedFile(path);
				try
				{
					using SHA256 sHA = SHA256.Create();
					return BitConverter.ToString(sHA.ComputeHash(x509Certificate.GetRawCertData()));
				}
				finally
				{
					x509Certificate.Reset();
				}
			}
			}
		}
		finally
		{
			data.State = 2u;
			WinVerifyTrust(new IntPtr(-1), ref action, ref data);
			Marshal.DestroyStructure(intPtr, typeof(TrustFile));
			Marshal.FreeHGlobal(intPtr);
		}
	}

	[DllImport("wintrust.dll", ExactSpelling = true)]
	private static extern int WinVerifyTrust(IntPtr window, ref Guid action, ref TrustData data);
}
