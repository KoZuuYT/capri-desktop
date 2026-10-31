using System.Runtime.Serialization;

namespace CapriDesktop;

[DataContract]
internal sealed class Release
{
	[DataMember(Name = "tag_name")]
	public string Tag;

	[DataMember(Name = "draft")]
	public bool Draft;

	[DataMember(Name = "prerelease")]
	public bool Prerelease;

	[DataMember(Name = "assets")]
	public ReleaseAsset[] Assets;
}
