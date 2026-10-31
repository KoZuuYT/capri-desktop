using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace CapriDesktop;

internal sealed class Mainframe : Form, IMessageFilter
{
	private static class Panels
	{
		private struct Luid
		{
			internal uint Low;

			internal int High;
		}

		private struct PathInfo
		{
			internal Luid SourceAdapter;

			internal uint SourceId;

			internal uint SourceMode;

			internal uint SourceFlags;

			internal Luid TargetAdapter;

			internal uint TargetId;

			internal uint TargetMode;

			internal uint Technology;

			internal uint Rotation;

			internal uint Scaling;

			internal uint RefreshNumerator;

			internal uint RefreshDenominator;

			internal uint ScanLines;

			internal int Available;

			internal uint TargetFlags;

			internal uint Flags;
		}

		[StructLayout(LayoutKind.Explicit, Size = 64)]
		private struct ModeInfo
		{
			[FieldOffset(0)]
			internal uint Kind;

			[FieldOffset(16)]
			internal uint Width;

			[FieldOffset(20)]
			internal uint Height;

			[FieldOffset(28)]
			internal int X;

			[FieldOffset(32)]
			internal int Y;

			[FieldOffset(40)]
			internal uint ActiveWidth;
		}

		[StructLayout(LayoutKind.Explicit, Size = 80)]
		private struct PreferredMode
		{
			[FieldOffset(0)]
			internal uint Type;

			[FieldOffset(4)]
			internal uint Size;

			[FieldOffset(8)]
			internal Luid Adapter;

			[FieldOffset(16)]
			internal uint Id;

			[FieldOffset(20)]
			internal uint Width;
		}

		[DllImport("user32.dll")]
		private static extern int GetDisplayConfigBufferSizes(uint flags, out uint paths, out uint modes);

		[DllImport("user32.dll")]
		private static extern int QueryDisplayConfig(uint flags, ref uint pathCount, [Out] PathInfo[] paths, ref uint modeCount, [Out] ModeInfo[] modes, IntPtr topology);

		[DllImport("user32.dll")]
		private static extern int DisplayConfigGetDeviceInfo(ref PreferredMode request);

		internal static float Shrink(Screen screen)
		{
			try
			{
				if (GetDisplayConfigBufferSizes(2u, out var paths, out var modes) != 0)
				{
					return 1f;
				}
				PathInfo[] array = new PathInfo[paths];
				ModeInfo[] array2 = new ModeInfo[modes];
				if (QueryDisplayConfig(2u, ref paths, array, ref modes, array2, IntPtr.Zero) != 0)
				{
					return 1f;
				}
				for (int i = 0; i < paths; i++)
				{
					PathInfo pathInfo = array[i];
					if (pathInfo.SourceMode >= modes || array2[pathInfo.SourceMode].Kind != 1)
					{
						continue;
					}
					ModeInfo modeInfo = array2[pathInfo.SourceMode];
					if (modeInfo.X == screen.Bounds.X && modeInfo.Y == screen.Bounds.Y)
					{
						uint num = ((pathInfo.TargetMode < modes && array2[pathInfo.TargetMode].Kind == 2) ? array2[pathInfo.TargetMode].ActiveWidth : 0u);
						PreferredMode request = new PreferredMode
						{
							Type = 3u,
							Size = 80u,
							Adapter = pathInfo.TargetAdapter,
							Id = pathInfo.TargetId
						};
						if (DisplayConfigGetDeviceInfo(ref request) == 0 && request.Width != 0)
						{
							num = ((num == 0) ? request.Width : Math.Min(num, request.Width));
						}
						uint num2 = ((pathInfo.Rotation == 2 || pathInfo.Rotation == 4) ? modeInfo.Height : modeInfo.Width);
						return (num == 0) ? 1f : Math.Max(1f, (float)num2 / (float)num);
					}
				}
			}
			catch (Exception)
			{
			}
			return 1f;
		}
	}

	private sealed class Note : Label
	{
		internal Note()
		{
			SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
		}

		protected override void OnPaintBackground(PaintEventArgs e)
		{
			Backdrop(e.Graphics, this, ScaleOf(this));
		}

		protected override void WndProc(ref Message message)
		{
			if (message.Msg == 132)
			{
				message.Result = new IntPtr(-1);
			}
			else
			{
				base.WndProc(ref message);
			}
		}
	}

	private sealed class Head : Label
	{
		internal int Span;

		private string Shown
		{
			get
			{
				if (!Text.EndsWith(" >", StringComparison.Ordinal))
				{
					return Text;
				}
				return Text.Substring(0, Text.Length - 2);
			}
		}

		internal Head()
		{
			SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
			AutoSize = true;
		}

		internal RectangleF Band(float s)
		{
			return new RectangleF((float)Left - 14f * s, (float)Top - 5f * s, (float)Span * s, 24f * s);
		}

		public override Size GetPreferredSize(Size proposed)
		{
			Size size = TextRenderer.MeasureText(Shown, Font, Size.Empty, TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine);
			return new Size(size.Width + Padding.Horizontal, size.Height + Padding.Vertical);
		}

		protected override void OnPaintBackground(PaintEventArgs e)
		{
			Backdrop(e.Graphics, this, ScaleOf(this));
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			TextRenderer.DrawText(e.Graphics, Shown, Font, ClientRectangle, ForeColor, TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter);
		}

		protected override void WndProc(ref Message message)
		{
			if (message.Msg == 132)
			{
				message.Result = new IntPtr(-1);
			}
			else
			{
				base.WndProc(ref message);
			}
		}
	}

	private sealed class Rim : Label
	{
		internal Rim()
		{
			SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
			AutoSize = false;
			BackColor = Ground;
			Visible = false;
		}

		protected override void OnPaintBackground(PaintEventArgs e)
		{
			Backdrop(e.Graphics, this, ((Mainframe)Parent).Unit());
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			Mainframe mainframe = (Mainframe)Parent;
			e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
			e.Graphics.TranslateTransform(-Left, -Top);
			Edging(e.Graphics, mainframe, mainframe.Unit());
		}

		protected override void WndProc(ref Message message)
		{
			if (message.Msg == 132)
			{
				message.Result = new IntPtr(-1);
			}
			else
			{
				base.WndProc(ref message);
			}
		}
	}

	private sealed class Key : Button
	{
		internal Color Accent = Rose;

		internal bool Loud;

		internal bool Picked;

		internal bool Chevron;

		internal int Glyph;

		private bool hot;

		private bool down;

		internal Key()
		{
			SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
			BackColor = Ground;
			Cursor = Cursors.Hand;
		}

		protected override void OnMouseEnter(EventArgs e)
		{
			hot = true;
			Invalidate();
			base.OnMouseEnter(e);
		}

		protected override void OnMouseLeave(EventArgs e)
		{
			hot = false;
			down = false;
			Invalidate();
			base.OnMouseLeave(e);
		}

		protected override void OnMouseDown(MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				down = true;
				Invalidate();
			}
			base.OnMouseDown(e);
		}

		protected override void OnMouseUp(MouseEventArgs e)
		{
			down = false;
			Invalidate();
			base.OnMouseUp(e);
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			float num = ScaleOf(this);
			Graphics graphics = e.Graphics;
			Backdrop(graphics, this, num);
			graphics.SmoothingMode = SmoothingMode.AntiAlias;
			float num2 = Math.Max(1f, num);
			RectangleF box = new RectangleF(num2 / 2f, num2 / 2f, (float)Width - num2, (float)Height - num2);
			float num3 = (float)Width / 2f;
			float num4 = (float)Height / 2f;
			if (Glyph != 0)
			{
				if (hot)
				{
					using GraphicsPath path = Cut(box, 0f, 0f, 4f * num, 0f);
					using Brush brush = new SolidBrush(down ? Pressed : Hover);
					graphics.FillPath(brush, path);
				}
				Color color;
				if (hot)
				{
					color = ((Glyph == 2) ? Hot : Cyan);
				}
				else
				{
					color = Faint;
				}
				using Pen pen = new Pen(color, 1.5f * num);
				if (Glyph == 1)
				{
					graphics.DrawLine(pen, num3 - 5f * num, num4, num3 + 5f * num, num4);
					return;
				}
				graphics.DrawLine(pen, num3 - 4.5f * num, num4 - 4.5f * num, num3 + 4.5f * num, num4 + 4.5f * num);
				graphics.DrawLine(pen, num3 - 4.5f * num, num4 + 4.5f * num, num3 + 4.5f * num, num4 - 4.5f * num);
				return;
			}
			bool flag = hot || (Focused && ShowFocusCues);
			Color color2;
			Color color3;
			Color color4;
			if (!Enabled)
			{
				color2 = Field;
				color3 = Color.FromArgb(52, 22, 32);
				color4 = Color.FromArgb(112, 94, 104);
			}
			else if (Picked)
			{
				color2 = (hot ? Color.FromArgb(160, 248, 251) : Cyan);
				color3 = color2;
				color4 = OnInk;
			}
			else if (Accent == Cyan)
			{
				if (down)
				{
					color2 = Color.FromArgb(18, 62, 67);
				}
				else
				{
					color2 = (hot ? Color.FromArgb(13, 45, 49) : Deep);
				}
				if (flag)
				{
					color3 = Cyan;
				}
				else
				{
					color3 = (Loud ? DeepEdge : Color.FromArgb(28, 84, 92));
				}
				color4 = Cyan;
			}
			else if (Accent == Hot)
			{
				if (down)
				{
					color2 = Color.FromArgb(72, 18, 30);
				}
				else
				{
					color2 = (hot ? Color.FromArgb(54, 16, 28) : Color.FromArgb(34, 12, 22));
				}
				color3 = (flag ? Color.FromArgb(255, 124, 122) : Hot);
				color4 = Color.FromArgb(255, 166, 162);
			}
			else
			{
				if (down)
				{
					color2 = Pressed;
				}
				else
				{
					color2 = (hot ? Hover : Field);
				}
				color3 = (flag ? Hot : Edge);
				color4 = (Chevron ? Cyan : Accent);
			}
			using (GraphicsPath path2 = Cut(box, 0f, 0f, 5f * num, 0f))
			{
				using (Brush brush2 = new SolidBrush(color2))
				{
					graphics.FillPath(brush2, path2);
				}
				using Pen pen2 = new Pen(color3, num2);
				graphics.DrawPath(pen2, path2);
			}
			if (Chevron)
			{
				Drop(graphics, (float)Width - 19f * num, num4, num, Enabled ? Cyan : color4, hot);
			}
			Rectangle bounds = (Chevron ? new Rectangle((int)(10f * num), 0, Width - (int)(32f * num), Height) : ClientRectangle);
			TextFormatFlags flags = (TextFormatFlags)(0x8824 | ((!Chevron) ? 1 : 0));
			TextRenderer.DrawText(graphics, Text, Font, bounds, color4, flags);
		}
	}

	private sealed class Entry : Control
	{
		internal readonly TextBox Box = new TextBox();

		private bool chevron;

		private string cue = "";

		private bool hot;

		internal string Cue
		{
			get
			{
				return cue;
			}
			set
			{
				cue = value ?? "";
				ShowCue();
			}
		}

		internal bool Chevron
		{
			get
			{
				return chevron;
			}
			set
			{
				chevron = value;
				PerformLayout();
				Invalidate();
			}
		}

		internal event EventHandler Drop;

		internal Entry()
		{
			SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
			BackColor = Ground;
			Cursor = Cursors.IBeam;
			Box.BorderStyle = BorderStyle.None;
			Box.BackColor = Well;
			Box.ForeColor = White;
			TextBox box = Box;
			EventHandler value = (object param0, EventArgs param1) =>
			{
				Invalidate();
			};
			box.GotFocus += value;
			Box.LostFocus += (object param0, EventArgs param1) =>
			{
				Invalidate();
			};
			Box.HandleCreated += (object param0, EventArgs param1) =>
			{
				ShowCue();
			};
			Controls.Add(Box);
		}

		private float Unit()
		{
			return ScaleOf(this);
		}

		private bool OnChevron(int x)
		{
			if (Chevron)
			{
				return (float)x >= (float)Width - 28f * Unit();
			}
			return false;
		}

		private void ShowCue()
		{
			if (Box.IsHandleCreated)
			{
				Native.SendMessage(Box.Handle, 5377, new IntPtr(1), cue);
			}
		}

		protected override void OnLayout(LayoutEventArgs e)
		{
			base.OnLayout(e);
			float num = Unit();
			int num2 = (int)(10f * num);
			int num3 = (int)((float)(Chevron ? 28 : 10) * num);
			int preferredHeight = Box.PreferredHeight;
			Box.SetBounds(num2, (Height - preferredHeight) / 2, Math.Max(1, Width - num2 - num3), preferredHeight);
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			bool flag = OnChevron(e.X);
			Cursor = (flag ? Cursors.Hand : Cursors.IBeam);
			if (flag != hot)
			{
				hot = flag;
				Invalidate();
			}
			base.OnMouseMove(e);
		}

		protected override void OnMouseLeave(EventArgs e)
		{
			hot = false;
			Invalidate();
			base.OnMouseLeave(e);
		}

		protected override void OnMouseDown(MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				if (!OnChevron(e.X))
				{
					Box.Focus();
				}
				else if (Drop != null)
				{
					Drop(this, EventArgs.Empty);
				}
			}
			base.OnMouseDown(e);
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			float num = Unit();
			Graphics graphics = e.Graphics;
			Backdrop(graphics, this, num);
			graphics.SmoothingMode = SmoothingMode.AntiAlias;
			float num2 = Math.Max(1f, num);
			using (GraphicsPath path = Cut(new RectangleF(num2 / 2f, num2 / 2f, (float)Width - num2, (float)Height - num2), 0f, 0f, 5f * num, 0f))
			{
				using (Brush brush = new SolidBrush(Well))
				{
					graphics.FillPath(brush, path);
				}
				using Pen pen = new Pen(Box.Focused ? Cyan : WellEdge, num2);
				graphics.DrawPath(pen, path);
			}
			if (Chevron)
			{
				Mainframe.Drop(graphics, (float)Width - 19f * num, (float)Height / 2f, num, Cyan, hot);
			}
		}
	}

	private sealed class Readout
	{
		private readonly Slider slider;

		internal string Text
		{
			get
			{
				return slider.Shown;
			}
			set
			{
				slider.Shown = value;
			}
		}

		internal Color ForeColor
		{
			get
			{
				return slider.Ink;
			}
			set
			{
				slider.Ink = value;
			}
		}

		internal Rectangle Bounds => slider.Bounds;

		internal Readout(Slider slider)
		{
			this.slider = slider;
		}
	}

	private sealed class MenuColors : ProfessionalColorTable
	{
		internal static readonly Color Back = Color.FromArgb(16, 13, 22);

		public override Color ToolStripDropDownBackground => Back;

		public override Color ImageMarginGradientBegin => Back;

		public override Color ImageMarginGradientMiddle => Back;

		public override Color ImageMarginGradientEnd => Back;

		public override Color MenuBorder => Edge;

		public override Color MenuItemSelected => Color.FromArgb(48, 18, 32);

		public override Color MenuItemBorder => Hot;

		public override Color CheckBackground => DeepEdge;

		public override Color CheckSelectedBackground => Color.FromArgb(60, 160, 172);

		public override Color CheckPressedBackground => Color.FromArgb(60, 160, 172);

		public override Color SeparatorDark => Edge;

		public override Color SeparatorLight => Back;
	}

	internal sealed class MenuLook : ToolStripProfessionalRenderer
	{
		internal static readonly object Title = new object();

		internal static readonly Color Panel = MenuColors.Back;

		internal static readonly Color Ink = White;

		private static readonly Color Chosen = Color.FromArgb(48, 18, 32);

		internal MenuLook()
			: base(new MenuColors())
		{
			RoundedEdges = false;
		}

		private static float Unit(ToolStrip strip)
		{
			return (float)strip.DeviceDpi / 96f;
		}

		protected override void Initialize(ToolStrip strip)
		{
			base.Initialize(strip);
			ToolStripDropDown drop = strip as ToolStripDropDown;
			if (drop != null)
			{
				drop.DropShadowEnabled = false;
				drop.SizeChanged += (object param0, EventArgs param1) =>
				{
					Shape(drop);
				};
				Shape(drop);
			}
		}

		private static void Shape(ToolStripDropDown drop)
		{
			if (drop.Width > 0 && drop.Height > 0)
			{
				float num = Unit(drop);
				Neon.Clip(drop, 10f * num, 4f * num);
			}
		}

		protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
		{
			if (!(e.ToolStrip is ToolStripDropDown))
			{
				base.OnRenderToolStripBackground(e);
				return;
			}
			float num = Unit(e.ToolStrip);
			Size size = e.ToolStrip.Size;
			using (Brush brush = new SolidBrush(Panel))
			{
				e.Graphics.FillRectangle(brush, 0, 0, size.Width, size.Height);
			}
			float num2 = Math.Min(size.Height, 90f * num);
			using LinearGradientBrush linearGradientBrush = new LinearGradientBrush(new RectangleF(0f, -1f, 16f, num2 + 2f), Glow, Panel, LinearGradientMode.Vertical);
			linearGradientBrush.Blend = Fade;
			e.Graphics.FillRectangle(linearGradientBrush, 0f, 0f, size.Width, num2);
		}

		protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
		{
			if (!(e.ToolStrip is ToolStripDropDown))
			{
				base.OnRenderImageMargin(e);
			}
		}

		protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
		{
			if (!(e.ToolStrip is ToolStripDropDown))
			{
				base.OnRenderToolStripBorder(e);
				return;
			}
			float num = Unit(e.ToolStrip);
			Neon.Frame(e.Graphics, e.ToolStrip.Size, num, 10f * num, 4f * num, 30f * num, 18f * num);
		}

		protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
		{
			if (!(e.ToolStrip is ToolStripDropDown))
			{
				base.OnRenderMenuItemBackground(e);
				return;
			}
			ToolStripItem item = e.Item;
			if (!item.Enabled || (!item.Selected && !item.Pressed))
			{
				return;
			}
			Graphics graphics = e.Graphics;
			float num = Unit(e.ToolStrip);
			RectangleF box = new RectangleF(4f * num, 1f, (float)item.Width - 8f * num, item.Height - 2);
			SmoothingMode smoothingMode = graphics.SmoothingMode;
			graphics.SmoothingMode = SmoothingMode.AntiAlias;
			using (GraphicsPath path = Cut(box, 0f, 0f, 5f * num, 0f))
			{
				using Brush brush = new SolidBrush(Chosen);
				graphics.FillPath(brush, path);
			}
			graphics.SmoothingMode = smoothingMode;
			using Brush brush2 = new SolidBrush(Hot);
			graphics.FillRectangle(brush2, box.Left, box.Top + 4f * num, 2.5f * num, Math.Max(1f, box.Height - 8f * num));
		}

		protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
		{
			ToolStripItem item = e.Item;
			Color foreColor;
			if (item.Tag == Title)
			{
				foreColor = Rose;
			}
			else if (item.Enabled)
			{
				foreColor = (((item.Selected || item.Pressed) && e.ToolStrip is ToolStripDropDown) ? Cyan : e.TextColor);
			}
			else
			{
				foreColor = Faint;
			}
			TextRenderer.DrawText(e.Graphics, e.Text, e.TextFont, e.TextRectangle, foreColor, e.TextFormat);
		}

		protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
		{
			if (!(e.ToolStrip is ToolStripDropDown))
			{
				base.OnRenderItemCheck(e);
				return;
			}
			Graphics graphics = e.Graphics;
			float num = Unit(e.ToolStrip);
			Rectangle imageRectangle = e.ImageRectangle;
			float num2 = Math.Max(6f * num, (float)Math.Min(imageRectangle.Width, imageRectangle.Height) - 4f * num);
			RectangleF box = new RectangleF((float)imageRectangle.Left + ((float)imageRectangle.Width - num2) / 2f, (float)imageRectangle.Top + ((float)imageRectangle.Height - num2) / 2f, num2, num2);
			SmoothingMode smoothingMode = graphics.SmoothingMode;
			graphics.SmoothingMode = SmoothingMode.AntiAlias;
			using (GraphicsPath path = Cut(box, 0f, 0f, 3f * num, 0f))
			{
				using Brush brush = new SolidBrush(e.Item.Enabled ? Cyan : Faint);
				graphics.FillPath(brush, path);
			}
			using (Pen pen = new Pen(OnInk, 1.6f * num))
			{
				pen.LineJoin = LineJoin.Miter;
				graphics.DrawLines(pen, new PointF[3]
				{
					new PointF(box.Left + num2 * 0.22f, box.Top + num2 * 0.52f),
					new PointF(box.Left + num2 * 0.42f, box.Top + num2 * 0.72f),
					new PointF(box.Left + num2 * 0.78f, box.Top + num2 * 0.3f)
				});
			}
			graphics.SmoothingMode = smoothingMode;
		}

		protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
		{
			ToolStrip toolStrip = ((e.Item != null) ? e.Item.Owner : null);
			if (!(toolStrip is ToolStripDropDown) || (e.Direction != ArrowDirection.Right && e.Direction != ArrowDirection.Left))
			{
				base.OnRenderArrow(e);
				return;
			}
			Graphics graphics = e.Graphics;
			float num = Unit(toolStrip);
			Rectangle arrowRectangle = e.ArrowRectangle;
			float num2 = (float)arrowRectangle.Left + (float)arrowRectangle.Width / 2f - 3f * num;
			float num3 = (float)arrowRectangle.Top + (float)arrowRectangle.Height / 2f;
			PointF[] points = ((e.Direction == ArrowDirection.Right) ? new PointF[3]
			{
				new PointF(num2, num3 - 4f * num),
				new PointF(num2 + 6f * num, num3),
				new PointF(num2, num3 + 4f * num)
			} : new PointF[3]
			{
				new PointF(num2 + 6f * num, num3 - 4f * num),
				new PointF(num2, num3),
				new PointF(num2 + 6f * num, num3 + 4f * num)
			});
			bool flag = e.Item != null && e.Item.Enabled && (e.Item.Selected || e.Item.Pressed);
			SmoothingMode smoothingMode = graphics.SmoothingMode;
			graphics.SmoothingMode = SmoothingMode.AntiAlias;
			if (flag)
			{
				using Brush brush = new SolidBrush(Cyan);
				graphics.FillPolygon(brush, points);
			}
			using (Pen pen = new Pen(flag ? Cyan : Faint, 1.3f * num))
			{
				pen.LineJoin = LineJoin.Miter;
				graphics.DrawPolygon(pen, points);
			}
			graphics.SmoothingMode = smoothingMode;
		}

		protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
		{
			if (!(e.ToolStrip is ToolStripDropDown) || e.Vertical)
			{
				base.OnRenderSeparator(e);
				return;
			}
			float num = Unit(e.ToolStrip);
			using Brush brush = new SolidBrush(Edge);
			e.Graphics.FillRectangle(brush, 10f * num, e.Item.Height / 2, (float)e.Item.Width - 20f * num, Math.Max(1, (int)num));
		}
	}

	private sealed class ServerSheet : Form
	{
		internal const string NewFishVoice = "New voice from a clip...";

		private readonly Mainframe owner;

		private readonly Label title;

		private readonly Label hint;

		private readonly Label from;

		private readonly Key service;

		private readonly ContextMenuStrip services;

		private readonly ContextMenuStrip voices;

		private readonly Label place;

		private readonly Entry url;

		private readonly Label modelName;

		private readonly Entry model;

		private readonly Label voiceName;

		private readonly Entry speaker;

		private readonly Label keyName;

		private readonly Entry key;

		private readonly Key forget;

		private readonly Key browse;

		private readonly Key test;

		private readonly Label note;

		private VoiceServer.Service picked = VoiceServer.Services[0];

		private string savedUrl;

		private string savedApi;

		private string savedTail;

		private bool addressShown = true;

		internal readonly float Zoom;

		internal string HintPlace
		{
			get
			{
				for (int i = 0; i < Controls.Count; i++)
				{
					Control control = Controls[i];
					if (!control.Visible || (control is Label && control.Text.Length == 0))
					{
						continue;
					}
					for (int j = i + 1; j < Controls.Count; j++)
					{
						Control control2 = Controls[j];
						if (control2.Visible && (!(control2 is Label) || control2.Text.Length != 0) && control.Bounds.IntersectsWith(control2.Bounds))
						{
							return "overlap: " + ((control.Text.Length > 0) ? control.Text : control.AccessibleName) + " / " + ((control2.Text.Length > 0) ? control2.Text : control2.AccessibleName);
						}
					}
				}
				return "fits: hint " + hint.Top + " to " + hint.Bottom + ", Voice from at " + from.Top + ", its box at " + service.Top;
			}
		}

		internal string Voices
		{
			get
			{
				List<string> list = new List<string>();
				foreach (ToolStripItem item in voices.Items)
				{
					list.Add((item is ToolStripMenuItem) ? (item.Text + (((ToolStripMenuItem)item).Checked ? " (picked)" : "")) : "-");
				}
				return string.Join(", ", list.ToArray());
			}
		}

		internal string Showing => picked.Name + " | " + url.Box.Text + " | " + model.Box.Text + " | " + speaker.Box.Text + " | " + key.Cue + " | " + (forget.Visible ? "Forget" : "-") + " | " + note.Text;

		internal string Fields => (addressShown ? place.Text : "no address") + " | " + modelName.Text + " | " + voiceName.Text + " | " + (key.Visible ? "Key" : "no key") + " | " + (browse.Visible ? browse.Text : "-");

		internal string Choices
		{
			get
			{
				List<string> list = new List<string>();
				foreach (ToolStripItem item in services.Items)
				{
					list.Add(item.Text);
				}
				return title.Text + " | " + string.Join(", ", list.ToArray());
			}
		}

		internal ServerSheet(Mainframe owner, float zoom)
		{
			ServerSheet serverSheet = this;
			this.owner = owner;
			Zoom = zoom;
			Text = "Any voice model";
			Icon = owner.Icon;
			ShowInTaskbar = false;
			FormBorderStyle = FormBorderStyle.None;
			StartPosition = FormStartPosition.Manual;
			AutoScaleMode = AutoScaleMode.Dpi;
			AutoScaleDimensions = new SizeF(96f, 96f);
			Font = Mainframe.Type(9.5f, false);
			BackColor = Ground;
			ForeColor = White;
			DoubleBuffered = true;
			ClientSize = new Size(380, 420);
			title = Heading(this, "", new Point(24, 15), Display(15f), Rose, 0);
			EventHandler click = (object param0, EventArgs param1) =>
			{
				Close();
			};
			Key key = Make("", 330, 20, 26, 22, click);
			key.Glyph = 2;
			key.AccessibleName = "Close";
			hint = Caption("", 28, 52, Look.Mono(8.5f, FontStyle.Regular, GraphicsUnit.Point), Faint);
			from = Caption("Voice from", 28, 112, Font, Rose);
			services = owner.Dropdown();
			service = Make(picked.Name, 28, 132, 328, 28, (object param0, EventArgs param1) =>
			{
				services.Show(service, new Point(0, service.Height));
			});
			service.Chevron = true;
			service.AccessibleName = "Voice from";
			place = Caption("Address", 28, 168, Font, Rose);
			url = Input(28, 188, 328, "http://localhost:8880/v1", "Voice server address, or a voice model's folder");
			url.Box.TextChanged += (object param0, EventArgs param1) =>
			{
				Kept();
			};
			modelName = Caption("Model", 28, 224, Font, Rose);
			model = Input(28, 244, 160, "Optional", "Model name, optional");
			voiceName = Caption("Voice", 196, 224, Font, Rose);
			speaker = Input(196, 244, 160, "Optional", "Voice name or ID, optional");
			voices = owner.Dropdown();
			speaker.Drop += (object param0, EventArgs param1) =>
			{
				if (picked.Api == "file")
				{
					PickClip();
				}
				else
				{
					ListVoices();
				}
			};
			keyName = Caption("Key", 28, 280, Font, Rose);
			this.key = Input(28, 300, 244, "Optional", "Key. It goes only to this server, as a Bearer key");
			this.key.Box.UseSystemPasswordChar = true;
			forget = Make("Forget", 280, 300, 76, 28, (object param0, EventArgs param1) =>
			{
				owner.capri.ForgetServerKey(serverSheet.savedUrl, serverSheet.savedApi);
				serverSheet.savedTail = null;
				serverSheet.Kept();
				serverSheet.Say("Key forgotten.", problem: false);
			});
			forget.Accent = Hot;
			browse = Make("Browse...", 28, 300, 150, 28, (object param0, EventArgs param1) =>
			{
				Browse();
			});
			browse.Visible = false;
			test = Make("Test", 28, 344, 84, 28, (object param0, EventArgs param1) =>
			{
				Try();
			});
			Key key2 = Make("Use this server", 120, 344, 150, 28, (object param0, EventArgs param1) =>
			{
				Use();
			});
			key2.Accent = Cyan;
			key2.Loud = true;
			AcceptButton = key2;
			CancelButton = Make("Cancel", 278, 344, 78, 28, (object param0, EventArgs param1) =>
			{
				Close();
			});
			note = Caption("", 28, 384, Look.Mono(8.5f, FontStyle.Regular, GraphicsUnit.Point), Faint);
			if (zoom != 1f)
			{
				Grow(this, zoom);
				Scale(new SizeF(zoom, zoom));
			}
		}

		internal void Fill(bool cloud, string wanted)
		{
			bool flag = owner.capri.SavedServer(cloud, out var api, out var text, out var text2, out var text3, out var tail);
			Text = (cloud ? "Cloud voice" : "Custom local voice");
			title.Text = (cloud ? "CLOUD VOICE" : "CUSTOM LOCAL") + " >";
			ShowAddress(!cloud);
			for (int num = services.Items.Count - 1; num >= 0; num--)
			{
				services.Items[num].Dispose();
			}
			VoiceServer.Service[] array = VoiceServer.Services;
			foreach (VoiceServer.Service service in array)
			{
				VoiceServer.Service each = service;
				if (Cloudy(each) == cloud)
				{
					services.Items.Add(Choice(each.Name, chosen: false, (object param0, EventArgs param1) =>
					{
						Pick(each, fill: true);
					}));
				}
			}
			savedUrl = (flag ? text : null);
			savedApi = api;
			savedTail = tail;
			key.Box.Text = "";
			if (flag && (wanted == null || api == wanted))
			{
				url.Box.Text = text ?? "";
				model.Box.Text = text2 ?? "";
				speaker.Box.Text = text3 ?? "";
				Pick(VoiceServer.Find(api, text), fill: false);
			}
			else
			{
				Pick(First(cloud, wanted), fill: true);
			}
			Say("", problem: false);
		}

		private static bool Cloudy(VoiceServer.Service choice)
		{
			if (choice.Url != null)
			{
				return VoiceServer.InCloud(choice.Url, choice.Api);
			}
			return false;
		}

		private void ShowAddress(bool shown)
		{
			if (shown == addressShown)
			{
				return;
			}
			addressShown = shown;
			int num = (int)Math.Round(56f * (float)url.Height / 28f) * (shown ? 1 : (-1));
			Label label = place;
			bool visible = (url.Visible = shown);
			label.Visible = visible;
			foreach (Control control in Controls)
			{
				if (control != place && control != url && control.Top >= place.Top)
				{
					control.Top += num;
				}
			}
			ClientSize = new Size(ClientSize.Width, ClientSize.Height + num);
		}

		private void FitHint()
		{
			int num = Math.Max(hint.Top + (int)Math.Round(64f * (float)url.Height / 28f), hint.Bottom + (int)Math.Round(8f * (float)url.Height / 28f));
			int top = from.Top;
			int num2 = num - top;
			if (num2 == 0)
			{
				return;
			}
			foreach (Control control in Controls)
			{
				if (control.Top >= top)
				{
					control.Top += num2;
				}
			}
			ClientSize = new Size(ClientSize.Width, ClientSize.Height + num2);
		}

		private static VoiceServer.Service First(bool cloud, string wanted)
		{
			VoiceServer.Service[] array = VoiceServer.Services;
			foreach (VoiceServer.Service service in array)
			{
				if (Cloudy(service) == cloud && (wanted == null || service.Api == wanted))
				{
					return service;
				}
			}
			return VoiceServer.Services[0];
		}

		private void Pick(VoiceServer.Service choice, bool fill)
		{
			bool flag = choice.Api == "file";
			bool flag2 = choice != picked && key.Box.Text.Trim().Length > 0;
			if (choice != picked)
			{
				key.Box.Text = "";
			}
			picked = choice;
			service.Text = choice.Name;
			hint.Text = choice.Hint;
			FitHint();
			place.Text = (flag ? "Model folder" : "Address");
			url.Cue = (flag ? "Browse to a voice model" : "http://localhost:8880/v1");
			modelName.Text = (flag ? "Language" : "Model");
			model.Cue = (flag ? "Kokoro only, like es" : "Optional");
			voiceName.Text = (flag ? "Speaker or clip" : "Voice");
			Label label = keyName;
			bool visible = (key.Visible = !flag);
			label.Visible = visible;
			browse.Visible = flag;
			speaker.Cue = choice.VoiceCue;
			speaker.Chevron = flag || (choice.Api == "fish" && choice.Url == "https://api.fish.audio");
			if (fill && choice.Url != null)
			{
				url.Box.Text = choice.Url;
				model.Box.Text = choice.Model;
				speaker.Box.Text = choice.Speaker;
			}
			foreach (ToolStripItem item in services.Items)
			{
				((ToolStripMenuItem)item).Checked = item.Text == choice.Name;
			}
			Kept();
			ServerSheet serverSheet = this;
			string text;
			if (flag2)
			{
				text = (flag ? "Key cleared: a model file needs none." : ("Key cleared: paste the key for " + choice.Name + "."));
			}
			else
			{
				text = "";
			}
			serverSheet.Say(text, problem: false);
		}

		internal void Choose(string name)
		{
			foreach (ToolStripItem item in services.Items)
			{
				if (item.Text == name)
				{
					item.PerformClick();
					return;
				}
			}
			throw new InvalidOperationException("The voice model panel has no " + name + " to pick.");
		}

		private void ListVoices()
		{
			Say("Getting your Fish Audio voices...", problem: false);
			owner.capri.FishVoices(url.Box.Text, key.Box.Text, (List<KeyValuePair<string, string>> found, string problem) =>
			{
				if (!IsDisposed)
				{
					if (problem != null)
					{
						Say(problem, problem: true);
					}
					else
					{
						voices.Items.Clear();
						foreach (KeyValuePair<string, string> item in found)
						{
							string id = item.Key;
							voices.Items.Add(Choice(item.Value, VoiceServer.FishVoice(speaker.Box.Text) == id, (object param0, EventArgs param1) =>
							{
								speaker.Box.Text = id;
							}));
						}
						if (voices.Items.Count > 0)
						{
							voices.Items.Add(new ToolStripSeparator());
						}
						voices.Items.Add(Choice("New voice from a clip...", chosen: false, (object param0, EventArgs param1) =>
						{
							MakeVoice();
						}));
						Say("", problem: false);
						voices.Show(speaker, new Point(0, speaker.Height));
					}
				}
			});
		}

		private void MakeVoice()
		{
			using OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.Title = "Pick 10 to 30 seconds of a voice you have the rights and consent to use";
			openFileDialog.Filter = "Voice clips (*.wav;*.mp3;*.m4a;*.flac)|*.wav;*.mp3;*.m4a;*.flac|All files (*.*)|*.*";
			if (openFileDialog.ShowDialog(this) == DialogResult.OK)
			{
				MakeVoice(openFileDialog.FileName);
			}
		}

		internal void MakeVoice(string clip)
		{
			Say("Making your Fish Audio voice...", problem: false);
			owner.capri.MakeFishVoice(url.Box.Text, key.Box.Text, clip, (string id, string problem) =>
			{
				if (!IsDisposed)
				{
					if (problem != null)
					{
						Say(problem, problem: true);
					}
					else
					{
						speaker.Box.Text = id;
						Say("Made your Fish Audio voice. Press Test to hear it.", problem: false);
					}
				}
			});
		}

		internal void PickVoice(string name)
		{
			foreach (ToolStripItem item in voices.Items)
			{
				if (item.Text == name)
				{
					item.PerformClick();
					return;
				}
			}
			throw new InvalidOperationException("Fish Audio's voice menu has no " + name + ".");
		}

		internal void DropVoices()
		{
			ListVoices();
		}

		private void PickClip()
		{
			using OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.Title = "Pick a clip of a voice you have the rights and consent to use";
			openFileDialog.Filter = "Voice clips (*.wav;*.mp3;*.m4a;*.flac)|*.wav;*.mp3;*.m4a;*.flac|All files (*.*)|*.*";
			if (openFileDialog.ShowDialog(this) == DialogResult.OK)
			{
				TakeClip(openFileDialog.FileName);
			}
		}

		internal void TakeClip(string clip)
		{
			speaker.Box.Text = clip;
			Say("Pocket TTS models copy this clip's voice. Press Test to hear it.", problem: false);
		}

		private void Browse()
		{
			using OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.Title = "Pick a voice model: its .tar.bz2, or the .onnx in its folder";
			openFileDialog.Filter = "Voice models (*.tar.bz2;*.zip;*.onnx)|*.tar.bz2;*.zip;*.onnx|All files (*.*)|*.*";
			if (openFileDialog.ShowDialog(this) == DialogResult.OK)
			{
				Take(openFileDialog.FileName);
			}
		}

		internal void Take(string path)
		{
			if (path.EndsWith(".onnx", StringComparison.OrdinalIgnoreCase))
			{
				url.Box.Text = Path.GetDirectoryName(path);
				Say((ModelFile.Kind(url.Box.Text) == null) ? "That's no voice model Capri can run. Pick a Piper, Kokoro, Kitten or Pocket TTS model for sherpa-onnx: its .tar.bz2, or the .onnx in its folder." : "", ModelFile.Kind(url.Box.Text) == null);
				return;
			}
			browse.Enabled = false;
			Say("Unpacking " + Path.GetFileName(path) + "...", problem: false);
			ThreadPool.QueueUserWorkItem((object param0) =>
			{
				string folder = null;
				string problem = null;
				try
				{
					folder = ModelFile.Unpack(path);
				}
				catch (VoiceException ex)
				{
					problem = ex.Message;
				}
				catch (Exception ex2)
				{
					problem = Voice.Short("Couldn't unpack that: " + ex2.Message);
				}
				try
				{
					BeginInvoke((System.Windows.Forms.MethodInvoker)(() =>
					{
						browse.Enabled = true;
						if (folder != null)
						{
							url.Box.Text = folder;
						}
						Say((folder != null) ? "Unpacked. Press Test to hear it." : problem, folder == null);
					}));
				}
				catch (InvalidOperationException)
				{
				}
			});
		}

		private void Kept()
		{
			bool flag = savedTail != null && savedUrl != null && VoiceServer.Same(savedUrl, savedApi, url.Box.Text, picked.Api);
			key.Cue = (flag ? ("Key saved, ends " + savedTail) : picked.KeyCue);
			forget.Visible = flag;
		}

		private void Say(string text, bool problem)
		{
			note.Text = text;
			note.ForeColor = (problem ? Hot : Faint);
		}

		private void Try()
		{
			test.Enabled = false;
			Say("Asking your server...", problem: false);
			owner.capri.TestVoiceServer(url.Box.Text, model.Box.Text, speaker.Box.Text, key.Box.Text, picked.Api, (string said, bool problem) =>
			{
				if (!IsDisposed)
				{
					test.Enabled = true;
					Say(said, problem);
				}
			});
		}

		private void Use()
		{
			owner.wantEleven = false;
			owner.wantCloud = null;
			owner.capri.UseVoiceServer(url.Box.Text, model.Box.Text, speaker.Box.Text, key.Box.Text, picked.Api);
			Close();
		}

		internal void Type(string address, string name, string voice, string secret)
		{
			url.Box.Text = address;
			model.Box.Text = name;
			speaker.Box.Text = voice;
			key.Box.Text = secret;
		}

		internal void Press(string button)
		{
			foreach (Control control in Controls)
			{
				if (control is Key && control.Text == button && control.Visible && control.Enabled)
				{
					typeof(Control).GetMethod("OnClick", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(control, new object[1] { EventArgs.Empty });
					return;
				}
			}
			throw new InvalidOperationException("The voice server panel has no " + button + " button to press.");
		}

		protected override void OnShown(EventArgs e)
		{
			base.OnShown(e);
			url.Box.Focus();
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				services.Dispose();
				voices.Dispose();
			}
			base.Dispose(disposing);
		}

		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);
			float num = ScaleOf(this);
			foreach (Control control in Controls)
			{
				if (control is Label)
				{
					control.MaximumSize = new Size((int)Math.Round(328f * num), 0);
				}
			}
			FitHint();
			Rectangle workingArea = Screen.FromControl(owner).WorkingArea;
			int val = owner.Left + (owner.Width - Width) / 2;
			int val2 = owner.Top + (int)(48f * num);
			Location = new Point(Math.Max(workingArea.Left, Math.Min(workingArea.Right - Width, val)), Math.Max(workingArea.Top, Math.Min(workingArea.Bottom - Height, val2)));
		}

		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged(e);
			float s = ScaleOf(this);
			Clip(this, s);
			Invalidate(invalidateChildren: true);
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);
			float s = ScaleOf(this);
			Graphics graphics = e.Graphics;
			graphics.SmoothingMode = SmoothingMode.AntiAlias;
			Backdrop(graphics, this, s);
			Edging(graphics, this, s);
		}

		protected override void WndProc(ref Message message)
		{
			base.WndProc(ref message);
			if (message.Msg == 132 && message.Result == new IntPtr(1))
			{
				message.Result = new IntPtr(2);
			}
		}

		private Label Caption(string text, int x, int y, Font font, Color colour)
		{
			Label label = new Note();
			label.Text = text;
			label.Font = font;
			label.ForeColor = colour;
			label.BackColor = Ground;
			label.AutoSize = true;
			label.Location = new Point(x, y);
			Controls.Add(label);
			return label;
		}

		private Entry Input(int x, int y, int width, string cue, string name)
		{
			Entry entry = new Entry();
			entry.Bounds = new Rectangle(x, y, width, 28);
			entry.Cue = cue;
			entry.Box.AccessibleName = name;
			Controls.Add(entry);
			return entry;
		}

		private Key Make(string text, int x, int y, int width, int height, EventHandler click)
		{
			Key key = new Key();
			key.Text = text;
			key.Font = Mainframe.Type(9f, true);
			key.Bounds = new Rectangle(x, y, width, height);
			key.Click += click;
			Controls.Add(key);
			return key;
		}
	}

	private sealed class SecuritySheet : Form
	{
		internal const int Wide = 900;

		internal const int Tall = 470;

		internal const int Column = 410;

		internal readonly float Zoom;

		private readonly Mainframe owner;

		private readonly Slider presets;

		private readonly Readout presetName;

		private readonly Label presetLine;

		private readonly Toggle[] toggles = new Toggle[11];

		private bool syncing;

		internal string Showing
		{
			get
			{
				StringBuilder stringBuilder = new StringBuilder();
				Toggle[] array = toggles;
				foreach (Toggle toggle in array)
				{
					stringBuilder.Append(toggle.Checked ? '1' : '0');
				}
				return string.Concat(presetName.Text, " | ", Security.Presets[presets.Value], " | ", stringBuilder, " | ", presetLine.Text);
			}
		}

		internal SecuritySheet(Mainframe owner, float zoom)
		{
			this.owner = owner;
			Zoom = zoom;
			Text = "Security";
			Icon = owner.Icon;
			ShowInTaskbar = false;
			FormBorderStyle = FormBorderStyle.None;
			StartPosition = FormStartPosition.Manual;
			AutoScaleMode = AutoScaleMode.Dpi;
			AutoScaleDimensions = new SizeF(96f, 96f);
			Font = Type(9f * zoom);
			BackColor = Ground;
			ForeColor = White;
			DoubleBuffered = true;
			KeyPreview = true;
			ClientSize = new Size(Z(900), Z(470));
			Font font = Look.Mono(8.5f * zoom, FontStyle.Regular, GraphicsUnit.Point);
			Heading(this, "SECURITY >", new Point(Z(24), Z(15)), Display(15f * zoom), Rose, 0);
			Key key = new Key
			{
				Bounds = Box(846, 20, 26, 22),
				Glyph = 2,
				AccessibleName = "Close"
			};
			EventHandler value = (object param0, EventArgs param1) =>
			{
				Close();
			};
			key.Click += value;
			Controls.Add(key);
			Caption("What she does on your PC without asking. With a toggle off she asks first, and does it when you say yes.", 28, 52, font, Faint);
			Caption("Preset", 28, 82, Type(9.5f * zoom), Rose);
			presets = new Slider(new string[4]
			{
				Security.Presets[0],
				Security.Presets[1],
				Security.Presets[2],
				Security.Presets[3]
			});
			presets.Font = Type(9.5f * zoom);
			presets.Bounds = Box(120, 79, 260, 22);
			presets.AccessibleName = "Security preset: Lockdown, Guarded, Trusted or Blackwall";
			presets.Changed += (object param0, EventArgs param1) =>
			{
				if (!syncing)
				{
					Security.Pick(presets.Value);
				}
			};
			presets.MouseUp += (object param0, MouseEventArgs param1) =>
			{
				if (!syncing && Security.Preset == 4)
				{
					Security.Pick(presets.Value);
				}
			};
			Controls.Add(presets);
			presetName = new Readout(presets);
			presetLine = Caption("", 28, 110, font, Faint);
			Font font2 = Type(10f * zoom, bold: true);
			Heading(this, "HER HANDS >", new Point(Z(28), Z(144)), font2, White, 424);
			Heading(this, "HER JOBS >", new Point(Z(462), Z(144)), font2, White, 424);
			Heading(this, "LISTENING >", new Point(Z(462), Z(362)), font2, White, 424);
			for (int num = 0; num < 11; num++)
			{
				int num2 = ((num <= 5) ? 28 : 462);
				int num3 = ((num <= 5) ? (168 + 46 * num) : ((num < 10) ? (168 + 46 * (num - 6)) : 386));
				int toggle = num;
				Toggle toggle2 = new Toggle();
				toggle2.Text = Security.Names[num];
				toggle2.Font = Font;
				toggle2.Switch = 96;
				toggle2.Bounds = Box(num2, num3, 410, 22);
				toggle2.AccessibleName = Security.Names[num] + ". " + Security.Lines[num];
				toggle2.CheckedChanged += (object param0, EventArgs param1) =>
				{
					if (!syncing)
					{
						Security.Set(toggle, toggles[toggle].Checked);
					}
				};
				Controls.Add(toggle2);
				toggles[num] = toggle2;
				Caption(Security.Lines[num], num2, num3 + 24, font, Faint);
			}
			Sync();
		}

		private int Z(int value)
		{
			return (int)Math.Round((float)value * Zoom);
		}

		private Rectangle Box(int x, int y, int width, int height)
		{
			return new Rectangle(Z(x), Z(y), Z(width), Z(height));
		}

		private Label Caption(string text, int x, int y, Font font, Color colour)
		{
			Label label = new Note();
			label.Text = text;
			label.Font = font;
			label.ForeColor = colour;
			label.BackColor = Ground;
			label.AutoSize = true;
			label.Location = new Point(Z(x), Z(y));
			Controls.Add(label);
			return label;
		}

		internal void Sync()
		{
			syncing = true;
			int preset = Security.Preset;
			presets.Value = ((preset == 4) ? Nearest() : preset);
			presetName.Text = Security.Presets[preset];
			presetName.ForeColor = ((preset == 4) ? Look.Yellow : Cyan);
			presetLine.Text = Security.PresetLine(preset);
			for (int i = 0; i < 11; i++)
			{
				toggles[i].Checked = Security.On(i);
			}
			syncing = false;
		}

		private static int Nearest()
		{
			int mask = Security.Mask;
			int result = 2;
			int num = int.MaxValue;
			for (int i = 0; i < 4; i++)
			{
				int num2 = 0;
				int num3 = mask ^ Security.MaskOf(i);
				for (int j = 0; j < 11; j++)
				{
					if ((num3 & (1 << j)) != 0)
					{
						num2++;
					}
				}
				if (num2 < num)
				{
					num = num2;
					result = i;
				}
			}
			return result;
		}

		internal void Flip(int toggle)
		{
			toggles[toggle].Checked = !toggles[toggle].Checked;
		}

		internal void Slide(int preset)
		{
			presets.Slide(preset);
		}

		internal void Tap(int preset)
		{
			presets.Slide(preset);
			typeof(Control).GetMethod("OnMouseUp", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(presets, new object[1]
			{
				new MouseEventArgs(MouseButtons.Left, 1, 0, 0, 0)
			});
		}

		internal string Crowded()
		{
			float num = ScaleOf(this);
			for (int i = 0; i < Controls.Count; i++)
			{
				Control control = Controls[i];
				if (!control.Visible || (control is Label && control.Text.Length == 0))
				{
					continue;
				}
				if (!ClientRectangle.Contains(control.Bounds) || control.Right > ClientSize.Width - (int)(16f * num))
				{
					return string.Concat("pokes out: ", (control.Text.Length > 0) ? control.Text : control.AccessibleName, " ", control.Bounds, " in ", ClientSize);
				}
				if (control is Toggle toggle && TextRenderer.MeasureText(toggle.Text, toggle.Font).Width > toggle.NameRoom)
				{
					return "doesn't fit: " + toggle.Text + " " + TextRenderer.MeasureText(toggle.Text, toggle.Font).Width + " in " + toggle.NameRoom;
				}
				for (int j = i + 1; j < Controls.Count; j++)
				{
					Control control2 = Controls[j];
					if (control2.Visible && (!(control2 is Label) || control2.Text.Length != 0) && control.Bounds.IntersectsWith(control2.Bounds))
					{
						return "overlap: " + ((control.Text.Length > 0) ? control.Text : control.AccessibleName) + " / " + ((control2.Text.Length > 0) ? control2.Text : control2.AccessibleName);
					}
				}
			}
			return null;
		}

		internal void Picture(string path)
		{
			using Bitmap bitmap = new Bitmap(Width, Height);
			DrawToBitmap(bitmap, new Rectangle(Point.Empty, Size));
			bitmap.Save(path, ImageFormat.Png);
		}

		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);
			Rectangle workingArea = Screen.FromControl(owner).WorkingArea;
			int val = owner.Left + (owner.Width - Width) / 2;
			int val2 = owner.Top + (int)(48f * ScaleOf(this));
			Location = new Point(Math.Max(workingArea.Left, Math.Min(workingArea.Right - Width, val)), Math.Max(workingArea.Top, Math.Min(workingArea.Bottom - Height, val2)));
		}

		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged(e);
			float s = ScaleOf(this);
			Clip(this, s);
			Invalidate(invalidateChildren: true);
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);
			float s = ScaleOf(this);
			Graphics graphics = e.Graphics;
			graphics.SmoothingMode = SmoothingMode.AntiAlias;
			Backdrop(graphics, this, s);
			Edging(graphics, this, s);
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Escape)
			{
				e.Handled = true;
				Close();
			}
			else
			{
				base.OnKeyDown(e);
			}
		}

		protected override void WndProc(ref Message message)
		{
			base.WndProc(ref message);
			if (message.Msg == 132 && message.Result == new IntPtr(1))
			{
				message.Result = new IntPtr(2);
			}
		}
	}

	private sealed class LayersSheet : Form
	{
		internal const int Wide = 760;

		internal const int Tall = 356;

		internal const int Column = 364;

		internal const int DelayStep = 5;

		internal readonly float Zoom;

		private static readonly string[] PitchSteps = PitchLabels();

		private static readonly string[] DelaySteps = DelayLabels();

		private readonly Mainframe owner;

		private readonly Toggle onOff;

		private readonly Slider mainPitch;

		private readonly Readout mainName;

		private readonly Slider[] pitches = new Slider[2];

		private readonly Slider[] delays = new Slider[2];

		private readonly Readout[] pitchNames = new Readout[2];

		private readonly Readout[] delayNames = new Readout[2];

		private bool syncing;

		internal string Showing => (onOff.Checked ? "On" : "Off") + " | " + mainName.Text + " | " + pitchNames[0].Text + ", " + delayNames[0].Text + " | " + pitchNames[1].Text + ", " + delayNames[1].Text;

		internal LayersSheet(Mainframe owner, float zoom)
		{
			LayersSheet layersSheet = this;
			this.owner = owner;
			Zoom = zoom;
			Text = "Voice layers";
			Icon = owner.Icon;
			ShowInTaskbar = false;
			FormBorderStyle = FormBorderStyle.None;
			StartPosition = FormStartPosition.Manual;
			AutoScaleMode = AutoScaleMode.Dpi;
			AutoScaleDimensions = new SizeF(96f, 96f);
			Font = Type(9.5f * zoom);
			BackColor = Ground;
			ForeColor = White;
			DoubleBuffered = true;
			KeyPreview = true;
			ClientSize = new Size(Z(760), Z(356));
			Font font = Look.Mono(8.5f * zoom, FontStyle.Regular, GraphicsUnit.Point);
			Font font2 = Type(10f * zoom, bold: true);
			Heading(this, "VOICE LAYERS >", new Point(Z(24), Z(15)), Display(15f * zoom), Rose, 0);
			Key key = new Key
			{
				Bounds = Box(706, 20, 26, 22),
				Glyph = 2,
				AccessibleName = "Close"
			};
			EventHandler value = (object param0, EventArgs param1) =>
			{
				Close();
			};
			key.Click += value;
			Controls.Add(key);
			Caption("Her voice, and two copies of it mixed in under her, each with its own pitch and delay.", 28, 52, font, Faint);
			onOff = new Toggle();
			onOff.Text = "Voice layers";
			onOff.Font = Font;
			onOff.Switch = 120;
			onOff.Bounds = Box(28, 76, 260, 22);
			onOff.AccessibleName = "Voice layers. Off, she sounds as she always has";
			onOff.CheckedChanged += (object param0, EventArgs param1) =>
			{
				if (!layersSheet.syncing)
				{
					owner.capri.SetVoiceLayers(owner.capri.VoiceLayers.Turned(layersSheet.onOff.Checked));
					owner.ShowLayers();
					owner.capri.HearVoiceStyle();
				}
			};
			Controls.Add(onOff);
			Caption("Off: she sounds as she always has. On: every voice she has, ElevenLabs too.", 28, 102, font, Faint);
			Make("Hear her", 524, 76, 100, 22, (object param0, EventArgs param1) =>
			{
				owner.capri.HearVoiceStyle();
			}).AccessibleName = "Hear her: she says a line in her voice as it is now";
			Make("Defaults", 632, 76, 100, 22, (object param0, EventArgs param1) =>
			{
				Defaults();
			}).AccessibleName = "Defaults: Layer 1 " + VoiceFx.Layering.PitchName(2.0) + " and Layer 2 " + VoiceFx.Layering.PitchName(-2.5) + " semitones, both " + VoiceFx.Layering.DelayName(25) + " behind her";
			Heading(this, "MAIN >", new Point(Z(28), Z(136)), font2, White, 732);
			Caption("Pitch", 28, 166, Font, Rose);
			mainPitch = Bar(PitchNames, 120, 163, 300, "Her voice's pitch, the same as Settings > VOICE > Pitch");
			mainName = new Readout(mainPitch);
			mainPitch.Changed += (object param0, EventArgs param1) =>
			{
				if (!layersSheet.syncing)
				{
					owner.capri.SetVoicePitch(layersSheet.mainPitch.Value - 6);
					layersSheet.mainName.Text = PitchNames[layersSheet.mainPitch.Value];
					owner.Sync();
				}
			};
			mainPitch.Released += (object param0, EventArgs param1) =>
			{
				if (!layersSheet.syncing)
				{
					owner.capri.HearVoiceStyle();
				}
			};
			Caption("Her voice itself. Its pitch is Settings > VOICE > Pitch, and they move together.", 28, 192, font, Faint);
			for (int num = 1; num <= 2; num++)
			{
				int num2 = ((num == 1) ? 28 : 392);
				int num3 = num - 1;
				int which = num;
				Heading(this, "LAYER " + num + " >", new Point(Z(num2), Z(228)), font2, White, 352);
				Caption("Pitch", num2, 258, Font, Rose);
				pitches[num3] = Bar(PitchSteps, num2 + 84, 255, 244, "Layer " + num + "'s pitch, in semitones from hers");
				pitches[num3].TickEvery = 4;
				pitchNames[num3] = new Readout(pitches[num3]);
				Caption("Delay", num2, 286, Font, Rose);
				delays[num3] = Bar(DelaySteps, num2 + 84, 283, 244, "Layer " + num + "'s delay behind her");
				delays[num3].TickEvery = 10;
				delayNames[num3] = new Readout(delays[num3]);
				Caption((num == 1) ? "Out of the box a little higher, just behind her." : "Out of the box lower, just behind her.", num2, 314, font, Faint);
				pitches[num3].Changed += (object param0, EventArgs param1) =>
				{
					if (!layersSheet.syncing)
					{
						VoiceFx.Layering voiceLayers = owner.capri.VoiceLayers;
						owner.capri.SetVoiceLayers(voiceLayers.With(which, (double)layersSheet.pitches[which - 1].Value / 2.0 - 12.0, voiceLayers.DelayOf(which)));
						layersSheet.pitchNames[which - 1].Text = VoiceFx.Layering.PitchName(owner.capri.VoiceLayers.PitchOf(which));
					}
				};
				delays[num3].Changed += (object param0, EventArgs param1) =>
				{
					if (!layersSheet.syncing)
					{
						VoiceFx.Layering voiceLayers = owner.capri.VoiceLayers;
						owner.capri.SetVoiceLayers(voiceLayers.With(which, voiceLayers.PitchOf(which), layersSheet.delays[which - 1].Value * 5));
						layersSheet.delayNames[which - 1].Text = VoiceFx.Layering.DelayName(owner.capri.VoiceLayers.DelayOf(which));
					}
				};
				pitches[num3].Released += (object param0, EventArgs param1) =>
				{
					if (!layersSheet.syncing)
					{
						owner.capri.HearVoiceStyle();
					}
				};
				delays[num3].Released += (object param0, EventArgs param1) =>
				{
					if (!layersSheet.syncing)
					{
						owner.capri.HearVoiceStyle();
					}
				};
			}
			Sync();
		}

		private static string[] PitchLabels()
		{
			string[] array = new string[49];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = VoiceFx.Layering.PitchName((double)i / 2.0 - 12.0);
			}
			return array;
		}

		private static string[] DelayLabels()
		{
			string[] array = new string[51];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = VoiceFx.Layering.DelayName(i * 5);
			}
			return array;
		}

		private int Z(int value)
		{
			return (int)Math.Round((float)value * Zoom);
		}

		private Rectangle Box(int x, int y, int width, int height)
		{
			return new Rectangle(Z(x), Z(y), Z(width), Z(height));
		}

		private Label Caption(string text, int x, int y, Font font, Color colour)
		{
			Label label = new Note();
			label.Text = text;
			label.Font = font;
			label.ForeColor = colour;
			label.BackColor = Ground;
			label.AutoSize = true;
			label.Location = new Point(Z(x), Z(y));
			Controls.Add(label);
			return label;
		}

		private Slider Bar(string[] names, int x, int y, int width, string name)
		{
			Slider slider = new Slider(names);
			slider.Font = Font;
			slider.Bounds = Box(x, y, width, 22);
			slider.AccessibleName = name;
			Controls.Add(slider);
			return slider;
		}

		private Key Make(string text, int x, int y, int width, int height, EventHandler click)
		{
			Key key = new Key();
			key.Text = text;
			key.Font = Type(9f * Zoom, bold: true);
			key.Bounds = Box(x, y, width, height);
			key.Click += click;
			Controls.Add(key);
			return key;
		}

		internal void Sync()
		{
			syncing = true;
			VoiceFx.Layering voiceLayers = owner.capri.VoiceLayers;
			onOff.Checked = voiceLayers.On;
			Put(mainPitch, owner.capri.VoicePitch + 6);
			mainName.Text = PitchNames[mainPitch.Value];
			for (int i = 0; i < 2; i++)
			{
				Put(pitches[i], (int)Math.Round((voiceLayers.PitchOf(i + 1) + 12.0) * 2.0));
				pitchNames[i].Text = VoiceFx.Layering.PitchName(voiceLayers.PitchOf(i + 1));
				Put(delays[i], (int)Math.Round((double)voiceLayers.DelayOf(i + 1) / 5.0));
				delayNames[i].Text = VoiceFx.Layering.DelayName(voiceLayers.DelayOf(i + 1));
			}
			syncing = false;
		}

		private static void Put(Slider slider, int value)
		{
			if (slider.Value != value)
			{
				slider.Value = value;
			}
		}

		internal void Defaults()
		{
			owner.capri.SetVoiceLayers(new VoiceFx.Layering(owner.capri.VoiceLayers.On, 2.0, 25, -2.5, 25));
			Sync();
			owner.capri.HearVoiceStyle();
		}

		internal void Flip()
		{
			onOff.Checked = !onOff.Checked;
		}

		internal void Slide(int slider, int index)
		{
			(slider switch
			{
				3 => pitches[1], 
				2 => delays[0], 
				1 => pitches[0], 
				0 => mainPitch, 
				_ => delays[1], 
			}).Slide(index);
		}

		internal string Crowded()
		{
			float num = ScaleOf(this);
			for (int i = 0; i < Controls.Count; i++)
			{
				Control control = Controls[i];
				if (!control.Visible || (control is Label && control.Text.Length == 0))
				{
					continue;
				}
				if (!ClientRectangle.Contains(control.Bounds) || control.Right > ClientSize.Width - (int)(16f * num))
				{
					return string.Concat("pokes out: ", (control.Text.Length > 0) ? control.Text : control.AccessibleName, " ", control.Bounds, " in ", ClientSize);
				}
				if (control is Toggle toggle && TextRenderer.MeasureText(toggle.Text, toggle.Font).Width > toggle.NameRoom)
				{
					return "doesn't fit: " + toggle.Text + " " + TextRenderer.MeasureText(toggle.Text, toggle.Font).Width + " in " + toggle.NameRoom;
				}
				if (control is Key key && key.Text.Length > 0 && TextRenderer.MeasureText(key.Text, key.Font).Width + (int)Math.Ceiling(12f * num) > key.Width)
				{
					return "doesn't fit: " + key.Text + " " + TextRenderer.MeasureText(key.Text, key.Font).Width + " in " + key.Width;
				}
				for (int j = i + 1; j < Controls.Count; j++)
				{
					Control control2 = Controls[j];
					if (control2.Visible && (!(control2 is Label) || control2.Text.Length != 0) && control.Bounds.IntersectsWith(control2.Bounds))
					{
						return "overlap: " + ((control.Text.Length > 0) ? control.Text : control.AccessibleName) + " / " + ((control2.Text.Length > 0) ? control2.Text : control2.AccessibleName);
					}
				}
			}
			Slider[] array = new Slider[5]
			{
				pitches[0],
				pitches[1],
				delays[0],
				delays[1],
				mainPitch
			};
			foreach (Slider slider in array)
			{
				string text;
				if (slider == mainPitch)
				{
					text = "Normal";
				}
				else
				{
					text = ((Array.IndexOf(pitches, slider) >= 0) ? "-11.5" : "250 ms");
				}
				if (TextRenderer.MeasureText(text, slider.Font).Width + (int)Math.Ceiling(24f * num) > slider.Width)
				{
					return "doesn't fit: " + text + " in " + slider.AccessibleName;
				}
			}
			return null;
		}

		internal void Picture(string path)
		{
			using Bitmap bitmap = new Bitmap(Width, Height);
			DrawToBitmap(bitmap, new Rectangle(Point.Empty, Size));
			bitmap.Save(path, ImageFormat.Png);
		}

		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);
			Rectangle workingArea = Screen.FromControl(owner).WorkingArea;
			int val = owner.Left + (owner.Width - Width) / 2;
			int val2 = owner.Top + (int)(48f * ScaleOf(this));
			Location = new Point(Math.Max(workingArea.Left, Math.Min(workingArea.Right - Width, val)), Math.Max(workingArea.Top, Math.Min(workingArea.Bottom - Height, val2)));
		}

		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged(e);
			float s = ScaleOf(this);
			Clip(this, s);
			Invalidate(invalidateChildren: true);
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);
			float s = ScaleOf(this);
			Graphics graphics = e.Graphics;
			graphics.SmoothingMode = SmoothingMode.AntiAlias;
			Backdrop(graphics, this, s);
			Edging(graphics, this, s);
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Escape)
			{
				e.Handled = true;
				Close();
			}
			else
			{
				base.OnKeyDown(e);
			}
		}

		protected override void WndProc(ref Message message)
		{
			base.WndProc(ref message);
			if (message.Msg == 132 && message.Result == new IntPtr(1))
			{
				message.Result = new IntPtr(2);
			}
		}
	}

	private sealed class Slider : Control
	{
		private readonly string[] labels;

		private int value;

		private bool hot;

		private bool sliding;

		private int settled;

		private int zone = 2;

		private string shown;

		private Color ink = Color.Empty;

		internal int TickEvery = 1;

		internal int Value
		{
			get
			{
				return value;
			}
			set
			{
				Place(value);
				settled = this.value;
			}
		}

		internal string Shown
		{
			get
			{
				return shown ?? labels[value];
			}
			set
			{
				shown = value;
				Invalidate();
			}
		}

		internal Color Ink
		{
			get
			{
				if (!ink.IsEmpty)
				{
					return ink;
				}
				return Cyan;
			}
			set
			{
				ink = value;
				Invalidate();
			}
		}

		private bool Stepper => labels.Length <= 7;

		private float Grip => 10f * Unit();

		internal event EventHandler Changed;

		internal event EventHandler Released;

		internal Slider(string[] labels)
		{
			this.labels = labels;
			SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.Selectable | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
			BackColor = Ground;
			Cursor = Cursors.Hand;
			TabStop = true;
			AccessibleRole = AccessibleRole.Slider;
		}

		private void Place(int wanted)
		{
			int num = Math.Max(0, Math.Min(labels.Length - 1, wanted));
			if (num != value)
			{
				value = num;
				AccessibleDescription = labels[num];
				Invalidate();
			}
		}

		private void Pick(int next)
		{
			int num = value;
			Place(next);
			if (value != num && Changed != null)
			{
				Changed(this, EventArgs.Empty);
			}
		}

		private void Settle()
		{
			if (value != settled)
			{
				settled = value;
				if (Released != null)
				{
					Released(this, EventArgs.Empty);
				}
			}
		}

		internal void Slide(int next)
		{
			Pick(next);
			Settle();
		}

		private float Unit()
		{
			return ScaleOf(this);
		}

		private int At(int x)
		{
			float num = Unit();
			float num2 = (float)Width - 2f * num - Grip;
			return (int)Math.Round(((float)x - num - Grip / 2f) / num2 * (float)(labels.Length - 1));
		}

		private int Zone(int x)
		{
			float num = Math.Min((float)Width / 3f, 36f * Unit());
			if (!((float)x < num))
			{
				if (!((float)x > (float)Width - num))
				{
					return 0;
				}
				return 1;
			}
			return -1;
		}

		protected override void OnMouseEnter(EventArgs e)
		{
			hot = true;
			Invalidate();
			base.OnMouseEnter(e);
		}

		protected override void OnMouseLeave(EventArgs e)
		{
			hot = false;
			zone = 2;
			Invalidate();
			base.OnMouseLeave(e);
		}

		protected override void OnMouseDown(MouseEventArgs e)
		{
			Focus();
			sliding = true;
			if (!Stepper)
			{
				Pick(At(e.X));
			}
			else if (e.Button == MouseButtons.Left)
			{
				Pick(value + Zone(e.X));
			}
			base.OnMouseDown(e);
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			if (sliding && !Stepper)
			{
				Pick(At(e.X));
			}
			int num = Zone(e.X);
			if (Stepper && num != zone)
			{
				zone = num;
				Invalidate();
			}
			base.OnMouseMove(e);
		}

		protected override void OnMouseUp(MouseEventArgs e)
		{
			bool flag = sliding;
			sliding = false;
			if (flag)
			{
				Settle();
			}
			Invalidate();
			base.OnMouseUp(e);
		}

		protected override bool IsInputKey(Keys key)
		{
			if (key != Keys.Left && key != Keys.Right)
			{
				return base.IsInputKey(key);
			}
			return true;
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Left)
			{
				Pick(value - 1);
			}
			else if (e.KeyCode == Keys.Right)
			{
				Pick(value + 1);
			}
			base.OnKeyDown(e);
		}

		protected override void OnKeyUp(KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Left || e.KeyCode == Keys.Right)
			{
				Settle();
			}
			base.OnKeyUp(e);
		}

		protected override void OnGotFocus(EventArgs e)
		{
			Invalidate();
			base.OnGotFocus(e);
		}

		protected override void OnLostFocus(EventArgs e)
		{
			Invalidate();
			base.OnLostFocus(e);
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			float num = Unit();
			Graphics graphics = e.Graphics;
			Backdrop(graphics, this, num);
			graphics.SmoothingMode = SmoothingMode.AntiAlias;
			float num2 = Math.Max(1f, num);
			RectangleF box = new RectangleF(num2 / 2f, num2 / 2f, (float)Width - num2, (float)Height - num2);
			Color color;
			if (Focused && ShowFocusCues)
			{
				color = Cyan;
			}
			else
			{
				color = ((hot || sliding) ? Color.FromArgb(156, 40, 56) : Edge);
			}
			TextFormatFlags textFormatFlags = TextFormatFlags.EndEllipsis | TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter;
			if (Stepper)
			{
				using (GraphicsPath path = Cut(box, 0f, 5f * num, 5f * num, 0f))
				{
					using (Brush brush = new SolidBrush(Field))
					{
						graphics.FillPath(brush, path);
					}
					using Pen pen = new Pen(color, num2);
					graphics.DrawPath(pen, path);
				}
				float middle = (float)Height / 2f - 1.5f * num;
				Arrow(graphics, 11f * num, middle, num, -1, value > 0, zone == -1);
				Arrow(graphics, (float)Width - 11f * num, middle, num, 1, value < labels.Length - 1, zone == 1);
				int num3 = (int)(26f * num);
				TextRenderer.DrawText(graphics, Shown, Font, new Rectangle(num3, 0, Width - 2 * num3, (int)((float)Height - 4f * num)), Ink, textFormatFlags);
				float num4 = (float)((labels.Length <= 4) ? 11 : 8) * num;
				float num5 = 3f * num;
				float num6 = (float)Width / 2f - ((float)labels.Length * num4 + (float)(labels.Length - 1) * num5) / 2f;
				using Brush brush2 = new SolidBrush(Dash);
				using Brush brush3 = new SolidBrush(Lit);
				for (int i = 0; i < labels.Length; i++)
				{
					graphics.FillRectangle((i == value) ? brush3 : brush2, num6 + (float)i * (num4 + num5), (float)Height - 5.5f * num, num4, Math.Max(1f, 1.5f * num));
				}
				return;
			}
			float num7 = (float)Width - 2f * num - Grip;
			float num8 = num + num7 * (float)value / (float)(labels.Length - 1);
			using (GraphicsPath path2 = Cut(box, 0f, 0f, 5f * num, 0f))
			{
				using (Brush brush4 = new SolidBrush(Field))
				{
					graphics.FillPath(brush4, path2);
				}
				using (Brush brush5 = new SolidBrush(Color.FromArgb(42, 14, 25)))
				{
					graphics.FillRectangle(brush5, num2, num2, num8 - num2, (float)Height - 2f * num2);
				}
				using (Brush brush6 = new SolidBrush(Dash))
				{
					for (int j = 0; j < labels.Length; j += Math.Max(1, TickEvery))
					{
						graphics.FillRectangle(brush6, num + Grip / 2f + num7 * (float)j / (float)(labels.Length - 1) - 0.5f * num, (float)Height - 5f * num, Math.Max(1f, num), 3f * num);
					}
				}
				using (GraphicsPath path3 = Cut(new RectangleF(num8, num2, Grip, (float)Height - 2f * num2), 0f, 0f, 4f * num, 0f))
				{
					using Brush brush7 = new SolidBrush((hot || sliding || Focused) ? Lit : Hot);
					graphics.FillPath(brush7, path3);
				}
				using Pen pen2 = new Pen(color, num2);
				graphics.DrawPath(pen2, path2);
			}
			Rectangle rectangle = new Rectangle(0, 0, Width, Height);
			Rectangle rectangle2 = Rectangle.Round(new RectangleF(num8, 0f, Grip, Height));
			textFormatFlags |= TextFormatFlags.PreserveGraphicsClipping;
			using (Region region = new Region(rectangle))
			{
				region.Exclude(rectangle2);
				graphics.SetClip(region, CombineMode.Replace);
				TextRenderer.DrawText(graphics, Shown, Font, rectangle, Ink, textFormatFlags);
			}
			graphics.SetClip(rectangle2);
			TextRenderer.DrawText(graphics, Shown, Font, rectangle, Color.FromArgb(40, 6, 12), textFormatFlags);
			graphics.ResetClip();
		}

		private static void Arrow(Graphics graphics, float tip, float middle, float s, int way, bool open, bool over)
		{
			float num = tip - (float)(way * 7) * s;
			PointF[] points = ((way < 0) ? new PointF[3]
			{
				new PointF(tip, middle),
				new PointF(num, middle - 5.5f * s),
				new PointF(num, middle + 5.5f * s)
			} : new PointF[3]
			{
				new PointF(tip, middle),
				new PointF(num, middle - 5.5f * s),
				new PointF(num, middle + 5.5f * s)
			});
			Color color = (open ? Cyan : Dash);
			if (open && over)
			{
				using Brush brush = new SolidBrush(color);
				graphics.FillPolygon(brush, points);
			}
			using Pen pen = new Pen(color, 1.3f * s);
			pen.LineJoin = LineJoin.Miter;
			graphics.DrawPolygon(pen, points);
		}
	}

	private sealed class Rung : RadioButton
	{
		private bool hot;

		internal string Hint;

		internal Font HintFont;

		internal string Flag;

		internal Font FlagFont;

		internal Rung()
		{
			SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
			BackColor = Ground;
			Cursor = Cursors.Hand;
		}

		protected override void OnMouseEnter(EventArgs e)
		{
			hot = true;
			Invalidate();
			base.OnMouseEnter(e);
		}

		protected override void OnMouseLeave(EventArgs e)
		{
			hot = false;
			Invalidate();
			base.OnMouseLeave(e);
		}

		protected override void OnEnter(EventArgs e)
		{
			if (AutoCheck)
			{
				base.OnEnter(e);
			}
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			float num = ScaleOf(this);
			Graphics graphics = e.Graphics;
			Backdrop(graphics, this, num);
			graphics.SmoothingMode = SmoothingMode.AntiAlias;
			bool flag = hot || (Focused && ShowFocusCues);
			if (flag)
			{
				float num2 = Math.Max(1f, num);
				using GraphicsPath path = Cut(new RectangleF(num2 / 2f, num2 / 2f, (float)Width - num2, (float)Height - num2), 0f, 0f, 5f * num, 0f);
				using (Brush brush = new SolidBrush(Color.FromArgb(24, Cyan)))
				{
					graphics.FillPath(brush, path);
				}
				using Pen pen = new Pen(Color.FromArgb(190, Cyan), num2);
				graphics.DrawPath(pen, path);
			}
			float num3 = 12f * num;
			float num4 = (float)Height / 2f;
			float num5 = 4.5f * num;
			PointF[] points = new PointF[4]
			{
				new PointF(num3, num4 - num5),
				new PointF(num3 + num5, num4),
				new PointF(num3, num4 + num5),
				new PointF(num3 - num5, num4)
			};
			if (Checked)
			{
				using Brush brush2 = new SolidBrush(Cyan);
				graphics.FillPolygon(brush2, points);
			}
			else
			{
				using Pen pen2 = new Pen(flag ? Cyan : Faint, 1.2f * num);
				graphics.DrawPolygon(pen2, points);
			}
			int num6 = (int)(24f * num);
			TextFormatFlags flags = TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter;
			TextRenderer.DrawText(graphics, Text, Font, new Rectangle(num6, 0, Width - num6, Height), Checked ? Cyan : White, flags);
			int num7 = num6 + TextRenderer.MeasureText(graphics, Text, Font, Size.Empty, flags).Width + (int)(8f * num);
			if (Hint != null)
			{
				TextRenderer.DrawText(graphics, Hint, HintFont ?? Font, new Rectangle(num7, 0, Math.Max(0, Width - num7), Height), Faint, flags);
			}
			if (Flag != null)
			{
				TextRenderer.DrawText(graphics, Flag, FlagFont ?? Font, new Rectangle(0, 0, Width - (int)(6f * num), Height), Look.Yellow, TextFormatFlags.NoPrefix | TextFormatFlags.Right | TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter);
			}
		}
	}

	private sealed class Toggle : CheckBox
	{
		private int over = 2;

		private Font small;

		internal string Hint;

		internal Font HintFont;

		internal int Switch = 172;

		internal int NameRoom => Width - (int)Math.Ceiling((float)(Switch + 10) * ScaleOf(this));

		internal Toggle()
		{
			SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
			BackColor = Ground;
			Cursor = Cursors.Hand;
			AutoCheck = false;
		}

		private int Part(int x)
		{
			float num = ScaleOf(this);
			float num2 = (float)Width - (float)Switch * num;
			if (!((float)x < num2))
			{
				if (!((float)x < num2 + (float)Switch * num / 2f))
				{
					return 1;
				}
				return 0;
			}
			return -1;
		}

		protected override void OnFontChanged(EventArgs e)
		{
			if (small != null)
			{
				small.Dispose();
			}
			small = null;
			base.OnFontChanged(e);
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing && small != null)
			{
				small.Dispose();
			}
			base.Dispose(disposing);
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			int num = Part(e.X);
			if (num != over)
			{
				over = num;
				Invalidate();
			}
			base.OnMouseMove(e);
		}

		protected override void OnMouseLeave(EventArgs e)
		{
			over = 2;
			Invalidate();
			base.OnMouseLeave(e);
		}

		protected override void OnClick(EventArgs e)
		{
			int num = ((!(e is MouseEventArgs e2)) ? (-1) : Part(e2.X));
			Checked = ((num < 0) ? (!Checked) : (num == 1));
			base.OnClick(e);
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			float num = ScaleOf(this);
			Graphics graphics = e.Graphics;
			Backdrop(graphics, this, num);
			graphics.SmoothingMode = SmoothingMode.AntiAlias;
			if (small == null)
			{
				small = Type(Font.SizeInPoints - 0.75f, bold: true);
			}
			float num2 = Math.Max(1f, num);
			float num3 = (float)Switch * num;
			float num4 = 4f * num;
			float num5 = (num3 - num4) / 2f;
			float num6 = (float)Width - num3;
			RectangleF rectangleF = new RectangleF(num6 + num2 / 2f, num2 / 2f, num5 - num2, (float)Height - num2);
			RectangleF rectangleF2 = new RectangleF(num6 + num5 + num4 + num2 / 2f, num2 / 2f, num5 - num2, (float)Height - num2);
			Color color;
			Color color2;
			Color foreColor;
			Color color3;
			Color color4;
			Color foreColor2;
			if (Checked)
			{
				color = ((over == 0) ? Color.FromArgb(50, 16, 28) : Color.FromArgb(32, 12, 19));
				color2 = ((over == 0) ? Color.FromArgb(120, 36, 50) : Color.FromArgb(50, 18, 28));
				foreColor = ((over == 0) ? Color.FromArgb(170, 60, 72) : Color.FromArgb(84, 26, 38));
				color3 = (color4 = Cyan);
				foreColor2 = OnInk;
			}
			else
			{
				color = Color.FromArgb(155, 50, 46);
				color2 = Color.FromArgb(222, 69, 63);
				foreColor = Color.FromArgb(255, 150, 144);
				color3 = ((over == 1) ? Color.FromArgb(12, 46, 50) : Color.FromArgb(9, 30, 33));
				color4 = ((over == 1) ? DeepEdge : Color.FromArgb(16, 44, 48));
				foreColor2 = ((over == 1) ? Color.FromArgb(110, 210, 216) : Color.FromArgb(28, 68, 72));
			}
			if (!Enabled)
			{
				color = (color3 = Field);
				color2 = (color4 = Color.FromArgb(52, 22, 32));
				foreColor = (foreColor2 = Color.FromArgb(112, 94, 104));
			}
			TextFormatFlags flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter;
			using (GraphicsPath path = Cut(rectangleF, 0f, 0f, 0f, 5f * num))
			{
				using (Brush brush = new SolidBrush(color))
				{
					graphics.FillPath(brush, path);
				}
				using Pen pen = new Pen(color2, num2);
				graphics.DrawPath(pen, path);
			}
			using (GraphicsPath path2 = Cut(rectangleF2, 0f, 0f, 5f * num, 0f))
			{
				using (Brush brush2 = new SolidBrush(color3))
				{
					graphics.FillPath(brush2, path2);
				}
				using Pen pen2 = new Pen(color4, num2);
				graphics.DrawPath(pen2, path2);
			}
			TextRenderer.DrawText(graphics, "OFF", small, Rectangle.Round(rectangleF), foreColor, flags);
			TextRenderer.DrawText(graphics, "ON", small, Rectangle.Round(rectangleF2), foreColor2, flags);
			if (Focused && ShowFocusCues)
			{
				using GraphicsPath path3 = Cut(new RectangleF(num6 - 2f * num, num2 / 2f, num3 + 2f * num - num2 / 2f, (float)Height - num2), 0f, 0f, 5f * num, 0f);
				using Pen pen3 = new Pen(Cyan, num2);
				graphics.DrawPath(pen3, path3);
			}
			TextFormatFlags flags2 = TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter;
			int nameRoom = NameRoom;
			TextRenderer.DrawText(graphics, Text, Font, new Rectangle(0, 0, nameRoom, Height), Enabled ? Rose : Faint, flags2);
			if (Hint != null)
			{
				TextFormatFlags flags3 = TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine;
				int num7 = TextRenderer.MeasureText(graphics, Text, Font, Size.Empty, flags3).Width + (int)(10f * num);
				Font font = HintFont ?? Font;
				if (num7 + TextRenderer.MeasureText(graphics, Hint, font, Size.Empty, flags3).Width <= nameRoom)
				{
					TextRenderer.DrawText(graphics, Hint, font, new Rectangle(num7, 0, nameRoom - num7, Height), Faint, flags2);
				}
			}
		}
	}

	private const int Wide = 380;

	private const int Tall = 642;

	private const int SetupHeight = 118;

	private const int PageTall = 960;

	private const int Column = 380;

	private const int Far = 30000;

	private const int VoiceHeight = 300;

	private const int InstallAt = 443;

	private const int InstallRow = 40;

	private const int KeyAt = 310;

	private const int KeyFold = 36;

	private const string Eleven = "ElevenLabs";

	private const int ScrollRow = 36;

	private const int Row = 28;

	private const int ValueAt = 184;

	private const int ValueWide = 172;

	private const string Waiting = "Press new keys, then Confirm";

	internal const string OpenSpeech = " Click to open it.";

	private static readonly Color Ground = Neon.Ground;

	private static readonly Color Glow = Neon.Glow;

	private static readonly Color Band = Neon.Band;

	private static readonly Color Rule = Neon.Rule;

	private static readonly Color Rose = Neon.Rose;

	private static readonly Color White = Neon.White;

	private static readonly Color Faint = Neon.Faint;

	private static readonly Color Field = Neon.Field;

	private static readonly Color Hover = Neon.Hover;

	private static readonly Color Pressed = Neon.Pressed;

	private static readonly Color Edge = Neon.Edge;

	private static readonly Color Hot = Neon.Hot;

	private static readonly Color Cyan = Neon.Cyan;

	private static readonly Color Deep = Neon.Deep;

	private static readonly Color DeepEdge = Neon.DeepEdge;

	private static readonly Color OnInk = Neon.OnInk;

	private static readonly Color Dash = Neon.Dash;

	private static readonly Color Lit = Neon.Lit;

	private static readonly Color Brim = Neon.Brim;

	private static readonly Color Well = Neon.Well;

	private static readonly Color WellEdge = Neon.WellEdge;

	private static readonly FontFamily Plain = Family("Bahnschrift SemiCondensed");

	private static readonly FontFamily Heavy = Family("Bahnschrift SemiBold SemiConden");

	private static readonly FontFamily Narrow = Family("Bahnschrift SemiBold Condensed");

	private static readonly int[] Gaps = new int[9] { 10, 15, 30, 60, 90, 120, 180, 300, 600 };

	private static readonly string[] GapNames = new string[9] { "10 s", "15 s", "30 s", "1 min", "1.5 min", "2 min", "3 min", "5 min", "10 min" };

	private static readonly string[] PerchNames = Array.ConvertAll(Companion.PerchRates, (int rate) => rate + " fps");

	private static readonly string[] PitchNames = new string[13]
	{
		"-6", "-5", "-4", "-3", "-2", "-1", "Normal", "+1", "+2", "+3",
		"+4", "+5", "+6"
	};

	private static readonly int[] Paces = new int[9] { 70, 80, 90, 100, 110, 120, 130, 140, 150 };

	private static readonly string[] PaceNames = new string[9] { "0.7x", "0.8x", "0.9x", "Normal", "1.1x", "1.2x", "1.3x", "1.4x", "1.5x" };

	private static readonly int[] Amounts = new int[13]
	{
		0, 5, 10, 15, 20, 30, 40, 50, 60, 70,
		80, 90, 100
	};

	private static readonly string[] AmountNames = new string[13]
	{
		"Off", "5%", "10%", "15%", "20%", "30%", "40%", "50%", "60%", "70%",
		"80%", "90%", "100%"
	};

	private static readonly int[] Volumes = new int[13]
	{
		0, 10, 20, 30, 40, 50, 60, 70, 80, 90,
		100, 125, 150
	};

	private static readonly string[] VolumeNames = new string[13]
	{
		"Mute", "10%", "20%", "30%", "40%", "50%", "60%", "70%", "80%", "90%",
		"100%", "125%", "150%"
	};

	private static readonly int[] SizeSteps = new int[10] { 140, 170, 200, 240, 280, 320, 380, 440, 500, 560 };

	private static readonly string[] SizeNames = new string[10] { "Tiny", "Small", "Small+", "Medium", "Medium+", "Large", "Large+", "Huge", "Huge+", "Giant" };

	private static readonly int[] Sleeps = new int[6] { 0, 60, 120, 300, 600, 1800 };

	private static readonly string[] SleepNames = new string[6] { "Never", "1 min", "2 min", "5 min", "10 min", "30 min" };

	private static readonly string[] PopNames = new string[3] { "Rarely", "Sometimes", "Often" };

	private static readonly string[] EyePaceNames = new string[4] { "Rarely", "Sometimes", "Often", "Swarm" };

	private static readonly string[] EyeSizeNames = new string[4] { "Small", "Medium", "Large", "Mixed" };

	private static readonly string[] EyeMostNames = new string[5] { "1", "2", "3", "4", "5" };

	private Toggle eyesOn;

	private Slider eyePace;

	private Readout eyePaceName;

	private Slider eyeSize;

	private Readout eyeSizeName;

	private Slider eyeMost;

	private Readout eyeMostName;

	private static readonly string[] BubbleNames = new string[3] { "Quick", "Normal", "Long" };

	private static readonly string[] SoundNames = new string[11]
	{
		"Off", "10%", "20%", "30%", "40%", "50%", "60%", "70%", "80%", "90%",
		"100%"
	};

	private static readonly string[] WanderNames = new string[5] { "Off", "Rarely", "Sometimes", "Often", "Restless" };

	private static readonly string[] Lengths = new string[3] { "Short", "Medium", "Long" };

	private static readonly int[] Recalls = new int[3] { 8, 16, 32 };

	private float voiceFilled = -1f;

	private readonly Label installNote;

	private readonly Companion capri;

	private readonly Label brainName;

	private readonly Label brainNote;

	private readonly Key models;

	private readonly ContextMenuStrip modelMenu;

	private readonly Key refresh;

	private readonly Label[] steps = new Label[3];

	private readonly string[] stepText = new string[3];

	private readonly Key setup;

	private readonly Label hint;

	private readonly Label tierNote;

	private readonly Rung[] rungs = new Rung[3];

	private int tier;

	private readonly Entry nickname;

	private readonly Key showHide;

	private readonly Key perch;

	private readonly Slider sizeSlider;

	private readonly Readout sizeName;

	private readonly Toggle checkIns;

	private readonly Toggle onTop;

	private readonly Toggle startup;

	private readonly Toggle discord;

	private readonly Toggle cursor;

	private readonly Key security;

	private readonly Key talkKeys;

	private readonly Key muteKeys;

	private readonly Key typeKeys;

	private readonly Key hideKeys;

	private readonly Key quietKeys;

	private readonly Key confirmKeys;

	private readonly Key cancelKeys;

	private int pendingMods = -1;

	private int pendingVk;

	private int heldMods;

	private readonly Toggle watch;

	private readonly Toggle caughtUp;

	private readonly Toggle stayOnModel;

	private readonly Toggle remembering;

	private readonly Label voiceHead;

	private readonly Rung[] voiceRungs = new Rung[4];

	private int voiceRow = 1;

	private bool off;

	private bool eleven;

	private bool wantEleven;

	private string wantCloud;

	private static readonly string[] Clouds = new string[3] { "Fish Audio", "ElevenLabs", "OpenAI" };

	private string cloudName = Clouds[0];

	private readonly Key cloudPick;

	private readonly ContextMenuStrip cloudMenu;

	private readonly Key cloudVoice;

	private readonly Key customVoice;

	private readonly Key setUpModel;

	private int arrangedFold = -1;

	private readonly Key freeVoices;

	private readonly ContextMenuStrip freeMenu;

	private readonly Key freeSetup;

	private bool freeReady;

	private bool yoursShown;

	private ServerSheet sheet;

	private SecuritySheet guard;

	private LayersSheet layered;

	private Key layersKey;

	private Label styleNote;

	private readonly Slider gap;

	private readonly Readout gapName;

	private readonly Entry keyEntry;

	private readonly Key connect;

	private readonly Entry voiceEntry;

	private readonly ContextMenuStrip voiceMenu;

	private readonly Key speechModels;

	private readonly ContextMenuStrip speechModelMenu;

	private readonly Label voiceNote;

	private bool speechFix;

	private readonly Label keysNote;

	private readonly Label blackwallNote;

	private readonly Label updateNote;

	private readonly Key keyHelp;

	private readonly Label shortcut;

	private bool shortcutReady;

	private bool muteReady = true;

	private int capturing;

	private string talkName = "Ctrl + Alt + V";

	private string muteName = "Ctrl + Alt + N";

	private string typeName = "Ctrl + Alt + T";

	private string hideName = "Ctrl + Alt + H";

	private string quietName = "Ctrl + Alt + P";

	private bool typeReady = true;

	private bool hideReady = true;

	private bool quietReady = true;

	private readonly Label muteShortcut;

	private readonly Key mics;

	private readonly ContextMenuStrip micMenu;

	private bool keySaved;

	private readonly Entry textEntry;

	private readonly Key textConnect;

	private readonly Key freeKey;

	private bool textSaved;

	private readonly List<KeyValuePair<Control, int>> lower = new List<KeyValuePair<Control, int>>();

	private string[] lastInstalled;

	private float filled;

	private bool expanded = true;

	private bool loaded;

	private bool syncing;

	internal bool Quitting;

	private readonly Key more;

	private readonly Key minimize;

	private readonly Key close;

	private readonly Label build;

	private readonly List<KeyValuePair<Control, int>> mainLeft = new List<KeyValuePair<Control, int>>();

	private readonly List<KeyValuePair<Control, int>> pageLeft = new List<KeyValuePair<Control, int>>();

	private readonly Dictionary<Control, bool> stops = new Dictionary<Control, bool>();

	private bool paged;

	private readonly List<Control> scrolled = new List<Control>();

	private readonly Dictionary<Control, int> tops = new Dictionary<Control, int>();

	private int pageView = 960;

	private int pageScroll;

	private int wheel;

	private readonly Rim topRim = new Rim();

	private readonly Rim bottomRim = new Rim();

	internal int ShortScreen;

	private readonly Key languages;

	private readonly ContextMenuStrip languageMenu;

	private readonly Slider volume;

	private readonly Readout volumeName;

	private readonly Slider pitchSlider;

	private readonly Readout pitchName;

	private readonly Slider paceSlider;

	private readonly Readout paceName;

	private readonly Slider robotSlider;

	private readonly Readout robotName;

	private readonly Slider glitchSlider;

	private readonly Readout glitchName;

	private readonly Slider sounds;

	private readonly Slider sleepAfter;

	private readonly Slider bubble;

	private readonly Readout bubbleName;

	private readonly Toggle intro;

	private readonly Toggle typing;

	private readonly Toggle hdGames;

	private readonly Toggle miniPlayer;

	private readonly Readout sleepName;

	private readonly Readout soundsName;

	private readonly Slider wander;

	private readonly Readout wanderName;

	private readonly Slider pops;

	private readonly Readout popsName;

	private readonly Toggle dance;

	private readonly Slider replies;

	private readonly Readout repliesName;

	private readonly Slider recall;

	private readonly Readout recallName;

	private readonly Entry custom;

	private readonly Label customNote;

	private readonly Slider perchFps;

	private readonly Readout perchFpsName;

	private static readonly int[] FrameSizes = new int[6] { 0, 100, 125, 150, 175, 200 };

	private static readonly string[] FrameSizeNames = new string[6] { "Auto", "100%", "125%", "150%", "175%", "200%" };

	private readonly Slider frameSize;

	private readonly Readout frameSizeName;

	private readonly Label perchNote;

	private readonly List<KeyValuePair<Control, int>> underVoice = new List<KeyValuePair<Control, int>>();

	private int lifted;

	private float autoZoom = 1f;

	private static readonly Blend Fade = Neon.Fade;

	private float zoom = 1f;

	private readonly Font strong = Type(9f, bold: true);

	internal string NicknameShown => nickname.Box.Text;

	internal bool SettingsShown => paged;

	internal string SizeStatus => sizeName.Text;

	internal bool TypingShown => typing.Checked;

	internal string HdGamesStatus
	{
		get
		{
			if (!hdGames.Visible)
			{
				return "-";
			}
			if (!hdGames.Checked)
			{
				return "off";
			}
			return "on";
		}
	}

	internal bool PlayerShown => miniPlayer.Checked;

	internal string HabitsStatus
	{
		get
		{
			if (!startup.Checked)
			{
				return "-";
			}
			return "startup";
		}
	}

	internal bool DiscordShown => discord.Checked;

	internal string JobsStatus => (cursor.Checked ? "cursor" : "-") + " | " + security.Text;

	internal string BlackwallStatus => security.Text + " | " + blackwallNote.Text;

	internal string SettingsStatus => languages.Text + " | " + volumeName.Text + " | " + soundsName.Text + " | " + wanderName.Text + " | " + sleepName.Text + " | " + (dance.Checked ? "dance" : "-") + " | " + (intro.Checked ? "intro" : "-") + " | " + repliesName.Text + " | " + recallName.Text + " | " + bubbleName.Text;

	protected override CreateParams CreateParams
	{
		get
		{
			CreateParams createParams = base.CreateParams;
			createParams.Style = (createParams.Style | 0x20000 | 0x80000) & -65537;
			return createParams;
		}
	}

	internal float Zoom => zoom;

	internal string FrameSizeShown => frameSizeName.Text;

	internal string PerchStatus => perchFpsName.Text + " | " + ((perchNote.Text.Length > 0) ? perchNote.Text : "-");

	internal string PopsShown => popsName.Text;

	internal bool PerchFits
	{
		get
		{
			Rectangle pageArea = PageArea;
			if (pageArea.Contains(perchFpsName.Bounds) && pageArea.Contains(perchFps.Bounds) && pageArea.Contains(hideKeys.Bounds))
			{
				return pageArea.Contains(quietKeys.Bounds);
			}
			return false;
		}
	}

	internal string Status => brainName.Text + " | " + brainNote.Text;

	internal bool WatchShown => watch.Checked;

	internal bool CaughtUpShown => caughtUp.Checked;

	internal bool StayShown => stayOnModel.Checked;

	internal bool RemembersShown => remembering.Checked;

	internal bool ElevenShown
	{
		get
		{
			if (voiceRow == 3 && cloudName == "ElevenLabs" && voiceRungs[3].Checked && !voiceRungs[0].Checked && !voiceRungs[1].Checked)
			{
				return !voiceRungs[2].Checked;
			}
			return false;
		}
	}

	internal string VoiceTierStatus
	{
		get
		{
			string[] array = new string[4] { "OFF", "PREBUILT LOCAL", "CUSTOM LOCAL", "CLOUD VOICE" };
			bool flag = voiceRow == 3 && cloudName == "ElevenLabs";
			return array[voiceRow] + " | " + ((voiceRow != 3) ? "-" : (flag ? "key" : cloudName)) + " | " + ((voiceRow == 1) ? "free voice" : ((voiceRow == 2) ? "voice model" : ((flag && keySaved) ? "voice id" : "-"))) + " | fold " + arrangedFold;
		}
	}

	private Rectangle PageArea
	{
		get
		{
			if (!paged)
			{
				return ClientRectangle;
			}
			return new Rectangle(0, -Scaled(pageScroll), ClientSize.Width, Math.Max(ClientSize.Height, Scaled(960)));
		}
	}

	internal string ScrollStatus => ((paged && pageView < 960) ? (pageView + " tall") : "whole") + " | " + ((topRim.Visible && bottomRim.Visible) ? "rims" : "-");

	internal int Scrolled => pageScroll;

	internal int ScrollMost => Math.Max(0, 960 - pageView);

	internal int BackTop => more.Top;

	internal bool HideKeyShows
	{
		get
		{
			if (ClientRectangle.Contains(hideKeys.Bounds))
			{
				return ClientRectangle.Contains(quietKeys.Bounds);
			}
			return false;
		}
	}

	internal string VoiceStyleStatus => pitchName.Text + " | " + paceName.Text + " | " + robotName.Text + " | " + glitchName.Text;

	internal string GapShown => gapName.Text;

	private int KeyRow
	{
		get
		{
			if (tier <= 0)
			{
				return 94;
			}
			return 128;
		}
	}

	internal bool TiersFit
	{
		get
		{
			Rectangle bounds = rungs[rungs.Length - 1].Bounds;
			bool flag = !textEntry.Visible || (textEntry.Top >= bounds.Bottom && textConnect.Right <= ClientSize.Width);
			int num = (textEntry.Visible ? textEntry.Bottom : bounds.Bottom);
			if (rungs[0].Top >= tierNote.Bottom && rungs[1].Top >= rungs[0].Bottom && rungs[2].Top >= rungs[1].Bottom && freeKey.Left >= rungs[1].Right && freeKey.Top >= rungs[1].Top && freeKey.Bottom <= rungs[1].Bottom && flag && num <= voiceHead.Top)
			{
				return rungs[0].Right <= ClientSize.Width;
			}
			return false;
		}
	}

	internal string TextStatus => "tier " + tier + " | " + brainName.Text + " | " + brainNote.Text + " | " + (textEntry.Visible ? (textEntry.Cue + " | " + textConnect.Text) : "no key row");

	internal string InstallShown
	{
		get
		{
			if (!(voiceFilled < 0f))
			{
				return installNote.Text + " | " + (int)Math.Round(voiceFilled * 100f) + "%";
			}
			return null;
		}
	}

	internal int CapriRowTop => showHide.Top;

	private int Fold
	{
		get
		{
			if (!off)
			{
				if (voiceRow != 3 || !(cloudName == "ElevenLabs") || !keySaved)
				{
					return 36;
				}
				return 0;
			}
			return 72;
		}
	}

	internal string YourVoiceStatus
	{
		get
		{
			List<string> list = new List<string>();
			foreach (ToolStripItem item in freeMenu.Items)
			{
				if (item is ToolStripMenuItem toolStripMenuItem && (toolStripMenuItem.Text.StartsWith("HD ", StringComparison.Ordinal) || toolStripMenuItem.Text.IndexOf("you", StringComparison.OrdinalIgnoreCase) >= 0))
				{
					list.Add(toolStripMenuItem.Text + (toolStripMenuItem.Checked ? " (picked)" : ""));
				}
			}
			return freeVoices.Text + " | " + string.Join(", ", list.ToArray());
		}
	}

	internal string FreeStatus => (freeVoices.Visible ? freeVoices.Text : "-") + " | " + (freeSetup.Visible ? freeSetup.Text : "-");

	internal string HdChoice
	{
		get
		{
			foreach (ToolStripItem item in freeMenu.Items)
			{
				if (item is ToolStripMenuItem toolStripMenuItem && toolStripMenuItem.Text.StartsWith("HD ", StringComparison.Ordinal))
				{
					return toolStripMenuItem.Text + (toolStripMenuItem.Enabled ? "" : " (off)") + (toolStripMenuItem.Checked ? " (picked)" : "");
				}
			}
			return "-";
		}
	}

	internal string LayersShown
	{
		get
		{
			if (layered != null && !layered.IsDisposed && layered.Visible)
			{
				return layered.Showing;
			}
			return null;
		}
	}

	internal string LayersKeyShown => layersKey.Text + (layersKey.Picked ? " (lit)" : "");

	internal string LayersCrowded
	{
		get
		{
			if (layered != null && !layered.IsDisposed)
			{
				return layered.Crowded();
			}
			return "not open";
		}
	}

	internal string SecurityShown
	{
		get
		{
			if (guard != null && !guard.IsDisposed && guard.Visible)
			{
				return guard.Showing;
			}
			return null;
		}
	}

	internal string SecurityCrowded
	{
		get
		{
			if (guard != null && !guard.IsDisposed)
			{
				return guard.Crowded();
			}
			return "not open";
		}
	}

	internal string ServerChoices => sheet.Choices;

	internal string CloudPicked => cloudName;

	internal string ModelChoices
	{
		get
		{
			List<string> list = new List<string>();
			foreach (ToolStripMenuItem item in modelMenu.Items)
			{
				list.Add(item.Text + (item.Checked ? " (picked)" : ""));
			}
			return string.Join(", ", list);
		}
	}

	internal string CloudMenu
	{
		get
		{
			List<string> list = new List<string>();
			foreach (ToolStripItem item in cloudMenu.Items)
			{
				list.Add(item.Text);
			}
			return string.Join(", ", list);
		}
	}

	internal string CloudShown => (cloudPick.Visible ? cloudPick.Text : "-") + " | " + (cloudVoice.Visible ? cloudVoice.Text : "-") + " | " + (customVoice.Visible ? customVoice.Text : "-");

	internal string ServerShown
	{
		get
		{
			if (sheet != null && !sheet.IsDisposed && sheet.Visible)
			{
				return sheet.Showing;
			}
			return "-";
		}
	}

	internal string ServerFields => sheet.Fields;

	internal string ServerHintPlace => sheet.HintPlace;

	internal string FishVoicesShown => sheet.Voices;

	internal Form ServerPanel => sheet;

	internal Point FreeMenuAt => new Point(freeVoices.Left, freeVoices.Bottom);

	internal string FreeMenuShown
	{
		get
		{
			List<string> list = new List<string>();
			foreach (ToolStripItem item in freeMenu.Items)
			{
				ToolStripMenuItem toolStripMenuItem = item as ToolStripMenuItem;
				list.Add((toolStripMenuItem == null) ? "-" : (toolStripMenuItem.Text + (toolStripMenuItem.Enabled ? "" : " (off)") + (toolStripMenuItem.Checked ? " (picked)" : "")));
			}
			return string.Join(", ", list.ToArray());
		}
	}

	internal bool ConfirmReady
	{
		get
		{
			if (confirmKeys.Visible)
			{
				return confirmKeys.Enabled;
			}
			return false;
		}
	}

	private Control PickedKey
	{
		get
		{
			if (capturing != 1)
			{
				if (capturing != 2)
				{
					if (capturing != 3)
					{
						if (capturing != 4)
						{
							if (capturing != 5)
							{
								return null;
							}
							return quietKeys;
						}
						return hideKeys;
					}
					return typeKeys;
				}
				return muteKeys;
			}
			return talkKeys;
		}
	}

	internal string KeysNoteText => keysNote.Text;

	internal bool Picking => capturing != 0;

	internal string TierNote => tierNote.Text;

	internal string KeyStatus => shortcut.Text + " | " + muteShortcut.Text;

	internal string KeysShown => talkKeys.Text + " | " + muteKeys.Text + " | " + typeKeys.Text + " | " + hideKeys.Text;

	internal string QuietKeyShown => quietKeys.Text;

	internal string MicStatus => "mic | " + mics.Text + " | " + (keyHelp.Visible ? "help" : "-") + " | " + ((!shortcut.Visible) ? "-" : (shortcutReady ? "keys" : "taken")) + " | " + ((!muteShortcut.Visible) ? "-" : (muteReady ? "mute" : "taken"));

	internal bool SpeechFixShown
	{
		get
		{
			if (speechFix && voiceNote.Cursor == Cursors.Hand)
			{
				return voiceNote.Text.EndsWith(" Click to open it.", StringComparison.Ordinal);
			}
			return false;
		}
	}

	internal string VoiceNoteText => voiceNote.Text;

	internal string UpdateStatus => updateNote.Text;

	internal string VoiceStatus => voiceNote.Text + " | " + voiceEntry.Box.Text + " | " + speechModels.Text + " | " + connect.Text;

	internal Mainframe(Companion owner, Icon icon, string version)
	{
		capri = owner;
		Text = "Capri Mainframe";
		Icon = icon;
		ShowInTaskbar = true;
		FormBorderStyle = FormBorderStyle.None;
		StartPosition = FormStartPosition.CenterScreen;
		AutoScaleMode = AutoScaleMode.Dpi;
		AutoScaleDimensions = new SizeF(96f, 96f);
		Font = Type(9.5f);
		BackColor = Ground;
		ForeColor = White;
		DoubleBuffered = true;
		ClientSize = new Size(380, 760 + KeyRow);
		Font font = Look.Mono(8.5f, FontStyle.Regular, GraphicsUnit.Point);
		Caption("MAINFRAME", 24, 15, Display(16f), Rose);
		build = Caption("v" + version, 200, 20, font, Faint);
		build.AutoSize = false;
		build.TextAlign = ContentAlignment.MiddleRight;
		build.Bounds = new Rectangle(230, 20, 62, 22);
		EventHandler click = (object param0, EventArgs param1) =>
		{
			WindowState = FormWindowState.Minimized;
		};
		minimize = Make("", 300, 20, 26, 22, click);
		minimize.Glyph = 1;
		minimize.AccessibleName = "Minimize";
		close = Make("", 330, 20, 26, 22, (object param0, EventArgs param1) =>
		{
			Close();
		});
		close.Glyph = 2;
		close.AccessibleName = "Close";
		more = Make("Settings", 146, 20, 84, 22, (object param0, EventArgs param1) =>
		{
			ShowSettings(!paged);
		});
		more.Accent = Cyan;
		more.Loud = true;
		int count = Controls.Count;
		for (int num = 0; num < count; num++)
		{
			Scrolls(Controls[num]);
		}
		Section("BRAIN >", 0, 58);
		brainName = Caption("", 28, 79, Type(11f, bold: true), White);
		brainNote = Caption("", 28, 101, Font, Faint);
		modelMenu = Dropdown();
		models = Make("", 28, 134, 244, 28, (object param0, EventArgs param1) =>
		{
			modelMenu.Show(models, new Point(0, models.Height));
		});
		models.Chevron = true;
		refresh = Make("Refresh", 280, 134, 76, 28, (object param0, EventArgs param1) =>
		{
			capri.FindBrain();
		});
		for (int num2 = 0; num2 < 3; num2++)
		{
			steps[num2] = Caption("", 28, 138 + num2 * 22, Font, Faint);
		}
		setup = Make("Set up her brain", 28, 226, 170, 28, (object param0, EventArgs param1) =>
		{
			if (capri.SettingUp)
			{
				capri.CancelSetup();
			}
			else
			{
				capri.SetUpBrain();
			}
		});
		setup.Accent = Cyan;
		setup.Loud = true;
		hint = Caption("Have Ollama? Open it, then press Refresh.", 28, 264, Look.Mono(9f, FontStyle.Italic, GraphicsUnit.Point), Faint);
		tierNote = Caption("Pick one. Only one brain runs at a time.", 28, 168, Look.Mono(8.5f, FontStyle.Italic, GraphicsUnit.Point), Faint);
		string[,] array = new string[3, 2]
		{
			{ "NORMAL", "Free · local model" },
			{ "HIGHER", "Free · NVIDIA API" },
			{ "MAX INTELLIGENCE", "ChatGPT or Claude API" }
		};
		for (int num3 = 0; num3 < rungs.Length; num3++)
		{
			int picked = num3;
			Rung rung = new Rung();
			rung.Text = array[num3, 0];
			rung.Hint = array[num3, 1];
			rung.Font = Type(9.5f, bold: true);
			rung.HintFont = Type(9f);
			rung.AccessibleName = array[num3, 0] + ", " + array[num3, 1];
			rung.Bounds = new Rectangle(28, 184 + 24 * num3, (num3 == 1) ? 232 : 328, 24);
			rung.CheckedChanged += (object param0, EventArgs param1) =>
			{
				if (!syncing && rungs[picked].Checked)
				{
					capri.ChooseTier(picked);
				}
			};
			Controls.Add(rung);
			rungs[num3] = rung;
		}
		rungs[0].Flag = "Start with this!";
		rungs[0].FlagFont = Type(9f);
		freeKey = Make("Free key", 266, 209, 90, 22, (object param0, EventArgs param1) =>
		{
			capri.OpenFreeKeyPage();
		});
		textEntry = Input(28, 262, 244);
		textEntry.Box.UseSystemPasswordChar = true;
		textEntry.Box.AccessibleName = "NVIDIA API key";
		textEntry.Cue = "Paste your free NVIDIA key";
		textConnect = Make("Connect", 280, 262, 76, 28, (object param0, EventArgs param1) =>
		{
			ConnectText();
		});
		textEntry.Box.TextChanged += (object param0, EventArgs param1) =>
		{
			RelabelText();
		};
		textEntry.Box.KeyDown += (object sender, KeyEventArgs e) =>
		{
			if (e.KeyCode == Keys.Return)
			{
				e.SuppressKeyPress = true;
				ConnectText();
			}
		};
		voiceHead = Lower(Section("VOICE >", 0, 184));
		shortcut = Caption("Ctrl + Alt + V to talk", 156, 181, font, Faint);
		shortcut.AutoSize = false;
		shortcut.TextAlign = ContentAlignment.MiddleRight;
		shortcut.Bounds = new Rectangle(156, 181, 200, 20);
		shortcut.Cursor = Cursors.Hand;
		shortcut.Click += (object param0, EventArgs param1) =>
		{
			ShowSettings(on: true);
			PickKeys(0);
		};
		Lower(shortcut);
		string[,] array2 = new string[4, 2]
		{
			{ "OFF", "Silent · text only" },
			{ "PREBUILT LOCAL", "Her voices · free" },
			{ "CUSTOM LOCAL", "Your own model" },
			{ "CLOUD VOICE", "Needs a key" }
		};
		for (int num4 = 0; num4 < voiceRungs.Length; num4++)
		{
			int row = num4;
			Rung rung2 = new Rung();
			rung2.AutoCheck = false;
			rung2.TabStop = true;
			rung2.Text = array2[num4, 0];
			rung2.Hint = array2[num4, 1];
			rung2.Font = Type(9.5f, bold: true);
			rung2.HintFont = Type(9f);
			rung2.AccessibleName = array2[num4, 0] + ", " + array2[num4, 1];
			rung2.Bounds = new Rectangle(28, 208 + 24 * num4, (num4 >= 2) ? 246 : 328, 24);
			rung2.Click += (object param0, EventArgs param1) =>
			{
				if (!syncing)
				{
					PickRow(row);
				}
			};
			Controls.Add(rung2);
			voiceRungs[num4] = Lower(rung2);
		}
		setUpModel = Lower(Make("Set up", 276, 257, 80, 22, (object param0, EventArgs param1) =>
		{
			OpenServer(cloud: false, null);
		}));
		setUpModel.AccessibleName = "Set up your own voice model";
		keyHelp = Lower(Make("Setup guide", 276, 281, 80, 22, (object param0, EventArgs param1) =>
		{
			CloudGuide();
		}));
		cloudMenu = Dropdown();
		string[] clouds = Clouds;
		foreach (string text in clouds)
		{
			string each = text;
			cloudMenu.Items.Add(Choice(each, chosen: false, (object param0, EventArgs param1) =>
			{
				PickCloud(each);
			}));
		}
		cloudPick = Lower(Make(Clouds[0], 28, 310, 100, 28, (object param0, EventArgs param1) =>
		{
			cloudMenu.Show(cloudPick, new Point(0, cloudPick.Height));
		}));
		cloudPick.Chevron = true;
		cloudPick.AccessibleName = "Cloud voice service";
		keyEntry = Lower(Input(136, 310, 136));
		keyEntry.Box.UseSystemPasswordChar = true;
		keyEntry.Box.AccessibleName = "ElevenLabs API key";
		connect = Lower(Make("Connect", 280, 310, 76, 28, (object param0, EventArgs param1) =>
		{
			Connect();
		}));
		keyEntry.Box.TextChanged += (object param0, EventArgs param1) =>
		{
			Relabel();
		};
		keyEntry.Box.KeyDown += (object sender, KeyEventArgs e) =>
		{
			if (e.KeyCode == Keys.Return)
			{
				e.SuppressKeyPress = true;
				Connect();
			}
		};
		cloudVoice = Lower(Make("", 136, 310, 220, 28, (object param0, EventArgs param1) =>
		{
			OpenServer(cloud: true, CloudApi(cloudName));
		}));
		voiceMenu = Dropdown();
		voiceEntry = Lower(Input(28, 346, 196));
		voiceEntry.Chevron = true;
		voiceEntry.Cue = "Voice ID";
		voiceEntry.Box.AccessibleName = "ElevenLabs voice ID";
		voiceEntry.Drop += (object param0, EventArgs param1) =>
		{
			voiceMenu.Show(voiceEntry, new Point(0, voiceEntry.Height));
		};
		voiceEntry.Box.KeyDown += (object sender, KeyEventArgs e) =>
		{
			if (e.KeyCode == Keys.Return)
			{
				e.SuppressKeyPress = true;
				capri.ChooseVoice(voiceEntry.Box.Text);
			}
		};
		voiceEntry.Box.Leave += (object param0, EventArgs param1) =>
		{
			capri.ChooseVoice(voiceEntry.Box.Text);
		};
		speechModelMenu = Dropdown();
		speechModels = Lower(Make("Default model", 232, 346, 124, 28, (object param0, EventArgs param1) =>
		{
			speechModelMenu.Show(speechModels, new Point(0, speechModels.Height));
		}));
		speechModels.Chevron = true;
		freeMenu = Dropdown();
		freeVoices = Lower(Make("Free voice: Her own", 28, 346, 196, 28, (object param0, EventArgs param1) =>
		{
			freeMenu.Show(freeVoices, new Point(0, freeVoices.Height));
		}));
		freeVoices.Chevron = true;
		freeVoices.Visible = false;
		customVoice = Lower(Make("Voice model", 28, 346, 328, 28, (object param0, EventArgs param1) =>
		{
			OpenServer(cloud: false, null);
		}));
		customVoice.Chevron = true;
		customVoice.AccessibleName = "Your voice model";
		customVoice.Visible = false;
		freeSetup = Lower(Make("Get free voice", 232, 346, 124, 28, (object param0, EventArgs param1) =>
		{
			if (capri.SettingUpVoice)
			{
				capri.CancelFreeVoice();
			}
			else
			{
				capri.SetUpFreeVoice();
			}
		}));
		freeSetup.Accent = Cyan;
		freeSetup.Loud = true;
		freeSetup.Visible = false;
		micMenu = Dropdown();
		mics = Lower(Make("Mic: default", 28, 382, 244, 28, (object param0, EventArgs param1) =>
		{
			ShowMics();
		}));
		mics.Chevron = true;
		mics.AccessibleName = "Microphone";
		Lower(Make("Test", 280, 382, 76, 28, (object param0, EventArgs param1) =>
		{
			capri.TestVoice();
		}));
		muteShortcut = Lower(Caption("Ctrl + Alt + N mutes mic", 28, 417, font, Faint));
		muteShortcut.Cursor = Cursors.Hand;
		muteShortcut.Click += (object param0, EventArgs param1) =>
		{
			ShowSettings(on: true);
			PickKeys(1);
		};
		Deactivate += (object param0, EventArgs param1) =>
		{
			CancelKeys();
		};
		voiceNote = Lower(Caption("", 28, 443, Font, Faint));
		voiceNote.Click += (object param0, EventArgs param1) =>
		{
			if (speechFix)
			{
				capri.OpenSpeechSettings();
			}
		};
		installNote = Caption("", 28, 445, Font, White);
		installNote.Visible = false;
		int num6 = 300;
		Lower(Section("CAPRI >", 0, 184 + num6));
		showHide = Lower(Make("Hide Capri", 28, 206 + num6, 104, 28, (object param0, EventArgs param1) =>
		{
			capri.ToggleHidden();
		}));
		Lower(Make("Bring here", 140, 206 + num6, 104, 28, (object param0, EventArgs param1) =>
		{
			capri.BringHere();
		}));
		perch = Lower(Make("Ride cursor", 252, 206 + num6, 104, 28, (object param0, EventArgs param1) =>
		{
			capri.TogglePerch();
		}));
		Lower(Make("Check for updates", 28, 250 + num6, 150, 28, (object param0, EventArgs param1) =>
		{
			capri.CheckUpdates();
		}));
		Key key = Lower(Make("Help", 186, 250 + num6, 62, 28, (object param0, EventArgs param1) =>
		{
			capri.OpenHelp();
		}));
		key.AccessibleName = "Help: ask Capri on the website";
		Key key2 = Lower(Make("Quit Capri", 256, 250 + num6, 100, 28, (object param0, EventArgs param1) =>
		{
			capri.Quit();
		}));
		key2.Accent = Hot;
		updateNote = Lower(Caption("", 28, 288 + num6, font, Faint));
		for (int num7 = count; num7 < Controls.Count; num7++)
		{
			mainLeft.Add(new KeyValuePair<Control, int>(Controls[num7], Controls[num7].Left));
		}
		int count2 = Controls.Count;
		Section("YOU >", 0, 58);
		Make("Open her notes", 236, 55, 120, 20, (object param0, EventArgs param1) =>
		{
			capri.OpenNotes();
		}).AccessibleName = "Open her notes: me.md, her notes about you";
		Named("Language", 0, 82);
		languageMenu = Dropdown();
		for (int num8 = 0; num8 < Brain.Languages.GetLength(0); num8++)
		{
			string picked2 = Brain.Languages[num8, 1];
			ToolStripMenuItem toolStripMenuItem = Choice(Brain.Languages[num8, 0], chosen: false, (object param0, EventArgs param1) =>
			{
				capri.SetLanguage(picked2);
				Sync();
			});
			toolStripMenuItem.Tag = picked2;
			languageMenu.Items.Add(toolStripMenuItem);
		}
		languages = Make("English", 184, 85, 172, 22, (object param0, EventArgs param1) =>
		{
			languageMenu.Show(languages, new Point(0, languages.Height));
		});
		languages.Chevron = true;
		languages.AccessibleName = "Her language";
		Named("What should she call you?", 0, 110);
		Make("Get to know me", 236, 113, 120, 22, (object param0, EventArgs param1) =>
		{
			capri.GetToKnow();
		}).AccessibleName = "Get to know me: she asks what to call you, your pronouns and what you're into, and keeps them in me.md";
		nickname = Input(28, 141, 244, 22);
		nickname.Cue = "Your name or a nickname";
		nickname.Box.AccessibleName = "What should she call you?";
		nickname.Box.KeyDown += (object sender, KeyEventArgs e) =>
		{
			if (e.KeyCode == Keys.Return)
			{
				e.SuppressKeyPress = true;
				UseNickname();
			}
		};
		Make("Save", 280, 141, 76, 22, (object param0, EventArgs param1) =>
		{
			UseNickname();
		});
		Section("VOICE >", 0, 180);
		styleNote = Caption("Every voice, ElevenLabs too", 88, 178, font, Faint);
		styleNote.AutoSize = false;
		styleNote.TextAlign = ContentAlignment.MiddleRight;
		styleNote.Bounds = new Rectangle(88, 178, 192, 20);
		layersKey = Make("Layers", 286, 177, 70, 20, (object param0, EventArgs param1) =>
		{
			OpenLayers();
		});
		layersKey.AccessibleName = "Voice layers: two copies of her voice under her, each with its own pitch and delay";
		volume = Setting("Volume", VolumeNames, 0, 204, "Her voice volume");
		volumeName = new Readout(volume);
		volume.Changed += (object param0, EventArgs param1) =>
		{
			if (!syncing)
			{
				volumeName.Text = VolumeNames[volume.Value];
				capri.SetVolume(Volumes[volume.Value]);
			}
		};
		pitchSlider = Setting("Pitch", PitchNames, 0, 232, "Her voice's pitch");
		pitchName = new Readout(pitchSlider);
		pitchSlider.Changed += (object param0, EventArgs param1) =>
		{
			if (!syncing)
			{
				pitchName.Text = PitchNames[pitchSlider.Value];
				capri.SetVoicePitch(pitchSlider.Value - 6);
				ShowLayers();
			}
		};
		pitchSlider.Released += (object param0, EventArgs param1) =>
		{
			if (!syncing)
			{
				capri.HearVoiceStyle();
			}
		};
		paceSlider = Setting("Speed", PaceNames, 0, 260, "Her voice's speed");
		paceName = new Readout(paceSlider);
		paceSlider.Changed += (object param0, EventArgs param1) =>
		{
			if (!syncing)
			{
				paceName.Text = PaceNames[paceSlider.Value];
				capri.SetVoiceSpeed(Paces[paceSlider.Value]);
			}
		};
		paceSlider.Released += (object param0, EventArgs param1) =>
		{
			if (!syncing)
			{
				capri.HearVoiceStyle();
			}
		};
		robotSlider = Setting("Robot", AmountNames, 0, 288, "How robotic her voice sounds");
		robotName = new Readout(robotSlider);
		robotSlider.Changed += (object param0, EventArgs param1) =>
		{
			if (!syncing)
			{
				robotName.Text = AmountName(robotSlider.Value);
				capri.SetVoiceRobot(Amounts[robotSlider.Value]);
			}
		};
		robotSlider.Released += (object param0, EventArgs param1) =>
		{
			if (!syncing)
			{
				capri.HearVoiceStyle();
			}
		};
		glitchSlider = Setting("Glitch", AmountNames, 0, 316, "How much her voice glitches");
		glitchName = new Readout(glitchSlider);
		glitchSlider.Changed += (object param0, EventArgs param1) =>
		{
			if (!syncing)
			{
				glitchName.Text = AmountName(glitchSlider.Value);
				capri.SetVoiceGlitch(Amounts[glitchSlider.Value]);
			}
		};
		glitchSlider.Released += (object param0, EventArgs param1) =>
		{
			if (!syncing)
			{
				capri.HearVoiceStyle();
			}
		};
		sounds = Setting("Sounds", SoundNames, 0, 344, "Sound effects volume");
		soundsName = new Readout(sounds);
		sounds.Changed += (object param0, EventArgs param1) =>
		{
			if (!syncing)
			{
				soundsName.Text = SoundNames[sounds.Value];
				capri.SetSoundLevel(sounds.Value * 10);
			}
		};
		typing = Switch("Typing sound", 0, 372, (object param0, EventArgs param1) =>
		{
			if (!syncing)
			{
				capri.ToggleTyping();
			}
		});
		typing.Hint = "When she types";
		typing.HintFont = font;
		typing.AccessibleName = "Typing sound. A soft typing sound while her text replies type out";
		hdGames = Switch("Free voice in games", 0, 400, (object param0, EventArgs param1) =>
		{
			if (!syncing)
			{
				capri.ToggleHdGameRest();
			}
		});
		hdGames.AccessibleName = "Free voice in games: while a game or video is full screen, her free voice talks instead of her HD voice, so the game gets the graphics card";
		hdGames.Visible = false;
		int count3 = Controls.Count;
		Section("MOVES >", 0, 442);
		miniPlayer = Switch("Mini player", 0, 466, (object param0, EventArgs param1) =>
		{
			if (!syncing)
			{
				capri.TogglePlayer();
			}
		});
		miniPlayer.AccessibleName = "Mini player. It shows what's playing while music plays, and she sits on it";
		wander = Setting("Wander", WanderNames, 0, 494, "How much she wanders");
		wanderName = new Readout(wander);
		wander.Changed += (object param0, EventArgs param1) =>
		{
			if (!syncing)
			{
				wanderName.Text = WanderNames[wander.Value];
				capri.SetWanderLevel(wander.Value);
			}
		};
		sleepAfter = Setting("Sleep", SleepNames, 0, 522, "How long until she falls asleep");
		sleepName = new Readout(sleepAfter);
		sleepAfter.Changed += (object param0, EventArgs param1) =>
		{
			if (!syncing)
			{
				sleepName.Text = SleepNames[sleepAfter.Value];
				capri.SetSleepAfter(Sleeps[sleepAfter.Value]);
			}
		};
		pops = Setting("Pop-ins", PopNames, 0, 550, "How often she pops in to say hi");
		popsName = new Readout(pops);
		pops.Changed += (object param0, EventArgs param1) =>
		{
			if (!syncing)
			{
				popsName.Text = PopNames[pops.Value];
				capri.SetPopPace(pops.Value);
			}
		};
		dance = Switch("Dance to music", 0, 578, (object param0, EventArgs param1) =>
		{
			if (!syncing)
			{
				capri.ToggleDance();
			}
		});
		intro = Switch("Glitch in at startup", 0, 606, (object param0, EventArgs param1) =>
		{
			if (!syncing)
			{
				capri.ToggleIntro();
			}
		});
		Section("KEYS >", 0, 648);
		talkKeys = Make("Talk: Ctrl + Alt + V", 28, 675, 328, 22, (object param0, EventArgs param1) =>
		{
			PickKeys(0);
		});
		talkKeys.AccessibleName = "Her talk key. Click, press new keys, then Confirm";
		muteKeys = Make("Mute mic: Ctrl + Alt + N", 28, 703, 328, 22, (object param0, EventArgs param1) =>
		{
			PickKeys(1);
		});
		muteKeys.AccessibleName = "Her mute key. Click, press new keys, then Confirm";
		typeKeys = Make("Type: Ctrl + Alt + T", 28, 731, 328, 22, (object param0, EventArgs param1) =>
		{
			PickKeys(2);
		});
		typeKeys.AccessibleName = "Her typing key. Click, press new keys, then Confirm";
		hideKeys = Make("Hide/show: Ctrl + Alt + H", 28, 759, 328, 22, (object param0, EventArgs param1) =>
		{
			PickKeys(3);
		});
		hideKeys.AccessibleName = "Her hide key. Click, press new keys, then Confirm";
		quietKeys = Make("Quiet: Ctrl + Alt + P", 28, 787, 328, 22, (object param0, EventArgs param1) =>
		{
			PickKeys(4);
		});
		quietKeys.AccessibleName = "Her quiet key: she only talks when you talk to her until you press it again. Click, press new keys, then Confirm";
		confirmKeys = Make("Confirm", 212, 645, 78, 20, (object param0, EventArgs param1) =>
		{
			ConfirmKeys();
		});
		confirmKeys.Accent = Cyan;
		confirmKeys.Loud = true;
		confirmKeys.AccessibleName = "Confirm her new keys";
		cancelKeys = Make("Cancel", 296, 645, 60, 20, (object param0, EventArgs param1) =>
		{
			CancelKeys();
		});
		cancelKeys.AccessibleName = "Cancel and keep her old keys";
		confirmKeys.Visible = (cancelKeys.Visible = false);
		keysNote = Caption("", 28, 816, font, Faint);
		keysNote.MaximumSize = new Size(328, 0);
		for (int num9 = count3; num9 < Controls.Count; num9++)
		{
			underVoice.Add(new KeyValuePair<Control, int>(Controls[num9], Controls[num9].Top));
		}
		Section("CAPRI >", 380, 58);
		sizeSlider = Setting("Size", SizeNames, 380, 82, "Her size");
		sizeName = new Readout(sizeSlider);
		sizeSlider.Changed += (object param0, EventArgs param1) =>
		{
			if (!syncing)
			{
				sizeName.Text = SizeNames[sizeSlider.Value];
				capri.SetCharacterSize(SizeSteps[sizeSlider.Value]);
			}
		};
		checkIns = Switch("Check in on me", 380, 110, (object param0, EventArgs param1) =>
		{
			if (!syncing)
			{
				capri.ToggleCheckIns();
			}
		});
		onTop = Switch("Always on top", 380, 138, (object param0, EventArgs param1) =>
		{
			if (!syncing)
			{
				capri.ToggleOnTop();
			}
		});
		frameSize = Setting("Mainframe size", FrameSizeNames, 380, 166, "Mainframe size. Auto makes it bigger when Virtual Super Resolution or DSR shrinks a big desktop onto a smaller screen");
		frameSizeName = new Readout(frameSize);
		frameSize.Changed += (object param0, EventArgs param1) =>
		{
			if (!syncing)
			{
				ShowFrameSize();
			}
		};
		frameSize.Released += (object param0, EventArgs param1) =>
		{
			if (!syncing)
			{
				capri.SetMainframeSize(FrameSizes[frameSize.Value]);
				Rezoom(Wanted());
				ShowFrameSize();
			}
		};
		Section("WATCH >", 380, 208);
		watch = Switch("Watch my screen", 380, 232, (object param0, EventArgs param1) =>
		{
			if (!syncing)
			{
				capri.ToggleWatch();
			}
		});
		watch.AccessibleName = "Watch my screen. She comments now and then";
		caughtUp = Switch("Keep her caught up", 380, 260, (object param0, EventArgs param1) =>
		{
			if (!syncing)
			{
				capri.ToggleCaughtUp();
			}
		});
		caughtUp.AccessibleName = "Keep her caught up on my screen. Glances she stays quiet about still go in her memory, which uses a little more of your API";
		gap = Setting("Every", GapNames, 380, 288, "Time between glances");
		gapName = new Readout(gap);
		gap.Changed += (object param0, EventArgs param1) =>
		{
			if (!syncing)
			{
				gapName.Text = GapNames[gap.Value];
				capri.SetGlanceGap(Gaps[gap.Value]);
			}
		};
		Section("BRAIN >", 380, 330);
		stayOnModel = Switch("Stay on my model", 380, 354, (object param0, EventArgs param1) =>
		{
			if (!syncing)
			{
				capri.ToggleStayOnModel();
			}
		});
		stayOnModel.AccessibleName = "Stay on my model. When an NVIDIA model gets stuck she says so instead of switching to another one";
		replies = Setting("Replies", Lengths, 380, 382, "Reply length");
		repliesName = new Readout(replies);
		replies.Changed += (object param0, EventArgs param1) =>
		{
			if (!syncing)
			{
				repliesName.Text = Lengths[replies.Value];
				capri.SetWordiness(replies.Value);
			}
		};
		recall = Setting("Memory", Lengths, 380, 410, "How much of the chat she keeps");
		recallName = new Readout(recall);
		recall.Changed += (object param0, EventArgs param1) =>
		{
			if (!syncing)
			{
				recallName.Text = Lengths[recall.Value];
				capri.SetRecall(Recalls[recall.Value]);
			}
		};
		bubble = Setting("Bubble", BubbleNames, 380, 438, "How long her replies stay up");
		bubbleName = new Readout(bubble);
		bubble.Changed += (object param0, EventArgs param1) =>
		{
			if (!syncing)
			{
				bubbleName.Text = BubbleNames[bubble.Value];
				capri.SetBubbleTime(bubble.Value);
			}
		};
		custom = Input(408, 469, 244, 22);
		custom.Cue = "Custom model name";
		custom.Box.AccessibleName = "Custom model name";
		custom.Box.KeyDown += (object sender, KeyEventArgs e) =>
		{
			if (e.KeyCode == Keys.Return)
			{
				e.SuppressKeyPress = true;
				UseCustom();
			}
		};
		Make("Use", 660, 469, 76, 22, (object param0, EventArgs param1) =>
		{
			UseCustom();
		});
		customNote = Caption("Any Ollama model you've pulled, or a model id on your key.", 408, 498, font, Faint);
		customNote.MaximumSize = new Size(328, 0);
		Section("HABITS >", 380, 544);
		Make("Open her memories", 600, 541, 136, 20, (object param0, EventArgs param1) =>
		{
			capri.OpenMemories();
		}).AccessibleName = "Open her memories: memories.md and her chat transcripts, kept on this PC";
		startup = Switch("Start with Windows", 380, 568, (object param0, EventArgs param1) =>
		{
			if (!syncing)
			{
				capri.ToggleStartup();
			}
		});
		discord = Switch("Show on Discord", 380, 596, (object param0, EventArgs param1) =>
		{
			if (!syncing)
			{
				capri.ToggleDiscord();
			}
		});
		cursor = Switch("Use her cursor", 380, 624, (object param0, EventArgs param1) =>
		{
			if (!syncing)
			{
				capri.ToggleCursor();
			}
		});
		remembering = Switch("Remember our chats", 380, 652, (object param0, EventArgs param1) =>
		{
			if (!syncing)
			{
				capri.ToggleRemembering();
			}
		});
		remembering.AccessibleName = "Remember our chats. She keeps daily transcripts and memories.md on this PC, never uploaded";
		security = Make("Security: " + Security.PresetName, 408, 683, 328, 22, (object param0, EventArgs param1) =>
		{
			OpenSecurity();
		});
		security.AccessibleName = "Security: what she does on your PC without asking first";
		blackwallNote = Caption(Security.Short, 408, 707, font, Faint);
		blackwallNote.AutoSize = false;
		blackwallNote.TextAlign = ContentAlignment.MiddleRight;
		blackwallNote.Bounds = new Rectangle(408, 707, 328, 18);
		perchFps = Setting("Perch", PerchNames, 380, 728, "Her perch frames per second");
		perchFpsName = new Readout(perchFps);
		perchFps.Changed += (object param0, EventArgs param1) =>
		{
			if (!syncing)
			{
				capri.SetPerchFps(Companion.PerchRates[perchFps.Value]);
				ShowPerch();
			}
		};
		perchNote = Caption("", 408, 756, font, Faint);
		perchNote.AutoSize = false;
		perchNote.TextAlign = ContentAlignment.MiddleRight;
		perchNote.Bounds = new Rectangle(408, 756, 328, 18);
		Section("EYES >", 380, 790);
		Make("Peek now", 600, 787, 136, 20, (object param0, EventArgs param1) =>
		{
			capri.PeekNow();
		}).AccessibleName = "Make one of her eyes peek in right now";
		eyesOn = Switch("Eye pop-ups", 380, 814, (object param0, EventArgs param1) =>
		{
			if (!syncing)
			{
				capri.ToggleEyes();
			}
		});
		eyesOn.AccessibleName = "Eye pop-ups: her eyes peek in around the screen now and then";
		eyePace = Setting("How often", EyePaceNames, 380, 842, "How often her eyes peek in");
		eyePaceName = new Readout(eyePace);
		eyePace.Changed += (object param0, EventArgs param1) =>
		{
			if (!syncing)
			{
				eyePaceName.Text = EyePaceNames[eyePace.Value];
				capri.SetEyePace(eyePace.Value);
			}
		};
		eyeSize = Setting("Eye size", EyeSizeNames, 380, 870, "How big her eyes are");
		eyeSizeName = new Readout(eyeSize);
		eyeSize.Changed += (object param0, EventArgs param1) =>
		{
			if (!syncing)
			{
				eyeSizeName.Text = EyeSizeNames[eyeSize.Value];
				capri.SetEyeSize(eyeSize.Value);
			}
		};
		eyeMost = Setting("Most at once", EyeMostNames, 380, 898, "How many eyes can show at once");
		eyeMostName = new Readout(eyeMost);
		eyeMost.Changed += (object param0, EventArgs param1) =>
		{
			if (!syncing)
			{
				eyeMostName.Text = EyeMostNames[eyeMost.Value];
				capri.SetEyeMost(eyeMost.Value);
			}
		};
		for (int num10 = count2; num10 < Controls.Count; num10++)
		{
			Control control = Controls[num10];
			pageLeft.Add(new KeyValuePair<Control, int>(control, control.Left));
			Scrolls(control);
			control.Left += 30000;
			Reachable(control, on: false);
			control.Enter += (object param0, EventArgs param1) =>
			{
				if (capturing != 0 && control != confirmKeys && control != cancelKeys && control != PickedKey)
				{
					CancelKeys();
				}
			};
		}
		Controls.Add(topRim);
		Controls.Add(bottomRim);
		topRim.BringToFront();
		bottomRim.BringToFront();
		Searching();
		Sync();
		Application.AddMessageFilter(this);
	}

	private Head Section(string title, int x, int y)
	{
		return Heading(this, title, new Point(28 + x, y), Type(10f, bold: true), White, 352);
	}

	private static Head Heading(Control window, string text, Point at, Font font, Color colour, int span)
	{
		Head head = new Head();
		head.Text = text;
		head.Font = font;
		head.ForeColor = colour;
		head.BackColor = Ground;
		head.Span = span;
		head.Location = at;
		window.Controls.Add(head);
		return head;
	}

	private Label Named(string name, int x, int y)
	{
		return Caption(name, 28 + x, y + 6, Font, Rose);
	}

	private Slider Setting(string name, string[] steps, int x, int y, string spoken)
	{
		Named(name, x, y);
		Slider slider = new Slider(steps);
		slider.Font = Type(9.5f);
		slider.Bounds = new Rectangle(184 + x, y + 3, 172, 22);
		slider.AccessibleName = spoken;
		Controls.Add(slider);
		return slider;
	}

	private Toggle Switch(string name, int x, int y, EventHandler changed)
	{
		Toggle toggle = new Toggle();
		toggle.Text = name;
		toggle.Font = Font;
		toggle.Bounds = new Rectangle(28 + x, y + 3, 328, 22);
		toggle.CheckedChanged += changed;
		Controls.Add(toggle);
		return toggle;
	}

	private void UseNickname()
	{
		capri.SetNickname(nickname.Box.Text);
		nickname.Box.Text = capri.Nickname;
	}

	internal void TypeNickname(string typed)
	{
		nickname.Box.Text = typed;
		UseNickname();
	}

	private void UseCustom()
	{
		customNote.Text = capri.ChooseCustomModel(custom.Box.Text, lastInstalled);
	}

	private void ShowHdGames()
	{
		bool flag = syncing;
		bool hdVoiceOn = capri.HdVoiceOn;
		syncing = true;
		hdGames.Visible = hdVoiceOn;
		hdGames.Checked = capri.HdGameRest;
		syncing = flag;
		int num = ((!hdVoiceOn) ? 28 : 0);
		if (num == lifted)
		{
			return;
		}
		lifted = num;
		int to = pageScroll;
		ScrollPage(0);
		foreach (KeyValuePair<Control, int> item in underVoice)
		{
			item.Key.Top = Scaled(item.Value - num);
		}
		ScrollPage(to);
	}

	internal void FlipPlayer()
	{
		miniPlayer.Checked = !miniPlayer.Checked;
	}

	internal void ShowSettings(bool on)
	{
		if (!on)
		{
			CancelKeys();
		}
		paged = on;
		more.Text = (on ? "Back" : "Settings");
		more.Invalidate();
		Place();
		foreach (KeyValuePair<Control, int> item in mainLeft)
		{
			Reachable(item.Key, !on);
		}
		foreach (KeyValuePair<Control, int> item2 in pageLeft)
		{
			Reachable(item2.Key, on);
		}
		if (Hidden(ActiveControl))
		{
			more.Focus();
		}
		if (on)
		{
			Sync();
		}
		wheel = 0;
		Arrange(expanded);
		Rectangle workingArea = Screen.FromControl(this).WorkingArea;
		if (on && Right > workingArea.Right)
		{
			Left = Math.Max(workingArea.Left, workingArea.Right - Width);
		}
		if (on && Bottom > workingArea.Bottom)
		{
			Top = Math.Max(workingArea.Top, workingArea.Bottom - Height);
		}
	}

	private void Place()
	{
		foreach (KeyValuePair<Control, int> item in mainLeft)
		{
			item.Key.Left = Scaled(item.Value) + (paged ? 30000 : 0);
		}
		foreach (KeyValuePair<Control, int> item2 in pageLeft)
		{
			item2.Key.Left = Scaled(item2.Value) + ((!paged) ? 30000 : 0);
		}
		int num = (paged ? 380 : 0);
		build.Left = Scaled(230 + num);
		minimize.Left = Scaled(300 + num);
		close.Left = Scaled(330 + num);
	}

	private void Scrolls(Control control)
	{
		scrolled.Add(control);
		control.Enter += (object param0, EventArgs param1) =>
		{
			Reveal(control);
		};
	}

	private void Reveal(Control control)
	{
		if (paged && pageView < 960 && Control.MouseButtons == MouseButtons.None)
		{
			float num = Unit();
			int num2 = Scaled(8);
			int num3 = pageScroll - (int)Math.Ceiling((float)(num2 - control.Top) / num);
			if (control.Top < num2)
			{
				ScrollPage((num3 >= 72) ? num3 : 0);
			}
			else if (control.Bottom > ClientSize.Height - num2)
			{
				ScrollPage(pageScroll + (int)Math.Ceiling((float)(control.Bottom - ClientSize.Height + num2) / num));
			}
		}
	}

	private void ScrollPage(int to)
	{
		int num = (paged ? Math.Max(0, Math.Min(960 - pageView, to)) : 0);
		if (num == pageScroll)
		{
			return;
		}
		if (pageScroll == 0)
		{
			foreach (Control item in scrolled)
			{
				tops[item] = item.Top;
			}
		}
		pageScroll = num;
		int num2 = Scaled(num);
		Native.SendMessage(Handle, 11, IntPtr.Zero, IntPtr.Zero);
		try
		{
			foreach (Control item2 in scrolled)
			{
				item2.Top = tops[item2] - num2;
			}
		}
		finally
		{
			Native.SendMessage(Handle, 11, new IntPtr(1), IntPtr.Zero);
		}
		Invalidate(invalidateChildren: true);
	}

	private int Fit()
	{
		int val = ((ShortScreen > 0) ? ShortScreen : ((int)((float)Screen.FromControl(this).WorkingArea.Height / Unit())));
		return Math.Max(144, Math.Min(960, val));
	}

	protected override void OnMouseWheel(MouseEventArgs e)
	{
		base.OnMouseWheel(e);
		if (paged && pageView < 960)
		{
			wheel += e.Delta;
			int num = wheel / 120;
			wheel -= num * 120;
			if (num != 0)
			{
				ScrollPage(pageScroll - num * 36);
			}
		}
	}

	private void Reachable(Control control, bool on)
	{
		if (!stops.ContainsKey(control))
		{
			stops[control] = control.TabStop;
		}
		control.TabStop = on && stops[control];
		foreach (Control control2 in control.Controls)
		{
			Reachable(control2, on);
		}
	}

	private bool Hidden(Control control)
	{
		List<KeyValuePair<Control, int>> list = (paged ? mainLeft : pageLeft);
		Control control2 = control;
		while (control2 != null && control2 != this)
		{
			foreach (KeyValuePair<Control, int> item in list)
			{
				if (item.Key == control2)
				{
					return true;
				}
			}
			control2 = control2.Parent;
		}
		return false;
	}

	internal string TabWalk()
	{
		Control control = more;
		int num = 0;
		for (int i = 0; i < 80; i++)
		{
			if (!SelectNextControl(control, forward: true, tabStopOnly: true, nested: true, wrap: true))
			{
				break;
			}
			Control control2 = ActiveControl;
			if (control2 == null || control2 == control || control2 == more)
			{
				break;
			}
			if (Hidden(control2))
			{
				return "Tab reached a hidden " + (control2.AccessibleName ?? control2.Text);
			}
			num++;
			control = control2;
		}
		more.Focus();
		if (num != 0)
		{
			return null;
		}
		return "Tab reached nothing";
	}

	private T Lower<T>(T control) where T : Control
	{
		lower.Add(new KeyValuePair<Control, int>(control, control.Top));
		return control;
	}

	protected override void WndProc(ref Message message)
	{
		base.WndProc(ref message);
		if (message.Msg == 132 && message.Result == new IntPtr(1))
		{
			message.Result = new IntPtr(2);
		}
	}

	protected override void OnLoad(EventArgs e)
	{
		base.OnLoad(e);
		loaded = true;
		Rezoom(Wanted());
		ShowFrameSize();
		Arrange(expanded);
	}

	private float Wanted()
	{
		float num = Panels.Shrink(Screen.FromControl(this));
		autoZoom = Math.Max(1f, Math.Min(2f, (float)Math.Round(num / DeviceScale() * 20f) / 20f));
		int mainframeSize = capri.MainframeSize;
		if (mainframeSize <= 0)
		{
			return autoZoom;
		}
		return (float)mainframeSize / 100f;
	}

	private void ShowFrameSize()
	{
		int num = (int)Math.Round(autoZoom * 100f);
		frameSizeName.Text = ((frameSize.Value == 0 && num != 100) ? ("Auto · " + num + "%") : FrameSizeNames[frameSize.Value]);
	}

	private void Rezoom(float next)
	{
		if (loaded && !(Math.Abs(next - zoom) < 0.005f))
		{
			float factor = next / zoom;
			ScrollPage(0);
			SuspendLayout();
			Grow(this, factor);
			ContextMenuStrip[] array = new ContextMenuStrip[7] { modelMenu, cloudMenu, voiceMenu, speechModelMenu, freeMenu, micMenu, languageMenu };
			foreach (ContextMenuStrip contextMenuStrip in array)
			{
				contextMenuStrip.Font = Font;
			}
			Scale(new SizeF(factor, factor));
			zoom = next;
			ResumeLayout(performLayout: false);
			Place();
			Arrange(expanded);
			Shape();
		}
	}

	internal void SizeMainframe(int size)
	{
		frameSize.Slide(Math.Max(0, Array.IndexOf(FrameSizes, size)));
	}

	private static void Grow(Control control, float factor)
	{
		foreach (Control control2 in control.Controls)
		{
			Grow(control2, factor);
		}
		control.Font = Bigger(control.Font, factor);
		if (control is Rung rung)
		{
			rung.HintFont = Bigger(rung.HintFont, factor);
			rung.FlagFont = Bigger(rung.FlagFont, factor);
		}
		if (control is Toggle toggle)
		{
			toggle.HintFont = Bigger(toggle.HintFont, factor);
		}
	}

	private static Font Bigger(Font font, float factor)
	{
		if (font != null)
		{
			return new Font(font.FontFamily, font.SizeInPoints * factor, font.Style, GraphicsUnit.Point);
		}
		return null;
	}

	protected override void OnSizeChanged(EventArgs e)
	{
		base.OnSizeChanged(e);
		Shape();
		Invalidate(invalidateChildren: true);
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		base.OnPaint(e);
		float num = Unit();
		Graphics graphics = e.Graphics;
		Backdrop(graphics, this, num);
		graphics.SmoothingMode = SmoothingMode.AntiAlias;
		Edging(graphics, this, num);
		if (paged)
		{
			if (pageView < 960)
			{
				float num2 = (float)Height - 36f * num;
				float num3 = num2 * (float)pageView / 860f;
				float num4 = 18f * num + (num2 - num3) * (float)pageScroll / (float)(960 - pageView);
				using (Brush brush = new SolidBrush(Dash))
				{
					graphics.FillRectangle(brush, (float)Width - 8f * num, 18f * num, 2f * num, num2);
				}
				using Brush brush2 = new SolidBrush(Cyan);
				graphics.FillRectangle(brush2, (float)Width - 8f * num, num4, 2f * num, num3);
			}
		}
		else
		{
			int num5 = (expanded ? 118 : 0) + KeyRow;
			int num6 = ((voiceFilled >= 0f) ? 40 : 0);
			if (expanded)
			{
				Dots(graphics, num, 210, filled);
			}
			if (num6 > 0)
			{
				Dots(graphics, num, 467 + num5 - arrangedFold, voiceFilled);
			}
		}
	}

	private static void Dots(Graphics graphics, float s, int y, float share)
	{
		float num = 6f * s;
		int num2 = 54;
		int num3 = (int)Math.Round((float)num2 * share);
		using Brush brush = new SolidBrush(Cyan);
		using Brush brush2 = new SolidBrush(Dash);
		for (int i = 0; i < num2; i++)
		{
			graphics.FillRectangle((i < num3) ? brush : brush2, 28f * s + (float)i * num, (float)y * s, num - 2f * s, 5f * s);
		}
	}

	private static void Backdrop(Graphics graphics, Control control, float s)
	{
		graphics.Clear(Ground);
		Form form = (control as Form) ?? control.FindForm();
		if (form == null)
		{
			return;
		}
		int num = 0;
		int num2 = 0;
		Control control2 = control;
		while (control2 != null && control2 != form)
		{
			num += control2.Left;
			num2 += control2.Top;
			control2 = control2.Parent;
		}
		GraphicsState gstate = graphics.Save();
		graphics.TranslateTransform(-num, -num2);
		float num3 = 210f * s;
		if ((float)num2 < num3)
		{
			using LinearGradientBrush linearGradientBrush = new LinearGradientBrush(new RectangleF(0f, -1f, 16f, num3 + 2f), Glow, Ground, LinearGradientMode.Vertical);
			linearGradientBrush.Blend = Fade;
			graphics.FillRectangle(linearGradientBrush, num, num2, control.Width, Math.Min(control.Height, num3 - (float)num2));
		}
		foreach (Control control3 in form.Controls)
		{
			if (!(control3 is Head { Span: >0, Visible: not false, Left: var left } head) || left >= form.ClientSize.Width)
			{
				continue;
			}
			RectangleF rect = head.Band(s);
			if (!(rect.Bottom <= (float)num2) && !(rect.Top >= (float)(num2 + control.Height)) && !(rect.Right <= (float)num) && !(rect.Left >= (float)(num + control.Width)))
			{
				using (Brush brush = new SolidBrush(Band))
				{
					graphics.FillRectangle(brush, rect);
				}
				using (Brush brush2 = new SolidBrush(Rule))
				{
					graphics.FillRectangle(brush2, rect.Left, rect.Top, rect.Width, Math.Max(1, (int)s));
				}
				using Brush brush3 = new SolidBrush(Hot);
				graphics.FillRectangle(brush3, rect.Left, rect.Top + 7f * s, 2.5f * s, rect.Height - 13f * s);
			}
		}
		graphics.Restore(gstate);
	}

	private static GraphicsPath Cut(RectangleF box, float topLeft, float topRight, float bottomRight, float bottomLeft)
	{
		return Neon.Cut(box, topLeft, topRight, bottomRight, bottomLeft);
	}

	private static void Edging(Graphics graphics, Control window, float s)
	{
		Neon.Frame(graphics, window.Size, s, 16f * s, 6f * s, 64f * s, 34f * s);
	}

	private static void Clip(Form window, float s)
	{
		Neon.Clip(window, 16f * s, 6f * s);
	}

	private void Shape()
	{
		Clip(this, Unit());
	}

	private static FontFamily Family(string name)
	{
		try
		{
			return new FontFamily(name);
		}
		catch (ArgumentException)
		{
			return null;
		}
	}

	private static Font Type(float points, bool bold = false)
	{
		FontFamily fontFamily = (bold ? Heavy : Plain);
		if (fontFamily == null)
		{
			return Look.Mono(points - 0.5f, bold ? FontStyle.Bold : FontStyle.Regular, GraphicsUnit.Point);
		}
		return new Font(fontFamily, points, FontStyle.Regular, GraphicsUnit.Point);
	}

	private static Font Display(float points)
	{
		if (Narrow == null)
		{
			return Look.Mono(points - 3f, FontStyle.Bold, GraphicsUnit.Point);
		}
		return new Font(Narrow, points, FontStyle.Regular, GraphicsUnit.Point);
	}

	private float DeviceScale()
	{
		return CurrentAutoScaleDimensions.Width / 96f;
	}

	private float Unit()
	{
		return DeviceScale() * zoom;
	}

	private static float ScaleOf(Control control)
	{
		Form form = control.FindForm();
		if (form == null)
		{
			return 1f;
		}
		Mainframe mainframe = form as Mainframe;
		SecuritySheet securitySheet = form as SecuritySheet;
		LayersSheet layersSheet = form as LayersSheet;
		float num = mainframe?.zoom ?? securitySheet?.Zoom ?? layersSheet?.Zoom ?? 1f;
		return form.CurrentAutoScaleDimensions.Width / 96f * num;
	}

	private int Scaled(int value)
	{
		if (!loaded)
		{
			return value;
		}
		return (int)Math.Round((float)value * Unit());
	}

	private void Arrange(bool setupShown)
	{
		expanded = setupShown;
		int num = (setupShown ? 118 : 0);
		int num2 = num + KeyRow;
		tierNote.Top = Scaled(168 + num);
		for (int i = 0; i < rungs.Length; i++)
		{
			rungs[i].Top = Scaled(184 + 24 * i + num);
		}
		freeKey.Top = Scaled(209 + num);
		textEntry.Top = Scaled(262 + num);
		textConnect.Top = textEntry.Top;
		Entry entry = textEntry;
		bool visible = (textConnect.Visible = tier > 0);
		entry.Visible = visible;
		int num3 = ((voiceFilled >= 0f) ? 40 : 0);
		int num4 = (arrangedFold = Fold);
		foreach (KeyValuePair<Control, int> item in lower)
		{
			item.Key.Top = Scaled(item.Value + num2 + ((item.Value >= 443) ? num3 : 0) - ((item.Value > 310) ? num4 : 0));
		}
		installNote.Top = Scaled(445 + num2 - num4);
		installNote.Visible = num3 > 0;
		Label[] array = steps;
		foreach (Label label in array)
		{
			label.Visible = setupShown;
		}
		setup.Visible = setupShown;
		refresh.Top = Scaled(setupShown ? 226 : 134);
		brainNote.MaximumSize = new Size(Scaled(328), 0);
		voiceNote.MaximumSize = new Size(Scaled(328), 0);
		updateNote.MaximumSize = new Size(Scaled(328), 0);
		customNote.MaximumSize = new Size(Scaled(328), 0);
		if (keysNote != null)
		{
			keysNote.MaximumSize = new Size(Scaled(328), 0);
		}
		if (paged)
		{
			pageView = Fit();
		}
		ClientSize = new Size(Scaled(paged ? 760 : 380), Scaled(paged ? pageView : (642 + num2 + num3 - num4)));
		ScrollPage(pageScroll);
		bool flag2 = paged && pageView < 960;
		Rim rim = topRim;
		bool visible2 = (bottomRim.Visible = flag2);
		rim.Visible = visible2;
		if (flag2)
		{
			topRim.Bounds = new Rectangle(Scaled(14), 0, ClientSize.Width - Scaled(28), Scaled(3));
			bottomRim.Bounds = new Rectangle(Scaled(14), ClientSize.Height - Scaled(3), ClientSize.Width - Scaled(28), Scaled(3));
		}
		Invalidate(invalidateChildren: true);
	}

	protected override void OnFormClosing(FormClosingEventArgs e)
	{
		if (!Quitting && e.CloseReason == CloseReason.UserClosing)
		{
			e.Cancel = true;
			WindowState = FormWindowState.Minimized;
		}
		base.OnFormClosing(e);
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			Application.RemoveMessageFilter(this);
			modelMenu.Dispose();
			voiceMenu.Dispose();
			speechModelMenu.Dispose();
			micMenu.Dispose();
			languageMenu.Dispose();
			freeMenu.Dispose();
		}
		base.Dispose(disposing);
	}

	internal void Restore()
	{
		if (!Visible)
		{
			Show();
		}
		if (WindowState == FormWindowState.Minimized)
		{
			WindowState = FormWindowState.Normal;
		}
		Activate();
	}

	internal void Sync()
	{
		syncing = true;
		showHide.Text = (capri.IsHidden ? "Show Capri" : "Hide Capri");
		perch.Text = (capri.Riding ? "Hop off" : "Ride cursor");
		int num = 0;
		for (int i = 0; i < SizeSteps.Length; i++)
		{
			if (Math.Abs(SizeSteps[i] - capri.CharacterSize) < Math.Abs(SizeSteps[num] - capri.CharacterSize))
			{
				num = i;
			}
		}
		sizeSlider.Value = num;
		sizeName.Text = SizeNames[num];
		checkIns.Checked = capri.CheckIns;
		onTop.Checked = capri.OnTop;
		startup.Checked = capri.StartsWithWindows;
		discord.Checked = capri.OnDiscord;
		cursor.Checked = capri.UsesCursor;
		security.Text = "Security: " + Security.PresetName;
		blackwallNote.Text = Security.Short;
		if (guard != null && !guard.IsDisposed)
		{
			guard.Sync();
		}
		watch.Checked = capri.Watching;
		caughtUp.Checked = capri.CaughtUp;
		stayOnModel.Checked = capri.StayOnModel;
		remembering.Checked = capri.Remembers;
		frameSize.Value = Math.Max(0, Array.IndexOf(FrameSizes, capri.MainframeSize));
		ShowFrameSize();
		VoiceTier();
		int num2 = Array.IndexOf(Gaps, capri.GlanceGap);
		gap.Value = ((num2 < 0) ? 5 : num2);
		gapName.Text = GapNames[gap.Value];
		if (!nickname.Box.Focused)
		{
			nickname.Box.Text = capri.Nickname;
		}
		string language = capri.Language;
		foreach (ToolStripMenuItem item in languageMenu.Items)
		{
			item.Checked = (string)item.Tag == language;
			if (item.Checked)
			{
				languages.Text = item.Text;
			}
		}
		languages.Invalidate();
		int num3 = 0;
		for (int j = 0; j < Volumes.Length; j++)
		{
			if (Math.Abs(Volumes[j] - capri.Volume) < Math.Abs(Volumes[num3] - capri.Volume))
			{
				num3 = j;
			}
		}
		volume.Value = num3;
		volumeName.Text = VolumeNames[num3];
		pitchSlider.Value = capri.VoicePitch + 6;
		pitchName.Text = PitchNames[pitchSlider.Value];
		ShowLayers();
		int num4 = 0;
		for (int k = 0; k < Paces.Length; k++)
		{
			if (Math.Abs(Paces[k] - capri.VoiceSpeed) < Math.Abs(Paces[num4] - capri.VoiceSpeed))
			{
				num4 = k;
			}
		}
		paceSlider.Value = num4;
		paceName.Text = PaceNames[num4];
		sounds.Value = Math.Max(0, Math.Min(10, (capri.SoundLevel + 5) / 10));
		soundsName.Text = SoundNames[sounds.Value];
		sleepAfter.Value = Math.Max(0, Array.IndexOf(Sleeps, capri.SleepAfter));
		sleepName.Text = SleepNames[sleepAfter.Value];
		wander.Value = capri.WanderLevel;
		wanderName.Text = WanderNames[wander.Value];
		pops.Value = Math.Max(0, Math.Min(PopNames.Length - 1, capri.PopPace));
		popsName.Text = PopNames[pops.Value];
		dance.Checked = capri.Dancing;
		bubble.Value = capri.BubbleTime;
		bubbleName.Text = BubbleNames[bubble.Value];
		intro.Checked = capri.Intro;
		typing.Checked = capri.TypingSound;
		ShowHdGames();
		miniPlayer.Checked = capri.PlayerOn;
		replies.Value = capri.Wordiness;
		repliesName.Text = Lengths[replies.Value];
		recall.Value = Math.Max(0, Array.IndexOf(Recalls, capri.Recall));
		recallName.Text = Lengths[recall.Value];
		perchFps.Value = Math.Max(0, Array.IndexOf(Companion.PerchRates, capri.PerchFps));
		ShowPerch();
		eyesOn.Checked = capri.EyesOn;
		eyePace.Value = Math.Clamp(capri.EyePace, 0, EyePaceNames.Length - 1);
		eyePaceName.Text = EyePaceNames[eyePace.Value];
		eyeSize.Value = Math.Clamp(capri.EyeSize, 0, EyeSizeNames.Length - 1);
		eyeSizeName.Text = EyeSizeNames[eyeSize.Value];
		eyeMost.Value = Math.Clamp(capri.EyeMost, 0, EyeMostNames.Length - 1);
		eyeMostName.Text = EyeMostNames[eyeMost.Value];
		syncing = false;
	}

	private void ShowPerch()
	{
		perchFpsName.Text = PerchNames[perchFps.Value];
		perchNote.Text = ((Companion.PerchRates[perchFps.Value] > 30) ? "Perch over 30 fps uses more CPU" : "");
	}

	internal void SlidePerch(int steps)
	{
		Press(perchFps, steps);
	}

	internal void SlidePops(int steps)
	{
		Press(pops, steps);
	}

	internal void FlipCaughtUp()
	{
		caughtUp.Checked = !caughtUp.Checked;
	}

	internal void FlipStay()
	{
		stayOnModel.Checked = !stayOnModel.Checked;
	}

	internal void FlipRemembering()
	{
		remembering.Checked = !remembering.Checked;
	}

	internal string Overlap()
	{
		for (int i = 0; i < Controls.Count; i++)
		{
			for (int j = i + 1; j < Controls.Count; j++)
			{
				Control control = Controls[i];
				Control control2 = Controls[j];
				if (control.Visible && control2.Visible && (!(control is Label) || control.Text.Length != 0) && (!(control2 is Label) || control2.Text.Length != 0) && control.Bounds.IntersectsWith(control2.Bounds))
				{
					return control.Text + " / " + control2.Text;
				}
			}
		}
		return null;
	}

	internal string Outside()
	{
		Rectangle pageArea = PageArea;
		foreach (KeyValuePair<Control, int> item in pageLeft)
		{
			Control key = item.Key;
			if (key.Visible && (!(key is Label) || key.Text.Length != 0) && !pageArea.Contains(key.Bounds))
			{
				return (key.Text.Length > 0) ? key.Text : key.AccessibleName;
			}
		}
		return null;
	}

	internal int Px(int value)
	{
		return Scaled(value);
	}

	internal void DrawRims(Bitmap panel)
	{
		Rim[] array = new Rim[2] { topRim, bottomRim };
		foreach (Rim rim in array)
		{
			if (!rim.Visible)
			{
				continue;
			}
			using Bitmap bitmap = new Bitmap(rim.Width, rim.Height);
			using Graphics graphics = Graphics.FromImage(panel);
			rim.DrawToBitmap(bitmap, new Rectangle(Point.Empty, rim.Size));
			graphics.DrawImageUnscaled(bitmap, rim.Location);
		}
	}

	internal void Wheel(int notches)
	{
		Point point = perchFps.PointToScreen(new Point(perchFps.Width / 2, perchFps.Height / 2));
		for (int i = 0; i < Math.Abs(notches); i++)
		{
			Native.SendMessage(perchFps.Handle, 522, new IntPtr(((notches > 0) ? 120 : (-120)) << 16), new IntPtr((point.Y << 16) | (point.X & 0xFFFF)));
		}
	}

	internal string TabShows()
	{
		Control control = more;
		bool flag = false;
		for (int i = 0; i < 160; i++)
		{
			if (!SelectNextControl(control, forward: true, tabStopOnly: true, nested: true, wrap: true))
			{
				break;
			}
			Control control2 = ActiveControl;
			if (control2 == null || control2 == control || control2 == more)
			{
				break;
			}
			Control control3 = control2;
			while (control3.Parent != this)
			{
				control3 = control3.Parent;
			}
			if (!ClientRectangle.Contains(control3.Bounds))
			{
				return "Tab reached " + (control3.AccessibleName ?? control3.Text) + " half off the screen";
			}
			flag = flag || control3 == quietKeys;
			control = control2;
		}
		more.Focus();
		if (!flag)
		{
			return "Tab never reached the quiet key";
		}
		return null;
	}

	internal void SlideVoice(int which, int index)
	{
		(which switch
		{
			2 => glitchSlider, 
			1 => paceSlider, 
			0 => pitchSlider, 
			_ => robotSlider, 
		}).Slide(index);
	}

	internal void SlideGap(int steps)
	{
		Press(gap, steps);
	}

	private static void Press(Slider slider, int steps)
	{
		slider.Focus();
		for (int i = 0; i < Math.Abs(steps); i++)
		{
			KeyEventArgs e = new KeyEventArgs((steps > 0) ? Keys.Right : Keys.Left);
			typeof(Control).GetMethod("OnKeyDown", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(slider, new object[1] { e });
		}
	}

	internal void Searching()
	{
		brainName.Text = "Looking for a brain...";
		brainNote.Text = "";
		models.Visible = false;
		hint.Visible = false;
		Arrange(setupShown: false);
	}

	internal void ShowOllama(string[] installed, string chosen)
	{
		brainName.Text = "Ollama · " + chosen;
		brainNote.Text = "Running on this PC";
		models.Text = chosen;
		models.Visible = true;
		hint.Visible = false;
		lastInstalled = installed;
		modelMenu.Items.Clear();
		foreach (string text in installed)
		{
			string pick = text;
			ToolStripMenuItem toolStripMenuItem = new ToolStripMenuItem(text, null, (object param0, EventArgs param1) =>
			{
				capri.ChooseModel(pick);
				ShowOllama(lastInstalled, pick);
			});
			toolStripMenuItem.Checked = text == chosen;
			modelMenu.Items.Add(toolStripMenuItem);
		}
		Arrange(setupShown: false);
	}

	internal void ShowCloud(Cloud remote, List<string> found)
	{
		brainName.Text = remote.Name + " · " + remote.Model;
		brainNote.Text = "Using your " + remote.Company + " key";
		models.Text = remote.Model;
		models.Visible = true;
		hint.Visible = false;
		modelMenu.Items.Clear();
		List<string> list = found;
		if (list == null)
		{
			List<string> list2 = new List<string>();
			list2.Add(remote.Model);
			list = list2;
		}
		foreach (string item in list)
		{
			string pick = item;
			modelMenu.Items.Add(Choice(Brain.Shown(remote.Kind, item), item == remote.Model, (object param0, EventArgs param1) =>
			{
				capri.ChooseModel(pick);
				ShowCloud(remote, found);
			}));
		}
		Arrange(setupShown: false);
	}

	internal void ShowTiers(int pick, string nvidiaTail, string paidTail)
	{
		tier = Math.Max(0, Math.Min(rungs.Length - 1, pick));
		syncing = true;
		for (int i = 0; i < rungs.Length; i++)
		{
			rungs[i].Checked = i == tier;
		}
		syncing = false;
		string text;
		if (tier == 1)
		{
			text = nvidiaTail;
		}
		else
		{
			text = ((tier == 2) ? paidTail : null);
		}
		textSaved = text != null;
		textEntry.Box.AccessibleName = ((tier == 1) ? "NVIDIA API key" : "OpenAI, Anthropic or DeepSeek API key");
		tierNote.Text = ((tier == 1) ? "Most NVIDIA models can't see your screen." : "Pick one. Only one brain runs at a time.");
		Entry entry = textEntry;
		string cue;
		if (textSaved)
		{
			cue = ((tier == 1) ? "NVIDIA key saved, ends " : "Key saved, ends ") + text;
		}
		else
		{
			cue = ((tier == 1) ? "Paste your free NVIDIA key" : "Paste a ChatGPT or Claude key, or claude-code");
		}
		entry.Cue = cue;
		RelabelText();
		Arrange(expanded);
	}

	private void ConnectText()
	{
		string text = textEntry.Box.Text.Trim();
		if (text.Length > 0)
		{
			textEntry.Box.Text = "";
			capri.ConnectText(text);
		}
		else if (textSaved)
		{
			capri.ForgetText();
		}
		else
		{
			textEntry.Box.Focus();
		}
	}

	private void RelabelText()
	{
		bool flag = textSaved && textEntry.Box.Text.Trim().Length == 0;
		textConnect.Text = (flag ? "Forget" : "Connect");
		textConnect.Accent = (flag ? Hot : Rose);
		textConnect.Invalidate();
	}

	internal void ShowLocal(Pick pick)
	{
		brainName.Text = "Built-in · " + pick.Name;
		brainNote.Text = "Running on this PC";
		models.Visible = false;
		hint.Visible = false;
		Arrange(setupShown: false);
	}

	internal void ShowSetup(Machine machine, Pick pick, bool viaOllama, string problem)
	{
		brainName.Text = ((problem == null) ? "No brain yet" : "Setup stopped");
		brainNote.Text = problem ?? ("Best fit for this PC: " + pick.Name);
		models.Visible = false;
		Step(0, "✓", machine.Describe(), White);
		Step(1, "2", "Download " + pick.Name + " (" + pick.Download + ")" + (viaOllama ? " with Ollama" : ""), Faint);
		Step(2, "3", "Wake her up", Faint);
		filled = 0f;
		setup.Text = ((problem == null) ? "Set up her brain" : "Try again");
		setup.Enabled = true;
		hint.Visible = !viaOllama;
		Arrange(setupShown: true);
	}

	internal void ShowProgress(int step, string text, long done, long total)
	{
		Step(1, (step > 1) ? "✓" : "2", stepText[1], (step == 1) ? Cyan : White);
		Step(2, "3", stepText[2], (step == 2) ? Cyan : Faint);
		brainName.Text = ((step == 1) ? "Setting up her brain" : "Waking her up");
		brainNote.Text = ((total > 0) ? (text + " · " + Engine.Amount(done) + " of " + Engine.Amount(total)) : text);
		ref float reference = ref filled;
		float num;
		if (total > 0)
		{
			num = Math.Min(1f, (float)done / (float)total);
		}
		else
		{
			num = ((step == 2) ? 1f : 0f);
		}
		reference = num;
		setup.Text = ((step == 1) ? "Cancel" : "Starting...");
		setup.Enabled = step == 1;
		hint.Visible = false;
		if (!expanded)
		{
			Arrange(setupShown: true);
		}
		else
		{
			Invalidate(new Rectangle(0, Scaled(204), Width, Scaled(18)));
		}
	}

	internal void ShowProblem(string problem)
	{
		brainNote.Text = problem;
	}

	internal void ShowInstall(string text, long done, long total)
	{
		bool flag = voiceFilled < 0f;
		installNote.Text = ((total > 0) ? (text + " · " + Engine.Amount(done) + " of " + Engine.Amount(total)) : (text + "..."));
		voiceFilled = ((total > 0) ? Math.Min(1f, (float)done / (float)total) : 0f);
		if (flag)
		{
			Arrange(expanded);
		}
		else
		{
			Invalidate(new Rectangle(0, Scaled(463 + (expanded ? 118 : 0) + KeyRow - arrangedFold), Width, Scaled(14)));
		}
	}

	internal void HideInstall()
	{
		if (!(voiceFilled < 0f))
		{
			voiceFilled = -1f;
			installNote.Text = "";
			Arrange(expanded);
		}
	}

	private void VoiceTier()
	{
		off = capri.Silent;
		eleven = (keySaved ? capri.Speaking : wantEleven);
		bool flag = !off && !eleven && capri.ServerPicked;
		bool flag2 = flag && capri.ServerInCloud;
		if (eleven)
		{
			cloudName = "ElevenLabs";
		}
		else if (flag2)
		{
			cloudName = capri.ServerCloudName;
		}
		else if (wantCloud != null)
		{
			cloudName = wantCloud;
		}
		else
		{
			cloudName = capri.CloudChoice;
		}
		voiceRow = ((!off) ? ((eleven || flag2 || wantCloud != null) ? 3 : ((!flag) ? 1 : 2)) : 0);
		for (int i = 0; i < voiceRungs.Length; i++)
		{
			voiceRungs[i].Checked = i == voiceRow;
		}
		bool flag3 = voiceRow == 3;
		bool flag4 = flag3 && cloudName == "ElevenLabs";
		cloudPick.Visible = flag3;
		cloudPick.Text = cloudName;
		foreach (ToolStripMenuItem item in cloudMenu.Items)
		{
			item.Checked = item.Text == cloudName;
		}
		keyEntry.Visible = flag4;
		connect.Visible = flag4;
		cloudVoice.Visible = flag3 && !flag4;
		cloudVoice.Text = (flag2 ? "Voice, key and model..." : ("Set up " + cloudName + "..."));
		voiceEntry.Visible = flag4 && keySaved;
		speechModels.Visible = flag4 && keySaved;
		freeVoices.Visible = voiceRow == 1;
		customVoice.Visible = voiceRow == 2;
		freeSetup.Visible = (voiceRow == 1 || voiceRow == 2) && !freeReady;
		customVoice.Width = Scaled(freeReady ? 328 : 196);
		cloudPick.Invalidate();
		cloudVoice.Invalidate();
		if (Fold != arrangedFold)
		{
			Arrange(expanded);
		}
		ShowEffects();
	}

	private void PickRow(int row)
	{
		switch (row)
		{
		case 0:
			PickSilent();
			break;
		case 1:
			wantEleven = false;
			wantCloud = null;
			capri.UsePrebuilt();
			break;
		case 2:
			PickModel(cloud: false, null);
			break;
		default:
			PickCloud(cloudName);
			break;
		}
	}

	private void PickCloud(string name)
	{
		cloudName = name;
		if (name == "ElevenLabs")
		{
			PickVoice(paid: true);
		}
		else
		{
			PickModel(cloud: true, CloudApi(name));
		}
	}

	private void PickModel(bool cloud, string api)
	{
		if (capri.HasServer(cloud, api))
		{
			wantEleven = false;
			wantCloud = null;
			capri.UseSavedServer(cloud);
		}
		else
		{
			wantCloud = (cloud ? cloudName : null);
			OpenServer(cloud, api);
			VoiceTier();
		}
	}

	private static string CloudApi(string name)
	{
		if (!(name == Clouds[0]))
		{
			if (!(name == Clouds[2]))
			{
				return null;
			}
			return "openai";
		}
		return "fish";
	}

	private void CloudGuide()
	{
		if (cloudName == "ElevenLabs")
		{
			capri.OpenKeyPage();
		}
		else
		{
			OpenServer(cloud: true, CloudApi(cloudName));
		}
	}

	private void ShowEffects()
	{
		bool flag = syncing;
		syncing = true;
		robotSlider.Value = Nearest(capri.VoiceRobot);
		robotName.Text = AmountName(robotSlider.Value);
		glitchSlider.Value = Nearest(capri.VoiceGlitch);
		glitchName.Text = AmountName(glitchSlider.Value);
		syncing = flag;
	}

	private static int Nearest(int amount)
	{
		int num = 0;
		for (int i = 0; i < Amounts.Length; i++)
		{
			if (Math.Abs(Amounts[i] - amount) < Math.Abs(Amounts[num] - amount))
			{
				num = i;
			}
		}
		return num;
	}

	private string AmountName(int step)
	{
		string obj = AmountNames[step];
		string text;
		if (capri.SpeaksHd)
		{
			text = " · HD";
		}
		else
		{
			text = (capri.SpeaksServer ? " · server" : "");
		}
		return obj + text;
	}

	internal void PickVoice(bool paid)
	{
		wantEleven = paid;
		wantCloud = null;
		capri.ChooseVoiceTier(paid);
		if (paid && !keySaved)
		{
			keyEntry.Box.Focus();
		}
	}

	internal void PickSilent()
	{
		wantCloud = null;
		capri.Silence();
	}

	internal void ShowVoice(string tail, string voiceId, string model, VoiceListing listing)
	{
		keySaved = tail != null;
		keyEntry.Cue = (keySaved ? ("Saved, ends " + tail) : "Paste API key");
		Relabel();
		VoiceTier();
		if (!voiceEntry.Box.Focused || voiceEntry.Box.Text.Trim().Length == 0)
		{
			voiceEntry.Box.Text = voiceId;
		}
		voiceMenu.Items.Clear();
		voiceEntry.Chevron = listing != null && listing.Voices.Count > 0;
		speechModelMenu.Items.Clear();
		speechModelMenu.Items.Add(Choice("Default model", string.IsNullOrEmpty(model), (object param0, EventArgs param1) =>
		{
			capri.ChooseVoiceModel(null);
		}));
		speechModels.Text = (string.IsNullOrEmpty(model) ? "Default model" : model);
		if (listing != null)
		{
			foreach (VoiceEntry voice in listing.Voices)
			{
				string id = voice.Id;
				voiceMenu.Items.Add(Choice(voice.Name ?? id, id == voiceId, (object param0, EventArgs param1) =>
				{
					voiceEntry.Box.Text = id;
					capri.ChooseVoice(id);
				}));
			}
			foreach (VoiceModel model2 in listing.Models)
			{
				string id2 = model2.Id;
				string text = model2.Name ?? id2;
				if (id2 == model)
				{
					speechModels.Text = (text.StartsWith("Eleven ", StringComparison.OrdinalIgnoreCase) ? text.Substring(7) : text);
				}
				speechModelMenu.Items.Add(Choice(text, id2 == model, (object param0, EventArgs param1) =>
				{
					capri.ChooseVoiceModel(id2);
				}));
			}
		}
		speechModels.Invalidate();
	}

	internal void ShowFree(bool ready, string chosen, bool busy, bool hdFits, bool yours, bool kept, string served = null)
	{
		bool flag = chosen == "HD";
		ShowHdGames();
		freeReady = ready;
		yoursShown = flag && yours;
		freeVoices.Text = "Free voice: " + (yoursShown ? "Your voice" : (served ?? chosen));
		customVoice.Text = ((served != null) ? ("Voice model: " + served) : "Pick a voice model...");
		freeSetup.Text = (busy ? "Cancel" : "Get free voice");
		freeSetup.Visible = (voiceRow == 1 || voiceRow == 2) && !ready;
		freeVoices.Width = Scaled((ready && !yoursShown) ? 328 : 196);
		customVoice.Width = Scaled(ready ? 328 : 196);
		freeMenu.Items.Clear();
		FreeStyle[] styles = LocalVoice.Styles;
		foreach (FreeStyle freeStyle in styles)
		{
			string name = freeStyle.Name;
			freeMenu.Items.Add(Choice(name, name == chosen, (object param0, EventArgs param1) =>
			{
				capri.ChooseVoiceStyle(name);
			}));
		}
		freeMenu.Items.Add(new ToolStripSeparator());
		if (!yours && !kept)
		{
			ToolStripMenuItem toolStripMenuItem = Choice((!hdFits) ? "HD · needs a 6 GB graphics card" : ("HD · graphics card, " + Engine.Amount(QwenVoice.PackSize)), flag, (object param0, EventArgs param1) =>
			{
				PickYourVoice();
			});
			toolStripMenuItem.Enabled = hdFits;
			freeMenu.Items.Add(toolStripMenuItem);
		}
		if (yours || kept)
		{
			ToolStripMenuItem toolStripMenuItem2 = Choice("Your voice (HD)", yoursShown, (object param0, EventArgs param1) =>
			{
				if (yours)
				{
					capri.ChooseVoiceStyle("HD");
				}
				else
				{
					capri.UseKeptVoice();
				}
			});
			toolStripMenuItem2.Enabled = hdFits;
			freeMenu.Items.Add(toolStripMenuItem2);
		}
		ToolStripMenuItem toolStripMenuItem3 = Choice((yours || kept) ? "Pick a new clip of you..." : "Your voice (HD) · pick a clip of you...", chosen: false, (object param0, EventArgs param1) =>
		{
			PickYourVoice();
		});
		toolStripMenuItem3.Enabled = hdFits;
		freeMenu.Items.Add(toolStripMenuItem3);
		freeVoices.Invalidate();
		customVoice.Invalidate();
		freeSetup.Invalidate();
		ShowEffects();
	}

	private void PickYourVoice()
	{
		using OpenFileDialog openFileDialog = new OpenFileDialog();
		openFileDialog.Title = "Pick 5 to 30 seconds of you talking, or of a voice you have the rights and consent to use";
		openFileDialog.Filter = "Voice clips (*.wav;*.mp3;*.m4a)|*.wav;*.mp3;*.m4a|All files (*.*)|*.*";
		if (openFileDialog.ShowDialog(this) == DialogResult.OK)
		{
			capri.UseYourVoice(openFileDialog.FileName);
		}
	}

	internal bool PickFree(string text)
	{
		foreach (ToolStripItem item in freeMenu.Items)
		{
			if (item is ToolStripMenuItem && item.Enabled && item.Text.StartsWith(text, StringComparison.Ordinal))
			{
				item.PerformClick();
				return true;
			}
		}
		return false;
	}

	private void OpenServer(bool cloud, string api)
	{
		if (sheet != null && !sheet.IsDisposed && !sheet.Visible && sheet.Zoom != zoom)
		{
			sheet.Close();
		}
		if (sheet == null || sheet.IsDisposed)
		{
			sheet = new ServerSheet(this, zoom);
		}
		sheet.Fill(cloud, api);
		if (!sheet.Visible)
		{
			sheet.Show(this);
		}
		sheet.Activate();
	}

	private void OpenSecurity()
	{
		if (guard != null && !guard.IsDisposed && guard.Zoom != zoom)
		{
			guard.Close();
		}
		if (guard == null || guard.IsDisposed)
		{
			guard = new SecuritySheet(this, zoom);
		}
		guard.Sync();
		if (!guard.Visible)
		{
			guard.Show(this);
		}
		guard.Activate();
	}

	private void OpenLayers()
	{
		if (layered != null && !layered.IsDisposed && layered.Zoom != zoom)
		{
			layered.Close();
		}
		if (layered == null || layered.IsDisposed)
		{
			layered = new LayersSheet(this, zoom);
		}
		layered.Sync();
		if (!layered.Visible)
		{
			layered.Show(this);
		}
		layered.Activate();
	}

	private void ShowLayers()
	{
		bool flag = capri.VoiceLayers.On;
		layersKey.AccessibleDescription = (flag ? "On" : "Off");
		if (layersKey.Picked != flag)
		{
			layersKey.Picked = flag;
			layersKey.Invalidate();
		}
		if (layered != null && !layered.IsDisposed)
		{
			layered.Sync();
		}
	}

	internal void ShowLayersWindow()
	{
		OpenLayers();
	}

	internal void FlipLayers()
	{
		layered.Flip();
	}

	internal void SlideLayers(int slider, int index)
	{
		layered.Slide(slider, index);
	}

	internal void LayersDefaults()
	{
		layered.Defaults();
	}

	internal void CloseLayers()
	{
		if (layered != null && !layered.IsDisposed)
		{
			layered.Close();
		}
	}

	internal string VoiceHeadingFits()
	{
		if (styleNote.Bounds.IntersectsWith(layersKey.Bounds) || styleNote.Right > layersKey.Left)
		{
			return "the note runs into the Layers button";
		}
		float num = Unit();
		float[] array = new float[5] { 1f, 1.25f, 1.5f, 1.75f, 2f };
		foreach (float num2 in array)
		{
			using Font font = new Font(styleNote.Font.FontFamily, styleNote.Font.SizeInPoints * num2 / num, styleNote.Font.Style, GraphicsUnit.Point);
			using Font font2 = new Font(layersKey.Font.FontFamily, layersKey.Font.SizeInPoints * num2 / num, layersKey.Font.Style, GraphicsUnit.Point);
			int num3 = TextRenderer.MeasureText(styleNote.Text, font).Width;
			int num4 = (int)((float)styleNote.Width / num * num2);
			if (num3 > num4)
			{
				return "\"" + styleNote.Text + "\" is " + num3 + " px in " + num4 + " at " + num2;
			}
			string[] array2 = new string[1] { layersKey.Text };
			foreach (string text in array2)
			{
				int num5 = TextRenderer.MeasureText(text, font2).Width + (int)Math.Ceiling(12f * num2);
				int num6 = (int)((float)layersKey.Width / num * num2);
				if (num5 > num6)
				{
					return "\"" + text + "\" is " + num5 + " px in " + num6 + " at " + num2;
				}
			}
		}
		return null;
	}

	internal string LayersAt(float zoom, string picture)
	{
		using LayersSheet layersSheet = new LayersSheet(this, zoom);
		layersSheet.Show(this);
		string text = layersSheet.Crowded();
		string text2 = " (" + layersSheet.ClientSize.Width + " x " + layersSheet.ClientSize.Height + " at " + zoom + ")";
		if (picture != null)
		{
			layersSheet.Picture(picture);
		}
		layersSheet.Close();
		return (text == null) ? null : (text + text2);
	}

	internal void ShowSecurity()
	{
		OpenSecurity();
	}

	internal void FlipSecurity(int toggle)
	{
		guard.Flip(toggle);
	}

	internal void SlideSecurity(int preset)
	{
		guard.Slide(preset);
	}

	internal void TapSecurity(int preset)
	{
		guard.Tap(preset);
	}

	internal void CloseSecurity()
	{
		if (guard != null && !guard.IsDisposed)
		{
			guard.Close();
		}
	}

	internal string SecurityAt(float zoom, string picture)
	{
		using SecuritySheet securitySheet = new SecuritySheet(this, zoom);
		securitySheet.Show(this);
		string text = securitySheet.Crowded();
		string text2 = " (" + securitySheet.ClientSize.Width + " x " + securitySheet.ClientSize.Height + " at " + zoom + ")";
		if (picture != null)
		{
			securitySheet.Picture(picture);
		}
		securitySheet.Close();
		return (text == null) ? null : (text + text2);
	}

	internal void PickVoiceRow(int row)
	{
		PickRow(row);
	}

	internal void PickCloudVoice(string name)
	{
		PickCloud(name);
	}

	internal void SetUpCustom()
	{
		OpenServer(cloud: false, null);
	}

	internal void SetUpCloud()
	{
		OpenServer(cloud: true, CloudApi(cloudName));
	}

	internal void TypeServer(string address, string name, string voice, string secret)
	{
		sheet.Type(address, name, voice, secret);
	}

	internal void PickService(string name)
	{
		sheet.Choose(name);
	}

	internal void TakeModel(string path)
	{
		sheet.Take(path);
	}

	internal void DropFishVoices()
	{
		sheet.DropVoices();
	}

	internal void PickFishVoice(string name)
	{
		sheet.PickVoice(name);
	}

	internal void MakeFishVoice(string clip)
	{
		sheet.MakeVoice(clip);
	}

	internal void TakeVoiceClip(string clip)
	{
		sheet.TakeClip(clip);
	}

	internal void PressServer(string button)
	{
		sheet.Press(button);
	}

	internal Bitmap FreeMenuPicture()
	{
		freeMenu.Show(freeVoices, new Point(0, freeVoices.Height));
		try
		{
			Bitmap bitmap = new Bitmap(freeMenu.Width, freeMenu.Height);
			freeMenu.DrawToBitmap(bitmap, new Rectangle(Point.Empty, freeMenu.Size));
			return bitmap;
		}
		finally
		{
			freeMenu.Close();
		}
	}

	internal void ShowShortcut(bool talk, bool mute)
	{
		shortcutReady = talk;
		muteReady = mute;
		KeyLabels();
	}

	internal void ShowKeys(bool type, bool hide, bool quiet)
	{
		typeReady = type;
		hideReady = hide;
		quietReady = quiet;
		KeyLabels();
	}

	internal void Hotkeys(string talk, string mute, string type, string hide, string quiet)
	{
		talkName = talk;
		muteName = mute;
		typeName = type;
		hideName = hide;
		quietName = quiet;
		KeyLabels();
	}

	internal void PickKeys(int slot)
	{
		capturing = slot + 1;
		pendingMods = -1;
		heldMods = 0;
		if (keysNote != null)
		{
			keysNote.Text = "";
		}
		KeyLabels();
	}

	internal void TakeKeys(Keys keys)
	{
		TakeKeys(keys, win: false);
	}

	internal void TakeKeys(Keys keys, bool win)
	{
		if (capturing == 0)
		{
			return;
		}
		Keys keys2 = keys & Keys.KeyCode;
		int mods = Companion.HotMods(keys, win || keys2 == Keys.LWin || keys2 == Keys.RWin);
		if (keys2 == Keys.ControlKey || keys2 == Keys.LControlKey || keys2 == Keys.RControlKey || keys2 == Keys.Menu || keys2 == Keys.LMenu || keys2 == Keys.RMenu || keys2 == Keys.ShiftKey || keys2 == Keys.LShiftKey || keys2 == Keys.RShiftKey || keys2 == Keys.LWin || keys2 == Keys.RWin)
		{
			if (pendingMods < 0)
			{
				heldMods = mods;
				KeyLabels();
			}
		}
		else if (Companion.Bindable(mods, (int)keys2))
		{
			pendingMods = mods;
			pendingVk = (int)keys2;
			KeyLabels();
			string text = Companion.KeyName(pendingMods, pendingVk);
			Mainframe mainframe = this;
			string text2;
			if (Companion.Swallows(pendingMods, pendingVk))
			{
				text2 = text + " won't type in other apps while she runs. Confirm to keep it.";
			}
			else
			{
				text2 = (Companion.ByMouse(pendingVk) ? (text + " still does its own job in other apps too. Confirm to keep it.") : ("Confirm to keep " + text + ", or Cancel."));
			}
			mainframe.KeysNote(text2, problem: false);
		}
	}

	internal void ConfirmKeys()
	{
		if (capturing != 0 && pendingMods >= 0)
		{
			int num = capturing - 1;
			int mods = pendingMods;
			int vk = pendingVk;
			string text = capri.SetHotkey(num, mods, vk);
			if (text != null)
			{
				KeysNote(text, problem: true);
				return;
			}
			capturing = 0;
			pendingMods = -1;
			KeyLabels();
			Mainframe mainframe = this;
			string text2 = num switch
			{
				3 => hideName + " hides or shows her.", 
				2 => typeName + " opens her typing box.", 
				1 => muteName + " mutes your mic.", 
				0 => talkName + " starts voice chat.", 
				_ => quietName + " keeps her quiet, and lets her talk again.", 
			};
			string text3;
			if (Companion.Swallows(mods, vk))
			{
				text3 = " It won't type in other apps while she runs.";
			}
			else
			{
				text3 = (Companion.ByMouse(vk) ? " It still does its own job in other apps too." : "");
			}
			mainframe.KeysNote("Saved. " + text2 + text3, problem: false);
		}
	}

	internal void CancelKeys()
	{
		if (capturing != 0)
		{
			capturing = 0;
			pendingMods = -1;
			if (keysNote != null)
			{
				keysNote.Text = "";
			}
			KeyLabels();
		}
	}

	private static bool WinHeld()
	{
		if ((Native.GetKeyState(91) & 0x8000) == 0)
		{
			return (Native.GetKeyState(92) & 0x8000) != 0;
		}
		return true;
	}

	private void KeysNote(string text, bool problem)
	{
		if (!paged)
		{
			VoiceNote(text, problem);
			return;
		}
		keysNote.Text = text;
		keysNote.ForeColor = (problem ? Hot : Faint);
	}

	protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
	{
		if (capturing == 0)
		{
			return base.ProcessCmdKey(ref msg, keyData);
		}
		TakeKeys(keyData, WinHeld());
		return true;
	}

	public bool PreFilterMessage(ref Message m)
	{
		if (capturing == 0 || (m.Msg != 519 && m.Msg != 521 && m.Msg != 523 && m.Msg != 525))
		{
			return false;
		}
		Control control = Control.FromChildHandle(m.HWnd);
		if (control == null || control.FindForm() != this)
		{
			return false;
		}
		Keys keys;
		if (m.Msg == 519 || m.Msg == 521)
		{
			keys = Keys.MButton;
		}
		else
		{
			keys = ((((m.WParam.ToInt64() >> 16) & 0xFFFF) == 2) ? Keys.XButton2 : Keys.XButton1);
		}
		TakeKeys(keys | Control.ModifierKeys, WinHeld());
		return true;
	}

	private void KeyLabels()
	{
		shortcut.Text = ((capturing == 1) ? "Press new keys, then Confirm" : (talkName + (shortcutReady ? " to talk" : " is taken")));
		muteShortcut.Text = ((capturing == 2) ? "Press new keys, then Confirm" : (muteName + (muteReady ? " mutes mic" : " is taken")));
		if (cancelKeys != null)
		{
			talkKeys.Text = ((capturing == 1) ? PickingText("Talk: ") : ("Talk: " + talkName + (shortcutReady ? "" : " (taken)")));
			muteKeys.Text = ((capturing == 2) ? PickingText("Mute mic: ") : ("Mute mic: " + muteName + (muteReady ? "" : " (taken)")));
			typeKeys.Text = ((capturing == 3) ? PickingText("Type: ") : ("Type: " + typeName + (typeReady ? "" : " (taken)")));
			hideKeys.Text = ((capturing == 4) ? PickingText("Hide/show: ") : ("Hide/show: " + hideName + (hideReady ? "" : " (taken)")));
			quietKeys.Text = ((capturing == 5) ? PickingText("Quiet: ") : ("Quiet: " + quietName + (quietReady ? "" : " (taken)")));
			Key key = confirmKeys;
			bool visible = (cancelKeys.Visible = capturing != 0);
			key.Visible = visible;
			confirmKeys.Enabled = pendingMods >= 0;
			confirmKeys.Invalidate();
		}
	}

	private string PickingText(string job)
	{
		if (pendingMods >= 0)
		{
			return job + Companion.KeyName(pendingMods, pendingVk);
		}
		if (heldMods != 0)
		{
			return job + Companion.ModNames(heldMods) + "...";
		}
		return "Press new keys, then Confirm";
	}

	internal void ShowMic(string name)
	{
		mics.Text = "Mic: " + (name ?? "default");
		mics.Invalidate();
	}

	private void ShowMics()
	{
		string micId = capri.MicId;
		List<Mic> list = Ears.List();
		micMenu.Items.Clear();
		micMenu.Items.Add(Choice("Windows default", string.IsNullOrEmpty(micId), (object param0, EventArgs param1) =>
		{
			capri.ChooseMic(null);
		}));
		if (list.Count > 0)
		{
			micMenu.Items.Add(new ToolStripSeparator());
		}
		foreach (Mic item in list)
		{
			string id = item.Id;
			micMenu.Items.Add(Choice(item.Name, id == micId, (object param0, EventArgs param1) =>
			{
				capri.ChooseMic(id);
			}));
		}
		micMenu.Show(mics, new Point(0, mics.Height));
	}

	internal void VoiceNote(string text, bool problem)
	{
		speechFix = text != null && text.IndexOf("Online speech recognition", StringComparison.Ordinal) >= 0;
		voiceNote.Text = (speechFix ? (text + " Click to open it.") : text);
		voiceNote.ForeColor = (problem ? Hot : Faint);
		voiceNote.Cursor = (speechFix ? Cursors.Hand : Cursors.Default);
	}

	internal void UpdateNote(string text, bool problem)
	{
		updateNote.Text = text;
		updateNote.ForeColor = (problem ? Hot : Faint);
	}

	private void Connect()
	{
		string text = keyEntry.Box.Text.Trim();
		if (text.Length > 0)
		{
			keyEntry.Box.Text = "";
			capri.ConnectVoice(text);
		}
		else if (keySaved)
		{
			capri.ForgetVoice();
		}
		else
		{
			keyEntry.Box.Focus();
		}
	}

	private void Relabel()
	{
		bool flag = keySaved && keyEntry.Box.Text.Trim().Length == 0;
		connect.Text = (flag ? "Forget" : "Connect");
		connect.Accent = (flag ? Hot : Rose);
		connect.Invalidate();
	}

	private static ToolStripMenuItem Choice(string text, bool chosen, EventHandler click)
	{
		ToolStripMenuItem toolStripMenuItem = new ToolStripMenuItem(text, null, click);
		toolStripMenuItem.Checked = chosen;
		return toolStripMenuItem;
	}

	private ContextMenuStrip Dropdown()
	{
		ContextMenuStrip contextMenuStrip = new ContextMenuStrip();
		contextMenuStrip.Renderer = new MenuLook();
		contextMenuStrip.BackColor = Ground;
		contextMenuStrip.ForeColor = White;
		contextMenuStrip.Font = Font;
		contextMenuStrip.ShowImageMargin = false;
		contextMenuStrip.ShowCheckMargin = true;
		return contextMenuStrip;
	}

	private Entry Input(int x, int y, int width, int height = 28)
	{
		Entry entry = new Entry();
		entry.Bounds = new Rectangle(x, y, width, height);
		Controls.Add(entry);
		return entry;
	}

	private void Step(int index, string mark, string text, Color colour)
	{
		stepText[index] = text;
		steps[index].Text = mark + "  " + text;
		steps[index].ForeColor = colour;
	}

	private Label Caption(string text, int x, int y, Font font, Color colour)
	{
		Label label = new Note();
		label.Text = text;
		label.Font = font;
		label.ForeColor = colour;
		label.BackColor = Ground;
		label.AutoSize = true;
		label.Location = new Point(x, y);
		Controls.Add(label);
		return label;
	}

	private Key Make(string text, int x, int y, int width, int height, EventHandler click)
	{
		Key key = new Key();
		key.Text = text;
		key.Font = strong;
		key.Bounds = new Rectangle(x, y, width, height);
		key.Click += click;
		Controls.Add(key);
		return key;
	}

	private static void Drop(Graphics graphics, float left, float middle, float s, Color colour, bool filled)
	{
		PointF[] points = new PointF[3]
		{
			new PointF(left, middle - 3f * s),
			new PointF(left + 9f * s, middle - 3f * s),
			new PointF(left + 4.5f * s, middle + 3.5f * s)
		};
		if (filled)
		{
			using Brush brush = new SolidBrush(colour);
			graphics.FillPolygon(brush, points);
		}
		using Pen pen = new Pen(colour, 1.3f * s);
		pen.LineJoin = LineJoin.Miter;
		graphics.DrawPolygon(pen, points);
	}
}
