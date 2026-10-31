using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;

namespace CapriDesktop;

internal sealed class Presence : IDisposable
{
	internal const string Picture = "capri";

	private const string Site = "https://lachydotmcg.github.io/capri-desktop/";

	private const string Server = "https://discord.gg/F4fDHfGtFk";

	private const int Wait = 5000;

	private const int Retry = 30000;

	private const int Refused = 600000;

	private const int Spacing = 4000;

	private const int Refresh = 60000;

	internal static string AppId = "1564889759157850182";

	internal static string PipeName = "discord-ipc-";

	private readonly object gate = new object();

	private readonly AutoResetEvent nudge = new AutoResetEvent(initialState: false);

	private readonly string version;

	private readonly long started = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

	private readonly int pid = Process.GetCurrentProcess().Id;

	private string wanted;

	private string noted;

	private Thread worker;

	private volatile bool closing;

	private volatile string status = "Off";

	internal string Status => status;

	internal Presence(string version)
	{
		this.version = version;
	}

	internal void Show(string doing)
	{
		if (string.IsNullOrEmpty(AppId))
		{
			return;
		}
		lock (gate)
		{
			if (doing == wanted)
			{
				return;
			}
			wanted = doing;
			if (worker == null && doing != null)
			{
				worker = new Thread(Run);
				worker.IsBackground = true;
				worker.Name = "Discord status";
				worker.Start();
			}
		}
		nudge.Set();
	}

	public void Dispose()
	{
		closing = true;
		nudge.Set();
	}

	private void Run()
	{
		while (!closing)
		{
			if (Wanted() == null)
			{
				status = "Off";
				nudge.WaitOne();
				continue;
			}
			NamedPipeClientStream namedPipeClientStream = Connect();
			if (namedPipeClientStream == null)
			{
				status = "No Discord";
				Rest(30000);
				continue;
			}
			int pause = 30000;
			using (namedPipeClientStream)
			{
				try
				{
					pause = Talk(namedPipeClientStream);
				}
				catch (IOException)
				{
					status = "No Discord";
				}
				catch (TimeoutException)
				{
					status = "No Discord";
				}
				catch (ObjectDisposedException)
				{
				}
				catch (InvalidOperationException)
				{
				}
			}
			Rest(pause);
		}
	}

	private void Rest(int pause)
	{
		int num = Environment.TickCount + pause;
		while (!closing && Wanted() != null)
		{
			int num2 = num - Environment.TickCount;
			if (num2 <= 0)
			{
				break;
			}
			nudge.WaitOne(num2);
		}
	}

	private string Wanted()
	{
		lock (gate)
		{
			return wanted;
		}
	}

	private static NamedPipeClientStream Connect()
	{
		foreach (string item in Names())
		{
			NamedPipeClientStream namedPipeClientStream = new NamedPipeClientStream(".", item, PipeDirection.InOut, PipeOptions.Asynchronous);
			try
			{
				namedPipeClientStream.Connect(100);
				return namedPipeClientStream;
			}
			catch (TimeoutException)
			{
				namedPipeClientStream.Dispose();
			}
			catch (IOException)
			{
				namedPipeClientStream.Dispose();
			}
			catch (UnauthorizedAccessException)
			{
				namedPipeClientStream.Dispose();
			}
		}
		return null;
	}

	internal static List<string> Names()
	{
		List<string> list = new List<string>();
		for (int i = 0; i < 10; i++)
		{
			list.Add(PipeName + i);
		}
		return list;
	}

	private int Talk(Stream pipe)
	{
		Send(pipe, 0, Json(new DiscordHello
		{
			ClientId = AppId
		}));
		string said = Receive(pipe, out var op);
		if (op != 1)
		{
			Note("Discord refused her: " + Reason(said));
			return 600000;
		}
		string text = null;
		int num = Environment.TickCount - 60000;
		while (!closing)
		{
			string text2 = Wanted();
			if (text2 == null)
			{
				return 0;
			}
			int num2 = Environment.TickCount - num;
			if (text2 == text && num2 < 60000)
			{
				nudge.WaitOne(60000 - num2);
				continue;
			}
			if (num2 < 4000)
			{
				nudge.WaitOne(4000 - num2);
				continue;
			}
			Send(pipe, 1, Json(Activity(text2)));
			num = Environment.TickCount;
			text = text2;
			if (Answer(pipe))
			{
				status = "Showing: " + text2;
			}
		}
		return 0;
	}

	private DiscordCommand Activity(string doing)
	{
		DiscordCommand discordCommand = new DiscordCommand();
		discordCommand.Nonce = Guid.NewGuid().ToString();
		discordCommand.Args = new DiscordArgs
		{
			Pid = pid,
			Activity = new DiscordActivity
			{
				Details = doing,
				Times = new DiscordTimes
				{
					Start = started
				},
				Assets = new DiscordAssets
				{
					Image = "capri",
					Text = "Capri " + version
				},
				Buttons = new DiscordButton[2]
				{
					new DiscordButton
					{
						Label = "Get Capri",
						Url = "https://lachydotmcg.github.io/capri-desktop/"
					},
					new DiscordButton
					{
						Label = "Join her Discord",
						Url = "https://discord.gg/F4fDHfGtFk"
					}
				}
			}
		};
		return discordCommand;
	}

	private bool Answer(Stream pipe)
	{
		while (true)
		{
			string text = Receive(pipe, out var op);
			switch (op)
			{
			case 3:
				break;
			case 2:
				throw new IOException("Discord closed the connection: " + Reason(text));
			default:
			{
				DiscordReply discordReply = Parse(text);
				if (discordReply == null || discordReply.Evt != "ERROR")
				{
					return true;
				}
				Note("Discord turned down her status: " + Reason(text));
				return false;
			}
			}
			Send(pipe, 4, text);
		}
	}

	private void Note(string problem)
	{
		status = problem;
		if (!(problem == noted))
		{
			noted = problem;
			Brain.Log("Discord status", -1L, problem);
		}
	}

	private static string Reason(string said)
	{
		DiscordReply discordReply = Parse(said);
		if (discordReply == null || discordReply.Data == null || string.IsNullOrEmpty(discordReply.Data.Message))
		{
			return said;
		}
		return discordReply.Data.Message;
	}

	internal static void Send(Stream pipe, int op, string json)
	{
		byte[] bytes = Encoding.UTF8.GetBytes(json);
		byte[] array = new byte[8 + bytes.Length];
		BitConverter.GetBytes(op).CopyTo(array, 0);
		BitConverter.GetBytes(bytes.Length).CopyTo(array, 4);
		bytes.CopyTo(array, 8);
		pipe.Write(array, 0, array.Length);
		pipe.Flush();
	}

	private static string Receive(Stream pipe, out int op)
	{
		byte[] value = Take(pipe, 8, 5000);
		op = BitConverter.ToInt32(value, 0);
		int num = BitConverter.ToInt32(value, 4);
		if (num < 0 || num > 1048576)
		{
			throw new IOException("Discord sent a frame " + num + " bytes long.");
		}
		return Encoding.UTF8.GetString(Take(pipe, num, 5000));
	}

	internal static byte[] Take(Stream pipe, int count, int wait)
	{
		byte[] array = new byte[count];
		int num;
		for (int i = 0; i < count; i += num)
		{
			IAsyncResult asyncResult = pipe.BeginRead(array, i, count - i, null, null);
			if (!asyncResult.AsyncWaitHandle.WaitOne(wait))
			{
				throw new TimeoutException("Discord didn't answer.");
			}
			num = pipe.EndRead(asyncResult);
			if (num == 0)
			{
				throw new EndOfStreamException("Discord hung up.");
			}
		}
		return array;
	}

	private static string Json(object value)
	{
		using MemoryStream memoryStream = new MemoryStream();
		new DataContractJsonSerializer(value.GetType()).WriteObject(memoryStream, value);
		return Encoding.UTF8.GetString(memoryStream.ToArray());
	}

	private static DiscordReply Parse(string json)
	{
		try
		{
			using MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(json ?? ""));
			return (DiscordReply)new DataContractJsonSerializer(typeof(DiscordReply)).ReadObject(stream);
		}
		catch (SerializationException)
		{
			return null;
		}
	}
}
