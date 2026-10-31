using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CapriDesktop;

internal static class Look
{
	private struct IconInfo
	{
		internal bool Icon;

		internal int X;

		internal int Y;

		internal IntPtr Mask;

		internal IntPtr Colour;
	}

	private struct ScriptAnalysis
	{
		internal ushort Bits;

		internal ushort State;
	}

	private struct ScriptItem
	{
		internal int Start;

		internal ScriptAnalysis Analysis;
	}

	private const string Extras = "ıŁłŒœŠšŸŽžƒ–—‘’‚“”„†‡•…‰‹›€™";

	private const string Closing = "，。、！？：；）」』】〕〉》｝’”…,.!?:;)";

	internal static readonly Color Panel = Color.FromArgb(9, 11, 15);

	internal static readonly Color Cyan = Color.FromArgb(58, 214, 235);

	internal static readonly Color Red = Color.FromArgb(232, 36, 64);

	internal static readonly Color Yellow = Color.FromArgb(244, 214, 70);

	internal static readonly Color Neon = Color.FromArgb(56, 176, 255);

	internal static readonly Color Ink = Color.FromArgb(232, 236, 240);

	internal static readonly Color Dim = Color.FromArgb(122, 128, 138);

	internal static readonly Color Pink = Color.FromArgb(255, 46, 136);

	private static readonly FontFamily embedded = Embedded("Capri.Font.Mono.ttf");

	private static Cursor pointer;

	private static readonly Dictionary<string, double> measured = new Dictionary<string, double>();

	private static Bitmap rulerPage;

	private static Graphics ruler;

	private static Font rulerFont;

	private static Font rulerBurmese;

	private static float rulerColumn;

	internal static readonly StringFormat Leftward = Mirrored();

	internal static readonly StringFormat Spaced = Trailing();

	internal static Cursor Pointer
	{
		get
		{
			if (pointer != null)
			{
				return pointer;
			}
			float num;
			using (Graphics graphics = Graphics.FromHwnd(IntPtr.Zero))
			{
				num = Math.Max(1f, graphics.DpiX / 96f);
			}
			int size = (int)Math.Round(32f * num);
			using (Bitmap bitmap = PointerArt(size))
			{
				IntPtr hicon = bitmap.GetHicon();
				IconInfo info = default;
				GetIconInfo(hicon, ref info);
				info.Icon = false;
				info.X = (int)Math.Round(2f * num);
				info.Y = (int)Math.Round(2f * num);
				IntPtr intPtr = CreateIconIndirect(ref info);
				DeleteObject(info.Mask);
				DeleteObject(info.Colour);
				DestroyIcon(hicon);
				pointer = ((intPtr == IntPtr.Zero) ? Cursors.Hand : new Cursor(intPtr));
			}
			return pointer;
		}
	}

	[DllImport("user32.dll")]
	private static extern bool GetIconInfo(IntPtr icon, ref IconInfo info);

	[DllImport("user32.dll")]
	private static extern IntPtr CreateIconIndirect(ref IconInfo info);

	[DllImport("user32.dll")]
	private static extern bool DestroyIcon(IntPtr icon);

	[DllImport("gdi32.dll")]
	private static extern bool DeleteObject(IntPtr value);

	internal static Bitmap PointerArt(int size)
	{
		float num = (float)size / 32f;
		PointF[] points = new PointF[7]
		{
			new PointF(2f, 2f),
			new PointF(2f, 24f),
			new PointF(8f, 18.5f),
			new PointF(12f, 27f),
			new PointF(16f, 25.2f),
			new PointF(12f, 16.8f),
			new PointF(19.5f, 16.8f)
		};
		Bitmap bitmap = new Bitmap(size, size, PixelFormat.Format32bppArgb);
		using Bitmap image = new Bitmap(size, size, PixelFormat.Format32bppArgb);
		using (Graphics graphics = Graphics.FromImage(image))
		{
			graphics.SmoothingMode = SmoothingMode.AntiAlias;
			using (Brush brush = new SolidBrush(Color.FromArgb(200, Cyan)))
			{
				graphics.FillPolygon(brush, Shift(points, 1.6f, 0.4f, num));
			}
			using (Brush brush2 = new SolidBrush(Color.FromArgb(170, Red)))
			{
				graphics.FillPolygon(brush2, Shift(points, -1.1f, -0.3f, num));
			}
			PointF[] points2 = Shift(points, 0f, 0f, num);
			using (Brush brush3 = new SolidBrush(Pink))
			{
				graphics.FillPolygon(brush3, points2);
			}
			using (Pen pen = new Pen(Color.FromArgb(20, 6, 14), 1.3f * num))
			{
				pen.LineJoin = LineJoin.Round;
				graphics.DrawPolygon(pen, points2);
			}
			using Pen pen2 = new Pen(Color.FromArgb(150, 255, 214, 236), 1f * num);
			graphics.DrawLine(pen2, 3.6f * num, 5.5f * num, 3.6f * num, 17f * num);
		}
		using Graphics graphics2 = Graphics.FromImage(bitmap);
		int num2 = (int)Math.Round(11f * num);
		int num3 = Math.Max(2, (int)Math.Round(2.5f * num));
		int num4 = Math.Max(1, (int)Math.Round(2.5f * num));
		graphics2.DrawImage(image, new Rectangle(0, 0, size, num2), new Rectangle(0, 0, size, num2), GraphicsUnit.Pixel);
		graphics2.DrawImage(image, new Rectangle(num4, num2, size - num4, num3), new Rectangle(0, num2, size - num4, num3), GraphicsUnit.Pixel);
		graphics2.DrawImage(image, new Rectangle(0, num2 + num3, size, size - num2 - num3), new Rectangle(0, num2 + num3, size, size - num2 - num3), GraphicsUnit.Pixel);
		return bitmap;
	}

	private static PointF[] Shift(PointF[] points, float x, float y, float s)
	{
		PointF[] array = new PointF[points.Length];
		for (int i = 0; i < points.Length; i++)
		{
			ref PointF reference = ref array[i];
			reference = new PointF((points[i].X + x) * s, (points[i].Y + y) * s);
		}
		return array;
	}

	[DllImport("gdi32.dll")]
	private static extern IntPtr AddFontMemResourceEx(IntPtr font, uint length, IntPtr reserved, ref uint count);

	private static FontFamily Embedded(string name)
	{
		using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name);
		if (stream == null)
		{
			return null;
		}
		byte[] array = new byte[stream.Length];
		stream.Read(array, 0, array.Length);
		IntPtr intPtr = Marshal.AllocCoTaskMem(array.Length);
		Marshal.Copy(array, 0, intPtr, array.Length);
		uint count = 0u;
		AddFontMemResourceEx(intPtr, (uint)array.Length, IntPtr.Zero, ref count);
		PrivateFontCollection privateFontCollection = new PrivateFontCollection();
		privateFontCollection.AddMemoryFont(intPtr, array.Length);
		return (privateFontCollection.Families.Length > 0) ? privateFontCollection.Families[0] : null;
	}

	internal static bool Drawable(string text)
	{
		string text2 = text ?? "";
		foreach (char c in text2)
		{
			if (c > 'ÿ' && "ıŁłŒœŠšŸŽžƒ–—‘’‚“”„†‡•…‰‹›€™".IndexOf(c) < 0)
			{
				return false;
			}
		}
		return true;
	}

	internal static Font Mono(float size, FontStyle style, GraphicsUnit unit = GraphicsUnit.Pixel, string text = null)
	{
		if (embedded != null && Drawable(text))
		{
			return new Font(embedded, size, embedded.IsStyleAvailable(style) ? style : FontStyle.Regular, unit);
		}
		string[] array = new string[3] { "Cascadia Mono", "Consolas", "Courier New" };
		foreach (string text2 in array)
		{
			Font font = new Font(text2, size, style, unit);
			if (font.Name == text2)
			{
				return font;
			}
			font.Dispose();
		}
		return new Font(FontFamily.GenericMonospace, size, style, unit);
	}

	internal static float CharWidth(Font font)
	{
		using Bitmap image = new Bitmap(1, 1);
		using Graphics graphics = Graphics.FromImage(image);
		return graphics.MeasureString(new string('M', 20), font, PointF.Empty, StringFormat.GenericTypographic).Width / 20f;
	}

	internal static GraphicsPath Plate(RectangleF box, float cut, float tipX, float tipY, float tail, bool both = false)
	{
		float num = (both ? cut : 0f);
		List<PointF> list = new List<PointF>();
		float x = Math.Max(box.Left + cut + 2f, tipX - tail * 0.8f);
		float x2 = Math.Min(box.Right - num - 2f, tipX + tail * 0.2f);
		list.Add(new PointF(box.Left + cut, box.Top));
		if (tipY < box.Top)
		{
			list.Add(new PointF(x, box.Top));
			list.Add(new PointF(tipX, tipY));
			list.Add(new PointF(x2, box.Top));
		}
		list.Add(new PointF(box.Right - num, box.Top));
		if (both)
		{
			list.Add(new PointF(box.Right, box.Top + cut));
		}
		if (both)
		{
			list.Add(new PointF(box.Right, box.Bottom - cut));
		}
		list.Add(new PointF(box.Right - num, box.Bottom));
		if (tipY > box.Bottom)
		{
			list.Add(new PointF(x2, box.Bottom));
			list.Add(new PointF(tipX, tipY));
			list.Add(new PointF(x, box.Bottom));
		}
		list.Add(new PointF(box.Left + cut, box.Bottom));
		list.Add(new PointF(box.Left, box.Bottom - cut));
		list.Add(new PointF(box.Left, box.Top + cut));
		GraphicsPath graphicsPath = new GraphicsPath();
		graphicsPath.AddPolygon(list.ToArray());
		return graphicsPath;
	}

	internal static GraphicsPath Rounded(RectangleF box, float radius)
	{
		GraphicsPath graphicsPath = new GraphicsPath();
		float num = radius * 2f;
		graphicsPath.AddArc(box.Left, box.Top, num, num, 180f, 90f);
		graphicsPath.AddArc(box.Right - num, box.Top, num, num, 270f, 90f);
		graphicsPath.AddArc(box.Right - num, box.Bottom - num, num, num, 0f, 90f);
		graphicsPath.AddArc(box.Left, box.Bottom - num, num, num, 90f, 90f);
		graphicsPath.CloseFigure();
		return graphicsPath;
	}

	internal static int Cells(char c)
	{
		switch (c)
		{
		default:
			if (c < '\u0e47' || c > '\u0e4e')
			{
				break;
			}
			goto case '\u0e31';
		case '\u0e31':
		case '\u0e34':
		case '\u0e35':
		case '\u0e36':
		case '\u0e37':
		case '\u0e38':
		case '\u0e39':
		case '\u0e3a':
			return 0;
		}
		if ((c < 'ᄀ' || c > 'ᅟ') && (c < '⺀' || c > '〾') && (c < 'ぁ' || c > '㏿') && (c < '㐀' || c > '䶿') && (c < '一' || c > '鿿') && (c < 'ꀀ' || c > '\ua4cf') && (c < 'ꥠ' || c > '\ua97f') && (c < '가' || c > '힣') && (c < '豈' || c > '\ufaff') && (c < '︐' || c > '︙') && (c < '︰' || c > '\ufe6f') && (c < '\uff00' || c > '｠') && (c < '￠' || c > '￦'))
		{
			return 1;
		}
		return 2;
	}

	internal static int Cells(string text)
	{
		if (Indic(text))
		{
			return (int)Math.Ceiling(Span(text) - 0.01);
		}
		int num = 0;
		string text2 = text ?? "";
		foreach (char c in text2)
		{
			num += Cells(c);
		}
		return num;
	}

	internal static bool Indic(string text)
	{
		string text2 = text ?? "";
		foreach (char c in text2)
		{
			if ((c >= '\u0900' && c <= '\u0dff') || (c >= 'ሀ' && c <= '\u139f') || (c >= '\u0e80' && c <= '\u0eff') || (c >= 'ក' && c <= '\u17ff') || (c >= '᧠' && c <= '᧿') || Myanmar(c) || (c >= 'Ⴀ' && c <= 'ჿ') || (c >= 'Ა' && c <= 'Ჿ') || (c >= '\u0530' && c <= '֏'))
			{
				return true;
			}
		}
		return false;
	}

	internal static bool Burmese(string text)
	{
		string text2 = text ?? "";
		foreach (char c in text2)
		{
			if (Myanmar(c))
			{
				return true;
			}
		}
		return false;
	}

	private static bool Myanmar(char c)
	{
		if ((c < 'က' || c > '႟') && (c < 'ꧠ' || c > '\ua9ff'))
		{
			if (c >= 'ꩠ')
			{
				return c <= 'ꩿ';
			}
			return false;
		}
		return true;
	}

	internal static Font Myanmar(Font font)
	{
		string[] array = new string[2] { "Myanmar Text", "Segoe UI" };
		foreach (string text in array)
		{
			Font font2 = new Font(text, font.Size, font.Style, font.Unit);
			if (font2.Name == text)
			{
				return font2;
			}
			font2.Dispose();
		}
		return new Font(FontFamily.GenericSansSerif, font.Size, font.Style, font.Unit);
	}

	internal static float Draw(Graphics graphics, string text, Font font, Brush brush, float x, float y)
	{
		if (!Burmese(text))
		{
			graphics.DrawString(text, font, brush, x, y, StringFormat.GenericTypographic);
			return x;
		}
		using Font font2 = Myanmar(font);
		float num = Ascent(font2) - Ascent(font);
		foreach (string item in Pieces(text))
		{
			Font font3 = (Myanmar(item[0]) ? font2 : font);
			graphics.DrawString(item, font3, brush, x, (font3 == font) ? y : (y - num), StringFormat.GenericTypographic);
			x += graphics.MeasureString(item, font3, PointF.Empty, Spaced).Width;
		}
		return x;
	}

	private static float Mixed(Graphics graphics, string text, Font font, Font burmese)
	{
		float num = 0f;
		foreach (string item in Pieces(text))
		{
			num += graphics.MeasureString(item, Myanmar(item[0]) ? burmese : font, PointF.Empty, Spaced).Width;
		}
		return num;
	}

	private static List<string> Pieces(string text)
	{
		List<string> list = new List<string>();
		int num = 0;
		for (int i = 1; i <= text.Length; i++)
		{
			if (i == text.Length || Myanmar(text[i]) != Myanmar(text[num]))
			{
				list.Add(text.Substring(num, i - num));
				num = i;
			}
		}
		return list;
	}

	private static float Ascent(Font font)
	{
		return font.Size * (float)font.FontFamily.GetCellAscent(font.Style) / (float)font.FontFamily.GetEmHeight(font.Style);
	}

	private static double Span(string text)
	{
		double num = 0.0;
		string[] array = text.Split(' ');
		string[] array2 = array;
		foreach (string text2 in array2)
		{
			if (!Indic(text2))
			{
				string text3 = text2;
				foreach (char c in text3)
				{
					num += (double)Cells(c);
				}
				continue;
			}
			lock (measured)
			{
				if (!measured.TryGetValue(text2, out var value))
				{
					if (ruler == null)
					{
						rulerPage = new Bitmap(1, 1);
						ruler = Graphics.FromImage(rulerPage);
						ruler.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
						rulerFont = Mono(14f, FontStyle.Regular, GraphicsUnit.Pixel, "क");
						rulerColumn = ruler.MeasureString(new string('M', 20), rulerFont, PointF.Empty, StringFormat.GenericTypographic).Width / 20f;
						rulerBurmese = Myanmar(rulerFont);
					}
					value = (double)((Burmese(text2) ? Mixed(ruler, text2, rulerFont, rulerBurmese) : ruler.MeasureString(text2, rulerFont, PointF.Empty, StringFormat.GenericTypographic).Width) / rulerColumn) * 1.03;
					if (measured.Count > 4000)
					{
						measured.Clear();
					}
					measured[text2] = value;
				}
				num += value;
			}
		}
		return num + (double)array.Length - 1.0;
	}

	internal static bool RightToLeft(string text)
	{
		string text2 = text ?? "";
		foreach (char c in text2)
		{
			if ((c >= '\u0590' && c <= '\u08ff') || (c >= 'יִ' && c <= '﷿') || (c >= 'ﹰ' && c <= '\ufefe'))
			{
				return true;
			}
			if (char.IsLetter(c))
			{
				return false;
			}
		}
		return false;
	}

	private static StringFormat Trailing()
	{
		StringFormat stringFormat = (StringFormat)StringFormat.GenericTypographic.Clone();
		stringFormat.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces;
		return stringFormat;
	}

	private static StringFormat Mirrored()
	{
		StringFormat stringFormat = (StringFormat)StringFormat.GenericTypographic.Clone();
		stringFormat.FormatFlags = (stringFormat.FormatFlags | StringFormatFlags.DirectionRightToLeft | StringFormatFlags.NoWrap) & ~StringFormatFlags.LineLimit;
		return stringFormat;
	}

	internal static string Cut(string text, int columns)
	{
		text = text ?? "";
		if (Indic(text))
		{
			bool[] array = Letters(text);
			int length = 0;
			for (int i = 1; i <= text.Length; i++)
			{
				if (i >= text.Length || array[i])
				{
					if (Cells(text.Substring(0, i)) > columns)
					{
						break;
					}
					length = i;
				}
			}
			return text.Substring(0, length);
		}
		int num = 0;
		int num2 = 0;
		while (num2 < text.Length && num + Cells(text[num2]) <= columns)
		{
			num += Cells(text[num2++]);
		}
		return text.Substring(0, num2);
	}

	internal static bool Thai(string text)
	{
		string text2 = text ?? "";
		foreach (char c in text2)
		{
			if (c >= 'ก' && c <= '๛')
			{
				return true;
			}
		}
		return false;
	}

	internal static bool Unspaced(string text)
	{
		string text2 = text ?? "";
		foreach (char c in text2)
		{
			if ((c >= '\u0e80' && c <= '\u0eff') || (c >= 'ក' && c <= '\u17ff') || (c >= '᧠' && c <= '᧿') || Myanmar(c))
			{
				return true;
			}
		}
		return Thai(text);
	}

	[DllImport("usp10.dll", CharSet = CharSet.Unicode)]
	private static extern int ScriptItemize(string text, int length, int most, IntPtr control, IntPtr state, [Out] ScriptItem[] items, out int count);

	[DllImport("usp10.dll", CharSet = CharSet.Unicode)]
	private static extern int ScriptBreak(string text, int length, ref ScriptAnalysis analysis, [Out] byte[] marks);

	[DllImport("icu.dll", CallingConvention = CallingConvention.Cdecl)]
	private static extern IntPtr ubrk_open(int type, [MarshalAs(UnmanagedType.LPStr)] string locale, IntPtr text, int length, ref int status);

	[DllImport("icu.dll", CallingConvention = CallingConvention.Cdecl)]
	private static extern int ubrk_next(IntPtr iterator);

	[DllImport("icu.dll", CallingConvention = CallingConvention.Cdecl)]
	private static extern void ubrk_close(IntPtr iterator);

	internal static bool[] Breaks(string word)
	{
		if (!Thai(word))
		{
			bool[] array = Icu(word);
			if (array != null)
			{
				return array;
			}
		}
		bool[] array2 = new bool[word.Length];
		try
		{
			ScriptItem[] array3 = new ScriptItem[word.Length + 1];
			int count = 0;
			bool flag = word.Length > 0 && ScriptItemize(word, word.Length, word.Length, IntPtr.Zero, IntPtr.Zero, array3, out count) == 0;
			int num = 0;
			while (flag && num < count)
			{
				string text = word.Substring(array3[num].Start, array3[num + 1].Start - array3[num].Start);
				byte[] array4 = new byte[text.Length];
				flag = ScriptBreak(text, text.Length, ref array3[num].Analysis, array4) == 0;
				int num2 = 0;
				while (flag && num2 < text.Length)
				{
					array2[array3[num].Start + num2] = (array4[num2] & 1) != 0;
					num2++;
				}
				num++;
			}
			if (flag)
			{
				return array2;
			}
		}
		catch (DllNotFoundException)
		{
		}
		catch (EntryPointNotFoundException)
		{
		}
		return Letters(word);
	}

	private static bool[] Icu(string word)
	{
		IntPtr intPtr = Marshal.StringToHGlobalUni(word);
		try
		{
			int status = 0;
			IntPtr intPtr2 = ubrk_open(2, "", intPtr, word.Length, ref status);
			if (intPtr2 == IntPtr.Zero || status > 0)
			{
				return null;
			}
			bool[] array = new bool[word.Length];
			try
			{
				int num = ubrk_next(intPtr2);
				while (num > 0 && num < word.Length)
				{
					array[num] = true;
					num = ubrk_next(intPtr2);
				}
			}
			finally
			{
				ubrk_close(intPtr2);
			}
			return array;
		}
		catch (DllNotFoundException)
		{
			return null;
		}
		catch (EntryPointNotFoundException)
		{
			return null;
		}
		finally
		{
			Marshal.FreeHGlobal(intPtr);
		}
	}

	internal static bool[] Letters(string word)
	{
		bool[] array = new bool[word.Length];
		if (!Thai(word))
		{
			int[] array2 = StringInfo.ParseCombiningCharacters(word);
			foreach (int num in array2)
			{
				if (num > 0 && !Joins(word[num - 1]) && !Ends(word, num) && (word[num - 1] < 'ເ' || word[num - 1] > 'ໄ') && "ະາຳໆຯ\u102b\u102c\u1038".IndexOf(word[num]) < 0)
				{
					array[num] = true;
				}
			}
			return array;
		}
		for (int j = 1; j < word.Length; j++)
		{
			array[j] = Cells(word[j]) > 0 && "ฯะาำๅๆ".IndexOf(word[j]) < 0 && (word[j - 1] < 'เ' || word[j - 1] > 'ไ');
		}
		return array;
	}

	private static bool Joins(char c)
	{
		if (c != '\u094d' && c != '\u09cd' && c != '\u0a4d' && c != '\u0acd' && c != '\u0b4d' && c != '\u0bcd' && c != '\u0c4d' && c != '\u0ccd' && c != '\u0d4d' && c != '\u17d2')
		{
			return c == '\u1039';
		}
		return true;
	}

	private static bool Ends(string word, int at)
	{
		int num = ((at + 1 < word.Length && word[at + 1] == '\u1037') ? (at + 2) : (at + 1));
		if (num < word.Length)
		{
			if (word[num] != '\u103a')
			{
				return word[num] == '\u1039';
			}
			return true;
		}
		return false;
	}

	internal static string Fill(string word, int columns)
	{
		bool[] array = Breaks(word);
		int length = 0;
		for (int i = 1; i <= word.Length; i++)
		{
			if (i >= word.Length || array[i])
			{
				if (Cells(word.Substring(0, i)) > columns)
				{
					return word.Substring(0, length);
				}
				length = i;
			}
		}
		return word;
	}

	internal static List<string> Wrap(string text, int columns)
	{
		List<string> list = new List<string>();
		string text2 = "";
		string[] array = (text ?? "").Split(' ');
		foreach (string text3 in array)
		{
			string text4 = text3;
			bool flag = Unspaced(text4);
			if (text4.Length > 0 && (Cells(text4[0]) == 2 || flag) && text2.Length > 0 && Cells(text2) + 1 + Cells(text4) > columns)
			{
				string text5 = (flag ? Fill(text4, columns - Cells(text2) - 1) : Kept(text4, Cut(text4, columns - Cells(text2) - 1)));
				if (text5.Length > 0)
				{
					text2 = text2 + " " + text5;
					text4 = text4.Substring(text5.Length);
				}
				list.Add(text2);
				text2 = "";
			}
			while (Cells(text4) > columns)
			{
				if (text2.Length > 0)
				{
					list.Add(text2);
					text2 = "";
				}
				string text6 = (flag ? Fill(text4, columns) : "");
				if (text6.Length == 0)
				{
					text6 = Kept(text4, Cut(text4, columns));
				}
				if (text6.Length == 0)
				{
					text6 = text4.Substring(0, 1);
				}
				list.Add(text6);
				text4 = text4.Substring(text6.Length);
			}
			if (text2.Length == 0)
			{
				text2 = text4;
				continue;
			}
			if (Cells(text2) + 1 + Cells(text4) <= columns)
			{
				text2 = text2 + " " + text4;
				continue;
			}
			list.Add(text2);
			text2 = text4;
		}
		if (text2.Length > 0 || list.Count == 0)
		{
			list.Add(text2);
		}
		return list;
	}

	private static string Kept(string word, string cut)
	{
		while (cut.Length > 1 && cut.Length < word.Length && "，。、！？：；）」』】〕〉》｝’”…,.!?:;)".IndexOf(word[cut.Length]) >= 0)
		{
			cut = cut.Substring(0, cut.Length - 1);
		}
		return cut;
	}

	internal static int[] Takes(List<string> lines, string text, int revealed)
	{
		int[] array = new int[lines.Count];
		int num = 0;
		for (int i = 0; i < lines.Count; i++)
		{
			if (revealed <= 0)
			{
				break;
			}
			array[i] = Math.Min(lines[i].Length, revealed);
			revealed -= array[i];
			num += lines[i].Length;
			while (num < text.Length && text[num] == ' ')
			{
				num++;
				revealed--;
			}
		}
		return array;
	}
}
