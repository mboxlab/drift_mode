using System;
namespace DM.Car;
public sealed class Steering : Component
{
	[Property] public List<GameObject> Wheels { get; set; }
	[Property] public List<Wheel> WheelsComponent { get; set; }
	[Property] public float MaxSteeringAngle { get; set; } = 35f;
	[Property] public float MinSteeringAngle { get; set; } = 20f;
	[Property] public float SteeringSmoothness { get; set; } = 10f;
	[Property] public Angles Offset { get; set; }
	[Property] public Rigidbody Rigidbody { get; set; }

	protected override void OnStart()
	{
		base.OnStart();
		WheelsComponent = new();
		foreach ( var item in Wheels )
		{
			WheelsComponent.Add( item.Components.Get<Wheel>() );
		}
	}
	protected override void OnFixedUpdate()
	{
		if ( Scene.IsEditor )
			return;

		if ( IsProxy )
			return;

		foreach ( Wheel wheel in WheelsComponent )
		{
			//float corr = (float)((360 / (Math.PI * 2)) * Math.Acos( Transform.Rotation.Right.Dot( Rigidbody.Velocity.Normal ) ) - 90f);

			float corr = Math.Max( Rigidbody.Velocity.Length * 0.001f, 1f );
			float steerAngle = MathX.Lerp( MaxSteeringAngle, MinSteeringAngle, corr * 0.04f ) * Input.AnalogMove.y;
			var targetRotation = Rotation.FromYaw( Math.Clamp( steerAngle, -MaxSteeringAngle, MaxSteeringAngle ) ) * Rotation.From( Offset );
			wheel.SteerAngle = wheel.SteerAngle.LerpDegreesTo( steerAngle, Time.Delta * SteeringSmoothness ); // Rotation.Slerp( wheel.Transform.LocalRotation, targetRotation, Time.Delta * SteeringSmoothness );
																											  //wheel.SteerAngle = MathX.Lerp( wheel.SteerAngle, MaxSteeringAngle * Input.AnalogMove.y, Time.Delta * SteeringSmoothness );
		}
	}
}
