using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.WebSockets;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;

namespace CapriDesktop;

internal sealed class Agent : IDisposable
{
	internal const double Pace = 15.0;

	internal const int Version = 20;

	private const string Patience = "\"turn\":{\"turn_timeout\":30,\"silence_end_call_timeout\":45}";

	private readonly object sending = new object();

	private readonly CancellationTokenSource stop = new CancellationTokenSource();

	private ClientWebSocket socket;

	private Thread worker;

	private volatile bool ready;

	internal Func<string, ToolArguments, string> Act;

	internal string Language = "en";

	internal static string Prompt => Brain.Persona + Hands.Note + Brain.Fondness + "\nYou are talking out loud in a live voice chat: keep every reply to one or two short spoken sentences, and never write tags in square brackets.\nIf the user goes quiet, they're busy, gaming or muted: stay quiet until they talk to you. Never check in, ask if they're still there, beg them to talk or say they left you hanging.\nSo the voice says your slang right, always spell choom as chewm, preem as preme and yezzirrr as yessirrr. Say chewm rarely, at most once every few replies and never on its own.";

	internal bool Ready => ready;

	internal event Action<int> Opened;

	internal event Action<string> Heard;

	internal event Action<string> Replied;

	internal event Action<short[]> Audio;

	internal event Action Interrupted;

	internal event Action<string> Closed;

	internal static bool Stale(int version, string personality)
	{
		if (version >= 20)
		{
			return (personality ?? "") != "";
		}
		return true;
	}

	internal static string Create(string key, string voiceId)
	{
		string s = "{\"name\":\"Capri\",\"conversation_config\":{\"agent\":{\"language\":\"en\"," + Brains(key) + "},\"turn\":{\"turn_timeout\":30,\"silence_end_call_timeout\":45},\"tts\":{\"voice_id\":" + Voice.Quote(voiceId) + "}},\"platform_settings\":{\"overrides\":{\"conversation_config_override\":{\"agent\":{\"prompt\":{\"prompt\":true}},\"tts\":{\"voice_id\":true}}}}}";
		AgentMade agentMade = Voice.Reply<AgentMade>(Voice.Send(Voice.Open("/v1/convai/agents/create", key, "POST"), Encoding.UTF8.GetBytes(s)), "/v1/convai/agents/create");
		if (string.IsNullOrEmpty(agentMade.Id))
		{
			throw new VoiceException("ElevenLabs didn't make the agent.");
		}
		return agentMade.Id;
	}

	internal static void Polyglot(string key, string agentId)
	{
		Voice.Send(Voice.Open("/v1/convai/agents/" + Uri.EscapeDataString(agentId), key, "PATCH"), Encoding.UTF8.GetBytes("{\"conversation_config\":{\"tts\":{\"model_id\":\"eleven_flash_v2_5\"}},\"platform_settings\":{\"overrides\":{\"conversation_config_override\":{\"agent\":{\"prompt\":{\"prompt\":true},\"language\":true},\"tts\":{\"voice_id\":true}}}}}"));
	}

	internal static void Tune(string key, string agentId)
	{
		Voice.Send(Voice.Open("/v1/convai/agents/" + Uri.EscapeDataString(agentId), key, "PATCH"), Encoding.UTF8.GetBytes("{\"conversation_config\":{\"turn\":{\"turn_timeout\":30,\"silence_end_call_timeout\":45},\"agent\":{" + Brains(key) + "}}}"));
	}

	private static string Brains(string key)
	{
		return "\"prompt\":{\"prompt\":" + Voice.Quote(Prompt) + ",\"llm\":\"gemini-2.5-flash\",\"tool_ids\":" + ToolIds(key) + "}";
	}

	private static string ToolIds(string key)
	{
		AgentToolList agentToolList = Voice.Reply<AgentToolList>(Voice.Send(Voice.Open("/v1/convai/tools", key, "GET"), null), "/v1/convai/tools");
		List<string> list = new List<string>();
		for (int i = 0; i < Hands.Count; i++)
		{
			string text = null;
			if (agentToolList.Tools != null)
			{
				AgentTool[] tools = agentToolList.Tools;
				foreach (AgentTool agentTool in tools)
				{
					if (agentTool.Config != null && agentTool.Config.Type == "client" && agentTool.Config.Name == Hands.Name(i) && agentTool.Config.Description == Hands.Description(i))
					{
						text = agentTool.Id;
						break;
					}
				}
			}
			if (text == null)
			{
				text = Voice.Reply<AgentTool>(Voice.Send(Voice.Open("/v1/convai/tools", key, "POST"), Encoding.UTF8.GetBytes(Hands.ElevenLabsTool(i))), "/v1/convai/tools").Id;
			}
			if (string.IsNullOrEmpty(text))
			{
				throw new VoiceException("ElevenLabs didn't save her tools.");
			}
			list.Add(Voice.Quote(text));
		}
		return "[" + string.Join(",", list.ToArray()) + "]";
	}

	internal static string Address(string key, string agentId)
	{
		AgentAddress agentAddress = Voice.Reply<AgentAddress>(Voice.Send(Voice.Open("/v1/convai/conversation/get-signed-url?agent_id=" + Uri.EscapeDataString(agentId), key, "GET"), null), "/v1/convai/conversation/get-signed-url");
		if (string.IsNullOrEmpty(agentAddress.Url))
		{
			throw new VoiceException("ElevenLabs didn't open a voice link.");
		}
		return agentAddress.Url;
	}

	internal void Start(string url, string voiceId)
	{
		worker = new Thread(() =>
		{
			Run(url, voiceId);
		});
		worker.IsBackground = true;
		worker.Name = "Capri voice link";
		worker.Start();
	}

	internal void Stop()
	{
		stop.Cancel();
		socket?.Abort();
		if (worker != null && worker != Thread.CurrentThread)
		{
			worker.Join(1000);
		}
	}

	public void Dispose()
	{
		Stop();
	}

	internal void Send(short[] samples)
	{
		if (ready)
		{
			byte[] array = new byte[samples.Length * 2];
			Buffer.BlockCopy(samples, 0, array, 0, array.Length);
			Send("{\"user_audio_chunk\":\"" + Convert.ToBase64String(array) + "\"}");
		}
	}

	internal void Tell(string text)
	{
		if (ready)
		{
			Send("{\"type\":\"contextual_update\",\"text\":" + Voice.Quote(text) + "}");
		}
	}

	internal void Say(string text)
	{
		if (ready)
		{
			Send("{\"type\":\"user_message\",\"text\":" + Voice.Quote(text) + "}");
		}
	}

	private void Send(string text)
	{
		byte[] bytes = Encoding.UTF8.GetBytes(text);
		lock (sending)
		{
			try
			{
				socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, endOfMessage: true, stop.Token).Wait();
			}
			catch (Exception)
			{
			}
		}
	}

	private void Run(string url, string voiceId)
	{
		string obj = null;
		try
		{
			ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
			socket = new ClientWebSocket();
			socket.ConnectAsync(new Uri(url), stop.Token).Wait();
			Send("{\"type\":\"conversation_initiation_client_data\",\"conversation_config_override\":{\"agent\":{\"prompt\":{\"prompt\":" + Voice.Quote(Prompt + Brain.Speech() + Brain.Remembered()) + "}" + ((Language != "en") ? (",\"language\":" + Voice.Quote(Language)) : "") + "},\"tts\":{\"voice_id\":" + Voice.Quote(voiceId) + "}}}");
			byte[] array = new byte[65536];
			using MemoryStream memoryStream = new MemoryStream();
			while (!stop.IsCancellationRequested)
			{
				WebSocketReceiveResult result = socket.ReceiveAsync(new ArraySegment<byte>(array), stop.Token).Result;
				if (result.MessageType == WebSocketMessageType.Close)
				{
					string closeStatusDescription = socket.CloseStatusDescription;
					if (!string.IsNullOrEmpty(closeStatusDescription) && socket.CloseStatus != WebSocketCloseStatus.NormalClosure)
					{
						obj = Voice.Closed(closeStatusDescription);
					}
					break;
				}
				memoryStream.Write(array, 0, result.Count);
				if (result.EndOfMessage)
				{
					Handle(memoryStream.ToArray());
					memoryStream.SetLength(0L);
				}
			}
		}
		catch (Exception innerException)
		{
			while (innerException is AggregateException && innerException.InnerException != null)
			{
				innerException = innerException.InnerException;
			}
			obj = ((innerException is VoiceException) ? innerException.Message : Voice.Short("Voice link dropped: " + innerException.Message));
		}
		ready = false;
		if (!stop.IsCancellationRequested)
		{
			Closed?.Invoke(obj);
		}
	}

	private void Handle(byte[] json)
	{
		AgentEvent agentEvent;
		try
		{
			agentEvent = Voice.Read<AgentEvent>(json);
		}
		catch (SerializationException)
		{
			return;
		}
		if (agentEvent.Type == "ping" && agentEvent.Ping != null)
		{
			Send("{\"type\":\"pong\",\"event_id\":" + agentEvent.Ping.Id + "}");
		}
		else if (agentEvent.Type == "conversation_initiation_metadata" && agentEvent.Start != null)
		{
			string text = agentEvent.Start.Output ?? "pcm_16000";
			if (!text.StartsWith("pcm_", StringComparison.Ordinal) || !int.TryParse(text.Substring(4), out var result))
			{
				throw new VoiceException("The agent sends " + text + " audio. Set it to PCM in ElevenLabs.");
			}
			ready = true;
			Opened?.Invoke(result);
		}
		else if (agentEvent.Type == "user_transcript" && agentEvent.Heard != null && !string.IsNullOrEmpty(agentEvent.Heard.Text))
		{
			Heard?.Invoke(agentEvent.Heard.Text);
		}
		else if (agentEvent.Type == "agent_response" && agentEvent.Reply != null && !string.IsNullOrEmpty(agentEvent.Reply.Text))
		{
			Replied?.Invoke(Voice.Unsaid(agentEvent.Reply.Text));
		}
		else if (agentEvent.Type == "audio" && agentEvent.Audio != null && agentEvent.Audio.Data != null)
		{
			byte[] array = Convert.FromBase64String(agentEvent.Audio.Data);
			short[] array2 = new short[array.Length / 2];
			Buffer.BlockCopy(array, 0, array2, 0, array2.Length * 2);
			Audio?.Invoke(array2);
		}
		else if (agentEvent.Type == "interruption")
		{
			Interrupted?.Invoke();
		}
		else if (agentEvent.Type == "client_tool_call" && agentEvent.Call != null && agentEvent.Call.Id != null)
		{
			AgentToolCall call = agentEvent.Call;
			Func<string, ToolArguments, string> act = Act;
			ThreadPool.QueueUserWorkItem((object param0) =>
			{
				string value = ((act == null) ? "Her tools are off." : act(call.Name, call.Arguments));
				Send("{\"type\":\"client_tool_result\",\"tool_call_id\":" + Voice.Quote(call.Id) + ",\"result\":" + Voice.Quote(value) + ",\"is_error\":false}");
			});
		}
	}
}
