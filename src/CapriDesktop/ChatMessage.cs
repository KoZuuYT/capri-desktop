using System.Runtime.Serialization;

namespace CapriDesktop;

[DataContract]
internal sealed class ChatMessage
{
	[DataMember(Name = "role")]
	public string Role;

	[DataMember(Name = "content")]
	public string Content;

	[DataMember(Name = "tool_calls", EmitDefaultValue = false)]
	public ToolCall[] Calls;

	[DataMember(Name = "images", EmitDefaultValue = false)]
	public string[] Images;

	[DataMember(Name = "thinking", EmitDefaultValue = false)]
	public string Thinking;

	internal ChatMessage(string role, string content)
	{
		Role = role;
		Content = content;
	}
}
