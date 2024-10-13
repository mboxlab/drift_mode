
namespace Sandbox.Powertrain.Modules;

[Title( "ABS Module" )]
public class ABSModule : BaseModule
{
	/// <summary>
	///     Is ABS currently active?
	/// </summary>
	[Property, ReadOnly] public bool IsActive;


	/// <summary>
	///     ABS will not work below this speed.
	/// </summary>
	[Property] public float LowerSpeedThreshold = 1f;

	/// <summary>
	///     Longitudinal slip required for ABS to trigger. Larger value means less sensitive ABS.
	/// </summary>
	[Property, Range( 0, 1 )] public float SlipThreshold = 0.1f;



	public float BrakeTorqueModifier()
	{
		if ( !Active )
			return 1;

		IsActive = false;

		// Prevent ABS from working at low speeds
		if ( CarController.CurrentSpeed < LowerSpeedThreshold )
		{
			return 1f;
		}

		if ( CarController.Input.Brakes > 0 && !CarController.Powertrain.Engine.RevLimiterActive && CarController.Input.Handbrake < 0.1f )
		{
			for ( int index = 0; index < CarController.Powertrain.WheelCount; index++ )
			{
				WheelComponent wheelComponent = CarController.Powertrain.Wheels[index];
				if ( !wheelComponent.Wheel.IsGrounded )
				{
					continue;
				}

				float longSlip = wheelComponent.Wheel.ForwardSlip;
				if ( longSlip * Math.Sign( CarController.LocalVelocity.y ) > SlipThreshold )
				{
					IsActive = true;
					return 0.01f;
				}
			}
		}

		return 1f;
	}
}
