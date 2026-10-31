using System;

namespace CapriDesktop;

internal sealed class Tune
{
	internal string Title = "";

	internal string Artist = "";

	internal string App = "";

	internal bool Playing;

	internal double Start;

	internal double Position;

	internal double Length;

	internal double ReadAt;

	internal double Rate = 1.0;

	internal bool CanSeek;

	internal bool CanSkip = true;

	internal bool CanBack = true;

	internal bool CanPlay = true;

	internal bool CanPause = true;

	internal bool CanToggle = true;

	internal object Session;

	internal bool Spotify => App.IndexOf("spotify", StringComparison.OrdinalIgnoreCase) >= 0;

	internal string Key => App + "\n" + Title + "\n" + Artist;

	internal double At(double now)
	{
		double num = Position + (Playing ? (Math.Max(0.0, now - ReadAt) * Rate) : 0.0);
		if (!(Length > 0.0))
		{
			return num;
		}
		return Math.Min(num, Length);
	}
}
