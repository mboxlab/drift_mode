using System.Text.Json.Serialization;

namespace Sandbox.Tuning;

public class WheelTuningOption : TuningOption, IJsonConvert
{

	[JsonInclude, Property] public override string Name { get; set; } = "Wheels";
	[JsonInclude, Property] public override List<CarTuning> Options { get; set; } = new() { new() { Name = "Radius" }, new() { Name = "Width" } };

}
