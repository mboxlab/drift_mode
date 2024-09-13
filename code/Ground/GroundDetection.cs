using DM.Car;
namespace DM.Ground;

/// <summary>
/// Base class for wheel ground detection. 
/// By default StandardGroundDetection is used, but in case it is needed a custom GroundDetection script
/// can be created and attached to the WheelController. It only needs to implement the WheelCast() function
/// and return the point nearest to the wheel center.
/// </summary>
public interface IGroundDetection
{
	public abstract GroundHit Cast( GameObject gameObject, Wheel wheel );
}
