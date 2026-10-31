using System;

namespace CapriDesktop;

internal sealed class ClaudeCodeException : Exception
{
	internal ClaudeCodeException(string message)
		: base(message)
	{
	}
}
