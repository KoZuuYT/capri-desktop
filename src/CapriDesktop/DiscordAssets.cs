using System.Runtime.Serialization;

namespace CapriDesktop;

[DataContract]
internal sealed class DiscordAssets
{
	[DataMember(Name = "large_image")]
	public string Image;

	[DataMember(Name = "large_text")]
	public string Text;
}
