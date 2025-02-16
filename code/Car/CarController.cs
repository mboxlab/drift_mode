using Sandbox.Engine;
using Sandbox.Powertrain;
using Sandbox.Powertrain.Modules;
using Sandbox.Tuning;

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
	[Property] public CarTuning.CarCategory Category { get; set; }

	[Property] public WheelCollider[] Wheels { get; private set; }
	[Property, Group( "Wheel Properties" )] public float WheelRadius { get; set; }
	[Property, Group( "Wheel Properties" )] public float WheelWidth { get; set; }

	public bool IsBot;
	public static CarController Local { get; private set; }
	[Rpc.Owner]
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
	[Property, Group( "Wheel Properties" )] public PacejkaPreset FrictionPresetEnum { get; set; } = PacejkaPreset.Asphalt;
	public PacejkaCurve FrictionPreset;

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
		FrictionPreset = PacejkaCurve.GetPreset( FrictionPresetEnum );
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
		SimulateAerodinamics();
		SimulateTCS();
	}

	float CurrentSteerAngle;
	public float VelocityAngle { get; private set; }

	void UpdateSteerAngle()
	{
		float targetSteerAngle = Input.Steering * MaxSteerAngle;

		if ( EnableSteerAngleMultiplier )
			targetSteerAngle *= Math.Clamp( 1 - CurrentSpeed / MaxSpeedForMinAngleMultiplier, MinSteerAngleMultiplier, MaxSteerAngleMultiplier );

		CurrentSteerAngle = MathX.Lerp( CurrentSteerAngle, targetSteerAngle, Time.Delta * 5f );

		VelocityAngle = -Rigidbody.Velocity.SignedAngle( WorldRotation.Left, Vector3.Up );


		//Wheel turn limitation.
		float targetAngle = MathX.Clamp( CurrentSteerAngle, -(MaxSteerAngle + 10), MaxSteerAngle + 10 );

		//Front wheel turn.
		Wheels[0].SteerAngle = targetAngle;
		Wheels[1].SteerAngle = targetAngle;

	}

	#region Aerodinamics
	private class DownforcePoint
	{
		public float MaxForce { get; set; }
		public Vector3 Position { get; set; }
	}
	public const float RHO = 1.225f;
	[Property, Group( "Aerodinamics" )] public Vector3 Dimensions = new( 2f, 4.5f, 1.5f );
	[Property, Group( "Aerodinamics" )] public float DamageDragEffect { get; set; } = 0.5f;
	[Property, Group( "Aerodinamics" )] public float FrontalCd { get; set; } = 0.35f;
	[Property, Group( "Aerodinamics" )] public float SideCd { get; set; } = 1.05f;
	[Property, Group( "Aerodinamics" )] public float MaxDownforceSpeed { get; set; } = 80f;
	[Property, Group( "Aerodinamics" )] private List<DownforcePoint> DownforcePoints { get; set; } = new();

	private float _forwardSpeed;
	private float _frontalArea;

	private float _sideArea;
	private float _sideSpeed;
	private float lateralDragForce;
	private float longitudinalDragForce;

	private void SimulateAerodinamics()
	{
		if ( CurrentSpeed < 1f )
		{
			longitudinalDragForce = 0;
			lateralDragForce = 0;
			return;
		}

		_frontalArea = Dimensions.x * Dimensions.z * 0.85f;
		_sideArea = Dimensions.y * Dimensions.z * 0.8f;
		_forwardSpeed = LocalVelocity.y.InchToMeter();
		_sideSpeed = LocalVelocity.x.InchToMeter();
		longitudinalDragForce = 0.5f * RHO * _frontalArea * FrontalCd * (_forwardSpeed * _forwardSpeed) * (_forwardSpeed > 0 ? -1f : 1f);
		lateralDragForce = 0.5f * RHO * _sideArea * SideCd * (_sideSpeed * _sideSpeed) * (_sideSpeed > 0 ? -1f : 1f);


		Rigidbody.ApplyForce( new Vector3( lateralDragForce.MeterToInch(), longitudinalDragForce.MeterToInch(), 0 ).RotateAround( Vector3.Zero, WorldRotation ) );

		float speedPercent = CurrentSpeed / MaxDownforceSpeed;
		float forceCoeff = 1f - (1f - MathF.Pow( speedPercent, 2f ));

		foreach ( DownforcePoint dp in DownforcePoints )
			Rigidbody.ApplyForceAt( Transform.World.PointToWorld( dp.Position ), forceCoeff.MeterToInch() * dp.MaxForce.MeterToInch() * -WorldRotation.Up );
	}
	#endregion

	#region Traction Control System
	/// <summary>
	///     Traction Control System (TCS) module. Reduces engine throttle when excessive slip is present.
	/// </summary>
	[Property, Group( "Traction Control" )] bool IsSimulateTCS { get; set; } = false;
	[Property, Group( "Traction Control" ), ShowIf( nameof( IsSimulateTCS ), true )] private float SlipThreshold { get; set; } = 0.1f;
	private void SimulateTCS()
	{
		Powertrain.Engine.PowerMultiplayer = 1;
		if ( !IsSimulateTCS )
			return;

		foreach ( WheelComponent wheelComponent in Powertrain.Wheels )
		{
			if ( !wheelComponent.Wheel.IsGrounded || Powertrain.Transmission.IsShifting )
				continue;

			float longSlip = wheelComponent.Wheel.ForwardSlip;
			if ( -longSlip * MathF.Sign( LocalVelocity.y ) > SlipThreshold )
			{
				Powertrain.Engine.PowerMultiplayer = 0.05f;
				return;
			}
		}
	}

	#endregion
}

