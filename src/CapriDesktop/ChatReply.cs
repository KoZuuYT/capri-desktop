using System.Runtime.Serialization;

namespace CapriDesktop;

[DataContract]
internal sealed class ChatReply
{
	[DataMember(Name = "message")]
	public ChatMessage Message;

	[DataMember(Name = "error")]
	public string Error;

	[DataMember(Name = "done_reason")]
	public string Stopped;
}
