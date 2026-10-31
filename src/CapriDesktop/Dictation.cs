using System;
using Windows.Media.Devices;

namespace CapriDesktop;

internal abstract class Dictation : IDisposable
{
	internal const string PrivacyOff = "Online speech recognition is off in Windows. Turn it on in Settings > Privacy & security > Speech.";

	internal const string MicBlocked = "Windows is blocking the mic. Turn on Let desktop apps access your microphone in Settings > Privacy & security > Microphone.";

	internal const string NoMic = "Windows can't find a mic. Plug one in, or pick one in Settings > System > Sound.";

	internal static Func<Dictation> Make = () => new WindowsDictation();

	internal static Func<string> DefaultMic = () =>
	{
		try
		{
			return MediaDevice.GetDefaultAudioCaptureId((AudioDeviceRole)0);
		}
		catch (Exception)
		{
			return (string)null;
		}
	};

	internal Action<string> Heard;

	internal Action<string> Lost;

	internal string Language;

	internal abstract void Start(Action<string> started);

	internal abstract void Hold(bool held);

	public abstract void Dispose();

	internal static string OtherMic(string chosen, string name, string defaultId)
	{
		if (string.IsNullOrEmpty(chosen) || string.IsNullOrEmpty(name) || string.IsNullOrEmpty(defaultId) || defaultId.IndexOf(chosen, StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return null;
		}
		return "Windows speech only hears your default mic, not " + name + ". Make " + name + " the default in Windows Settings > System > Sound > Input, or pick Mic: default here.";
	}

	internal static string Explain(Exception error)
	{
		while (error is AggregateException && error.InnerException != null)
		{
			error = error.InnerException;
		}
		if (error.HResult == -2147199735)
		{
			return "Online speech recognition is off in Windows. Turn it on in Settings > Privacy & security > Speech.";
		}
		if (error.HResult == -2147024891)
		{
			return "Windows is blocking the mic. Turn on Let desktop apps access your microphone in Settings > Privacy & security > Microphone.";
		}
		if (error.HResult == -1072845856)
		{
			return "Windows can't find a mic. Plug one in, or pick one in Settings > System > Sound.";
		}
		return "Windows speech recognition didn't start: " + Voice.Short(error.Message);
	}
}
