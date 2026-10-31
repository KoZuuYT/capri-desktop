using System.Runtime.Serialization;

namespace CapriDesktop;

[DataContract]
internal sealed class ClaudeCodeLine
{
	[DataMember(Name = "type")]
	public string Type;

	[DataMember(Name = "subtype")]
	public string Subtype;

	[DataMember(Name = "is_error")]
	public bool Failed;

	[DataMember(Name = "result")]
	public string Result;

	[DataMember(Name = "message")]
	public ClaudeCodeMessage Message;
}
