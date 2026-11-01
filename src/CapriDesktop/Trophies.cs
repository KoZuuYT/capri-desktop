using System;
using System.Collections.Generic;

namespace CapriDesktop;

// mod: achievements, worked out from the counters in her settings; unlocked ids are kept in Preferences.Trophies
internal sealed class Trophy
{
	internal readonly string Id;

	internal readonly string Name;

	internal readonly string Text;

	internal readonly int Need;

	private readonly Func<Preferences, double> have;

	private Trophy(string id, string name, string text, int need, Func<Preferences, double> have)
	{
		Id = id;
		Name = name;
		Text = text;
		Need = need;
		this.have = have;
	}

	internal double Have(Preferences preferences)
	{
		try
		{
			return Math.Max(0.0, have(preferences));
		}
		catch (Exception)
		{
			return 0.0;
		}
	}

	internal static int Days(Preferences preferences)
	{
		if (preferences.FirstMet == 0)
		{
			return 0;
		}
		return (int)(DateTime.UtcNow - new DateTime(preferences.FirstMet, DateTimeKind.Utc)).TotalDays;
	}

	private static int Bits(int mask)
	{
		int count = 0;
		for (; mask != 0; mask &= mask - 1)
		{
			count++;
		}
		return count;
	}

	internal static readonly Trophy[] All =
	{
		new Trophy("pet1", "Primeiro carinho", "Faça carinho nela pela primeira vez.", 1, (Preferences p) => p.Pets),
		new Trophy("pet100", "Mão de seda", "Faça 100 carinhos nela.", 100, (Preferences p) => p.Pets),
		new Trophy("purr25", "Ronronômetro", "Faça ela ronronar 25 vezes.", 25, (Preferences p) => p.Purrs),
		new Trophy("throw10", "Arremesso livre", "Arremesse a Capri 10 vezes.", 10, (Preferences p) => p.Throws),
		new Trophy("throw100", "Catapulta humana", "Arremesse a Capri 100 vezes.", 100, (Preferences p) => p.Throws),
		new Trophy("days7", "Uma semana juntos", "Fique 7 dias com ela.", 7, (Preferences p) => Days(p)),
		new Trophy("days30", "Um mês juntos", "Fique 30 dias com ela.", 30, (Preferences p) => Days(p)),
		new Trophy("charm60", "Apegada", "Chegue ao nível Apegada no carinho.", 60, (Preferences p) => p.Charm),
		new Trophy("charm100", "Inseparáveis", "Chegue a 100% de carinho.", 100, (Preferences p) => Math.Round(p.Charm)),
		new Trophy("snack1", "Primeira mordida", "Dê um arquivo pra ela comer.", 1, (Preferences p) => p.Snacks),
		new Trophy("snack50", "Glutona digital", "Dê 50 arquivos pra ela comer.", 50, (Preferences p) => p.Snacks),
		new Trophy("eye10", "Olho vivo", "Faça 10 pontos no Pega o olho.", 10, (Preferences p) => p.EyeBest),
		new Trophy("eye40", "Olho de águia", "Faça 40 pontos no Pega o olho.", 40, (Preferences p) => p.EyeBest),
		new Trophy("found1", "Te achei!", "Encontre a Capri no esconde-esconde.", 1, (Preferences p) => p.Found),
		new Trophy("found10", "Detetive", "Encontre a Capri 10 vezes.", 10, (Preferences p) => p.Found),
		new Trophy("prank10", "Vítima favorita", "Sofra 10 travessuras dela.", 10, (Preferences p) => p.Pranks),
		new Trophy("owl", "Coruja", "Esteja com ela às 3 da manhã.", 1, (Preferences p) => p.Owl),
		new Trophy("moods", "Todas as caras", "Veja os 4 humores do dia dela.", 4, (Preferences p) => Bits(p.MoodsSeen))
	};

	internal static int Count(Preferences preferences)
	{
		HashSet<string> got = Unlocked(preferences);
		return Array.FindAll(All, (Trophy each) => got.Contains(each.Id)).Length;
	}

	internal static HashSet<string> Unlocked(Preferences preferences)
	{
		return new HashSet<string>((preferences.Trophies ?? "").Split(new char[1] { ',' }, StringSplitOptions.RemoveEmptyEntries));
	}

	// the ones newly earned since last time, which are also marked as unlocked
	internal static List<Trophy> Earn(Preferences preferences)
	{
		HashSet<string> got = Unlocked(preferences);
		List<Trophy> fresh = new List<Trophy>();
		foreach (Trophy trophy in All)
		{
			if (!got.Contains(trophy.Id) && trophy.Have(preferences) >= trophy.Need)
			{
				got.Add(trophy.Id);
				fresh.Add(trophy);
			}
		}
		if (fresh.Count > 0)
		{
			preferences.Trophies = string.Join(",", got);
		}
		return fresh;
	}
}
