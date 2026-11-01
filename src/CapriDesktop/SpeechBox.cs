using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Windows.Forms;

namespace CapriDesktop;

internal sealed class SpeechBox : Overlay
{
	private const int Columns = 34;

	private const string Speaker = "CAPRI: ";

	internal TalkState State;

	internal string Heard;

	internal string Prompt = "> you: ";

	internal string Reply;

	internal int Revealed;

	internal double Opened = -1.0;

	internal double Leaving = -1.0;

	private readonly Random random = new Random();

	private Rectangle panelOnScreen;

	private bool clean;

	private bool lastCursor;

	private TalkState lastState;

	private string lastHeard;

	private string lastPrompt;

	private string lastReply;

	private int lastRevealed;

	private float lastScale;

	private Point lastHead;

	private Point lastFeet;

	private Rectangle lastArea;

	internal Rectangle PanelOnScreen => panelOnScreen;

	internal event Action Dismissed;

	internal SpeechBox()
		: base("Capri says")
	{
		Cursor = Look.Pointer;
	}

	protected override void OnMouseUp(MouseEventArgs e)
	{
		base.OnMouseUp(e);
		if (e.Button == MouseButtons.Left && Dismissed != null)
		{
			Dismissed();
		}
	}

	internal static int Fit(string text)
	{
		int num = Look.Cells(text);
		if (num <= 420)
		{
			if (num <= 220)
			{
				return 34;
			}
			return 44;
		}
		return 56;
	}

	internal static float PanelWidth(float s)
	{
		using Font font = Look.Mono(14f * s, FontStyle.Regular);
		return 24f * s + 34f * Look.CharWidth(font) + 14f * s;
	}

	internal void Update(double now, float s, Point head, Point feet, Rectangle area)
	{
		float num = 0f;
		if (Opened >= 0.0 && now - Opened < 0.2)
		{
			num = (float)(1.0 - (now - Opened) / 0.2);
		}
		if (Leaving >= 0.0)
		{
			num = (float)Math.Min(1.0, (now - Leaving) / 0.25 + 0.3);
		}
		// mod: a short signal hiccup every few seconds while she talks, like a terminal on a bad line
		if (num == 0f && State == TalkState.Talking && random.NextDouble() < 0.006)
		{
			num = 0.3f;
		}
		bool flag = ((State != TalkState.Thinking && State != TalkState.Talking) ? ((int)(now * 2.0) % 2 == 0) : (State == TalkState.Talking || (int)(now * 2.5) % 2 == 0));
		int num2 = ((State != TalkState.Thinking && Reply != null) ? Math.Min(Reply.Length, Math.Max(0, Revealed)) : 0);
		if (num != 0f || !clean || !Visible || Image == null || flag != lastCursor || State != lastState || !(Heard == lastHeard) || !(Prompt == lastPrompt) || !(Reply == lastReply) || num2 != lastRevealed || s != lastScale || !(head == lastHead) || !(feet == lastFeet) || !(area == lastArea))
		{
			Bitmap bitmap = Render(s, head, feet, area, flag, out var at);
			if (num > 0f)
			{
				Bitmap bitmap2 = Glitch(bitmap, num, s);
				bitmap.Dispose();
				bitmap = bitmap2;
			}
			Present(bitmap, at);
			clean = num == 0f;
			lastCursor = flag;
			lastState = State;
			lastHeard = Heard;
			lastPrompt = Prompt;
			lastReply = Reply;
			lastRevealed = num2;
			lastScale = s;
			lastHead = head;
			lastFeet = feet;
			lastArea = area;
		}
	}

	private Bitmap Render(float s, Point head, Point feet, Rectangle area, bool cursorOn, out Point at)
	{
		string text = "CAPRI: " + (Reply ?? "") + (Heard ?? "");
		using Font font = Look.Mono(14f * s, FontStyle.Regular, GraphicsUnit.Pixel, text);
		using Font font2 = Look.Mono(14f * s, FontStyle.Italic, GraphicsUnit.Pixel, text);
		using Font font3 = Look.Mono(14f * s, FontStyle.Bold, GraphicsUnit.Pixel, text);
		float num = Look.CharWidth(font);
		float num2 = 19f * s;
		float num3 = 12f * s;
		float num4 = 10f * s;
		float num5 = 16f * s;
		int num6 = Fit("CAPRI: " + (Reply ?? ""));
		List<string> list = Look.Wrap("CAPRI: " + (Reply ?? ""), num6);
		int num7 = ((!string.IsNullOrEmpty(Prompt)) ? 1 : 0);
		float num8 = 24f * s + (float)num6 * num + 14f * s;
		// mod: a terminal title bar on top of the text
		float bar = 20f * s;
		float num9 = 13f * s + bar + num2 * (float)(num7 + list.Count) + 10f * s;
		int num10 = (int)Math.Ceiling(num8 + num3 * 2f);
		int num11 = (int)Math.Ceiling(num4 + num9 + num5 + 4f * s);
		bool flag = head.Y - num11 < area.Top;
		Point point = (flag ? feet : head);
		int num12 = Math.Max(area.Left, Math.Min(area.Right - num10, (int)((float)point.X - num3 - num8 * 0.7f)));
		int num13 = (flag ? point.Y : (point.Y - num11));
		at = new Point(num12, num13);
		RectangleF box = new RectangleF(num3, flag ? (num5 + 4f * s) : num4, num8, num9);
		panelOnScreen = new Rectangle(num12 + (int)box.X, num13 + (int)box.Y, (int)box.Width, (int)box.Height);
		float tipX = Math.Max(box.Left + 32f * s, Math.Min(box.Right - 10f * s, point.X - num12));
		float tipY = ((!flag) ? (num11 - 1) : 0);
		Bitmap bitmap = new Bitmap(num10, num11, PixelFormat.Format32bppPArgb);
		using (Graphics graphics = Graphics.FromImage(bitmap))
		{
			using GraphicsPath graphicsPath = Look.Plate(box, 10f * s, tipX, tipY, 18f * s);
			graphics.SmoothingMode = SmoothingMode.AntiAlias;
			graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
			graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
			using (Pen pen = new Pen(Color.FromArgb(46, Look.Cyan), 7f * s))
			{
				graphics.DrawPath(pen, graphicsPath);
			}
			using (Brush brush = new SolidBrush(Color.FromArgb(238, Look.Panel)))
			{
				graphics.FillPath(brush, graphicsPath);
			}
			graphics.SetClip(graphicsPath);
			using (Pen pen2 = new Pen(Color.FromArgb(9, 255, 255, 255), Math.Max(1f, s)))
			{
				for (float num14 = box.Top; num14 < box.Bottom; num14 += 3f * s)
				{
					graphics.DrawLine(pen2, box.Left, num14, box.Right, num14);
				}
			}
			graphics.ResetClip();
			using (Pen pen3 = new Pen(Look.Cyan, 2f * s))
			{
				graphics.DrawPath(pen3, graphicsPath);
			}
			using (Brush brush2 = new SolidBrush(Look.Red))
			{
				graphics.FillRectangle(brush2, box.Left + 10f * s, box.Top + 13f * s + bar, 3f * s, box.Height - 26f * s - bar);
			}
			TitleBar(graphics, box, s, bar);
			float num15 = box.Left + 24f * s;
			float num16 = box.Top + 12f * s + bar;
			if (num7 > 0)
			{
				string text2 = Prompt + (Heard ?? "");
				if (Look.Cells(text2) > num6)
				{
					text2 = Look.Cut(text2, num6 - 3) + "...";
				}
				using (Brush brush3 = new SolidBrush(Look.Dim))
				{
					Look.Draw(graphics, text2, font2, brush3, num15, num16);
				}
				num16 += num2;
			}
			int[] array = Look.Takes(list, "CAPRI: " + (Reply ?? ""), "CAPRI: ".Length + ((State == TalkState.Thinking) ? (Reply ?? "").Length : Math.Max(0, Revealed)));
			PointF pointF = new PointF(num15, num16);
			using Brush brush4 = new SolidBrush(Look.Red);
			using Brush brush5 = new SolidBrush(Look.Neon);
			float num17 = num15 + (float)num6 * num;
			for (int i = 0; i < list.Count && array[i] > 0; i++)
			{
				string text3 = list[i].Substring(0, array[i]);
				int num18 = ((i == 0) ? Math.Min(array[i], "CAPRI: ".Length - 1) : 0);
				if (num18 > 0)
				{
					graphics.DrawString(text3.Substring(0, num18), font3, brush4, num15, num16, StringFormat.GenericTypographic);
				}
				string s2 = text3.Substring(num18);
				float num19 = num15 + (float)num18 * num;
				if (Look.RightToLeft(list[i].Substring(num18)))
				{
					graphics.DrawString(s2, font, brush5, new RectangleF(num19, num16 + (float)i * num2, num17 - num19, num2), Look.Leftward);
					pointF = new PointF(num17 - graphics.MeasureString(s2, font, PointF.Empty, Look.Leftward).Width - num - 2f * s, num16 + (float)i * num2);
				}
				else
				{
					float num20 = Look.Draw(graphics, s2, font, brush5, num19, num16 + (float)i * num2);
					float num21;
					if (Look.Burmese(s2))
					{
						num21 = num20;
					}
					else
					{
						num21 = ((Look.Thai(s2) || Look.Indic(s2)) ? (num19 + graphics.MeasureString(s2, font, PointF.Empty, Look.Spaced).Width) : (num15 + (float)Look.Cells(text3) * num));
					}
					pointF = new PointF(num21, num16 + (float)i * num2);
				}
			}
			if (cursorOn)
			{
				graphics.FillRectangle(brush5, pointF.X + s, pointF.Y + s, num * 0.85f, num2 - 5f * s);
			}
		}
		return bitmap;
	}

	// "capri@k.mod:~$" on the left, a live status and three window squares on the right, and a rule under it
	private void TitleBar(Graphics graphics, RectangleF box, float s, float bar)
	{
		using Font font = Look.Mono(10f * s, FontStyle.Bold, GraphicsUnit.Pixel);
		float y = box.Top + 7f * s;
		using (Brush rose = new SolidBrush(Look.Red))
		{
			graphics.DrawString("capri@k.mod", font, rose, box.Left + 22f * s, y, StringFormat.GenericTypographic);
		}
		float at = box.Left + 22f * s + graphics.MeasureString("capri@k.mod", font, PointF.Empty, StringFormat.GenericTypographic).Width;
		using (Brush dim = new SolidBrush(Look.Dim))
		{
			graphics.DrawString(":~$ tty0", font, dim, at, y, StringFormat.GenericTypographic);
		}
		bool blink = (Environment.TickCount / 400) % 2 == 0;
		string status;
		Color tone;
		switch (State)
		{
		case TalkState.Thinking:
			status = blink ? "PROC..." : "PROC.  ";
			tone = Look.Cyan;
			break;
		case TalkState.Talking:
			status = "TX";
			tone = Look.Red;
			break;
		default:
			status = "RX";
			tone = Look.Dim;
			break;
		}
		float right = box.Right - 12f * s;
		for (int i = 0; i < 3; i++)
		{
			Color square = (i == 0) ? Look.Cyan : ((i == 1) ? Look.Red : Look.Dim);
			using Brush brush = new SolidBrush(Color.FromArgb(200, square));
			graphics.FillRectangle(brush, right - (i + 1) * 9f * s, y + 2f * s, 6f * s, 6f * s);
		}
		float statusWidth = graphics.MeasureString(status, font, PointF.Empty, StringFormat.GenericTypographic).Width;
		float statusX = right - 30f * s - statusWidth;
		if (State == TalkState.Talking && blink)
		{
			using Brush dot = new SolidBrush(tone);
			graphics.FillEllipse(dot, statusX - 9f * s, y + 2f * s, 6f * s, 6f * s);
		}
		using (Brush brush2 = new SolidBrush(tone))
		{
			graphics.DrawString(status, font, brush2, statusX, y, StringFormat.GenericTypographic);
		}
		using Pen rule = new Pen(Color.FromArgb(80, Look.Cyan), Math.Max(1f, s));
		graphics.DrawLine(rule, box.Left + 10f * s, box.Top + bar + 4f * s, box.Right - 10f * s, box.Top + bar + 4f * s);
	}

	private Bitmap Glitch(Bitmap clean, float amount, float s)
	{
		Bitmap bitmap = new Bitmap(clean.Width, clean.Height, PixelFormat.Format32bppPArgb);
		if (random.NextDouble() < 0.25 * (double)amount)
		{
			return bitmap;
		}
		using Graphics graphics = Graphics.FromImage(bitmap);
		int num;
		for (int i = 0; i < clean.Height; i += num)
		{
			num = Math.Min(clean.Height - i, (int)((float)(3 + random.Next(10)) * s));
			int num2 = (int)((random.NextDouble() * 2.0 - 1.0) * 10.0 * (double)s * (double)amount);
			graphics.DrawImage(clean, new Rectangle(num2, i, clean.Width, num), new Rectangle(0, i, clean.Width, num), GraphicsUnit.Pixel);
		}
		for (int j = 0; j < 2; j++)
		{
			int num3 = (int)((float)(2 + random.Next(6)) * s);
			int srcY = random.Next(Math.Max(1, clean.Height - num3));
			int num4 = (int)((random.NextDouble() * 2.0 - 1.0) * 14.0 * (double)s * (double)amount);
			bool flag = j == 0;
			using ImageAttributes imageAttributes = new ImageAttributes();
			imageAttributes.SetColorMatrix(new ColorMatrix(new float[5][]
			{
				new float[5]
				{
					(!flag) ? 1 : 0,
					0f,
					0f,
					0f,
					0f
				},
				new float[5]
				{
					0f,
					flag ? 1 : 0,
					0f,
					0f,
					0f
				},
				new float[5]
				{
					0f,
					0f,
					flag ? 1 : 0,
					0f,
					0f
				},
				new float[5]
				{
					0f,
					0f,
					0f,
					0.6f * amount,
					0f
				},
				new float[5] { 0f, 0f, 0f, 0f, 1f }
			}));
			graphics.DrawImage(clean, new Rectangle(num4, srcY, clean.Width, num3), 0, srcY, clean.Width, num3, GraphicsUnit.Pixel, imageAttributes);
		}
		return bitmap;
	}
}
