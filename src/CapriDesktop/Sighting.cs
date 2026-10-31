using System.Runtime.Serialization;

namespace CapriDesktop;

[DataContract]
internal sealed class Sighting
{
	[DataMember(Name = "at")]
	public string At;

	[DataMember(Name = "saw")]
	public string Saw;

	[DataMember(Name = "said", EmitDefaultValue = false)]
	public string Said;

	[DataMember(Name = "asked", EmitDefaultValue = false)]
	public bool Asked;
}
