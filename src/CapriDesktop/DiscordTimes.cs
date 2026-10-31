using System.Runtime.Serialization;

namespace CapriDesktop;

[DataContract]
internal sealed class DiscordTimes
{
	[DataMember(Name = "start")]
	public long Start;
}
