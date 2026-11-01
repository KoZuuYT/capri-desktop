using System;
using System.Drawing;

namespace CapriDesktop;

// mod: her XP and level. The XP is worked out from the counters she already keeps, so everything done before this
// version counts too; each counter shows as a tile in the stats window and is worth some XP per point.
internal sealed class Stat
{
	internal readonly string Name;

	internal readonly int Xp;

	internal readonly Color Tone;

	// 7x7 pixel icon, '#' lit
	internal readonly string[] Icon;

	private readonly Func<Preferences, int> value;

	internal Stat(string name, int xp, Color tone, string[] icon, Func<Preferences, int> value)
	{
		Name = name;
		Xp = xp;
		Tone = tone;
		Icon = icon;
		this.value = value;
	}

	internal int Value(Preferences preferences)
	{
		try
		{
			return Math.Max(0, value(preferences));
		}
		catch (Exception)
		{
			return 0;
		}
	}
}

internal readonly struct Level
{
	internal readonly int Xp;

	internal readonly int Number;

	// XP gathered inside this level, and how much the level takes
	internal readonly int Into;

	internal readonly int Need;

	internal Level(int xp, int number, int into, int need)
	{
		Xp = xp;
		Number = number;
		Into = into;
		Need = need;
	}

	internal double Part => Need <= 0 ? 0.0 : Math.Clamp((double)Into / Need, 0.0, 1.0);

	internal string Title => Progress.TitleAt(Number);
}

internal static class Progress
{
	private static readonly (int From, string Name)[] Titles =
	{
		(1, "Visitante"),
		(3, "Colega"),
		(6, "Cúmplice"),
		(10, "Dupla dinâmica"),
		(15, "Inseparáveis"),
		(21, "Alma gêmea"),
		(30, "Lenda do Mainframe")
	};

	internal static readonly Stat[] All =
	{
		new Stat("Carinhos", 5, Neon.Rose, new[] { ".##.##.", "#######", "#######", "#######", ".#####.", "..###..", "...#..." }, (Preferences p) => p.Pets),
		new Stat("Ronronados", 3, Neon.Rose, new[] { ".......", "#..#..#", "##.##.#", "#.##.##", "#..#..#", ".......", "......." }, (Preferences p) => p.Purrs),
		new Stat("Arremessos", 4, Neon.Cyan, new[] { "...####", ".....##", "....#.#", "...#..#", "..#....", ".#.....", "#......" }, (Preferences p) => p.Throws),
		new Stat("Arquivos comidos", 2, Toast.Mint, new[] { "####...", "#..##..", "#...##.", "#....#.", "#....#.", "#....#.", "######." }, (Preferences p) => p.Snacks),
		new Stat("Recorde no olho", 3, Neon.Cyan, new[] { ".......", "..###..", ".#...#.", "#..#..#", ".#...#.", "..###..", "......." }, (Preferences p) => p.EyeBest),
		new Stat("Partidas", 10, Neon.Cyan, new[] { "#......", "###....", "#####..", "#######", "#####..", "###....", "#......" }, (Preferences p) => p.EyeGames),
		new Stat("Achou ela", 25, Toast.Gold, new[] { ".###...", "#...#..", "#...#..", "#...#..", ".###...", "....##.", ".....##" }, (Preferences p) => p.Found),
		new Stat("Travessuras", 6, Neon.Rose, new[] { "...##..", "..##...", ".####..", "...##..", "..##...", ".##....", "##....." }, (Preferences p) => p.Pranks),
		new Stat("Dias juntos", 30, Toast.Gold, new[] { ".#...#.", "#######", "#.....#", "#.#.#.#", "#.....#", "#.#.#.#", "#######" }, (Preferences p) => Trophy.Days(p)),
		new Stat("Carinho %", 3, Neon.Rose, new[] { "...#...", "...#...", "#######", ".#####.", "..###..", ".##.##.", "#.....#" }, (Preferences p) => (int)Math.Round(p.Charm)),
		new Stat("Madrugadas", 20, Neon.Cyan, new[] { "..###..", ".##....", "##.....", "##.....", "##.....", ".##....", "..###.." }, (Preferences p) => p.Owl),
		new Stat("Conquistas", 150, Toast.Gold, new[] { "#######", "#.###.#", ".#####.", "..###..", "...#...", "..###..", ".#####." }, (Preferences p) => Trophy.Count(p))
	};

	// XP it takes to go from level `n` to the next
	internal static int Step(int n)
	{
		return 80 + 40 * n;
	}

	internal static int Xp(Preferences preferences)
	{
		long xp = 0;
		foreach (Stat stat in All)
		{
			xp += (long)stat.Value(preferences) * stat.Xp;
		}
		return (int)Math.Min(int.MaxValue / 2, xp);
	}

	internal static Level Of(Preferences preferences)
	{
		return At(Xp(preferences));
	}

	internal static Level At(int xp)
	{
		int n = 1;
		int left = xp;
		while (left >= Step(n) && n < 999)
		{
			left -= Step(n);
			n++;
		}
		return new Level(xp, n, left, Step(n));
	}

	internal static string TitleAt(int level)
	{
		string name = Titles[0].Name;
		foreach ((int from, string title) in Titles)
		{
			if (level >= from)
			{
				name = title;
			}
		}
		return name;
	}

	// the next title and the level it comes at, or null at the top
	internal static (int Level, string Name)? NextTitle(int level)
	{
		foreach ((int from, string title) in Titles)
		{
			if (from > level)
			{
				return (from, title);
			}
		}
		return null;
	}

	// changes whenever any counter does; cheap enough to check every frame
	internal static int Signature(Preferences p)
	{
		return HashCode.Combine(HashCode.Combine(p.Pets, p.Purrs, p.Throws, p.Snacks, p.EyeBest, p.EyeGames, p.Found, p.Pranks), p.Owl, (int)Math.Round(p.Charm), p.Trophies?.Length ?? 0, Trophy.Days(p));
	}
}
