using System;

namespace DM.Vehicle;

[Serializable]
/// <summary>
///     Represents single ground ray hit.
/// </summary>
public struct WheelHit
{
	/// <summary>
	/// Collider that was hit. If no hit, null.
	/// </summary>
	public Collider Collider;

	/// <summary>
	///     The normal at the point of contact
	/// </summary>

	public Vector3 Normal;

	/// <summary>
	///     The point of contact between the wheel and the ground.
	/// </summary>

	public Vector3 Point;
}
