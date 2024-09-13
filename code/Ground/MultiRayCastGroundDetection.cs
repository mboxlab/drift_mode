
using System;
using DM.Car;
using DM.Ground;
namespace DM.Ground;

public sealed class MultiRayCastGroundDetection : IGroundDetection
{
	SceneTraceResult _nearestHit;
	private SceneTrace TraceBuilder;
	private SceneTraceResult Trace;

	private void SphereCast( in Vector3 castOrigin, in float castRadius, in Vector3 direction, in float distance, GameObject root )
	{
		TraceBuilder = Game.ActiveScene.Trace
			.Radius( castRadius )
			.IgnoreGameObjectHierarchy( root );

		Trace = TraceBuilder.FromTo( castOrigin, castOrigin + direction * distance ).Run();

	}
	private GroundHit GetGroundHit( GameObject gameObject, Wheel wheel )
	{
		//in Vector3 origin, in Vector3 direction, in float distance, in float radius, in float width 
		float nearestDistance = 1e10f;
		_nearestHit.Hit = false;
		float width = wheel.Width;
		float radius = wheel.Radius;

		//float offset = wheel.Radius * 1.1f;

		bool hasSuspension = wheel.Spring.MaxLength > 0f;
		float offset = hasSuspension ? radius * 1.1f : radius * 0.1f;
		float length = hasSuspension ? radius * 2.2f + wheel.Spring.MaxLength : radius * 0.02f + offset;

		float distance = radius * 2.2f + wheel.Spring.MaxLength + offset;


		Vector3 origin = gameObject.Transform.Position + gameObject.Transform.Rotation.Up * offset;

		Vector3 direction = gameObject.Transform.Rotation.Down;

		float castRadius = width * 0.5f;

		int zSteps = 21;
		//zSteps = zSteps % 2 == 0 ? zSteps + 1 : zSteps; // Ensure there is always a center sphere (odd number of spheres).

		float stepAngle = 180f / (zSteps - 1);
		var _transform = gameObject.Transform;

		Vector3 up = _transform.Rotation.Up;
		Vector3 left = _transform.Rotation.Left;
		Vector3 forwardOffset = _transform.Rotation.Forward * wheel.Radius;

		Rotation steerRotation = Rotation.FromAxis( up, wheel.SteerAngle );
		Rotation xStepRotation = Rotation.FromAxis( left, stepAngle );
		Rotation xRotation = Rotation.Identity;

		for ( int z = 0; z < zSteps; z++ )
		{
			Vector3 castOrigin = origin + steerRotation * xRotation * forwardOffset;

			SphereCast( castOrigin, castRadius, direction, distance, gameObject.Root );

			if ( Trace.Hit )
			{
				Vector3 hitLocalPoint = _transform.World.PointToLocal( Trace.HitPosition );
				float sine = hitLocalPoint.x / radius;
				sine = sine < -1f ? -1f : sine > 1f ? 1f : sine;
				float hitAngle = MathF.Asin( sine );
				float potentialWheelPosition = hitLocalPoint.z + radius * MathF.Cos( hitAngle );
				float dist = -potentialWheelPosition;

				if ( dist < nearestDistance )
				{
					nearestDistance = dist;
					_nearestHit = Trace;
				}
			}

			xRotation *= xStepRotation;
		}

		return new( _nearestHit );
	}
	public GroundHit Cast( GameObject gameObject, Wheel wheel )
	{
		return GetGroundHit( gameObject, wheel );
	}
}
