using System.Runtime.Serialization;

namespace CapriDesktop;

[DataContract]
internal sealed class CompletionRequest
{
	[DataMember(Name = "model")]
	public string Model = "capri";

	[DataMember(Name = "messages")]
	public ChatMessage[] Messages;

	[DataMember(Name = "max_tokens")]
	public int Limit = 90;

	[DataMember(Name = "temperature")]
	public double Temperature = 0.85;

	[DataMember(Name = "stream")]
	public bool Stream;
}
