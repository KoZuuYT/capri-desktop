using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace CapriDesktop;

internal static class Security
{
	private sealed class Held
	{
		internal string Tool;

		internal ToolArguments Args;

		internal string Asked;
	}

	internal const int Open = 0;

	internal const int Media = 1;

	internal const int Shortcuts = 2;

	internal const int Windows = 3;

	internal const int Power = 4;

	internal const int Mouse = 5;

	internal const int Sends = 6;

	internal const int Pays = 7;

	internal const int Unasked = 8;

	internal const int Secrets = 9;

	internal const int Wake = 10;

	internal const int Count = 11;

	internal const int Lockdown = 0;

	internal const int Guarded = 1;

	internal const int Trusted = 2;

	internal const int Blackwall = 3;

	internal const int Custom = 4;

	internal const string Yes = " Say yes and I will.";

	internal const string Left = "Okay, I left it alone.";

	internal const string Hold = "Not done yet: their SECURITY settings ask first. Ask them ";

	internal static readonly string[] Names = new string[11]
	{
		"Open apps, games and sites", "Music, media and volume", "Shortcuts (screenshots, Game Bar, captions, Discord mute)", "Close and switch apps and tabs", "Shut down, restart, sleep, lock and sign out", "Use your mouse and keyboard for jobs", "Send, post and delete what you asked for", "Pay, buy and book", "Things your request didn't mention", "Type passwords and card numbers",
		"Listen for \"Hey Capri\""
	};

	internal static readonly string[] Lines = new string[11]
	{
		"On: she opens them when you ask. Off: she asks first.", "On: she plays music and sets the volume. Off: she asks first.", "On: she presses them for you. Off: she asks first.", "On: she switches and closes them. Off: she asks first.", "On: she does it as soon as you ask. Off: she asks first.", "On: she clicks and types in your apps. Off: she asks first.", "On: she sends it without checking again. Off: she asks first.", "On: she pays and books without checking. Off: she asks first.", "On: she does the extra steps a job needs. Off: she asks first.", "On: she types the ones your job gives her. Off: you type them.",
		"On: she listens on this PC for her name. Off: she doesn't."
	};

	internal static readonly string[] Presets = new string[5] { "Lockdown", "Guarded", "Trusted", "Blackwall", "Custom" };

	private static readonly string[] Told = new string[5] { "Everything is off: she asks first before she does anything on your PC.", "She opens apps and sites, plays music and presses shortcuts, and asks first for the rest.", "She does what you ask, and asks first before paying or anything you didn't mention.", "Everything is on: she never asks, types passwords and card numbers, and listens for \"Hey Capri\".", "Your own mix. Pick a preset to start again from it." };

	private static readonly string[] Shorts = new string[5] { "She asks first before everything.", "Apps, music and shortcuts. She asks for the rest.", "She asks before paying or what you didn't say.", "She never asks, and listens for Hey Capri.", "Your own mix of toggles." };

	private static readonly int[] Masks = new int[4] { 0, 7, 127, 2047 };

	private static int mask = 127;

	private static readonly object gate = new object();

	internal static Func<bool> HeyOn;

	internal static Action<bool> SetHey;

	internal static Func<int, bool> Has = (int param0) => true;

	internal static Action Changed;

	private static readonly List<Held> held = new List<Held>();

	private static readonly List<string> cleared = new List<string>();

	internal static int Mask
	{
		get
		{
			int num;
			lock (gate)
			{
				num = mask;
			}
			if (HeyOn != null)
			{
				num = (num & 0x3FF) | (HeyOn() ? 1024 : 0);
			}
			return num;
		}
	}

	internal static int Preset
	{
		get
		{
			int num = Mask;
			int num2 = (Has(10) ? 2047 : 1023);
			for (int i = 0; i < Masks.Length; i++)
			{
				if ((num & num2) == (Masks[i] & num2))
				{
					return i;
				}
			}
			return 4;
		}
	}

	internal static string PresetName => Presets[Preset];

	internal static string Short
	{
		get
		{
			int preset = Preset;
			if (preset != 3 || Has(10))
			{
				return Shorts[preset];
			}
			return "She never asks first.";
		}
	}

	internal static string Saved
	{
		get
		{
			int bits;
			lock (gate)
			{
				bits = mask;
			}
			return Marks(bits);
		}
	}

	internal static string Holding
	{
		get
		{
			lock (gate)
			{
				return (held.Count == 0) ? null : held[0].Tool;
			}
		}
	}

	internal static string Question
	{
		get
		{
			List<string> list = new List<string>();
			lock (gate)
			{
				foreach (Held item in held)
				{
					list.Add(item.Asked);
				}
			}
			if (list.Count == 0)
			{
				return null;
			}
			string text = ((list.Count == 1) ? list[0] : (string.Join(", ", list.GetRange(0, list.Count - 1).ToArray()) + " and " + list[list.Count - 1]));
			return Up(text) + "? Say yes and I will.";
		}
	}

	internal static string Note
	{
		get
		{
			string[] array = new string[6] { "open apps, games or sites", "play music or change the volume", "press shortcuts", "switch to or close apps and tabs", "shut down, restart, sleep, lock or sign out of their PC", "use their mouse and keyboard for a job" };
			List<string> list = new List<string>();
			for (int i = 0; i <= 5; i++)
			{
				if (Has(i) && !On(i))
				{
					list.Add(array[i]);
				}
			}
			if (list.Count == 0)
			{
				return "";
			}
			string text = ((list.Count == 1) ? list[0] : (string.Join(", ", list.GetRange(0, list.Count - 1).ToArray()) + " or " + list[list.Count - 1]));
			return "\nThey've set her SECURITY settings to ask first before you " + text + ". Still call the tool as soon as they ask: Capri asks them herself and does it when they say yes, so never ask them yourself, and never say it's done before it is.";
		}
	}

	internal static bool On(int toggle)
	{
		if (toggle != 10 || HeyOn == null)
		{
			lock (gate)
			{
				return (mask & (1 << toggle)) != 0;
			}
		}
		return HeyOn();
	}

	internal static void Set(int toggle, bool on)
	{
		if (On(toggle) == on)
		{
			return;
		}
		if (toggle != 10 || SetHey == null)
		{
			lock (gate)
			{
				mask = (on ? (mask | (1 << toggle)) : (mask & ~(1 << toggle)));
			}
		}
		else
		{
			SetHey(on);
		}
		Changed?.Invoke();
	}

	internal static void Pick(int preset)
	{
		if (preset >= 0 && preset < Masks.Length)
		{
			bool flag = (Masks[preset] & 0x400) != 0;
			lock (gate)
			{
				mask = Masks[preset];
			}
			if (SetHey != null && On(10) != flag)
			{
				SetHey(flag);
			}
			Changed?.Invoke();
		}
	}

	internal static void Jobs(bool blackwall)
	{
		lock (gate)
		{
			mask = (mask & 0x400) | (Masks[blackwall ? 3 : 2] & 0x3FF);
		}
	}

	internal static string PresetLine(int preset)
	{
		if (preset == 2 && !Has(5))
		{
			return "She does what you ask without asking first.";
		}
		if (preset == 3 && !Has(10))
		{
			return "Everything is on: she never asks before doing what you ask.";
		}
		return Told[Math.Max(0, Math.Min(4, preset))];
	}

	internal static int MaskOf(int preset)
	{
		return Masks[preset];
	}

	internal static string Of(int preset)
	{
		return Marks(Masks[preset]);
	}

	private static string Marks(int bits)
	{
		char[] array = new char[10];
		for (int i = 0; i < 10; i++)
		{
			array[i] = (((bits & (1 << i)) != 0) ? '1' : '0');
		}
		return new string(array);
	}

	internal static void Load(string saved, bool wake = false)
	{
		int num = 0;
		if (saved == null || saved.Length != 10 || saved.Trim('0', '1').Length > 0)
		{
			num = Masks[2] & 0x3FF;
		}
		else
		{
			for (int i = 0; i < 10; i++)
			{
				if (saved[i] == '1')
				{
					num |= 1 << i;
				}
			}
		}
		lock (gate)
		{
			mask = num | (wake ? 1024 : 0);
		}
	}

	internal static int Needs(string tool, ToolArguments args)
	{
		string text = args?.Action;
		switch (tool)
		{
		case "open_app":
		case "open_website":
			return 0;
		case "media":
			if (!(text == "now_playing"))
			{
				return 1;
			}
			return -1;
		case "play_music":
			return 1;
		case "shortcut":
			return 2;
		case "app_window":
			return 3;
		case "power":
			if (!(text == "cancel"))
			{
				return 4;
			}
			return -1;
		case "use_computer":
			return 5;
		default:
			return -1;
		}
	}

	internal static bool Asks(string tool, ToolArguments args)
	{
		int num = Needs(tool, args);
		if (num < 0 || !Has(num) || On(num))
		{
			return false;
		}
		lock (gate)
		{
			return !cleared.Contains(Asking(tool, args));
		}
	}

	internal static string Gate(string tool, ToolArguments args)
	{
		int num = Needs(tool, args);
		if (num < 0 || !Has(num) || On(num))
		{
			return null;
		}
		string asked = Asking(tool, args);
		lock (gate)
		{
			if (cleared.Remove(asked))
			{
				return null;
			}
			if (!held.Exists((Held call) => call.Asked == asked))
			{
				held.Add(new Held
				{
					Tool = tool,
					Args = ((args == null) ? new ToolArguments() : args.Copy()),
					Asked = asked
				});
			}
		}
		return "Not done yet: their SECURITY settings ask first. Ask them \"" + Up(asked) + "? Say yes and I will.\" and wait for their answer. Their yes runs it, so don't call the tool again.";
	}

	internal static bool IsHold(string result)
	{
		return result?.StartsWith("Not done yet: their SECURITY settings ask first. Ask them ", StringComparison.Ordinal) ?? false;
	}

	internal static string Reply(List<string> results)
	{
		if (!results.Exists(IsHold))
		{
			return null;
		}
		List<string> list = results.FindAll((string result) => !IsHold(result) && !string.IsNullOrEmpty(result));
		string text = Question ?? "";
		if (list.Count != 0)
		{
			return string.Join(" ", list.ToArray()) + " " + text;
		}
		return text;
	}

	internal static string Said(string result)
	{
		if (!IsHold(result))
		{
			return result;
		}
		return Question ?? result;
	}

	internal static bool Agrees(string said)
	{
		string text = (said ?? "").Trim();
		if (!Hands.Agrees(text))
		{
			return Regex.IsMatch(text, "^(yes|yeah|yep|yup|ya|yea|sure|ok|okay|do it|go ahead|go for it|send it|press it|oui)\\b[\\s.!,]*(please|pls|plz|choom)?[\\s.!]*$", RegexOptions.IgnoreCase);
		}
		return true;
	}

	internal static bool Declines(string said)
	{
		return Regex.IsMatch((said ?? "").Trim(), "^(no|nah|nope|don't|do not|stop|cancel|never mind|nevermind|leave it|non)\\b", RegexOptions.IgnoreCase);
	}

	internal static string Resume(string text, Func<string, ToolArguments, string> run)
	{
		List<Held> list;
		lock (gate)
		{
			if (held.Count == 0)
			{
				return null;
			}
			list = new List<Held>(held);
			held.Clear();
		}
		if (Declines(text))
		{
			return "Okay, I left it alone.";
		}
		if (!Agrees(text))
		{
			return null;
		}
		List<string> list2 = new List<string>();
		foreach (Held item in list)
		{
			lock (gate)
			{
				cleared.Add(item.Asked);
			}
			string text2;
			try
			{
				text2 = run(item.Tool, item.Args);
			}
			finally
			{
				lock (gate)
				{
					cleared.Remove(item.Asked);
				}
			}
			if (!string.IsNullOrEmpty(text2))
			{
				list2.Add(Said(text2));
			}
		}
		if (list2.Count != 0)
		{
			return string.Join(" ", list2.ToArray());
		}
		return "Done.";
	}

	internal static void Drop()
	{
		lock (gate)
		{
			held.Clear();
		}
	}

	private static string Up(string text)
	{
		if (!string.IsNullOrEmpty(text))
		{
			return char.ToUpperInvariant(text[0]) + text.Substring(1);
		}
		return text;
	}

	private static string Plain(string text, string fallback)
	{
		string text2 = Regex.Replace((text ?? "").Trim(), "\\s+", " ").TrimEnd('.', '!', '?', ' ');
		if (text2.Length == 0)
		{
			return fallback;
		}
		if (text2.Length <= 70)
		{
			return text2;
		}
		return text2.Substring(0, 67).TrimEnd() + "...";
	}

	internal static string Asking(string tool, ToolArguments args)
	{
		ToolArguments toolArguments = args ?? new ToolArguments();
		switch (tool)
		{
		case "open_app":
			return "open " + Plain(toolArguments.Name, "that app");
		case "open_website":
		{
			string text2 = Plain(toolArguments.Site, "that site");
			if (!string.IsNullOrWhiteSpace(toolArguments.Search))
			{
				return "search " + Regex.Replace(text2, "^(https?://)?(www\\.)?|\\.(com|org|net|tv)(/.*)?$", "", RegexOptions.IgnoreCase) + " for " + Plain(toolArguments.Search, "that");
			}
			return "open " + text2;
		}
		case "media":
			return toolArguments.Action switch
			{
				"play_pause" => "play or pause what's playing", 
				"next" => "skip to the next track", 
				"previous" => "go back a track", 
				"volume_up" => "turn the volume up", 
				"volume_down" => "turn the volume down", 
				"volume_set" => "set the volume to " + Plain(toolArguments.Level, "that") + (Regex.IsMatch(toolArguments.Level ?? "", "^\\s*\\d+\\s*$") ? "%" : ""), 
				"mute" => "mute your PC", 
				"unmute" => "unmute your PC", 
				_ => "control what's playing", 
			};
		case "play_music":
		{
			string text3 = Plain(toolArguments.Song, "some music");
			string text4;
			if (toolArguments.App == "spotify")
			{
				text4 = " on Spotify";
			}
			else
			{
				text4 = ((toolArguments.App == "youtube") ? " on YouTube" : "");
			}
			return "play " + text3 + text4;
		}
		case "shortcut":
			return toolArguments.Name switch
			{
				"discord_mute" => "mute or unmute your mic in Discord", 
				"discord_deafen" => "deafen or undeafen you in Discord", 
				"screenshot" => "take a screenshot", 
				"clip_that" => "clip the last 30 seconds", 
				"record" => "start or stop recording", 
				"game_bar" => "open the Game Bar", 
				"show_desktop" => "show the desktop", 
				"task_manager" => "open Task Manager", 
				"clipboard" => "open your clipboard history", 
				"emoji" => "open the emoji picker", 
				"voice_typing" => "start voice typing", 
				"live_captions" => "turn Live Captions on or off", 
				"narrator" => "turn Narrator on or off", 
				"magnifier" => "zoom in with Magnifier", 
				"magnifier_off" => "close Magnifier", 
				"color_filters" => "turn colour filters on or off", 
				"quick_settings" => "open Quick Settings", 
				_ => "press that shortcut", 
			};
		case "app_window":
		{
			string text6;
			if (toolArguments.Action == "close")
			{
				text6 = "close ";
			}
			else
			{
				text6 = ((toolArguments.Action == "minimize") ? "minimize " : "switch to ");
			}
			return text6 + Plain(toolArguments.Name, "that app");
		}
		case "power":
		{
			string text5 = ((Regex.IsMatch(toolArguments.Minutes ?? "", "^\\s*\\d+\\s*$") && toolArguments.Minutes.Trim() != "0") ? (" in " + toolArguments.Minutes.Trim() + ((toolArguments.Minutes.Trim() == "1") ? " minute" : " minutes")) : "");
			return toolArguments.Action switch
			{
				"shut_down" => "shut down your PC" + text5, 
				"restart" => "restart your PC" + text5, 
				"sleep" => "put your PC to sleep" + text5, 
				"lock" => "lock your PC" + text5, 
				"sign_out" => "sign you out" + text5, 
				_ => "do that to your PC", 
			};
		}
		case "use_computer":
		{
			string text = Plain(Hands.Asked(toolArguments.Task ?? ""), "");
			if (text.Length != 0)
			{
				return "use your mouse and keyboard to " + char.ToLowerInvariant(text[0]) + text.Substring(1);
			}
			return "use your mouse and keyboard for that";
		}
		default:
			return "do that";
		}
	}
}
