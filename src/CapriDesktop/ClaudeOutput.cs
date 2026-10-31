using System.Runtime.Serialization;

namespace CapriDesktop;

[DataContract]
internal sealed class ClaudeOutput
{
	[DataMember(Name = "effort")]
	public string Effort = "low";
}
