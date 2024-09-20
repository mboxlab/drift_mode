
using System;
using AltCurves;
using DM.Car;

namespace DM.Engine;
[Category( "Vehicles" )]
public partial class EngineICE : Component
{

	private float inertia;
	private float flywheelMass = 2.4f;
	private float flywheelRadius = 0.358f;

	public const float RPM_TO_RAD = 0.10472f;
	public const float RAD_TO_RPM = 60 / (2 * MathF.PI);
	public const float IDLE_FADE_START_OFFSET = 300f;
	public const float IDLE_FADE_END_OFFSET = 600f;
	[Property] public Rigidbody Body { get; set; }
	[Property] public Steering Steering { get; set; }
	[Property] public SoundInterpolator Interpolator { get; set; }
	[Property] public float MaxRPM { get; set; } = 7000;
	[Property] public float IdleRPM { get; set; } = 800;
	[Property] public float FlywheelMass { get => flywheelMass; set { flywheelMass = value; CalcInertia(); } }
	[Property] public float FlywheelRadius { get => flywheelRadius; set { flywheelRadius = value; CalcInertia(); } }
	[Property] public float StartFriction { get; set; } = 0.05f;
	[Property] public float FrictionCoeff { get; set; } = 0.02f;
	[Property] public float LimiterDuration { get; set; } = 0.03f;
	[Property, ReadOnly] public float RPM { get; set; } = 0f;
	[Property, Range( 0, 1 ), Sync] public float InputThrottle { get; set; }
	[Property, ReadOnly] private float MasterThrottle { get; set; } = 0f;
	[Property] public Clutch Clutch { get; set; }

	private TimeSince _limiterTime = 0;
	private void CalcInertia()
	{
		inertia = 0.5f * (flywheelMass * FlywheelRadius * FlywheelRadius);
	}
	[Property, ReadOnly] public float Inertia { get => inertia; }
	[Property] public AltCurve TorqueMap { get; set; }
	[Property, ReadOnly] public float Torque { get; private set; }

	protected override void OnStart()
	{
		base.OnStart();
		Interpolator.MaxValue = MaxRPM;

	}
	protected override void OnDisabled()
	{
		base.OnDisabled();
		Interpolator.Enabled = false;
		Steering.Enabled = false;
	}
	protected override void OnFixedUpdate()
	{

		//float rpmFrac = Math.Clamp( RPM / MaxRPM, 0, 1 );
		float friction = StartFriction - RPM * FrictionCoeff;

		float maxInitialTorque = TorqueMap.Evaluate( RPM );


		// Calculate idle fade
		float idleFadeStart = Math.Clamp( MathX.Remap( RPM, IdleRPM - IDLE_FADE_START_OFFSET, IdleRPM, 1, 0 ), 0, 1 );
		float idleFadeEnd = Math.Clamp( MathX.Remap( RPM, IdleRPM, IdleRPM + IDLE_FADE_END_OFFSET, 1, 0 ), 0, 1 );

		float additionalEnergySupply = idleFadeEnd * (-friction / maxInitialTorque) + idleFadeStart;

		float throttle = InputThrottle;
		if ( RPM > MaxRPM )
		{
			throttle = 0f;
			_limiterTime = 0;
		}
		else if ( _limiterTime < LimiterDuration )
			throttle = 0f;
		MasterThrottle = Math.Clamp( additionalEnergySupply + throttle, 0, 1 );

		var realInitialTorque = maxInitialTorque * MasterThrottle;

		Torque = realInitialTorque + friction;
		Clutch.Think();
		float invTorque = Clutch.Torque;
		RPM = RPM + (Torque - invTorque) / Inertia * RAD_TO_RPM * Time.Delta;
		RPM = Math.Max( RPM, 0 );

		ApplyBodyForce();
		SetSoundInterpolatorValues();

	}

	private void ApplyBodyForce()
	{
		if ( IsProxy )
			return;
		float tiltForce = Torque / 2;
		Body.ApplyImpulseAt( Body.PhysicsBody.MassCenter + Body.Transform.Rotation.Right * 39.37f, Body.Transform.Rotation.Up * tiltForce );
		Body.ApplyImpulseAt( Body.PhysicsBody.MassCenter + Body.Transform.Rotation.Left * 39.37f, Body.Transform.Rotation.Down * tiltForce );
	}

	private void SetSoundInterpolatorValues()
	{
		Interpolator.Value = RPM;
		Interpolator.Volume = MasterThrottle;
	}

}
