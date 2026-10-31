using System.Runtime.Serialization;

namespace CapriDesktop;

[DataContract]
internal sealed class CloudModel
{
	[DataMember(Name = "id")]
	public string Id;

	[DataMember(Name = "created")]
	public long Created;

	[DataMember(Name = "created_at")]
	public string CreatedAt;
}
