using Sandbox.Car;

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

		public override void Apply( GameObject body, SkinnedModelRenderer renderer, TuningEntry entry )
		{
			var c = body.Components.Get<CarController>( FindMode.InAncestors );

			if ( !Radius.HasValue || Radius <= 0 )
				Radius = c.WheelRadius;

			if ( !Width.HasValue || Width <= 0 )
				Width = c.WheelWidth;

			Vector3 size = renderer.Model.Bounds.Size;

			body.WorldScale = new( Width.Value / size.x, Radius.Value / (size.y / 2), Radius.Value / (size.z / 2) );

			var wheel = body.Components.Get<WheelCollider>( FindMode.InParent );

			wheel.Radius = Radius.Value;
			wheel.Width = Width.Value;
			wheel.ApplyVisual( renderer );
		}
		public new WheelTuning CarTuning { get; set; }
		public float? Radius { get; set; }
		public float? Width { get; set; }

		public override string GetSerialized()
		{
			return $"{base.GetSerialized()}:{Radius}:{Width}";
		}
	}
	[BitFlags, Category( "Body Slots" )] public override Slots SlotsOver { get => Slots.Wheel; }
	[BitFlags, Category( "Tuning Setup" )] public override BodyGroups HideBody { get => BodyGroups.FL | BodyGroups.FR | BodyGroups.RR | BodyGroups.RL; }
	[Category( "Display Information" )] public override CarTuningCategory Category { get => CarTuningCategory.Wheel; }

	[Category( "User Customization" )] bool AllowRadiusSelect { get; set; } = false;
	[Category( "User Customization" )] bool AllowWidthSelect { get; set; } = false;

}
