using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace CapriDesktop;

internal sealed class TaskPanel : Overlay
{
	private const int Columns = 36;

	private const int MostRows = 7;

	private const string Speaker = "CAPRI: ";

	private readonly Timer ticks = new Timer();

	private readonly Stopwatch watch = Stopwatch.StartNew();

	private readonly List<JobStep> steps = new List<JobStep>();

	private object owner;

	private string words = "";

	private string busy;

	private string outcome;

	private string ending = "";

	private double endedAt = -1.0;

	private Rectangle area;

	private float scale = 1f;

	private string drawn;

	private Point? corner;

	private bool grabbing;

	private Point grabbed;

	private Point grabbedAt;

	internal int Room;

	internal uint Affinity
	{
		get
		{
			if (!IsHandleCreated || !GetWindowDisplayAffinity(Handle, out var affinity))
			{
				return 0u;
			}
			return affinity;
		}
	}

	internal Rectangle Plate
	{
		get
		{
			int glow = Glow;
			if (!Visible)
			{
				return Rectangle.Empty;
			}
			return new Rectangle(Left + glow, Top + glow, Width - glow * 2, Height - glow * 2);
		}
	}

	private int Glow => (int)Math.Round(12f * scale);

	internal bool Showing => owner != null;

	internal string Outcome => outcome;

	internal int Rows => steps.Count;

	private double Now => watch.Elapsed.TotalSeconds;

	internal event Action Moved;

	internal TaskPanel()
		: base("Capri task")
	{
		Cursor = Cursors.SizeAll;
		ticks.Interval = 50;
		Timer timer = ticks;
		EventHandler value = (object param0, EventArgs param1) =>
		{
			Step();
		};
		timer.Tick += value;
	}

	[DllImport("user32.dll")]
	private static extern bool SetWindowDisplayAffinity(IntPtr window, uint affinity);

	[DllImport("user32.dll")]
	private static extern bool GetWindowDisplayAffinity(IntPtr window, out uint affinity);

	internal bool Unseen(bool unseen)
	{
		if (!IsHandleCreated)
		{
			return false;
		}
		try
		{
			SetWindowDisplayAffinity(Handle, unseen ? 17u : 0u);
		}
		catch (EntryPointNotFoundException)
		{
			return false;
		}
		if (unseen)
		{
			return Visible;
		}
		return false;
	}

	protected override void OnMouseDown(MouseEventArgs e)
	{
		base.OnMouseDown(e);
		if (e.Button == MouseButtons.Left)
		{
			Grab(PointToScreen(e.Location));
		}
	}

	protected override void OnMouseMove(MouseEventArgs e)
	{
		base.OnMouseMove(e);
		Haul(PointToScreen(e.Location));
	}

	protected override void OnMouseUp(MouseEventArgs e)
	{
		base.OnMouseUp(e);
		LetGo();
	}

	protected override void OnMouseCaptureChanged(EventArgs e)
	{
		base.OnMouseCaptureChanged(e);
		if (!Capture)
		{
			grabbing = false;
		}
	}

	internal void Grab(Point at)
	{
		grabbing = true;
		grabbed = at;
		grabbedAt = Location;
		Capture = true;
	}

	internal void Haul(Point at)
	{
		if (grabbing)
		{
			MoveTo(new Point(grabbedAt.X + at.X - grabbed.X, grabbedAt.Y + at.Y - grabbed.Y));
		}
	}

	internal void LetGo()
	{
		grabbing = false;
		if (Capture)
		{
			Capture = false;
		}
	}

	internal void MoveTo(Point topLeft)
	{
		Bitmap image = Image;
		if (image == null || !Visible)
		{
			return;
		}
		int glow = Glow;
		corner = new Point(topLeft.X + glow, topLeft.Y + image.Height - glow);
		Point point = Spot(image.Size);
		corner = new Point(point.X + glow, point.Y + image.Height - glow);
		if (!(Location == point))
		{
			Location = point;
			if (Moved != null)
			{
				Moved();
			}
		}
	}

	internal void ToCorner()
	{
		corner = null;
	}

	private Point Spot(Size size)
	{
		int glow = Glow;
		Rectangle rectangle = (corner.HasValue ? Screen.FromPoint(corner.Value).WorkingArea : area);
		Point point = (corner.HasValue ? new Point(corner.Value.X - glow, corner.Value.Y + glow - size.Height) : new Point(area.Right - size.Width - (int)(8f * scale), area.Bottom - size.Height - (int)(8f * scale)));
		return new Point(Math.Max(rectangle.Left - glow, Math.Min(point.X, rectangle.Right + glow - size.Width)), Math.Max(rectangle.Top - glow + Room, Math.Min(point.Y, rectangle.Bottom + glow - size.Height)));
	}

	internal void Begin(object job, string task, Rectangle screen)
	{
		if (job != owner)
		{
			steps.Clear();
			words = task ?? "";
		}
		owner = job;
		outcome = null;
		ending = "";
		endedAt = -1.0;
		busy = null;
		area = screen;
		using (Graphics graphics = Graphics.FromHwnd(IntPtr.Zero))
		{
			scale = Math.Max(1f, graphics.DpiX / 96f);
		}
		ticks.Start();
		Step();
	}

	internal void Busy(object job, string what)
	{
		if (job == owner && outcome == null)
		{
			busy = what;
			Step();
		}
	}

	internal void Add(object job, JobStep step)
	{
		if (job == owner && step != null)
		{
			if (steps.Count > 0 && !steps[steps.Count - 1].Finished)
			{
				steps[steps.Count - 1] = step;
			}
			else
			{
				steps.Add(step);
			}
			busy = null;
			Step();
		}
	}

	internal void End(object job, string how, string said)
	{
		if (job == owner && owner != null)
		{
			outcome = how ?? "failed";
			ending = Regex.Replace(said ?? "", "\\s+", " ").Trim();
			busy = null;
			endedAt = Now;
			if (steps.Count > 0 && !steps[steps.Count - 1].Finished)
			{
				steps.RemoveAt(steps.Count - 1);
			}
			Step();
		}
	}

	internal void Stop()
	{
		ticks.Stop();
		owner = null;
		steps.Clear();
		if (Visible)
		{
			Hide();
		}
	}

	private void Step()
	{
		if (owner == null)
		{
			ticks.Stop();
			return;
		}
		double num = ((outcome == "done") ? 4 : ((outcome == "waiting" || outcome == "asked") ? 8 : 6));
		double num2 = ((outcome == null) ? 0.0 : ((Now - endedAt - num) / 0.4));
		if (num2 >= 1.0)
		{
			Stop();
			return;
		}
		bool flag = outcome == null && (int)(Now * 2.5) % 2 == 0;
		double fade = 1.0 - Math.Max(0.0, num2);
		string text = string.Concat(outcome, "|", busy, "|", steps.Count, "|", (steps.Count > 0) ? (steps[steps.Count - 1].Label + steps[steps.Count - 1].Finished) : "", "|", flag, "|", fade.ToString("0.00"), "|", area, "|", Room);
		if (!(text == drawn) || !Visible)
		{
			drawn = text;
			Bitmap bitmap = Frame(words, steps, busy, outcome, ending, flag, fade, scale);
			Point location = Location;
			Present(bitmap, Spot(bitmap.Size));
			if (Location != location && Moved != null)
			{
				Moved();
			}
		}
	}

	internal static Bitmap Frame(string task, IList<JobStep> steps, string busy, string outcome, string ending, bool caret, double fade, float s)
	{
		string text;
		if (outcome == null)
		{
			text = "working on your task.";
		}
		else if (ending.Length > 0)
		{
			text = ending;
		}
		else
		{
			text = ((outcome == "done") ? "done." : "stopped.");
		}
		string text2 = "CAPRI: " + text;
		StringBuilder stringBuilder = new StringBuilder(task + text2 + busy);
		foreach (JobStep step in steps)
		{
			stringBuilder.Append(step.Label);
		}
		using Font font = Look.Mono(14f * s, FontStyle.Regular, GraphicsUnit.Pixel, stringBuilder.ToString());
		using Font font2 = Look.Mono(14f * s, FontStyle.Italic, GraphicsUnit.Pixel, stringBuilder.ToString());
		using Font font3 = Look.Mono(14f * s, FontStyle.Bold, GraphicsUnit.Pixel, stringBuilder.ToString());
		float num = Look.CharWidth(font);
		float num2 = 19f * s;
		float num3 = 12f * s;
		List<string> list = Look.Wrap("> you: " + Regex.Replace(task ?? "", "\\s+", " ").Trim(), 36);
		if (list.Count > 2)
		{
			list.RemoveRange(2, list.Count - 2);
			list[1] = Look.Cut(list[1], 33) + "...";
		}
		List<string> list2 = Look.Wrap(text2, 36);
		if (list2.Count > 3)
		{
			list2.RemoveRange(3, list2.Count - 3);
			list2[2] = Look.Cut(list2[2], 33) + "...";
		}
		int num4 = ((steps.Count > 7) ? (steps.Count - 6) : 0);
		List<string> list3 = new List<string>();
		List<Color> list4 = new List<Color>();
		if (num4 > 0)
		{
			list3.Add("  +" + num4 + " earlier steps");
			list4.Add(Look.Dim);
		}
		for (int i = ((num4 > 0) ? (steps.Count - 6) : 0); i < steps.Count; i++)
		{
			list3.Add((steps[i].Finished ? "• " : "> ") + steps[i].Label + (steps[i].Finished ? "" : "..."));
			List<Color> list5 = list4;
			Color item;
			if (steps[i].Failed)
			{
				item = Look.Red;
			}
			else
			{
				item = (steps[i].Finished ? Look.Dim : Look.Neon);
			}
			list5.Add(item);
		}
		bool flag = outcome == null && busy != null;
		if (flag)
		{
			list3.Add("> " + busy);
			list4.Add(Look.Neon);
		}
		for (int j = 0; j < list3.Count; j++)
		{
			if (Look.Cells(list3[j]) > 36)
			{
				list3[j] = Look.Cut(list3[j], 33) + "...";
			}
		}
		bool flag2 = outcome == null;
		float num5 = 30f * s + 36f * num;
		float num6 = 12f * s + num2 * (float)(list.Count + list2.Count + list3.Count) + ((list3.Count > 0) ? (6f * s) : 0f) + (flag2 ? num2 : 0f) + 8f * s;
		Bitmap bitmap = new Bitmap((int)Math.Ceiling(num5 + num3 * 2f), (int)Math.Ceiling(num6 + num3 * 2f), PixelFormat.Format32bppPArgb);
		int num7 = (int)(255.0 * Math.Max(0.0, Math.Min(1.0, fade)));
		if (num7 == 0)
		{
			return bitmap;
		}
		RectangleF box = new RectangleF(num3, num3, num5, num6);
		using (Graphics graphics = Graphics.FromImage(bitmap))
		{
			using GraphicsPath graphicsPath = Look.Plate(box, 10f * s, -1f, box.Top + box.Height / 2f, 0f, both: true);
			graphics.SmoothingMode = SmoothingMode.AntiAlias;
			graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
			graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
			using (Pen pen = new Pen(Color.FromArgb(46 * num7 / 255, Look.Cyan), 7f * s))
			{
				graphics.DrawPath(pen, graphicsPath);
			}
			using (Brush brush = new SolidBrush(Color.FromArgb(238 * num7 / 255, Look.Panel)))
			{
				graphics.FillPath(brush, graphicsPath);
			}
			graphics.SetClip(graphicsPath);
			using (Pen pen2 = new Pen(Color.FromArgb(9 * num7 / 255, 255, 255, 255), Math.Max(1f, s)))
			{
				for (float num8 = box.Top; num8 < box.Bottom; num8 += 3f * s)
				{
					graphics.DrawLine(pen2, box.Left, num8, box.Right, num8);
				}
			}
			graphics.ResetClip();
			using (Pen pen3 = new Pen(Color.FromArgb(num7, Look.Cyan), 2f * s))
			{
				graphics.DrawPath(pen3, graphicsPath);
			}
			float num9 = box.Left + 15f * s;
			float num10 = box.Top + 12f * s;
			using (Brush brush2 = new SolidBrush(Color.FromArgb(num7, Look.Dim)))
			{
				foreach (string item2 in list)
				{
					Look.Draw(graphics, item2, font2, brush2, num9, num10);
					num10 += num2;
				}
			}
			using (Brush brush3 = new SolidBrush(Color.FromArgb(num7, Look.Red)))
			{
				using Brush brush4 = new SolidBrush(Color.FromArgb(num7, Look.Neon));
				for (int k = 0; k < list2.Count; k++)
				{
					int num11 = ((k == 0) ? Math.Min(list2[k].Length, "CAPRI: ".Length - 1) : 0);
					if (num11 > 0)
					{
						graphics.DrawString(list2[k].Substring(0, num11), font3, brush3, num9, num10, StringFormat.GenericTypographic);
					}
					Look.Draw(graphics, list2[k].Substring(num11), font, brush4, num9 + (float)num11 * num, num10);
					num10 += num2;
				}
			}
			if (list3.Count > 0)
			{
				num10 += 6f * s;
			}
			for (int l = 0; l < list3.Count; l++)
			{
				using (Brush brush5 = new SolidBrush(Color.FromArgb(num7, list4[l])))
				{
					Look.Draw(graphics, list3[l], font, brush5, num9, num10);
				}
				if (flag && caret && l == list3.Count - 1)
				{
					using Brush brush6 = new SolidBrush(Color.FromArgb(num7, Look.Neon));
					graphics.FillRectangle(brush6, num9 + (float)Look.Cells(list3[l]) * num + 2f * s, num10 + s, num * 0.85f, num2 - 5f * s);
				}
				num10 += num2;
			}
			if (flag2)
			{
				using Font font4 = Look.Mono(12f * s, FontStyle.Regular);
				using Brush brush7 = new SolidBrush(Color.FromArgb(num7, Look.Dim));
				graphics.DrawString("ESC STOPS ME", font4, brush7, box.Right - 15f * s - graphics.MeasureString("ESC STOPS ME", font4, PointF.Empty, StringFormat.GenericTypographic).Width, num10 + 2f * s, StringFormat.GenericTypographic);
			}
		}
		return bitmap;
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			ticks.Dispose();
		}
		base.Dispose(disposing);
	}
}
