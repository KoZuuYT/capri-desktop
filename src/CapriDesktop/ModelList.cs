using System.Runtime.Serialization;

namespace CapriDesktop;

[DataContract]
internal sealed class ModelList
{
	[DataMember(Name = "models")]
	public ModelInfo[] Models;
}
