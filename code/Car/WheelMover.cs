using System;
using Sandbox.Car;
public sealed class WheelMover : Component
{
	[Property] public WheelCollider Wheel { get; set; }
	[Property] public bool ReverseRotation { get; set; }
	[Property] public float Speed { get; set; } = MathF.PI;

	private Rigidbody _rigidbody;

	protected override void OnEnabled()
	{
		_rigidbody = Components.GetInAncestors<Rigidbody>();
	}

	protected override void OnFixedUpdate()
	{
		if ( IsProxy )
			return;

		var groundVel = _rigidbody.Velocity;
		var SideFrictionSpeed = groundVel.Dot( Wheel.Transform.Rotation.Forward );

		Transform.Position = Wheel.GetCenter();
		Transform.LocalRotation *= Rotation.From( SideFrictionSpeed * Time.Delta * (ReverseRotation ? -1f : 1f) * Speed, 0, 0 );
	}
}
