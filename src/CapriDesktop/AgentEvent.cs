using System.Runtime.Serialization;

namespace CapriDesktop;

[DataContract]
internal sealed class AgentEvent
{
	[DataMember(Name = "type")]
	public string Type;

	[DataMember(Name = "conversation_initiation_metadata_event")]
	public AgentStart Start;

	[DataMember(Name = "audio_event")]
	public AgentAudio Audio;

	[DataMember(Name = "agent_response_event")]
	public AgentReply Reply;

	[DataMember(Name = "user_transcription_event")]
	public AgentHeard Heard;

	[DataMember(Name = "ping_event")]
	public AgentPing Ping;

	[DataMember(Name = "client_tool_call")]
	public AgentToolCall Call;
}
