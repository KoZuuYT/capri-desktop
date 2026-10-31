using System.Runtime.Serialization;

namespace CapriDesktop;

[DataContract]
internal sealed class CloudTemplate
{
	[DataMember(Name = "enable_thinking")]
	public bool Think;
}
