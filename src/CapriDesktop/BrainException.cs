using System;

namespace CapriDesktop;

internal sealed class BrainException : Exception
{
	internal readonly bool Told;

	internal BrainException(string message, bool told = false)
		: base(told ? message : (Brain.Plain(message) ?? message))
	{
		Told = told || Brain.Plain(message) != null;
	}
}
