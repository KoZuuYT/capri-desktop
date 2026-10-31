using System;

namespace CapriDesktop;

internal sealed class StandInDictation : Dictation
{
	internal string Problem;

	internal bool Late;

	internal bool Started;

	internal bool Held;

	internal bool Disposed;

	internal Action<string> Pending;

	internal override void Start(Action<string> started)
	{
		Started = true;
		if (Late)
		{
			Pending = started;
		}
		else
		{
			started(Problem);
		}
	}

	internal override void Hold(bool held)
	{
		Held = held;
	}

	public override void Dispose()
	{
		Disposed = true;
	}
}
