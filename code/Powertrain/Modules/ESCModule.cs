namespace Sandbox.Powertrain.Modules;

/// <summary>
///     Electronic Stability Control (ESC) module.
///     Applies braking on individual wheels to try and stabilize the vehicle when the vehicle velocity and vehicle
///     direction do not match.
/// </summary>

[Title( "ESC Module" )]
public class ESCModule : BaseModule
{
	/// <summary>
	///     Intensity of stability control.
	/// </summary>
	[Property, Range( 0, 1 )] public float Intensity { get; set; } = 0.4f;

	/// <summary>
	///     ESC will not work below this speed.
	///     Setting this to a too low value might cause vehicle to be hard to steer at very low speeds.
	/// </summary>
	[Property] public float LowerSpeedThreshold { get; set; } = 4f;

	protected override void OnFixedUpdate()
	{
		// Prevent ESC from working in reverse and at low speeds
		if ( CarController.LocalVelocity.x < LowerSpeedThreshold )
			return;

		float angle = CarController.Rigidbody.Velocity.SignedAngle( CarController.WorldRotation.Forward, CarController.WorldRotation.Up );
		angle -= CarController.Input.Steering * 0.5f;
		float absAngle = angle < 0 ? -angle : angle;

		if ( CarController.Powertrain.Engine.RevLimiterActive || absAngle < 2f )
			return;

		foreach ( WheelComponent wheelComponent in CarController.Powertrain.Wheels )
		{
			if ( !wheelComponent.Wheel.IsGrounded )
				continue;

			float additionalBrakeTorque = -angle * Math.Sign( wheelComponent.Wheel.WorldPosition.x ) * 50f * Intensity;
			wheelComponent.AddBrakeTorque( additionalBrakeTorque );
		}
	}
}
