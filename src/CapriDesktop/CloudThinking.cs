using System.Runtime.Serialization;

namespace CapriDesktop;

[DataContract]
internal sealed class CloudThinking
{
	[DataMember(Name = "type")]
	public string Type = "disabled";
}
