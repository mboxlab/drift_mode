using System;
using Sandbox.Car;
using Sandbox.Ground;
namespace Sandbox.Car;

[Category( "Vehicles" )]
public class WheelCollider : Stereable
{
	[Property] public float MinSuspensionLength { get => minSuspensionLength; set { minSuspensionLength = value; UpdateTotalSuspensionLength(); } }
	[Property] public float MaxSuspensionLength { get => maxSuspensionLength; set { maxSuspensionLength = value; UpdateTotalSuspensionLength(); } }
	[Property] public float SuspensionStiffness { get; set; } = 3000.0f;
	[Property] public float SuspensionDamping { get; set; } = 140.0f;
	[Property] public float Radius { get => wheelRadius; set { wheelRadius = value; UpdateTotalSuspensionLength(); } }
	[Property] public float Width { get; set; } = 6;
	[Property] public float Mass { get; set; } = 15;
	[Property, ReadOnly] public float Inertia { get => _inertia; set => _inertia = value; }
	//[Property] public WheelFrictionInfo ForwardFriction { get; set; }
	//[Property] public WheelFrictionInfo SidewayFriction { get; set; }
	[Property] public PacejkaCurve FrictionPreset { get; set; } = PacejkaCurve.Asphalt;

	[Property] public ParticleSphereEmitter SmokeEmitter { get; set; }

	[Property] public float Load { get; private set; }


	public bool IsGrounded => groundHit.Hit;
	public float ForwardSlip { get => ForwardFriction.Slip; }
	public float SidewaySlip { get => SidewayFriction.Slip; }
	public float AngularVelocity { get; private set; } = 0;
	[Property, ReadOnly] public float RPM { get => Math.Abs( AngularVelocity * 9.55f ); }
	public float MotorTorque { get => _motorTorque; set => _motorTorque = value; }
	public float BrakeTorque { get => _brakeTorque; set => _brakeTorque = value; }

	private GroundHit groundHit;
	private Rigidbody _rigidbody;
	private float _motorTorque;
	private float _brakeTorque;
	private float _suspensionTotalLength;
	private float maxSuspensionLength = 8f;
	private float wheelRadius = 14.0f;
	private float minSuspensionLength = 0f;
	[Property, Group( "Friction" )] public Friction ForwardFriction { get; set; } = new();
	[Property, Group( "Friction" )] public Friction SidewayFriction { get; set; } = new();
	public WheelManager Manager { get; set; }
	private Rotation TransformRotationSteer;
	protected override void OnEnabled()
	{
		Inertia = 0.5f * Mass * (Radius.InchToMeter() * Radius.InchToMeter());
		SmokeEmitter.Enabled = false;
		Manager ??= Components.Get<WheelManager>( FindMode.InAncestors );
		Manager.Register( this );
		_rigidbody = Components.GetInAncestorsOrSelf<Rigidbody>();
		UpdateTotalSuspensionLength();
	}
	private void UpdateTotalSuspensionLength()
	{
		_suspensionTotalLength = (maxSuspensionLength + wheelRadius) - minSuspensionLength;
	}

	public void Update()
	{
		if ( !_rigidbody.IsValid() )
			return;

		DoTrace();

		if ( IsProxy )
			return;

		SmokeEmitter.Enabled = IsGrounded && (Math.Abs( ForwardSlip ) + Math.Abs( SidewaySlip )) > 2f;
		SmokeEmitter.WorldPosition = groundHit.Point;

		var steerRotation = Rotation.FromAxis( Vector3.Up, SteerAngle );
		TransformRotationSteer = WorldRotation * steerRotation;
		UpdateHitVariables();
		UpdateSuspension();
		UpdateFriction();
	}

	protected override void OnFixedUpdate()
	{
		if ( AutoSimulate )
			Update();
	}
	[Description( "Constant torque acting similar to brake torque.\r\nImitates rolling resistance." )]
	[Property, Range( 0, 500 )] public float RollingResistanceTorque { get; set; } = 30f;
	[Description( "The percentage this wheel is contributing to the total vehicle load bearing." )]
	[Property, ReadOnly] public float LoadContribution { get; set; } = 0.25f;

	[Description( "Maximum load the tire is rated for in [N]. \r\nUsed to calculate friction.Default value is adequate for most cars but\r\nlarger and heavier vehicles such as semi trucks will use higher values.\r\nA good rule of the thumb is that this value should be 2x the Load\r\nwhile vehicle is stationary." )]
	[Property] public float LoadRating { get; set; } = 5400;

	/// <summary>
	/// The amount of torque returned by the wheel.
	/// Under no-slip conditions this will be equal to the torque that was input.
	/// When there is wheel spin, the value will be less than the input torque.
	/// </summary>
	[Property, ReadOnly] public float CounterTorque { get; private set; }
	public bool AutoSimulate { get; set; } = true;

	private Vector3 FrictionForce;
	public Vector3 hitContactVelocity;
	private Vector3 hitForwardDirection;
	private Vector3 hitSidewaysDirection;
	private Vector3 referenceError;
	private Vector3 correctiveForce;
	private Vector3 currentPosition;
	private bool _lowSpeedReferenceIsSet;
	private Vector3 _lowSpeedReferencePosition;
	private float _inertia;

	protected void UpdateFriction()
	{
		var motorTorque = MotorTorque;
		var brakeTorque = BrakeTorque;

		float allWheelLoadSum = Manager.CombinedLoad;

		LoadContribution = allWheelLoadSum == 0 ? 1f : Load / allWheelLoadSum;

		float Radius = this.Radius.InchToMeter();

		float invDt = 1f / Time.Delta;
		float invRadius = 1f / Radius;
		float inertia = Inertia;
		float invInertia = 1f / Inertia;

		float loadClamped = Load < 0f ? 0f : Load > LoadRating ? LoadRating : Load;
		float forwardLoadFactor = loadClamped * 1.35f;
		float sideLoadFactor = loadClamped * 1.9f;


		float loadPercent = Load / LoadRating;
		loadPercent = loadPercent < 0f ? 0f : loadPercent > 1f ? 1f : loadPercent;
		float slipLoadModifier = 1f - loadPercent * 0.4f;

		float mass = _rigidbody.PhysicsBody.Mass;
		float absForwardSpeed = ForwardFriction.Speed < 0 ? -ForwardFriction.Speed : ForwardFriction.Speed;
		float forwardForceClamp = mass * LoadContribution * absForwardSpeed * invDt;
		float absSideSpeed = SidewayFriction.Speed < 0 ? -SidewayFriction.Speed : SidewayFriction.Speed;
		float sideForceClamp = mass * LoadContribution * absSideSpeed * invDt;

		float forwardSpeedClamp = 1.5f * (Time.Delta / 0.005f);
		forwardSpeedClamp = forwardSpeedClamp < 1.5f ? 1.5f : forwardSpeedClamp > 10f ? 10f : forwardSpeedClamp;
		float clampedAbsForwardSpeed = absForwardSpeed < forwardSpeedClamp ? forwardSpeedClamp : absForwardSpeed;


		//// Calculate effect of camber on friction
		float camberFrictionCoeff = WorldRotation.Up.Dot( groundHit.Normal );
		camberFrictionCoeff = camberFrictionCoeff < 0f ? 0f : camberFrictionCoeff;

		// *******************************
		// ******** LONGITUDINAL ********* 
		// *******************************

		// T = r * F
		// F = T / r;

		// *** FRICTION ***

		float peakForwardFrictionForce = FrictionPreset.PeakValue * forwardLoadFactor * ForwardFriction.Grip;
		float absCombinedBrakeTorque = brakeTorque + RollingResistanceTorque;
		absCombinedBrakeTorque = absCombinedBrakeTorque < 0 ? 0 : absCombinedBrakeTorque;
		float signedCombinedBrakeTorque = absCombinedBrakeTorque * (ForwardFriction.Speed < 0 ? 1f : -1f);
		float signedCombinedBrakeForce = signedCombinedBrakeTorque * invRadius;
		float motorForce = motorTorque * invRadius;
		float forwardInputForce = motorForce + signedCombinedBrakeForce;
		float absMotorTorque = motorTorque < 0 ? -motorTorque : motorTorque;
		float absBrakeTorque = brakeTorque < 0 ? -brakeTorque : brakeTorque;
		float maxForwardForce = peakForwardFrictionForce < forwardForceClamp ? peakForwardFrictionForce : forwardForceClamp;
		maxForwardForce = absMotorTorque < absBrakeTorque ? maxForwardForce : peakForwardFrictionForce;
		ForwardFriction.Force = forwardInputForce > maxForwardForce ? maxForwardForce
			: forwardInputForce < -maxForwardForce ? -maxForwardForce : forwardInputForce;

		// *** ANGULAR VELOCITY ***

		// Brake and motor (corruptive) force
		bool wheelIsBlocked = false;
		if ( IsGrounded )
		{
			float absCombinedBrakeForce = absCombinedBrakeTorque * invRadius;
			float brakeForceSign = AngularVelocity < 0 ? 1f : -1f;
			float signedWheelBrakeForce = absCombinedBrakeForce * brakeForceSign;
			float combinedWheelForce = motorForce + signedWheelBrakeForce;
			float wheelForceClampOverflow = 0;
			if ( (combinedWheelForce >= 0 && AngularVelocity < 0) || (combinedWheelForce < 0 && AngularVelocity > 0) )
			{
				float absAngVel = AngularVelocity < 0 ? -AngularVelocity : AngularVelocity;
				float absWheelForceClamp = absAngVel * inertia * invRadius * invDt;
				float absCombinedWheelForce = combinedWheelForce < 0 ? -combinedWheelForce : combinedWheelForce;
				float combinedWheelForceSign = combinedWheelForce < 0 ? -1f : 1f;
				float wheelForceDiff = absCombinedWheelForce - absWheelForceClamp;
				float clampedWheelForceDiff = wheelForceDiff < 0f ? 0f : wheelForceDiff;
				wheelForceClampOverflow = clampedWheelForceDiff * combinedWheelForceSign;
				combinedWheelForce = combinedWheelForce < -absWheelForceClamp ? -absWheelForceClamp :
					combinedWheelForce > absWheelForceClamp ? absWheelForceClamp : combinedWheelForce;
			}
			AngularVelocity += combinedWheelForce * Radius * invInertia * Time.Delta;

			// Surface (corrective) force
			float noSlipAngularVelocity = ForwardFriction.Speed * invRadius;
			float angularVelocityError = AngularVelocity - noSlipAngularVelocity;
			float angularVelocityCorrectionForce = -angularVelocityError * inertia * invRadius * invDt;
			angularVelocityCorrectionForce = angularVelocityCorrectionForce < -maxForwardForce ? -maxForwardForce :
				angularVelocityCorrectionForce > maxForwardForce ? maxForwardForce : angularVelocityCorrectionForce;

			float absWheelForceClampOverflow = wheelForceClampOverflow < 0 ? -wheelForceClampOverflow : wheelForceClampOverflow;
			float absAngularVelocityCorrectionForce = angularVelocityCorrectionForce < 0 ? -angularVelocityCorrectionForce : angularVelocityCorrectionForce;
			if ( absMotorTorque < absBrakeTorque && absWheelForceClampOverflow > absAngularVelocityCorrectionForce )
			{
				wheelIsBlocked = true;
				AngularVelocity = AngularVelocity + ForwardFriction.Speed > 0 ? 1e-10f : -1e-10f;
			}
			else
			{
				AngularVelocity += angularVelocityCorrectionForce * Radius * invInertia * Time.Delta;

			}
		}
		else
		{
			float maxBrakeTorque = AngularVelocity * inertia * invDt + motorTorque;
			maxBrakeTorque = maxBrakeTorque < 0 ? -maxBrakeTorque : maxBrakeTorque;
			float brakeTorqueSign = AngularVelocity < 0f ? -1f : 1f;
			float clampedBrakeTorque = absCombinedBrakeTorque > maxBrakeTorque ? maxBrakeTorque :
				absCombinedBrakeTorque < -maxBrakeTorque ? -maxBrakeTorque : absCombinedBrakeTorque;
			AngularVelocity += (motorTorque - brakeTorqueSign * clampedBrakeTorque) * invInertia * Time.Delta;
		}

		float absAngularVelocity = AngularVelocity < 0 ? -AngularVelocity : AngularVelocity;

		// Powertrain counter torque
		CounterTorque = (signedCombinedBrakeForce - ForwardFriction.Force) * Radius;
		float maxCounterTorque = inertia * absAngularVelocity;
		CounterTorque = Math.Clamp( CounterTorque, -maxCounterTorque, maxCounterTorque );

		// Calculate slip based on the corrected angular velocity
		ForwardFriction.Slip = (ForwardFriction.Speed - AngularVelocity * Radius) / clampedAbsForwardSpeed;
		ForwardFriction.Slip *= slipLoadModifier;

		// *******************************
		// ********** LATERAL ************ 
		// *******************************

		SidewayFriction.Slip = MathF.Atan2( SidewayFriction.Speed, clampedAbsForwardSpeed ).RadianToDegree() * 0.01111f;
		SidewayFriction.Slip *= slipLoadModifier;

		float sideSlipSign = SidewayFriction.Slip < 0 ? -1f : 1f;
		float absSideSlip = SidewayFriction.Slip < 0 ? -SidewayFriction.Slip : SidewayFriction.Slip;
		float peakSideFrictionForce = FrictionPreset.PeakValue * sideLoadFactor * SidewayFriction.Grip;
		float sideForce = -sideSlipSign * FrictionPreset.Curve.Evaluate( absSideSlip ) * sideLoadFactor * SidewayFriction.Grip;
		SidewayFriction.Force = sideForce > sideForceClamp ? sideForce : sideForce < -sideForceClamp ? -sideForceClamp : sideForce;
		SidewayFriction.Force *= camberFrictionCoeff;



		// *******************************
		// ******* ANTI - CREEP **********
		// *******************************

		//Get the error to the reference point and apply the force to keep the wheel at that point
		if ( IsGrounded && absForwardSpeed < 0.12f && absSideSpeed < 0.12f )
		{
			float verticalOffset = _suspensionTotalLength.InchToMeter() + Radius;
			var _transformPosition = WorldPosition;
			var _transformUp = TransformRotationSteer.Up;
			currentPosition.x = _transformPosition.x - _transformUp.x * verticalOffset;
			currentPosition.y = _transformPosition.y - _transformUp.y * verticalOffset;
			currentPosition.z = _transformPosition.z - _transformUp.z * verticalOffset;

			if ( !_lowSpeedReferenceIsSet )
			{
				_lowSpeedReferenceIsSet = true;
				_lowSpeedReferencePosition = currentPosition;
			}
			else
			{


				referenceError.x = _lowSpeedReferencePosition.x - currentPosition.x;
				referenceError.y = _lowSpeedReferencePosition.y - currentPosition.y;
				referenceError.z = _lowSpeedReferencePosition.z - currentPosition.z;

				correctiveForce.x = invDt * LoadContribution * mass * referenceError.x;
				correctiveForce.y = invDt * LoadContribution * mass * referenceError.y;
				correctiveForce.z = invDt * LoadContribution * mass * referenceError.z;

				if ( wheelIsBlocked && absAngularVelocity < 0.5f )
				{
					ForwardFriction.Force += correctiveForce.Dot( hitForwardDirection );
				}
				SidewayFriction.Force += correctiveForce.Dot( hitSidewaysDirection );

			}

		}
		else
		{
			_lowSpeedReferenceIsSet = false;
		}

		// Clamp the forces once again, this time ignoring the force clamps as the anti-creep forces do not cause jitter,
		// so the forces are limited only by the surface friction.

		ForwardFriction.Force = ForwardFriction.Force > peakForwardFrictionForce ? peakForwardFrictionForce
			: ForwardFriction.Force < -peakForwardFrictionForce ? -peakForwardFrictionForce : ForwardFriction.Force;

		SidewayFriction.Force = SidewayFriction.Force > peakSideFrictionForce ? peakSideFrictionForce
			: SidewayFriction.Force < -peakSideFrictionForce ? -peakSideFrictionForce : SidewayFriction.Force;

		// *******************************
		// ********* SLIP CIRCLE ********* 
		// *******************************
		if ( 1f > 0 && (absForwardSpeed > 2f || absAngularVelocity > 4f) )
		{
			float forwardSlipPercent = ForwardFriction.Slip / FrictionPreset.PeakSlip;
			float sideSlipPercent = SidewayFriction.Slip / FrictionPreset.PeakSlip;
			float slipCircleLimit = MathF.Sqrt( forwardSlipPercent * forwardSlipPercent + sideSlipPercent * sideSlipPercent );
			if ( slipCircleLimit > 1f )
			{
				float beta = MathF.Atan2( sideSlipPercent, forwardSlipPercent * 1.75f );
				float sinBeta = MathF.Sin( beta );
				float cosBeta = MathF.Cos( beta );

				float absForwardForce = ForwardFriction.Force < 0 ? -ForwardFriction.Force : ForwardFriction.Force;

				float absSideForce = SidewayFriction.Force < 0 ? -SidewayFriction.Force : SidewayFriction.Force;
				float f = absForwardForce * cosBeta * cosBeta + absSideForce * sinBeta * sinBeta;

				ForwardFriction.Force = 0.5f * ForwardFriction.Force - 1f * f * cosBeta;
				SidewayFriction.Force = 0.5f * SidewayFriction.Force - 1f * f * sinBeta;
			}
		}

		// Apply the forces
		if ( IsGrounded )
		{
			FrictionForce.x = (hitSidewaysDirection.x * SidewayFriction.Force + hitForwardDirection.x * ForwardFriction.Force).MeterToInch();
			FrictionForce.y = (hitSidewaysDirection.y * SidewayFriction.Force + hitForwardDirection.y * ForwardFriction.Force).MeterToInch();
			FrictionForce.z = (hitSidewaysDirection.z * SidewayFriction.Force + hitForwardDirection.z * ForwardFriction.Force).MeterToInch();

			Vector3 forcePosition = groundHit.Point + TransformRotationSteer.Up * 0.8f * MaxSuspensionLength;
			_rigidbody.ApplyForceAt( forcePosition, FrictionForce );
		}
		else
			FrictionForce = Vector3.Zero;

	}
	public Vector3 GetCenter()
	{
		return WorldPosition + (Vector3.Down * (groundHit.Distance - (_suspensionTotalLength - maxSuspensionLength)));
	}

	private void UpdateHitVariables()
	{
		if ( IsGrounded )
		{
			hitContactVelocity = _rigidbody.GetVelocityAtPoint( groundHit.Point + _rigidbody.PhysicsBody.LocalMassCenter );

			hitForwardDirection = groundHit.Normal.Cross( TransformRotationSteer.Right ).Normal;
			hitSidewaysDirection = Rotation.FromAxis( groundHit.Normal, 90f ) * hitForwardDirection;

			ForwardFriction.Speed = hitContactVelocity.Dot( hitForwardDirection ).InchToMeter();
			SidewayFriction.Speed = hitContactVelocity.Dot( hitSidewaysDirection ).InchToMeter();
		}
		else
		{
			ForwardFriction.Speed = 0f;
			SidewayFriction.Speed = 0f;
		}
	}

	private void UpdateSuspension()
	{
		if ( !IsGrounded )
			return;

		var worldVelocity = _rigidbody.GetVelocityAtPoint( groundHit.Point + _rigidbody.PhysicsBody.LocalMassCenter );

		var localVel = worldVelocity.Dot( groundHit.Normal ).InchToMeter();

		var suspensionCompression = (groundHit.Distance - _suspensionTotalLength) / _suspensionTotalLength;
		var dampingForce = -SuspensionDamping * localVel;
		var springForce = -SuspensionStiffness * suspensionCompression;

		Load = Math.Abs( dampingForce + springForce );

		var totalForce = Load.MeterToInch() * Time.Delta;

		var suspensionForce = groundHit.Normal * totalForce.MeterToInch();
		_rigidbody.ApplyForceAt( WorldPosition, suspensionForce );
	}
	public void UpdateStiffness( float forward = 1f, float side = 1f )
	{
		ForwardFriction.Grip = forward;
		SidewayFriction.Grip = side;
	}

	private void DoTrace()
	{
		var down = _rigidbody.WorldRotation.Down;
		var startPos = WorldPosition + down * MinSuspensionLength;
		var endPos = startPos + down * (MaxSuspensionLength + Radius);

		groundHit = new( Scene.Trace
				.Radius( 1f )
				.IgnoreGameObjectHierarchy( GameObject.Root )
				.FromTo( startPos, endPos )
				.Run() );

		if ( groundHit.Distance > Radius )
			groundHit.Hit = false;
		groundHit.Distance = Math.Min( groundHit.Distance, Radius );
	}
	protected override void DrawGizmos()
	{

		if ( !Gizmo.IsSelected )
			return;

		Gizmo.Draw.IgnoreDepth = true;

		//
		// Suspension length
		//
		{
			var suspensionStart = Vector3.Zero - Vector3.Down * MinSuspensionLength;
			var suspensionEnd = Vector3.Zero + Vector3.Down * MaxSuspensionLength;

			Gizmo.Draw.Color = Color.Cyan;
			Gizmo.Draw.LineThickness = 0.25f;

			Gizmo.Draw.Line( suspensionStart, suspensionEnd );

			Gizmo.Draw.Line( suspensionStart + Vector3.Forward, suspensionStart + Vector3.Backward );
			Gizmo.Draw.Line( suspensionEnd + Vector3.Forward, suspensionEnd + Vector3.Backward );
		}

		//
		// Wheel radius
		//
		{

			var circleAxis = Vector3.Right;
			var circlePosition = Vector3.Zero;

			Gizmo.Draw.LineThickness = 1.0f;
			Gizmo.Draw.Color = Color.White;
			Gizmo.Draw.LineCircle( circlePosition, circleAxis, Radius );
		}

		//
		// Forward direction
		//
		{
			var arrowStart = Vector3.Forward * Radius;
			var arrowEnd = arrowStart + Vector3.Forward * 8f;

			Gizmo.Draw.Color = Color.Red;
			Gizmo.Draw.Arrow( arrowStart, arrowEnd, 4, 1 );
		}
	}

}
