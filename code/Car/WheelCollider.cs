using System;
using DM.Car;
using DM.Ground;
namespace Sandbox.Car;

[Category( "Vehicles" )]
public class WheelCollider : Stereable
{
	[Property] public float MinSuspensionLength { get => minSuspensionLength; set { minSuspensionLength = value; UpdateTotalSuspensionLength(); } }
	[Property] public float MaxSuspensionLength { get => maxSuspensionLength; set { maxSuspensionLength = value; UpdateTotalSuspensionLength(); } }
	[Property] public float SuspensionStiffness { get; set; } = 3000.0f;
	[Property] public float SuspensionDamping { get; set; } = 140.0f;
	[Property] public float WheelRadius { get => wheelRadius; set { wheelRadius = value; UpdateTotalSuspensionLength(); } }
	[Property] public float WheelWidth { get; set; } = 6;
	[Property] public WheelFrictionInfo ForwardFriction { get; set; }
	[Property] public WheelFrictionInfo SideFriction { get; set; }
	[Property] public ParticleSphereEmitter SmokeEmitter { get; set; }

	private float ForwardStiffness = 1;
	private float SideStiffness = 1;
	private const float LowSpeedThreshold = 32.0f;

	public bool IsGrounded => groundHit.Hit;
	public float ForwardSlip { get; private set; } = 0;
	public float SideSlip { get; private set; } = 0;
	public float AngleVelocity { get; private set; } = 0;
	public float RPM { get => Math.Abs( AngleVelocity * 6 ); }
	public float MotorTorque { get => _motorTorque; set => _motorTorque = value; }

	private GroundHit groundHit;
	private Rigidbody _rigidbody;
	private float _motorTorque;
	private float _suspensionTotalLength;
	private float maxSuspensionLength = 8f;
	private float wheelRadius = 14.0f;
	private float minSuspensionLength = 0f;

	protected override void OnEnabled()
	{

		_rigidbody = Components.GetInAncestorsOrSelf<Rigidbody>();
		UpdateTotalSuspensionLength();
	}
	private void UpdateTotalSuspensionLength()
	{
		_suspensionTotalLength = (maxSuspensionLength + wheelRadius) - minSuspensionLength;
	}
	protected override void OnFixedUpdate()
	{
		if ( !_rigidbody.IsValid() )
			return;

		DoTrace();

		if ( IsProxy )
			return;

		UpdateSuspension();
		UpdateWheelForces();
	}
	private void UpdateWheelForces()
	{

		if ( SmokeEmitter is not null ) SmokeEmitter.Enabled = IsGrounded;

		if ( SmokeEmitter is not null )
		{
			if ( AngleVelocity > 100f )
				SmokeEmitter.Rate += Math.Abs( SideSlip ) * AngleVelocity / 24f;
			else
				SmokeEmitter.Rate = 0;
			SmokeEmitter.Rate /= 1.07f;
		}
		if ( !IsGrounded )
		{

			AngleVelocity /= 1.1f;
			return;
		}

		var hitContactVelocity = _rigidbody.GetVelocityAtPoint( groundHit.Point + _rigidbody.PhysicsBody.LocalMassCenter ).RotateAround( Vector3.Zero, Rotation.FromYaw( -SteerAngle ) );

		var hitForwardDirection = groundHit.Normal.Cross( Transform.Rotation.Right ).Normal;
		var hitSidewaysDirection = Rotation.FromAxis( groundHit.Normal, 90f ) * hitForwardDirection;

		var wheelSpeed = hitContactVelocity.Length;

		SideSlip = CalculateSlip( hitContactVelocity, hitSidewaysDirection, wheelSpeed ) * SideStiffness;
		ForwardSlip = CalculateSlip( hitContactVelocity, hitForwardDirection, wheelSpeed ) * ForwardStiffness;

		var sideForce = CalculateFrictionForce( SideFriction, SideSlip, hitSidewaysDirection );
		var forwardForce = CalculateFrictionForce( ForwardFriction, ForwardSlip, hitForwardDirection );

		float factor = wheelSpeed.LerpInverse( 0f, LowSpeedThreshold );

		var targetAcceleration = (sideForce + forwardForce) * factor;
		targetAcceleration += _motorTorque * MathF.Tau * Transform.Rotation.Forward;

		targetAcceleration.x = targetAcceleration.x.MeterToInch();
		targetAcceleration.y = targetAcceleration.y.MeterToInch();
		targetAcceleration.z = targetAcceleration.z.MeterToInch();

		var force = targetAcceleration * Time.Delta;
		AngleVelocity = hitContactVelocity.Dot( hitForwardDirection ) * MathF.Tau / WheelRadius;

		_rigidbody.ApplyImpulseAt( GameObject.Transform.Position, force );
	}
	private static float CalculateSlip( Vector3 velocity, Vector3 direction, float speed )
	{
		return Vector3.Dot( velocity, direction ) / (speed + 0.01f);
	}

	private static Vector3 CalculateFrictionForce( WheelFrictionInfo friction, float slip, Vector3 direction )
	{
		return -friction.Evaluate( MathF.Abs( slip ) ) * MathF.Sign( slip ) * direction;
	}
	public Vector3 GetCenter()
	{
		return Transform.Position + (Vector3.Down * (groundHit.Distance - (_suspensionTotalLength - maxSuspensionLength)));
	}

	private void UpdateSuspension()
	{
		if ( !IsGrounded )
			return;

		var worldVelocity = _rigidbody.GetVelocityAtPoint( groundHit.Point + _rigidbody.PhysicsBody.LocalMassCenter );

		var localVel = worldVelocity.Dot( groundHit.Normal );

		var suspensionCompression = groundHit.Distance - _suspensionTotalLength;
		var dampingForce = -SuspensionDamping * localVel;
		var springForce = -SuspensionStiffness * suspensionCompression;
		var totalForce = (dampingForce + springForce).MeterToInch() * Time.Delta;

		var suspensionForce = groundHit.Normal * totalForce;
		_rigidbody.ApplyImpulseAt( Transform.Position, suspensionForce );
	}
	public void UpdateStiffness( float forward = 1f, float side = 1f )
	{
		ForwardStiffness = forward;
		SideStiffness = side;
	}

	private void DoTrace()
	{
		var down = _rigidbody.Transform.Rotation.Down;

		var startPos = Transform.Position + down * MinSuspensionLength;
		var endPos = startPos + down * (MaxSuspensionLength + WheelRadius);

		groundHit = new( Scene.Trace
				.Radius( 1f )
				.IgnoreGameObjectHierarchy( GameObject.Root )
				.FromTo( startPos, endPos )
				.Run() );

		if ( groundHit.Distance > WheelRadius )
			groundHit.Hit = false;
		groundHit.Distance = Math.Min( groundHit.Distance, WheelRadius );
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
			Gizmo.Draw.LineCircle( circlePosition, circleAxis, WheelRadius );
		}

		//
		// Forward direction
		//
		{
			var arrowStart = Vector3.Forward * WheelRadius;
			var arrowEnd = arrowStart + Vector3.Forward * 8f;

			Gizmo.Draw.Color = Color.Red;
			Gizmo.Draw.Arrow( arrowStart, arrowEnd, 4, 1 );
		}
	}

}
