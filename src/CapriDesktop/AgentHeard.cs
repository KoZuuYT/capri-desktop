using System.Runtime.Serialization;

namespace CapriDesktop;

[DataContract]
internal sealed class AgentHeard
{
	[DataMember(Name = "user_transcript")]
	public string Text;
}
