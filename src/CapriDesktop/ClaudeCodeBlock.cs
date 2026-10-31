using System.Runtime.Serialization;

namespace CapriDesktop;

[DataContract]
internal sealed class ClaudeCodeBlock
{
	[DataMember(Name = "type")]
	public string Type;

	[DataMember(Name = "text")]
	public string Text;
}
