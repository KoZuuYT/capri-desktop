using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace CapriDesktop;

// the mod's changelog: neon cards per version that slide in one after another, smooth wheel scrolling and sounds
internal sealed class ChangelogView : Form
{
	private sealed class Entry
	{
		internal string Version;

		internal string Title;

		internal readonly List<string> Lines = new List<string>();

		internal int Top;

		internal int Height;
	}

	private readonly List<Entry> entries = new List<Entry>();

	private readonly Font head;

	private readonly Font body;

	private readonly Font strong;

	private readonly Font tag;

	private readonly float s;

	private readonly System.Windows.Forms.Timer frames = new System.Windows.Forms.Timer { Interval = 30 };

	private readonly Stopwatch clock = Stopwatch.StartNew();

	private float scroll;

	private float scrollTo;

	private int contentTall;

	private int hover = -1;

	private bool closeHot;

	private bool closing;

	private double closingAt;

	private Point dragFrom;

	private bool dragging;

	internal ChangelogView(string markdown, Icon icon, float scale)
	{
		s = scale;
		Text = "Changelog";
		Icon = icon;
		FormBorderStyle = FormBorderStyle.None;
		StartPosition = FormStartPosition.CenterParent;
		ShowInTaskbar = false;
		DoubleBuffered = true;
		BackColor = Neon.Ground;
		ClientSize = new Size(S(560), S(640));
		head = Look.Mono(13f * s, FontStyle.Bold, GraphicsUnit.Pixel);
		body = Look.Mono(11.5f * s, FontStyle.Regular, GraphicsUnit.Pixel);
		strong = Look.Mono(11.5f * s, FontStyle.Bold, GraphicsUnit.Pixel);
		tag = Look.Mono(10f * s, FontStyle.Bold, GraphicsUnit.Pixel);
		Parse(markdown);
		Opacity = 0.0;
		frames.Tick += (object sender, EventArgs e) => Step();
		Neon.Clip(this, 16f * s, 6f * s);
	}

	private int S(float v)
	{
		return (int)Math.Round(v * s);
	}

	private void Parse(string markdown)
	{
		Entry current = null;
		foreach (string raw in markdown.Replace("\r\n", "\n").Split('\n'))
		{
			string line = raw.TrimEnd();
			Match m = Regex.Match(line, "^##\\s+(v[\\d.]+)\\s*[—-]*\\s*(.*)$");
			if (m.Success)
			{
				current = new Entry
				{
					Version = m.Groups[1].Value,
					Title = m.Groups[2].Value
				};
				entries.Add(current);
			}
			else if (current != null && line.Trim().Length > 0)
			{
				current.Lines.Add(line);
			}
		}
	}

	protected override void OnShown(EventArgs e)
	{
		base.OnShown(e);
		Layout(CreateGraphics());
		Sfx.Play(Cue.Arrive);
		clock.Restart();
		frames.Start();
	}

	private void Layout(Graphics g)
	{
		int y = S(78);
		int width = ClientSize.Width - S(84);
		foreach (Entry entry in entries)
		{
			entry.Top = y;
			int h = S(44);
			foreach (string line in entry.Lines)
			{
				string plain = Plain(line, out int indent);
				h += (int)Math.Ceiling(g.MeasureString(plain, body, width - indent).Height) + S(4);
			}
			entry.Height = h + S(10);
			y += entry.Height + S(14);
		}
		contentTall = y + S(10);
	}

	private string Plain(string line, out int indent)
	{
		int spaces = line.Length - line.TrimStart().Length;
		string text = line.Trim();
		indent = S(18 + spaces * 7);
		if (text.StartsWith("- "))
		{
			text = text.Substring(2);
		}
		return text.Replace("**", "");
	}

	private int MaxScroll => Math.Max(0, contentTall - ClientSize.Height);

	private void Step()
	{
		double t = clock.Elapsed.TotalSeconds;
		if (closing)
		{
			double k = (t - closingAt) / 0.18;
			Opacity = Math.Max(0.0, 1.0 - k);
			if (k >= 1.0)
			{
				frames.Stop();
				Close();
				return;
			}
		}
		else
		{
			Opacity = Math.Min(1.0, t / 0.22);
		}
		bool gliding = Math.Abs(scrollTo - scroll) >= 0.5f;
		scroll = gliding ? (scroll + (scrollTo - scroll) * 0.35f) : scrollTo;
		// only repaint while something moves: the intro, a scroll, a close, or the slow NEW pulse
		bool intro = t < 0.6 + entries.Count * 0.07;
		if (intro || gliding || closing || (long)(t * 30.0) % 3 == 0)
		{
			Invalidate();
		}
	}

	private static float Ease(double k)
	{
		k = Math.Max(0.0, Math.Min(1.0, k));
		return (float)(1.0 - Math.Pow(1.0 - k, 3.0));
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		Graphics g = e.Graphics;
		g.SmoothingMode = SmoothingMode.AntiAlias;
		g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
		double t = clock.Elapsed.TotalSeconds;
		int width = ClientSize.Width;
		for (int i = 0; i < entries.Count; i++)
		{
			Entry entry = entries[i];
			float k = Ease((t - 0.12 - i * 0.07) / 0.35);
			if (k <= 0f)
			{
				continue;
			}
			float x = S(28) + (1f - k) * S(60);
			float y = entry.Top - scroll;
			if (y > ClientSize.Height || y + entry.Height < S(60))
			{
				continue;
			}
			int alpha = (int)(255f * k);
			bool hot = i == hover;
			RectangleF card = new RectangleF(x, y, width - S(56), entry.Height);
			using (GraphicsPath path = Neon.Cut(card, 0f, 0f, 10f * s, 0f))
			{
				using (Brush fill = new SolidBrush(Color.FromArgb(alpha * (hot ? 40 : 22) / 255, Neon.Rose)))
				{
					g.FillPath(fill, path);
				}
				using Pen edge = new Pen(Color.FromArgb(alpha * (hot ? 255 : 110) / 255, (i == 0) ? Look.Cyan : Neon.Rose), hot ? 1.6f * s : 1f * s);
				g.DrawPath(edge, path);
			}
			float pulse = (i == 0) ? (float)(0.5 + 0.5 * Math.Sin(t * 4.0)) : 0f;
			using (Brush bar = new SolidBrush(Color.FromArgb(alpha, (i == 0) ? Look.Cyan : Neon.Rose)))
			{
				g.FillRectangle(bar, x, y + S(8), 3f * s, entry.Height - S(16));
			}
			string version = entry.Version + ((i == 0) ? "  NEW" : "");
			SizeF vs = g.MeasureString(version, tag);
			RectangleF chip = new RectangleF(x + S(14), y + S(12), vs.Width + S(8), vs.Height + S(2));
			using (Brush chipFill = new SolidBrush(Color.FromArgb((int)(alpha * (0.75f + 0.25f * pulse)), (i == 0) ? Look.Cyan : Neon.Rose)))
			{
				g.FillRectangle(chipFill, chip);
			}
			using (Brush ink = new SolidBrush(Color.FromArgb(alpha, Neon.Ground)))
			{
				g.DrawString(version, tag, ink, chip.X + S(4), chip.Y + S(1));
			}
			using (Brush title = new SolidBrush(Color.FromArgb(alpha, Neon.White)))
			{
				g.DrawString(entry.Title, head, title, chip.Right + S(10), y + S(10));
			}
			float ly = y + S(40);
			foreach (string line in entry.Lines)
			{
				string plain = Plain(line, out int indent);
				bool bullet = line.TrimStart().StartsWith("- ");
				if (bullet)
				{
					using Brush dot = new SolidBrush(Color.FromArgb(alpha, (indent > S(18)) ? Neon.Faint : Neon.Rose));
					g.FillRectangle(dot, x + S(4) + indent - S(10), ly + S(6), 4f * s, 4f * s);
				}
				Match bold = Regex.Match(line, "\\*\\*(.+?)\\*\\*");
				Color colour = bold.Success ? Look.Cyan : Neon.White;
				RectangleF area = new RectangleF(x + S(4) + indent, ly, card.Width - indent - S(20), ClientSize.Height);
				SizeF size = g.MeasureString(plain, body, (int)area.Width);
				using (Brush text = new SolidBrush(Color.FromArgb(alpha, colour)))
				{
					g.DrawString(plain, bold.Success && bold.Index == line.IndexOf("**") && bold.Index <= line.Length - line.TrimStart().Length + 2 ? strong : body, text, area);
				}
				ly += size.Height + S(4);
			}
		}
		// header on top of the scrolled cards
		using (Brush ground = new SolidBrush(Neon.Ground))
		{
			g.FillRectangle(ground, 0, 0, width, S(64));
		}
		float wipe = Ease(t / 0.5);
		using (Brush rose = new SolidBrush(Neon.Rose))
		{
			g.DrawString("CHANGELOG", Look.Mono(20f * s, FontStyle.Bold, GraphicsUnit.Pixel), rose, S(26), S(16));
			g.FillRectangle(rose, S(28), S(54), (width - S(56)) * wipe, 2f * s);
		}
		using (Brush faint = new SolidBrush(Neon.Faint))
		{
			g.DrawString(entries.Count + " versions", tag, faint, S(200), S(28));
		}
		RectangleF x0 = CloseBox();
		using (Pen cross = new Pen(closeHot ? Neon.Rose : Neon.Faint, 2f * s))
		{
			g.DrawLine(cross, x0.Left + S(7), x0.Top + S(7), x0.Right - S(7), x0.Bottom - S(7));
			g.DrawLine(cross, x0.Right - S(7), x0.Top + S(7), x0.Left + S(7), x0.Bottom - S(7));
		}
		if (MaxScroll > 0)
		{
			float track = ClientSize.Height - S(84);
			float thumb = Math.Max(S(30), track * ClientSize.Height / contentTall);
			float at = S(70) + (track - thumb) * scroll / MaxScroll;
			using Brush b = new SolidBrush(Color.FromArgb(160, Neon.Rose));
			g.FillRectangle(b, width - S(12), at, 3f * s, thumb);
		}
	}

	private RectangleF CloseBox()
	{
		return new RectangleF(ClientSize.Width - S(52), S(14), S(28), S(28));
	}

	protected override void OnPaintBackground(PaintEventArgs e)
	{
		e.Graphics.Clear(Neon.Ground);
	}

	protected override void OnMouseWheel(MouseEventArgs e)
	{
		base.OnMouseWheel(e);
		scrollTo = Math.Max(0, Math.Min(MaxScroll, scrollTo - e.Delta * S(90) / 120));
	}

	protected override void OnMouseMove(MouseEventArgs e)
	{
		base.OnMouseMove(e);
		if (dragging)
		{
			Location = new Point(Location.X + e.X - dragFrom.X, Location.Y + e.Y - dragFrom.Y);
			return;
		}
		bool hotNow = CloseBox().Contains(e.Location);
		int over = -1;
		for (int i = 0; i < entries.Count; i++)
		{
			float y = entries[i].Top - scroll;
			if (e.Y >= S(64) && e.Y >= y && e.Y < y + entries[i].Height)
			{
				over = i;
			}
		}
		if (over != hover && over >= 0)
		{
			Sfx.Play(Cue.Click);
		}
		if (hotNow && !closeHot)
		{
			Sfx.Play(Cue.Click);
		}
		if (over != hover || hotNow != closeHot)
		{
			Invalidate();
		}
		hover = over;
		closeHot = hotNow;
		Cursor = hotNow ? Cursors.Hand : Cursors.Default;
	}

	protected override void OnMouseLeave(EventArgs e)
	{
		base.OnMouseLeave(e);
		hover = -1;
		closeHot = false;
	}

	protected override void OnMouseDown(MouseEventArgs e)
	{
		base.OnMouseDown(e);
		if (CloseBox().Contains(e.Location))
		{
			Leave();
		}
		else if (e.Y < S(64))
		{
			dragging = true;
			dragFrom = e.Location;
		}
	}

	protected override void OnMouseUp(MouseEventArgs e)
	{
		base.OnMouseUp(e);
		dragging = false;
	}

	protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
	{
		switch (keyData)
		{
		case Keys.Escape:
			Leave();
			return true;
		case Keys.Down:
			scrollTo = Math.Min(MaxScroll, scrollTo + S(60));
			return true;
		case Keys.Up:
			scrollTo = Math.Max(0, scrollTo - S(60));
			return true;
		case Keys.Next:
			scrollTo = Math.Min(MaxScroll, scrollTo + ClientSize.Height * 0.8f);
			return true;
		case Keys.Prior:
			scrollTo = Math.Max(0, scrollTo - ClientSize.Height * 0.8f);
			return true;
		default:
			return base.ProcessCmdKey(ref msg, keyData);
		}
	}

	private void Leave()
	{
		if (!closing)
		{
			closing = true;
			closingAt = clock.Elapsed.TotalSeconds;
			Sfx.Play(Cue.Leave);
		}
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			frames.Dispose();
			head.Dispose();
			body.Dispose();
			strong.Dispose();
			tag.Dispose();
		}
		base.Dispose(disposing);
	}
}
