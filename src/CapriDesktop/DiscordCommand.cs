using System.Runtime.Serialization;

namespace CapriDesktop;

[DataContract]
internal sealed class DiscordCommand
{
	[DataMember(Name = "cmd")]
	public string Cmd = "SET_ACTIVITY";

	[DataMember(Name = "args")]
	public DiscordArgs Args;

	[DataMember(Name = "nonce")]
	public string Nonce;
}
