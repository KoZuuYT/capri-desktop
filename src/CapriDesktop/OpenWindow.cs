using System;

namespace CapriDesktop;

internal sealed class OpenWindow
{
	internal readonly IntPtr Handle;

	internal readonly string Title;

	internal readonly string Program;

	internal OpenWindow(IntPtr handle, string title, string program)
	{
		Handle = handle;
		Title = title;
		Program = program;
	}
}
