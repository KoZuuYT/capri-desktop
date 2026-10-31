using System.Runtime.Serialization;

namespace CapriDesktop;

[DataContract]
internal sealed class AgentPing
{
	[DataMember(Name = "event_id")]
	public long Id;
}
