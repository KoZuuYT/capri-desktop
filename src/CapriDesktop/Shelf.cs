using System.Drawing;
using System.Windows.Forms;

namespace CapriDesktop;

internal sealed class Shelf : Form
{
	private readonly int extra;

	protected override bool ShowWithoutActivation => true;

	protected override CreateParams CreateParams
	{
		get
		{
			CreateParams createParams = base.CreateParams;
			createParams.ExStyle |= 0x8000008 | extra;
			return createParams;
		}
	}

	internal Shelf(int extended = 0)
	{
		extra = extended;
		FormBorderStyle = FormBorderStyle.None;
		ShowInTaskbar = false;
		StartPosition = FormStartPosition.Manual;
		BackColor = Color.FromArgb(40, 44, 52);
	}
}
