﻿using Sandbox.Utils;

namespace Sandbox.Powertrain;

public class EngineComponent : PowertrainComponent
{
	protected override void OnAwake()
	{
		base.OnAwake();
		Name ??= "Engine";
		UpdatePeakPowerAndTorque();

	}

	[Hide] public new bool Input { get; set; }

	public delegate float CalculateTorque( float angularVelocity, float dt );
	/// <summary>
	///     Delegate for a function that modifies engine power.
	/// </summary>
	public delegate float PowerModifier();

	public enum EngineType
	{
		ICE,
		Electric,
	}
	/// <summary>
	///     If true starter will be ran for [starterRunTime] seconds if engine receives any throttle input.
	/// </summary>
	[Property] public bool AutoStartOnThrottle { get; set; } = true;

	/// <summary>
	///     Assign your own delegate to use different type of torque calculation.
	/// </summary>
	public CalculateTorque CalculateTorqueDelegate;


	/// <summary>
	///     Engine type. ICE (Internal Combustion Engine) supports features such as starter, stalling, etc.
	///     Electric engine (motor) can run in reverse, can not be stalled and does not use starter.
	/// </summary>
	[Property] public EngineType Type { get; set; } = EngineType.ICE;

	/// <summary>
	///     Power generated by the engine in kW
	/// </summary>
	public float generatedPower;


	/// <summary>
	///     RPM at which idler circuit will try to keep RPMs when there is no input.
	/// </summary>
	[Property] public float IdleRPM { get; set; } = 900;

	/// <summary>
	///     Maximum engine power in [kW].
	/// </summary>
	[Property, Group( "Power" )] public float MaxPower { get; set; } = 120;

	/// <summary>
	/// Loss power (pumping, friction losses) is calculated as the percentage of maxPower.
	/// Should be between 0 and 1 (100%).
	/// </summary>
	[Range( 0, 1 ), Property] public float EngineLossPercent { get; set; } = 0.25f;


	/// <summary>
	///     If true the engine will be started immediately, without running the starter, when the vehicle is enabled.
	///     Sets engine angular velocity to idle angular velocity.
	/// </summary>
	[Property] public bool FlyingStartEnabled { get; set; }

	[Property] public bool Ignition { get; set; } = true;

	/// <summary>
	///     Power curve with RPM range [0,1] on the X axis and power coefficient [0,1] on Y axis.
	///     Both values are represented as percentages and should be in 0 to 1 range.
	///     Power coefficient is multiplied by maxPower to get the final power at given RPM.
	/// </summary>
	[Property, Group( "Power" )] public Curve PowerCurve { get; set; }

	/// <summary>
	///     Is the engine currently hitting the rev limiter?
	/// </summary>
	public bool RevLimiterActive;

	/// <summary>
	///     If engine RPM rises above revLimiterRPM, how long should fuel cutoff last?
	///     Higher values make hitting rev limiter more rough and choppy.
	/// </summary>
	[Property] public float RevLimiterCutoffDuration { get; set; } = 0f;

	/// <summary>
	///     Engine RPM at which rev limiter activates.
	/// </summary>
	[Property] public float RevLimiterRPM { get; set; } = 4700;


	/// <summary>
	///     Can the vehicle be stalled?
	///     If disabled engine will run no matter the RPM. Automatically disabled when electric engine type is used.
	/// </summary>
	[Property, ShowIf( nameof( Type ), EngineType.ICE )] public bool StallingEnabled { get; set; } = true;
	/// <summary>
	///     Is the starter currently active?
	/// </summary>
	[Property, ReadOnly, Group( "Info" )] public bool StarterActive = false;

	/// <summary>
	///     Torque starter motor can put out. Make sure that this torque is more than loss torque
	///     at the starter RPM limit. If too low the engine will fail to start.
	/// </summary>
	[Property] public float StartDuration = 0.5f;


	/// <summary>
	/// Peak power as calculated from the power curve. If the power curve peaks at Y=1 peak power will equal max power field value.
	/// After changing power, power curve or RPM range call UpdatePeakPowerAndTorque() to get update the value.
	/// </summary>
	[Property, ReadOnly, Group( "Info" )]
	public float EstimatedPeakPower
	{
		get { return _peakPower; }
	}

	private float _peakPower;



	/// <summary>
	/// RPM at which the peak power is achieved.
	/// After changing power, power curve or RPM range call UpdatePeakPowerAndTorque() to get update the value.
	/// </summary>
	[Property, ReadOnly, Group( "Info" )]
	public float EstimatedPeakPowerRPM
	{
		get { return _peakPowerRpm; }
	}

	private float _peakPowerRpm;

	/// <summary>
	/// Peak torque value as calculated from the power curve.
	/// After changing power, power curve or RPM range call UpdatePeakPowerAndTorque() to get update the value.
	/// </summary>
	[Property, ReadOnly, Group( "Info" )]
	public float EstimatedPeakTorque
	{
		get { return _peakTorque; }
	}

	private float _peakTorque;

	/// <summary>
	/// RPM at which the engine achieves the peak torque, calculated from the power curve.
	/// After changing power, power curve or RPM range call UpdatePeakPowerAndTorque() to get update the value.
	/// </summary>
	[Property, ReadOnly, Group( "Info" )]
	public float EstimatedPeakTorqueRPM
	{
		get { return _peakTorqueRpm; }
	}

	private float _peakTorqueRpm;

	/// <summary>
	///     RPM as a percentage of maximum RPM.
	/// </summary>
	[Property, ReadOnly, Group( "Info" )]
	public float RPMPercent
	{
		get { return _rpmPercent; }
	}

	private float _rpmPercent;
	/// <summary>
	///     Engine throttle position. 0 for no throttle and 1 for full throttle.
	/// </summary>
	[Property, ReadOnly, Group( "Info" )]
	public float ThrottlePosition
	{
		get { return _throttlePosition; }
	}

	private float _throttlePosition;

	public float PowerMultiplayer { get; set; }
	/// <summary>
	/// Is the engine currently running?
	/// Requires ignition to be enabled and stall RPM above the stall RPM.
	/// </summary>
	[Property, ReadOnly, Group( "Info" )]
	public bool IsRunning
	{
		get { return _isRunning; }
	}

	private bool _isRunning;

	/// <summary>
	/// Is the engine stalled or stalling?
	/// ICE engine only.
	/// </summary>
	[Property]
	public bool IsStalled
	{
		get
		{
			return Type != EngineType.Electric && StallingEnabled && OutputAngularVelocity < _idleAngularVelocity * 0.4f;
		}
	}
	private float _idleAngularVelocity;



	/// <summary>
	/// Current load of the engine, based on the power produced.
	/// </summary>
	public float Load
	{
		get { return _load; }
	}
	public event Action OnEngineStart;
	public event Action OnEngineStop;
	public event Action OnRevLimiter;

	private float _load;


	protected override void OnStart()
	{
		base.OnStart();

		if ( Type == EngineType.ICE )
		{
			CalculateTorqueDelegate = CalculateTorqueICE;
		}
		else if ( Type == EngineType.Electric )
		{
			StallingEnabled = false;
			IdleRPM = 0f;
			FlyingStartEnabled = true;
			CalculateTorqueDelegate = CalculateTorqueElectric;
			StarterActive = false;
			StartDuration = 0.001f;
			RevLimiterCutoffDuration = 0f;
		}
	}

	public void StartEngine()
	{
		Ignition = true;

		OnEngineStart?.Invoke();

		if ( Type != EngineType.Electric )
			if ( FlyingStartEnabled )
				FlyingStart();
			else if ( !StarterActive )
				if ( CarController != null )
					StarterCoroutine();
	}
	private async void StarterCoroutine()
	{
		if ( Type == EngineType.Electric || StarterActive )
			return;

		float startTimer = 0f;

		StarterActive = true;

		// Calculate starter torque from the start duration and the inertia of the engine
		// Avoid start duration around 0 as that will apply large torque impulse.
		if ( StartDuration < 0.1f )
			StartDuration = 0.1f;

		_starterTorque = ((_idleAngularVelocity - OutputAngularVelocity) * Inertia) / StartDuration;

		// Run the starter
		while ( startTimer <= StartDuration )
		{
			startTimer += 0.1f;
			await GameTask.DelaySeconds( 0.1f );
		}

		// Start finished
		_starterTorque = 0;
		StarterActive = false;
		// Check if engine running or start failed
		if ( OutputAngularVelocity < _stallAngularVelocity )
			StopEngine();
	}


	private void FlyingStart()
	{
		Ignition = true;
		StarterActive = false;
		OutputAngularVelocity = UnitConverter.RPMToAngularVelocity( IdleRPM );
	}

	public void StopEngine()
	{
		Ignition = false;
		OnEngineStop?.Invoke();
	}


	/// <summary>
	/// Toggles engine state.
	/// </summary>
	public void StartStopEngine()
	{
		if ( IsRunning )
			StopEngine();
		else
			StartEngine();
	}

	public void UpdatePeakPowerAndTorque()
	{
		GetPeakPower( out _peakPower, out _peakPowerRpm );
		GetPeakTorque( out _peakTorque, out _peakTorqueRpm );
	}

	protected override void OnFixedUpdate()
	{
		float dt = Time.Delta;
		// Cache values
		_userThrottleInput = CarController.Input.InputSwappedThrottle;
		_throttlePosition = _userThrottleInput;
		_idleAngularVelocity = UnitConverter.RPMToAngularVelocity( IdleRPM );
		_stallAngularVelocity = StallingEnabled ? _idleAngularVelocity * 0.4f : -1e10f;
		_revLimiterAngularVelocity = UnitConverter.RPMToAngularVelocity( RevLimiterRPM );
		if ( _revLimiterAngularVelocity == 0f )
			return;

		// Check for start on throttle
		if ( !IsRunning && !StarterActive && AutoStartOnThrottle && _throttlePosition > 0.2f )
			StartEngine();

		// Check for user start/stop input
		if ( CarController.Input.EngineStartStop )
		{
			StartStopEngine();
			CarController.Input.EngineStartStop = false;
		}

		// Check if stall needed
		bool wasRunning = _isRunning;
		_isRunning = Ignition && !IsStalled;
		if ( wasRunning && !_isRunning )
			StopEngine();

		// Physics update
		if ( OutputNameHash == 0 )
			return;

		float drivetrainInertia = _output.QueryInertia();
		float inertiaSum = Inertia + drivetrainInertia;
		if ( inertiaSum == 0 )
			return;

		float drivetrainAngularVelocity = QueryAngularVelocity( OutputAngularVelocity, dt );
		float targetAngularVelocity = Inertia / inertiaSum * OutputAngularVelocity + drivetrainInertia / inertiaSum * drivetrainAngularVelocity;

		// Calculate generated torque and power
		float generatedTorque = CalculateTorqueDelegate( OutputAngularVelocity, dt );
		generatedPower = TorqueToPowerInKW( in OutputAngularVelocity, in generatedTorque );

		// Calculate reaction torque
		float reactionTorque = (targetAngularVelocity - OutputAngularVelocity) * Inertia / dt;

		// Calculate/get torque returned from wheels
		OutputTorque = generatedTorque - reactionTorque;
		float returnTorque = ForwardStep( OutputTorque, 0, dt );
		float totalTorque = generatedTorque + returnTorque + reactionTorque;
		OutputAngularVelocity += totalTorque / inertiaSum * dt;

		// Clamp the angular velocity to prevent any powertrain instabilities over the limits
		float minAngularVelocity = 0f;
		float maxAngularVelocity = _revLimiterAngularVelocity * 1.05f;
		OutputAngularVelocity = Math.Clamp( OutputAngularVelocity, minAngularVelocity, maxAngularVelocity );

		// Calculate cached values
		_rpmPercent = Math.Clamp( OutputAngularVelocity / _revLimiterAngularVelocity, 0, 1 );
		_load = Math.Clamp( generatedPower / MaxPower, 0, 1 );
	}

	private float _starterTorque;
	private float _stallAngularVelocity;
	private float _revLimiterAngularVelocity;
	private float _userThrottleInput;

	private async void RevLimiter()
	{
		if ( RevLimiterActive || Type == EngineType.Electric || RevLimiterCutoffDuration == 0 )
			return;

		RevLimiterActive = true;
		OnRevLimiter?.Invoke();
		await GameTask.DelayRealtimeSeconds( RevLimiterCutoffDuration );
		RevLimiterActive = false;
	}


	/// <summary>
	///     Calculates torque for electric engine type.
	/// </summary>
	public float CalculateTorqueElectric( float angularVelocity, float dt )
	{
		float absAngVel = Math.Abs( angularVelocity );

		// Avoid angular velocity spikes while shifting
		if ( CarController.Powertrain.Transmission.IsShifting )
			_throttlePosition = 0;

		float maxLossPower = MaxPower * 0.3f;
		float lossPower = maxLossPower * (1f - _throttlePosition) * RPMPercent;
		float genPower = MaxPower * _throttlePosition;
		float totalPower = genPower - lossPower;
		totalPower = MathX.Lerp( totalPower * 0.1f, totalPower, RPMPercent * 10f );
		float clampedAngVel = absAngVel < 10f ? 10f : absAngVel;
		return PowerInKWToTorque( clampedAngVel, totalPower );
	}


	/// <summary>
	/// Calculates torque for ICE (Internal Combustion Engine).
	/// </summary>
	public float CalculateTorqueICE( float angularVelocity, float dt )
	{
		// Set the throttle to 0 when shifting, but avoid doing so around idle RPM to prevent stalls.
		if ( CarController.Powertrain.Transmission.IsShifting && angularVelocity > _idleAngularVelocity )
			_throttlePosition = 0f;

		// Set throttle to 0 when starter active.
		if ( StarterActive )
			_throttlePosition = 0f;
		// Apply idle throttle correction to keep the engine running
		else
			ApplyICEIdleCorrection();

		// Trigger rev limiter if needed
		if ( angularVelocity >= _revLimiterAngularVelocity && !RevLimiterActive )
			RevLimiter();

		// Calculate torque
		float generatedTorque;

		// Do not generate any torque while starter is active to prevent RPM spike during startup
		// or while stalled to prevent accidental starts.
		if ( StarterActive || IsStalled )
			generatedTorque = 0f;
		else
			generatedTorque = CalculateICEGeneratedTorqueFromPowerCurve();

		float lossTorque = StarterActive ? 0f : CalculateICELossTorqueFromPowerCurve();

		// Reduce the loss torque at rev limiter, but allow it to be >0 to prevent vehicle getting
		// stuck at rev limiter.
		if ( RevLimiterActive )
			lossTorque *= 0.25f;

		generatedTorque += _starterTorque + lossTorque;
		return generatedTorque;
	}

	private float CalculateICELossTorqueFromPowerCurve()
	{
		// Avoid issues with large torque spike around 0 angular velocity.
		if ( OutputAngularVelocity < 10f )
			return -OutputAngularVelocity * MaxPower * 0.03f;

		float angVelPercent = OutputAngularVelocity < 10f ? 0.1f : Math.Clamp( OutputAngularVelocity, _stallAngularVelocity, _revLimiterAngularVelocity ) / _revLimiterAngularVelocity;

		float lossPower = PowerCurve.Evaluate( angVelPercent ) * -MaxPower * Math.Clamp( _userThrottleInput + 0.5f, 0, 1 ) * EngineLossPercent;

		return PowerInKWToTorque( OutputAngularVelocity, lossPower );
	}

	private void ApplyICEIdleCorrection()
	{
		if ( Ignition && OutputAngularVelocity < _idleAngularVelocity * 1.1f )
		{
			// Apply a small correction to account for the error since the throttle is applied only
			// if the idle RPM is below the target RPM.
			float idleCorrection = _idleAngularVelocity * 1.08f - OutputAngularVelocity;
			idleCorrection = idleCorrection < 0f ? 0f : idleCorrection;
			float idleThrottlePosition = Math.Clamp( idleCorrection * 0.01f, 0, 1 );
			_throttlePosition = Math.Max( _userThrottleInput, idleThrottlePosition );
		}
	}

	private float CalculateICEGeneratedTorqueFromPowerCurve()
	{
		generatedPower = 0;
		float torque = 0;

		if ( !Ignition && !StarterActive )
			return 0;

		if ( RevLimiterActive )
			_throttlePosition = 0.2f;
		else
		{
			// Add maximum losses to the maximum power when calculating the generated power since the maxPower is net value (after losses).
			generatedPower = PowerCurve.Evaluate( _rpmPercent ) * (MaxPower * (1f + EngineLossPercent)) * _throttlePosition * PowerMultiplayer;
			torque = PowerInKWToTorque( OutputAngularVelocity, generatedPower );
		}
		return torque;
	}


	public void GetPeakTorque( out float peakTorque, out float peakTorqueRpm )
	{
		peakTorque = 0;
		peakTorqueRpm = 0;

		for ( float i = 0.05f; i < 1f; i += 0.05f )
		{
			float rpm = i * RevLimiterRPM;
			float P = PowerCurve.Evaluate( i ) * MaxPower;
			if ( rpm < IdleRPM )
			{
				continue;
			}

			float W = UnitConverter.RPMToAngularVelocity( rpm );
			float T = (P * 1000f) / W;

			if ( T > peakTorque )
			{
				peakTorque = T;
				peakTorqueRpm = rpm;
			}
		}
	}

	public void GetPeakPower( out float peakPower, out float peakPowerRpm )
	{
		float maxY = 0f;
		float maxX = 1f;
		for ( float i = 0f; i < 1f; i += 0.05f )
		{
			float y = PowerCurve.Evaluate( i );
			if ( y > maxY )
			{
				maxY = y;
				maxX = i;
			}
		}

		peakPower = maxY * MaxPower;
		peakPowerRpm = maxX * RevLimiterRPM;
	}

}
