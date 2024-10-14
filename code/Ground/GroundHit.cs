namespace Sandbox.Ground;

[Serializable]
/// <summary>
///     Represents single ground ray hit.
/// </summary>
public struct GroundHit
{

	public GroundHit( SceneTraceResult ray ) : this()
	{
		Normal = ray.Normal;
		if ( ray.HitPosition == Vector3.Zero )
			Point = ray.EndPosition;
		else
			Point = ray.HitPosition;

		Surface = ray.Surface;
		StartPosition = ray.StartPosition;
		EndPosition = ray.EndPosition;
		HitPosition = ray.HitPosition;
		Hit = ray.Hit;
		Distance = HitPosition.Distance( ray.StartPosition );
		Body = ray.Body;
		//Collider = ray.GameObject.Components?.Get<Collider>();
	}

	/// <summary>
	/// Collider that was hit.
	/// </summary>
	public Collider Collider { get; }

	public PhysicsBody Body { get; }

	/// <summary>
	///     The normal at the point of contact.
	/// </summary>
	public Vector3 Normal { get; }

	/// <summary>
	///     The point of contact between the wheel and the ground.
	/// </summary>
	public Vector3 Point { get; }
	public Surface Surface { get; }
	public Vector3 StartPosition { get; }
	public Vector3 EndPosition { get; }
	public Vector3 HitPosition { get; }
	public bool Hit { get; set; } = false;
	public float Distance { get; set; }
}
