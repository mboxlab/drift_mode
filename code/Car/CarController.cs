
using Sandbox.Engine;
using Sandbox.Powertrain.Modules;
using Sandbox.Tuning;
using Sandbox.Utils;

namespace Sandbox.Car;

[Category( "Vehicles" )]
public sealed class CarController : Component
{
	[Property] public string Name { get; set; }
	[Property] public TuningContainer TuningContainer { get; set; } = new();
	[Property] public SkinnedModelRenderer BodyRenderer { get; set; }
	[Property] public Rigidbody Rigidbody { get; private set; }
	[Property] public SoundInterpolator SoundInterpolator { get; set; }
	[Property] public CarInputHandler Input { get; set; }
	[Property] public Powertrain.Powertrain Powertrain { get; set; }
	[Property, Title( "ABS Module" )] public ABSModule ABSModule { get; set; }

	[Property] public WheelCollider[] Wheels { get; private set; }
	[Property, Group( "Wheel Properties" )] public float WheelRadius { get; set; }
	[Property, Group( "Wheel Properties" )] public float WheelWidth { get; set; }

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
	[Property, Group( "Steering" )] public float MaxSteerAngle { get; set; }
	[Property, Group( "Steering" )] public bool EnableSteerAngleMultiplier { get; set; } = true;
	[Property, Group( "Steering" ), ShowIf( nameof( EnableSteerAngleMultiplier ), true )] public float MaxSpeedForMinAngleMultiplier { get; set; } = 100;
	[Property, Group( "Steering" ), ShowIf( nameof( EnableSteerAngleMultiplier ), true )] public float MinSteerAngleMultiplier { get; set; } = 0.05f;
	[Property, Group( "Steering" ), ShowIf( nameof( EnableSteerAngleMultiplier ), true )] public float MaxSteerAngleMultiplier { get; set; } = 1f;
	[Property, Group( "Wheel Properties" )] public PacejkaCurve.PresetsEnum FrictionPresetEnum { get; set; } = PacejkaCurve.PresetsEnum.Asphalt;
	public PacejkaCurve FrictionPreset { get; set; }

	/// <summary>
	/// Speed, magnitude of velocity.
	/// </summary>
	public float CurrentSpeed { get; private set; }
	public int CarDirection { get { return CurrentSpeed < 1 ? 0 : (VelocityAngle < 90 && VelocityAngle > -90 ? 1 : -1); } }
	public Vector3 LocalVelocity;
	protected override void OnAwake()
	{
		base.OnAwake();


		if ( !IsProxy )
			ClientInit();
	}
	public void UpdateCarProperties()
	{
		UpdateWheelsProperties();
		OnCarPropertiesChanged?.Invoke();
	}
	public Action OnCarPropertiesChanged;

	private void UpdateWheelsProperties() => UpdateWheelsProperties( WheelRadius, WheelWidth );
	private void UpdateWheelsProperties( float radius, float width )
	{
		foreach ( WheelCollider wheel in Wheels )
		{
			if ( radius > 0 ) wheel.Radius = radius;
			if ( width > 0 ) wheel.Width = width;
		}
	}

	private bool IsEnabled = true;
	public void Disable()
	{
		base.OnEnabled();

		Powertrain.Engine.Enabled = false;
		SoundInterpolator.Enabled = false;
		IsEnabled = false;
	}
	public void Enable()
	{
		base.OnEnabled();

		Powertrain.Engine.Enabled = true;
		SoundInterpolator.Enabled = true;
		IsEnabled = true;
	}

	protected override void OnEnabled()
	{
		base.OnEnabled();

		UpdateCarProperties();
	}

	protected override void OnStart()
	{
		SoundInterpolator.MaxValue = Powertrain.Engine.RevLimiterRPM;
		OnFrictionChanged();
		var gauge = Scene.Components.Get<Gauge>( FindMode.InDescendants );
		if ( gauge != null )
			gauge.Car = this;

	}
	protected override void OnUpdate()
	{
		if ( !IsEnabled )
			return;
		LocalVelocity = WorldTransform.PointToLocal( Rigidbody.GetVelocityAtPoint( WorldPosition ) + WorldPosition );
		Input.Update();

		SoundInterpolator.Value = Powertrain.Engine.OutputRPM;
		SoundInterpolator.Volume = 1;
	}
	public event EventHandler<PacejkaCurve> FrictionChanged;
	private void OnFrictionChanged()
	{
		FrictionPreset = PacejkaCurve.Presets[FrictionPresetEnum];
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
		VelocityAngle = -Rigidbody.Velocity.SignedAngle( WorldRotation.Left, Vector3.Up );
		if ( needHelp )
			targetAngle = VelocityAngle * 0.8f;


		//Wheel turn limitation.
		targetAngle = MathX.Clamp( targetAngle + CurrentSteerAngle, -(MaxSteerAngle + 10), MaxSteerAngle + 10 );

		//Front wheel turn.
		Wheels[0].SteerAngle = targetAngle;
		Wheels[1].SteerAngle = targetAngle;

	}

}
