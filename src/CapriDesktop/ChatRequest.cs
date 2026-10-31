using System.Runtime.Serialization;

namespace CapriDesktop;

[DataContract]
internal sealed class ChatRequest
{
	[DataMember(Name = "model")]
	public string Model;

	[DataMember(Name = "messages")]
	public ChatMessage[] Messages;

	[DataMember(Name = "stream")]
	public bool Stream;

	[DataMember(Name = "think", EmitDefaultValue = false)]
	public bool? Think;

	[DataMember(Name = "keep_alive")]
	public string KeepAlive = "15m";

	[DataMember(Name = "options", EmitDefaultValue = false)]
	public ChatOptions Options;
}
