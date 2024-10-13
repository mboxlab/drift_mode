
namespace Sandbox.Tuning.Tunings;

[GameResource( "Wheel tuning Definition", "wheel", "Describes the car tuning element and indirectly indicates which other elements it can be combined with.", Icon = "trip_origin", IconBgColor = "gray", IconFgColor = "black" )]
public class WheelTuning : CarTuning
{
	[BitFlags, Category( "Body Slots" )] public override Slots SlotsOver { get => Slots.Wheel; }
	[BitFlags, Category( "Tuning Setup" )] public override BodyGroups HideBody { get => BodyGroups.FL | BodyGroups.FR | BodyGroups.RR | BodyGroups.RL; }
	[Category( "Display Information" )] public override CarTuningCategory Category { get => CarTuningCategory.Wheel; }

	[Category( "User Customization" )]
	public float Radius { get; set; } = 1f;

	[Category( "User Customization" )]
	public float Width { get; set; } = 1f;
}
