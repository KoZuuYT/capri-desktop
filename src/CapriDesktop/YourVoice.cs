using System;
using System.Globalization;
using System.IO;
using System.Text;
using Windows.Media.MediaProperties;
using Windows.Media.Transcoding;

namespace CapriDesktop;

internal static class YourVoice
{
	internal const int Rate = 24000;

	internal const int Shortest = 5;

	internal const int Longest = 30;

	internal const string Using = "reference.wav";

	internal const string Kept = "your-voice.wav";

	internal const string Silent = "That clip is almost silent. Record yourself talking, closer to the mic, then pick it again.";

	internal const string Unreadable = "Windows couldn't read that clip. Pick a WAV, MP3 or M4A of you talking.";

	internal static bool Active(string folder)
	{
		return File.Exists(Path.Combine(folder, "reference.wav"));
	}

	internal static bool Saved(string folder)
	{
		return File.Exists(Path.Combine(folder, "your-voice.wav"));
	}

	internal static short[] Prepare(string path, out string problem)
	{
		problem = null;
		try
		{
			return Shape(Read(path), out problem);
		}
		catch (VoiceException ex)
		{
			problem = ex.Message;
			return null;
		}
	}

	internal static short[] Read(string path)
	{
		FileInfo fileInfo = new FileInfo(path);
		if (!fileInfo.Exists)
		{
			throw new VoiceException("That clip isn't there anymore. Pick it again.");
		}
		bool flag = fileInfo.Extension.Equals(".wav", StringComparison.OrdinalIgnoreCase);
		if (fileInfo.Length > (flag ? 67108864 : 16777216))
		{
			throw new VoiceException("That clip is too long. Pick " + 5 + " to " + 30 + " seconds of you talking.");
		}
		int rate = 0;
		float[] array = null;
		try
		{
			if (flag)
			{
				array = Wav(File.ReadAllBytes(path), out rate);
			}
		}
		catch (IOException)
		{
			throw new VoiceException("Couldn't open that clip. Close the app using it, then pick it again.");
		}
		catch (UnauthorizedAccessException)
		{
			throw new VoiceException("Windows won't let Capri open that clip. Copy it to your Documents and pick it there.");
		}
		if (array == null)
		{
			array = Wav(Decode(fileInfo.FullName), out rate);
		}
		if (array == null)
		{
			throw new VoiceException("Windows couldn't read that clip. Pick a WAV, MP3 or M4A of you talking.");
		}
		short[] array2 = new short[array.Length];
		for (int i = 0; i < array.Length; i++)
		{
			array2[i] = (short)Math.Max(-32768.0, Math.Min(32767.0, Math.Round(array[i] * 32767f)));
		}
		if (rate != 24000)
		{
			return VoiceFx.Resample(array2, (double)rate / 24000.0);
		}
		return array2;
	}

	internal static float[] Wav(byte[] wav, out int rate)
	{
		rate = 0;
		if (wav.Length < 12 || Encoding.ASCII.GetString(wav, 0, 4) != "RIFF" || Encoding.ASCII.GetString(wav, 8, 4) != "WAVE")
		{
			return null;
		}
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		int num4 = 12;
		while (num4 + 8 <= wav.Length)
		{
			string text = Encoding.ASCII.GetString(wav, num4, 4);
			int num5 = BitConverter.ToInt32(wav, num4 + 4);
			if (text == "fmt " && num5 >= 16 && num4 + 24 <= wav.Length)
			{
				num = BitConverter.ToUInt16(wav, num4 + 8);
				num2 = BitConverter.ToUInt16(wav, num4 + 10);
				rate = BitConverter.ToInt32(wav, num4 + 12);
				num3 = BitConverter.ToUInt16(wav, num4 + 22);
				if (num == 65534 && num5 >= 40 && num4 + 34 <= wav.Length)
				{
					num = BitConverter.ToUInt16(wav, num4 + 32);
				}
			}
			if (text == "data")
			{
				if ((num != 1 && num != 3) || num2 < 1 || rate < 4000 || (num == 1 && num3 != 8 && num3 != 16 && num3 != 24 && num3 != 32) || (num == 3 && num3 != 32))
				{
					return null;
				}
				int num6 = wav.Length - num4 - 8;
				int num7 = ((num5 > 0 && num5 <= num6) ? num5 : num6);
				int num8 = num3 / 8;
				int num9 = num7 / (num8 * num2);
				float[] array = new float[num9];
				for (int i = 0; i < num9; i++)
				{
					double num10 = 0.0;
					for (int j = 0; j < num2; j++)
					{
						int num11 = num4 + 8 + (i * num2 + j) * num8;
						num10 += ((num == 3) ? ((double)BitConverter.ToSingle(wav, num11)) : (num3 switch
						{
							24 => (double)(wav[num11] | (wav[num11 + 1] << 8) | ((sbyte)wav[num11 + 2] << 16)) / 8388608.0, 
							16 => (double)BitConverter.ToInt16(wav, num11) / 32768.0, 
							8 => (double)(wav[num11] - 128) / 128.0, 
							_ => (double)BitConverter.ToInt32(wav, num11) / 2147483648.0, 
						}));
					}
					array[i] = (float)Math.Max(-1.0, Math.Min(1.0, num10 / (double)num2));
				}
				return array;
			}
			long num12 = (long)num4 + 8L + num5 + (num5 & 1);
			if (num5 < 0 || num12 > wav.Length)
			{
				break;
			}
			num4 = (int)num12;
		}
		return null;
	}

	private static byte[] Decode(string path)
	{
		byte[] array;
		try
		{
			using FileStream input = File.OpenRead(path);
			array = Decode(input);
		}
		catch (Exception)
		{
			throw new VoiceException("Windows couldn't read that clip. Pick a WAV, MP3 or M4A of you talking.");
		}
		if (array == null)
		{
			throw new VoiceException("Windows couldn't read that clip. Pick a WAV, MP3 or M4A of you talking.");
		}
		return array;
	}

	internal static byte[] Decode(Stream input)
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		using MemoryStream memoryStream = new MemoryStream();
		MediaEncodingProfile val = MediaEncodingProfile.CreateWav((AudioEncodingQuality)1);
		val.Audio = AudioEncodingProperties.CreatePcm(24000u, 1u, 16u);
		PrepareTranscodeResult result = new MediaTranscoder().PrepareStreamTranscodeAsync(input.AsRandomAccessStream(), memoryStream.AsRandomAccessStream(), val).AsTask<PrepareTranscodeResult>().Result;
		if (!result.CanTranscode)
		{
			return null;
		}
		result.TranscodeAsync().AsTask<double>().Wait();
		return memoryStream.ToArray();
	}

	internal static short[] Shape(short[] clip, out string problem)
	{
		problem = null;
		int num = 240;
		int num2 = clip.Length / num;
		double[] array = new double[num2];
		double num3 = 0.0;
		for (int i = 0; i < num2; i++)
		{
			double num4 = 0.0;
			for (int j = i * num; j < (i + 1) * num; j++)
			{
				num4 += (double)clip[j] * (double)clip[j];
			}
			array[i] = Math.Sqrt(num4 / (double)num);
			num3 = Math.Max(num3, array[i]);
		}
		if (num3 < 400.0)
		{
			problem = "That clip is almost silent. Record yourself talking, closer to the mic, then pick it again.";
			return null;
		}
		double num5 = Math.Max(120.0, num3 * 0.06);
		int k = 0;
		int num6 = num2 - 1;
		for (; array[k] < num5; k++)
		{
		}
		while (array[num6] < num5)
		{
			num6--;
		}
		int num7 = Math.Max(0, (k - 15) * num);
		int num8 = Math.Min(clip.Length, (num6 + 16) * num);
		double num9 = (double)(num8 - num7) / 24000.0;
		string text = num9.ToString("0.#", CultureInfo.InvariantCulture) + " seconds";
		if (num9 < 5.0)
		{
			problem = "That clip has only " + text + " of talking. Pick " + 5 + " to " + 30 + " seconds of you talking.";
			return null;
		}
		if (num9 > 30.0)
		{
			problem = "That clip is " + text + " long. Pick " + 5 + " to " + 30 + " seconds of you talking, or trim it.";
			return null;
		}
		int num10 = 1;
		for (int l = num7; l < num8; l++)
		{
			num10 = Math.Max(num10, Math.Abs((int)clip[l]));
		}
		double num11 = Math.Min(8.0, 29000.0 / (double)num10);
		short[] array2 = new short[num8 - num7];
		for (int m = 0; m < array2.Length; m++)
		{
			array2[m] = (short)Math.Max(-32767.0, Math.Min(32767.0, Math.Round((double)clip[num7 + m] * num11)));
		}
		return array2;
	}

	internal static void Save(short[] clip, string folder)
	{
		Directory.CreateDirectory(folder);
		string text = Path.Combine(folder, "reference.wav");
		string text2 = text + ".new";
		File.WriteAllBytes(text2, Voice.Wave(clip, 24000));
		if (File.Exists(text))
		{
			File.Replace(text2, text, null);
		}
		else
		{
			File.Move(text2, text);
		}
		string path = Path.Combine(folder, "your-voice.wav");
		if (File.Exists(path))
		{
			File.Delete(path);
		}
	}

	internal static void Shelve(string folder)
	{
		Move(Path.Combine(folder, "reference.wav"), Path.Combine(folder, "your-voice.wav"));
	}

	internal static void Unshelve(string folder)
	{
		Move(Path.Combine(folder, "your-voice.wav"), Path.Combine(folder, "reference.wav"));
	}

	private static void Move(string from, string to)
	{
		if (File.Exists(from))
		{
			if (File.Exists(to))
			{
				File.Delete(to);
			}
			File.Move(from, to);
		}
	}
}
