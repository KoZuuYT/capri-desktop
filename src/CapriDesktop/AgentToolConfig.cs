using System.Runtime.Serialization;

namespace CapriDesktop;

[DataContract]
internal sealed class AgentToolConfig
{
	[DataMember(Name = "type")]
	public string Type;

	[DataMember(Name = "name")]
	public string Name;

	[DataMember(Name = "description")]
	public string Description;
}
