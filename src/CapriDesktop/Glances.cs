using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace CapriDesktop;

internal static class Glances
{
	internal const string Line = "(You glance at their screen while they do their thing. Reply in two parts, with the labels SAW: and SAY: written exactly like that. SAW: what's on their screen, in under 20 plain words: the game or app and what's happening. SAY: your reaction in one short line, in character, to something specific you can see: tease them, cheer them on, or ask about it. A game scene, scoreboard, inventory or quiet moment is enough. Don't invent action from a still image, and never guess who a person is; name someone only if their name is written on screen. If nothing's worth a line, still give the SAW part and make it SAY: SKIP. For a blank screen, a bare desktop or a loading screen, reply with just SKIP.)";

	internal const double Memory = 3600.0;

	private static readonly HashSet<string> Plain = new HashSet<string>("about after again all also always and another any anything are around away back been before being best better big bit bring but came can come coming could damn dang did does doing done down each even ever every feel feels finally first for from game games get gets getting give going gonna good got great guess had has have hey here hope how huge huh into just keep kind know last least left let like little look looks looking lol lots made make many maybe mean more most move much nah need never new next nice not nothing now off okay old omg once one only ooh other out over own play played player playing pretty probably quite ready real really right same say screen see seen should show some somebody someone something soon still such sure take than that the them then there they thing things think this those though time today too took tried try trying turn use very want wanna was wait watch watching way well went were what when where which while who whoa why will with without wow yeah year yep yes yet you your yours choom preem nova gonk delta eddies flatline flatlined corpo corpos yezzirrr night city arasaka capri".Split(' '));

	private static readonly Regex Label = new Regex("(?<![\\p{L}\\p{N}'])(?:(?<label>SAW|SAY)[*_]*\\s*(?::|\\p{Pd}(?=\\s))|(?<label>[Ss]aw|[Ss]ay)[*_]*\\s*:)[*_]*");

	internal static bool Moved(byte[] print, byte[] before)
	{
		if (before == null || before.Length != print.Length)
		{
			return true;
		}
		long num = 0L;
		int num2 = 0;
		for (int i = 0; i + 2 < print.Length; i += 3)
		{
			int num3 = Math.Max(Math.Abs(print[i] - before[i]), Math.Max(Math.Abs(print[i + 1] - before[i + 1]), Math.Abs(print[i + 2] - before[i + 2])));
			num += num3;
			if (num3 > 24)
			{
				num2++;
			}
		}
		if (num <= 13824)
		{
			return num2 >= 24;
		}
		return true;
	}

	internal static string Ask(List<string> said)
	{
		if (said.Count == 0)
		{
			return "(You glance at their screen while they do their thing. Reply in two parts, with the labels SAW: and SAY: written exactly like that. SAW: what's on their screen, in under 20 plain words: the game or app and what's happening. SAY: your reaction in one short line, in character, to something specific you can see: tease them, cheer them on, or ask about it. A game scene, scoreboard, inventory or quiet moment is enough. Don't invent action from a still image, and never guess who a person is; name someone only if their name is written on screen. If nothing's worth a line, still give the SAW part and make it SAY: SKIP. For a blank screen, a bare desktop or a loading screen, reply with just SKIP.)";
		}
		int num = Math.Max(0, said.Count - 6);
		string text = "(You glance at their screen while they do their thing. Reply in two parts, with the labels SAW: and SAY: written exactly like that. SAW: what's on their screen, in under 20 plain words: the game or app and what's happening. SAY: your reaction in one short line, in character, to something specific you can see: tease them, cheer them on, or ask about it. A game scene, scoreboard, inventory or quiet moment is enough. Don't invent action from a still image, and never guess who a person is; name someone only if their name is written on screen. If nothing's worth a line, still give the SAW part and make it SAY: SKIP. For a blank screen, a bare desktop or a loading screen, reply with just SKIP.)\n(You already said these about their screen:";
		for (int i = num; i < said.Count; i++)
		{
			text = text + "\n- " + said[i];
		}
		List<string> list = new List<string>();
		for (int j = 0; j < num; j++)
		{
			foreach (string item in Keys(said[j], Brain.Nickname))
			{
				if (list.Count < 12)
				{
					Key(list, item);
				}
			}
		}
		if (list.Count > 0)
		{
			text = text + "\nEarlier you also brought up: " + string.Join(", ", list.ToArray()) + ".";
		}
		return text + "\nDon't bring any of it up again: no character, move, item, place, number, name or joke from those lines. Pick something new and specific, or make it SAY: SKIP.)";
	}

	internal static bool Repeats(string said, IEnumerable<string> before, string nickname)
	{
		string text = " " + string.Join(" ", Spoken(said, out var capital, out var opening)) + " ";
		string[] own = Own(nickname);
		List<string> list = Keys(said, nickname);
		List<string> list2 = new List<string>();
		string[] array = Spoken(said, out capital, out opening);
		foreach (string text2 in array)
		{
			if (Content(text2, own))
			{
				Key(list2, text2);
			}
		}
		foreach (string item in before)
		{
			string text3 = " " + string.Join(" ", Spoken(item, out capital, out opening)) + " ";
			if (text3 == text)
			{
				return true;
			}
			foreach (string item2 in list)
			{
				if (text3.IndexOf(" " + item2 + " ", StringComparison.Ordinal) >= 0)
				{
					return true;
				}
			}
			foreach (string item3 in Keys(item, nickname))
			{
				if (text.IndexOf(" " + item3 + " ", StringComparison.Ordinal) >= 0)
				{
					return true;
				}
			}
			int num = 0;
			foreach (string item4 in list2)
			{
				if (text3.IndexOf(" " + item4 + " ", StringComparison.Ordinal) >= 0)
				{
					num++;
				}
			}
			if (num >= 2 && num * 5 >= list2.Count * 3)
			{
				return true;
			}
		}
		return false;
	}

	internal static List<string> Keys(string line, string nickname)
	{
		string[] array = Spoken(line, out var capital, out var opening);
		string[] own = Own(nickname);
		List<string> list = new List<string>();
		for (int i = 0; i < array.Length; i++)
		{
			if (!capital[i])
			{
				continue;
			}
			int j;
			for (j = i; j + 1 < array.Length && (capital[j + 1] || (j + 2 < array.Length && capital[j + 2] && (array[j + 1] == "of" || array[j + 1] == "the" || array[j + 1] == "and"))); j += (capital[j + 1] ? 1 : 2))
			{
			}
			int k = i;
			int num;
			for (num = j; k <= num && Ordinary(array[k], own); k++)
			{
			}
			while (num >= k && Ordinary(array[num], own))
			{
				num--;
			}
			int num2 = 0;
			for (int l = k; l <= num; l++)
			{
				if (capital[l])
				{
					num2++;
				}
			}
			if (num2 >= 2)
			{
				Key(list, string.Join(" ", array, k, num - k + 1));
			}
			else if (k == num && !opening[k] && array[k].Length >= 4)
			{
				Key(list, array[k]);
			}
			i = j;
		}
		for (int m = 0; m < array.Length; m++)
		{
			if (array[m].Length >= 2 && Regex.IsMatch(array[m], "^\\d+$"))
			{
				Key(list, array[m]);
			}
			else if (m + 1 < array.Length && Content(array[m], own) && Content(array[m + 1], own))
			{
				Key(list, array[m] + " " + array[m + 1]);
			}
		}
		return list;
	}

	private static string[] Spoken(string line, out bool[] capital, out bool[] opening)
	{
		string input = Regex.Replace(line ?? "", "\\[[^\\]]*\\]", ".");
		input = Regex.Replace(input, "(?<=\\d),(?=\\d{3}(?!\\d))", "");
		int num = 0;
		int num2 = 0;
		string text = input;
		foreach (char c in text)
		{
			if (char.IsLetter(c))
			{
				num++;
				if (char.IsUpper(c))
				{
					num2++;
				}
			}
		}
		if (num2 * 2 > num)
		{
			input = input.ToLowerInvariant();
		}
		MatchCollection matchCollection = Regex.Matches(input, "[\\p{L}\\p{Nd}][\\p{L}\\p{Nd}'’]*|[.!?]");
		string[] array = new string[matchCollection.Count];
		capital = new bool[matchCollection.Count];
		opening = new bool[matchCollection.Count];
		bool flag = true;
		for (int j = 0; j < matchCollection.Count; j++)
		{
			string value = matchCollection[j].Value;
			switch (value)
			{
			case ".":
			case "!":
			case "?":
				array[j] = ".";
				flag = true;
				break;
			default:
				array[j] = Regex.Replace(value.ToLowerInvariant().Replace('’', '\''), "'s$", "");
				capital[j] = char.IsUpper(value[0]);
				opening[j] = flag;
				flag = false;
				break;
			}
		}
		return array;
	}

	private static string[] Own(string nickname)
	{
		return Regex.Split((nickname ?? "").ToLowerInvariant().Replace('’', '\''), "[^\\p{L}\\p{Nd}']+");
	}

	private static bool Ordinary(string word, string[] own)
	{
		if (word.Length >= 3 && word.IndexOf('\'') < 0 && !Plain.Contains(word))
		{
			return Array.IndexOf(own, word) >= 0;
		}
		return true;
	}

	private static bool Content(string word, string[] own)
	{
		if (word.Length >= 4 && !Ordinary(word, own))
		{
			return !Regex.IsMatch(word, "^\\d+$");
		}
		return false;
	}

	private static void Key(List<string> keys, string key)
	{
		if (!keys.Contains(key))
		{
			keys.Add(key);
		}
	}

	internal static string Reply(string said)
	{
		string saw;
		return Reply(said, out saw);
	}

	internal static string Reply(string raw, out string saw)
	{
		saw = null;
		string text = raw ?? "";
		Match match = null;
		Match match2 = null;
		foreach (Match item in Label.Matches(text))
		{
			bool flag = item.Groups["label"].Value.ToUpperInvariant() == "SAY";
			if (flag && match2 == null)
			{
				match2 = item;
			}
			else if (!flag && match == null)
			{
				match = item;
			}
		}
		if (match != null || match2 != null)
		{
			string text2 = ((match == null) ? "" : Brain.Clean(Labelled(text, match, match2)));
			if (text2.Length > 0 && !Regex.IsMatch(text2, "^[\\[(]?SKIP[\\])]?[.!?]*$", RegexOptions.IgnoreCase))
			{
				saw = ((text2.Length > 200) ? (text2.Substring(0, 200).TrimEnd() + "...") : text2);
			}
			text = ((match2 == null) ? "" : Labelled(text, match2, match));
		}
		string text3 = Brain.Sparing(Brain.Clean(text), lately: false, Brain.Nickname);
		if (!Regex.IsMatch(text3, "^[\\[(]?(SKIP|nothing|none)[\\])]?[.!?]*$", RegexOptions.IgnoreCase) && !Regex.IsMatch(text3, "^[\\[(]?SKIP\\b"))
		{
			return text3;
		}
		return "";
	}

	private static string Labelled(string text, Match label, Match other)
	{
		int num = label.Index + label.Length;
		int num2 = ((other != null && other.Index > label.Index) ? other.Index : text.Length);
		return text.Substring(num, num2 - num).Trim(' ', '*', '_', '#', '>', '-', '|', '/', ';', ',');
	}
}
