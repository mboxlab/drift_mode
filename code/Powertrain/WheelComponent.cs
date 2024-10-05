
using Sandbox.Car;

namespace Sandbox.Powertrain;

public partial class WheelComponent : PowertrainComponent
{
	protected override void OnAwake()
	{
		base.OnAwake();
		Name ??= Wheel.ToString();
	}
	[Property] public WheelCollider Wheel { get; set; }

	protected override void OnStart()
	{
		base.OnStart();
		_initialRollingResistance = Wheel.RollingResistanceTorque;
		_initialWheelInertia = Wheel.Inertia;

	}
	private float _initialRollingResistance;
	private float _initialWheelInertia;


	/// <summary>
	///     Adds brake torque to the wheel on top of the existing torque. Value is clamped to max brake torque.
	/// </summary>
	/// <param name="torque">Torque in Nm that will be applied to the wheel to slow it down.</param>
	/// <param name="isHandbrake">If true brakes.IsBraking flag will be set. This triggers brake lights.</param>
	public void AddBrakeTorque( float torque, bool isHandbrake = false )
	{
		float brakeTorque = Wheel.BrakeTorque;

		if ( torque > 0 )
		{
			brakeTorque += torque;
		}

		brakeTorque = Math.Max( brakeTorque, CarController.MaxBrakeTorque );

		if ( brakeTorque < 0 )
		{
			brakeTorque = 0;
		}

		Wheel.BrakeTorque = brakeTorque;
	}


	public override float QueryAngularVelocity( float angularVelocity, float dt )
	{
		InputAngularVelocity = OutputAngularVelocity = Wheel.AngularVelocity;

		return OutputAngularVelocity;
	}

	public override float QueryInertia()
	{
		// Calculate the base inertia of the wheel and scale it by the inverse of the dt.
		float dtScale = Math.Clamp( Time.Delta, 0.01f, 0.05f ) / 0.005f;
		float radius = Wheel.Radius.InchToMeter();
		return 0.5f * Wheel.Mass * radius * radius * dtScale;
	}

	public void ApplyRollingResistanceMultiplier( float multiplier )
	{

		Wheel.RollingResistanceTorque = _initialRollingResistance * multiplier;
	}
	public override float ForwardStep( float torque, float inertiaSum, float dt )
	{
		InputTorque = torque;
		InputInertia = inertiaSum;

		OutputTorque = InputTorque;
		OutputInertia = _initialWheelInertia + inertiaSum;
		Wheel.MotorTorque = OutputTorque;
		Wheel.Inertia = OutputInertia;

		Wheel.AutoSimulate = false;
		Wheel.Update();

		return Math.Abs( Wheel.CounterTorque );
	}
}
