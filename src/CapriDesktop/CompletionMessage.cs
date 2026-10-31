using System.Runtime.Serialization;

namespace CapriDesktop;

[DataContract]
internal sealed class CompletionMessage
{
	[DataMember(Name = "content")]
	public string Content;

	[DataMember(Name = "tool_calls")]
	public CompletionCall[] Calls;

	[DataMember(Name = "reasoning_content")]
	public string Thinking;

	[DataMember(Name = "reasoning")]
	public string Reasoning;
}
