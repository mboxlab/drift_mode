using Sandbox.Car;
using static Sandbox.Tuning.TuningContainer;

namespace Sandbox.Tuning.Tunings;

[GameResource( "Wheel tuning Definition", "wheel", "Describes the car tuning element and indirectly indicates which other elements it can be combined with.", Icon = "trip_origin", IconBgColor = "gray", IconFgColor = "black" )]
public class WheelTuning : CarTuning
{
	public override WheelTuningEntry Parse( string[] data )
	{
		return new( this )
		{
			Tint = int.Parse( data[1] ),
			Radius = float.Parse( data[2] ),
			Width = float.Parse( data[3] ),
		}; ;
	}

	public override WheelTuningEntry GetEntry()
	{
		return new( this );
	}
	public class WheelTuningEntry : TuningEntry
	{
		public WheelTuningEntry( WheelTuning tuning ) : base( tuning )
		{
		}

		public override void Apply( GameObject body )
		{
			body.WorldScale = new Vector3( Width, Radius, Radius );
			var wheel = body.Components.Get<WheelCollider>( FindMode.InParent );
			wheel.Radius *= Radius;
			wheel.Width *= Width;
		}
		public new WheelTuning CarTuning { get; set; }
		public float Radius { get; set; } = 1f;
		public float Width { get; set; } = 1f;

		public override string GetSerialized()
		{
			return $"{base.GetSerialized()}:{Radius}:{Width}";
		}
	}
	[BitFlags, Category( "Body Slots" )] public override Slots SlotsOver { get => Slots.Wheel; }
	[BitFlags, Category( "Tuning Setup" )] public override BodyGroups HideBody { get => BodyGroups.FL | BodyGroups.FR | BodyGroups.RR | BodyGroups.RL; }
	[Category( "Display Information" )] public override CarTuningCategory Category { get => CarTuningCategory.Wheel; }
}
