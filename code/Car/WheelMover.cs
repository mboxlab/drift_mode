
using Sandbox.Car;
using Sandbox.Utils;
public sealed class WheelMover : Component, ICarSaverEvent, ICarDresserEvent
{
	[Property] public WheelCollider Wheel { get; set; }
	[Property] public bool ReverseRotation { get; set; }
	[Property] public float Speed { get; set; } = MathF.PI;

	private Rotation VelocityRotation;
	private float AxleAngle;
	protected override void OnEnabled()
	{
		VelocityRotation = LocalRotation;
	}
	void ICarSaverEvent.OnLoad()
	{

		ModelScale( Wheel.Radius );
	}
	void ICarDresserEvent.OnPartChanged( Part part )
	{
		Log.Info( "awd" );
		ModelScale( Wheel.Radius );
	}

	private void ModelScale( float wheelRadius )
	{
		var scale = wheelRadius / (GetComponent<ModelRenderer>().Model.RenderBounds.Maxs.z);
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
