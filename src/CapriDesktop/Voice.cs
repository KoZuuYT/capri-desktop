using System;
using System.Collections.Generic;
using System.IO;
using System.Media;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace CapriDesktop;

internal sealed class Voice
{
	internal const string Rejected = "ElevenLabs rejected the key. Press Forget, paste a new one.";

	internal const string RejectedNew = "ElevenLabs rejected that key. Copy it again from ElevenLabs and paste it.";

	internal const string Restricted = "ElevenLabs has restricted your account, so her voice can't connect. Sign in at elevenlabs.io to see why, or pick PREBUILT LOCAL in the Mainframe.";

	internal const string Silent = "That reply had nothing to say out loud.";

	internal const string LibraryVoice = "ElevenLabs only lets paid plans use Voice Library voices in apps. Pick a voice you made or a default one.";

	internal const string RestrictHint = " Still failing? Make a new key with Restrict key turned off.";

	internal const string Refused = "ElevenLabs turned her voice away (error 403) without saying why. A VPN or proxy can do that. Still failing? Make a new key with Restrict key turned off.";

	internal const string Unreadable = "ElevenLabs sent a reply Capri couldn't read. Right-click her, Help, Copy error details, and send it in the Discord.";

	internal static string Host = "https://api.elevenlabs.io";

	private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("Capri.ElevenLabs");

	internal volatile string Key;

	internal volatile string VoiceId;

	internal volatile string Model;

	private SoundPlayer player;

	internal static volatile int Loudness = 100;

	private static readonly string[,] Respelled = new string[4, 2]
	{
		{ "chooms", "chewms" },
		{ "choom", "chewm" },
		{ "preem", "preme" },
		{ "yezzirrr", "yessirrr" }
	};

	private volatile Tuple<string, byte[]> lastSaid;

	internal bool Ready
	{
		get
		{
			if (Key != null)
			{
				return VoiceId != null;
			}
			return false;
		}
	}

	internal static short[] Louder(short[] samples)
	{
		int loudness = Loudness;
		if (loudness == 100)
		{
			return samples;
		}
		short[] array = new short[samples.Length];
		for (int i = 0; i < samples.Length; i++)
		{
			array[i] = (short)Math.Max(-32768, Math.Min(32767, samples[i] * loudness / 100));
		}
		return array;
	}

	internal static byte[] Louder(byte[] wav)
	{
		if (Loudness == 100)
		{
			return wav;
		}
		short[] array = Samples(wav, out var rate);
		if (array.Length != 0)
		{
			return Wave(Louder(array), rate);
		}
		return wav;
	}

	internal static string Protect(string key)
	{
		return Convert.ToBase64String(ProtectedData.Protect(Encoding.UTF8.GetBytes(key), Entropy, DataProtectionScope.CurrentUser));
	}

	internal static string Unprotect(string saved)
	{
		if (string.IsNullOrEmpty(saved))
		{
			return null;
		}
		try
		{
			return Encoding.UTF8.GetString(ProtectedData.Unprotect(Convert.FromBase64String(saved), Entropy, DataProtectionScope.CurrentUser));
		}
		catch (CryptographicException)
		{
			return null;
		}
		catch (FormatException)
		{
			return null;
		}
	}

	internal static string Tail(string key)
	{
		if (key != null && key.Length >= 4)
		{
			return key.Substring(key.Length - 4);
		}
		return "";
	}

	internal static string Spoken(string said)
	{
		string text = Regex.Replace(Regex.Replace(said ?? "", "\\[[^\\]]*\\]", " ").Replace("*", ""), "\\s+", " ").Trim();
		if (!Regex.IsMatch(text, "[\\p{L}\\p{Nd}]"))
		{
			return "";
		}
		return text;
	}

	internal static string Sayable(string text)
	{
		return Swap(Plain(text), 0, 1);
	}

	private static string Plain(string text)
	{
		text = Regex.Replace(text ?? "", "https?://(?:www\\.)?([^/\\s]+?)(?:/\\S*?)?(?=[.,!?)]*(?:\\s|$))", (Match link) => link.Groups[1].Value.Replace(".", " dot "));
		text = Regex.Replace(text, "[\\uD800-\\uDBFF][\\uDC00-\\uDFFF]|[☀-➿\ufe0f\u200d]", "");
		text = Regex.Replace(text, "\\s*&\\s*", " and ");
		text = Regex.Replace(text, "(\\d)\\s*%", "$1 percent");
		text = Regex.Replace(text, "\\bw/(?=\\s)", "with");
		text = Regex.Replace(text, "\\s*(->|=>|~+)\\s*", " ");
		return Regex.Replace(text, "\\s+", " ").Trim();
	}

	internal static string Unsaid(string text)
	{
		return Swap(text, 1, 0);
	}

	private static string Swap(string text, int from, int to)
	{
		text = text ?? "";
		for (int i = 0; i < Respelled.GetLength(0); i++)
		{
			string target = Respelled[i, to];
			text = Regex.Replace(text, "\\b" + Respelled[i, from] + "\\b", (Match found) =>
			{
				string value = found.Value;
				if (value.ToUpperInvariant() == value)
				{
					return target.ToUpperInvariant();
				}
				return (!char.IsUpper(value[0])) ? target : (char.ToUpperInvariant(target[0]) + target.Substring(1));
			}, RegexOptions.IgnoreCase);
		}
		return text;
	}

	internal static HttpWebRequest Open(string path, string key, string method)
	{
		ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
		HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(Host + path);
		httpWebRequest.Method = method;
		httpWebRequest.UserAgent = "Capri-Desktop";
		httpWebRequest.Headers["xi-api-key"] = key;
		httpWebRequest.ServicePoint.Expect100Continue = false;
		httpWebRequest.Timeout = 20000;
		httpWebRequest.ReadWriteTimeout = 20000;
		return httpWebRequest;
	}

	internal static byte[] Send(HttpWebRequest request, byte[] body)
	{
		try
		{
			if (body != null)
			{
				request.ContentType = "application/json";
				request.ContentLength = body.Length;
				using Stream stream = request.GetRequestStream();
				stream.Write(body, 0, body.Length);
			}
			using WebResponse webResponse = request.GetResponse();
			using Stream stream2 = webResponse.GetResponseStream();
			using MemoryStream memoryStream = new MemoryStream();
			stream2.CopyTo(memoryStream);
			return memoryStream.ToArray();
		}
		catch (WebException ex)
		{
			if (!(ex.Response is HttpWebResponse httpWebResponse))
			{
				throw new VoiceException("Can't reach ElevenLabs. Check your internet.");
			}
			int statusCode = (int)httpWebResponse.StatusCode;
			string text;
			using (httpWebResponse)
			{
				using StreamReader streamReader = new StreamReader(httpWebResponse.GetResponseStream());
				text = streamReader.ReadToEnd();
			}
			string text2 = Regex.Replace(text ?? "", "\\s+", " ").Trim();
			Brain.Log(request.RequestUri.GetLeftPart(UriPartial.Path), -1L, "HTTP " + statusCode + " " + ((text2.Length > 200) ? text2.Substring(0, 200) : text2));
			throw new VoiceException(Explain(statusCode, text), statusCode);
		}
		catch (IOException)
		{
			throw new VoiceException("Can't reach ElevenLabs. Check your internet.");
		}
	}

	internal static string Unsaved(string problem)
	{
		if (!(problem == "ElevenLabs rejected the key. Press Forget, paste a new one."))
		{
			return problem;
		}
		return "ElevenLabs rejected that key. Copy it again from ElevenLabs and paste it.";
	}

	internal static string Closed(string reason)
	{
		if (Regex.IsMatch(reason ?? "", "account_restricted|workspace is restricted", RegexOptions.IgnoreCase))
		{
			return "ElevenLabs has restricted your account, so her voice can't connect. Sign in at elevenlabs.io to see why, or pick PREBUILT LOCAL in the Mainframe.";
		}
		return Short("Voice link closed: " + reason);
	}

	internal static string Explain(int status, string detail)
	{
		detail = detail ?? "";
		if (detail.Contains("quota_exceeded"))
		{
			return "Out of ElevenLabs credits. Pick PREBUILT LOCAL in the Mainframe.";
		}
		if (detail.Contains("account_restricted"))
		{
			return "ElevenLabs has restricted your account, so her voice can't connect. Sign in at elevenlabs.io to see why, or pick PREBUILT LOCAL in the Mainframe.";
		}
		if (detail.Contains("missing_permissions"))
		{
			return "Key is missing permissions. Make a new ElevenLabs key with Restrict key turned off.";
		}
		if (detail.Contains("voice_not_found"))
		{
			return "Voice not found. Add it to your ElevenLabs voices.";
		}
		if (Regex.IsMatch(detail, "free_users_not_allowed|library voices?", RegexOptions.IgnoreCase))
		{
			return "ElevenLabs only lets paid plans use Voice Library voices in apps. Pick a voice you made or a default one.";
		}
		if (Regex.IsMatch(detail, "fine.?tun", RegexOptions.IgnoreCase))
		{
			return "That voice is still training on ElevenLabs. Pick another until it's ready.";
		}
		if (detail.Contains("invalid_api_key"))
		{
			return "ElevenLabs rejected the key. Press Forget, paste a new one.";
		}
		Match match = Regex.Match(detail, "\"message\"\\s*:\\s*\"((?:[^\"\\\\]|\\\\.)*)\"");
		string text = (match.Success ? Regex.Unescape(match.Groups[1].Value).Trim() : "");
		if (Regex.IsMatch(text, "empty text|non-empty", RegexOptions.IgnoreCase))
		{
			return "That reply had nothing to say out loud.";
		}
		if (Regex.IsMatch(text, "permission|api.?key|unauthori[sz]ed", RegexOptions.IgnoreCase))
		{
			return "Key can't do that. Make a new ElevenLabs key with Restrict key turned off.";
		}
		if (status == 403 && text.Length == 0)
		{
			return "ElevenLabs turned her voice away (error 403) without saying why. A VPN or proxy can do that. Still failing? Make a new key with Restrict key turned off.";
		}
		if (text.Length > 0)
		{
			return Short(text) + ((status == 401 || status == 402 || status == 429 || Regex.IsMatch(text, "unusual activity|\\b(voices?|models?|plans?|subscription|upgrade)\\b", RegexOptions.IgnoreCase)) ? "" : " Still failing? Make a new key with Restrict key turned off.");
		}
		return status switch
		{
			401 => "ElevenLabs rejected the key. Press Forget, paste a new one.", 
			429 => "ElevenLabs is busy. Try again in a moment.", 
			_ => "ElevenLabs error " + status + ". Still failing? Make a new key with Restrict key turned off.", 
		};
	}

	internal static string Short(string text)
	{
		text = (text ?? "").Trim();
		if (text.Length <= 72)
		{
			return text;
		}
		return text.Substring(0, 69) + "...";
	}

	internal static T Read<T>(byte[] json)
	{
		using MemoryStream stream = new MemoryStream(json);
		return (T)new DataContractJsonSerializer(typeof(T)).ReadObject(stream);
	}

	internal static T Reply<T>(byte[] json, string path)
	{
		try
		{
			return Read<T>(json);
		}
		catch (SerializationException)
		{
			string text = Encoding.UTF8.GetString(json).Trim();
			Brain.Log(Host + path, -1L, "Unreadable " + typeof(T).Name + " reply: " + ((text.Length > 200) ? text.Substring(0, 200) : text));
			throw new VoiceException("ElevenLabs sent a reply Capri couldn't read. Right-click her, Help, Copy error details, and send it in the Discord.");
		}
	}

	internal static VoiceListing Fetch(string key)
	{
		VoiceListing voiceListing = new VoiceListing();
		string text = null;
		for (int i = 0; i < 10; i++)
		{
			string path = "/v2/voices?page_size=100" + ((text == null) ? "" : ("&next_page_token=" + Uri.EscapeDataString(text)));
			VoicePage voicePage = Reply<VoicePage>(Send(Open(path, key, "GET"), null), "/v2/voices");
			if (voicePage.Voices != null)
			{
				voiceListing.Voices.AddRange(voicePage.Voices);
			}
			if (!voicePage.More || string.IsNullOrEmpty(voicePage.Next))
			{
				break;
			}
			text = voicePage.Next;
		}
		try
		{
			foreach (VoiceModel item in Read<List<VoiceModel>>(Send(Open("/v1/models", key, "GET"), null)))
			{
				if (item.Speaks)
				{
					voiceListing.Models.Add(item);
				}
			}
		}
		catch (VoiceException ex)
		{
			if (ex.Status == 401)
			{
				throw;
			}
		}
		catch (SerializationException)
		{
		}
		voiceListing.Voices.Sort((VoiceEntry a, VoiceEntry b) =>
		{
			int num = (a.Category == "premade").CompareTo(b.Category == "premade");
			return (num == 0) ? string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase) : num;
		});
		return voiceListing;
	}

	internal byte[] Speak(string text)
	{
		string key = Key;
		string voiceId = VoiceId;
		string model = Model;
		if (key == null || voiceId == null)
		{
			throw new VoiceException("No voice picked.");
		}
		StringBuilder stringBuilder = new StringBuilder("{\"text\":");
		stringBuilder.Append(Quote(Sayable(text)));
		if (!string.IsNullOrEmpty(model))
		{
			stringBuilder.Append(",\"model_id\":").Append(Quote(model));
		}
		stringBuilder.Append('}');
		string text2 = key + "\n" + voiceId + "\n" + stringBuilder;
		Tuple<string, byte[]> tuple = lastSaid;
		if (tuple != null && tuple.Item1 == text2)
		{
			return tuple.Item2;
		}
		byte[] array = Clean(Send(Open("/v1/text-to-speech/" + Uri.EscapeDataString(voiceId) + "?output_format=wav_24000", key, "POST"), Encoding.UTF8.GetBytes(stringBuilder.ToString())));
		lastSaid = Tuple.Create(text2, array);
		return array;
	}

	internal static string Quote(string value)
	{
		StringBuilder stringBuilder = new StringBuilder("\"");
		foreach (char c in value)
		{
			if (c == '"' || c == '\\')
			{
				stringBuilder.Append('\\').Append(c);
			}
			else if (c < ' ')
			{
				StringBuilder stringBuilder2 = stringBuilder.Append("\\u");
				int num = c;
				stringBuilder2.Append(num.ToString("x4"));
			}
			else
			{
				stringBuilder.Append(c);
			}
		}
		return stringBuilder.Append('"').ToString();
	}

	internal static short[] Samples(byte[] wav, out int rate)
	{
		rate = 24000;
		int num = 12;
		while (num + 8 <= wav.Length)
		{
			string text = Encoding.ASCII.GetString(wav, num, 4);
			int num2 = BitConverter.ToInt32(wav, num + 4);
			if (text == "fmt " && num + 16 <= wav.Length)
			{
				rate = Math.Max(1000, BitConverter.ToInt32(wav, num + 12));
			}
			if (text == "data")
			{
				int num3 = wav.Length - num - 8;
				int num4 = ((num2 > 0 && num2 <= num3) ? num2 : num3) / 2;
				short[] array = new short[num4];
				Buffer.BlockCopy(wav, num + 8, array, 0, num4 * 2);
				return array;
			}
			long num5 = (long)num + 8L + num2 + (num2 & 1);
			if (num2 < 0 || num5 > wav.Length)
			{
				break;
			}
			num = (int)num5;
		}
		return new short[0];
	}

	internal static int[] Mouths(byte[] wav, double perSecond)
	{
		short[] array = Samples(wav, out var rate);
		int num = Math.Max(1, (int)((double)rate / perSecond));
		int num2 = array.Length / num;
		double[] array2 = new double[num2];
		for (int i = 0; i < num2; i++)
		{
			double num3 = 0.0;
			for (int j = i * num; j < (i + 1) * num; j++)
			{
				num3 += (double)array[j] * (double)array[j];
			}
			array2[i] = Math.Sqrt(num3 / (double)num) / 32768.0;
		}
		double num4 = 0.0;
		double[] array3 = array2;
		foreach (double val in array3)
		{
			num4 = Math.Max(num4, val);
		}
		double num5 = Math.Max(0.004, num4 * 0.08);
		List<double> talking = new List<double>();
		double[] array4 = array2;
		foreach (double num6 in array4)
		{
			if (num6 > num5)
			{
				talking.Add(num6);
			}
		}
		talking.Sort();
		Func<double, double> func = (double q) => (talking.Count != 0) ? talking[Math.Min(talking.Count - 1, (int)(q * (double)talking.Count))] : 1.0;
		double num7 = func(0.35);
		double num8 = func(0.7);
		double num9 = func(0.97);
		int[] array5 = new int[num2];
		for (int num10 = 0; num10 < num2; num10++)
		{
			double num11 = array2[num10];
			ref int reference = ref array5[num10];
			int num12;
			if (num11 <= num5)
			{
				num12 = -1;
			}
			else if (num11 < num7)
			{
				num12 = ((num10 % 3 != 0) ? 1 : 0);
			}
			else if (num11 < num8)
			{
				num12 = ((num10 % 2 != 1) ? 1 : 2);
			}
			else
			{
				num12 = ((num11 < num9) ? 3 : 4);
			}
			reference = num12;
		}
		return array5;
	}

	internal static double Seconds(byte[] wav)
	{
		int rate;
		return (double)Samples(wav, out rate).Length / (double)rate;
	}

	internal static byte[] Clean(byte[] wav)
	{
		short[] samples = Samples(wav, out var rate);
		return Wave(samples, rate);
	}

	internal static byte[] Wave(short[] samples, int rate)
	{
		using MemoryStream memoryStream = new MemoryStream();
		using BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
		binaryWriter.Write(Encoding.ASCII.GetBytes("RIFF"));
		binaryWriter.Write(36 + samples.Length * 2);
		binaryWriter.Write(Encoding.ASCII.GetBytes("WAVEfmt "));
		binaryWriter.Write(16);
		binaryWriter.Write((short)1);
		binaryWriter.Write((short)1);
		binaryWriter.Write(rate);
		binaryWriter.Write(rate * 2);
		binaryWriter.Write((short)2);
		binaryWriter.Write((short)16);
		binaryWriter.Write(Encoding.ASCII.GetBytes("data"));
		binaryWriter.Write(samples.Length * 2);
		byte[] array = new byte[samples.Length * 2];
		Buffer.BlockCopy(samples, 0, array, 0, array.Length);
		binaryWriter.Write(array);
		binaryWriter.Flush();
		return memoryStream.ToArray();
	}

	internal void Play(byte[] wav)
	{
		wav = Louder(wav);
		Stop();
		player = new SoundPlayer(new MemoryStream(wav));
		player.Play();
	}

	internal void Stop()
	{
		if (player != null)
		{
			player.Stop();
			player.Dispose();
			player = null;
		}
	}
}
