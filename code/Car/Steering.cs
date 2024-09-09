using System;


public sealed class Steering : Component
{
	[Property] public List<GameObject> Wheels { get; set; }
	[Property] public float MaxSteeringAngle { get; set; } = 35f;
	[Property] public float SteeringSmoothness { get; set; } = 10f;
	[Property] public Angles Offset { get; set; }
	[Property] public Rigidbody Rigidbody { get; set; }

	protected override void OnFixedUpdate()
	{
		if ( Scene.IsEditor )
			return;

		if ( IsProxy )
			return;

		foreach ( var wheel in Wheels )
		{
			//float corr = (float)((360 / (Math.PI * 2)) * Math.Acos( Transform.Rotation.Right.Dot( Rigidbody.Velocity.Normal ) ) - 90f);

			var targetRotation = Rotation.FromYaw( MaxSteeringAngle * Input.AnalogMove.y ) * Rotation.From( Offset );
			wheel.Transform.LocalRotation = Rotation.Lerp( wheel.Transform.LocalRotation, targetRotation, Time.Delta * SteeringSmoothness );
		}
	}
}
