using System.Runtime.Serialization;

namespace CapriDesktop;

[DataContract]
internal sealed class PullRequest
{
	[DataMember(Name = "model")]
	public string Model;

	[DataMember(Name = "stream")]
	public bool Stream = true;
}
