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
			Radius = AllowRadiusSelect ? float.Parse( data[2] ) : RadiusDefault,
			Width = AllowWidthSelect ? float.Parse( data[3] ) : WidthDefault,
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
			Radius = tuning.RadiusDefault;
			Width = tuning.WidthDefault;
		}

		public override void Apply( GameObject body, SkinnedModelRenderer renderer, TuningEntry entry )
		{
			Vector3 size = renderer.Model.Bounds.Size;

			body.WorldScale = new( Width / size.x, Radius / (size.y / 2), Radius / (size.z / 2) );
			var wheel = body.Components.Get<WheelCollider>( FindMode.InParent );
			wheel.Radius = Radius;
			wheel.Width = Width;
			wheel.ApplyVisual( renderer );
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

	[Category( "User Customization" )] bool AllowRadiusSelect { get; set; } = false;
	[Category( "User Customization" )] bool AllowWidthSelect { get; set; } = false;

	[Category( "User Customization" )]
	public virtual float RadiusDefault { get; set; } = 15f;


	[Category( "User Customization" )]
	public virtual float WidthDefault { get; set; } = 8f;

}
