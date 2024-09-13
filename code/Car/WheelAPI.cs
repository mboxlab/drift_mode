using System;
using DM.Ground;

namespace DM.Car;

public partial class Wheel
{
	private FrictionPreset.PresetsEnum activePresetEnum = FrictionPreset.PresetsEnum.Asphalt;

	[Property] public float SteerAngle { get; set; }
	[Property] public WheelManager Manager { get; set; }
	[Property] public float MotorTorque { get; set; }
	[Property] public bool IsPower { get; set; }
	[Property] public float BrakeTorque { get; set; }

	[Description( "The amount of torque returned by the wheel.\r\nUnder no-slip conditions this will be equal to the torque that was input.\r\nWhen there is wheel spin, the value will be less than the input torque." )]
	[Property, ReadOnly] public float CounterTorque { get; private set; }

	[Description( "Current angular velocity of the wheel in rad/s." )]
	[Property, ReadOnly] public float AngularVelocity { get; set; }
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

	[Property, Group( "Components" )]
	public FrictionPreset.PresetsEnum ActivePresetEnum
	{
		get => activePresetEnum;
		set
		{
			activePresetEnum = value;
			FrictionPreset.To( FrictionPreset.Presets[value] );
		}
	}
	[Property, Group( "Components" )] public FrictionPreset FrictionPreset { get; set; } = FrictionPreset.Asphalt;

	public Friction ForwardFriction { get; set; } = new();
	public Friction SideFriction { get; set; } = new();
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
	[Property, Sync] public float AxleAngle { get; set; }
}
