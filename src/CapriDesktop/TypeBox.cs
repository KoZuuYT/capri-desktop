using System;
using System.Drawing;
using System.Windows.Forms;

namespace CapriDesktop;

internal sealed class TypeBox : Form
{
	private readonly TextBox input;

	private readonly Label prompt;

	private float scale = 1f;

	private Font typed;

	private Font label;

	internal string Draft
	{
		get
		{
			return input.Text;
		}
		set
		{
			input.Text = value;
		}
	}

	internal event Action<string> Submitted;

	internal TypeBox()
	{
		Text = "Talk to Capri";
		FormBorderStyle = FormBorderStyle.None;
		ShowInTaskbar = false;
		StartPosition = FormStartPosition.Manual;
		TopMost = true;
		AutoScaleMode = AutoScaleMode.None;
		BackColor = Neon.Well;
		DoubleBuffered = true;
		prompt = new Label();
		prompt.Text = "YOU";
		prompt.ForeColor = Neon.Rose;
		prompt.BackColor = Neon.Well;
		prompt.AutoSize = true;
		prompt.UseMnemonic = false;
		input = new TextBox();
		input.BorderStyle = BorderStyle.None;
		input.BackColor = Neon.Well;
		input.ForeColor = Neon.White;
		input.MaxLength = 240;
		input.AccessibleName = Text;
		TextBox textBox = input;
		EventHandler value = (object param0, EventArgs param1) =>
		{
			Native.SendMessage(input.Handle, 5377, new IntPtr(1), "Say something to Capri");
		};
		textBox.HandleCreated += value;
		input.KeyDown += (object sender, KeyEventArgs key) =>
		{
			if (key.KeyCode == Keys.Return)
			{
				key.SuppressKeyPress = true;
				Submit();
			}
			else if (key.KeyCode == Keys.Escape)
			{
				key.SuppressKeyPress = true;
				Hide();
			}
		};
		Controls.Add(prompt);
		Controls.Add(input);
		Deactivate += (object param0, EventArgs param1) =>
		{
			Hide();
		};
	}

	internal void Open(Point head, Point feet, Rectangle area, float s, Rectangle above)
	{
		if (typed == null || scale != s)
		{
			scale = s;
			Font font = typed;
			Font font2 = label;
			typed = Neon.Type(15f * s, bold: false, GraphicsUnit.Pixel);
			label = Neon.Type(12.5f * s, bold: true, GraphicsUnit.Pixel);
			input.Font = typed;
			prompt.Font = label;
			if (font != null)
			{
				font.Dispose();
				font2.Dispose();
			}
		}
		int num = (int)SpeechBox.PanelWidth(s);
		int num2 = (int)(44f * s);
		bool flag = (float)(head.Y - num2) - 20f * s < (float)area.Top;
		int num3 = Math.Max(area.Left, Math.Min(area.Right - num, (int)((float)head.X - (float)num * 0.7f)));
		int num4 = (flag ? (feet.Y + (int)(8f * s)) : (head.Y - num2 - (int)(18f * s)));
		Bounds = new Rectangle(num3, num4, num, num2);
		if (!above.IsEmpty)
		{
			Follow(above, area);
		}
		prompt.Location = new Point((int)(24f * s), (num2 - prompt.PreferredHeight) / 2);
		input.Location = new Point(prompt.Right + (int)(10f * s), (num2 - input.PreferredHeight) / 2 + (int)s);
		input.Width = num - input.Left - (int)(16f * s);
		Neon.Clip(this, 10f * s, 4f * s);
		Show();
		Activate();
		input.Focus();
		input.SelectionStart = input.Text.Length;
	}

	internal void Follow(Rectangle above, Rectangle area)
	{
		int num = above.Top - Height - (int)(16f * scale);
		if (num < area.Top)
		{
			num = above.Bottom + (int)(8f * scale);
		}
		Point point = new Point(above.Left, num);
		if (Location != point)
		{
			Location = point;
		}
	}

	private void Submit()
	{
		string text = input.Text.Trim();
		if (text.Length != 0)
		{
			input.Text = "";
			if (Submitted != null)
			{
				Submitted(text);
			}
		}
	}

	internal void Send(string text)
	{
		input.Text = text;
		Submit();
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		base.OnPaint(e);
		float num = scale;
		Neon.Frame(e.Graphics, Size, num, 10f * num, 4f * num, 36f * num, 10f * num);
		using Brush brush = new SolidBrush(Neon.Hot);
		e.Graphics.FillRectangle(brush, 12f * num, 12f * num, 2.5f * num, (float)Height - 24f * num);
	}
}
