using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;

namespace CapriDesktop;

internal sealed class Clip
{
	internal const int RestAfter = 90000;

	private static readonly List<Clip> all = new List<Clip>();

	private readonly string name;

	private readonly bool lazy;

	private readonly bool flippable;

	private readonly Clip source;

	private readonly object gate = new object();

	private Bitmap strip;

	private Bitmap flipped;

	private bool fetching;

	private int stripUsed;

	private int flippedUsed;

	internal readonly int Count;

	internal readonly double Fps;

	internal readonly float Height;

	internal readonly float ShiftX;

	internal readonly float ShiftY;

	internal Bitmap Strip
	{
		get
		{
			if (source != null)
			{
				return source.Strip;
			}
			stripUsed = Environment.TickCount;
			Bitmap bitmap = strip;
			if (bitmap != null)
			{
				return bitmap;
			}
			lock (gate)
			{
				if (strip == null)
				{
					strip = Companion.LoadArt(Resource(left: false));
				}
				return strip;
			}
		}
	}

	internal Bitmap Flipped
	{
		get
		{
			if (source != null)
			{
				return source.Flipped;
			}
			if (!flippable)
			{
				return null;
			}
			flippedUsed = Environment.TickCount;
			Bitmap bitmap = flipped;
			if (bitmap == null)
			{
				Warm(left: true);
			}
			return bitmap;
		}
	}

	internal Bitmap FlippedNow
	{
		get
		{
			if (source != null)
			{
				return source.FlippedNow;
			}
			if (!flippable)
			{
				return null;
			}
			flippedUsed = Environment.TickCount;
			lock (gate)
			{
				if (flipped == null)
				{
					flipped = Companion.LoadArt(Resource(left: true));
				}
				return flipped;
			}
		}
	}

	internal bool HasFlipped
	{
		get
		{
			if (source == null)
			{
				return flippable;
			}
			return source.HasFlipped;
		}
	}

	internal Clip(string name, int count, double fps, float height, float shiftX = 0f, float shiftY = 0f, bool lazy = false)
	{
		this.name = name;
		this.lazy = lazy;
		Count = count;
		Fps = fps;
		Height = height;
		ShiftX = shiftX;
		ShiftY = shiftY;
		if (!Companion.HasArt(Resource(left: false)))
		{
			throw new FileNotFoundException("Capri's " + name + " animation is missing. Rebuild with assets/sprites.");
		}
		flippable = Companion.HasArt(Resource(left: true));
		if (!lazy)
		{
			strip = Companion.LoadArt(Resource(left: false));
		}
		lock (all)
		{
			all.Add(this);
		}
	}

	internal Clip(Clip art)
	{
		source = art;
		Count = art.Count;
		Fps = art.Fps;
		Height = art.Height;
		ShiftX = art.ShiftX;
		ShiftY = art.ShiftY;
	}

	private string Resource(bool left)
	{
		return "Capri.Sprite." + name + (left ? "_flipped" : "") + ".png";
	}

	internal void Load(bool left)
	{
		if (source != null)
		{
			source.Load(left);
			return;
		}
		bool flag = left && flippable;
		if (flag)
		{
			flippedUsed = Environment.TickCount;
		}
		else
		{
			stripUsed = Environment.TickCount;
		}
		lock (gate)
		{
			if (flag && flipped == null)
			{
				flipped = Companion.LoadArt(Resource(left: true));
			}
			else if (!flag && strip == null)
			{
				strip = Companion.LoadArt(Resource(left: false));
			}
		}
	}

	internal bool Ready(bool left)
	{
		if (source != null)
		{
			return source.Ready(left);
		}
		if (!left || !flippable)
		{
			return strip != null;
		}
		return flipped != null;
	}

	internal void Warm(bool left)
	{
		if (source != null)
		{
			source.Warm(left);
			return;
		}
		bool wantLeft = left && flippable;
		if (wantLeft)
		{
			flippedUsed = Environment.TickCount;
		}
		else
		{
			stripUsed = Environment.TickCount;
		}
		lock (gate)
		{
			if (fetching || (wantLeft ? (flipped != null) : (strip != null)))
			{
				return;
			}
			fetching = true;
		}
		ThreadPool.QueueUserWorkItem((object param0) =>
		{
			Bitmap bitmap = null;
			try
			{
				bitmap = Companion.LoadArt(Resource(wantLeft));
			}
			catch (Exception)
			{
			}
			lock (gate)
			{
				fetching = false;
				if (bitmap != null)
				{
					if (wantLeft && flipped == null)
					{
						flipped = bitmap;
					}
					else if (!wantLeft && strip == null)
					{
						strip = bitmap;
					}
					else
					{
						bitmap.Dispose();
					}
				}
			}
		});
	}

	internal static void RestAll(int now)
	{
		Clip[] array;
		lock (all)
		{
			array = all.ToArray();
		}
		Clip[] array2 = array;
		foreach (Clip clip in array2)
		{
			clip.Rest(now);
		}
	}

	private void Rest(int now)
	{
		lock (gate)
		{
			if (!fetching)
			{
				if (lazy && strip != null && now - stripUsed > 90000)
				{
					strip.Dispose();
					strip = null;
				}
				if (flipped != null && now - flippedUsed > 90000)
				{
					flipped.Dispose();
					flipped = null;
				}
			}
		}
	}

	internal static long Loaded()
	{
		Clip[] array;
		lock (all)
		{
			array = all.ToArray();
		}
		long num = 0L;
		Clip[] array2 = array;
		foreach (Clip clip in array2)
		{
			lock (clip.gate)
			{
				Bitmap[] array3 = new Bitmap[2] { clip.strip, clip.flipped };
				foreach (Bitmap bitmap in array3)
				{
					if (bitmap != null)
					{
						num += 4L * (long)bitmap.Width * bitmap.Height;
					}
				}
			}
		}
		return num;
	}

	internal static void DisposeAll()
	{
		Clip[] array;
		lock (all)
		{
			array = all.ToArray();
			all.Clear();
		}
		Clip[] array2 = array;
		foreach (Clip clip in array2)
		{
			lock (clip.gate)
			{
				if (clip.strip != null)
				{
					clip.strip.Dispose();
				}
				if (clip.flipped != null)
				{
					clip.flipped.Dispose();
				}
				clip.strip = (clip.flipped = null);
			}
		}
	}
}
