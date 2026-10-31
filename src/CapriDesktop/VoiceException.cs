using System;

namespace CapriDesktop;

internal sealed class VoiceException : Exception
{
	internal readonly int Status;

	internal VoiceException(string message, int status = 0)
		: base(message)
	{
		Status = status;
	}
}
