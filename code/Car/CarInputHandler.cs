
namespace Sandbox.Car;

public partial class CarInputHandler
{

	[Property] public CarController CarController { get; set; }

	/// <summary>
	///     Swaps throttle and brake axes when vehicle is in reverse.
	/// </summary>
	[Property] public bool SwapInputInReverse { get; set; } = true;

	/// <summary>
	///     When enabled input will be auto-retrieved from the InputProviders present in the scene.
	///     Disable to manually set the input through external scripts, i.e. AI controller.
	/// </summary>
	[Property] public bool AutoSetInput { get; set; } = true;
	/// <summary>
	///     All the input states of the vehicle. Can be used to set input through scripting or copy the inputs
	///     over from other vehicle, such as truck to trailer.
	/// </summary>
	[NonSerialized] public VehicleInputStates states;

	/// <summary>
	///     Convenience function for setting throttle/brakes as a single value.
	///     Use Throttle/Brake axes to apply throttle and braking separately.
	///     If the set value is larger than 0 throttle will be set, else if value is less than 0 brake axis will be set.
	/// </summary>
	public float Vertical
	{
		get { return states.Throttle - states.Brakes; }
		set
		{
			float clampedValue = Math.Clamp( value, 0, 1 );

			if ( value > 0 )
			{
				states.Throttle = clampedValue;
				states.Brakes = 0;
			}
			else
			{
				states.Throttle = 0;
				states.Brakes = -clampedValue;
			}
		}
	}

	/// <summary>
	///     Throttle axis.
	///     For combined throttle/brake input (such as prior to v1.0.1) use 'Vertical' instead.
	/// </summary>
	public float Throttle
	{
		get { return states.Throttle; }
		set
		{
			states.Throttle = Math.Clamp( value, 0, 1 );
		}
	}

	/// <summary>
	///     Brake axis.
	///     For combined throttle/brake input use 'Vertical' instead.
	/// </summary>
	public float Brakes
	{
		get { return states.Brakes; }
		set
		{
			states.Brakes = Math.Clamp( value, 0, 1 );
		}
	}
	/// <summary>
	///     Returns throttle or brake input based on 'swapInputInReverse' setting and current gear.
	///     If swapInputInReverse is true, brake will act as throttle and vice versa while driving in reverse.
	/// </summary>
	public float InputSwappedThrottle
	{
		get { return _inputSwappedThrottle; }
	}

	private float _inputSwappedThrottle;


	/// <summary>
	///     Returns throttle or brake input based on 'swapInputInReverse' setting and current gear.
	///     If swapInputInReverse is true, throttle will act as brake and vise versa while driving in reverse.
	/// </summary>
	public float InputSwappedBrakes
	{
		get { return _inputSwappedBrakes; }
	}

	private float _inputSwappedBrakes;



	/// <summary>
	///     Steering axis.
	/// </summary>
	public float Steering
	{
		get { return states.Steering; }
		set
		{
			states.Steering = Math.Clamp( value, -1, 1 );
		}
	}
	/// <summary>
	///     Clutch axis.
	/// </summary>
	public float Clutch
	{
		get { return states.Clutch; }
		set
		{
			states.Clutch = Math.Clamp( value, 0, 1 );
		}
	}

	public bool EngineStartStop
	{
		get { return states.EngineStartStop; }
		set { states.EngineStartStop = value; }
	}

	public float Handbrake
	{
		get { return states.Handbrake; }
		set
		{
			states.Handbrake = Math.Clamp( value, 0, 1 );
		}
	}

	public bool ShiftDown
	{
		get { return states.ShiftDown; }
		set { states.ShiftDown = value; }
	}

	public bool ShiftUp
	{
		get { return states.ShiftUp; }
		set { states.ShiftUp = value; }
	}

	public bool Boost
	{
		get { return states.Boost; }
		set { states.Boost = value; }
	}

	public bool FlipOver
	{
		get { return states.FlipOver; }
		set { states.FlipOver = value; }
	}

	/// <summary>
	///     True when throttle and brake axis are swapped.
	/// </summary>
	public bool IsInputSwapped
	{
		get { return SwapInputInReverse && CarController.Powertrain.Transmission.Gear < 0; }
	}


	public void Update()
	{
		if ( !AutoSetInput )
		{
			CalculateInputSwappedValues();
			return;
		}

		Throttle = MathF.Max( Input.GetAnalog( InputAnalog.RightTrigger ), Input.AnalogMove.x );
		Brakes = MathF.Max( Input.GetAnalog( InputAnalog.LeftTrigger ), -Input.AnalogMove.x );
		Handbrake = Input.Down( "HandBrake" ) ? 1 : 0;

		Steering = Input.AnalogMove.y;

		Clutch = (Input.Down( "Clutch" ) || Input.Down( "HandBrake" )) ? 1 : 0;

		ShiftUp |= Input.Pressed( "Attack1" );
		ShiftDown |= Input.Pressed( "Attack2" );
		CalculateInputSwappedValues();
	}
	public void ResetShiftFlags()
	{
		states.ShiftUp = false;
		states.ShiftDown = false;
		states.ShiftInto = -999;
	}
	private void CalculateInputSwappedValues()
	{
		bool isInputSwapped = IsInputSwapped;
		_inputSwappedThrottle = isInputSwapped ? Brakes : Throttle;
		_inputSwappedBrakes = isInputSwapped ? Throttle : Brakes;
	}
}
