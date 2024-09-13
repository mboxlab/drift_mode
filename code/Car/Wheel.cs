using DM.Ground;
using System;
namespace DM.Car;

[Category( "Vehicles" )]
[Title( "Car Wheel" )]
[Icon( "sync" )]
public partial class Wheel : Component
{

	GroundHit groundHit;
	public bool IsGrounded => groundHit.Hit;

	private bool _lowSpeedReferenceIsSet;
	private Vector3 _lowSpeedReferencePosition;
	public Vector3 hitContactVelocity;
	private Vector3 hitForwardDirection;
	private Vector3 hitSidewaysDirection;

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

	private Vector3 FrictionForce;
	private Vector3 SuspensionForce;


	private MultiRayCastGroundDetection GroundDetection { get; } = new();

	protected override void OnEnabled()
	{
		Rigidbody ??= Components.Get<Rigidbody>( FindMode.InAncestors );
		Visual ??= Components.Get<Renderer>( FindMode.InDescendants ).GameObject;
		Manager ??= Components.Get<WheelManager>( FindMode.InAncestors );
		Manager.Register( this );
		Rigidbody.AngularDamping = 0;
		Rigidbody.LinearDamping = 0;
	}
	private Rotation transformRotation;
	protected override void OnFixedUpdate()
	{

		ApplyVisuals();
		if ( IsProxy )
			return;

		groundHit = GroundDetection.Cast( GameObject, this );


		PrevAngularVelocity = AngularVelocity;
		Spring.PrevLength = Spring.Length;
		var steerRotation = Rotation.FromAxis( Vector3.Up, SteerAngle );
		transformRotation = Transform.Rotation * steerRotation;

		UpdateSuspension();
		UpdateHitVariables();

		UpdateFriction();
		//ApplyForceToHitBody();
		ApplySquatAndChassisTorque();

	}

	private void ApplySquatAndChassisTorque()
	{
		float squatMagnitude = ForwardFriction.Force * Radius * AntiSquat;
		Vector3 squatTorque = 0;
		var right = transformRotation.Right;

		squatTorque.x = squatMagnitude * right.x;
		squatTorque.y = squatMagnitude * right.y;
		squatTorque.z = squatMagnitude * right.z;

		// torque around the X-axis.
		float chassisTorqueMag = ((PrevAngularVelocity - AngularVelocity) * Inertia) / Time.Delta;

		Vector3 chassisTorque = 0;
		chassisTorque.x = chassisTorqueMag * right.x;
		chassisTorque.y = chassisTorqueMag * right.y;
		chassisTorque.z = chassisTorqueMag * right.z;

		Vector3 combinedTorque = 0;
		combinedTorque.x = MathX.MeterToInch( squatTorque.x + chassisTorque.x );
		combinedTorque.y = MathX.MeterToInch( squatTorque.y + chassisTorque.y );
		combinedTorque.z = MathX.MeterToInch( squatTorque.z + chassisTorque.z );

		Rigidbody.ApplyTorque( combinedTorque );
	}


	private void UpdateHitVariables()
	{
		if ( IsGrounded )
		{
			hitContactVelocity = Rigidbody.GetVelocityAtPoint( groundHit.Point + Rigidbody.PhysicsBody.LocalMassCenter );

			hitForwardDirection = groundHit.Normal.Cross( transformRotation.Right ).Normal;
			hitSidewaysDirection = Rotation.FromAxis( groundHit.Normal, 90f ) * hitForwardDirection;

			// Get forward and side friction speed components
			ForwardFriction.Speed = hitContactVelocity.Dot( hitForwardDirection ).InchToMeter();

			SideFriction.Speed = hitContactVelocity.Dot( hitSidewaysDirection ).InchToMeter();

		}
		else
		{
			ForwardFriction.Speed = 0f;
			SideFriction.Speed = 0f;
		}
	}
	private void ApplyVisuals()
	{
		AxleAngle = AxleAngle % 360.0f + AngularVelocity.RadianToDegree() * Time.Delta;
		Visual.Transform.LocalPosition = Visual.Transform.LocalPosition.WithZ( -Spring.Length );

		var axleRotation = Rotation.FromAxis( Vector3.Right, -AxleAngle );
		Visual.Transform.Rotation = transformRotation * axleRotation;
	}

	public void ApplyMotorTorque( float motorTorque )
	{
		if ( IsPower ) MotorTorque = motorTorque;
	}

	public void ApplyBrakeTorque( float brakeTorque ) => BrakeTorque = brakeTorque;

	Vector3 localPosition = 0;
	protected void UpdateSuspension()
	{

		float localAirZPosition = localPosition.z - Time.Delta * Spring.MaxLength * SuspensionExtensionSpeedCoeff;

		var hitLocalPoint = Transform.World.PointToLocal( groundHit.Point );

		if ( IsGrounded )
		{
			float sine = hitLocalPoint.x / Radius;
			sine = sine > 1f ? 1f : sine < -1f ? -1f : sine;
			float hitAngle = MathF.Asin( sine );
			float localGroundedZPosition = hitLocalPoint.z + Radius * MathF.Cos( hitAngle );
			localPosition.z = localGroundedZPosition > localAirZPosition
				? localGroundedZPosition
				: localAirZPosition;
		}
		else
		{
			localPosition.z = localAirZPosition;
		}


		Spring.Length = -localPosition.z;

		if ( Spring.Length <= 0f || Spring.MaxLength == 0f )
		{
			Spring.State = Spring.ExtensionState.BottomedOut;
			Spring.Length = 0;
		}
		else if ( Spring.Length >= Spring.MaxLength )
		{
			Spring.State = Spring.ExtensionState.OverExtended;
			Spring.Length = Spring.MaxLength;
			groundHit.Hit = false;
		}
		else
		{
			Spring.State = Spring.ExtensionState.Normal;
		}
		Spring.CompressionVelocity = (Spring.PrevLength - Spring.Length).InchToMeter() / Time.Delta;
		Spring.Compression = Spring.MaxLength == 0 ? 1f : (Spring.MaxLength - Spring.Length) / Spring.MaxLength;
		Spring.Force = IsGrounded ? Spring.MaxForce * Spring.ForceCurve.Evaluate( Spring.Compression ) : 0f;
		Damper.Force = IsGrounded ? Damper.CalculateDamperForce( Spring.CompressionVelocity ) : 0f;

		if ( IsGrounded )
		{
			Load = Spring.Force + Damper.Force;
			Load = Load < 0f ? 0f : Load;
			SuspensionForce = Load * groundHit.Normal;

			Rigidbody.ApplyImpulseAt( Transform.Position, SuspensionForce );
		}
		else
		{
			Load = 0;
		}

	}
	private Vector3 referenceError = 0;
	private Vector3 correctiveForce = 0;
	private Vector3 currentPosition = 0;

	protected void UpdateFriction()
	{
		var motorTorque = MotorTorque;
		var brakeTorque = BrakeTorque;

		CounterTorque = 0;
		ForwardFriction.Force = 0;
		SideFriction.Force = 0;

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

		float mass = Rigidbody.PhysicsBody.Mass;
		float absForwardSpeed = ForwardFriction.Speed < 0 ? -ForwardFriction.Speed : ForwardFriction.Speed;
		float forwardForceClamp = mass * LoadContribution * absForwardSpeed * invDt;
		float absSideSpeed = SideFriction.Speed < 0 ? -SideFriction.Speed : SideFriction.Speed;
		float sideForceClamp = mass * LoadContribution * absSideSpeed * invDt;

		float forwardSpeedClamp = 1.5f * (Time.Delta / 0.005f);
		forwardSpeedClamp = forwardSpeedClamp < 1.5f ? 1.5f : forwardSpeedClamp > 10f ? 10f : forwardSpeedClamp;
		float clampedAbsForwardSpeed = absForwardSpeed < forwardSpeedClamp ? forwardSpeedClamp : absForwardSpeed;



		// TODO

		//// Calculate effect of camber on friction
		float camberFrictionCoeff = Transform.Rotation.Up.Dot( groundHit.Normal );
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

		SideFriction.Slip = MathF.Atan2( SideFriction.Speed, clampedAbsForwardSpeed ).RadianToDegree() * 0.01111f;
		SideFriction.Slip *= slipLoadModifier;

		float sideSlipSign = SideFriction.Slip < 0 ? -1f : 1f;
		float absSideSlip = SideFriction.Slip < 0 ? -SideFriction.Slip : SideFriction.Slip;
		float peakSideFrictionForce = FrictionPreset.PeakValue * sideLoadFactor * SideFriction.Grip;
		float sideForce = -sideSlipSign * FrictionPreset.Curve.Evaluate( absSideSlip ) * sideLoadFactor * SideFriction.Grip;
		SideFriction.Force = sideForce > sideForceClamp ? sideForce : sideForce < -sideForceClamp ? -sideForceClamp : sideForce;
		SideFriction.Force *= camberFrictionCoeff;



		// *******************************
		// ******* ANTI - CREEP **********
		// *******************************

		// Get the error to the reference point and apply the force to keep the wheel at that point
		if ( IsGrounded && absForwardSpeed < 0.12f && absSideSpeed < 0.12f )
		{
			float verticalOffset = Spring.Length.InchToMeter() + Radius;
			var _transformPosition = Transform.Position;
			var _transformUp = transformRotation.Up;
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
				SideFriction.Force += correctiveForce.Dot( hitSidewaysDirection );

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

		SideFriction.Force = SideFriction.Force > peakSideFrictionForce ? peakSideFrictionForce
			: SideFriction.Force < -peakSideFrictionForce ? -peakSideFrictionForce : SideFriction.Force;

		// *******************************
		// ********* SLIP CIRCLE ********* 
		// *******************************
		if ( FrictionCircleStrength > 0 && (absForwardSpeed > 2f || absAngularVelocity > 4f) )
		{
			float forwardSlipPercent = ForwardFriction.Slip / FrictionPreset.PeakSlip;
			float sideSlipPercent = SideFriction.Slip / FrictionPreset.PeakSlip;
			float slipCircleLimit = MathF.Sqrt( forwardSlipPercent * forwardSlipPercent + sideSlipPercent * sideSlipPercent );
			if ( slipCircleLimit > 1f )
			{
				float beta = MathF.Atan2( sideSlipPercent, forwardSlipPercent * FrictionCircleShape );
				float sinBeta = MathF.Sin( beta );
				float cosBeta = MathF.Cos( beta );

				float absForwardForce = ForwardFriction.Force < 0 ? -ForwardFriction.Force : ForwardFriction.Force;

				float absSideForce = SideFriction.Force < 0 ? -SideFriction.Force : SideFriction.Force;
				float f = absForwardForce * cosBeta * cosBeta + absSideForce * sinBeta * sinBeta;

				float invSlipCircleCoeff = 1f - FrictionCircleStrength;
				ForwardFriction.Force = invSlipCircleCoeff * ForwardFriction.Force - FrictionCircleStrength * f * cosBeta;
				SideFriction.Force = invSlipCircleCoeff * SideFriction.Force - FrictionCircleStrength * f * sinBeta;
			}
		}


		// Apply the forces
		if ( IsGrounded )
		{
			FrictionForce.x = (hitSidewaysDirection.x * SideFriction.Force + hitForwardDirection.x * ForwardFriction.Force).MeterToInch();
			FrictionForce.y = (hitSidewaysDirection.y * SideFriction.Force + hitForwardDirection.y * ForwardFriction.Force).MeterToInch();
			FrictionForce.z = (hitSidewaysDirection.z * SideFriction.Force + hitForwardDirection.z * ForwardFriction.Force).MeterToInch();
			// Avoid adding calculated friction when using native friction
			Vector3 forcePosition = groundHit.Point + transformRotation.Up * ForceApplicationPointDistance * Spring.MaxLength;
			Rigidbody.ApplyForceAt( forcePosition, FrictionForce );

		}
		else
		{
			FrictionForce = Vector3.Zero;
		}


	}
	private void ApplyForceToHitBody()
	{
		if ( IsGrounded )
		{
			groundHit.Body?.ApplyForceAt( groundHit.Point, (-FrictionForce - SuspensionForce) );
		}
	}

	protected override void DrawGizmos()
	{

		if ( !Gizmo.IsSelected )
			return;
		if ( Visual is null )
			return;
		Gizmo.Draw.IgnoreDepth = true;

		// 
		//	Collider visual
		//	

		var circlePosition = Visual.Transform.LocalPosition;
		Gizmo.Draw.LineThickness = 1.0f;
		Gizmo.Draw.Color = Color.Yellow;
		var offset = Vector3.Right * Width / 2;
		Gizmo.Draw.LineCylinder( circlePosition - offset, circlePosition + offset, Radius, Radius, 16 );

		for ( float i = 0; i < 16; i++ )
		{

			var pos = circlePosition + Vector3.Up.RotateAround( Vector3.Zero, new Angles( i / 16 * 360, 0, 0 ) ) * Radius;

			Gizmo.Draw.Line( pos - offset, pos + offset );
			var pos2 = circlePosition + Vector3.Up.RotateAround( Vector3.Zero, new Angles( (i + 1) / 16 * 360, 0, 0 ) ) * Radius;
			Gizmo.Draw.Line( pos - offset, pos2 + offset );
		}

		if ( IsGrounded )
		{
			Vector3 localPoint = Transform.World.PointToLocal( groundHit.Point );
			Gizmo.Draw.LineThickness = 3.0f;
			Gizmo.Draw.Color = Color.Green;
			Gizmo.Draw.Line( circlePosition, localPoint );
			Gizmo.Draw.Color = Color.White;

			Gizmo.Draw.Line( -localPoint, -localPoint + groundHit.Normal * Radius );
			Gizmo.Draw.Color = IsGrounded ? Color.Orange : Color.Red;
			Gizmo.Transform = Scene.Transform.World;
			Gizmo.Draw.LineSphere( groundHit.Point, 0.04f );
			Gizmo.Transform = Transform.World;

		}

		//
		// Forward direction
		//
		{
			Gizmo.Transform = Scene.Transform.World;
			var suspensionStart = groundHit.StartPosition;
			var suspensionEnd = groundHit.EndPosition;

			Gizmo.Draw.Color = Color.Red;
			Gizmo.Draw.LineThickness = 0.25f;

			Gizmo.Draw.Line( suspensionStart, suspensionEnd );

		}

	}
}
