using System.Runtime.Serialization;

namespace CapriDesktop;

[DataContract]
internal sealed class ClaudeReply
{
	[DataMember(Name = "content")]
	public ClaudeBlock[] Content;

	[DataMember(Name = "stop_reason")]
	public string StopReason;

	internal string Raw;
}
