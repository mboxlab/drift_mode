using System;
using DM.Car;
using DM.Ground;
namespace Sandbox.Car;

public sealed class WheelCollider : Stereable
{
	[Property] public float MinSuspensionLength { get => minSuspensionLength; set { minSuspensionLength = value; UpdateTotalSuspensionLength(); } }
	[Property] public float MaxSuspensionLength { get => maxSuspensionLength; set { maxSuspensionLength = value; UpdateTotalSuspensionLength(); } }
	[Property] public float SuspensionStiffness { get; set; } = 3000.0f;
	[Property] public float SuspensionDamping { get; set; } = 140.0f;
	[Property] public float WheelRadius { get => wheelRadius; set { wheelRadius = value; UpdateTotalSuspensionLength(); } }
	[Property] public WheelFrictionInfo ForwardFriction { get; set; }
	[Property] public WheelFrictionInfo SideFriction { get; set; }

	private const float LowSpeedThreshold = 32.0f;

	public bool IsGrounded => groundHit.Hit;

	public float SteerAngle { get; set; }
	public float ForwardSlip { get; private set; }
	public float SideSlip { get; private set; }

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
		if ( !IsGrounded )
			return;

		var hitContactVelocity = _rigidbody.GetVelocityAtPoint( groundHit.Point + _rigidbody.PhysicsBody.LocalMassCenter );

		var hitForwardDirection = groundHit.Normal.Cross( Transform.Rotation.Right ).Normal;
		var hitSidewaysDirection = Rotation.FromAxis( groundHit.Normal, 90f ) * hitForwardDirection;

		var wheelSpeed = hitContactVelocity.Length;

		SideSlip = CalculateSlip( hitContactVelocity, hitSidewaysDirection, wheelSpeed );
		ForwardSlip = CalculateSlip( hitContactVelocity, hitForwardDirection, wheelSpeed );

		var sideForce = CalculateFrictionForce( SideFriction, SideSlip, hitSidewaysDirection );
		var forwardForce = CalculateFrictionForce( ForwardFriction, ForwardSlip, hitForwardDirection );

		float factor = wheelSpeed.LerpInverse( 0f, LowSpeedThreshold );
		float groundFriction = groundHit.Surface.Friction;

		var targetAcceleration = (sideForce + forwardForce) * factor * groundFriction;
		targetAcceleration += _motorTorque * Transform.Rotation.Forward;

		var force = targetAcceleration / Time.Delta;

		_rigidbody.ApplyForceAt( GameObject.Transform.Position, force );
	}
	private float CalculateSlip( Vector3 velocity, Vector3 direction, float speed )
	{
		var epsilon = 0.01f;
		return Vector3.Dot( velocity, direction ) / (speed + epsilon);
	}

	private Vector3 CalculateFrictionForce( WheelFrictionInfo friction, float slip, Vector3 direction )
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

		var worldVelocity = _rigidbody.GetVelocityAtPoint( Transform.Position );

		var localVel = worldVelocity.Dot( groundHit.Normal );

		var suspensionCompression = groundHit.Distance - _suspensionTotalLength;
		var dampingForce = -SuspensionDamping * localVel;
		var springForce = -SuspensionStiffness * suspensionCompression;
		var totalForce = (dampingForce + springForce) / Time.Delta;

		var suspensionForce = groundHit.Normal * totalForce;
		_rigidbody.ApplyForceAt( Transform.Position, suspensionForce );
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
