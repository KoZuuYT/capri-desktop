using System.Runtime.Serialization;

namespace CapriDesktop;

[DataContract]
internal sealed class ChatOptions
{
	[DataMember(Name = "num_predict")]
	public int Limit = 90;

	[DataMember(Name = "temperature")]
	public double Temperature = 0.85;

	[DataMember(Name = "num_ctx")]
	public int Context = 4096;

	[DataMember(Name = "num_gpu", EmitDefaultValue = false)]
	public int? Gpu;
}
