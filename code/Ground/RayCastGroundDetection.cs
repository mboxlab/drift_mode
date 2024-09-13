
using DM.Car;
namespace DM.Ground;

public sealed class RayCastGroundDetection : IGroundDetection
{
	SceneTraceResult _nearestHit;
	private static readonly float RayCount = 20;

	private SceneTraceResult GetNearestHit( GameObject gameObject, Wheel wheel )
	{
		_nearestHit.Hit = false;
		GameTransform transform = gameObject.Transform;

		Vector3 startPos = transform.Position;

		float nearestDistance = float.MaxValue;

		SceneTrace traceBuilder = Game.ActiveScene.Trace
					.Radius( wheel.Width * 0.5f )
					.IgnoreGameObjectHierarchy( gameObject.Root );

		Rotation rotation = wheel.Rigidbody.Transform.Rotation;
		Vector3 down = wheel.Rigidbody.Transform.Rotation.Down;
		Rotation up = Rotation.FromAxis( rotation.Up, transform.LocalRotation.Yaw() );
		float additionalLength = (wheel.Spring.MaxLength * 0.5f + wheel.Radius);

		for ( float i = 0; i < RayCount; i++ )
		{

			Rotation right = Rotation.FromAxis( rotation.Right, 70 - (i / RayCount) * 140 );

			Vector3 endPos = startPos + down.RotateAround( Vector3.Zero, right ).RotateAround( Vector3.Zero, up ) * additionalLength;
			SceneTraceResult trace = traceBuilder.FromTo( startPos, endPos ).Run();

			if ( trace.Hit )
			{
				var frac = Vector3.DistanceBetweenSquared( trace.StartPosition, trace.HitPosition );
				if ( frac <= nearestDistance )
				{
					nearestDistance = frac;
					_nearestHit = trace;
				}
			}
		}
		return _nearestHit;
	}

	public GroundHit Cast( GameObject gameObject, Wheel wheel )
	{
		return new( GetNearestHit( gameObject, wheel ) );
	}
}
