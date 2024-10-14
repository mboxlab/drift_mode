
using System.Text.Json;
using System.Text.Json.Serialization;
using static Sandbox.Tuning.TuningContainer;

namespace Sandbox.Tuning.Tunings;

[GameResource( "Wheel tuning Definition", "wheel", "Describes the car tuning element and indirectly indicates which other elements it can be combined with.", Icon = "trip_origin", IconBgColor = "gray", IconFgColor = "black" )]
public class WheelTuning : CarTuning
{
	public class WheelTuningEntry : TuningEntry
	{
		public WheelTuningEntry( CarTuning tuning ) : base( tuning )
		{
		}

		public override string GetSerialized()
		{
			return JsonSerializer.Serialize( new WheelEntry()
			{
				Id = CarTuning.ResourceId,
				Tint = Tint,
				Radius = ((WheelTuning)CarTuning).Radius,
				Width = ((WheelTuning)CarTuning).Width,
			} );
		}
	}

	/// <summary>
	/// Used for serialization
	/// </summary>
	public class WheelEntry : Entry
	{
		[JsonPropertyName( "radius" )]
		public float Radius { get; set; }

		[JsonPropertyName( "width" )]
		public float Width { get; set; }
	}
	[BitFlags, Category( "Body Slots" )] public override Slots SlotsOver { get => Slots.Wheel; }
	[BitFlags, Category( "Tuning Setup" )] public override BodyGroups HideBody { get => BodyGroups.FL | BodyGroups.FR | BodyGroups.RR | BodyGroups.RL; }
	[Category( "Display Information" )] public override CarTuningCategory Category { get => CarTuningCategory.Wheel; }

	[Category( "User Customization" )]
	public float Radius { get; set; } = 1f;

	[Category( "User Customization" )]
	public float Width { get; set; } = 1f;
}
