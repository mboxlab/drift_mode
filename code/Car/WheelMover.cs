
using Sandbox.Car;
public sealed class WheelMover : Component, ICarDresserEvent
{
	[Property] public WheelCollider Wheel { get; set; }
	[Property] public bool ReverseRotation { get; set; }
	[Property] public float Speed { get; set; } = MathF.PI;

	private Rigidbody _rigidbody;
	private Rotation VelocityRotation;
	private float AxleAngle;
	private Part TuningPart;
	protected override void OnEnabled()
	{
		_rigidbody = Components.Get<Rigidbody>( FindMode.InAncestors );
		VelocityRotation = LocalRotation;
	}
	void ICarDresserEvent.OnLoad( List<Part> parts )
	{
		Part wheelPart = parts.Find( part => part.Name == "Wheels" );
		TuningPart = wheelPart;
		ModelScale( Wheel.Radius );
		Log.Info( TuningPart.Value );
		Wheel.OnRadiusChanged += ModelScale;
	}

	void ICarDresserEvent.OnSave( Part part )
	{
		if ( part.Name != "Wheels" )
			return;
		ModelScale( Wheel.Radius );
	}

	private void ModelScale( float wheelRadius )
	{
		TuningPart.Value = wheelRadius;
		var scale = wheelRadius / (TuningPart.Current.RenderBounds.Maxs.z);
		LocalScale = new Vector3( scale, LocalScale.y, scale );
	}

	protected override void OnFixedUpdate()
	{
		AxleAngle = Wheel.AngularVelocity.RadianToDegree() * Time.Delta;
		if ( IsProxy )
			return;

		WorldPosition = Wheel.GetCenter();

		VelocityRotation *= Rotation.From( AxleAngle * (ReverseRotation ? -1f : 1f), 0, 0 );

		LocalRotation = Rotation.FromYaw( Wheel.SteerAngle ) * VelocityRotation;

	}
}
