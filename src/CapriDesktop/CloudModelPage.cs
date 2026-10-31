using System.Runtime.Serialization;

namespace CapriDesktop;

[DataContract]
internal sealed class CloudModelPage
{
	[DataMember(Name = "data")]
	public CloudModel[] Models;
}
