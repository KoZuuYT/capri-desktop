using System.Runtime.Serialization;

namespace CapriDesktop;

[DataContract]
internal sealed class AgentToolCall
{
	[DataMember(Name = "tool_name")]
	public string Name;

	[DataMember(Name = "tool_call_id")]
	public string Id;

	[DataMember(Name = "parameters")]
	public ToolArguments Arguments;
}
