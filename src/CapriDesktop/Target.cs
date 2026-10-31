using System.Drawing;

namespace CapriDesktop;

internal sealed class Target
{
	internal int Number;

	internal string Role;

	internal string Name;

	internal string Value;

	internal Rectangle Box;

	internal bool Secret;

	internal bool Focused;

	internal bool Line;

	internal bool Page;

	internal Point Middle => new Point(Box.Left + Box.Width / 2, Box.Top + Box.Height / 2);

	internal string Label => "[" + Number + "] " + Role + ((Name.Length > 0) ? (" " + Voice.Quote(Name)) : "");
}
