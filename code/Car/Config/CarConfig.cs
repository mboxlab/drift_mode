using System;

namespace Sandbox.Car.Config;

public enum CarConfigEnum
{
	Drift,
	Street,
}

public enum DriveType
{
	AWD,                    //All wheels drive
	FWD,                    //Forward wheels drive
	RWD                     //Rear wheels drive
}

public abstract class ICarConfig
{

	/// <summary>
	/// Drive type AWD, FWD, RWD. 
	/// </summary>
	[Property, Group( "Engine" )] public virtual DriveType DriveType { get; set; }
	[Property, Group( "Engine" )] public virtual bool AutomaticGearBox { get; set; }
	/// <summary>
	/// Max motor torque engine (Without GearBox multiplier).
	/// </summary>
	[Property, Group( "Engine" )] public virtual float MaxMotorTorque { get; set; }
	[Property, Group( "Engine" )] public virtual float MaxRPM { get; set; }
	[Property, Group( "Engine" )] public virtual float MinRPM { get; set; }
	/// <summary>
	/// The RPM at which the cutoff is triggered.
	/// </summary>
	[Property, Group( "Engine" )] public virtual float CutOffRPM { get; set; }
	[Property, Group( "Engine" )] public virtual float CutOffOffsetRPM { get; set; }
	[Property, Group( "Engine" )] public virtual float CutOffTime { get; set; }
	/// <summary>
	/// Probability backfire: 0 - off backfire, 1 always on backfire.
	/// </summary>
	[Property, Group( "Engine" ), Range( 0, 1 )] public virtual float ProbabilityBackfire { get; set; }
	/// <summary>
	/// The speed at which there is an increase in gearbox.
	/// </summary>
	[Property, Group( "Engine" )] public virtual float RpmToNextGear { get; set; }
	/// <summary>
	/// The speed at which there is an decrease in gearbox.
	/// </summary>
	[Property, Group( "Engine" )] public virtual float RpmToPrevGear { get; set; }
	/// <summary>
	/// Maximum rear wheel slip for shifting gearbox.
	/// </summary>
	[Property, Group( "Engine" )] public virtual float MaxForwardSlipToBlockChangeGear { get; set; }
	/// <summary>
	/// Lerp Speed change of RPM.
	/// </summary>
	[Property, Group( "Engine" )] public virtual float RpmEngineToRpmWheelsLerpSpeed { get; set; }
	/// <summary>
	/// Forward gears ratio.
	/// </summary>
	[Property, Group( "Engine" )] public virtual float[] GearsRatio { get; set; }
	[Property, Group( "Engine" )] public virtual float MainRatio { get; set; }
	/// <summary>
	/// Reverse gear ratio.
	/// </summary>
	[Property, Group( "Engine" )] public virtual float ReversGearRatio { get; set; }

	[Property, Group( "Brake" )] public virtual float MaxBrakeTorque { get; set; }
	[Property, Group( "Brake" )] public virtual float TargetSpeedIfBrakingGround { get; set; }
	[Property, Group( "Brake" )] public virtual bool EnableSteerAngleMultiplier { get; set; }
	/// <summary>
	/// Min steer angle multiplayer to limit understeer at high speeds.
	/// </summary>
	[Property, Group( "Brake" )] public virtual float MinSteerAngleMultiplier { get; set; }
	/// <summary>
	/// Max steer angle multiplayer to limit understeer at high speeds.          
	/// </summary>
	[Property, Group( "Brake" )] public virtual float MaxSteerAngleMultiplier { get; set; }
	/// <summary>
	/// The maximum speed at which there will be a minimum steering angle multiplier.
	/// </summary>
	[Property, Group( "Brake" )] public virtual float MaxSpeedForMinAngleMultiplier { get; set; }

	[Property, Group( "Steer" )] public virtual float MaxSteerAngle { get; set; }

	/// <summary>
	/// Wheel turn speed
	/// </summary>
	[Property, Group( "Steer" )] public virtual float SteerAngleChangeSpeed { get; set; }
	/// <summary>
	/// Min speed at which helpers are enabled.
	/// </summary>
	[Property, Group( "Steer" )] public virtual float MinSpeedForSteerHelp { get; set; }
	/// <summary>
	/// The power of turning the wheels in the direction of the drift.
	/// </summary>
	[Property, Group( "Steer" ), Range( 0, 1 )] public virtual float HelpSteerPower { get; set; }
	/// <summary>
	/// The power of the helper to turn the rigidbody in the direction of the control turn.
	/// </summary>
	[Property, Group( "Steer" )] public virtual float OppositeAngularVelocityHelpPower { get; set; }
	/// <summary>
	/// The power of the helper to positive turn the rigidbody in the direction of the control turn.
	/// </summary>
	[Property, Group( "Steer" )] public virtual float PositiveAngularVelocityHelpPower { get; set; }
	/// <summary>
	/// The angle at which the assistant works 100%.
	/// </summary>
	[Property, Group( "Steer" )] public virtual float MaxAngularVelocityHelpAngle { get; set; }
	/// <summary>
	/// Min angular velocity, reached at max drift angles.
	/// </summary>
	[Property, Group( "Steer" )] public virtual float AngularVelucityInMaxAngle { get; set; }
	/// <summary>
	/// Max angular velocity, reached at min drift angles.
	/// </summary>
	[Property, Group( "Steer" )] public virtual float AngularVelucityInMinAngle { get; set; }
	/// <summary>
	/// To change the friction of the rear wheels with a hand brake.
	/// </summary>
	[Property, Group( "Steer" )] public virtual float HandBrakeForwardStiffness { get; set; }
	/// <summary>
	/// To change the friction of the rear wheels with a hand brake.
	/// </summary>
	[Property, Group( "Steer" )] public virtual float HandBrakeSidewaysStiffness { get; set; }

	[Property, Group( "Friction" )] public virtual PacejkaCurve FrictionPreset { get; set; }
}
