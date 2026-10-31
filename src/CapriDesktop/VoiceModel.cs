using System.Runtime.Serialization;

namespace CapriDesktop;

[DataContract]
internal sealed class VoiceModel
{
	[DataMember(Name = "model_id")]
	public string Id;

	[DataMember(Name = "name")]
	public string Name;

	[DataMember(Name = "can_do_text_to_speech")]
	public bool Speaks;
}
