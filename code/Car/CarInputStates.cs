
namespace Sandbox.Car;

/// <summary>
///     Struct for storing input states of the vehicle.
///     Allows for input to be copied between the vehicles.
/// </summary>
[Serializable]
public struct VehicleInputStates
{
	[Range( -1f, 1f )]
	[NonSerialized]
	public float Steering;

	[Range( 0, 1f )]
	[NonSerialized]
	public float Throttle;

	[Range( 0, 1f )]
	[NonSerialized]
	public float Brakes;

	[Range( 0f, 1f )]
	[NonSerialized]
	public float Clutch;

	[NonSerialized]
	public bool EngineStartStop;

	[Range( 0f, 1f )]
	[NonSerialized]
	public float Handbrake;

	[NonSerialized]
	public bool ShiftDown;

	[NonSerialized]
	public int ShiftInto;

	[NonSerialized]
	public bool ShiftUp;

	[NonSerialized]
	public bool Boost;

	[NonSerialized]
	public bool FlipOver;

	public void Reset()
	{
		Steering = 0;
		Throttle = 0;
		Clutch = 0;
		Handbrake = 0;
		ShiftInto = -999;
		ShiftUp = false;
		ShiftDown = false;
		EngineStartStop = false;
		Boost = false;
		FlipOver = false;
	}
}
