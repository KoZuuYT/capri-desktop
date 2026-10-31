using System.Runtime.Serialization;

namespace CapriDesktop;

[DataContract]
internal sealed class ClaudeBlock
{
	[DataMember(Name = "type")]
	public string Type;

	[DataMember(Name = "text")]
	public string Text;

	[DataMember(Name = "id")]
	public string Id;

	[DataMember(Name = "name")]
	public string Name;

	[DataMember(Name = "toolset_name")]
	public string Toolset;

	[DataMember(Name = "input")]
	public ToolArguments Input;
}
