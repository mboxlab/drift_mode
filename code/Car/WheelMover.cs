using System;
using Sandbox.Car;
public sealed class WheelMover : Component
{
	[Property] public WheelCollider Wheel { get; set; }
	[Property] public bool ReverseRotation { get; set; }
	[Property] public float Speed { get; set; } = MathF.PI;

	private Rigidbody _rigidbody;
	private Rotation VelocityRotation;
	private float AxleAngle;
	protected override void OnEnabled()
	{
		_rigidbody = Components.Get<Rigidbody>( FindMode.InAncestors );
		VelocityRotation = LocalRotation;
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
