using System.Runtime.Serialization;

namespace CapriDesktop;

[DataContract]
internal sealed class DiscordData
{
	[DataMember(Name = "code")]
	public int Code;

	[DataMember(Name = "message")]
	public string Message;
}
