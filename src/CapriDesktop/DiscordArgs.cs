using System.Runtime.Serialization;

namespace CapriDesktop;

[DataContract]
internal sealed class DiscordArgs
{
	[DataMember(Name = "pid")]
	public int Pid;

	[DataMember(Name = "activity", EmitDefaultValue = false)]
	public DiscordActivity Activity;
}
