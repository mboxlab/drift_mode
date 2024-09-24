
using System;
using System.IO;
using System.Text.RegularExpressions;
using AltCurves;
using DM.Car;

namespace Sandbox.Car;

[Category( "Vehicles" )]
public sealed class CarController : Component
{
	public string CharacterName { get; set; }
	[Property] public WheelCollider[] Wheels { get; private set; }
	[Property] public Rigidbody Rigidbody { get; private set; }

	[Property] CarConfig CarConfig;
	[Property] CarDriftConfig CarDriftConfig;

	#region Properties of car Settings

	float MaxMotorTorque;
	float MaxSteerAngle { get { return CarConfig.MaxSteerAngle; } }
	DriveType DriveType { get { return CarConfig.DriveType; } }
	bool AutomaticGearBox { get { return CarConfig.AutomaticGearBox; } }
	AltCurve MotorTorqueFromRpmCurve { get { return CarConfig.MotorTorqueFromRpmCurve; } }
	float MaxRPM { get { return CarConfig.MaxRPM; } }
	float MinRPM { get { return CarConfig.MinRPM; } }
	float CutOffRPM { get { return CarConfig.CutOffRPM; } }
	float CutOffOffsetRPM { get { return CarConfig.CutOffOffsetRPM; } }
	float RpmToNextGear { get { return CarConfig.RpmToNextGear; } }
	float RpmToPrevGear { get { return CarConfig.RpmToPrevGear; } }
	float MaxForwardSlipToBlockChangeGear { get { return CarConfig.MaxForwardSlipToBlockChangeGear; } }
	float RpmEngineToRpmWheelsLerpSpeed { get { return CarConfig.RpmEngineToRpmWheelsLerpSpeed; } }
	float[] GearsRatio { get { return CarConfig.GearsRatio; } }
	float MainRatio { get { return CarConfig.MainRatio; } }
	float ReversGearRatio { get { return CarConfig.ReversGearRatio; } }

	float MaxBrakeTorque { get { return CarConfig.MaxBrakeTorque; } }
	float TargetSpeedIfBrakingGround { get { return CarConfig.TargetSpeedIfBrakingGround; } }
	float BrakingSpeedOneWheelTime { get { return CarConfig.BrakingSpeedOneWheelTime; } }

	#endregion //Properties of car Settings

	#region Properties of drift Settings

	public bool EnableSteerAngleMultiplier { get { return CarDriftConfig.EnableSteerAngleMultiplier; } }
	float MinSteerAngleMultiplier { get { return CarDriftConfig.MinSteerAngleMultiplier; } }
	float MaxSteerAngleMultiplier { get { return CarDriftConfig.MaxSteerAngleMultiplier; } }
	float MaxSpeedForMinAngleMultiplier { get { return CarDriftConfig.MaxSpeedForMinAngleMultiplier; } }
	float SteerAngleChangeSpeed { get { return CarDriftConfig.SteerAngleChangeSpeed; } }
	float MinSpeedForSteerHelp { get { return CarDriftConfig.MinSpeedForSteerHelp; } }
	float HelpSteerPower { get { return CarDriftConfig.HelpSteerPower; } }
	float OppositeAngularVelocityHelpPower { get { return CarDriftConfig.OppositeAngularVelocityHelpPower; } }
	float PositiveAngularVelocityHelpPower { get { return CarDriftConfig.PositiveAngularVelocityHelpPower; } }
	float MaxAngularVelocityHelpAngle { get { return CarDriftConfig.MaxAngularVelocityHelpAngle; } }
	float AngularVelucityInMaxAngle { get { return CarDriftConfig.AngularVelucityInMaxAngle; } }
	float AngularVelucityInMinAngle { get { return CarDriftConfig.AngularVelucityInMinAngle; } }

	#endregion //Properties of drift Settings

	/// <summary>
	/// Max slip of all wheels.
	/// </summary>
	public float CurrentMaxSlip { get; private set; }

	/// <summary>
	/// Max slip wheel index.
	/// </summary>
	public int CurrentMaxSlipWheelIndex { get; private set; }

	/// <summary>
	/// Speed, magnitude of velocity.
	/// </summary>
	public float CurrentSpeed { get; private set; }
	public int CarDirection { get { return CurrentSpeed < 1 ? 0 : (VelocityAngle < 90 && VelocityAngle > -90 ? 1 : -1); } }

	int FirstDriveWheel;
	int LastDriveWheel;
	float[] AllGearsRatio;

	protected override void OnAwake()
	{
		base.OnAwake();


		//Set drive wheel.
		switch ( DriveType )
		{
			case DriveType.AWD:
				FirstDriveWheel = 0;
				LastDriveWheel = 3;
				break;
			case DriveType.FWD:
				FirstDriveWheel = 0;
				LastDriveWheel = 1;
				break;
			case DriveType.RWD:
				FirstDriveWheel = 2;
				LastDriveWheel = 3;
				break;
		}

		//Divide the motor torque by the count of driving wheels
		MaxMotorTorque = CarConfig.MaxMotorTorque / (LastDriveWheel - FirstDriveWheel + 1);


		//Calculated gears ratio with main ratio
		AllGearsRatio = new float[GearsRatio.Length + 2];
		AllGearsRatio[0] = ReversGearRatio * MainRatio;
		AllGearsRatio[1] = 0;
		for ( int i = 0; i < GearsRatio.Length; i++ )
		{
			AllGearsRatio[i + 2] = GearsRatio[i] * MainRatio;
		}

	}
	protected override void OnUpdate()
	{
		UpdateControls();
	}

	protected override void OnFixedUpdate()
	{
		CurrentSpeed = Rigidbody.Velocity.Length;

		UpdateSteerAngleLogic();
		UpdateRpmAndTorqueLogic();
	}

	#region Rpm and torque logic

	public int CurrentGear { get; private set; }
	public int CurrentGearIndex { get { return CurrentGear + 1; } }
	public float EngineRPM { get; private set; }
	public float GetMaxRPM { get { return MaxRPM; } }
	public float GetMinRPM { get { return MinRPM; } }
	public float GetInCutOffRPM { get { return CutOffRPM - CutOffOffsetRPM; } }

	float CutOffTimer;
	bool InCutOff;
	float CurrentSteerAngle;
	float CurrentAcceleration;
	float CurrentBrake;
	bool InHandBrake;

	void UpdateRpmAndTorqueLogic()
	{

		if ( InCutOff )
		{
			if ( CutOffTimer > 0 )
			{
				CutOffTimer -= Time.Delta;
				EngineRPM = MathX.Lerp( EngineRPM, GetInCutOffRPM, RpmEngineToRpmWheelsLerpSpeed * Time.Delta );
			}
			else
			{
				InCutOff = false;
			}
		}

		//if ( GameController.Instance != null && !GameController.RaceIsStarted )
		//{
		//	if ( InCutOff ) return;

		//	float rpm = CurrentAcceleration > 0 ? MaxRPM : MinRPM;
		//	float speed = CurrentAcceleration > 0 ? RpmEngineToRpmWheelsLerpSpeed : RpmEngineToRpmWheelsLerpSpeed * 0.2f;
		//	EngineRPM = Mathf.Lerp( EngineRPM, rpm, speed * Time.Delta );
		//	if ( EngineRPM >= CutOffRPM )
		//	{
		//		PlayBackfireWithProbability();
		//		InCutOff = true;
		//		CutOffTimer = CarConfig.CutOffTime;
		//	}
		//	return;
		//}

		//Get drive wheel with MinRPM.
		float minRPM = 0;
		for ( int i = FirstDriveWheel + 1; i <= LastDriveWheel; i++ )
		{
			minRPM += Wheels[i].RPM;
		}

		minRPM /= LastDriveWheel - FirstDriveWheel + 1;


		if ( !InCutOff )
		{

			//Calculate the rpm based on rpm of the wheel and current gear ratio.
			float targetRPM = Math.Abs( (minRPM + 20f) * AllGearsRatio[CurrentGearIndex] );              //+20 for normal work CutOffRPM

			targetRPM = MathX.Clamp( targetRPM, MinRPM, MaxRPM );
			EngineRPM = MathX.Lerp( EngineRPM, targetRPM, RpmEngineToRpmWheelsLerpSpeed * Time.Delta );
		}
		if ( EngineRPM >= CutOffRPM )
		{
			//PlayBackfireWithProbability();
			InCutOff = true;
			CutOffTimer = CarConfig.CutOffTime;
			return;
		}
		if ( !MathX.AlmostEqual( CurrentAcceleration, 0 ) )
		{
			//If the direction of the car is the same as Current Acceleration.
			if ( CarDirection * CurrentAcceleration >= 0 )
			{
				CurrentBrake = 0;

				float motorTorqueFromRpm = MotorTorqueFromRpmCurve.Evaluate( EngineRPM * 0.001f );
				var motorTorque = CurrentAcceleration * (motorTorqueFromRpm * (MaxMotorTorque * AllGearsRatio[CurrentGearIndex]));
				if ( Math.Abs( minRPM ) * AllGearsRatio[CurrentGearIndex] > MaxRPM )
					motorTorque = 0;


				//If the rpm of the wheel is less than the max rpm engine * current ratio, then apply the current torque for wheel, else not torque for wheel.
				float maxWheelRPM = AllGearsRatio[CurrentGearIndex] * EngineRPM;
				for ( int i = FirstDriveWheel; i <= LastDriveWheel; i++ )
				{
					if ( Wheels[i].RPM <= maxWheelRPM )
						Wheels[i].MotorTorque = motorTorque;
					else
						Wheels[i].MotorTorque = 0;
				}
			}
			else
			{
				CurrentBrake = MaxBrakeTorque;
				for ( int i = FirstDriveWheel; i <= LastDriveWheel; i++ )
					Wheels[i].MotorTorque = 0;
			}
		}
		else
		{
			CurrentBrake = 0;

			for ( int i = FirstDriveWheel; i <= LastDriveWheel; i++ )
				Wheels[i].MotorTorque = 0;
		}
		//Automatic gearbox logic. 
		if ( AutomaticGearBox )
		{

			float prevRatio = 0;
			float newRatio = 0;

			if ( EngineRPM > RpmToNextGear && CurrentGear >= 0 && CurrentGear < (AllGearsRatio.Length - 2) )
			{
				prevRatio = AllGearsRatio[CurrentGearIndex];
				CurrentGear++;
				newRatio = AllGearsRatio[CurrentGearIndex];
			}
			else if ( EngineRPM < RpmToPrevGear && CurrentGear > 0 && (EngineRPM <= MinRPM || CurrentGear != 1) )
			{
				prevRatio = AllGearsRatio[CurrentGearIndex];
				CurrentGear--;
				newRatio = AllGearsRatio[CurrentGearIndex];
			}

			if ( !MathX.AlmostEqual( prevRatio, 0 ) && !MathX.AlmostEqual( newRatio, 0 ) )
			{
				EngineRPM = MathX.Lerp( EngineRPM, EngineRPM * (newRatio / prevRatio), RpmEngineToRpmWheelsLerpSpeed * Time.Delta ); //EngineRPM * (prevRatio / newRatio);// 
			}

			if ( CarDirection <= 0 && CurrentAcceleration < 0 )
			{
				CurrentGear = -1;
			}
			else if ( CurrentGear <= 0 && CarDirection >= 0 && CurrentAcceleration > 0 )
			{
				CurrentGear = 1;
			}
			else if ( CarDirection == 0 && CurrentAcceleration == 0 )
			{
				CurrentGear = 0;
			}
		}

		//TODO manual gearbox logic.
	}

	#endregion
	static float SignedAngle( Vector3 v1, Vector3 v2, Vector3 v3 )
	{
		float angle = Vector3.GetAngle( v1, v2 );
		angle *= Math.Sign( Vector3.Cross( v1, v3 ).y );
		return angle;
	}

	//Angle between forward point and velocity point.
	public float VelocityAngle { get; private set; }
	/// <summary>
	/// Update all helpers logic.
	/// </summary>
	void UpdateSteerAngleLogic()
	{
		var needHelp = CurrentSpeed.InchToMeter() > MinSpeedForSteerHelp && CarDirection > 0;
		float targetAngle = 0;

		VelocityAngle = -SignedAngle( Rigidbody.Velocity, Transform.Rotation.Forward, Vector3.Up );

		if ( needHelp )
		{
			//Wheel turning helper.
			targetAngle = Math.Clamp( VelocityAngle * HelpSteerPower, -MaxSteerAngle, MaxSteerAngle );
		}
		//Wheel turn limitation.
		targetAngle = Math.Clamp( targetAngle + CurrentSteerAngle, -(MaxSteerAngle + 10), MaxSteerAngle + 10 );

		//Front wheel turn.
		Wheels[0].SteerAngle = targetAngle;
		Wheels[1].SteerAngle = targetAngle;

		if ( needHelp )
		{
			//Angular velocity helper.
			var absAngle = Math.Abs( VelocityAngle );

			//Get current procent help angle.
			float currentAngularProcent = absAngle / MaxAngularVelocityHelpAngle;

			var currAngle = Rigidbody.AngularVelocity;

			if ( VelocityAngle * CurrentSteerAngle > 0 )
			{
				//Turn to the side opposite to the angle. To change the angular velocity.
				var angularVelocityMagnitudeHelp = OppositeAngularVelocityHelpPower * CurrentSteerAngle * Time.Delta;
				currAngle.y += angularVelocityMagnitudeHelp * currentAngularProcent;
			}
			else if ( !MathX.AlmostEqual( CurrentSteerAngle, 0 ) )
			{
				//Turn to the side positive to the angle. To change the angular velocity.
				var angularVelocityMagnitudeHelp = PositiveAngularVelocityHelpPower * CurrentSteerAngle * Time.Delta;
				currAngle.y += angularVelocityMagnitudeHelp * (1 - currentAngularProcent);
			}

			//Clamp and apply of angular velocity.
			var maxMagnitude = ((AngularVelucityInMaxAngle - AngularVelucityInMinAngle) * currentAngularProcent) + AngularVelucityInMinAngle;
			currAngle.y = Math.Clamp( currAngle.y, -maxMagnitude, maxMagnitude );
			Rigidbody.AngularVelocity = currAngle;

		}
	}

	#region Controls
	/// <summary>
	/// Update controls of car.
	/// </summary>
	/// <param name="horizontal">Turn direction</param>
	/// <param name="vertical">Acceleration</param>
	/// <param name="brake">Brake</param>
	public void UpdateControls( float horizontal, float vertical, bool brake )
	{
		float targetSteerAngle = horizontal * MaxSteerAngle;

		if ( EnableSteerAngleMultiplier )
			targetSteerAngle *= Math.Clamp( 1 - CurrentSpeed.InchToMeter() / MaxSpeedForMinAngleMultiplier, MinSteerAngleMultiplier, MaxSteerAngleMultiplier );

		CurrentSteerAngle = MathX.Approach( CurrentSteerAngle, targetSteerAngle, Time.Delta * SteerAngleChangeSpeed );

		CurrentAcceleration = vertical;
		if ( InHandBrake != brake )
		{
			float forwardStiffness = brake ? CarDriftConfig.HandBrakeForwardStiffness : 1;
			float sidewaysStiffness = brake ? CarDriftConfig.HandBrakeSidewaysStiffness : 1;
			Wheels[2].UpdateStiffness( forwardStiffness, sidewaysStiffness );
			Wheels[3].UpdateStiffness( forwardStiffness, sidewaysStiffness );
		}
		InHandBrake = brake;
	}
	private void UpdateControls() => UpdateControls( Input.AnalogMove.y, Input.AnalogMove.x, Input.Down( "HandBrake" ) );


	#endregion
}

public enum DriveType
{
	AWD,                    //All wheels drive
	FWD,                    //Forward wheels drive
	RWD                     //Rear wheels drive
}

/// <summary>
/// For easy initialization and change of parameters in the future. TODO Add tuning.
/// </summary>
public class CarConfig
{
	[Title( "Steer Settings" )]
	public float MaxSteerAngle = 25;

	[Title( "Engine and power settings" )]
	[Property] public DriveType DriveType { get; set; } = DriveType.RWD;             //Drive type AWD, FWD, RWD. With the current parameters of the car only RWD works well. TODO Add rally and offroad regime.
	[Property] public bool AutomaticGearBox { get; set; } = true;
	[Property] public float MaxMotorTorque { get; set; } = 150;                      //Max motor torque engine (Without GearBox multiplier).
	[Property] public AltCurve MotorTorqueFromRpmCurve { get; set; }          //Curve motor torque (Y(0-1) motor torque, X(0-7) motor RPM).
	[Property] public float MaxRPM { get; set; } = 7000;
	[Property] public float MinRPM { get; set; } = 700;
	[Property] public float CutOffRPM { get; set; } = 6800;                          //The RPM at which the cutoff is triggered.
	[Property] public float CutOffOffsetRPM { get; set; } = 500;
	[Property] public float CutOffTime { get; set; } = 0.1f;
	[Property, Range( 0, 1 )] public float ProbabilityBackfire { get; set; } = 0.2f;   //Probability backfire: 0 - off backfire, 1 always on backfire.
	[Property] public float RpmToNextGear { get; set; } = 6500;                      //The speed at which there is an increase in gearbox.
	[Property] public float RpmToPrevGear { get; set; } = 4500;                      //The speed at which there is an decrease in gearbox.
	[Property] public float MaxForwardSlipToBlockChangeGear { get; set; } = 0.5f;    //Maximum rear wheel slip for shifting gearbox.
	[Property] public float RpmEngineToRpmWheelsLerpSpeed { get; set; } = 15;        //Lerp Speed change of RPM.
	[Property] public float[] GearsRatio { get; set; }                              //Forward gears ratio.
	[Property] public float MainRatio { get; set; }
	[Property] public float ReversGearRatio { get; set; }                           //Reverse gear ratio.

	[Title( "Braking settings" )]
	[Property] public float MaxBrakeTorque { get; set; } = 1000;
	[Property] public float TargetSpeedIfBrakingGround { get; set; } = 20;
	[Property] public float BrakingSpeedOneWheelTime { get; set; } = 2;
}

/// <summary>
/// For easy initialization and change of parameters in the future. TODO Add tuning.
/// </summary>
public class CarDriftConfig
{
	[Property] public bool EnableSteerAngleMultiplier { get; set; } = true;
	[Property] public float MinSteerAngleMultiplier { get; set; } = 0.05f;       //Min steer angle multiplayer to limit understeer at high speeds.
	[Property] public float MaxSteerAngleMultiplier { get; set; } = 1f;          //Max steer angle multiplayer to limit understeer at high speeds.          
	[Property] public float MaxSpeedForMinAngleMultiplier { get; set; } = 250;   //The maximum speed at which there will be a minimum steering angle multiplier.

	[Property] public float SteerAngleChangeSpeed { get; set; } = 1f;                    //Wheel turn speed.
	[Property] public float MinSpeedForSteerHelp { get; set; } = 20f;                   //Min speed at which helpers are enabled.
	[Property, Range( 0, 1 )] public float HelpSteerPower { get; set; } = 0.1f;            //The power of turning the wheels in the direction of the drift.
	[Property] public float OppositeAngularVelocityHelpPower { get; set; } = 0.1f;       //The power of the helper to turn the rigidbody in the direction of the control turn.
	[Property] public float PositiveAngularVelocityHelpPower { get; set; } = 0.1f;       //The power of the helper to positive turn the rigidbody in the direction of the control turn.
	[Property] public float MaxAngularVelocityHelpAngle { get; set; } = 90f;             //The angle at which the assistant works 100%.
	[Property] public float AngularVelucityInMaxAngle { get; set; } = 0.5f;              //Min angular velocity, reached at max drift angles.
	[Property] public float AngularVelucityInMinAngle { get; set; } = 4f;                //Max angular velocity, reached at min drift angles.
	[Property] public float HandBrakeForwardStiffness { get; set; } = 0.5f;              //To change the friction of the rear wheels with a hand brake.
	[Property] public float HandBrakeSidewaysStiffness { get; set; } = 0.5f;             //To change the friction of the rear wheels with a hand brake.
}
