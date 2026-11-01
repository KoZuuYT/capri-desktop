using Condition = System.Windows.Automation.Condition;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using Windows.Foundation;
using Windows.Media.Control;

namespace CapriDesktop;

internal static class Tunes
{
	private static readonly Stopwatch clock = Stopwatch.StartNew();

	private static GlobalSystemMediaTransportControlsSessionManager manager;

	internal static Func<Tune> Read = ReadSession;

	internal static Func<Tune> Current = ReadCurrent;

	internal static Func<Tune, string, bool> Send = SendToSession;

	internal static Func<IntPtr, bool> Bring = Hands.Front;

	internal static Func<bool?> Hearted = SpotifyHearted;

	private static readonly string[,] Apps = new string[19, 3]
	{
		{ "spotify", "Spotify", "Spotify" },
		{ "^chrome", "Chrome", "chrome" },
		{ "msedge", "Edge", "msedge" },
		{ "firefox|^308046B0AF4A39CB$", "Firefox", "firefox" },
		{ "brave", "Brave", "brave" },
		{ "opera", "Opera", "opera" },
		{ "vivaldi", "Vivaldi", "vivaldi" },
		{ "zunemusic", "Media Player", "Microsoft.Media.Player" },
		{ "applemusic|itunes", "Apple Music", "AppleMusic" },
		{ "amazonmusic", "Amazon Music", "Amazon Music" },
		{ "tidal", "TIDAL", "TIDAL" },
		{ "deezer", "Deezer", "Deezer" },
		{ "soundcloud", "SoundCloud", "SoundCloud" },
		{ "foobar", "foobar2000", "foobar2000" },
		{ "musicbee", "MusicBee", "MusicBee" },
		{ "aimp", "AIMP", "AIMP" },
		{ "winamp", "Winamp", "winamp" },
		{ "vlc", "VLC", "vlc" },
		{ "discord", "Discord", "Discord" }
	};

	private static readonly string[] HeartNames = new string[6] { "Add to Liked Songs", "Save to Your Library", "Save to Liked Songs", "Remove from Liked Songs", "Remove from Your Library", "Add to playlist" };

	internal static double Now => clock.Elapsed.TotalSeconds;

	[DllImport("user32.dll")]
	private static extern bool IsIconic(IntPtr window);

	[DllImport("user32.dll")]
	private static extern bool ShowWindow(IntPtr window, int command);

	internal static string AppName(string app)
	{
		string input = app ?? "";
		for (int i = 0; i < Apps.GetLength(0); i++)
		{
			if (Regex.IsMatch(input, Apps[i, 0], RegexOptions.IgnoreCase))
			{
				return Apps[i, 1];
			}
		}
		Match match = Regex.Match(input, "^[^!_]*?\\.([^.!_]+)_[^!]*!");
		if (match.Success)
		{
			return match.Groups[1].Value;
		}
		Match match2 = Regex.Match(input, "([^\\\\/]+?)(?:\\.exe)?$", RegexOptions.IgnoreCase);
		if (!match2.Success || match2.Groups[1].Value.Length <= 0)
		{
			return "your player";
		}
		return match2.Groups[1].Value;
	}

	private static string Program(string app)
	{
		string input = app ?? "";
		for (int i = 0; i < Apps.GetLength(0); i++)
		{
			if (Regex.IsMatch(input, Apps[i, 0], RegexOptions.IgnoreCase))
			{
				return Apps[i, 2];
			}
		}
		return Regex.Replace(input, "\\.exe$", "", RegexOptions.IgnoreCase);
	}

	private static GlobalSystemMediaTransportControlsSessionManager Manager()
	{
		// the mod: Wine has no Windows media sessions
		if (manager == null && !Native.Wine)
		{
			try
			{
				Task<GlobalSystemMediaTransportControlsSessionManager> task = GlobalSystemMediaTransportControlsSessionManager.RequestAsync().AsTask<GlobalSystemMediaTransportControlsSessionManager>();
				if (task.Wait(5000))
				{
					manager = task.Result;
				}
			}
			catch (Exception)
			{
			}
		}
		return manager;
	}

	private static Tune ReadSession()
	{
		GlobalSystemMediaTransportControlsSessionManager val = Manager();
		if (val == null)
		{
			return null;
		}
		try
		{
			List<GlobalSystemMediaTransportControlsSession> list = new List<GlobalSystemMediaTransportControlsSession>();
			GlobalSystemMediaTransportControlsSession currentSession = val.GetCurrentSession();
			if (currentSession != null)
			{
				list.Add(currentSession);
			}
			foreach (GlobalSystemMediaTransportControlsSession session in val.GetSessions())
			{
				if (!object.ReferenceEquals(session, currentSession))
				{
					list.Add(session);
				}
			}
			Tune tune = null;
			foreach (GlobalSystemMediaTransportControlsSession item in list)
			{
				Tune tune2 = Describe(item);
				if (tune2.Title.Length != 0)
				{
					if (tune2.Playing)
					{
						return tune2;
					}
					if (tune == null)
					{
						tune = tune2;
					}
				}
			}
			return tune;
		}
		catch (Exception)
		{
			manager = null;
			return null;
		}
	}

	private static Tune ReadCurrent()
	{
		GlobalSystemMediaTransportControlsSessionManager val = Manager();
		if (val == null)
		{
			return null;
		}
		try
		{
			GlobalSystemMediaTransportControlsSession currentSession = val.GetCurrentSession();
			if (currentSession == null)
			{
				return null;
			}
			Tune tune = Describe(currentSession);
			return (tune.Title.Length == 0) ? null : tune;
		}
		catch (Exception)
		{
			manager = null;
			return null;
		}
	}

	private static Tune Describe(GlobalSystemMediaTransportControlsSession session)
	{
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Invalid comparison between Unknown and I4
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Invalid comparison between Unknown and I4
		Tune tune = new Tune();
		tune.Session = session;
		tune.App = session.SourceAppUserModelId ?? "";
		tune.ReadAt = Now;
		Tune tune2 = tune;
		Task<GlobalSystemMediaTransportControlsSessionMediaProperties> task = session.TryGetMediaPropertiesAsync().AsTask<GlobalSystemMediaTransportControlsSessionMediaProperties>();
		GlobalSystemMediaTransportControlsSessionMediaProperties val = (task.Wait(2000) ? task.Result : null);
		if (val != null)
		{
			tune2.Title = Clean(val.Title);
			tune2.Artist = Clean(val.Artist);
			if (tune2.Artist.Length == 0)
			{
				tune2.Artist = Clean(val.AlbumArtist);
			}
		}
		GlobalSystemMediaTransportControlsSessionPlaybackInfo playbackInfo = session.GetPlaybackInfo();
		if (playbackInfo != null)
		{
			tune2.Playing = (int)playbackInfo.PlaybackStatus == 4 || (int)playbackInfo.PlaybackStatus == 2;
			GlobalSystemMediaTransportControlsSessionPlaybackControls controls = playbackInfo.Controls;
			if (controls != null)
			{
				tune2.CanSkip = controls.IsNextEnabled;
				tune2.CanBack = controls.IsPreviousEnabled;
				tune2.CanSeek = controls.IsPlaybackPositionEnabled;
				tune2.CanPlay = controls.IsPlayEnabled;
				tune2.CanPause = controls.IsPauseEnabled;
				tune2.CanToggle = controls.IsPlayPauseToggleEnabled;
			}
			double? playbackRate = playbackInfo.PlaybackRate;
			if (playbackRate.HasValue && playbackRate.Value > 0.0)
			{
				tune2.Rate = playbackRate.Value;
			}
		}
		GlobalSystemMediaTransportControlsSessionTimelineProperties timelineProperties = session.GetTimelineProperties();
		if (timelineProperties != null)
		{
			tune2.Start = timelineProperties.StartTime.TotalSeconds;
			tune2.Length = Math.Max(0.0, (timelineProperties.EndTime - timelineProperties.StartTime).TotalSeconds);
			double num = (timelineProperties.Position - timelineProperties.StartTime).TotalSeconds;
			double totalSeconds = (DateTimeOffset.Now - timelineProperties.LastUpdatedTime).TotalSeconds;
			if (tune2.Playing && totalSeconds > 0.0 && totalSeconds < 21600.0)
			{
				num += totalSeconds * tune2.Rate;
			}
			tune2.Position = Math.Max(0.0, (tune2.Length > 0.0) ? Math.Min(num, tune2.Length) : num);
		}
		if (tune2.Length <= 0.0)
		{
			tune2.CanSeek = false;
		}
		return tune2;
	}

	private static string Clean(string text)
	{
		return Regex.Replace(text ?? "", "[\\s\\u0000-\\u001F\\u007F]+", " ").Trim();
	}

	private static bool SendToSession(Tune tune, string what)
	{
		if (what == "like" || what == "unlike")
		{
			return LikeOnSpotify(what == "like");
		}
		object obj;
		if (tune != null)
		{
			object session = tune.Session;
			obj = ((session is GlobalSystemMediaTransportControlsSession) ? session : null);
		}
		else
		{
			obj = null;
		}
		GlobalSystemMediaTransportControlsSession val = (GlobalSystemMediaTransportControlsSession)obj;
		if (val != null)
		{
			try
			{
				IAsyncOperation<bool> val2 = null;
				switch (what)
				{
				case "previous":
					val2 = val.TrySkipPreviousAsync();
					break;
				case "next":
					val2 = val.TrySkipNextAsync();
					break;
				case "play":
					val2 = ((tune.CanPlay || !tune.CanToggle) ? val.TryPlayAsync() : val.TryTogglePlayPauseAsync());
					break;
				case "pause":
					val2 = ((tune.CanPause || !tune.CanToggle) ? val.TryPauseAsync() : val.TryTogglePlayPauseAsync());
					break;
				default:
					if (what.StartsWith("seek:", StringComparison.Ordinal))
					{
						if (!double.TryParse(what.Substring(5), NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
						{
							return false;
						}
						val2 = val.TryChangePlaybackPositionAsync((long)((tune.Start + result) * 10000000.0));
					}
					break;
				}
				if (val2 != null)
				{
					Task<bool> task = val2.AsTask<bool>();
					if (task.Wait(3000) && task.Result)
					{
						return true;
					}
				}
			}
			catch (Exception)
			{
			}
		}
		int num;
		switch (what)
		{
		default:
			num = 0;
			break;
		case "play":
		case "pause":
			num = 179;
			break;
		case "next":
			num = 176;
			break;
		case "previous":
			num = 177;
			break;
		}
		byte b = (byte)num;
		if (b == 0)
		{
			return false;
		}
		Hands.Press(b);
		return true;
	}

	internal static bool Open(Tune tune)
	{
		if (tune == null)
		{
			return false;
		}
		string b = Program(tune.App);
		OpenWindow openWindow = null;
		foreach (OpenWindow item in Hands.Windows())
		{
			if (string.Equals(item.Program, b, StringComparison.OrdinalIgnoreCase) && (openWindow == null || (tune.Title.Length > 0 && (item.Title ?? "").IndexOf(tune.Title, StringComparison.OrdinalIgnoreCase) >= 0)))
			{
				openWindow = item;
			}
		}
		if (openWindow != null)
		{
			return Bring(openWindow.Handle);
		}
		if (!tune.Spotify)
		{
			return false;
		}
		Hands.Run("spotify:");
		return true;
	}

	private static bool LikeOnSpotify(bool want)
	{
		IntPtr intPtr = SpotifyWindow();
		if (intPtr == IntPtr.Zero)
		{
			return false;
		}
		try
		{
			AutomationElement automationElement = SpotifyHeart(intPtr);
			bool? flag = ((automationElement == null) ? ((bool?)null) : HeartLiked(automationElement));
			if (flag.HasValue && flag.Value == want)
			{
				return true;
			}
			if (want)
			{
				bool? flag2 = flag;
				if (flag2 != true && flag2.HasValue && automationElement.TryGetCurrentPattern(InvokePattern.Pattern, out var patternObject))
				{
					((InvokePattern)patternObject).Invoke();
					return true;
				}
			}
		}
		catch (ElementNotAvailableException)
		{
		}
		catch (InvalidOperationException)
		{
		}
		catch (COMException)
		{
		}
		IntPtr foregroundWindow = Native.GetForegroundWindow();
		bool flag3 = IsIconic(intPtr);
		if (!Bring(intPtr))
		{
			return false;
		}
		Hands.Chord(new byte[3] { 18, 16, 66 });
		Thread.Sleep(200);
		if (flag3)
		{
			ShowWindow(intPtr, 7);
		}
		if (foregroundWindow != IntPtr.Zero && foregroundWindow != intPtr)
		{
			Bring(foregroundWindow);
		}
		return true;
	}

	private static IntPtr SpotifyWindow()
	{
		foreach (OpenWindow item in Hands.Windows())
		{
			if (string.Equals(item.Program, "Spotify", StringComparison.OrdinalIgnoreCase) && item.Handle != IntPtr.Zero)
			{
				return item.Handle;
			}
		}
		return IntPtr.Zero;
	}

	private static bool? SpotifyHearted()
	{
		IntPtr intPtr = SpotifyWindow();
		if (intPtr == IntPtr.Zero)
		{
			return null;
		}
		try
		{
			AutomationElement automationElement = SpotifyHeart(intPtr);
			return (automationElement == null) ? ((bool?)null) : HeartLiked(automationElement);
		}
		catch (ElementNotAvailableException)
		{
		}
		catch (InvalidOperationException)
		{
		}
		catch (COMException)
		{
		}
		return null;
	}

	private static AutomationElement SpotifyHeart(IntPtr window)
	{
		AutomationElement automationElement = AutomationElement.FromHandle(window);
		System.Windows.Rect boundingRectangle = automationElement.Current.BoundingRectangle;
		if (boundingRectangle.IsEmpty)
		{
			return null;
		}
		Condition[] array = new Condition[HeartNames.Length];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = new PropertyCondition(AutomationElement.NameProperty, HeartNames[i]);
		}
		Condition condition = new AndCondition(new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button), new OrCondition(array));
		double num = Math.Max(150.0, boundingRectangle.Height * 0.2);
		foreach (AutomationElement item in automationElement.FindAll(TreeScope.Descendants, condition))
		{
			System.Windows.Rect boundingRectangle2 = item.Current.BoundingRectangle;
			if (!boundingRectangle2.IsEmpty && boundingRectangle2.Y + boundingRectangle2.Height / 2.0 >= boundingRectangle.Bottom - num && boundingRectangle2.X + boundingRectangle2.Width / 2.0 <= boundingRectangle.Left + boundingRectangle.Width / 2.0)
			{
				return item;
			}
		}
		return null;
	}

	private static bool? HeartLiked(AutomationElement heart)
	{
		if (heart.TryGetCurrentPattern(TogglePattern.Pattern, out var patternObject))
		{
			return ((TogglePattern)patternObject).Current.ToggleState == ToggleState.On;
		}
		string text = heart.Current.Name ?? "";
		if (text.StartsWith("Remove", StringComparison.Ordinal) || text == "Add to playlist")
		{
			return true;
		}
		if (text.StartsWith("Add to Liked", StringComparison.Ordinal) || text.StartsWith("Save", StringComparison.Ordinal))
		{
			return false;
		}
		return null;
	}
}
