using System.Collections.Generic;
using System.Runtime.Serialization;

namespace CapriDesktop;

[DataContract]
internal sealed class VoicePage
{
	[DataMember(Name = "voices")]
	public List<VoiceEntry> Voices;

	[DataMember(Name = "has_more")]
	public bool More;

	[DataMember(Name = "next_page_token")]
	public string Next;
}
