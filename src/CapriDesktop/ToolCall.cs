using System.Runtime.Serialization;

namespace CapriDesktop;

[DataContract]
internal sealed class ToolCall
{
	[DataMember(Name = "function")]
	public ToolFunction Function;
}
