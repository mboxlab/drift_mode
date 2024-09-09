using DM.Vehicle;

namespace DM.Ground;

/// <summary>
/// Base class for wheel ground detection. 
/// By default StandardGroundDetection is used, but in case it is needed a custom GroundDetection script
/// can be created and attached to the WheelController. It only needs to implement the WheelCast() function
/// and return the point nearest to the wheel center.
/// </summary>
public abstract class GroundDetection : Component
{
	/// <summary>
	/// 
	/// </summary>
	/// <param name="origin">Origin of the wheel cast.</param>
	/// <param name="direction">Direction of the wheel cast.</param>
	/// <param name="distance">Distance the cast will travel.</param>
	/// <param name="radius">Radius of the wheel.</param>
	/// <param name="width">Width of the wheel.</param>
	/// <param name="wheelHit">result of cast</param>
	/// 
	public abstract bool Cast( Vector3 origin, Vector3 direction, float distance, float radius, float width, ref WheelHit wheelHit );
}
