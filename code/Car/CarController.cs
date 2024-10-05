
using Sandbox.Engine;
using Sandbox.GamePlay;
using Sandbox.Powertrain.Modules;

namespace Sandbox.Car;

[Category( "Vehicles" )]
public sealed class CarController : Component
{
	[Property] public Rigidbody Rigidbody { get; private set; }
	[Property] public SoundInterpolator SoundInterpolator { get; set; }
	[Property] public WheelCollider[] Wheels { get; private set; }
	[Property] public CarInputHandler Input { get; set; }
	[Property] public Powertrain.Powertrain Powertrain { get; set; }
	[Property, Title("ABS Module")] public ABSModule ABSModule { get; set; }

	public bool IsBot;
	public static CarController Local { get; private set; }
	[Authority]
	public void ClientInit()
	{
		if ( IsBot )
			return;

		Local = this;
	}

	[Property] public float MaxBrakeTorque { get; set; }
	[Property] public float MaxSteerAngle { get; set; }
	[Property] public bool EnableSteerAngleMultiplier { get; set; } = true;


	[Property, ShowIf( nameof( EnableSteerAngleMultiplier ), true )] public float MaxSpeedForMinAngleMultiplier { get; set; } = 100;
	[Property, ShowIf( nameof( EnableSteerAngleMultiplier ), true )] public float MinSteerAngleMultiplier { get; set; } = 0.05f;
	[Property, ShowIf( nameof( EnableSteerAngleMultiplier ), true )] public float MaxSteerAngleMultiplier { get; set; } = 1f;
	[Property] public PacejkaCurve.PresetsEnum FrictionPresetEnum { get => _frictionPresetEnum; set { _frictionPresetEnum = value; OnFrictionChanged(); } }
	public PacejkaCurve.PresetsEnum _frictionPresetEnum { get; set; }
	public PacejkaCurve FrictionPreset { get; set; } = PacejkaCurve.Asphalt;

	/// <summary>
	/// Speed, magnitude of velocity.
	/// </summary>
	public float CurrentSpeed { get; private set; }
	public int CarDirection { get { return CurrentSpeed < 1 ? 0 : (VelocityAngle < 90 && VelocityAngle > -90 ? 1 : -1); } }
	public Vector3 LocalVelocity;

	private IManager GameManager;
	protected override void OnAwake()
	{
		base.OnAwake();

		if ( !IsProxy )
			ClientInit();
	}
	protected override void OnStart()
	{
		SoundInterpolator.MaxValue = Powertrain.Engine.RevLimiterRPM;
		UpdateWheelsFriction();
		GameManager = Scene.Components.Get<IManager>( FindMode.InDescendants );
		var gauge = Scene.Components.Get<Gauge>( FindMode.InDescendants );
		if ( gauge != null )
			gauge.Car = this;

	}
	protected override void OnUpdate()
	{
		LocalVelocity = WorldTransform.PointToLocal( Rigidbody.GetVelocityAtPoint( WorldPosition ) + WorldPosition );
		Input.Update();

		SoundInterpolator.Value = Powertrain.Engine.OutputRPM;
		SoundInterpolator.Volume = 1;
	}
	public event EventHandler<PacejkaCurve> FrictionChanged;
	private void OnFrictionChanged()
	{
		FrictionPreset = PacejkaCurve.Presets[_frictionPresetEnum];
		UpdateWheelsFriction();
		FrictionChanged?.Invoke( this, FrictionPreset );
	}
	private void UpdateWheelsFriction()
	{
		foreach ( WheelCollider wheel in Wheels )
		{
			wheel.FrictionPreset = FrictionPreset;
		}
	}
	protected override void OnFixedUpdate()
	{
		CurrentSpeed = Rigidbody.Velocity.Length.InchToMeter();
		// Reset brakes for this frame
		foreach ( WheelCollider wheel in Wheels )
		{
			wheel.BrakeTorque = Input.InputSwappedBrakes * MaxBrakeTorque;
			if ( ABSModule is not null )
				wheel.BrakeTorque *= ABSModule.BrakeTorqueModifier();

		}

		foreach ( var wheel in Powertrain.Wheels )
			wheel.Wheel.BrakeTorque += (Input.Handbrake * MaxBrakeTorque * 2);

		UpdateSteerAngle();
	}

	float CurrentSteerAngle;
	public float VelocityAngle { get; private set; }
	void UpdateSteerAngle()
	{
		float targetSteerAngle = Input.Steering * MaxSteerAngle;

		if ( EnableSteerAngleMultiplier )
			targetSteerAngle *= Math.Clamp( 1 - CurrentSpeed / MaxSpeedForMinAngleMultiplier, MinSteerAngleMultiplier, MaxSteerAngleMultiplier );

		CurrentSteerAngle = MathX.Lerp( CurrentSteerAngle, targetSteerAngle, Time.Delta * 5f );

		var needHelp = CurrentSpeed > 20 && CarDirection > 0;
		float targetAngle = 0;
		VelocityAngle = -Rigidbody.Velocity.SignedAngle( WorldRotation.Forward, Vector3.Up );
		if ( needHelp )
			targetAngle = VelocityAngle;


		//Wheel turn limitation.
		targetAngle = MathX.Clamp( targetAngle + CurrentSteerAngle, -(MaxSteerAngle + 10), MaxSteerAngle + 10 );

		//Front wheel turn.
		Wheels[0].SteerAngle = targetAngle;
		Wheels[1].SteerAngle = targetAngle;

	}
}
