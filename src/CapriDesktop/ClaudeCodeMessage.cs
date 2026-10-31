using System.Runtime.Serialization;

namespace CapriDesktop;

[DataContract]
internal sealed class ClaudeCodeMessage
{
	[DataMember(Name = "content")]
	public ClaudeCodeBlock[] Content;
}
