using System;
using DM.Ground;
using Sandbox.Car;

namespace DM.Car;

public partial class Wheel
{
	private FrictionPreset.PresetsEnum activePresetEnum = FrictionPreset.PresetsEnum.Asphalt;

	[Property, Sync] public float SteerAngle { get; set; }
	[Property] public WheelManager Manager { get; set; }
	[Property] public float MotorTorque { get; set; }
	[Property] public bool IsPower { get; set; }
	[Property] public float BrakeTorque { get; set; }

	/// <summary>
	/// The amount of torque returned by the wheel.
	/// Under no-slip conditions this will be equal to the torque that was input.
	/// When there is wheel spin, the value will be less than the input torque.
	/// </summary>
	[Property, ReadOnly] public float CounterTorque { get; private set; }

	/// <summary>
	/// Current angular velocity of the wheel in rad/s.
	/// </summary>
	[Property, ReadOnly, Sync] public float AngularVelocity { get; set; }
	public float PrevAngularVelocity { get; set; }

	[Property, ReadOnly]
	public float RPM
	{
		get { return AngularVelocity * 9.55f; }
	}


	[Description( "Constant torque acting similar to brake torque.\r\nImitates rolling resistance." )]
	[Property, Range( 0, 500 )] public float RollingResistanceTorque { get; set; }

	[Description( "Tire load in Nm." )]
	[Property, ReadOnly] public float Load { get; private set; }


	[Description( "The percentage this wheel is contributing to the total vehicle load bearing." )]
	[Property, ReadOnly] public float LoadContribution { get; set; } = 0.25f;


	[Description( "Maximum load the tire is rated for in [N]. \r\nUsed to calculate friction.Default value is adequate for most cars but\r\nlarger and heavier vehicles such as semi trucks will use higher values.\r\nA good rule of the thumb is that this value should be 2x the Load\r\nwhile vehicle is stationary." )]
	[Property] public float LoadRating { get; set; } = 5400;

	[Property, Group( "Friction" )]
	public FrictionPreset.PresetsEnum ActivePresetEnum
	{
		get => activePresetEnum;
		set
		{
			activePresetEnum = value;
			FrictionPreset.Apply( FrictionPreset.Presets[value] );
		}
	}
	[Property, Group( "Friction" )] public FrictionPreset FrictionPreset { get; set; } = FrictionPreset.Asphalt;

	[Property, Group( "Friction" )] public Friction ForwardFriction { get; set; } = new();
	[Property, Group( "Friction" )] public Friction SideFriction { get; set; } = new();
	[Property, Group( "Components" )] public Spring Spring { get; set; } = new();
	[Property, Group( "Components" )] public Damper Damper { get; set; } = new();
	[Property, Group( "Components" )] public Rigidbody Rigidbody { get; set; }
	[Property, Group( "Components" )] public GameObject Visual { get; set; }



	[Property]
	[Description( "Radius in Inches" )]
	public float Radius
	{
		get => _radius;
		set
		{
			_radius = value;
			CalcInertia();
		}
	}
	private float _radius { get; set; }

	[Property]
	[Description( "Mass in Kg" )]
	public float Mass
	{
		get => _mass;
		set
		{
			_mass = value;
			CalcInertia();
		}
	}

	[Property]
	[Description( "Width in Inches" )]
	public float Width { get; set; }

	private float _mass { get; set; }

	[Property, ReadOnly] public float Inertia { get; private set; }
	protected virtual void CalcInertia()
	{
		Inertia = 0.5f * (Mass * MathX.InchToMeter( Radius * Radius ));
	}

	[Property, Range( 0.01f, 30f )] public float SuspensionExtensionSpeedCoeff { get; set; } = 6;


	/// <summary>
	/// 	Amount of anti-squat geometry. 
	///     -1 = There is no anti-squat and full squat torque is applied to the chassis.
	///     0 = No squat torque is applied to the chassis.
	///     1 = Anti-squat torque (inverse squat) is applied to the chassis.
	///     Higher value will result in less vehicle squat/dive under acceleration/braking.
	/// </summary>
	[Property, Range( -1, 1 )] public float AntiSquat { get; set; }
	[Property, ReadOnly, Sync] public float AxleAngle { get; set; }

	/// <summary>
	/// Higher the number, higher the effect of longitudinal friction on lateral friction.
	/// If 1, when wheels are locked up or there is wheel spin it will be impossible to steer.
	/// If 0 doughnuts or power slides will be impossible.
	/// The 'accurate' value is 1 but might not be desirable for arcade games.
	/// </summary>
	[Range( 0, 1 )]
	[Property] private readonly float FrictionCircleStrength = 1f;

	/// <summary>
	/// Higher values have more pronounced slip circle effect as the lateral friction will be
	/// decreased with smaller amounts of longitudinal slip (wheel spin).
	/// Realistic is ~1.5-2.
	/// </summary>
	[Range( 0.0001f, 3f )]
	[Property] private readonly float FrictionCircleShape = 1.75f;

	/// <summary>
	/// Distance as a percentage of the max spring length. Value of 1 means that the friction force will
	/// be applied 1 max spring length above the contact point, and value of 0 means that it will be applied at the
	/// ground level. Value can be >1.
	/// Can be used instead of the anti-roll bar to prevent the vehicle from tipping over in corners
	/// and can be useful in low framerate applications where anti-roll bar might induce jitter.
	/// </summary>
	[Property] public float ForceApplicationPointDistance = 0.8f;
}
