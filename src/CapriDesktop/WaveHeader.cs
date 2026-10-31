using System;

namespace CapriDesktop;

internal struct WaveHeader
{
	internal IntPtr Data;

	internal int Length;

	internal int Recorded;

	internal IntPtr User;

	internal int Flags;

	internal int Loops;

	internal IntPtr Next;

	internal IntPtr Reserved;
}
