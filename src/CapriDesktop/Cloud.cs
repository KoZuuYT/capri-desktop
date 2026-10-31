using System;
using System.Text.RegularExpressions;

namespace CapriDesktop;

internal sealed class Cloud
{
	internal readonly string Key;

	internal readonly string Kind;

	internal volatile string Model;

	internal bool Claude => Kind == "anthropic";

	internal bool Code => Kind == "claudecode";

	internal string Name
	{
		get
		{
			if (!(Kind == "anthropic"))
			{
				if (!(Kind == "claudecode"))
				{
					if (!(Kind == "deepseek"))
					{
						if (!(Kind == "nvidia"))
						{
							return "ChatGPT";
						}
						return "NVIDIA";
					}
					return "DeepSeek";
				}
				return "Claude Code";
			}
			return "Claude";
		}
	}

	internal string Company
	{
		get
		{
			if (!(Kind == "anthropic"))
			{
				if (!(Kind == "claudecode"))
				{
					if (!(Kind == "deepseek"))
					{
						if (!(Kind == "nvidia"))
						{
							return "OpenAI";
						}
						return "NVIDIA";
					}
					return "DeepSeek";
				}
				return "Claude Code";
			}
			return "Anthropic";
		}
	}

	internal Cloud(string key, string model)
	{
		Key = key;
		Kind = KindOf(key);
		Model = model;
	}

	internal static string KindOf(string key)
	{
		if (ClaudeCode.Named(key))
		{
			return "claudecode";
		}
		if (key.StartsWith("sk-ant-", StringComparison.Ordinal))
		{
			return "anthropic";
		}
		if (key.StartsWith("nvapi-", StringComparison.Ordinal))
		{
			return "nvidia";
		}
		if (Regex.IsMatch(key, "^sk-[0-9a-f]{32}$"))
		{
			return "deepseek";
		}
		if (!key.StartsWith("sk-", StringComparison.Ordinal))
		{
			return null;
		}
		return "openai";
	}
}
