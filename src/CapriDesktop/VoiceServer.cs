using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;

namespace CapriDesktop;

internal sealed class VoiceServer
{
	internal sealed class Service
	{
		internal readonly string Name;

		internal readonly string Api;

		internal readonly string Url;

		internal readonly string Model;

		internal readonly string Speaker;

		internal readonly string VoiceCue;

		internal readonly string KeyCue;

		internal readonly string Hint;

		internal Service(string name, string api, string url, string model, string speaker, string voiceCue, string keyCue, string hint)
		{
			Name = name;
			Api = api;
			Url = url;
			Model = model;
			Speaker = speaker;
			VoiceCue = voiceCue;
			KeyCue = keyCue;
			Hint = hint;
		}
	}

	[DataContract]
	internal sealed class FishPage
	{
		[DataMember(Name = "items")]
		public List<FishModel> Items;

		[DataMember(Name = "has_more")]
		public bool More;
	}

	[DataContract]
	internal sealed class FishModel
	{
		[DataMember(Name = "_id")]
		public string Id;

		[DataMember(Name = "title")]
		public string Title;
	}

	[DataContract]
	internal sealed class FishMade
	{
		[DataMember(Name = "_id")]
		public string Id;
	}

	internal const string Style = "Your server";

	internal const string Menu = "Any voice model";

	internal const string Address = "http://localhost:8880/v1";

	internal const string Filling = " Her free voice is filling in.";

	internal const string OpenAi = "openai";

	internal const string Fish = "fish";

	internal const string Files = "file";

	internal const string FishCloud = "https://api.fish.audio";

	internal const string FishFree = "s2.1-pro-free";

	internal const string FishOldDefault = "Capri Default";

	internal const string OpenAiCloud = "https://api.openai.com/v1";

	internal const string Theirs = "Your voice server";

	internal static int Patience = 20000;

	internal volatile string Url = "http://localhost:8880/v1";

	internal volatile string Api = "openai";

	internal volatile string Model;

	internal volatile string Speaker;

	internal volatile string Key;

	internal readonly object Changing = new object();

	private volatile string noted;

	internal static readonly Service[] Services = new Service[6]
	{
		new Service("Fish Audio · cloud", "fish", "https://api.fish.audio", "s2.1-pro-free", "", "Voice ID or fish.audio link", "Paste your Fish Audio key", "Fish Audio's voices. Paste a key from fish.audio, then make your own voice with Fish Audio's Voice Design and paste its link or ID. Capri prohibits cloning or impersonating a living person's voice without explicit, informed consent or other valid authorization. s2.1-pro-free is its free tier."),
		new Service("Voice model file · on your PC", "file", "", "", "", "Number or clip, optional", "Optional", "A Piper, Kokoro, Kitten or Pocket TTS model for sherpa-onnx, run with no server. Browse to its .tar.bz2 or .onnx. Pocket TTS copies a voice clip you pick."),
		new Service("Fish Speech · on your PC", "fish", "http://127.0.0.1:8080", "", "", "Reference ID, optional", "Optional", "Fish Speech running on your PC with tools/api_server.py. Voice is a reference ID you added to it, or blank."),
		new Service("Kokoro · on your PC", "openai", "http://localhost:8880/v1", "kokoro", "af_heart", "Optional", "Optional", "Kokoro-FastAPI running on your PC, with voices like af_heart, af_bella and am_adam."),
		new Service("OpenAI · cloud", "openai", "https://api.openai.com/v1", "gpt-4o-mini-tts", "marin", "Optional", "Paste your OpenAI key", "OpenAI's voices with your OpenAI key: marin, cedar, coral, alloy and more."),
		new Service("Any OpenAI-style server", "openai", null, null, null, "Optional", "Optional", "Any speech model you run whose server speaks OpenAI's speech API, like Qwen3-TTS through vLLM-Omni, AllTalk or LocalAI.")
	};

	internal static string Known(string api)
	{
		if (!(api == "fish") && !(api == "file"))
		{
			return "openai";
		}
		return api;
	}

	internal static bool InCloud(string url, string api)
	{
		api = Known(api);
		if (!(api == "fish"))
		{
			if (api == "openai")
			{
				return Same(url, "openai", "https://api.openai.com/v1", "openai");
			}
			return false;
		}
		return Same(url, "fish", "https://api.fish.audio", "fish");
	}

	internal static string CloudName(string api)
	{
		if (!(Known(api) == "fish"))
		{
			return "OpenAI";
		}
		return "Fish Audio";
	}

	internal static Service Find(string api, string url)
	{
		Service result = Services[Services.Length - 1];
		Service[] services = Services;
		foreach (Service service in services)
		{
			if (!(service.Api != api))
			{
				if (service.Url != null && Same(service.Url, api, url, api))
				{
					return service;
				}
				result = service;
			}
		}
		return result;
	}

	internal static Uri Endpoint(string url)
	{
		return Endpoint(url, "openai");
	}

	internal static Uri Endpoint(string url, string api)
	{
		bool flag = api == "fish";
		string text = (flag ? "https://api.fish.audio" : "http://localhost:8880/v1");
		string text2 = (url ?? "").Trim();
		if (text2.Length == 0)
		{
			text2 = text;
		}
		if (!Regex.IsMatch(text2, "^[a-z][a-z0-9+.-]*://", RegexOptions.IgnoreCase))
		{
			text2 = ((Regex.IsMatch(text2, "^[^/:]+\\.[a-z]{2,}(/|$)", RegexOptions.IgnoreCase) && !Regex.IsMatch(text2, "^[^/:]+\\.(local|lan|home|internal)(/|$)", RegexOptions.IgnoreCase)) ? "https://" : "http://") + text2;
		}
		if (!Uri.TryCreate(text2, UriKind.Absolute, out var result) || (result.Scheme != Uri.UriSchemeHttp && result.Scheme != Uri.UriSchemeHttps) || result.Host.Length == 0)
		{
			throw new VoiceException("Your voice server's address doesn't work. Try " + text + ".");
		}
		string text3 = result.AbsolutePath.TrimEnd('/');
		if (flag)
		{
			if (!text3.EndsWith("/tts", StringComparison.OrdinalIgnoreCase))
			{
				text3 += (text3.EndsWith("/v1", StringComparison.OrdinalIgnoreCase) ? "/tts" : "/v1/tts");
			}
		}
		else
		{
			if (text3.Length == 0)
			{
				text3 = "/v1";
			}
			if (!text3.EndsWith("/audio/speech", StringComparison.OrdinalIgnoreCase))
			{
				text3 += "/audio/speech";
			}
		}
		UriBuilder uriBuilder = new UriBuilder(result);
		uriBuilder.Path = text3;
		return uriBuilder.Uri;
	}

	internal static string Where(string url)
	{
		return Where(url, "openai");
	}

	internal static string Where(string url, string api)
	{
		if (api == "file")
		{
			return ModelFile.Name(url);
		}
		try
		{
			return Where(Endpoint(url, api));
		}
		catch (VoiceException)
		{
			return (url ?? "").Trim();
		}
	}

	private static string Where(Uri at)
	{
		if (!at.IsDefaultPort)
		{
			return at.Host + ":" + at.Port;
		}
		return at.Host;
	}

	internal static bool Same(string url, string api, string other, string otherApi)
	{
		if (api == "file" || otherApi == "file")
		{
			return false;
		}
		try
		{
			Uri uri = Endpoint(url, api);
			Uri uri2 = Endpoint(other, otherApi);
			return uri.Scheme == uri2.Scheme && uri.Port == uri2.Port && string.Equals(uri.Host, uri2.Host, StringComparison.OrdinalIgnoreCase);
		}
		catch (VoiceException)
		{
			return false;
		}
	}

	internal static string Name(string url, string api)
	{
		if (api == "file")
		{
			return "Your voice model";
		}
		string text;
		try
		{
			text = Endpoint(url, api).Host.ToLowerInvariant();
		}
		catch (VoiceException)
		{
			return "Your voice server";
		}
		if (text == "fish.audio" || text.EndsWith(".fish.audio", StringComparison.Ordinal))
		{
			return "Fish Audio";
		}
		if (!(text == "api.openai.com"))
		{
			return "Your voice server";
		}
		return "OpenAI";
	}

	internal static string Label(string url, string api)
	{
		if (api == "file")
		{
			return ModelFile.Name(url);
		}
		string text = Name(url, api);
		if (!(text == "Your voice server"))
		{
			return text;
		}
		return "Your server";
	}

	internal static List<KeyValuePair<string, string>> FishVoices(string url, string key)
	{
		Uri uri = Endpoint(url, "fish");
		List<KeyValuePair<string, string>> list = new List<KeyValuePair<string, string>>();
		for (int i = 1; i <= 5; i++)
		{
			UriBuilder uriBuilder = new UriBuilder(uri.Scheme, uri.Host, uri.Port, "/model");
			uriBuilder.Query = "self=true&page_size=100&page_number=" + i;
			if (uri.Scheme == Uri.UriSchemeHttps)
			{
				ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
			}
			HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(uriBuilder.Uri);
			httpWebRequest.Method = "GET";
			httpWebRequest.UserAgent = "Capri-Desktop";
			if (uri.IsLoopback)
			{
				httpWebRequest.Proxy = null;
			}
			if (!string.IsNullOrEmpty(key))
			{
				httpWebRequest.Headers[HttpRequestHeader.Authorization] = "Bearer " + key;
			}
			httpWebRequest.Timeout = Patience;
			httpWebRequest.ReadWriteTimeout = Patience;
			FishPage fishPage;
			try
			{
				using HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
				using Stream stream = httpWebResponse.GetResponseStream();
				fishPage = (FishPage)new DataContractJsonSerializer(typeof(FishPage)).ReadObject(stream);
			}
			catch (WebException ex)
			{
				if (!(ex.Response is HttpWebResponse httpWebResponse2))
				{
					throw new VoiceException("Can't reach Fish Audio. Check your internet connection.");
				}
				int statusCode = (int)httpWebResponse2.StatusCode;
				httpWebResponse2.Close();
				if (statusCode == 401 || statusCode == 403)
				{
					throw new VoiceException(string.IsNullOrEmpty(key) ? "Paste your Fish Audio key first." : ("Fish Audio turned down its key (HTTP " + statusCode + ")."));
				}
				throw new VoiceException("Fish Audio said HTTP " + statusCode + ".");
			}
			catch (SerializationException)
			{
				throw new VoiceException("Fish Audio sent a voice list Capri can't read.");
			}
			catch (IOException)
			{
				throw new VoiceException("Can't reach Fish Audio. Check your internet connection.");
			}
			if (fishPage == null || fishPage.Items == null)
			{
				break;
			}
			foreach (FishModel item in fishPage.Items)
			{
				if (!string.IsNullOrEmpty(item.Id))
				{
					list.Add(new KeyValuePair<string, string>(item.Id, string.IsNullOrEmpty(item.Title) ? item.Id : item.Title));
				}
			}
			if (!fishPage.More || fishPage.Items.Count == 0)
			{
				break;
			}
		}
		return list;
	}

	internal static string MakeFishVoice(string url, string key, string clip)
	{
		if (string.IsNullOrEmpty(key))
		{
			throw new VoiceException("Paste your Fish Audio key first.");
		}
		FileInfo fileInfo = new FileInfo(clip);
		if (!fileInfo.Exists)
		{
			throw new VoiceException("That clip isn't there anymore. Pick it again.");
		}
		if (fileInfo.Length > 52428800)
		{
			throw new VoiceException("That clip is too big. Pick 10 to 30 seconds of talking.");
		}
		Uri uri = Endpoint(url, "fish");
		string boundary = "capri" + Guid.NewGuid().ToString("N");
		Func<string, string, string> func = (string name, string value) => "--" + boundary + "\r\nContent-Disposition: form-data; name=\"" + name + "\"\r\n\r\n" + value + "\r\n";
		byte[] bytes = Encoding.UTF8.GetBytes(func("type", "tts") + func("title", "Capri · " + Path.GetFileNameWithoutExtension(fileInfo.Name)) + func("train_mode", "fast") + func("visibility", "private") + "--" + boundary + "\r\nContent-Disposition: form-data; name=\"voices\"; filename=\"" + Regex.Replace(fileInfo.Name, "[\"\\r\\n]", "_") + "\"\r\nContent-Type: application/octet-stream\r\n\r\n");
		byte[] bytes2 = Encoding.UTF8.GetBytes("\r\n--" + boundary + "--\r\n");
		if (uri.Scheme == Uri.UriSchemeHttps)
		{
			ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
		}
		HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(new UriBuilder(uri.Scheme, uri.Host, uri.Port, "/model").Uri);
		httpWebRequest.Method = "POST";
		httpWebRequest.UserAgent = "Capri-Desktop";
		if (uri.IsLoopback)
		{
			httpWebRequest.Proxy = null;
		}
		httpWebRequest.Headers[HttpRequestHeader.Authorization] = "Bearer " + key;
		httpWebRequest.ContentType = "multipart/form-data; boundary=" + boundary;
		httpWebRequest.ContentLength = bytes.Length + fileInfo.Length + bytes2.Length;
		httpWebRequest.ServicePoint.Expect100Continue = false;
		httpWebRequest.Timeout = 120000;
		httpWebRequest.ReadWriteTimeout = 120000;
		try
		{
			using (Stream stream = httpWebRequest.GetRequestStream())
			{
				stream.Write(bytes, 0, bytes.Length);
				using (FileStream fileStream = fileInfo.OpenRead())
				{
					fileStream.CopyTo(stream);
				}
				stream.Write(bytes2, 0, bytes2.Length);
			}
			FishMade fishMade;
			using (HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse())
			{
				using Stream stream2 = httpWebResponse.GetResponseStream();
				fishMade = (FishMade)new DataContractJsonSerializer(typeof(FishMade)).ReadObject(stream2);
			}
			if (fishMade == null || string.IsNullOrEmpty(fishMade.Id))
			{
				throw new VoiceException("Fish Audio didn't say which voice it made. Look for it on fish.audio.");
			}
			return fishMade.Id;
		}
		catch (WebException ex)
		{
			if (!(ex.Response is HttpWebResponse httpWebResponse2))
			{
				throw new VoiceException("Can't reach Fish Audio. Check your internet connection.");
			}
			int statusCode = (int)httpWebResponse2.StatusCode;
			string text;
			using (httpWebResponse2)
			{
				using StreamReader streamReader = new StreamReader(httpWebResponse2.GetResponseStream());
				text = Said(streamReader.ReadToEnd());
			}
			if (statusCode == 401 || statusCode == 403)
			{
				throw new VoiceException("Fish Audio turned down its key (HTTP " + statusCode + ").");
			}
			throw new VoiceException("Fish Audio couldn't make that voice (HTTP " + statusCode + ")" + ((text.Length > 0) ? (": " + Voice.Short(text)) : "."));
		}
		catch (SerializationException)
		{
			throw new VoiceException("Fish Audio didn't say which voice it made. Look for it on fish.audio.");
		}
		catch (IOException)
		{
			throw new VoiceException("Can't reach Fish Audio. Check your internet connection.");
		}
	}

	internal static string FishVoice(string typed)
	{
		string text = (typed ?? "").Trim();
		Match match = Regex.Match(text, "fish\\.audio/\\S*?([0-9a-f]{32})", RegexOptions.IgnoreCase);
		if (match.Success)
		{
			return match.Groups[1].Value.ToLowerInvariant();
		}
		if (!string.Equals(text, "Capri Default", StringComparison.OrdinalIgnoreCase))
		{
			return text;
		}
		return "";
	}

	private static string Slow(string name)
	{
		return name + " took over " + ((double)Patience / 1000.0).ToString("0.#", CultureInfo.InvariantCulture) + " s to answer.";
	}

	internal byte[] Speak(string text)
	{
		string url;
		string api;
		string model;
		string speaker;
		string key;
		lock (Changing)
		{
			url = Url;
			api = Api;
			model = Model;
			speaker = Speaker;
			key = Key;
		}
		bool flag = api == "fish";
		string text2 = Name(url, api);
		int pitch = VoiceFx.Pitch;
		int speed = VoiceFx.Speed;
		int serverRobot = VoiceFx.ServerRobot;
		int serverGlitch = VoiceFx.ServerGlitch;
		VoiceFx.Layering layers = VoiceFx.Layers;
		List<short> list = new List<short>();
		if (api == "file")
		{
			short[] array = ModelFile.Speak(ModelFile.Clean(url), model, speaker, Voice.Sayable(text), out var rate);
			list.AddRange((rate == 24000 || array.Length == 0) ? array : VoiceFx.Resample(array, (double)rate / 24000.0));
		}
		Uri endpoint = ((api == "file") ? null : Endpoint(url, api));
		foreach (string item in (api == "file") ? new List<string>() : QwenVoice.Pieces(Voice.Sayable(text)))
		{
			StringBuilder stringBuilder = new StringBuilder("{");
			if (flag)
			{
				stringBuilder.Append("\"text\":").Append(Voice.Quote(item));
				string text3 = FishVoice(speaker);
				if (text3 != "")
				{
					stringBuilder.Append(",\"reference_id\":").Append(Voice.Quote(text3));
				}
				stringBuilder.Append(",\"format\":\"wav\",\"sample_rate\":").Append(24000).Append('}');
			}
			else
			{
				if (!string.IsNullOrEmpty(model))
				{
					stringBuilder.Append("\"model\":").Append(Voice.Quote(model)).Append(',');
				}
				stringBuilder.Append("\"input\":").Append(Voice.Quote(item));
				if (!string.IsNullOrEmpty(speaker))
				{
					stringBuilder.Append(",\"voice\":").Append(Voice.Quote(speaker));
				}
				stringBuilder.Append(",\"response_format\":\"wav\"}");
			}
			list.AddRange(Ask(endpoint, key, flag ? model : null, stringBuilder.ToString(), text2));
		}
		short[] array2 = list.ToArray();
		if (array2.Length == 0)
		{
			throw new VoiceException(text2 + " sent back no sound.");
		}
		if (!VoiceFx.Plain(pitch, speed, serverRobot, serverGlitch, layers))
		{
			array2 = VoiceFx.Restyled(array2, 24000, pitch, speed, serverRobot, serverGlitch, VoiceFx.Seed(text), layers);
		}
		return Voice.Wave(array2, 24000);
	}

	private static short[] Ask(Uri endpoint, string key, string model, string json, string name)
	{
		byte[] bytes = Encoding.UTF8.GetBytes(json);
		if (endpoint.Scheme == Uri.UriSchemeHttps)
		{
			ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
		}
		HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(endpoint);
		httpWebRequest.Method = "POST";
		if (endpoint.IsLoopback)
		{
			httpWebRequest.Proxy = null;
		}
		httpWebRequest.UserAgent = "Capri-Desktop";
		httpWebRequest.ContentType = "application/json";
		httpWebRequest.ContentLength = bytes.Length;
		if (!string.IsNullOrEmpty(key))
		{
			httpWebRequest.Headers[HttpRequestHeader.Authorization] = "Bearer " + key;
		}
		if (!string.IsNullOrEmpty(model))
		{
			httpWebRequest.Headers["model"] = model;
		}
		httpWebRequest.ServicePoint.Expect100Continue = false;
		httpWebRequest.Timeout = Patience;
		httpWebRequest.ReadWriteTimeout = Patience;
		bool flag = name == "Your voice server";
		try
		{
			using (Stream stream = httpWebRequest.GetRequestStream())
			{
				stream.Write(bytes, 0, bytes.Length);
			}
			using HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
			using Stream stream2 = httpWebResponse.GetResponseStream();
			using MemoryStream memoryStream = new MemoryStream();
			stream2.CopyTo(memoryStream);
			return Sound(memoryStream.ToArray(), httpWebResponse.ContentType, name);
		}
		catch (WebException ex)
		{
			if (!(ex.Response is HttpWebResponse httpWebResponse2))
			{
				if (ex.Status == WebExceptionStatus.Timeout)
				{
					throw new VoiceException(Slow(name));
				}
				if (ex.Status == WebExceptionStatus.ConnectFailure || ex.Status == WebExceptionStatus.NameResolutionFailure)
				{
					throw new VoiceException(flag ? ("Can't reach your voice server at " + Where(endpoint) + ". Is it running?") : ("Can't reach " + name + ". Check your internet connection."));
				}
				throw new VoiceException(name + " at " + Where(endpoint) + " stopped answering.");
			}
			int statusCode = (int)httpWebResponse2.StatusCode;
			string text;
			using (httpWebResponse2)
			{
				using StreamReader streamReader = new StreamReader(httpWebResponse2.GetResponseStream());
				text = Said(streamReader.ReadToEnd());
			}
			if (statusCode == 404 && name == "Fish Audio")
			{
				throw new VoiceException("Fish Audio can't find that voice (HTTP 404). Check its ID or link on fish.audio.");
			}
			switch (statusCode)
			{
			case 404:
				throw new VoiceException(name + " has no " + endpoint.AbsolutePath + " (HTTP 404). Check its address.");
			case 429:
				throw new VoiceException(name + " is busy (HTTP 429). Try again in a moment.");
			case 401:
			case 403:
				throw new VoiceException(string.IsNullOrEmpty(key) ? (name + " wants a key (HTTP " + statusCode + "). Add it under " + (flag ? "CUSTOM LOCAL > Set up" : "CLOUD VOICE") + ".") : (name + " turned down its key (HTTP " + statusCode + ")."));
			case 402:
				if (name == "Fish Audio" && model != "s2.1-pro-free")
				{
					throw new VoiceException("Fish Audio wants credit for " + (string.IsNullOrEmpty(model) ? "its usual model" : model) + " (HTTP 402). For its free tier, set Model to s2.1-pro-free.");
				}
				break;
			}
			throw new VoiceException(name + " said HTTP " + statusCode + ((text.Length > 0) ? (": " + Voice.Short(text)) : "."));
		}
		catch (IOException)
		{
			throw new VoiceException(name + " at " + Where(endpoint) + " stopped answering.");
		}
	}

	private static short[] Sound(byte[] reply, string type, string name)
	{
		if (reply.Length == 0)
		{
			throw new VoiceException(name + " sent back no sound.");
		}
		float[] array = YourVoice.Wav(reply, out var rate);
		if (array == null && Regex.IsMatch(type ?? "", "json|text|html", RegexOptions.IgnoreCase))
		{
			throw new VoiceException(name + " sent words, not sound: " + Voice.Short(Said(Encoding.UTF8.GetString(reply))));
		}
		if (array == null)
		{
			byte[] array2 = null;
			try
			{
				using MemoryStream input = new MemoryStream(reply);
				array2 = YourVoice.Decode(input);
			}
			catch (Exception)
			{
			}
			if (array2 != null)
			{
				array = YourVoice.Wav(array2, out rate);
			}
		}
		if (array == null)
		{
			throw new VoiceException("Capri couldn't read the sound " + ((name == "Your voice server") ? "your voice server" : name) + " sent.");
		}
		short[] array3 = new short[array.Length];
		for (int i = 0; i < array.Length; i++)
		{
			array3[i] = (short)Math.Max(-32768.0, Math.Min(32767.0, Math.Round(array[i] * 32767f)));
		}
		if (rate != 24000)
		{
			return VoiceFx.Resample(array3, (double)rate / 24000.0);
		}
		return array3;
	}

	private static string Said(string reply)
	{
		string text = reply ?? "";
		Match match = Regex.Match(text, "\"(?:message|detail|msg)\"\\s*:\\s*\"((?:[^\"\\\\]|\\\\.)*)\"");
		if (match.Success)
		{
			text = match.Groups[1].Value;
			try
			{
				text = Regex.Unescape(text);
			}
			catch (ArgumentException)
			{
			}
		}
		return Regex.Replace(text, "\\s+", " ").Trim();
	}

	internal Func<string, byte[]> Or(Func<string, byte[]> fallback, Action<string> trouble)
	{
		return (string words) =>
		{
			string text;
			try
			{
				byte[] result = Speak(words);
				noted = null;
				return result;
			}
			catch (VoiceException ex)
			{
				if (fallback == null)
				{
					throw;
				}
				text = ex.Message;
			}
			catch (Exception ex2)
			{
				if (fallback == null)
				{
					throw;
				}
				text = Voice.Short(Name(Url, Api) + " glitched: " + ex2.Message);
			}
			text = text + (Regex.IsMatch(text, "[.!?]$") ? "" : ".") + " Her free voice is filling in.";
			if (text != noted)
			{
				noted = text;
				trouble(text);
			}
			return fallback(words);
		};
	}
}
