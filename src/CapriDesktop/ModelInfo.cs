using System.Runtime.Serialization;

namespace CapriDesktop;

[DataContract]
internal sealed class ModelInfo
{
	[DataMember(Name = "name")]
	public string Name;
}
