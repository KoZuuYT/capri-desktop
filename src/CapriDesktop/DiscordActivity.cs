using System.Runtime.Serialization;

namespace CapriDesktop;

[DataContract]
internal sealed class DiscordActivity
{
	[DataMember(Name = "details")]
	public string Details;

	[DataMember(Name = "timestamps")]
	public DiscordTimes Times;

	[DataMember(Name = "assets")]
	public DiscordAssets Assets;

	[DataMember(Name = "buttons")]
	public DiscordButton[] Buttons;
}
