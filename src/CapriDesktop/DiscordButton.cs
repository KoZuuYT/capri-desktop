using System.Runtime.Serialization;

namespace CapriDesktop;

[DataContract]
internal sealed class DiscordButton
{
	[DataMember(Name = "label")]
	public string Label;

	[DataMember(Name = "url")]
	public string Url;
}
