using System;
using DM.Car;
using static Sandbox.PhysicsContact;

public sealed class WheelMover : Component
{
	[Property] public Wheel Target { get; set; }
	[Property] public bool ReverseRotation { get; set; }

	private Rigidbody _rigidbody;

	protected override void OnEnabled()
	{
		_rigidbody = Components.GetInAncestors<Rigidbody>();
		Target ??= Components.GetInAncestors<Wheel>();
	}

	protected override void OnFixedUpdate()
	{
		if ( IsProxy )
			return;

		var groundVel = _rigidbody.GetVelocityAtPoint( Transform.Position ).WithFriction( 0.11f );
		Transform.LocalPosition = Transform.LocalPosition.WithZ( Target.GetLocalCenter().z );
		Transform.LocalRotation *= Rotation.From( groundVel.Length * Time.Delta * (ReverseRotation ? -1f : 1f) * MathF.PI, 0, 0 );
	}
}
