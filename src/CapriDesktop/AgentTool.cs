using System.Runtime.Serialization;

namespace CapriDesktop;

[DataContract]
internal sealed class AgentTool
{
	[DataMember(Name = "id")]
	public string Id;

	[DataMember(Name = "tool_config")]
	public AgentToolConfig Config;
}
