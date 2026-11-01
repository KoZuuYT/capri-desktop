using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace CapriDesktop;

// mod: the fun pack. Mood of the day, sleeping at night, feeding her files, the "Pega o olho" minigame,
// hide-and-seek, achievements, and the neon notice cards (Toast) the helpers use.
internal sealed partial class Companion
{
	internal static readonly string[] MoodNames = new string[4] { "Sonolenta", "Elétrica", "Carente", "Rabugenta" };

	private static readonly string[] MoodTexts = new string[4]
	{
		"Hoje ela acordou com sono: boceja, cochila mais cedo e dorme rapidinho à noite.",
		"Hoje ela tá a mil: solta faíscas por aí e apronta mais travessuras.",
		"Hoje ela tá carente: pede carinho, e cada carinho vale mais.",
		"Hoje ela tá rabugenta: resmunga, e carinho rende menos. Mas no fundo ela gosta de você."
	};

	// added to her character for the AI, after the charm text
	private static readonly string[] MoodPrompts = new string[4]
	{
		"Today you are sleepy: you yawn now and then, keep answers a little shorter and complain softly about being tired.",
		"Today you are electric and hyper: extra energetic, playful and quick, buzzing with sparks.",
		"Today you are needy: you openly want attention and affection, and you ask for it.",
		"Today you are grumpy: a bit sarcastic and short, grumbling, but still caring underneath."
	};

	private static readonly string[][] MoodHello = new string[4][]
	{
		new string[3] { "*bocejo* Hoje eu tô no modo economia de energia, choom.", "Acordei com sono. Não repara se eu cochilar.", "Hoje é dia de preguiça. Só avisando." },
		new string[3] { "Tô ligada no 220 hoje! Bora fazer alguma coisa!", "Hoje eu tô elétrica, choom. Cuidado com as faíscas.", "Energia no máximo! Quem precisa de café?" },
		new string[3] { "Hoje eu tô carente. Me dá atenção, vai.", "Senti sua falta. Faz um carinho em mim?", "Hoje eu só quero ficar perto de você, choom." },
		new string[3] { "Hoje não tô de bom humor. Não leva pro pessoal.", "Acordei rabugenta. Fica esperto, choom.", "Hmpf. Dia ruim. Mas você pode ficar." }
	};

	private static readonly string[][] MoodBits = new string[4][]
	{
		new string[4] { "*boceeejo*", "Que sono...", "Cinco minutinhos de cochilo e eu volto.", "Meus olhos tão fechando sozinhos." },
		new string[4] { "Bzzzt! Desculpa, escapou uma faísca.", "Tô cheia de energia, choom!", "Alguém mais tá ouvindo esse zumbido? Sou eu.", "Faísca grátis pra você." },
		new string[4] { "Me faz um carinho?", "Ei. Lembra de mim aqui?", "Tô aqui, sabia? Esperando um carinho.", "Só passando pra dizer que gosto de você." },
		new string[4] { "Hmpf.", "Nem vem, tô de mau humor.", "Esse cursor tá me irritando.", "Tá, tá, eu tô aqui. Feliz?" }
	};

	private static readonly string[] NightNags = new string[3] { "Já passou da meia-noite, choom. Vai dormir, vai.", "Tá tarde. Até eu tô com sono.", "Amanhã você me agradece se for dormir agora." };

	private static readonly string[] NightWakes = new string[3] { "Hm? Ainda acordado? Que horas são...", "Me acordou... tá tudo bem, choom?", "*bocejo* Você também devia estar dormindo." };

	private EyeHunt eyeHunt;

	private HideSeek hideSeek;

	private bool hideSeekPending;

	private Snack.Hungry hungry;

	private Snack.Snooze snooze;

	private bool nightNap;

	private int nagDay = -1;

	private int owlDay = -1;

	private double nextFun = -1.0;

	private double nextMoodBit = -1.0;

	private double nextTrophyCheck;

	private double nextTrophyToast;

	private readonly Queue<Trophy> trophyQueue = new Queue<Trophy>();

	private readonly List<DateTime> snackTimes = new List<DateTime>();

	private static int Today => (int)(DateTime.Now.Date.Ticks / TimeSpan.TicksPerDay);

	private int MoodNow => Math.Clamp(preferences.Mood, 0, 3);

	internal string MoodName => MoodNames[MoodNow];

	internal string MoodText => MoodTexts[MoodNow];

	internal int CharmPercent => (int)preferences.Charm;

	internal bool NightSleepOn => preferences.NightSleep;

	internal bool BootScreenOn => preferences.BootScreen && !testing;

	internal int TrophyCount
	{
		get
		{
			HashSet<string> got = Trophy.Unlocked(preferences);
			return Array.FindAll(Trophy.All, (Trophy each) => got.Contains(each.Id)).Length;
		}
	}

	internal string[] FunStats => new string[2]
	{
		"Eye hunt record: " + preferences.EyeBest + "   Games: " + preferences.EyeGames,
		"Files eaten: " + preferences.Snacks + "   Found her: " + preferences.Found + "x"
	};

	// a game is running, so pranks, check-ins and the like wait
	private bool FunBusy => eyeHunt != null || hideSeek != null || hideSeekPending;

	private bool AtEase => !hidden && !dragging && !pressed && !chatting && !typer.Visible && !menu.Visible && speech == null && talkState == TalkState.Off && outro == null && working == null && !FunBusy;

	// pranks come sooner when she's hyper, a bit sooner when grumpy
	private double MoodPace => MoodNow switch
	{
		1 => 0.6,
		3 => 0.85,
		_ => 1.0
	};

	// how much a pet is worth today
	private double PetCharm => MoodNow switch
	{
		2 => 0.75,
		3 => 0.35,
		_ => 0.5
	};

	private void RollMood()
	{
		int today = Today;
		if (preferences.MoodDay != today)
		{
			preferences.MoodDay = today;
			int mood = random.Next(4);
			if (mood == preferences.Mood)
			{
				// not the same face two days running, most of the time
				mood = random.Next(4);
			}
			preferences.Mood = mood;
		}
		preferences.Mood = MoodNow;
		preferences.MoodsSeen |= 1 << preferences.Mood;
	}

	private string FondText()
	{
		return Brain.Fond(preferences.Charm) + " " + MoodPrompts[MoodNow];
	}

	private void Notify(ToastKind kind, string title, string body, double seconds = 6.5)
	{
		if (!testing)
		{
			Toast.Show(kind, title, body, seconds);
		}
	}

	// runs once, `seconds` from now, on the UI thread
	private static void After(double seconds, Action then)
	{
		System.Windows.Forms.Timer once = new System.Windows.Forms.Timer { Interval = Math.Max(1, (int)(seconds * 1000.0)) };
		once.Tick += (object sender, EventArgs e) =>
		{
			once.Stop();
			once.Dispose();
			then();
		};
		once.Start();
	}

	// once a second from the main loop
	private void CheckFun(double now, long quiet)
	{
		if (now < nextFun)
		{
			return;
		}
		nextFun = now + 1.0;
		if (preferences.MoodDay != Today)
		{
			RollMood();
			Brain.Fondness = FondText();
			SavePreferences();
		}
		if (hideSeek != null && hideSeek.Running && hideSeek.Elapsed > 2.5 && !hidden)
		{
			// she was shown some other way, so the game is off
			hideSeek.Stop();
			hideSeek = null;
		}
		if (hideSeek != null && hideSeek.HintDue)
		{
			hideSeek.Hinted();
			Notify(ToastKind.Hide, "Dica", "Ela está escondida na borda " + HideSeek.EdgeNames[hideSeek.Edge] + " da tela. Fica de olho!", 7.0);
		}
		CheckSnooze();
		if (now >= nextTrophyCheck)
		{
			nextTrophyCheck = now + 5.0;
			CheckTrophies();
		}
		if (trophyQueue.Count > 0 && now >= nextTrophyToast && !FunBusy)
		{
			Trophy trophy = trophyQueue.Dequeue();
			nextTrophyToast = now + 2.4;
			Notify(ToastKind.Trophy, "Conquista desbloqueada", trophy.Name + ": " + trophy.Text, 8.0);
			if (!hidden)
			{
				ShowPetHearts();
			}
		}
		CheckNight(now, quiet);
		if (preferences.MoodTold != Today && now > 25.0 && quiet < 60000 && AtEase)
		{
			preferences.MoodTold = Today;
			SavePreferences();
			Notify(ToastKind.Mood, "Humor do dia: " + MoodName, MoodText, 9.0);
			Remark("mood", MoodHello[MoodNow][random.Next(MoodHello[MoodNow].Length)]);
			nextMoodBit = now + 600.0 + random.NextDouble() * 600.0;
			return;
		}
		if (nextMoodBit < 0.0)
		{
			nextMoodBit = now + 600.0 + random.NextDouble() * 600.0;
		}
		else if (now >= nextMoodBit && quiet < 120000 && AtEase && !Asleep && !napAsked)
		{
			nextMoodBit = now + 720.0 + random.NextDouble() * 780.0;
			MoodBit();
		}
	}

	// a small thing in character with today's mood
	private void MoodBit()
	{
		int mood = MoodNow;
		bool speak = random.Next(3) > 0;
		switch (mood)
		{
		case 0:
			Sfx.Play(Cue.Yawn);
			break;
		case 1:
			Zap(0.5, ground: seat != null, Cue.Spark);
			break;
		case 2:
			ShowPetHearts();
			speak = true;
			break;
		}
		if (speak)
		{
			Remark("mood", MoodBits[mood][random.Next(MoodBits[mood].Length)]);
		}
	}

	private void CheckNight(double now, long quiet)
	{
		DateTime clockNow = DateTime.Now;
		bool night = clockNow.Hour < 6;
		if (night && clockNow.Hour == 3 && quiet < 60000 && owlDay != Today)
		{
			owlDay = Today;
			preferences.Owl++;
			SavePreferences();
		}
		if (nightNap && !Asleep && !napAsked)
		{
			// she was woken up
			nightNap = false;
			if (night && AtEase)
			{
				Remark("night", NightWakes[random.Next(NightWakes.Length)]);
			}
			return;
		}
		if (!preferences.NightSleep || !night || nightNap)
		{
			return;
		}
		// sleepy days she drops off sooner
		long after = (MoodNow == 0) ? 60000 : 120000;
		if (quiet >= after && !Asleep && !napAsked && AtEase && preferences.Motion)
		{
			nightNap = true;
			Sfx.Play(Cue.Yawn);
			Nap(sleeping: true);
			if (preferences.NightTold != Today)
			{
				preferences.NightTold = Today;
				SavePreferences();
				Notify(ToastKind.Night, "Modo noite", "A Capri foi dormir. Um clique nela acorda ela.", 7.0);
			}
		}
		else if (clockNow.Hour >= 1 && quiet < 10000 && nagDay != Today && AtEase && random.Next(900) == 0)
		{
			// still up and busy: once a night she nags you to sleep
			nagDay = Today;
			Sfx.Play(Cue.Yawn);
			Notify(ToastKind.Night, "Já é tarde", "São " + clockNow.ToString("HH:mm") + ". Ela acha que você devia ir dormir.", 7.0);
			Remark("night", NightNags[random.Next(NightNags.Length)]);
		}
	}

	private void CheckSnooze()
	{
		bool want = nightNap && Asleep && !hidden;
		if (want && snooze == null)
		{
			snooze = new Snack.Snooze(SnoozeAt);
		}
		else if (!want && snooze != null)
		{
			snooze.Dispose();
			snooze = null;
		}
	}

	private Point? SnoozeAt()
	{
		if (hidden || !Asleep || IsDisposed)
		{
			return null;
		}
		Rectangle body = BodyOnScreen(anchor, (seat != null || riding || onPanel) ? PerchedBody : Body);
		return new Point(body.Left + body.Width * 3 / 5, body.Top + body.Height / 6);
	}

	private void CheckTrophies()
	{
		List<Trophy> fresh = Trophy.Earn(preferences);
		if (fresh.Count == 0)
		{
			return;
		}
		SavePreferences();
		foreach (Trophy trophy in fresh)
		{
			trophyQueue.Enqueue(trophy);
		}
	}

	internal void ShowTrophies(IWin32Window owner, float scale)
	{
		CheckTrophies();
		TrophyView.Open(owner, preferences, tray.Icon, scale);
	}

	internal void ToggleNightSleep()
	{
		preferences.NightSleep = !preferences.NightSleep;
		SavePreferences();
	}

	internal void ToggleBootScreen()
	{
		preferences.BootScreen = !preferences.BootScreen;
		SavePreferences();
	}

	// ---- feeding her files ----

	private Point Mouth()
	{
		Rectangle body = BodyOnScreen(anchor, (seat != null || riding || onPanel) ? PerchedBody : Body);
		return new Point(body.Left + body.Width / 2, body.Top + body.Height / 5);
	}

	private static bool HasFiles(DragEventArgs e)
	{
		try
		{
			return e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop);
		}
		catch (Exception)
		{
			return false;
		}
	}

	protected override void OnDragEnter(DragEventArgs drgevent)
	{
		base.OnDragEnter(drgevent);
		if (testing || hidden || FunBusy || !HasFiles(drgevent))
		{
			drgevent.Effect = DragDropEffects.None;
			return;
		}
		drgevent.Effect = DragDropEffects.Copy;
		if (hungry == null)
		{
			hungry = new Snack.Hungry(Mouth);
			Sfx.Play(Cue.Peek);
		}
	}

	protected override void OnDragOver(DragEventArgs drgevent)
	{
		base.OnDragOver(drgevent);
		drgevent.Effect = (hungry != null && HasFiles(drgevent)) ? DragDropEffects.Copy : DragDropEffects.None;
	}

	protected override void OnDragLeave(EventArgs e)
	{
		base.OnDragLeave(e);
		hungry?.Dispose();
		hungry = null;
	}

	protected override void OnDragDrop(DragEventArgs drgevent)
	{
		base.OnDragDrop(drgevent);
		bool wanted = hungry != null;
		hungry?.Dispose();
		hungry = null;
		if (!wanted)
		{
			return;
		}
		string[] files = null;
		try
		{
			files = drgevent.Data.GetData(DataFormats.FileDrop) as string[];
		}
		catch (Exception)
		{
		}
		Feed(files, new Point(drgevent.X, drgevent.Y));
	}

	// she "eats" the files: only their icons fly into her mouth, the files stay exactly where they are
	private void Feed(string[] files, Point from)
	{
		if (files == null || files.Length == 0)
		{
			return;
		}
		bool first = preferences.Snacks == 0;
		int shown = Math.Min(files.Length, 6);
		Point mouth = Mouth();
		string line = SnackLine(files);
		for (int i = 0; i < shown; i++)
		{
			bool last = i == shown - 1;
			Snack.Eat(Snack.Picture(files[i]), new Point(from.X + i * 16, from.Y - i * 10), mouth, i * 0.26, () =>
			{
				Sfx.Play(Cue.Chomp);
				if (last)
				{
					Fed(files, line, first);
				}
			});
		}
		Sfx.Play(Cue.Whoosh);
	}

	private void Fed(string[] files, string line, bool first)
	{
		preferences.Snacks += files.Length;
		DateTime now = DateTime.Now;
		snackTimes.RemoveAll((DateTime at) => now - at > TimeSpan.FromMinutes(10.0));
		// snacks raise her charm a little, but only the first few every ten minutes
		int counted = Math.Min(files.Length, Math.Max(0, 3 - snackTimes.Count));
		for (int i = 0; i < files.Length; i++)
		{
			snackTimes.Add(now);
		}
		if (counted > 0)
		{
			Fond(0.3 * counted);
		}
		SavePreferences();
		if (first)
		{
			Notify(ToastKind.Snack, "Lanchinho", "Ela adora comer arquivos. Relaxa: nada é apagado, os arquivos continuam no lugar.", 8.0);
		}
		if (!chatting && talkState == TalkState.Off && !typer.Visible)
		{
			Remark("snack", line);
		}
		CheckTrophies();
	}

	private string SnackLine(string[] files)
	{
		if (MoodNow == 3 && random.Next(3) == 0)
		{
			return "Hmpf. Tá, eu como. Mas só porque tô com fome.";
		}
		if (files.Length > 3)
		{
			return "Um banquete inteiro? Tô cheia, choom.";
		}
		string path = files[0];
		if (Directory.Exists(path))
		{
			return "Uma pasta inteira? Gulosa eu, hein.";
		}
		string ext = Path.GetExtension(path).ToLowerInvariant();
		switch (ext)
		{
		case ".png":
		case ".jpg":
		case ".jpeg":
		case ".gif":
		case ".webp":
		case ".bmp":
			return new string[2] { "Hmm, pixels crocantes.", "Que foto gostosa. Literalmente." }[random.Next(2)];
		case ".txt":
		case ".md":
		case ".doc":
		case ".docx":
		case ".pdf":
			return new string[2] { "Muitas palavras. Meio seco, mas serve.", "Li tudo enquanto mastigava." }[random.Next(2)];
		case ".exe":
		case ".msi":
		case ".dll":
		case ".bat":
			return "Picante! Espero que não tenha vírus.";
		case ".mp3":
		case ".wav":
		case ".ogg":
		case ".flac":
			return "Tem gosto de música. Bom ritmo.";
		case ".mp4":
		case ".mkv":
		case ".avi":
		case ".mov":
			return "Um filme inteiro? Agora tô cheia.";
		case ".zip":
		case ".rar":
		case ".7z":
			return "Comprimido demais, quase quebrei um dente.";
		case ".json":
		case ".cs":
		case ".js":
		case ".py":
		case ".html":
			return "Código! Meu prato favorito.";
		case ".lnk":
		case ".url":
			return "Um atalho? Isso é só o cheirinho da comida.";
		default:
			return (ext.Length > 1) ? ("Nhac. Gostinho de " + ext.Substring(1) + ".") : "Nhac. Nem sei o que era, mas tava bom.";
		}
	}

	// ---- games ----

	internal void PlayEyeHunt()
	{
		if (testing || FunBusy)
		{
			return;
		}
		Rectangle work = Screen.FromPoint(Cursor.Position).WorkingArea;
		if (!hidden && !chatting && talkState == TalkState.Off)
		{
			Remark("game", "Bora jogar! Pega meus olhos antes que eles fechem.");
		}
		eyeHunt = new EyeHunt(work, preferences.EyeBest, (int score, int hits) =>
		{
			eyeHunt = null;
			EyeHuntOver(score);
		});
	}

	private void EyeHuntOver(int score)
	{
		bool record = score > preferences.EyeBest;
		preferences.EyeGames++;
		if (record)
		{
			preferences.EyeBest = score;
		}
		SavePreferences();
		Notify(ToastKind.Game, record ? "Novo recorde!" : "Fim de jogo", score + ((score == 1) ? " ponto" : " pontos") + (record ? ". Ninguém pega olho como você." : (". Recorde: " + preferences.EyeBest + ".")), 7.0);
		if (score >= 10)
		{
			Fond(1.0);
		}
		string line = record ? "Novo recorde! Tá ficando bom nisso, choom." : ((score < 5) ? "Meus olhos são rápidos demais pra você." : "Nada mal. Quer tentar de novo?");
		if (!chatting && talkState == TalkState.Off && !typer.Visible)
		{
			Remark("game", line);
		}
		CheckTrophies();
	}

	internal void PlayHideSeek()
	{
		if (testing || FunBusy)
		{
			return;
		}
		if (hidden)
		{
			SetHidden(value: false);
		}
		Bitmap picture = HeadPicture();
		if (picture == null)
		{
			return;
		}
		Rectangle work = Screen.FromPoint(new Point(anchor.X + Width / 2, anchor.Y + Height / 2)).WorkingArea;
		hideSeekPending = true;
		Notify(ToastKind.Hide, "Esconde-esconde", "Ela vai se esconder numa borda da tela e espiar de vez em quando. Ache e clique nela!", 7.0);
		Remark("hide", "Fecha os olhos e conta até três... vou me esconder!");
		After(2.6, () =>
		{
			if (!hideSeekPending || IsDisposed)
			{
				picture.Dispose();
				return;
			}
			hideSeekPending = false;
			SetHidden(value: true);
			hideSeek = new HideSeek(picture, work, HideSeekOver);
			picture.Dispose();
			preferences.HideGames++;
			SavePreferences();
		});
	}

	// her idle pose cut down to the top of her body (head and shoulders), for peeking
	private Bitmap HeadPicture()
	{
		try
		{
			using Bitmap full = new Bitmap(Width, Height, PixelFormat.Format32bppPArgb);
			using (Graphics graphics = Graphics.FromImage(full))
			{
				Draw(graphics, idle, 0);
			}
			Rectangle body = BodyOnScreen(Point.Empty, Body);
			Rectangle cut = Rectangle.Intersect(new Rectangle(body.Left - 12, body.Top - 8, body.Width + 24, (int)(body.Height * 0.46) + 8), new Rectangle(Point.Empty, full.Size));
			if (cut.Width < 8 || cut.Height < 8)
			{
				return null;
			}
			return full.Clone(cut, PixelFormat.Format32bppPArgb);
		}
		catch (Exception)
		{
			return null;
		}
	}

	private void HideSeekOver(bool found, Point spot)
	{
		int edge = hideSeek?.Edge ?? 0;
		double took = (hideSeek == null) ? 0.0 : Math.Max(0.0, hideSeek.Elapsed - HideSeek.CountIn);
		hideSeek = null;
		if (found)
		{
			preferences.Found++;
		}
		SavePreferences();
		// she comes back out right where she was hiding
		StopRide();
		seat = null;
		falling = false;
		walking = false;
		anchor = new Point(spot.X - Width / 2, spot.Y - Height / 2);
		if (hidden)
		{
			ShowHidden(value: false);
		}
		DropSoon();
		if (found)
		{
			Fond(1.0);
			Notify(ToastKind.Hide, "Achou!", "Você encontrou a Capri em " + (int)Math.Round(took) + " segundos.", 6.0);
			Remark("hide", new string[3] { "Achou! Você é bom nisso, choom.", "Droga, me achou! Da próxima eu me escondo melhor.", "Como você me viu? Eu tava tão quietinha." }[random.Next(3)]);
		}
		else
		{
			Notify(ToastKind.Hide, "Ela ganhou", "Ela estava escondida na borda " + HideSeek.EdgeNames[edge] + " da tela.", 6.0);
			Remark("hide", "Ganhei! Eu tava na borda " + HideSeek.EdgeNames[edge] + " o tempo todo.");
		}
		CheckTrophies();
	}

	private void CloseFun()
	{
		hideSeekPending = false;
		eyeHunt?.Dispose();
		eyeHunt = null;
		hideSeek?.Stop();
		hideSeek = null;
		hungry?.Dispose();
		hungry = null;
		snooze?.Dispose();
		snooze = null;
		Toast.ClearAll();
	}
}
