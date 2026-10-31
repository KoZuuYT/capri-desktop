using System.Runtime.Serialization;

namespace CapriDesktop;

[DataContract]
internal sealed class ClaudeRequest
{
	[DataMember(Name = "model")]
	public string Model;

	[DataMember(Name = "max_tokens")]
	public int Limit = 16000;

	[DataMember(Name = "system")]
	public string System;

	[DataMember(Name = "messages")]
	public ChatMessage[] Messages;

	[DataMember(Name = "output_config", EmitDefaultValue = false)]
	public ClaudeOutput Output;

	[DataMember(Name = "fallbacks", EmitDefaultValue = false)]
	public string Fallbacks;
}
