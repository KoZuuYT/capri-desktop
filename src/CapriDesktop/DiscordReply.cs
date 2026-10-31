using System.Runtime.Serialization;

namespace CapriDesktop;

[DataContract]
internal sealed class DiscordReply
{
	[DataMember(Name = "cmd")]
	public string Cmd;

	[DataMember(Name = "evt")]
	public string Evt;

	[DataMember(Name = "data")]
	public DiscordData Data;
}
