using System.Runtime.Serialization;

namespace CapriDesktop;

[DataContract]
internal sealed class AgentReply
{
	[DataMember(Name = "agent_response")]
	public string Text;
}
