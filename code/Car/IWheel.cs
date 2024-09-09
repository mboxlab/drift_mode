using System;
using DM.Vehicle;

namespace DM.Car;

internal interface IWheel
{

	// INPUTS

	/// <summary>
	///     Motor torque applied to the wheel in Nm.
	///     Can be positive or negative.
	/// </summary>
	public float MotorTorque { get; set; }
	/// <summary>
	///     Brake torque applied to the wheel in Nm.
	///     Must be positive.
	/// </summary>
	public float BrakeTorque { get; set; }

	/// <summary>
	///     The amount of torque returned by the wheel.
	///     Under no-slip conditions this will be equal to the torque that was input.
	///     When there is wheel spin, the value will be less than the input torque.
	/// </summary>
	public float CounterTorque { get; }


	/// <summary>
	///     Constant torque acting similar to brake torque.
	///     Imitates rolling resistance.
	/// </summary>
	[Range( 0, 500 )]
	public float RollingResistanceTorque { get; set; }

	/// <summary>
	///     Current steer angle of the wheel, in deg.
	/// </summary>
	public float SteerAngle { get; set; }

	/// <summary>
	///     Tire load in Nm.
	/// </summary>
	public float Load { get; }

	/// <summary>
	///     True if wheel touching ground.
	/// </summary>
	public abstract bool IsGrounded { get; }

	// Friction

	/// <summary>
	/// Forward (longitudinal) friction info.
	/// </summary>
	public FrictionPreset ForwardFriction { get; set; }

	/// <summary>
	/// Side (lateral) friction info.
	/// </summary>
	public FrictionPreset SideFriction { get; set; }

	/// <summary>
	/// Instance of the spring.
	/// </summary>
	public Spring Spring { get; set; }

	/// <summary>
	/// Instance of the damper.
	/// </summary>
	public Damper Damper { get; set; }

	/// <summary>
	///     Rigidbody to which the forces will be applied.
	/// </summary>
	public Rigidbody Rigidbody { get; set; }

	/// <summary>
	///     Total radius of the tire in [m].
	/// </summary>
	public float Radius { get; set; }

	/// <summary>
	///     Width of the tyre.
	/// </summary>
	public float Width { get; set; }

	/// <summary>
	/// Total inertia of the wheel and any attached components.
	/// </summary>

	public float Inertia { get; }

	/// <summary>
	///     Mass of the wheel. Inertia is calculated from this.
	/// </summary>
	public float Mass { get; set; }
}
