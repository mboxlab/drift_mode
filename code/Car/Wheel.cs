#define DM_DEBUG

using DM.Vehicle;
using System;
using System.Numerics;

namespace DM.Car;

[Category( "Vehicles" )]
[Title( "Car Wheel" )]
[Icon( "sync" )]
public partial class Wheel : Component
{

	//[Property] public float MinSuspensionLength { get; set; } = 0f;
	//[Property] public float MaxSuspensionLength { get; set; } = 8f;
	//[Property] public float SuspensionStiffness { get; set; } = 3000.0f;
	//[Property] public float SuspensionDamping { get; set; } = 140.0f;

	public bool IsGrounded => _groundTrace.Hit;

	private const float LowSpeedThreshold = 32.0f;

	private SceneTraceResult _groundTrace;
	private Rigidbody _rigidbody;
	private float _motorTorque;
	private Vector3 _hitLocalPoint;


	protected override void OnEnabled()
	{
		_rigidbody = Components.GetInAncestorsOrSelf<Rigidbody>();
	}

	protected override void OnFixedUpdate()
	{
		if ( !_rigidbody.IsValid() )
			return;

		DoTrace();
		DoMultiTrace();
		if ( IsProxy )
			return;

		UpdateSuspension();
		UpdateWheelForces();
	}

	public void ApplyMotorTorque( float value )
	{
		_motorTorque = value;
	}

	private void UpdateWheelForces()
	{
		if ( !IsGrounded )
			return;
		var forwardDir = Transform.Rotation.Forward;

		var sideDir = Transform.Rotation.Right;
		var wheelVelocity = _rigidbody.GetVelocityAtPoint( Transform.Position );
		var wheelSpeed = wheelVelocity.Length;

		var sideForce = Vector3.Zero;
		var forwardForce = Vector3.Zero;

		float sideSlip = CalculateSlip( wheelVelocity, sideDir, wheelSpeed );
		float forwardSlip = CalculateSlip( wheelVelocity, forwardDir, wheelSpeed );

		sideForce = SideFriction.PeakSlip * CalculateFrictionForce( SideFriction, sideSlip, sideDir );
		forwardForce = ForwardFriction.PeakSlip * CalculateFrictionForce( ForwardFriction, forwardSlip, forwardDir );

		float factor = wheelSpeed.LerpInverse( 0f, LowSpeedThreshold );
		float groundFriction = _groundTrace.Surface.Friction;

		var targetAcceleration = (sideForce + forwardForce) * factor * groundFriction;
		targetAcceleration += _motorTorque * Transform.Rotation.Forward;

		var force = targetAcceleration / Time.Delta;
		_rigidbody.ApplyForceAt( GameObject.Transform.Position, force );
	}

	private float CalculateSlip( Vector3 velocity, Vector3 direction, float speed )
	{
		var epsilon = 0.01f; // to avoid division by zero
		return Vector3.Dot( velocity, direction ) / (speed + epsilon);
	}

	private Vector3 CalculateFrictionForce( FrictionPreset friction, float slip, Vector3 direction )
	{
		return -friction.Curve.Evaluate( MathF.Abs( slip ) ) * MathF.Sign( slip ) * direction;
	}

	public Vector3 GetCenter()
	{
		var up = _hitLocalPoint.z - Radius;
		return Transform.Position.WithZ( Transform.Position.z - up );
	}
	public Vector3 GetLocalCenter()
	{
		var up = _hitLocalPoint.z - Radius;
		return -up;
	}

	private void UpdateSuspension()
	{
		if ( !IsGrounded )
			return;

		var worldVelocity = _rigidbody.GetVelocityAtPoint( Transform.Position ).RotateAround( Vector3.Zero, _rigidbody.Transform.Rotation.Conjugate );
		var suspensionCompression = (_groundTrace.Distance + Width / 2) - (Spring.MaxLength + Radius);

		var dampingForce = -Damper.BumpRate * worldVelocity.z;
		var springForce = -Spring.MaxForce * ((suspensionCompression + 0.01f) / Spring.MaxLength);
		var totalForce = (dampingForce + springForce) / Time.Delta;

		var suspensionForce = Transform.Rotation.Up * totalForce;
		_rigidbody.ApplyForceAt( Transform.Position, suspensionForce );

		//var worldVelocity = _rigidbody.GetVelocityAtPoint( Transform.Position ).RotateAround( Vector3.Zero, _rigidbody.Transform.Rotation.Conjugate );
		//var suspensionCompression = (_groundTrace.Distance + Width / 2) - (Spring.MaxLength + Radius);


		//var dampingForce = -Damper.CalculateDamperForce( worldVelocity.z );
		//var springForce = Spring.MaxForce * Spring.ForceCurve.Evaluate( -suspensionCompression / Spring.MaxLength + 0.01f );

		//var totalForce = (dampingForce + springForce) / Time.Delta;
		//var suspensionForce = Vector3.Up * totalForce;

		//_rigidbody.ApplyForceAt( Transform.Position, suspensionForce );
	}

	private void DoTrace()
	{
		var down = _rigidbody.Transform.Rotation.Down;

		var startPos = Transform.Position;
		var endPos = startPos + down * (Spring.MaxLength + Radius);

		_groundTrace = Scene.Trace
				.Radius( 1f )
				.IgnoreGameObjectHierarchy( GameObject.Root )
				.WithoutTags( "car" )
				.FromTo( startPos, endPos )
				.Run();
		if ( _groundTrace.Hit )
			_hitLocalPoint = new Vector3( 0, 0, (_groundTrace.Distance + Width / 2) );

	}
#if DM_DEBUG
	private List<SceneTraceResult> sceneTraceResults = new();
#endif
	private void DoMultiTrace()
	{
#if DM_DEBUG
		sceneTraceResults.Clear();
#endif
		var down = _rigidbody.Transform.Rotation.Down;

		var startPos = Transform.Position;

		SceneTraceResult _nearestHit = _groundTrace;
		float nearestDistance = float.MaxValue;

		for ( float i = 0; i < 45; i++ )
		{

			Rotation right = Rotation.FromAxis( _rigidbody.Transform.Rotation.Right, 70 - i / 45 * 140 );
			Rotation up = Rotation.FromAxis( _rigidbody.Transform.Rotation.Up, Transform.LocalRotation.Yaw() );

			var endPos = startPos + down.RotateAround( Vector3.Zero, right ).RotateAround( Vector3.Zero, up ) * (Spring.MaxLength / 2 + Radius);

			var trace = Scene.Trace
						.Radius( Width / 2 )
						.IgnoreGameObjectHierarchy( GameObject.Root )
						.WithoutTags( "car" )
						.FromTo( startPos, endPos )
						.Run();
			if ( trace.Hit )
			{

				if ( trace.Fraction <= nearestDistance )
				{
					nearestDistance = trace.Fraction;
					_nearestHit = trace;

				}
			}
#if DM_DEBUG
			sceneTraceResults.Add( trace );
#endif
		}

		_groundTrace = _nearestHit;

		if ( _groundTrace.Hit )
			_hitLocalPoint = new Vector3( 0, 0, (_groundTrace.Distance + Width / 2) );
	}

	protected override void DrawGizmos()
	{
#if !DM_DEBUG
		if ( !Gizmo.IsSelected )
			return;
#endif

		Gizmo.Draw.IgnoreDepth = true;

		// 
		//	Collider visual
		//	

		var circlePosition = Game.IsPlaying ? Transform.World.PointToLocal( GetCenter() ) : Vector3.Zero;
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
			Gizmo.Draw.LineThickness = 3.0f;
			Gizmo.Draw.Color = Color.Green;
			Gizmo.Draw.Line( circlePosition, -_hitLocalPoint );
			Gizmo.Draw.Color = Color.White;

			Gizmo.Draw.Line( -_hitLocalPoint, -_hitLocalPoint + _groundTrace.Normal * Radius );
			Gizmo.Draw.Color = IsGrounded ? Color.Orange : Color.Red;
			Gizmo.Draw.LineSphere( -_hitLocalPoint, 0.04f );

		}
		//
		// Suspension length
		//
		{
			var suspensionStart = Vector3.Zero;

			var suspensionEnd = Vector3.Zero + Vector3.Down * Spring.MaxLength;

			Gizmo.Draw.Color = Color.Cyan;
			Gizmo.Draw.LineThickness = 0.25f;

			Gizmo.Draw.Line( suspensionStart, suspensionEnd );
			Gizmo.Draw.Line( suspensionStart + Vector3.Forward, suspensionStart + Vector3.Backward );
			Gizmo.Draw.Line( suspensionEnd + Vector3.Forward, suspensionEnd + Vector3.Backward );
		}

		//
		// Forward Arrow
		//
		{
			var arrowStart = circlePosition + Vector3.Forward * 8f;
			var arrowEnd = arrowStart + Vector3.Forward * 8f;

			Gizmo.Draw.Color = Color.Red;
			Gizmo.Draw.Arrow( arrowStart, arrowEnd, 4, 1 );
		}
		//
		// Forward direction
		//
		{
			Gizmo.Transform = Scene.Transform.World;
			var suspensionStart = _groundTrace.StartPosition;
			var suspensionEnd = _groundTrace.EndPosition;

			Gizmo.Draw.Color = Color.Red;
			Gizmo.Draw.LineThickness = 0.25f;

			Gizmo.Draw.Line( suspensionStart, suspensionEnd );

		}

#if DM_DEBUG
		//
		// Multi tracer 
		//
		{
			Gizmo.Draw.LineThickness = 1.0f;

			foreach ( var item in sceneTraceResults )
			{
				Gizmo.Draw.Line( item.StartPosition, item.EndPosition );
			}
		}
#endif


	}
}
