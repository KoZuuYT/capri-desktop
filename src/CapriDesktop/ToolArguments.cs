using System.Runtime.Serialization;

namespace CapriDesktop;

[DataContract]
internal sealed class ToolArguments
{
	[DataMember(Name = "name")]
	public string Name;

	[DataMember(Name = "site")]
	public string Site;

	[DataMember(Name = "search")]
	public string Search;

	[DataMember(Name = "action")]
	public string Action;

	[DataMember(Name = "focus")]
	public string Focus;

	[DataMember(Name = "song")]
	public string Song;

	[DataMember(Name = "app")]
	public string App;

	[DataMember(Name = "kind")]
	public string Kind;

	[DataMember(Name = "task")]
	public string Task;

	internal string Asked;

	[DataMember(Name = "target")]
	public string Target;

	[DataMember(Name = "text")]
	public string Text;

	[DataMember(Name = "keys")]
	public string Keys;

	[DataMember(Name = "button")]
	public string Button;

	[DataMember(Name = "enter")]
	public string Enter;

	[DataMember(Name = "direction")]
	public string Direction;

	[DataMember(Name = "seconds")]
	public string Seconds;

	[DataMember(Name = "minutes")]
	public string Minutes;

	[DataMember(Name = "level")]
	public string Level;

	[DataMember(Name = "screen")]
	public string Screen;

	[DataMember(Name = "window")]
	public string Window;

	[DataMember(Name = "coordinate")]
	public double[] Coordinate;

	[DataMember(Name = "start_coordinate")]
	public double[] From;

	[DataMember(Name = "region")]
	public double[] Region;

	[DataMember(Name = "scroll_direction")]
	public string Way;

	[DataMember(Name = "scroll_amount")]
	public double? Amount;

	[DataMember(Name = "duration")]
	public double? Duration;

	[DataMember(Name = "repeat")]
	public double? Repeat;

	[DataMember(Name = "hold")]
	public string Hold;

	[DataMember(Name = "press")]
	public string Press;

	[DataMember(Name = "times")]
	public string Times;

	internal ToolArguments Copy()
	{
		return (ToolArguments)MemberwiseClone();
	}
}
