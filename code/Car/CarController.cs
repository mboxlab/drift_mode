
using System;
using System.Runtime.ConstrainedExecution;
using AltCurves;
using DM.Engine;
using Sandbox.Car.Config;
using Sandbox.GamePlay;

namespace Sandbox.Car;

[Category( "Vehicles" )]
public sealed class CarController : Component
{
	[Property] public Rigidbody Rigidbody { get; private set; }
	[Property] public ICarConfig CarConfig { get => carConfig; set { carConfig = value; OnConfigUpdated(); } }
	[Property] public SoundInterpolator SoundInterpolator { get; set; }
	[Property] public WheelCollider[] Wheels { get; private set; }
	public bool IsBot;

	public static CarController Local { get; private set; }
	[Authority]
	public void ClientInit()
	{
		if ( IsBot )
			return;

		Local = this;
	}
	#region Properties of car Settings
	private ICarConfig carConfig = new StreetCarConfig();

	float MaxMotorTorque;
	float MaxSteerAngle { get { return CarConfig.MaxSteerAngle; } }
	Config.DriveType DriveType { get { return CarConfig.DriveType; } }
	bool AutomaticGearBox { get { return CarConfig.AutomaticGearBox; } }
	/// <summary>
	/// Curve motor torque (Y(0-1) motor torque, X(0 - MaxRPM) motor RPM).
	/// </summary>
	[Property] public AltCurve TorqueCurve { get; set; }
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

	public bool EnableSteerAngleMultiplier { get { return CarConfig.EnableSteerAngleMultiplier; } }
	float MinSteerAngleMultiplier { get { return CarConfig.MinSteerAngleMultiplier; } }
	float MaxSteerAngleMultiplier { get { return CarConfig.MaxSteerAngleMultiplier; } }
	float MaxSpeedForMinAngleMultiplier { get { return CarConfig.MaxSpeedForMinAngleMultiplier; } }
	float SteerAngleChangeSpeed { get { return CarConfig.SteerAngleChangeSpeed; } }
	float MinSpeedForSteerHelp { get { return CarConfig.MinSpeedForSteerHelp; } }
	float HelpSteerPower { get { return CarConfig.HelpSteerPower; } }
	float OppositeAngularVelocityHelpPower { get { return CarConfig.OppositeAngularVelocityHelpPower; } }
	float PositiveAngularVelocityHelpPower { get { return CarConfig.PositiveAngularVelocityHelpPower; } }
	float MaxAngularVelocityHelpAngle { get { return CarConfig.MaxAngularVelocityHelpAngle; } }
	float AngularVelucityInMaxAngle { get { return CarConfig.AngularVelucityInMaxAngle; } }
	float AngularVelucityInMinAngle { get { return CarConfig.AngularVelucityInMinAngle; } }

	#endregion //Properties of car Settings

	/// <summary>
	/// Speed, magnitude of velocity.
	/// </summary>
	public float CurrentSpeed { get; private set; }
	public int CarDirection { get { return CurrentSpeed < 1 ? 0 : (VelocityAngle < 90 && VelocityAngle > -90 ? 1 : -1); } }

	int FirstDriveWheel;
	int LastDriveWheel;
	float[] AllGearsRatio;
	private IManager GameManager;
	protected override void OnAwake()
	{
		base.OnAwake();

		if ( !IsProxy )
			ClientInit();

		//Set drive wheel.
		switch ( DriveType )
		{
			case Config.DriveType.AWD:
				FirstDriveWheel = 0;
				LastDriveWheel = 3;
				break;
			case Config.DriveType.FWD:
				FirstDriveWheel = 0;
				LastDriveWheel = 1;
				break;
			case Config.DriveType.RWD:
				FirstDriveWheel = 2;
				LastDriveWheel = 3;
				break;
		}

		MaxMotorTorque = CarConfig.MaxMotorTorque / (LastDriveWheel - FirstDriveWheel + 1);


		AllGearsRatio = new float[GearsRatio.Length + 2];
		AllGearsRatio[0] = ReversGearRatio * MainRatio;
		AllGearsRatio[1] = 0;
		for ( int i = 0; i < GearsRatio.Length; i++ )
		{
			AllGearsRatio[i + 2] = GearsRatio[i] * MainRatio;
		}

	}
	protected override void OnStart()
	{
		SoundInterpolator.MaxValue = MaxRPM;
		UpdateWheelsFriction();
		GameManager = Scene.Components.Get<IManager>( FindMode.InDescendants );
		var gauge = Scene.Components.Get<Gauge>( FindMode.InDescendants );
		if ( gauge != null )
		{
			gauge.Car = this;
		}

	}
	protected override void OnUpdate()
	{

		UpdateControls();

		SoundInterpolator.Value = EngineRPM;
		SoundInterpolator.Volume = 1;
	}
	public event EventHandler<ICarConfig> ConfigUpdated;
	private void OnConfigUpdated()
	{
		ConfigUpdated?.Invoke( this, CarConfig );
		UpdateWheelsFriction();
	}
	private void UpdateWheelsFriction()
	{
		foreach ( WheelCollider wheel in Wheels )
		{
			wheel.FrictionPreset = CarConfig.FrictionPreset;
		}
	}
	protected override void OnFixedUpdate()
	{
		CurrentSpeed = Rigidbody.Velocity.Length;

		UpdateSteerAngle();
		UpdateRpmAndTorque();

		if ( InHandBrake )
		{
			Wheels[0].BrakeTorque = 0;
			Wheels[1].BrakeTorque = 0;
			Wheels[2].BrakeTorque = MaxBrakeTorque * 5;
			Wheels[3].BrakeTorque = MaxBrakeTorque * 5;
		}

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
	private bool InClutch;

	void UpdateRpmAndTorque()
	{

		if ( InCutOff )
			if ( CutOffTimer > 0 )
			{
				CutOffTimer -= Time.Delta;
				EngineRPM = MathX.Lerp( EngineRPM, GetInCutOffRPM, RpmEngineToRpmWheelsLerpSpeed * Time.Delta );
			}
			else
				InCutOff = false;


		if ( !GameManager.Started || InClutch )
		{
			if ( InCutOff ) return;

			float rpm = CurrentAcceleration > 0 ? MaxRPM : MinRPM;
			float speed = CurrentAcceleration > 0 ? RpmEngineToRpmWheelsLerpSpeed : RpmEngineToRpmWheelsLerpSpeed * 0.2f;
			EngineRPM = MathX.Lerp( EngineRPM, rpm, speed * Time.Delta );
			if ( EngineRPM >= CutOffRPM )
			{
				// TODO: backfire()
				InCutOff = true;
				CutOffTimer = CarConfig.CutOffTime;
			}
			return;
		}

		float minRPM = 0;
		for ( int i = FirstDriveWheel + 1; i <= LastDriveWheel; i++ )
			minRPM += Wheels[i].RPM;

		minRPM /= LastDriveWheel - FirstDriveWheel + 1;


		if ( !InCutOff )
		{
			float targetRPM = Math.Abs( (minRPM + 20f) * AllGearsRatio[CurrentGearIndex] );

			targetRPM = MathX.Clamp( targetRPM, MinRPM, MaxRPM );
			EngineRPM = MathX.Lerp( EngineRPM, targetRPM, RpmEngineToRpmWheelsLerpSpeed * Time.Delta );
		}
		if ( EngineRPM >= CutOffRPM )
		{
			// TODO: backfire()
			InCutOff = true;
			CutOffTimer = CarConfig.CutOffTime;
			return;
		}
		for ( int i = 0; i <= 3; i++ )
		{
			Wheels[i].MotorTorque = 0;
			Wheels[i].BrakeTorque = 0;
		}

		if ( !MathX.AlmostEqual( CurrentAcceleration, 0 ) )
			if ( CarDirection * CurrentAcceleration >= 0 )
			{
				CurrentBrake = 0;
				float motorTorqueFromRpm = TorqueCurve.Evaluate( EngineRPM * 0.001f );
				var motorTorque = CurrentAcceleration * (motorTorqueFromRpm * (MaxMotorTorque * AllGearsRatio[CurrentGearIndex]));
				if ( Math.Abs( minRPM ) * AllGearsRatio[CurrentGearIndex] > MaxRPM )
					motorTorque = 0;
				float maxWheelRPM = AllGearsRatio[CurrentGearIndex] * EngineRPM;
				for ( int i = FirstDriveWheel; i <= LastDriveWheel; i++ )
					if ( Wheels[i].RPM <= maxWheelRPM )
						Wheels[i].MotorTorque = motorTorque;
					else
						Wheels[i].MotorTorque = 0;
			}
			else
			{
				CurrentBrake = MaxBrakeTorque;
				for ( int i = 0; i <= 3; i++ )
					Wheels[i].BrakeTorque = CurrentBrake;
			}
		else
		{
			CurrentBrake = 0;

			for ( int i = 0; i <= 3; i++ )
				Wheels[i].BrakeTorque = CurrentBrake;
		}

		if ( AutomaticGearBox )
		{
			bool forwardIsSlip = false;
			for ( int i = FirstDriveWheel; i <= LastDriveWheel; i++ )
				if ( Wheels[i].ForwardSlip > MaxForwardSlipToBlockChangeGear )
				{
					forwardIsSlip = true;
					break;
				}

			float prevRatio = 0;
			float newRatio = 0;

			if ( !forwardIsSlip && EngineRPM > RpmToNextGear && CurrentGear >= 0 && CurrentGear < (AllGearsRatio.Length - 2) )
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
				CurrentGear = -1;
			else if ( CurrentGear <= 0 && CarDirection >= 0 && CurrentAcceleration > 0 )
				CurrentGear = 1;
			else if ( CarDirection == 0 && CurrentAcceleration == 0 )
				CurrentGear = 0;
		}
	}

	#endregion
	public static float SignedAngle( Vector3 from, Vector3 to, Vector3 axis )
	{
		float unsignedAngle = Vector3.GetAngle( from, to );

		float cross_x = from.y * to.z - from.z * to.y;
		float cross_y = from.z * to.x - from.x * to.z;
		float cross_z = from.x * to.y - from.y * to.x;
		float sign = MathF.Sign( axis.x * cross_x + axis.y * cross_y + axis.z * cross_z );
		return unsignedAngle * sign;
	}

	public float VelocityAngle { get; private set; }
	void UpdateSteerAngle()
	{
		var needHelp = CurrentSpeed > MinSpeedForSteerHelp && CarDirection > 0;
		float targetAngle = 0;
		VelocityAngle = -SignedAngle( Rigidbody.Velocity, Transform.Rotation.Forward, Vector3.Up );
		if ( needHelp )
		{
			//Wheel turning helper.
			targetAngle = VelocityAngle * HelpSteerPower;
		}

		//Wheel turn limitation.
		targetAngle = MathX.Clamp( targetAngle + CurrentSteerAngle, -(MaxSteerAngle + 10), MaxSteerAngle + 10 );

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
				currAngle.z += angularVelocityMagnitudeHelp * currentAngularProcent;
			}
			else if ( !MathX.AlmostEqual( CurrentSteerAngle, 0 ) )
			{
				//Turn to the side positive to the angle. To change the angular velocity.
				var angularVelocityMagnitudeHelp = PositiveAngularVelocityHelpPower * CurrentSteerAngle * Time.Delta;
				currAngle.z += angularVelocityMagnitudeHelp * (1 - currentAngularProcent);
			}

			//Clamp and apply of angular velocity.
			var maxMagnitude = ((AngularVelucityInMaxAngle - AngularVelucityInMinAngle) * currentAngularProcent) + AngularVelucityInMinAngle;
			currAngle.z = MathX.Clamp( currAngle.z, -maxMagnitude, maxMagnitude );
			Rigidbody.AngularVelocity = currAngle;

		}
	}

	#region Controls
	public void UpdateControls( float horizontal, float vertical, bool brake )
	{
		float targetSteerAngle = horizontal * MaxSteerAngle;

		if ( EnableSteerAngleMultiplier )
			targetSteerAngle *= Math.Clamp( 1 - CurrentSpeed.InchToMeter() / MaxSpeedForMinAngleMultiplier, MinSteerAngleMultiplier, MaxSteerAngleMultiplier );

		CurrentSteerAngle = MathX.Lerp( CurrentSteerAngle, targetSteerAngle, Time.Delta * SteerAngleChangeSpeed );

		CurrentAcceleration = vertical;
		if ( InHandBrake != brake )
		{
			float forwardStiffness = brake ? CarConfig.HandBrakeForwardStiffness : 1;
			float sidewaysStiffness = brake ? CarConfig.HandBrakeSidewaysStiffness : 1;
			Wheels[2].UpdateStiffness( forwardStiffness, sidewaysStiffness );
			Wheels[3].UpdateStiffness( forwardStiffness, sidewaysStiffness );
		}
		InHandBrake = brake;
		InClutch = Input.Down( "Clutch" );
	}
	private void UpdateControls() => UpdateControls( Input.AnalogMove.y, Input.AnalogMove.x, Input.Down( "HandBrake" ) );


	#endregion
}
