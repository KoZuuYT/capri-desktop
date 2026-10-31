using System.Runtime.Serialization;

namespace CapriDesktop;

[DataContract]
internal sealed class AgentAudio
{
	[DataMember(Name = "audio_base_64")]
	public string Data;
}
