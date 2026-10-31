using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CapriDesktop;

internal sealed class Tear : Overlay
{
	internal const double Length = 0.75;

	internal const double WideLength = 1.2;

	internal const double BurstLength = 1.5;

	internal const double Linger = 0.7;

	private static readonly Color Crimson = Color.FromArgb(176, 6, 32);

	private static readonly Color Ink = Color.FromArgb(8, 6, 10);

	private static readonly Color Bruise = Color.FromArgb(34, 8, 48);

	private static readonly Color Violet = Color.FromArgb(110, 30, 170);

	private static readonly Color Hot = Color.FromArgb(255, 120, 140);

	private readonly Timer ticks = new Timer();

	private readonly Stopwatch watch = new Stopwatch();

	private readonly Random random = new Random();

	private Rectangle area;

	private Rectangle heart;

	private int seed;

	private bool wide;

	private Point? core;

	private IntPtr under;

	protected override CreateParams CreateParams
	{
		get
		{
			CreateParams createParams = base.CreateParams;
			createParams.ExStyle |= 32;
			return createParams;
		}
	}

	internal Tear()
		: base("Capri glitch")
	{
		ticks.Interval = 33;
		Timer timer = ticks;
		EventHandler value = (object param0, EventArgs param1) =>
		{
			Step();
		};
		timer.Tick += value;
	}

	internal void Burst(Rectangle body, Rectangle screen, bool everywhere, bool spawn = false, IntPtr behind = default(IntPtr))
	{
		wide = everywhere || spawn;
		under = (wide ? behind : IntPtr.Zero);
		area = Rectangle.Intersect(wide ? Rectangle.Inflate(body, body.Height, body.Height * 2 / 3) : Rectangle.Inflate(body, body.Width, body.Height / 8), screen);
		if (area.Width >= 8 && area.Height >= 8)
		{
			heart = new Rectangle(body.Left - area.Left, body.Top - area.Top, body.Width, body.Height);
			core = (spawn ? new Point?(new Point(heart.Left + heart.Width / 2, heart.Top + heart.Height / 2)) : ((Point?)null));
			seed = random.Next();
			ticks.Interval = 33;
			watch.Reset();
			watch.Start();
			Step();
			ticks.Start();
		}
	}

	private void Step()
	{
		double totalSeconds = watch.Elapsed.TotalSeconds;
		double num;
		if (core.HasValue)
		{
			num = 1.5;
		}
		else
		{
			num = (wide ? 1.2 : 0.75);
		}
		double num2 = (core.HasValue ? 0.7 : 0.0);
		if (totalSeconds >= num + num2)
		{
			ticks.Stop();
			Hide();
			return;
		}
		Present(Frame(area.Size, Held(totalSeconds, num, num2), seed, wide, core, heart, (int)(totalSeconds * 30.0)), area.Location);
		if (under != IntPtr.Zero)
		{
			Native.SetWindowPos(Handle, under, 0, 0, 0, 0, 19);
			under = IntPtr.Zero;
		}
	}

	internal static double Held(double t, double length, double linger)
	{
		double num = 0.26 * length;
		double num2 = 0.3 * length + linger;
		if (!(t < num))
		{
			if (!(t < num + num2))
			{
				return (t - linger) / length;
			}
			return 0.26 + 0.3 * (t - num) / num2;
		}
		return t / length;
	}

	internal static Bitmap Frame(Size size, double progress, int seed, bool wide = false, Point? core = null, Rectangle? body = null, int flicker = 0)
	{
		Bitmap bitmap = new Bitmap(size.Width, size.Height, PixelFormat.Format32bppPArgb);
		Random random = new Random(seed + flicker);
		double num = Math.Max(0.0, Math.Min(1.0, (1.0 - progress) / 0.65));
		num = num * num * (3.0 - 2.0 * num);
		if (num <= 0.0)
		{
			return bitmap;
		}
		Rectangle rect = body ?? new Rectangle(size.Width * 2 / 5, size.Height / 4, size.Width / 5, size.Height / 2);
		if (wide)
		{
			Tendrils(bitmap, new Rectangle(Point.Empty, size), core.HasValue ? ((PointF)core.Value) : new PointF((float)rect.Left + (float)rect.Width / 2f, (float)rect.Top + (float)rect.Height / 2f), new SizeF((float)rect.Width * 0.1f, (float)rect.Height * 0.06f), Math.Max(0.6f, (float)rect.Height / 260f), progress, seed, flicker);
		}
		using Graphics graphics = Graphics.FromImage(bitmap);
		double num2 = (core.HasValue ? Math.Max(0.0, Math.Min(1.0, (progress - 0.25) * 3.0)) : 1.0);
		Rectangle rectangle = (wide ? Rectangle.Intersect(Rectangle.Inflate(rect, rect.Width / 2, rect.Height / 12), new Rectangle(Point.Empty, size)) : new Rectangle(Point.Empty, size));
		if (rectangle.Width < 4 || rectangle.Height < 4)
		{
			return bitmap;
		}
		int num3 = (int)Math.Round((double)(wide ? (2 + random.Next(4)) : (6 + random.Next(7))) * (0.45 + 0.55 * num) * num2);
		int num4 = Math.Max(2, wide ? (rect.Height / 50) : (size.Height / 26));
		for (int i = 0; i < num3; i++)
		{
			int num5 = rectangle.Top + random.Next(rectangle.Height);
			int num6 = 1 + (int)((double)num4 * (wide ? 0.6 : 6.0) * Math.Pow(random.NextDouble(), 2.2));
			int num7 = (wide ? (rectangle.Width / 6 + random.Next(Math.Max(1, rectangle.Width / 2))) : (rectangle.Width / 4 + random.Next(Math.Max(1, rectangle.Width * 3 / 4))));
			int num8 = rectangle.Left + random.Next(Math.Max(1, rectangle.Width - num7));
			int num9 = random.Next(10);
			Color baseColor;
			if (num9 < 4)
			{
				baseColor = Ink;
			}
			else
			{
				baseColor = ((num9 < 8) ? Look.Red : Crimson);
			}
			int num10 = (int)((double)((num9 < 4) ? (180 + random.Next(60)) : (150 + random.Next(100))) * num);
			if (num10 <= 0)
			{
				continue;
			}
			using (Brush brush = new SolidBrush(Color.FromArgb(num10, baseColor)))
			{
				graphics.FillRectangle(brush, num8, num5, num7, num6);
			}
			if (num9 < 4 && num6 > 3)
			{
				using Brush brush2 = new SolidBrush(Color.FromArgb(num10 * 2 / 3, Look.Red));
				graphics.FillRectangle(brush2, num8, num5, num7, 1);
				graphics.FillRectangle(brush2, num8, num5 + num6 - 1, num7, 1);
			}
			if (random.Next(3) == 0)
			{
				using Brush brush3 = new SolidBrush(Color.FromArgb(num10 / 3, Look.Red));
				graphics.FillRectangle(brush3, num8 + random.Next(-rectangle.Width / 16, rectangle.Width / 16 + 1), num5 + num6 + 1, num7 / 2, Math.Max(1, num6 / 2));
			}
		}
		return bitmap;
	}

	internal static void Tendrils(Bitmap image, Rectangle room, PointF middle, SizeF spread, float unit, double progress, int seed, int flicker)
	{
		room = Rectangle.Intersect(room, new Rectangle(Point.Empty, image.Size));
		if (room.Width < 8 || room.Height < 8)
		{
			return;
		}
		double num = Math.Max(0.0, Math.Min(1.0, (0.97 - progress) / 0.17));
		num = num * num * (3.0 - 2.0 * num);
		if (num <= 0.01)
		{
			return;
		}
		Random random = new Random(seed);
		Random random2 = new Random(seed ^ (flicker * 7919 + 1));
		double num2 = Math.Min(1.0, progress / 0.26);
		num2 = 1.0 - (1.0 - num2) * (1.0 - num2) * (1.0 - num2);
		double clock = (double)flicker / 30.0;
		List<PointF[]> list = new List<PointF[]>();
		List<float[]> list2 = new List<float[]>();
		int num3 = 4 + random.Next(3);
		for (int i = 0; i < num3; i++)
		{
			double num4 = ((double)i + 0.15 + random.NextDouble() * 0.7) / (double)num3 * 2.0 * Math.PI;
			double num5 = Math.Cos(num4);
			double num6 = Math.Sin(num4);
			float num7 = middle.X + (float)(num5 * (double)spread.Width);
			float num8 = middle.Y + (float)(num6 * (double)spread.Height);
			double num9;
			if (num5 > 0.01)
			{
				num9 = (double)((float)(room.Right - 12) - num7) / num5;
			}
			else
			{
				num9 = ((num5 < -0.01) ? ((double)((float)(room.Left + 12) - num7) / num5) : 1000000000.0);
			}
			double val = num9;
			double val2;
			if (num6 > 0.01)
			{
				val2 = (double)((float)(room.Bottom - 12) - num8) / num6;
			}
			else
			{
				val2 = ((num6 < -0.01) ? ((double)((float)(room.Top + 12) - num8) / num6) : 1000000000.0);
			}
			double val3 = Math.Min(val, val2);
			double length = Math.Max(0.0, val3) * (0.6 + 0.32 * random.NextDouble());
			double thick = (10.0 + random.NextDouble() * 6.0) * (double)unit;
			double num10 = Math.Max(0.0, Math.Min(1.0, (progress - 0.56 - 0.08 * random.NextDouble()) / 0.3));
			num10 = Math.Round(num10 * num10 * (3.0 - 2.0 * num10) * 6.0) / 6.0;
			Ribbon(list, list2, random, room, num7, num8, num4, length, thick, num2 * (1.0 - num10), clock, unit, 2);
		}
		if (list.Count == 0)
		{
			return;
		}
		float[] array = new float[list.Count];
		float[] array2 = new float[list.Count];
		for (int j = 0; j < list.Count; j++)
		{
			if (random2.NextDouble() < 0.08)
			{
				array[j] = (float)((random2.Next(2) * 2 - 1) * (3 + random2.Next(7))) * unit;
				array2[j] = (float)((random2.NextDouble() * 2.0 - 1.0) * 2.0 * (double)unit);
			}
		}
		using (Graphics graphics = Graphics.FromImage(image))
		{
			using (Bitmap bitmap = new Bitmap(Math.Max(1, room.Width / 4), Math.Max(1, room.Height / 4), PixelFormat.Format32bppPArgb))
			{
				using (Graphics graphics2 = Graphics.FromImage(bitmap))
				{
					using Brush brush = new SolidBrush(Color.FromArgb((int)(70.0 * num), 210, 12, 96));
					using Brush brush2 = new SolidBrush(Color.FromArgb((int)(150.0 * num), 230, 10, 80));
					graphics2.SmoothingMode = SmoothingMode.AntiAlias;
					for (int k = 0; k < list.Count; k++)
					{
						graphics2.FillPolygon(brush, Outline(list[k], list2[k], 0.25f, 3.4f, 18f * unit, array[k] - (float)room.X, array2[k] - (float)room.Y), FillMode.Winding);
					}
					for (int l = 0; l < list.Count; l++)
					{
						graphics2.FillPolygon(brush2, Outline(list[l], list2[l], 0.25f, 1.8f, 6f * unit, array[l] - (float)room.X, array2[l] - (float)room.Y), FillMode.Winding);
					}
				}
				using (Bitmap bitmap2 = new Bitmap(Math.Max(1, room.Width / 2), Math.Max(1, room.Height / 2), PixelFormat.Format32bppPArgb))
				{
					using (Graphics graphics3 = Graphics.FromImage(bitmap2))
					{
						graphics3.InterpolationMode = InterpolationMode.HighQualityBilinear;
						graphics3.PixelOffsetMode = PixelOffsetMode.Half;
						graphics3.DrawImage(bitmap, new Rectangle(0, 0, bitmap2.Width, bitmap2.Height), 0, 0, bitmap.Width, bitmap.Height, GraphicsUnit.Pixel);
					}
					graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
					graphics.PixelOffsetMode = PixelOffsetMode.Half;
					graphics.DrawImage(bitmap2, room, 0, 0, bitmap2.Width, bitmap2.Height, GraphicsUnit.Pixel);
				}
				graphics.PixelOffsetMode = PixelOffsetMode.Default;
			}
			graphics.SmoothingMode = SmoothingMode.AntiAlias;
			bool flag = progress < 0.1 || random2.Next(4) == 0;
			float num11 = (float)(2 + random2.Next(3)) * unit;
			using (Brush brush3 = new SolidBrush(Color.FromArgb((int)(170.0 * num), Shatter.Mint)))
			{
				using Brush brush4 = new SolidBrush(Color.FromArgb((int)(235.0 * num), Shatter.Violet));
				using Brush brush5 = new SolidBrush(Color.FromArgb((int)(255.0 * num), 236, 6, 68));
				for (int m = 0; m < list.Count; m++)
				{
					if (flag)
					{
						graphics.FillPolygon(brush3, Outline(list[m], list2[m], 1f, 1f, 0f, array[m] + num11, array2[m] - num11 * 0.4f), FillMode.Winding);
					}
					graphics.FillPolygon(brush4, Outline(list[m], list2[m], 1f, 1f, 0f, array[m] - 3f * unit, array2[m] + 3f * unit), FillMode.Winding);
					graphics.FillPolygon(brush5, Outline(list[m], list2[m], 1f, 1f, 0f, array[m], array2[m]), FillMode.Winding);
				}
			}
			graphics.SmoothingMode = SmoothingMode.None;
			using (Brush brush6 = new SolidBrush(Color.FromArgb((int)(210.0 * num), 255, 222, 232)))
			{
				for (int n = 0; n < list.Count; n++)
				{
					for (int num12 = 1; num12 < list[n].Length; num12++)
					{
						PointF pointF = list[n][num12 - 1];
						PointF pointF2 = list[n][num12];
						float num13 = list2[n][num12 - 1];
						float num14 = pointF2.X - pointF.X;
						float num15 = pointF2.Y - pointF.Y;
						float num16 = (float)Math.Sqrt(num14 * num14 + num15 * num15);
						if (num13 < 5f * unit || num16 < 1f)
						{
							continue;
						}
						Random random3 = new Random(seed ^ ((int)(list[n][0].X * 7f + list[n][0].Y * 13f) * 977 + num12 * 131));
						int num17 = 0;
						for (int num18 = (int)(num16 * num13 / (180f * unit * unit)) + 1; num17 < num18; num17++)
						{
							double num19 = random3.NextDouble();
							double num20 = (random3.NextDouble() * 2.0 - 1.0) * 0.3 * (double)num13;
							float num21 = (float)(1 + random3.Next(2)) * Math.Max(1f, unit * 0.8f);
							if (random2.Next(4) != 0)
							{
								graphics.FillRectangle(brush6, pointF.X + num14 * (float)num19 - num15 / num16 * (float)num20 + array[n], pointF.Y + num15 * (float)num19 + num14 / num16 * (float)num20 + array2[n], num21, num21);
							}
						}
					}
				}
			}
			Color[] array3 = new Color[6]
			{
				Look.Red,
				Hot,
				Color.White,
				Ink,
				Shatter.Violet,
				Shatter.Mint
			};
			for (int num22 = 0; num22 < list.Count; num22++)
			{
				PointF[] array4 = list[num22];
				if (array4.Length < 3)
				{
					continue;
				}
				PointF pointF3 = array4[array4.Length - 1];
				int num23 = ((progress > 0.55) ? (2 + random2.Next(4)) : random2.Next(3));
				float num24 = (float)(6.0 + 30.0 * Math.Max(0.0, progress - 0.5)) * unit;
				for (int num25 = 0; num25 < num23; num25++)
				{
					float num26 = (float)(2 + random2.Next(3)) * unit;
					using Brush brush7 = new SolidBrush(Color.FromArgb((int)(255.0 * num), array3[random2.Next(array3.Length)]));
					graphics.FillRectangle(brush7, pointF3.X + array[num22] + (float)((random2.NextDouble() * 2.0 - 1.0) * (double)num24), pointF3.Y + array2[num22] + (float)((random2.NextDouble() * 2.0 - 1.0) * (double)num24), num26 * (float)(1 + random2.Next(4)), num26);
				}
			}
			graphics.CompositingMode = CompositingMode.SourceCopy;
			using Brush brush8 = new SolidBrush(Color.Transparent);
			int num27 = 0;
			for (int num28 = random2.Next(5); num27 < num28; num27++)
			{
				PointF[] array5 = list[random2.Next(list.Count)];
				PointF pointF4 = array5[random2.Next(array5.Length)];
				graphics.FillRectangle(brush8, pointF4.X - (float)(4 + random2.Next(10)) * unit, pointF4.Y - unit, (float)(8 + random2.Next(20)) * unit, (float)(1 + random2.Next(3)) * unit);
			}
		}
		int num29;
		if (progress < 0.1)
		{
			num29 = 3 + random2.Next(3);
		}
		else
		{
			num29 = ((progress > 0.55) ? (2 + random2.Next(5)) : random2.Next(4));
		}
		if (num29 == 0)
		{
			return;
		}
		BitmapData bitmapData = image.LockBits(room, ImageLockMode.ReadWrite, PixelFormat.Format32bppPArgb);
		try
		{
			int[] array6 = new int[room.Width];
			int[] array7 = new int[room.Width];
			for (int num30 = 0; num30 < num29; num30++)
			{
				int num31 = random2.Next(room.Height);
				int num32 = Math.Max(1, (int)((float)(2 + random2.Next(14)) * unit));
				int num33 = (int)((float)((random2.Next(2) * 2 - 1) * (5 + random2.Next(18))) * unit);
				for (int num34 = num31; num34 < Math.Min(room.Height, num31 + num32); num34++)
				{
					IntPtr intPtr = bitmapData.Scan0 + num34 * bitmapData.Stride;
					Marshal.Copy(intPtr, array6, 0, room.Width);
					for (int num35 = 0; num35 < room.Width; num35++)
					{
						int num36 = num35 - num33;
						array7[num35] = ((num36 >= 0 && num36 < room.Width) ? array6[num36] : 0);
					}
					Marshal.Copy(array7, 0, intPtr, room.Width);
				}
			}
		}
		finally
		{
			image.UnlockBits(bitmapData);
		}
	}

	private static void Ribbon(List<PointF[]> spines, List<float[]> widths, Random shape, Rectangle room, float x, float y, double aim, double length, double thick, double shown, double clock, float unit, int depth)
	{
		int num = ((depth == 2) ? (4 + shape.Next(4)) : (2 + shape.Next(2)));
		double[] array = new double[num];
		double num2 = 0.0;
		bool flag = shape.NextDouble() < 0.35;
		for (int i = 0; i < num; i++)
		{
			array[i] = (0.35 + 1.3 * shape.NextDouble()) * ((flag && i == num - 1) ? 0.55 : 1.0);
			num2 += array[i];
		}
		PointF[] array2 = new PointF[num + 1];
		float[] array3 = new float[num + 1];
		double[] array4 = new double[num + 1];
		ref PointF reference = ref array2[0];
		reference = new PointF(x, y);
		array3[0] = (float)(thick * 0.9);
		array4[0] = aim;
		int num3 = shape.Next(2) * 2 - 1;
		double num4 = shape.NextDouble() * 6.28;
		double num5 = aim;
		float num6 = x;
		float num7 = y;
		for (int j = 1; j <= num; j++)
		{
			aim += (shape.NextDouble() - 0.5) * 0.32;
			if (shape.NextDouble() < 0.7)
			{
				num3 = -num3;
			}
			double num8 = ((flag && j == num) ? (1.0 + shape.NextDouble() * 0.6) : (0.22 + shape.NextDouble() * 0.48));
			double num9 = aim + (double)num3 * num8;
			double num10 = length * array[j - 1] / num2;
			num6 += (float)(Math.Cos(num9) * num10);
			num7 += (float)(Math.Sin(num9) * num10);
			ref PointF reference2 = ref array2[j];
			reference2 = new PointF(num6, num7);
			array4[j] = num9;
			double num11 = 0.7 + 0.6 * shape.NextDouble();
			array3[j] = ((j == num) ? 0f : ((float)(thick * Math.Pow(1.0 - (double)j / (double)num, 0.45) * num11)));
		}
		float num12 = 1f;
		float num13 = 6f;
		for (int k = 1; k <= num; k++)
		{
			float num14 = array2[k].X - x;
			float num15 = array2[k].Y - y;
			if (array2[k].X < (float)room.Left + num13 && num14 < 0f)
			{
				num12 = Math.Min(num12, (x - (float)room.Left - num13) / (0f - num14));
			}
			if (array2[k].X > (float)room.Right - num13 && num14 > 0f)
			{
				num12 = Math.Min(num12, ((float)room.Right - num13 - x) / num14);
			}
			if (array2[k].Y < (float)room.Top + num13 && num15 < 0f)
			{
				num12 = Math.Min(num12, (y - (float)room.Top - num13) / (0f - num15));
			}
			if (array2[k].Y > (float)room.Bottom - num13 && num15 > 0f)
			{
				num12 = Math.Min(num12, ((float)room.Bottom - num13 - y) / num15);
			}
		}
		num12 = Math.Max(0.2f, num12);
		for (int l = 1; l <= num; l++)
		{
			ref PointF reference3 = ref array2[l];
			reference3 = new PointF(x + (array2[l].X - x) * num12, y + (array2[l].Y - y) * num12);
		}
		int num16 = num * 3;
		PointF[] array5 = new PointF[num16 + 1];
		float[] array6 = new float[num16 + 1];
		double[] array7 = new double[num16 + 1];
		ref PointF reference4 = ref array5[0];
		reference4 = array2[0];
		array6[0] = array3[0];
		for (int m = 1; m <= num; m++)
		{
			float num17 = array2[m].X - array2[m - 1].X;
			float num18 = array2[m].Y - array2[m - 1].Y;
			double num19 = (shape.NextDouble() - 0.5) * 0.22;
			double num20 = 1.0 + shape.NextDouble() * 0.3;
			double num21 = ((m < num && shape.NextDouble() < 0.4) ? (0.35 + shape.NextDouble() * 0.25) : 1.0);
			for (int n = 1; n <= 3; n++)
			{
				int num22 = (m - 1) * 3 + n;
				double num23 = (double)n / 3.0;
				double num24 = num19 * 4.0 * num23 * (1.0 - num23);
				ref PointF reference5 = ref array5[num22];
				reference5 = new PointF(array2[m - 1].X + num17 * (float)num23 - num18 * (float)num24, array2[m - 1].Y + num18 * (float)num23 + num17 * (float)num24);
				array6[num22] = (float)(((double)array3[m - 1] + (double)(array3[m] - array3[m - 1]) * num23) * ((n == 3) ? num21 : num20));
				array7[num22] = array7[num22 - 1] + Math.Sqrt((array5[num22].X - array5[num22 - 1].X) * (array5[num22].X - array5[num22 - 1].X) + (array5[num22].Y - array5[num22 - 1].Y) * (array5[num22].Y - array5[num22 - 1].Y));
			}
		}
		int num25 = depth switch
		{
			1 => shape.Next(2), 
			2 => 1 + shape.Next(2), 
			_ => 0, 
		};
		for (int num26 = 0; num26 < num25; num26++)
		{
			int num27 = 1 + shape.Next(Math.Max(1, num - 2));
			int num28 = num27 * 3;
			double num29 = (double)(shape.Next(2) * 2 - 1) * (0.55 + shape.NextDouble() * 0.5);
			double num30 = Math.Max(1.0, array7[num16] - array7[num28]);
			double length2 = num30 * (0.4 + shape.NextDouble() * 0.25);
			Ribbon(spines, widths, shape, room, array5[num28].X, array5[num28].Y, array4[num27] + num29, length2, (double)array3[num27] * 0.6, Math.Max(0.0, Math.Min(1.0, (shown * array7[num16] - array7[num28]) / num30)), clock, unit, depth - 1);
		}
		double num31 = shown * array7[num16];
		if (num31 < 2.0)
		{
			return;
		}
		double num32 = Math.Max(4f * unit, array7[num16] * 0.14);
		float num33 = (float)(0.0 - Math.Sin(num5));
		float num34 = (float)Math.Cos(num5);
		List<PointF> list = new List<PointF>();
		List<float> list2 = new List<float>();
		for (int num35 = 0; num35 <= num16; num35++)
		{
			PointF pointF = array5[num35];
			float item = (float)((double)array6[num35] * Math.Min(1.0, (num31 - array7[num35]) / num32));
			bool flag2 = array7[num35] >= num31;
			if (flag2)
			{
				double num36 = (num31 - array7[num35 - 1]) / Math.Max(0.001, array7[num35] - array7[num35 - 1]);
				pointF = new PointF(array5[num35 - 1].X + (float)((double)(array5[num35].X - array5[num35 - 1].X) * num36), array5[num35 - 1].Y + (float)((double)(array5[num35].Y - array5[num35 - 1].Y) * num36));
				item = 0f;
			}
			float num37 = (float)(Math.Sin(clock * 3.6 + (double)num35 * 0.37 + num4) * 1.8 * (double)unit * array7[num35] / Math.Max(1.0, array7[num16]));
			list.Add(new PointF(pointF.X + num33 * num37, pointF.Y + num34 * num37));
			list2.Add(item);
			if (flag2)
			{
				break;
			}
		}
		spines.Add(list.ToArray());
		widths.Add(list2.ToArray());
	}

	private static PointF[] Outline(PointF[] spine, float[] width, float scale, float grow, float extra, float shiftX, float shiftY)
	{
		int num = spine.Length;
		PointF[] array = new PointF[num * 2];
		for (int i = 0; i < num; i++)
		{
			PointF pointF = spine[Math.Max(0, i - 1)];
			PointF pointF2 = spine[i];
			PointF pointF3 = spine[Math.Min(num - 1, i + 1)];
			double num2 = pointF2.X - pointF.X;
			double num3 = pointF2.Y - pointF.Y;
			double num4 = pointF3.X - pointF2.X;
			double num5 = pointF3.Y - pointF2.Y;
			double num6 = Math.Sqrt(num2 * num2 + num3 * num3);
			double num7 = Math.Sqrt(num4 * num4 + num5 * num5);
			if (num6 < 0.001)
			{
				num2 = num4;
				num3 = num5;
				num6 = num7;
			}
			if (num7 < 0.001)
			{
				num4 = num2;
				num5 = num3;
				num7 = num6;
			}
			if (num6 < 0.001)
			{
				num2 = (num4 = 1.0);
				num3 = (num5 = 0.0);
				num6 = (num7 = 1.0);
			}
			double num8 = (0.0 - num3) / num6;
			double num9 = num2 / num6;
			double num10 = (0.0 - num5) / num7;
			double num11 = num4 / num7;
			double num12 = num8 + num10;
			double num13 = num9 + num11;
			double num14 = Math.Sqrt(num12 * num12 + num13 * num13);
			if (num14 < 0.001)
			{
				num12 = num8;
				num13 = num9;
				num14 = 1.0;
			}
			num12 /= num14;
			num13 /= num14;
			double num15 = (double)((width[i] * grow + ((width[i] > 0f) ? extra : 0f)) / 2f) / Math.Max(0.4, num12 * num8 + num13 * num9);
			ref PointF reference = ref array[i];
			reference = new PointF((float)(((double)pointF2.X + num12 * num15 + (double)shiftX) * (double)scale), (float)(((double)pointF2.Y + num13 * num15 + (double)shiftY) * (double)scale));
			ref PointF reference2 = ref array[num * 2 - 1 - i];
			reference2 = new PointF((float)(((double)pointF2.X - num12 * num15 + (double)shiftX) * (double)scale), (float)(((double)pointF2.Y - num13 * num15 + (double)shiftY) * (double)scale));
		}
		return array;
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
