#if !UNINSTALLER
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace CapriSetup;

// the setup window: a borderless neon panel in the Mainframe's colours, drawn by hand into a back buffer so the whole
// frame can glitch, switch on like an old monitor and switch off when it closes. Five steps: boot, what changed,
// options, installing, online.
internal sealed class SetupForm : Form
{
	private enum Page
	{
		Boot,
		Mods,
		Config,
		Install,
		Done
	}

	private sealed class Hit
	{
		internal RectangleF Box;

		internal string Id;

		internal Action Act;

		internal string Tip;
	}

	private struct Star
	{
		internal float X;

		internal float Y;

		internal float Speed;

		internal float Size;

		internal float Phase;
	}

	private sealed class Spark
	{
		internal float X;

		internal float Y;

		internal float Vx;

		internal float Vy;

		internal float Life;

		internal Color Tint;
	}

	private const int W = 880;

	private const int H = 560;

	private const int Rail = 232;

	private const int Bar = 48;

	private const int Pane = 256;

	private const int Foot = 500;

	// the Mainframe palette (CapriDesktop.Neon)
	private static readonly Color Ground = Color.FromArgb(10, 10, 16);

	private static readonly Color Glow = Color.FromArgb(54, 14, 26);

	private static readonly Color Band = Color.FromArgb(16, 12, 20);

	private static readonly Color Rose = Color.FromArgb(255, 93, 102);

	private static readonly Color White = Color.FromArgb(236, 236, 241);

	private static readonly Color Faint = Color.FromArgb(146, 130, 140);

	private static readonly Color Field = Color.FromArgb(17, 16, 28);

	private static readonly Color Hover = Color.FromArgb(30, 20, 36);

	private static readonly Color Edge = Color.FromArgb(92, 24, 36);

	private static readonly Color Hot = Color.FromArgb(232, 66, 70);

	private static readonly Color Lit = Color.FromArgb(255, 124, 122);

	private static readonly Color Cyan = Color.FromArgb(94, 240, 246);

	private static readonly Color Deep = Color.FromArgb(9, 30, 33);

	private static readonly Color DeepEdge = Color.FromArgb(40, 120, 132);

	private static readonly Color OnInk = Color.FromArgb(6, 14, 18);

	private static readonly Color Green = Color.FromArgb(60, 230, 120);

	private static readonly Color Amber = Color.FromArgb(255, 196, 92);

	private static readonly string[] Steps = { "BOOT", "MODS", "CONFIG", "INSTALL", "ONLINE" };

	private static readonly Dictionary<string, Font> Fonts = new Dictionary<string, Font>();

	private readonly bool preview;

	private readonly Options options = new Options();

	private readonly bool existing;

	private readonly Bitmap head;

	private readonly System.Windows.Forms.Timer clock;

	private readonly Stopwatch watch = Stopwatch.StartNew();

	private readonly Random random = new Random();

	private readonly List<Hit> hits = new List<Hit>();

	private readonly Dictionary<string, float> swing = new Dictionary<string, float>();

	private readonly Star[] stars = new Star[70];

	private readonly List<Spark> sparks = new List<Spark>();

	private readonly List<string> boot = new List<string>();

	private readonly List<string> log = new List<string>();

	private readonly List<string> summary = new List<string>();

	private readonly object gate = new object();

	private Bitmap buffer;

	private float k = 1f;

	private double now;

	private double last;

	private Page page;

	private double pageAt;

	private Page reached;

	private string hover;

	private string tip;

	private double typed;

	private int filter = -1;

	private float scroll;

	private float scrollTo;

	private float listHeight;

	private double shown;

	private int target;

	private string step = "";

	private bool working;

	private bool finished;

	private string problem;

	private double startedAt;

	private double took;

	private int blipAt;

	private double glitchUntil;

	private double nextGlitch = 2.5;

	private int glitchSeed;

	private double closingAt = -1;

	private string[] stream = new string[0];

	private double streamAt;

	internal SetupForm(bool preview)
	{
		this.preview = preview;
		existing = Program.Existing;
		options.Apply = !Program.HasSettings;
		head = LoadHead();
		Text = "Capri K.mod — instalador";
		Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
		FormBorderStyle = FormBorderStyle.None;
		StartPosition = FormStartPosition.CenterScreen;
		AutoScaleMode = AutoScaleMode.None;
		KeyPreview = true;
		DoubleBuffered = true;
		SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.Opaque, true);
		BackColor = Ground;
		Seed();
		BootLines();
		clock = new System.Windows.Forms.Timer { Interval = 16 };
		clock.Tick += (_, _) => Frame();
	}

	protected override CreateParams CreateParams
	{
		get
		{
			CreateParams parameters = base.CreateParams;
			parameters.ClassStyle |= 0x20000; // CS_DROPSHADOW
			return parameters;
		}
	}

	protected override void OnLoad(EventArgs e)
	{
		base.OnLoad(e);
		k = DeviceDpi / 96f;
		ClientSize = new Size((int)(W * k), (int)(H * k));
		Rectangle screen = Screen.FromPoint(Cursor.Position).WorkingArea;
		Location = new Point(screen.Left + (screen.Width - Width) / 2, screen.Top + (screen.Height - Height) / 2);
		Shape();
		clock.Start();
		Chime.Play(Chime.Boot);
	}

	protected override void OnDpiChanged(DpiChangedEventArgs e)
	{
		base.OnDpiChanged(e);
		k = e.DeviceDpiNew / 96f;
		ClientSize = new Size((int)(W * k), (int)(H * k));
		Shape();
	}

	// cut corners, like the Mainframe's panels
	private void Shape()
	{
		using GraphicsPath path = Chamfer(new RectangleF(0, 0, ClientSize.Width, ClientSize.Height), 18 * k);
		Region = new Region(path);
	}

	private static Bitmap LoadHead()
	{
		using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("head.png");
		return stream == null ? new Bitmap(8, 8) : new Bitmap(stream);
	}

	private void Seed()
	{
		for (int i = 0; i < stars.Length; i++)
		{
			stars[i] = new Star
			{
				X = (float)(random.NextDouble() * W),
				Y = (float)(random.NextDouble() * H),
				Speed = 4 + (float)random.NextDouble() * 14,
				Size = 1 + (float)random.NextDouble() * 1.6f,
				Phase = (float)(random.NextDouble() * 6.28)
			};
		}
	}

	private void BootLines()
	{
		boot.Add("[OK] kernel K.MOD v" + Program.Version + " carregado");
		boot.Add("[OK] sprites, voz, raios e travessuras verificados");
		boot.Add("[OK] " + Mod.All.Length + " mudanças em relação à Capri original 0.7.7");
		boot.Add("[>>] destino: " + Program.Folder);
		boot.Add(existing ? "[!!] já existe uma Capri aqui: as memórias dela ficam guardadas" : "[OK] nenhuma Capri instalada antes, começando do zero");
		try
		{
			DriveInfo drive = new DriveInfo(Path.GetPathRoot(Program.Folder));
			boot.Add("[i ] espaço livre no disco: " + (drive.AvailableFreeSpace / 1073741824.0).ToString("0.0") + " GB");
		}
		catch (Exception)
		{
		}
		boot.Add("[i ] na 1ª vez ela baixa a voz e os ouvidos dela (alguns GB)");
		boot.Add("[OK] pronta para instalar_");
	}

	// ---------------------------------------------------------------- loop

	private void Frame()
	{
		now = watch.Elapsed.TotalSeconds;
		float dt = (float)Math.Min(0.05, now - last);
		last = now;
		for (int i = 0; i < stars.Length; i++)
		{
			stars[i].Y -= stars[i].Speed * dt;
			if (stars[i].Y < Bar)
			{
				stars[i].Y = H;
				stars[i].X = (float)(random.NextDouble() * W);
			}
		}
		for (int i = sparks.Count - 1; i >= 0; i--)
		{
			Spark spark = sparks[i];
			spark.X += spark.Vx * dt;
			spark.Y += spark.Vy * dt;
			spark.Vy += 260 * dt;
			spark.Vx *= 0.985f;
			spark.Life -= dt;
			if (spark.Life <= 0)
			{
				sparks.RemoveAt(i);
			}
		}
		if (now > nextGlitch)
		{
			Glitch(0.12 + random.NextDouble() * 0.12);
			nextGlitch = now + 2.5 + random.NextDouble() * 4.5;
		}
		if (page == Page.Boot)
		{
			int total = TotalBoot();
			int before = (int)typed;
			if (now - pageAt > 0.7 && typed < total)
			{
				typed = Math.Min(total, typed + dt * 75);
				if ((int)typed / 3 != before / 3)
				{
					Chime.Play(Chime.Key);
				}
			}
		}
		if (page == Page.Mods)
		{
			scroll += (scrollTo - scroll) * Math.Min(1, dt * 14);
		}
		if (page == Page.Install)
		{
			int goal;
			lock (gate)
			{
				goal = target;
			}
			shown = Math.Min(goal, shown + dt * 55);
			if ((int)shown / 5 != blipAt)
			{
				blipAt = (int)shown / 5;
				Chime.Play(Chime.Blip);
			}
			if (now - streamAt > 0.07)
			{
				streamAt = now;
				Churn();
			}
			if (finished && shown >= 99.9 && now - startedAt > 1.2)
			{
				Finish();
			}
			else if (problem != null && !working)
			{
				Fail();
			}
		}
		if (closingAt >= 0 && now - closingAt > 0.32)
		{
			clock.Stop();
			Close();
			return;
		}
		Invalidate();
	}

	private void Glitch(double seconds)
	{
		glitchUntil = now + seconds;
		glitchSeed = random.Next();
	}

	private void Go(Page next)
	{
		if (next == page)
		{
			return;
		}
		page = next;
		pageAt = now;
		if (next > reached)
		{
			reached = next;
		}
		hover = null;
		tip = null;
		Glitch(0.18);
		Chime.Play(Chime.Step);
		if (next == Page.Install)
		{
			Start();
		}
	}

	private void Quit()
	{
		if (working || closingAt >= 0)
		{
			return;
		}
		closingAt = now;
		Glitch(0.3);
		Chime.Play(Chime.Down);
	}

	// ---------------------------------------------------------------- install

	private void Start()
	{
		working = true;
		finished = false;
		problem = null;
		shown = 0;
		target = 0;
		blipAt = 0;
		startedAt = now;
		log.Clear();
		Options chosen = options;
		Thread worker = new Thread(() =>
		{
			string failure = null;
			try
			{
				Action<int, string> say = (percent, line) =>
				{
					lock (gate)
					{
						target = Math.Max(target, percent);
						if (line != null)
						{
							step = line;
							log.Add("[" + (watch.Elapsed.TotalSeconds - startedAt).ToString("00.00") + "] " + line);
						}
					}
				};
				if (preview)
				{
					Program.Pretend(chosen, say);
				}
				else
				{
					Program.Install(chosen, say);
				}
			}
			catch (Exception ex)
			{
				failure = ex.Message;
			}
			BeginInvoke((Action)(() =>
			{
				working = false;
				problem = failure;
				finished = failure == null;
			}));
		});
		worker.IsBackground = true;
		worker.SetApartmentState(ApartmentState.STA);
		worker.Start();
	}

	private void Finish()
	{
		finished = false;
		took = now - startedAt;
		summary.Clear();
		summary.Add("Capri K.mod v" + Program.Version + " instalada em " + Program.Folder);
		summary.Add(options.StartMenu ? "Atalho no Menu Iniciar criado" : "Sem atalho no Menu Iniciar");
		summary.Add(options.Desktop ? "Atalho na Área de Trabalho criado" : "Sem atalho na Área de Trabalho");
		summary.Add(options.Startup ? "Ela liga junto com o Windows" : "Ela não liga junto com o Windows");
		summary.Add(options.Apply ? "Configurações escolhidas aplicadas" : "Suas configurações e memórias foram mantidas");
		summary.Add("Para remover: Configurações > Aplicativos > Capri (mod)");
		Go(Page.Done);
		Chime.Play(Chime.Done);
		Burst(110, 132, 90);
		Burst(560, 150, 70);
		if (options.OpenAfter)
		{
			Launch();
		}
	}

	private void Fail()
	{
		problem ??= "erro desconhecido";
		took = now - startedAt;
		Go(Page.Done);
		Chime.Play(Chime.Error);
		Glitch(0.6);
	}

	private void Launch()
	{
		if (preview)
		{
			return;
		}
		try
		{
			Process.Start(new ProcessStartInfo(Program.Exe) { WorkingDirectory = Program.Folder, UseShellExecute = true });
		}
		catch (Exception)
		{
		}
	}

	private void Burst(float x, float y, int count)
	{
		for (int i = 0; i < count; i++)
		{
			double angle = random.NextDouble() * Math.PI * 2;
			double speed = 80 + random.NextDouble() * 260;
			sparks.Add(new Spark
			{
				X = x,
				Y = y,
				Vx = (float)(Math.Cos(angle) * speed),
				Vy = (float)(Math.Sin(angle) * speed) - 90,
				Life = 0.8f + (float)random.NextDouble() * 1.1f,
				Tint = random.Next(3) switch
				{
					0 => Cyan,
					1 => Rose,
					_ => Green
				}
			});
		}
	}

	private void Churn()
	{
		string[] rows = new string[14];
		for (int i = 0; i < rows.Length; i++)
		{
			char[] row = new char[11];
			for (int j = 0; j < row.Length; j++)
			{
				row[j] = j % 3 == 2 ? ' ' : "0123456789ABCDEF"[random.Next(16)];
			}
			rows[i] = new string(row);
		}
		stream = rows;
	}

	// ---------------------------------------------------------------- input

	protected override void OnMouseMove(MouseEventArgs e)
	{
		base.OnMouseMove(e);
		Hit hit = At(e.Location);
		string id = hit?.Id;
		tip = hit?.Tip;
		if (id != hover)
		{
			hover = id;
			if (hit?.Act != null)
			{
				Chime.Play(Chime.Tick);
			}
		}
		Cursor = hit?.Act != null ? Cursors.Hand : Cursors.Default;
	}

	protected override void OnMouseLeave(EventArgs e)
	{
		base.OnMouseLeave(e);
		hover = null;
		tip = null;
	}

	[DllImport("user32.dll")]
	private static extern bool ReleaseCapture();

	[DllImport("user32.dll")]
	private static extern IntPtr SendMessage(IntPtr window, int message, IntPtr wide, IntPtr low);

	protected override void OnMouseDown(MouseEventArgs e)
	{
		base.OnMouseDown(e);
		if (e.Button != MouseButtons.Left)
		{
			return;
		}
		Hit hit = At(e.Location);
		if (hit?.Act != null)
		{
			hit.Act();
			return;
		}
		if (e.Y < Bar * k)
		{
			ReleaseCapture();
			SendMessage(Handle, 0xA1, (IntPtr)2, IntPtr.Zero); // drag by the title bar
		}
	}

	protected override void OnMouseWheel(MouseEventArgs e)
	{
		base.OnMouseWheel(e);
		if (page == Page.Mods)
		{
			scrollTo = Math.Max(0, Math.Min(Math.Max(0, listHeight - ListBox().Height), scrollTo - e.Delta * 0.45f));
		}
	}

	protected override void OnKeyDown(KeyEventArgs e)
	{
		base.OnKeyDown(e);
		if (e.KeyCode == Keys.Escape)
		{
			Quit();
		}
		else if (e.KeyCode == Keys.Enter)
		{
			Primary();
		}
		else if (page == Page.Mods && (e.KeyCode == Keys.Down || e.KeyCode == Keys.Up || e.KeyCode == Keys.PageDown || e.KeyCode == Keys.PageUp))
		{
			float jump = e.KeyCode == Keys.Down ? 60 : e.KeyCode == Keys.Up ? -60 : e.KeyCode == Keys.PageDown ? 260 : -260;
			scrollTo = Math.Max(0, Math.Min(Math.Max(0, listHeight - ListBox().Height), scrollTo + jump));
		}
	}

	private void Primary()
	{
		switch (page)
		{
			case Page.Boot:
				Go(Page.Mods);
				break;
			case Page.Mods:
				Go(Page.Config);
				break;
			case Page.Config:
				Go(Page.Install);
				break;
			case Page.Done:
				if (problem != null)
				{
					Go(Page.Install);
				}
				else
				{
					Quit();
				}
				break;
		}
	}

	private Hit At(Point point)
	{
		PointF spot = new PointF(point.X / k, point.Y / k);
		for (int i = hits.Count - 1; i >= 0; i--)
		{
			if (hits[i].Box.Contains(spot))
			{
				return hits[i];
			}
		}
		return null;
	}

	private void Add(string id, RectangleF box, Action act, string tip = null)
	{
		hits.Add(new Hit { Id = id, Box = box, Act = act, Tip = tip });
	}

	// ---------------------------------------------------------------- painting

	protected override void OnPaint(PaintEventArgs e)
	{
		Size size = ClientSize;
		if (size.Width <= 0 || size.Height <= 0)
		{
			return;
		}
		if (buffer == null || buffer.Size != size)
		{
			buffer?.Dispose();
			buffer = new Bitmap(size.Width, size.Height, PixelFormat.Format32bppPArgb);
		}
		using (Graphics g = Graphics.FromImage(buffer))
		{
			g.ScaleTransform(k, k);
			Render(g);
		}
		Present(e.Graphics, size);
	}

	// switches on like a CRT (a line that opens up), glitches in slices, and switches off the same way
	private void Present(Graphics g, Size size)
	{
		g.Clear(Ground);
		double open = Math.Min(1, now / 0.45);
		double shut = closingAt < 0 ? 1 : Math.Max(0, 1 - (now - closingAt) / 0.3);
		double v = Math.Min(open, shut);
		if (v < 1)
		{
			float tall = (float)(size.Height * Ease(Math.Max(0, (v - 0.25) / 0.75)));
			float wide = (float)(size.Width * Ease(Math.Min(1, v / 0.25)));
			if (tall < 3)
			{
				using Brush line = new SolidBrush(Color.FromArgb(230, Cyan));
				g.FillRectangle(line, (size.Width - wide) / 2, size.Height / 2f - 1.5f * k, wide, 3 * k);
				return;
			}
			g.InterpolationMode = InterpolationMode.Low;
			g.DrawImage(buffer, new RectangleF(0, (size.Height - tall) / 2, size.Width, tall));
			using Brush wash = new SolidBrush(Color.FromArgb((int)(160 * (1 - v)), White));
			g.FillRectangle(wash, 0, (size.Height - tall) / 2, size.Width, tall);
			return;
		}
		g.DrawImageUnscaled(buffer, 0, 0);
		if (now < glitchUntil)
		{
			Random slice = new Random(glitchSeed + (int)(now * 30));
			int count = 3 + slice.Next(4);
			for (int i = 0; i < count; i++)
			{
				int y = slice.Next(size.Height);
				int h = (int)((4 + slice.Next(22)) * k);
				int dx = (int)((slice.Next(2) == 0 ? -1 : 1) * (6 + slice.Next(26)) * k);
				Rectangle from = new Rectangle(0, y, size.Width, Math.Min(h, size.Height - y));
				g.DrawImage(buffer, new Rectangle(dx, y, from.Width, from.Height), from, GraphicsUnit.Pixel);
				using Brush tint = new SolidBrush(Color.FromArgb(40 + slice.Next(50), slice.Next(2) == 0 ? Cyan : Rose));
				g.FillRectangle(tint, 0, y, size.Width, from.Height);
			}
		}
	}

	private void Render(Graphics g)
	{
		hits.Clear();
		g.SmoothingMode = SmoothingMode.AntiAlias;
		g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
		g.InterpolationMode = InterpolationMode.HighQualityBicubic;
		g.PixelOffsetMode = PixelOffsetMode.HighQuality;
		Backdrop(g);
		TitleBar(g);
		RailPanel(g);
		float enter = (float)Ease(Math.Min(1, (now - pageAt) / 0.35));
		GraphicsState state = g.Save();
		g.TranslateTransform((1 - enter) * 36, 0);
		switch (page)
		{
			case Page.Boot:
				BootPage(g, enter);
				break;
			case Page.Mods:
				ModsPage(g, enter);
				break;
			case Page.Config:
				ConfigPage(g, enter);
				break;
			case Page.Install:
				InstallPage(g, enter);
				break;
			case Page.Done:
				DonePage(g, enter);
				break;
		}
		g.Restore(state);
		Footer(g);
		foreach (Spark spark in sparks)
		{
			int alpha = (int)(255 * Math.Min(1, spark.Life));
			using Brush brush = new SolidBrush(Color.FromArgb(alpha, spark.Tint));
			g.FillRectangle(brush, spark.X - 1.5f, spark.Y - 1.5f, 3, 3);
		}
		Scanlines(g);
		using GraphicsPath frame = Chamfer(new RectangleF(0.75f, 0.75f, W - 1.5f, H - 1.5f), 18);
		using Pen border = new Pen(Color.FromArgb(200, Edge), 1.5f);
		g.DrawPath(border, frame);
	}

	private void Backdrop(Graphics g)
	{
		g.Clear(Ground);
		using (GraphicsPath glow = new GraphicsPath())
		{
			glow.AddEllipse(-260, -260, 760, 640);
			using PathGradientBrush brush = new PathGradientBrush(glow) { CenterColor = Color.FromArgb(150, Glow), SurroundColors = new[] { Color.FromArgb(0, Glow) } };
			g.FillPath(brush, glow);
		}
		// a neon floor scrolling towards you
		float horizon = 380;
		float shift = (float)(now * 22 % 24);
		using (Pen grid = new Pen(Color.FromArgb(26, Cyan), 1f))
		{
			for (float y = 0; y < H - horizon; y += 24)
			{
				float depth = (y + shift) / (H - horizon);
				float row = horizon + (float)Math.Pow(depth, 2) * (H - horizon);
				g.DrawLine(grid, Rail, row, W, row);
			}
			for (int i = -12; i <= 12; i++)
			{
				float x = (Rail + W) / 2f + i * 46;
				g.DrawLine(grid, (Rail + W) / 2f + i * 8, horizon, x + i * 30, H);
			}
		}
		using (LinearGradientBrush fade = new LinearGradientBrush(new RectangleF(0, horizon - 1, W, 80), Ground, Color.FromArgb(0, Ground), 90f))
		{
			g.FillRectangle(fade, Rail, horizon - 1, W - Rail, 80);
		}
		foreach (Star star in stars)
		{
			int alpha = (int)(70 + 90 * (0.5 + 0.5 * Math.Sin(now * 3 + star.Phase)));
			using Brush brush = new SolidBrush(Color.FromArgb(alpha, star.Phase > 4 ? Rose : White));
			g.FillRectangle(brush, star.X, star.Y, star.Size, star.Size);
		}
	}

	private void Scanlines(Graphics g)
	{
		using (Pen line = new Pen(Color.FromArgb(28, 0, 0, 0), 1f))
		{
			for (int y = 0; y < H; y += 3)
			{
				g.DrawLine(line, 0, y, W, y);
			}
		}
		float sweep = (float)(now * 90 % (H + 120)) - 60;
		using LinearGradientBrush band = new LinearGradientBrush(new RectangleF(0, sweep - 30, W, 60), Color.FromArgb(0, Cyan), Color.FromArgb(14, Cyan), 90f) { WrapMode = WrapMode.TileFlipXY };
		g.FillRectangle(band, 0, sweep - 30, W, 60);
	}

	private void TitleBar(Graphics g)
	{
		using (Brush bar = new SolidBrush(Color.FromArgb(235, Band)))
		{
			g.FillRectangle(bar, 0, 0, W, Bar);
		}
		g.DrawImage(head, new RectangleF(14, 9, 30, 30));
		float x = 54;
		GlitchText(g, "CAPRI", Face("Bahnschrift SemiBold SemiConden", 17), White, x, 11, 0.6f);
		x += Measure(g, "CAPRI", Face("Bahnschrift SemiBold SemiConden", 17)) + 4;
		Ink(g, "//", Face("Bahnschrift SemiBold SemiConden", 17), Rose, x, 11);
		x += 22;
		GlitchText(g, "K.MOD", Face("Bahnschrift SemiBold SemiConden", 17), Cyan, x, 11, 0.6f);
		x += Measure(g, "K.MOD", Face("Bahnschrift SemiBold SemiConden", 17)) + 14;
		Ink(g, "SETUP  v" + Program.Version + (preview ? "  ·  PRÉVIA (não instala nada)" : ""), Mono(8.5f), Faint, x, 19);
		// sound, minimise, close
		RectangleF sound = new RectangleF(W - 196, 11, 92, 26);
		Chip(g, "sound", sound, Chime.On ? "SOM: ON" : "SOM: OFF", Chime.On, () =>
		{
			Chime.On = !Chime.On;
			Chime.Play(Chime.Up);
		}, "Liga ou desliga os sons do instalador.");
		RectangleF min = new RectangleF(W - 96, 11, 36, 26);
		Glyph(g, "min", min, "—", () => WindowState = FormWindowState.Minimized, "Minimizar");
		RectangleF close = new RectangleF(W - 54, 11, 36, 26);
		Glyph(g, "close", close, "✕", Quit, working ? "Espere a instalação terminar." : "Fechar o instalador");
		// a pulse that runs along the bottom rule
		using (Pen rule = new Pen(Edge, 1f))
		{
			g.DrawLine(rule, 0, Bar, W, Bar);
		}
		float run = (float)(now * 260 % (W + 200)) - 100;
		using LinearGradientBrush pulse = new LinearGradientBrush(new RectangleF(run - 100, Bar - 1, 200, 2), Color.FromArgb(0, Rose), Rose, 0f) { WrapMode = WrapMode.TileFlipX };
		g.FillRectangle(pulse, run - 100, Bar - 1, 200, 2);
	}

	private void RailPanel(Graphics g)
	{
		using (Brush rail = new SolidBrush(Color.FromArgb(200, Band)))
		{
			g.FillRectangle(rail, 0, Bar + 1, Rail, H - Bar);
		}
		using (LinearGradientBrush line = new LinearGradientBrush(new RectangleF(Rail, Bar, 1, H - Bar), Rose, Color.FromArgb(0, Rose), 90f))
		{
			g.FillRectangle(line, Rail, Bar + 1, 1.5f, H - Bar);
		}
		// the badge: floats, gets a scan bar, a turning ring and now and then a glitch
		float bob = (float)Math.Sin(now * 1.6) * 3;
		RectangleF badge = new RectangleF(36, 70 + bob, 160, 160);
		PointF centre = new PointF(badge.X + badge.Width / 2, badge.Y + badge.Height / 2);
		float turn = (float)(now * 40 % 360);
		using (Pen ring = new Pen(Color.FromArgb(150, Cyan), 2f) { DashPattern = new[] { 6f, 4f } })
		{
			g.DrawArc(ring, centre.X - 98, centre.Y - 98, 196, 196, turn, 110);
			g.DrawArc(ring, centre.X - 98, centre.Y - 98, 196, 196, turn + 180, 110);
		}
		using (Pen ring = new Pen(Color.FromArgb(120, Rose), 1.5f))
		{
			g.DrawArc(ring, centre.X - 90, centre.Y - 90, 180, 180, -turn * 1.5f, 70);
			g.DrawArc(ring, centre.X - 90, centre.Y - 90, 180, 180, -turn * 1.5f + 180, 70);
		}
		bool glitching = now < glitchUntil;
		if (glitching)
		{
			Random slice = new Random(glitchSeed);
			ImageAttributes ghost = Tinted(Cyan, 0.55f);
			g.DrawImage(head, Rect(badge, -5, 0), 0, 0, head.Width, head.Height, GraphicsUnit.Pixel, ghost);
			ghost = Tinted(Rose, 0.55f);
			g.DrawImage(head, Rect(badge, 5, 0), 0, 0, head.Width, head.Height, GraphicsUnit.Pixel, ghost);
			g.DrawImage(head, badge);
			for (int i = 0; i < 3; i++)
			{
				float y = (float)slice.NextDouble() * badge.Height;
				float h = 6 + (float)slice.NextDouble() * 16;
				float dx = (float)(slice.NextDouble() - 0.5) * 30;
				RectangleF src = new RectangleF(0, y / badge.Height * head.Height, head.Width, h / badge.Height * head.Height);
				g.DrawImage(head, new RectangleF(badge.X + dx, badge.Y + y, badge.Width, h), src, GraphicsUnit.Pixel);
			}
		}
		else
		{
			g.DrawImage(head, badge);
		}
		GraphicsState clip = g.Save();
		g.SetClip(badge);
		float scan = badge.Y + (float)(now * 70 % (badge.Height + 40)) - 20;
		using (LinearGradientBrush bar = new LinearGradientBrush(new RectangleF(badge.X, scan - 14, badge.Width, 28), Color.FromArgb(0, Cyan), Color.FromArgb(90, Cyan), 90f) { WrapMode = WrapMode.TileFlipXY })
		{
			g.FillRectangle(bar, badge.X, scan - 14, badge.Width, 28);
		}
		g.Restore(clip);
		string status = page switch
		{
			Page.Install => "INSTALANDO",
			Page.Done => problem != null ? "ERRO" : "ONLINE",
			_ => "AGUARDANDO"
		};
		Color statusColor = page == Page.Done ? (problem != null ? Hot : Green) : page == Page.Install ? Amber : Cyan;
		Ink(g, "ID: CAPRI-K.MOD", Mono(8.5f), Faint, 36, 252);
		bool blink = now * 2 % 2 < 1.3;
		using (Brush dot = new SolidBrush(blink ? statusColor : Color.FromArgb(60, statusColor)))
		{
			g.FillEllipse(dot, 37, 274, 8, 8);
		}
		Ink(g, "STATUS: " + status, Mono(8.5f), statusColor, 50, 270);
		// the steps
		for (int i = 0; i < Steps.Length; i++)
		{
			Page which = (Page)i;
			float y = 312 + i * 36;
			RectangleF row = new RectangleF(20, y, Rail - 40, 30);
			bool current = which == page;
			bool done = which < page || (page == Page.Done && problem == null);
			bool clickable = !working && page != Page.Done && which <= reached && which < Page.Install && !current;
			string id = "step" + i;
			if (current)
			{
				using Brush fill = new SolidBrush(Color.FromArgb(120, Glow));
				using GraphicsPath shape = Chamfer(row, 7);
				g.FillPath(fill, shape);
				using Brush mark = new SolidBrush(Rose);
				g.FillRectangle(mark, row.X, row.Y + 4, 3, row.Height - 8);
			}
			else if (clickable && hover == id)
			{
				using Brush fill = new SolidBrush(Hover);
				using GraphicsPath shape = Chamfer(row, 7);
				g.FillPath(fill, shape);
			}
			Color ink = current ? White : done ? Cyan : Color.FromArgb(110, Faint);
			Ink(g, (i + 1).ToString("00"), Mono(9), current ? Rose : ink, row.X + 12, row.Y + 7);
			Ink(g, Steps[i], Face("Bahnschrift SemiBold SemiConden", 12), ink, row.X + 42, row.Y + 5);
			if (done && !current)
			{
				Ink(g, "✓", Face("Segoe UI Symbol", 10), Cyan, row.Right - 22, row.Y + 5);
			}
			else if (current)
			{
				Ink(g, now * 2 % 2 < 1 ? "◀" : "", Face("Segoe UI Symbol", 8), Rose, row.Right - 20, row.Y + 8);
			}
			if (clickable)
			{
				Add(id, row, () => Go(which), "Voltar para " + Steps[i]);
			}
		}
	}

	private void Footer(Graphics g)
	{
		using (Pen rule = new Pen(Color.FromArgb(120, Edge), 1f))
		{
			g.DrawLine(rule, Rail + 1, Foot, W, Foot);
		}
		string hint = tip ?? page switch
		{
			Page.Boot => "ENTER avança  ·  ESC fecha  ·  arraste pela barra de cima",
			Page.Mods => "Role com a rodinha ou as setas  ·  filtre pelos botões acima da lista",
			Page.Config => "Passe o mouse em uma opção para ver o que ela faz",
			Page.Install => "Não feche esta janela  ·  " + step,
			_ => problem != null ? "Nada foi apagado. Você pode tentar de novo." : "ENTER fecha o instalador"
		};
		if (tip != null)
		{
			Ink(g, "i", Face("Bahnschrift SemiBold SemiConden", 11), Cyan, Pane, Foot + 17);
		}
		Ink(g, Clip(g, hint, Face("Bahnschrift SemiCondensed", 10.5f), 250), Face("Bahnschrift SemiCondensed", 10.5f), tip != null ? White : Faint, Pane + (tip != null ? 14 : 0), Foot + 17);
		switch (page)
		{
			case Page.Boot:
				Press(g, "next", "VER MUDANÇAS  ▶", Slot(0), true, typed >= 10, () => Go(Page.Mods));
				break;
			case Page.Mods:
				Press(g, "back", "◀  VOLTAR", Slot(1), false, true, () => Go(Page.Boot));
				Press(g, "next", "CONFIGURAR  ▶", Slot(0), true, true, () => Go(Page.Config));
				break;
			case Page.Config:
				Press(g, "back", "◀  VOLTAR", Slot(1), false, true, () => Go(Page.Mods));
				Press(g, "next", preview ? "SIMULAR  ▶" : "INSTALAR  ▶", Slot(0), true, true, () => Go(Page.Install));
				break;
			case Page.Install:
				Press(g, "next", "INSTALANDO...", Slot(0), true, false, null);
				break;
			case Page.Done:
				if (problem != null)
				{
					Press(g, "back", "FECHAR", Slot(1), false, true, Quit);
					Press(g, "next", "TENTAR DE NOVO", Slot(0), true, true, () => Go(Page.Install));
				}
				else
				{
					Press(g, "back", options.OpenAfter ? "ABRIR DE NOVO" : "ABRIR CAPRI", Slot(1), false, true, Launch);
					Press(g, "next", "FECHAR  ✓", Slot(0), true, true, Quit);
				}
				break;
		}
	}

	private static RectangleF Slot(int slot)
	{
		return new RectangleF(W - 24 - 160 - slot * 172, Foot + 12, 160, 36);
	}

	// ---------------------------------------------------------------- pages

	private void Header(Graphics g, string code, string title, float enter)
	{
		Ink(g, "> " + code, Mono(9), Cyan, Pane, 66);
		float flicker = now - pageAt < 0.5 ? 1f : 0.35f;
		GlitchText(g, title, Face("Bahnschrift SemiBold SemiConden", 26), White, Pane - 1, 82, flicker);
	}

	private int TotalBoot()
	{
		int total = 0;
		foreach (string line in boot)
		{
			total += line.Length;
		}
		return total;
	}

	private void BootPage(Graphics g, float enter)
	{
		Header(g, "BOOT_SEQUENCE // K.MOD", "BEM-VINDO, CHOOM.", enter);
		Ink(g, "Este é o instalador da Capri K.mod: a Capri Desktop com um monte de mudanças", Face("Bahnschrift SemiCondensed", 11.5f), Faint, Pane, 124);
		Ink(g, "feitas para uso pessoal. Veja o que mudou, escolha como instalar e pronto.", Face("Bahnschrift SemiCondensed", 11.5f), Faint, Pane, 143);
		RectangleF well = new RectangleF(Pane, 178, W - Pane - 24, 236);
		Well(g, well, "TERMINAL");
		int left = (int)typed;
		float y = well.Y + 24;
		foreach (string line in boot)
		{
			if (left <= 0)
			{
				break;
			}
			string part = line.Length <= left ? line : line.Substring(0, left);
			left -= line.Length;
			string tag = part.Length >= 4 ? part.Substring(0, 4) : part;
			Color tagColor = tag.StartsWith("[OK") ? Green : tag.StartsWith("[!!") ? Amber : tag.StartsWith("[>>") ? Rose : Cyan;
			Ink(g, tag, Mono(9.5f), tagColor, well.X + 16, y);
			if (part.Length > 4)
			{
				Ink(g, Clip(g, part.Substring(4), Mono(9.5f), well.Width - 70), Mono(9.5f), White, well.X + 50, y);
			}
			if (left <= 0 && line.Length > part.Length || left <= 0 && now * 2 % 2 < 1)
			{
				float cx = well.X + 50 + Measure(g, part.Length > 4 ? part.Substring(4) : "", Mono(9.5f));
				using Brush caret = new SolidBrush(Rose);
				g.FillRectangle(caret, cx + 1, y + 2, 8, 14);
			}
			y += 25;
		}
		// three tiles under the terminal
		string[] heads = { "VERSÃO", "MUDANÇAS", "DESTINO" };
		string[] values = { "K.mod v" + Program.Version, Mod.All.Length + " vs. original", existing ? "atualizar" : "nova instalação" };
		for (int i = 0; i < 3; i++)
		{
			RectangleF tile = new RectangleF(Pane + i * ((well.Width + 12) / 3), 426, (well.Width - 24) / 3, 58);
			float appear = (float)Math.Max(0, Math.Min(1, (now - pageAt - 0.4 - i * 0.12) / 0.3));
			using Brush fill = new SolidBrush(Color.FromArgb((int)(220 * appear), Field));
			using GraphicsPath shape = Chamfer(tile, 8);
			g.FillPath(fill, shape);
			using Pen edge = new Pen(Color.FromArgb((int)(255 * appear), i == 1 ? DeepEdge : Edge), 1f);
			g.DrawPath(edge, shape);
			Ink(g, heads[i], Mono(8), Color.FromArgb((int)(255 * appear), Faint), tile.X + 12, tile.Y + 9);
			Ink(g, values[i], Face("Bahnschrift SemiBold SemiConden", 14), Color.FromArgb((int)(255 * appear), i == 1 ? Cyan : White), tile.X + 12, tile.Y + 25);
		}
	}

	private RectangleF ListBox()
	{
		return new RectangleF(Pane, 178, W - Pane - 24, Foot - 178 - 10);
	}

	private void ModsPage(Graphics g, float enter)
	{
		Header(g, "DIFF  ORIGINAL_0.7.7 .. K.MOD", "O QUE MUDOU", enter);
		int[] counts = new int[3];
		foreach (Mod mod in Mod.All)
		{
			counts[mod.Kind]++;
		}
		string[] names = { "TUDO " + Mod.All.Length, "NOVO " + counts[0], "MELHOR " + counts[1], "CORRIGIDO " + counts[2] };
		float cx = Pane;
		for (int i = 0; i < names.Length; i++)
		{
			int value = i - 1;
			float wide = Measure(g, names[i], Mono(8.5f)) + 22;
			RectangleF chip = new RectangleF(cx, 136, wide, 26);
			Chip(g, "filter" + i, chip, names[i], filter == value, () =>
			{
				filter = value;
				scroll = scrollTo = 0;
				pageAt = now - 0.35;
				Chime.Play(Chime.Up);
			}, null);
			cx += wide + 8;
		}
		RectangleF box = ListBox();
		GraphicsState state = g.Save();
		g.SetClip(box);
		float y = box.Y - scroll;
		int index = 0;
		Font title = Face("Bahnschrift SemiBold SemiConden", 12.5f);
		Font body = Face("Bahnschrift SemiCondensed", 10.5f);
		foreach (Mod mod in Mod.All)
		{
			if (filter >= 0 && mod.Kind != filter)
			{
				continue;
			}
			List<string> lines = Wrap(g, mod.Text, body, box.Width - 130);
			float tall = 38 + lines.Count * 17;
			float appear = (float)Math.Max(0, Math.Min(1, (now - pageAt - 0.1 - index * 0.045) / 0.3));
			RectangleF card = new RectangleF(box.X + (1 - (float)Ease(appear)) * 50, y, box.Width - 14, tall);
			string id = "mod" + index;
			bool lit = hover == id;
			if (lit)
			{
				card.X += 4;
			}
			Color tagColor = mod.Kind == Mod.New ? Cyan : mod.Kind == Mod.Better ? Rose : Amber;
			int a = (int)(255 * appear);
			using (GraphicsPath shape = Chamfer(card, 9))
			{
				using Brush fill = new SolidBrush(Color.FromArgb((int)(225 * appear), lit ? Hover : Field));
				g.FillPath(fill, shape);
				using Pen edge = new Pen(Color.FromArgb(a, lit ? tagColor : Color.FromArgb(70, 30, 44)), lit ? 1.5f : 1f);
				g.DrawPath(edge, shape);
			}
			using (Brush stripe = new SolidBrush(Color.FromArgb(a, tagColor)))
			{
				g.FillRectangle(stripe, card.X, card.Y + 8, 3, tall - 16);
			}
			RectangleF tag = new RectangleF(card.X + 14, card.Y + 11, 86, 20);
			using (GraphicsPath shape = Chamfer(tag, 5))
			{
				using Brush fill = new SolidBrush(Color.FromArgb((int)(40 * appear), tagColor));
				g.FillPath(fill, shape);
				using Pen edge = new Pen(Color.FromArgb(a, tagColor), 1f);
				g.DrawPath(edge, shape);
			}
			string label = Mod.Tags[mod.Kind];
			Ink(g, label, Mono(8), Color.FromArgb(a, tagColor), tag.X + (tag.Width - Measure(g, label, Mono(8))) / 2, tag.Y + 3);
			Ink(g, mod.Name, title, Color.FromArgb(a, White), card.X + 112, card.Y + 9);
			float ly = card.Y + 31;
			foreach (string line in lines)
			{
				Ink(g, line, body, Color.FromArgb(a, Faint), card.X + 112, ly);
				ly += 17;
			}
			if (card.Bottom > box.Y && card.Y < box.Bottom)
			{
				RectangleF seen = RectangleF.Intersect(card, box);
				Add(id, seen, null, null);
			}
			y += tall + 8;
			index++;
		}
		listHeight = y + scroll - box.Y;
		g.Restore(state);
		// fade at the edges and a thin scrollbar
		using (LinearGradientBrush fade = new LinearGradientBrush(new RectangleF(box.X, box.Bottom - 30, box.Width, 31), Color.FromArgb(0, Ground), Ground, 90f))
		{
			g.FillRectangle(fade, box.X - 2, box.Bottom - 29, box.Width + 4, 30);
		}
		if (listHeight > box.Height)
		{
			float thumb = Math.Max(30, box.Height * box.Height / listHeight);
			float at = box.Y + (box.Height - thumb) * scroll / (listHeight - box.Height);
			using Brush track = new SolidBrush(Color.FromArgb(60, Edge));
			g.FillRectangle(track, box.Right - 4, box.Y, 3, box.Height);
			using Brush bar = new SolidBrush(Rose);
			g.FillRectangle(bar, box.Right - 4, at, 3, thumb);
		}
	}

	private void ConfigPage(Graphics g, float enter)
	{
		Header(g, "CONFIG  --detalhado", "CONFIGURAR", enter);
		float colA = Pane;
		float colB = Pane + 306;
		float wide = 284;
		Ink(g, "// INSTALAÇÃO", Mono(9), Rose, colA, 134);
		float y = 156;
		Toggle(g, "desk", "Atalho na Área de Trabalho", options.Desktop, v => options.Desktop = v, colA, ref y, wide, true, "Coloca um ícone da Capri na sua Área de Trabalho.");
		Toggle(g, "menu", "Atalho no Menu Iniciar", options.StartMenu, v => options.StartMenu = v, colA, ref y, wide, true, "Coloca a Capri no Menu Iniciar, para achar pesquisando \"Capri\".");
		Toggle(g, "boot", "Ligar junto com o Windows", options.Startup, v => options.Startup = v, colA, ref y, wide, true, "Ela aparece sozinha quando você liga o computador. Dá para mudar depois no Mainframe.");
		Toggle(g, "open", "Abrir a Capri no final", options.OpenAfter, v => options.OpenAfter = v, colA, ref y, wide, true, "Abre a Capri assim que a instalação terminar.");
		Toggle(g, "sfx", "Sons do instalador", Chime.On, v => Chime.On = v, colA, ref y, wide, true, "Os bipes e efeitos desta janela (não muda os sons da Capri).");
		RectangleF where = new RectangleF(colA, y + 8, wide, 92);
		Well(g, where, "PASTA");
		Ink(g, Clip(g, Program.Folder, Mono(8.5f), wide - 24), Mono(8.5f), White, where.X + 12, where.Y + 22);
		Ink(g, "É a mesma pasta da Capri oficial, então", Face("Bahnschrift SemiCondensed", 10), Faint, where.X + 12, where.Y + 42);
		Ink(g, existing ? "as memórias e modelos dela continuam lá." : "ela se sente em casa na primeira vez.", Face("Bahnschrift SemiCondensed", 10), Faint, where.X + 12, where.Y + 58);
		Add("folder", where, null, "Fica em AppData\\Local\\Capri. Para remover depois: Configurações > Aplicativos > Capri (mod).");
		Ink(g, "// CAPRI", Mono(9), Rose, colB, 134);
		y = 156;
		Toggle(g, "apply", existing && Program.HasSettings ? "Trocar minhas configurações" : "Aplicar estas configurações", options.Apply, v => options.Apply = v, colB, ref y, wide, true, Program.HasSettings ? "Desligado: as configurações atuais dela ficam como estão. Ligado: troca só os itens abaixo (uma cópia de segurança é guardada)." : "Começa a Capri já com as escolhas abaixo.");
		bool on = options.Apply;
		using (Pen rule = new Pen(Color.FromArgb(70, Edge), 1f))
		{
			g.DrawLine(rule, colB, y - 2, colB + wide, y - 2);
		}
		y += 4;
		Choice(g, "lang", "Idioma", new[] { "SISTEMA", "PT-BR", "EN" }, options.Language, v => options.Language = v, colB, ref y, wide, on, "Em que língua ela fala e escreve.");
		Choice(g, "size", "Tamanho dela", new[] { "P", "M", "G" }, options.Size, v => options.Size = v, colB, ref y, wide, on, "Pequena, média ou grande na tela. Dá para mudar no menu do botão direito → Size.");
		Choice(g, "vol", "Sons dela", new[] { "BAIXO", "MÉDIO", "ALTO" }, options.Sounds, v => options.Sounds = v, colB, ref y, wide, on, "Volume dos efeitos dela (raios, olhos, travessuras). A voz não muda.");
		Toggle(g, "mischief", "Modo travessura", options.Mischief, v => options.Mischief = v, colB, ref y, wide, on, "De vez em quando ela apronta uma travessura inofensiva na sua tela.");
		Toggle(g, "eyes", "Olhos pela tela", options.Eyes, v => options.Eyes = v, colB, ref y, wide, on, "Olhos dela aparecem em lugares aleatórios da tela e seguem o cursor.");
		Toggle(g, "pet", "Carinho com o mouse", options.Petting, v => options.Petting = v, colB, ref y, wide, on, "Esfregar o cursor nela vira carinho, com corações e ronronar.");
		Toggle(g, "voice", "Atalhos por voz", options.VoiceOrders, v => options.VoiceOrders = v, colB, ref y, wide, on, "\"Deita\", \"fica quieta\", \"se esconde\", \"aparece\", \"sobe no meu mouse\"...");
		Toggle(g, "top", "Sempre por cima das janelas", options.OnTop, v => options.OnTop = v, colB, ref y, wide, on, "Ela fica na frente dos outros programas.");
		if (!on)
		{
			Ink(g, "suas configurações atuais serão mantidas", Mono(8), Amber, colB, y + 2);
		}
	}

	private void InstallPage(Graphics g, float enter)
	{
		string dots = new string('.', (int)(now * 3 % 4));
		Header(g, preview ? "SIMULANDO  --sem-mexer-em-nada" : "WRITE  %LOCALAPPDATA%\\Capri", "INSTALANDO" + dots, enter);
		int percent = (int)shown;
		Font big = Face("Bahnschrift SemiBold SemiConden", 52);
		string number = percent + "%";
		GlitchText(g, number, big, White, Pane, 124, now - pageAt < 0.4 ? 1 : 0.15f);
		string current;
		lock (gate)
		{
			current = step;
		}
		Ink(g, Clip(g, current, Face("Bahnschrift SemiCondensed", 12), 380), Face("Bahnschrift SemiCondensed", 12), Cyan, Pane + Measure(g, number, big) + 18, 160);
		// a segmented bar that fills from rose to cyan, the leading cell flashing
		const int cells = 40;
		float barX = Pane;
		float barW = W - Pane - 24;
		float cell = barW / cells;
		int lit = (int)(shown / 100 * cells);
		for (int i = 0; i < cells; i++)
		{
			RectangleF box = new RectangleF(barX + i * cell + 1, 204, cell - 3, 22);
			Color tone = Mix(Rose, Cyan, (float)i / cells);
			if (i < lit)
			{
				using Brush fill = new SolidBrush(tone);
				g.FillRectangle(fill, box);
			}
			else if (i == lit && shown < 100)
			{
				using Brush fill = new SolidBrush(Color.FromArgb(now * 8 % 2 < 1 ? 220 : 60, tone));
				g.FillRectangle(fill, box);
			}
			else
			{
				using Brush fill = new SolidBrush(Color.FromArgb(40, tone));
				g.FillRectangle(fill, box);
			}
		}
		float shine = barX + (float)(now * 340 % (barW + 120)) - 60;
		GraphicsState state = g.Save();
		g.SetClip(new RectangleF(barX, 204, barW * (float)shown / 100, 22));
		using (LinearGradientBrush gleam = new LinearGradientBrush(new RectangleF(shine - 40, 204, 80, 22), Color.FromArgb(0, White), Color.FromArgb(150, White), 0f) { WrapMode = WrapMode.TileFlipX })
		{
			g.FillRectangle(gleam, shine - 40, 204, 80, 22);
		}
		g.Restore(state);
		// the log and a stream of bytes beside it
		RectangleF well = new RectangleF(Pane, 244, barW - 120, 240);
		Well(g, well, "LOG");
		string[] lines;
		lock (gate)
		{
			lines = log.ToArray();
		}
		float y = well.Y + 24;
		int first = Math.Max(0, lines.Length - 8);
		for (int i = first; i < lines.Length; i++)
		{
			bool newest = i == lines.Length - 1;
			Ink(g, Clip(g, lines[i], Mono(9), well.Width - 30), Mono(9), newest ? White : Faint, well.X + 14, y);
			y += 25;
		}
		RectangleF side = new RectangleF(well.Right + 12, well.Y, 108, well.Height);
		Well(g, side, "DATA");
		for (int i = 0; i < stream.Length; i++)
		{
			Ink(g, stream[i], Mono(8), Color.FromArgb(60 + (i * 13 % 120), Cyan), side.X + 12, side.Y + 22 + i * 15);
		}
	}

	private void DonePage(Graphics g, float enter)
	{
		if (problem != null)
		{
			Header(g, "EXIT  CODE 1", "FALHA NA INSTALAÇÃO", enter);
			RectangleF well = new RectangleF(Pane, 140, W - Pane - 24, 150);
			Well(g, well, "ERRO");
			List<string> lines = Wrap(g, problem, Mono(9.5f), well.Width - 30);
			float y = well.Y + 26;
			foreach (string line in lines)
			{
				Ink(g, line, Mono(9.5f), Hot, well.X + 14, y);
				y += 20;
			}
			Ink(g, "Dica: feche a Capri se ela estiver aberta e tente de novo.", Face("Bahnschrift SemiCondensed", 11.5f), Faint, Pane, 310);
			return;
		}
		Ink(g, "> EXIT  CODE 0  //  " + took.ToString("0.0") + " s", Mono(9), Green, Pane, 66);
		float pulse = (float)(0.5 + 0.5 * Math.Sin(now * 4));
		GlitchText(g, "CAPRI ONLINE", Face("Bahnschrift SemiBold SemiConden", 40), White, Pane - 2, 82, now - pageAt < 0.8 ? 1 : 0.2f);
		using (Pen underline = new Pen(Color.FromArgb((int)(120 + 135 * pulse), Cyan), 2f))
		{
			g.DrawLine(underline, Pane, 140, Pane + 330 * (float)Ease(Math.Min(1, (now - pageAt) / 0.6)), 140);
		}
		Ink(g, preview ? "Prévia concluída. Nada foi instalado de verdade." : options.OpenAfter ? "Ela já está acordando no canto da sua tela." : "Abra pelo atalho ou pelo botão abaixo.", Face("Bahnschrift SemiCondensed", 12.5f), Faint, Pane, 152);
		float yy = 194;
		for (int i = 0; i < summary.Count; i++)
		{
			float appear = (float)Math.Max(0, Math.Min(1, (now - pageAt - 0.3 - i * 0.12) / 0.25));
			if (appear <= 0)
			{
				break;
			}
			RectangleF row = new RectangleF(Pane + (1 - appear) * 30, yy, W - Pane - 24, 36);
			using (GraphicsPath shape = Chamfer(row, 7))
			{
				using Brush fill = new SolidBrush(Color.FromArgb((int)(200 * appear), Field));
				g.FillPath(fill, shape);
			}
			bool last = i == summary.Count - 1;
			Ink(g, last ? "i" : "✓", Face("Segoe UI Symbol", 11), Color.FromArgb((int)(255 * appear), last ? Cyan : Green), row.X + 14, row.Y + 8);
			Ink(g, summary[i], Face("Bahnschrift SemiCondensed", 11.5f), Color.FromArgb((int)(255 * appear), last ? Faint : White), row.X + 40, row.Y + 8);
			yy += 44;
		}
	}

	// ---------------------------------------------------------------- widgets

	private void Well(Graphics g, RectangleF box, string name)
	{
		using (GraphicsPath shape = Chamfer(box, 10))
		{
			using Brush fill = new SolidBrush(Color.FromArgb(215, Field));
			g.FillPath(fill, shape);
			using Pen edge = new Pen(Color.FromArgb(150, Edge), 1f);
			g.DrawPath(edge, shape);
		}
		float wide = Measure(g, name, Mono(7.5f)) + 12;
		using (Brush tab = new SolidBrush(Edge))
		{
			g.FillRectangle(tab, box.Right - wide - 14, box.Y - 1, wide, 13);
		}
		Ink(g, name, Mono(7.5f), White, box.Right - wide - 8, box.Y);
	}

	private void Press(Graphics g, string id, string label, RectangleF box, bool primary, bool enabled, Action act)
	{
		bool lit = enabled && hover == id;
		float pulse = (float)(0.5 + 0.5 * Math.Sin(now * 5));
		using GraphicsPath shape = Chamfer(box, 10);
		if (lit)
		{
			using GraphicsPath halo = Chamfer(RectangleF.Inflate(box, 4, 4), 12);
			using Pen glow = new Pen(Color.FromArgb(primary ? 160 : 110, primary ? Cyan : Rose), 2f);
			g.DrawPath(glow, halo);
		}
		Color fill = !enabled ? Color.FromArgb(52, 22, 32) : primary ? (lit ? Lit : Hot) : (lit ? Hover : Field);
		using (Brush brush = new SolidBrush(fill))
		{
			g.FillPath(brush, shape);
		}
		Color edge = !enabled ? Color.FromArgb(80, 40, 50) : primary ? Color.FromArgb((int)(150 + 105 * pulse), Lit) : lit ? Rose : Edge;
		using (Pen pen = new Pen(edge, 1.5f))
		{
			g.DrawPath(pen, shape);
		}
		Font font = Face("Bahnschrift SemiBold SemiConden", 12);
		Color ink = !enabled ? Color.FromArgb(112, 94, 104) : primary ? OnInk : White;
		Ink(g, label, font, ink, box.X + (box.Width - Measure(g, label, font)) / 2, box.Y + 8);
		if (enabled && act != null)
		{
			Add(id, box, () =>
			{
				Chime.Play(Chime.Click);
				act();
			});
		}
	}

	private void Chip(Graphics g, string id, RectangleF box, string label, bool chosen, Action act, string tip)
	{
		bool lit = hover == id;
		using GraphicsPath shape = Chamfer(box, 6);
		using (Brush fill = new SolidBrush(chosen ? Color.FromArgb(lit ? 90 : 60, Cyan) : lit ? Hover : Field))
		{
			g.FillPath(fill, shape);
		}
		using (Pen edge = new Pen(chosen ? Cyan : lit ? Rose : Edge, 1f))
		{
			g.DrawPath(edge, shape);
		}
		Ink(g, label, Mono(8.5f), chosen ? Cyan : lit ? White : Faint, box.X + (box.Width - Measure(g, label, Mono(8.5f))) / 2, box.Y + (box.Height - 14) / 2);
		Add(id, box, act, tip);
	}

	private void Glyph(Graphics g, string id, RectangleF box, string glyph, Action act, string tip)
	{
		bool lit = hover == id;
		bool danger = id == "close";
		if (lit)
		{
			using GraphicsPath shape = Chamfer(box, 6);
			using Brush fill = new SolidBrush(danger ? Hot : Hover);
			g.FillPath(fill, shape);
		}
		Font font = Face("Segoe UI Symbol", 10.5f);
		Ink(g, glyph, font, lit ? White : Faint, box.X + (box.Width - Measure(g, glyph, font)) / 2, box.Y + 3);
		Add(id, box, act, tip);
	}

	private float Swing(string id, bool on)
	{
		swing.TryGetValue(id, out float value);
		value += ((on ? 1 : 0) - value) * 0.3f;
		swing[id] = value;
		return value;
	}

	private void Toggle(Graphics g, string id, string label, bool on, Action<bool> set, float x, ref float y, float wide, bool enabled, string tip)
	{
		RectangleF row = new RectangleF(x, y, wide, 30);
		bool lit = enabled && hover == id;
		if (lit)
		{
			using GraphicsPath shape = Chamfer(row, 6);
			using Brush fill = new SolidBrush(Hover);
			g.FillPath(fill, shape);
		}
		int alpha = enabled ? 255 : 90;
		Ink(g, label, Face("Bahnschrift SemiCondensed", 11.5f), Color.FromArgb(alpha, lit ? White : Color.FromArgb(214, 210, 218)), row.X + 8, row.Y + 5);
		float slide = Swing(id, on);
		RectangleF track = new RectangleF(row.Right - 52, row.Y + 7, 42, 16);
		Color tone = Mix(Color.FromArgb(70, 40, 52), Cyan, slide);
		using (GraphicsPath shape = Chamfer(track, 5))
		{
			using Brush fill = new SolidBrush(Color.FromArgb(enabled ? 255 : 90, Mix(Field, Deep, slide)));
			g.FillPath(fill, shape);
			using Pen edge = new Pen(Color.FromArgb(alpha, tone), 1.2f);
			g.DrawPath(edge, shape);
		}
		RectangleF knob = new RectangleF(track.X + 3 + slide * (track.Width - 20), track.Y + 3, 14, 10);
		using (Brush fill = new SolidBrush(Color.FromArgb(alpha, slide > 0.5f ? Cyan : Faint)))
		{
			g.FillRectangle(fill, knob);
		}
		if (enabled)
		{
			Add(id, row, () =>
			{
				set(!on);
				Chime.Play(on ? Chime.Down : Chime.Up);
			}, tip);
		}
		y += 32;
	}

	private void Choice(Graphics g, string id, string label, string[] names, int value, Action<int> set, float x, ref float y, float wide, bool enabled, string tip)
	{
		int alpha = enabled ? 255 : 90;
		Ink(g, label, Face("Bahnschrift SemiCondensed", 11.5f), Color.FromArgb(alpha, 214, 210, 218), x + 8, y + 5);
		float segments = 150;
		float each = segments / names.Length;
		for (int i = 0; i < names.Length; i++)
		{
			int which = i;
			RectangleF box = new RectangleF(x + wide - segments - 4 + i * each, y + 4, each - 3, 22);
			bool chosen = value == i;
			string sid = id + i;
			bool lit = enabled && hover == sid;
			using (GraphicsPath shape = Chamfer(box, 4))
			{
				using Brush fill = new SolidBrush(Color.FromArgb(alpha, chosen ? Hot : lit ? Hover : Field));
				g.FillPath(fill, shape);
				using Pen edge = new Pen(Color.FromArgb(alpha, chosen ? Lit : lit ? Rose : Edge), 1f);
				g.DrawPath(edge, shape);
			}
			Font font = Mono(7.5f);
			Ink(g, names[i], font, chosen ? (enabled ? OnInk : Color.FromArgb(150, White)) : Color.FromArgb(alpha, lit ? White : Faint), box.X + (box.Width - Measure(g, names[i], font)) / 2, box.Y + 4);
			if (enabled)
			{
				Add(sid, box, () =>
				{
					set(which);
					Chime.Play(Chime.Up);
				}, tip);
			}
		}
		if (enabled)
		{
			Add(id, new RectangleF(x, y, wide - segments - 6, 30), null, tip);
		}
		y += 32;
	}

	// ---------------------------------------------------------------- drawing helpers

	private void GlitchText(Graphics g, string text, Font font, Color color, float x, float y, float amount)
	{
		bool glitching = now < glitchUntil;
		float jitter = glitching ? (float)(random.NextDouble() - 0.5) * 6 * amount : 0;
		float split = 1.2f + (glitching ? 3 * amount : 0);
		Ink(g, text, font, Color.FromArgb(150, Cyan), x - split + jitter, y);
		Ink(g, text, font, Color.FromArgb(150, Rose), x + split + jitter, y);
		Ink(g, text, font, color, x + jitter * 0.3f, y);
	}

	private static void Ink(Graphics g, string text, Font font, Color color, float x, float y)
	{
		using Brush brush = new SolidBrush(color);
		g.DrawString(text, font, brush, x, y, StringFormat.GenericTypographic);
	}

	private static float Measure(Graphics g, string text, Font font)
	{
		return g.MeasureString(text, font, PointF.Empty, StringFormat.GenericTypographic).Width;
	}

	private static string Clip(Graphics g, string text, Font font, float wide)
	{
		if (Measure(g, text, font) <= wide)
		{
			return text;
		}
		while (text.Length > 1 && Measure(g, text + "…", font) > wide)
		{
			text = text.Substring(0, text.Length - 1);
		}
		return text + "…";
	}

	private static List<string> Wrap(Graphics g, string text, Font font, float wide)
	{
		List<string> lines = new List<string>();
		string line = "";
		foreach (string word in text.Split(' '))
		{
			string next = line.Length == 0 ? word : line + " " + word;
			if (line.Length > 0 && Measure(g, next, font) > wide)
			{
				lines.Add(line);
				line = word;
			}
			else
			{
				line = next;
			}
		}
		if (line.Length > 0)
		{
			lines.Add(line);
		}
		return lines;
	}

	private static GraphicsPath Chamfer(RectangleF box, float cut)
	{
		GraphicsPath path = new GraphicsPath();
		path.AddPolygon(new[]
		{
			new PointF(box.X + cut, box.Y),
			new PointF(box.Right, box.Y),
			new PointF(box.Right, box.Bottom - cut),
			new PointF(box.Right - cut, box.Bottom),
			new PointF(box.X, box.Bottom),
			new PointF(box.X, box.Y + cut)
		});
		return path;
	}

	private static Rectangle Rect(RectangleF box, float dx, float dy)
	{
		return new Rectangle((int)(box.X + dx), (int)(box.Y + dy), (int)box.Width, (int)box.Height);
	}

	private static ImageAttributes Tinted(Color tint, float alpha)
	{
		ImageAttributes attributes = new ImageAttributes();
		attributes.SetColorMatrix(new ColorMatrix(new[]
		{
			new float[] { 0, 0, 0, 0, 0 },
			new float[] { 0, 0, 0, 0, 0 },
			new float[] { 0, 0, 0, 0, 0 },
			new float[] { 0, 0, 0, alpha, 0 },
			new float[] { tint.R / 255f, tint.G / 255f, tint.B / 255f, 0, 1 }
		}));
		return attributes;
	}

	private static Color Mix(Color a, Color b, float t)
	{
		t = Math.Max(0, Math.Min(1, t));
		return Color.FromArgb((int)(a.R + (b.R - a.R) * t), (int)(a.G + (b.G - a.G) * t), (int)(a.B + (b.B - a.B) * t));
	}

	private static double Ease(double t)
	{
		return 1 - Math.Pow(1 - t, 3);
	}

	private static Font Face(string family, float size)
	{
		string key = family + size;
		if (!Fonts.TryGetValue(key, out Font font))
		{
			font = new Font(Installed(family) ? family : "Segoe UI", size, GraphicsUnit.Point);
			Fonts[key] = font;
		}
		return font;
	}

	private static Font Mono(float size)
	{
		return Face("Consolas", size);
	}

	private static bool Installed(string family)
	{
		foreach (FontFamily one in FontFamily.Families)
		{
			if (one.Name == family)
			{
				return true;
			}
		}
		return false;
	}

	// ---------------------------------------------------------------- pictures for checking the look

	internal static void Snap(string folder)
	{
		Directory.CreateDirectory(folder);
		Chime.On = false;
		using SetupForm form = new SetupForm(preview: true);
		form.now = 30;
		form.glitchUntil = 0;
		form.nextGlitch = 99;
		form.Churn();
		Page[] pages = { Page.Boot, Page.Mods, Page.Config, Page.Install, Page.Done };
		foreach (Page one in pages)
		{
			form.page = one;
			form.pageAt = 20;
			form.typed = form.TotalBoot();
			if (one == Page.Install)
			{
				form.shown = 62;
				form.step = "Copiando o núcleo da Capri (Capri.exe)...";
				form.log.Add("[00.00] Fechando a Capri, se estiver aberta...");
				form.log.Add("[00.52] Copiando o núcleo da Capri (Capri.exe)...");
			}
			if (one == Page.Done)
			{
				form.took = 3.4;
				form.page = Page.Install;
				form.Finish();
				form.page = Page.Done;
				form.pageAt = 20;
				form.sparks.Clear();
			}
			if (one == Page.Config)
			{
				form.hover = "mischief";
				form.tip = "De vez em quando ela apronta uma travessura inofensiva na sua tela.";
			}
			else
			{
				form.hover = null;
				form.tip = null;
			}
			using Bitmap picture = new Bitmap(W, H, PixelFormat.Format32bppPArgb);
			using (Graphics g = Graphics.FromImage(picture))
			{
				for (int i = 0; i < 12; i++)
				{
					form.Render(g); // lets the switches settle
				}
			}
			picture.Save(Path.Combine(folder, (int)one + "-" + one + ".png"), ImageFormat.Png);
		}
	}
}
#endif
