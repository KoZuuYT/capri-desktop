using System;
using System.Drawing;
using System.IO;
using System.Media;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CapriDesktop;

internal sealed class Notice : Form
{
	private const int Wide = 440;

	private readonly string caption;

	private readonly string message;

	private readonly Label title;

	private readonly Label body;

	private readonly Plate ok;

	internal string Said => title.Text + "\n" + body.Text;

	internal bool Fits
	{
		get
		{
			if (body.PreferredSize.Height <= body.Height && body.Right <= ClientSize.Width && ok.Bottom <= ClientSize.Height)
			{
				return ok.Top >= body.Bottom;
			}
			return false;
		}
	}

	internal static void Tell(IWin32Window owner, string message, string caption, bool error)
	{
		try
		{
			using Notice notice = new Notice(message, caption, error);
			if (owner == null)
			{
				notice.ShowInTaskbar = true;
				notice.StartPosition = FormStartPosition.CenterScreen;
			}
			if (error)
			{
				SystemSounds.Hand.Play();
			}
			else
			{
				SystemSounds.Asterisk.Play();
			}
			notice.ShowDialog(owner);
		}
		catch (Exception)
		{
			MessageBoxIcon messageBoxIcon = (error ? MessageBoxIcon.Hand : MessageBoxIcon.Asterisk);
			if (owner == null)
			{
				MessageBox.Show(message, caption, MessageBoxButtons.OK, messageBoxIcon);
			}
			else
			{
				MessageBox.Show(owner, message, caption, MessageBoxButtons.OK, messageBoxIcon);
			}
		}
	}

	internal Notice(string message, string caption, bool error)
	{
		this.message = message;
		this.caption = caption;
		Text = caption;
		FormBorderStyle = FormBorderStyle.None;
		StartPosition = FormStartPosition.CenterParent;
		ShowInTaskbar = false;
		MaximizeBox = false;
		MinimizeBox = false;
		KeyPreview = true;
		AutoScaleMode = AutoScaleMode.None;
		BackColor = Neon.Ground;
		ForeColor = Neon.White;
		DoubleBuffered = true;
		float num = (float)DeviceDpi / 96f;
		Font = Neon.Type(10.5f);
		using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Capri.Chip.ico"))
		{
			if (stream != null)
			{
				Icon = new Icon(stream);
			}
		}
		int num2 = Px(440f, num);
		int num3 = Px(26f, num);
		title = new Neon.Caption();
		title.Text = caption.ToUpperInvariant();
		title.Font = Neon.Display(15f);
		title.ForeColor = (error ? Neon.Hot : Neon.Rose);
		title.AutoSize = true;
		title.Location = new Point(num3, Px(18f, num));
		Controls.Add(title);
		body = new Neon.Caption();
		body.Text = message;
		body.Font = Font;
		body.ForeColor = Neon.White;
		body.MaximumSize = new Size(num2 - 2 * num3, 0);
		body.AutoSize = true;
		body.Location = new Point(num3, title.Bottom + Px(12f, num));
		Controls.Add(body);
		ok = new Plate();
		ok.Text = "OK";
		ok.Accent = Neon.Cyan;
		ok.Font = Neon.Type(10f, bold: true);
		ok.Size = new Size(Px(96f, num), Px(30f, num));
		ok.Location = new Point(num2 - num3 - ok.Width, body.Bottom + Px(22f, num));
		ok.DialogResult = DialogResult.OK;
		Controls.Add(ok);
		AcceptButton = ok;
		CancelButton = ok;
		ClientSize = new Size(num2, ok.Bottom + Px(22f, num));
		Neon.Clip(this, 16f * num, 6f * num);
	}

	private static int Px(float value, float s)
	{
		return (int)Math.Round(value * s);
	}

	protected override void OnShown(EventArgs e)
	{
		base.OnShown(e);
		ok.Focus();
	}

	protected override void OnKeyDown(KeyEventArgs e)
	{
		if (e.Control && e.KeyCode == Keys.C)
		{
			try
			{
				Clipboard.SetText(caption + Environment.NewLine + Environment.NewLine + message);
			}
			catch (ExternalException)
			{
			}
			e.Handled = true;
		}
		base.OnKeyDown(e);
	}

	protected override void WndProc(ref Message message)
	{
		base.WndProc(ref message);
		if (message.Msg == 132 && message.Result == new IntPtr(1))
		{
			message.Result = new IntPtr(2);
		}
	}

	protected override void OnPaintBackground(PaintEventArgs e)
	{
		Neon.Backdrop(e.Graphics, ClientSize, (float)(210 * DeviceDpi) / 96f);
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		base.OnPaint(e);
		float num = (float)DeviceDpi / 96f;
		Neon.Frame(e.Graphics, ClientSize, num, 16f * num, 6f * num, 52f * num, 26f * num);
	}
}
