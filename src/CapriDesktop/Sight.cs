using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Text.RegularExpressions;

namespace CapriDesktop;

internal sealed class Sight
{
	internal IntPtr Window;

	internal string Title = "";

	internal string Program = "";

	internal Rectangle Bounds;

	internal string Problem;

	internal readonly List<Target> Targets = new List<Target>();

	internal readonly List<string> Words = new List<string>();

	internal string Text
	{
		get
		{
			if (Window == IntPtr.Zero)
			{
				return "No app window is open. Open one with open_app or open_website.";
			}
			StringBuilder stringBuilder = new StringBuilder("Window in front: " + Voice.Quote(Title) + " (" + Program + ")");
			if (Problem != null)
			{
				stringBuilder.Append("\n" + Problem);
			}
			else if (Targets.Count == 0)
			{
				stringBuilder.Append("\nNo buttons or boxes can be read in this window. Use press for keys, or open something else.");
			}
			foreach (Target target in Targets)
			{
				stringBuilder.Append("\n" + target.Label);
				if (target.Secret)
				{
					stringBuilder.Append(" (password box)");
				}
				else if (!string.IsNullOrEmpty(target.Value))
				{
					stringBuilder.Append(" = " + Voice.Quote(target.Value));
				}
				if (target.Focused)
				{
					stringBuilder.Append(" (focused)");
				}
			}
			if (Words.Count > 0)
			{
				stringBuilder.Append("\nText on screen: " + string.Join(" | ", Words.ToArray()));
			}
			return stringBuilder.ToString();
		}
	}

	internal Target Find(string number)
	{
		if (!int.TryParse(Regex.Replace(number ?? "", "[^\\d]", ""), out var wanted))
		{
			return null;
		}
		return Targets.Find((Target target) => target.Number == wanted);
	}
}
