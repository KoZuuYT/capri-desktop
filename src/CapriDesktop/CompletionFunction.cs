using System.Runtime.Serialization;

namespace CapriDesktop;

[DataContract]
internal sealed class CompletionFunction
{
	[DataMember(Name = "name")]
	public string Name;

	[DataMember(Name = "arguments")]
	public string Arguments;
}
