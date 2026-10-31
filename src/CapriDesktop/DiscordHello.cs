using System.Runtime.Serialization;

namespace CapriDesktop;

[DataContract]
internal sealed class DiscordHello
{
	[DataMember(Name = "v")]
	public int V = 1;

	[DataMember(Name = "client_id")]
	public string ClientId;
}
