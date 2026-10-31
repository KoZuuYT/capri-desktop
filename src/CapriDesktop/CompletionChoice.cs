using System.Runtime.Serialization;

namespace CapriDesktop;

[DataContract]
internal sealed class CompletionChoice
{
	[DataMember(Name = "message")]
	public CompletionMessage Message;

	[DataMember(Name = "finish_reason")]
	public string Stopped;
}
