using System;
namespace DM.Car;

[Category( "Vehicles" )]
public sealed class Steering : Component
{
	[Property] public List<Wheel> WheelsComponent { get; set; }
	[Property] public float MaxSteeringAngle { get; set; } = 35f;
	[Property] public float MinSteeringAngle { get; set; } = 20f;
	[Property] public float SteeringSmoothness { get; set; } = 10f;
	[Property] public Angles Offset { get; set; }
	[Property] public Rigidbody Rigidbody { get; set; }
	private float SteerAngle { get; set; }

	protected override void OnFixedUpdate()
	{
		if ( Scene.IsEditor )
			return;

		if ( IsProxy )
			return;
		var speed = Rigidbody.Velocity.Length;
		float clamp = Math.Max( speed * 0.001f, 1f );
		float corr = speed > 11f ? (float)((360 / (Math.PI * 2)) * Math.Acos( Transform.Rotation.Right.Dot( Rigidbody.Velocity.Normal ) ) - 90f) : 0;

		SteerAngle = MathX.Lerp( MaxSteeringAngle, MinSteeringAngle, clamp * 0.04f ) * Input.AnalogMove.y;

		foreach ( Wheel wheel in WheelsComponent )
			wheel.SteerAngle = wheel.SteerAngle.LerpDegreesTo( Math.Clamp( SteerAngle + corr / 2, -MaxSteeringAngle, MaxSteeringAngle ), Time.Delta * SteeringSmoothness );

	}
}
