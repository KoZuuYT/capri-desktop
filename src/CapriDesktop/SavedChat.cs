using System.Runtime.Serialization;

namespace CapriDesktop;

[DataContract]
internal sealed class SavedChat
{
	[DataMember(Name = "at")]
	public string At;

	[DataMember(Name = "messages")]
	public ChatMessage[] Messages;

	[DataMember(Name = "seen", EmitDefaultValue = false)]
	public Sighting[] Seen;
}
