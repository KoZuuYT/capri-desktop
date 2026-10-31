using System.Runtime.Serialization;

namespace CapriDesktop;

[DataContract]
internal sealed class ReleaseAsset
{
	[DataMember(Name = "id")]
	public long Id;

	[DataMember(Name = "name")]
	public string Name;

	[DataMember(Name = "browser_download_url")]
	public string Url;

	[DataMember(Name = "size")]
	public long Size;
}
