using System.Runtime.Serialization;

namespace CapriDesktop;

[DataContract]
internal sealed class AgentAddress
{
	[DataMember(Name = "signed_url")]
	public string Url;
}
