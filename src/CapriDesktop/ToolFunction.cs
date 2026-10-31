using System.Runtime.Serialization;

namespace CapriDesktop;

[DataContract]
internal sealed class ToolFunction
{
	[DataMember(Name = "name")]
	public string Name;

	[DataMember(Name = "arguments")]
	public ToolArguments Arguments;
}
