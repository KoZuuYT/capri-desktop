using System.Runtime.Serialization;

namespace CapriDesktop;

[DataContract]
internal sealed class CompletionReply
{
	[DataMember(Name = "choices")]
	public CompletionChoice[] Choices;
}
