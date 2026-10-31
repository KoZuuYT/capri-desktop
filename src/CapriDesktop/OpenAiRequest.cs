using System.Runtime.Serialization;

namespace CapriDesktop;

[DataContract]
internal sealed class OpenAiRequest
{
	[DataMember(Name = "model")]
	public string Model;

	[DataMember(Name = "messages")]
	public ChatMessage[] Messages;

	[DataMember(Name = "max_completion_tokens", EmitDefaultValue = false)]
	public int? Limit;

	[DataMember(Name = "max_tokens", EmitDefaultValue = false)]
	public int? Tokens;

	[DataMember(Name = "reasoning_effort", EmitDefaultValue = false)]
	public string Effort;

	[DataMember(Name = "thinking", EmitDefaultValue = false)]
	public CloudThinking Thinking;

	[DataMember(Name = "chat_template_kwargs", EmitDefaultValue = false)]
	public CloudTemplate Template;
}
