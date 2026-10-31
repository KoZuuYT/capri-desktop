using System.Runtime.Serialization;

namespace CapriDesktop;

[DataContract]
internal sealed class ClaudeCodeAuth
{
	[DataMember(Name = "loggedIn")]
	public bool LoggedIn;
}
