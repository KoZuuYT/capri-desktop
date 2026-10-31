using System.Runtime.Serialization;

namespace CapriDesktop;

[DataContract]
internal sealed class AgentMade
{
	[DataMember(Name = "agent_id")]
	public string Id;
}
