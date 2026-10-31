using System.Runtime.Serialization;

namespace CapriDesktop;

[DataContract]
internal sealed class VoiceEntry
{
	[DataMember(Name = "voice_id")]
	public string Id;

	[DataMember(Name = "name")]
	public string Name;

	[DataMember(Name = "category")]
	public string Category;
}
