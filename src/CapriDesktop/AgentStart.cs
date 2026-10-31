using System.Runtime.Serialization;

namespace CapriDesktop;

[DataContract]
internal sealed class AgentStart
{
	[DataMember(Name = "agent_output_audio_format")]
	public string Output;
}
