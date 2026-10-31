using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Speech.AudioFormat;
using System.Speech.Recognition;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace CapriDesktop;

internal static class Hearing
{
	internal const string NoRecognizer = "Windows has no offline speech recognizer for your language. Turn on Online speech recognition in Settings > Privacy & security > Speech.";

	internal static Func<short[], string> Run = Recognize;

	internal static volatile LocalEars Own;

	private static readonly object gate = new object();

	private static SpeechRecognitionEngine engine;

	internal static string Text(short[] samples)
	{
		return Clean(Run(samples));
	}

	internal static string Clean(string text)
	{
		string text2 = Regex.Replace(text ?? "", "\\s+", " ").Trim();
		if (!Regex.IsMatch(text2, "[\\p{L}\\p{N}]"))
		{
			return "";
		}
		return text2;
	}

	internal static string Pick(IList<string> cultures, string wanted)
	{
		string b = wanted.Split('-')[0];
		foreach (string culture in cultures)
		{
			if (string.Equals(culture, wanted, StringComparison.OrdinalIgnoreCase))
			{
				return culture;
			}
		}
		foreach (string culture2 in cultures)
		{
			if (string.Equals(culture2.Split('-')[0], b, StringComparison.OrdinalIgnoreCase))
			{
				return culture2;
			}
		}
		foreach (string culture3 in cultures)
		{
			if (string.Equals(culture3, "en-US", StringComparison.OrdinalIgnoreCase))
			{
				return culture3;
			}
		}
		if (cultures.Count <= 0)
		{
			return null;
		}
		return cultures[0];
	}

	private static SpeechRecognitionEngine Open()
	{
		List<RecognizerInfo> list = new List<RecognizerInfo>(SpeechRecognitionEngine.InstalledRecognizers());
		List<string> list2 = list.ConvertAll((RecognizerInfo info) => info.Culture.Name);
		string text = Pick(list2, CultureInfo.CurrentUICulture.Name);
		if (text == null)
		{
			throw new IOException("Windows has no offline speech recognizer for your language. Turn on Online speech recognition in Settings > Privacy & security > Speech.");
		}
		SpeechRecognitionEngine speechRecognitionEngine = new SpeechRecognitionEngine(list[list2.IndexOf(text)]);
		speechRecognitionEngine.LoadGrammar(new DictationGrammar());
		return speechRecognitionEngine;
	}

	private static string Recognize(short[] samples)
	{
		LocalEars own = Own;
		if (own == null || !own.Ready)
		{
			lock (gate)
			{
				if (engine == null)
				{
					engine = Open();
				}
				byte[] array = new byte[samples.Length * 2];
				Buffer.BlockCopy(samples, 0, array, 0, array.Length);
				StringBuilder words = new StringBuilder();
				ManualResetEvent done = new ManualResetEvent(initialState: false);
				EventHandler<SpeechRecognizedEventArgs> value = (object sender, SpeechRecognizedEventArgs e) =>
				{
					lock (words)
					{
						words.Append(e.Result.Text).Append(' ');
					}
				};
				EventHandler<RecognizeCompletedEventArgs> value2 = (object param0, RecognizeCompletedEventArgs param1) =>
				{
					done.Set();
				};
				engine.SpeechRecognized += value;
				engine.RecognizeCompleted += value2;
				try
				{
					using MemoryStream audioSource = new MemoryStream(array);
					engine.SetInputToAudioStream(audioSource, new SpeechAudioFormatInfo(16000, AudioBitsPerSample.Sixteen, AudioChannel.Mono));
					engine.RecognizeAsync(RecognizeMode.Multiple);
					if (!done.WaitOne(20000))
					{
						engine.RecognizeAsyncCancel();
						done.WaitOne(3000);
						throw new IOException("Hearing that took too long. Try a shorter line.");
					}
				}
				finally
				{
					engine.SpeechRecognized -= value;
					engine.RecognizeCompleted -= value2;
					engine.SetInputToNull();
				}
				lock (words)
				{
					return words.ToString();
				}
			}
		}
		return own.Hear(samples);
	}
}
