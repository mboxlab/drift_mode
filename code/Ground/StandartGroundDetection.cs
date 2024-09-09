using System;
using System.Diagnostics;
using System.Drawing;
using System.Dynamic;
using System.Numerics;
using System.Reflection.Emit;
using DM.Vehicle;
using Sandbox;
using static Sandbox.VertexLayout;

namespace DM.Ground;
[Category( "Vehicles" )]
public sealed class StandardGroundDetection : GroundDetection, Component.ITriggerListener
{
	private WheelCastResult _nearestHit;
	private WheelCastResult _wheelCastResult;
	private GameTransform _transform;
	private struct WheelCastInfo
	{
		public WheelCastInfo( SceneTraceResult trace, float distance )
		{
			this.distance = distance;
			this.trace = trace;
		}

		public WheelCastInfo( SceneTraceResult trace, Vector3 origin, Vector3 direction, float distance, float radius, float width )
		{
			this.trace = trace;
			this.origin = origin;
			this.direction = direction;
			this.distance = distance;
			this.radius = radius;
			this.width = width;
		}

		public float distance;
		public SceneTraceResult trace;
		public Vector3 origin;
		public Vector3 direction;
		public float radius;
		public float width;
	}
	private struct WheelCastResult
	{
		public WheelCastResult( Vector3 point, Vector3 normal, WheelCastInfo castInfo )
		{
			this.point = point;
			this.normal = normal;
			this.castInfo = castInfo;
		}

		public Vector3 point;
		public Vector3 normal;
		public WheelCastInfo castInfo;
	}
	private readonly List<WheelCastResult> _wheelCastResults = new();
	private readonly List<WheelCastInfo> _wheelCasts = new();

	public override bool Cast( Vector3 origin, Vector3 direction, float distance, float radius, float width, ref WheelHit wheelHit )
	{
		_wheelCastResults.Clear();
		_wheelCasts.Clear();
		_transform = Transform;

		bool isValid = false;

		if ( WheelCastMultiSphere( origin, direction, distance, radius, width, ref _wheelCastResult ) )
		{

			isValid = true;
		}
		//if ( WheelCastSingleSphere( origin, direction, distance, radius, width, ref _wheelCastResult ) )
		//{

		//	isValid = true;
		//}

		if ( isValid )
		{

			wheelHit.Point = _wheelCastResult.point;
			wheelHit.Normal = _wheelCastResult.normal;

			wheelHit.Collider = _wheelCastResult.castInfo.trace.Component.Components.Get<Collider>();
		}

		return isValid;
	}

	protected override void DrawGizmos()
	{
		//if ( !Gizmo.IsSelected )
		//	return;
		Gizmo.Draw.IgnoreDepth = true;
		Gizmo.Transform = Scene.Transform.World;
		//Gizmo.Draw.Color = Color.Red;
		//Gizmo.Draw.LineThickness = 3f;
		//Gizmo.Draw.Line( _castResult.StartPosition, _castResult.EndPosition );

		Gizmo.Draw.Color = Color.Cyan;
		foreach ( WheelCastInfo wheelCast in _wheelCasts )
		{
			//Gizmo.Draw.LineSphere( Transform.World.PointToLocal( wheelCast.origin ), wheelCast.width );
			Gizmo.Draw.Line( wheelCast.origin, wheelCast.origin + wheelCast.direction * wheelCast.distance );
		}

		foreach ( WheelCastResult result in _wheelCastResults )
		{

			bool isInsideWheel = IsInsideWheel( result.point, result.castInfo.origin,
				result.castInfo.radius, result.castInfo.width );

			Gizmo.Draw.Color = isInsideWheel ? Color.Green : Color.Yellow;
			//Gizmo.Draw.LineSphere( result.castInfo.trace.HitPosition, result.castInfo.width / 2 );
			Gizmo.Draw.LineSphere( result.point, 0.1f );
			Gizmo.Draw.Line( result.point, result.point + result.normal * result.castInfo.distance );
		}
	}
	private bool SphereCast( Vector3 origin, float width, Vector3 direction, out SceneTraceResult hit, in float distance )
	{
		SceneTraceResult ray = Scene.Trace
				.Radius( width )
				.IgnoreGameObjectHierarchy( GameObject.Root )
				.FromTo( origin, origin + direction * distance )
				.Run();
		hit = ray;

		return ray.Hit;
	}

	private bool WheelCastSingleSphere( in Vector3 origin, in Vector3 direction, in float distance, in float radius, in float width, ref WheelCastResult hit )
	{

		var ishit = SphereCast( origin, width, direction, out SceneTraceResult castHit, distance );

		WheelCastInfo castInfo = new( castHit, origin, direction, distance, radius, width );
		_wheelCasts.Add( castInfo );

		if ( ishit )
		{
			hit = new( castHit.HitPosition, castHit.Normal, castInfo );
			_wheelCastResults.Add( hit );

			return true;
		}

		return false;
	}

	private bool WheelCastMultiSphere( in Vector3 origin, in Vector3 direction, in float distance, in float radius, in float width, ref WheelCastResult hit )
	{
		float nearestDistance = 1e10f;

		float castRadius = width * 0.5f;
		float zRange = 2f * radius;

		int xSteps = 11;

		xSteps = (int)Math.Round( (radius / castRadius) * 2f );
		xSteps = xSteps % 2 == 0 ? xSteps + 1 : xSteps; // Ensure there is always a center sphere (odd number of spheres).


		float stepAngle = 180f / (xSteps - 1);

		Vector3 up = _transform.Rotation.Up;
		Vector3 right = _transform.Rotation.Right;
		Vector3 forwardOffset = _transform.Rotation.Forward * radius;

		Quaternion steerQuaternion = Quaternion.CreateFromAxisAngle( up, MathX.DegreeToRadian( 0 ) );
		Quaternion yStepQuaternion = Quaternion.CreateFromAxisAngle( right, MathX.DegreeToRadian( stepAngle ) );
		Quaternion yRotationQuaternion = Quaternion.Identity;

		for ( int z = 0; z < xSteps; z++ )
		{
			Vector3 castOrigin = origin + (forwardOffset * yRotationQuaternion * steerQuaternion);

			SceneTraceResult castHit;

			bool hasHit = SphereCast( castOrigin, castRadius, direction, out castHit, distance );
			WheelCastInfo castInfo = new( castHit, castOrigin, direction, distance, radius, width );
			_wheelCasts.Add( castInfo );

			if ( hasHit )
			{
				Vector3 pos = castHit.HitPosition; // castHit.Body.FindClosestPoint( castHit.HitPosition );
				Vector3 hitLocalPoint = _transform.World.PointToLocal( pos );
				float sine = hitLocalPoint.z / radius;
				sine = Math.Clamp( sine, -1, 1 );
				float hitAngle = MathF.Asin( sine );
				float potentialWheelPosition = hitLocalPoint.y + radius * MathF.Cos( hitAngle );
				float dist = -potentialWheelPosition;

				WheelCastResult result = new( pos, castHit.Normal, castInfo );

				_wheelCastResults.Add( result );

				if ( castHit.Fraction < nearestDistance )
				{
					nearestDistance = castHit.Fraction;
					_nearestHit = result;

				}
			}

			yRotationQuaternion /= yStepQuaternion;
		}

		if ( nearestDistance < 1e9f )
		{
			//hit = _nearestHit;
			hit = new WheelCastResult( _nearestHit.point, _nearestHit.normal, new WheelCastInfo( _nearestHit.castInfo.trace, nearestDistance ) );
			return true;
		}

		return false;
	}


	private bool IsInsideWheel( in Vector3 point, in Vector3 wheelPos, in float radius, in float width )
	{
		Vector3 offset = point - wheelPos;

		Vector3 localOffset = _transform.World.PointToLocal( point );
		float halfWidth = width * 0.5f;
		if ( localOffset.x >= -halfWidth && localOffset.x <= halfWidth
			&& localOffset.y >= -radius && localOffset.y <= radius )
		{
			return true;
		}

		return false;
	}

}
