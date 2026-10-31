using System.Runtime.Serialization;

namespace CapriDesktop;

[DataContract]
internal sealed class PullLine
{
	[DataMember(Name = "status")]
	public string Status;

	[DataMember(Name = "total")]
	public long Total;

	[DataMember(Name = "completed")]
	public long Completed;

	[DataMember(Name = "error")]
	public string Error;
}
