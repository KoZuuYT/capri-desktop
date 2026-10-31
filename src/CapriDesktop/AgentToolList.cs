using System.Runtime.Serialization;

namespace CapriDesktop;

[DataContract]
internal sealed class AgentToolList
{
	[DataMember(Name = "tools")]
	public AgentTool[] Tools;
}
