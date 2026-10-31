using System.Runtime.Serialization;

namespace CapriDesktop;

[DataContract]
internal sealed class CompletionCall
{
	[DataMember(Name = "function")]
	public CompletionFunction Function;
}
