namespace DM.Vehicle;
public abstract class WheelAPI : Component
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
	[Property] public float Load { get; internal set; }

	/// <summary>
	///     True if wheel touching ground.
	/// </summary>
	public abstract bool IsGrounded { get; set; }

	/// <summary>
	/// Current camber value.
	/// </summary>
	[Property] public float Camber { get; set; }


	// FRICTION
	[Property] public FrictionPreset FrictionPreset { get; set; }

	/// <summary>
	/// Distance as a percentage of the max spring length. Value of 1 means that the friction force will
	/// be applied 1 max spring length above the contact point, and value of 0 means that it will be applied at the
	/// ground level. Value can be >1.
	/// Can be used instead of the anti-roll bar to prevent the vehicle from tipping over in corners
	/// and can be useful in low framerate applications where anti-roll bar might induce jitter.
	/// </summary>
	public float ForceApplicationPointDistance { get; set; }


	// LONGITUDINAL FRICTION
	public float LongitudinalSlip { get; }
	public float LongitudinalSpeed { get; }
	[Property]
	public virtual bool IsSkiddingLongitudinally
	{
		get { return NormalizedLongitudinalSlip > 0.35f; }
	}

	[Property]
	public virtual float NormalizedLongitudinalSlip
	{
		get
		{
			float lngSlip = LongitudinalSlip;
			float absLngSlip = lngSlip < 0f ? -lngSlip : lngSlip;
			return absLngSlip < 0f ? 0f : absLngSlip > 1f ? 1f : absLngSlip;
		}
	}


	// LATERAL FRICTION
	public float LateralSlip { get; }
	public float LateralSpeed { get; }
	[Property]
	public virtual bool IsSkiddingLaterally
	{
		get { return NormalizedLateralSlip > 0.35f; }
	}

	[Property]
	public virtual float NormalizedLateralSlip
	{
		get
		{
			float latSlip = LateralSlip;
			float absLatSlip = latSlip < 0f ? -latSlip : latSlip;
			return absLatSlip < 0f ? 0f : absLatSlip > 1f ? 1f : absLatSlip;
		}
	}


	// FRICTION CIRCLE

	/// <summary>
	/// Higher values have more pronounced slip circle effect as the lateral friction will be
	/// decreased with smaller amounts of longitudinal slip (wheel spin).
	/// Realistic is ~1.5-2.
	/// </summary>
	public float FrictionCircleShape { get; set; } = 0.9f;


	/// <summary>
	/// Higher the number, higher the effect of longitudinal friction on lateral friction.
	/// If 1, when wheels are locked up or there is wheel spin it will be impossible to steer.
	/// If 0 doughnuts or power slides will be impossible.
	/// The 'accurate' value is 1 but might not be desirable for arcade games.
	/// </summary>
	[Range( 0, 1 )]
	public float FrictionCircleStrength { get; set; } = 1f;


	// COLLISION
	public Vector3 HitPoint { get; }
	public Vector3 HitNormal { get; }
	public Collider HitCollider { get; }


	// GENERAL
	public Rigidbody ParentRigidbody { get; }

	[Property] public bool AutoSimulate { get; set; } = true;

	public abstract void Step();
}


