using System;
using System.Text.RegularExpressions;

namespace CapriDesktop;

internal sealed class Meeting
{
	internal const string AskName = "Let's get to know each other, choom. What should I call you? Say skip to skip one, or stop to stop.";

	internal const string AskCalled = "What should I call you?";

	internal const string AskPronouns = "What are your pronouns? Like she/her, he/him or they/them.";

	internal const string AskInto = "What are you into? Games, music, shows, anything.";

	internal const string Again = "Get to know me in my Settings asks again any time.";

	private static readonly Regex Nudge = new Regex("^(hi|hey|hello|heya|hiya|yo|sup|wassup|what'?s up|howdy|gm|good (morning|afternoon|evening)|ok|okay|k|kk|sure|yes|yeah|yep|alright|cool|go on|go ahead|ready|let'?s go|let'?s do (it|this))(\\s+(there|capri|choom))?\\W*$", RegexOptions.IgnoreCase);

	private static readonly Regex Pass = new Regex("^(skip|pass|next|no|nah|nope|none|nothing|not much|not really|idk|i don'?t know|dunno|not sure|rather not|i'?d rather not|prefer not to say|no thanks|no thank you|not telling)(\\s+(it|this|that|one|this one|that one|please|pls|thanks|really|much|tbh|honestly|lol))*\\W*$", RegexOptions.IgnoreCase);

	private static readonly Regex Halt = new Regex("^(stop|quit|cancel|enough|end|done|not now|later|maybe later|another time|skip (all|everything|it all|them all|the rest)|no more( questions)?|that'?s enough|stop asking( me)?( questions)?)(\\s+(please|pls|thanks))?\\W*$", RegexOptions.IgnoreCase);

	private static readonly Regex Called = new Regex("^(?:(?:you can |u can |just |please |pls |my friends |friends |people |everyone |most people )?call me|my (?:first |nick)?name(?:'s| is)|(?:the )?name'?s|i'?m|i am|it'?s|it is|they call me|i go by|people call me|everyone calls me)\\s+(?<n>.+)$", RegexOptions.IgnoreCase);

	private static readonly Regex Into = new Regex("^(?:i'?m (?:really |mostly |super )?into|i am (?:really |mostly )?into|into|i (?:really )?(?:like|love|enjoy)|mostly|probably|honestly|um+|uh+)[\\s,:]+", RegexOptions.IgnoreCase);

	private static readonly Regex Asked = new Regex("^\\W*(what|who|how|why|when|where|which|can|could|would|will|should|do|does|did|is|are|r u|have you)\\b.*\\?\\W*$", RegexOptions.IgnoreCase);

	private int step = -1;

	private bool kept;

	private bool loved;

	private string trouble;

	private string name;

	internal bool Asking => step >= 0;

	internal string Start()
	{
		step = 0;
		kept = (loved = false);
		trouble = null;
		name = null;
		return "Let's get to know each other, choom. What should I call you? Say skip to skip one, or stop to stop.";
	}

	internal void Stop()
	{
		step = -1;
	}

	internal string Take(string said, out string called)
	{
		called = null;
		if (step < 0)
		{
			return null;
		}
		string text = Regex.Replace((said ?? "").Trim(), "\\s+", " ");
		if (text.Length == 0 || Nudge.IsMatch(text))
		{
			return Ask();
		}
		if (Halt.IsMatch(text))
		{
			step = -1;
			return Wrap(stopped: true);
		}
		bool flag = Pass.IsMatch(text);
		if (!flag && Elsewhere(text))
		{
			step = -1;
			return null;
		}
		string text2 = "";
		if (!flag)
		{
			if (step == 0)
			{
				Match match = Called.Match(text);
				string text3 = Regex.Replace(match.Success ? match.Groups["n"].Value : text, "[\\s,]+(please|pls|thanks|thank you|thx|lol|haha|lmao)\\W*$", "", RegexOptions.IgnoreCase);
				if (!match.Success && Regex.Matches(text3, "\\S+").Count > 4)
				{
					step = -1;
					return null;
				}
				called = Brain.Nick(text3);
				name = ((called.Length == 0) ? null : called);
				if (name != null)
				{
					Keep("Name", name);
				}
				text2 = ((name == null) ? "Choom it is. " : ("Nice to meet you, " + name + ". "));
			}
			else if (step == 1)
			{
				if (Regex.Matches(text, "\\S+").Count > 6)
				{
					step = -1;
					return null;
				}
				Keep("Pronouns", Pronouns(text));
				text2 = "Got it. ";
			}
			else
			{
				string text4 = Regex.Replace(Into.Replace(text, ""), "[\\s,]+(lol|haha|lmao|tbh)\\W*$|[?!.]+$", "", RegexOptions.IgnoreCase).Trim();
				loved = Keep("Into", (text4.Length == 0) ? text : text4);
			}
		}
		step++;
		if (step < 3)
		{
			return text2 + Ask();
		}
		step = -1;
		return Wrap(stopped: false);
	}

	private string Ask()
	{
		if (step != 0)
		{
			if (step != 1)
			{
				return "What are you into? Games, music, shows, anything.";
			}
			return "What are your pronouns? Like she/her, he/him or they/them.";
		}
		return "What should I call you?";
	}

	private bool Elsewhere(string text)
	{
		bool flag;
		try
		{
			flag = Hands.Heard(text, out var _, out var _);
		}
		catch (Exception)
		{
			flag = false;
		}
		if (flag)
		{
			return true;
		}
		if (step == 2)
		{
			return Asked.IsMatch(text);
		}
		if (text.IndexOf('?') < 0)
		{
			return Hands.Wants(text);
		}
		return true;
	}

	private bool Keep(string label, string value)
	{
		if (Regex.Replace(value ?? "", "\\W", "").Length == 0)
		{
			return false;
		}
		string text = Brain.Jot(label, value);
		if (text != null)
		{
			trouble = text;
			return false;
		}
		kept = true;
		return true;
	}

	private string Wrap(bool stopped)
	{
		string text;
		if (kept)
		{
			text = (stopped ? "No worries. What you told me is in my notes, me.md. " : ((loved ? "Love that" : "Thanks") + ((name != null) ? (", " + name) : "") + ". It's all in my notes now, me.md. "));
		}
		else
		{
			text = (stopped ? "No worries, choom. " : "All good, choom. ");
		}
		return ((trouble != null) ? (trouble + " ") : "") + text + "Get to know me in my Settings asks again any time.";
	}

	internal static string Pronouns(string said)
	{
		string input = Regex.Replace((said ?? "").Trim().ToLowerInvariant(), "^(my pronouns are|my pronouns|pronouns are|pronouns|i use|i go by|i'?m|i am|it'?s|use)[\\s:]+", "");
		input = Regex.Replace(Regex.Replace(input, "^an? ", ""), "\\s*/\\s*", "/").Trim(' ', '.', '!', ',');
		if (Regex.IsMatch(input, "^(she|her|she/her|she/hers|she/her/hers|girl|woman|female|f|fem|feminine)$"))
		{
			return "she/her";
		}
		if (Regex.IsMatch(input, "^(he|him|he/him|he/his|he/him/his|boy|man|guy|dude|male|m|masc|masculine)$"))
		{
			return "he/him";
		}
		if (Regex.IsMatch(input, "^(they|them|they/them|they/theirs|they/them/theirs|non-?binary|enby|nb)$"))
		{
			return "they/them";
		}
		if (Regex.IsMatch(input, "^(any|any pronouns|anything|whatever|all|any/all|all/any|don'?t (care|mind)|no preference|whatever works)$"))
		{
			return "any pronouns";
		}
		if (input.Length <= 40)
		{
			return input;
		}
		return input.Substring(0, 40).TrimEnd();
	}
}
