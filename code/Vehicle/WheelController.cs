
using System;
using System.Collections.Generic;
using DM.Ground;

namespace DM.Vehicle;

[Category( "Vehicles" )]
[Title( "Wheel Controller" )]
[Icon( "sync" )]
public partial class WheelController : WheelAPI
{
	/// <summary>
	///     Rigidbody to which the forces will be applied.
	/// </summary>
	[Property, Group( "Components" )] public Rigidbody Rigidbody { get; set; }

	/// <summary>
	/// Instance of the spring.
	/// </summary>
	[Property, Group( "Components" )] public Spring Spring { get; set; }

	/// <summary>
	/// Instance of the damper.
	/// </summary>
	[Property, Group( "Components" )] public Damper Damper { get; set; }

	/// <summary>
	/// Instance of the wheel.
	/// </summary>
	[Property, Group( "Components" )] public Wheel Wheel { get; set; }

	/// <summary>
	/// Side (lateral) friction info.
	/// </summary>
	//[Property, Group( "Components" )] public Friction SideFriction { get; set; }

	/// <summary>
	/// Forward (longitudinal) friction info.
	/// </summary>
	//[Property, Group( "Components" )] public Friction ForwardFriction { get; set; }


	/// <summary>
	/// Instance of the Ground detection.
	/// </summary>
	[RequireComponent]
	private StandardGroundDetection GroundDetection { get; set; }

	/// <summary>
	///     Contains data about the ground contact point. 
	///     Not valid if !IsGrounded.
	/// </summary>
	[NonSerialized]
	private WheelHit wheelHit;

	/// <summary>
	/// The speed coefficient of the spring / suspension extension when not on the ground.
	/// wheel.perceivedPowertrainInertia.e. how fast the wheels extend when in the air.
	/// The setting of 1 will result in suspension fully extending in 1 second, 2 in 0.5s, 3 in 0.333s, etc.
	/// Recommended value is 6-10.
	/// </summary>
	[Range( 0.01f, 30f )]
	[Property] public float suspensionExtensionSpeedCoeff = 6f;

	private float _dt = Time.Delta;

	private bool _isGrounded;
	private Vector3 _hitLocalPoint;
	private Vector3 _suspensionForce;

	private WheelControllerManager _wheelControllerManager;

	[Property, Group( "Info" )] public override bool IsGrounded { get => _isGrounded; set => _isGrounded = value; }
	//[Property, Group( "Wheel Properties" )] public override float Mass { get => Wheel.Mass; set => Wheel.Mass = value; }
	//[Property, Group( "Wheel Properties" )] public override float Radius { get => Wheel.Radius; set => Wheel.Radius = value; }
	//[Property, Group( "Wheel Properties" )] public override float Width { get => Wheel.Width; set => Wheel.Width = value; }
	//[Property, Group( "Wheel Properties" )] public override float Inertia { get => Wheel.Inertia; set => Wheel.Inertia = value; }


	protected override void DrawGizmos()
	{
		//if ( !Gizmo.IsSelected || Wheel is null )
		//	return;
		Gizmo.Draw.IgnoreDepth = true;

		var circlePosition = Vector3.Zero;

		Gizmo.Draw.LineThickness = 1.0f;
		Gizmo.Draw.Color = Color.Yellow;
		var offset = Vector3.Right * Wheel.Width / 2;
		Gizmo.Draw.LineCylinder( circlePosition - offset, circlePosition + offset, Wheel.Radius, Wheel.Radius, 16 );

		for ( float i = 0; i < 16; i++ )
		{

			var pos = Vector3.Up.RotateAround( Vector3.Zero, new Angles( i / 16 * 360, 0, 0 ) ) * Wheel.Radius;

			Gizmo.Draw.Line( pos - offset, pos + offset );
			var pos2 = Vector3.Up.RotateAround( Vector3.Zero, new Angles( (i + 1) / 16 * 360, 0, 0 ) ) * Wheel.Radius;
			Gizmo.Draw.Line( pos - offset, pos2 + offset );
		}
		//if ( IsGrounded )
		//{
			Gizmo.Draw.LineThickness = 3.0f;
			Gizmo.Draw.Color = Color.Green;
			Gizmo.Draw.Line( circlePosition, _hitLocalPoint );
			Gizmo.Draw.Color = Color.White;

			Gizmo.Draw.Line( _hitLocalPoint, _hitLocalPoint + wheelHit.Normal * Wheel.Radius );
			Gizmo.Draw.Color = IsGrounded ? Color.Orange : Color.Red;
			Gizmo.Draw.LineSphere( _hitLocalPoint, Wheel.Width / 16f );

		//}
	}

	private bool FindTheHitPoint()
	{
		Vector3 origin = 0;
		Vector3 direction = 0;
		float length;

		float offset = Wheel.Radius * 1.1f;
		length = Wheel.Radius + Spring.MaxLength;

		origin.x = Transform.Position.x + Transform.Rotation.Up.x * offset;
		origin.y = Transform.Position.y + Transform.Rotation.Up.y * offset;
		origin.z = Transform.Position.z + Transform.Rotation.Up.z * offset;

		direction = Transform.Rotation.Down;

		// Find the hit point
		//bool hasHit = GroundDetection.Cast( Transform.Position, Transform.Rotation.Down, Wheel.Radius, Wheel.Width, ref wheelHit );
		bool hasHit = GroundDetection.Cast( origin, direction, length, Wheel.Radius, Wheel.Width, ref wheelHit );


		if ( hasHit )
		{
			_hitLocalPoint = Transform.World.PointToLocal( wheelHit.Point );			

		}
		return hasHit;
	}
	private void FindManager()
	{
		if ( _wheelControllerManager == null )
		{
			_wheelControllerManager = GameObject.Root.Components.Get<WheelControllerManager>();
			_wheelControllerManager ??= GameObject.Root.Components.Create<WheelControllerManager>();
		}
	}
	private void RegisterWithWheelControllerManager()
	{
		FindManager();
		_wheelControllerManager.Register( this );
	}


	private void DeregisterWithWheelControllerManager()
	{
		FindManager();
		_wheelControllerManager.Deregister( this );
	}

	private void UpdateSpringAndDamper()
	{

		Spring.PrevLength = Spring.Length;
		float localAirYPosition = Wheel.localPosition.z - _dt * Spring.MaxLength * suspensionExtensionSpeedCoeff;

		if ( _isGrounded )
		{
			float xDistance = _hitLocalPoint.x;
			float sine = xDistance / Wheel.Radius;
			sine = Math.Clamp( sine, -1, 1 );
			float hitAngle = MathF.Asin( sine );
			float localGroundedYPosition = _hitLocalPoint.z + Wheel.Radius * MathF.Cos( hitAngle );


			Wheel.localPosition.z = localGroundedYPosition > localAirYPosition
					? localGroundedYPosition
					: localAirYPosition;
		}
		else
		{
			Wheel.localPosition.z = localAirYPosition;
		}

		Spring.Length = - Wheel.localPosition.z;
		if ( Spring.Length <= 0f )
		{
			Spring.State = Spring.ExtensionState.BottomedOut;
			Spring.Length = 0;
		}
		else if ( Spring.Length >= Spring.MaxLength )
		{
			Spring.State = Spring.ExtensionState.OverExtended;
			Spring.Length = Spring.MaxLength;
			_isGrounded = false;
		}
		else
		{
			Spring.State = Spring.ExtensionState.Normal;
		}
		
		Spring.CompressionVelocity = (Spring.PrevLength - Spring.Length) / _dt;
		Spring.Compression = (Spring.MaxLength - Spring.Length) / Spring.MaxLength;
		Spring.Force = _isGrounded ? Spring.MaxForce * Spring.ForceCurve.Evaluate( Spring.Compression ) : 0f;
		Damper.Force = _isGrounded ? Damper.CalculateDamperForce( Spring.CompressionVelocity ) : 0f;

		if ( _isGrounded )
		{
			Load = Spring.Force + Damper.Force;
			Load = Load < 0f ? 0f : Load;

			_suspensionForce = wheelHit.Normal * Load / _dt;

			Rigidbody.ApplyForceAt( Transform.Position, _suspensionForce );

		}
		else
		{
			Load = 0;
			_suspensionForce = Vector3.Zero;
		}
	}

	protected override void OnStart()
	{
		base.OnStart();
		FindManager();

		// Initialize the wheel
		Wheel.UpdateProperties();
	}

	public override void Step()
	{
		if ( !Enabled )
			return;

		_isGrounded = FindTheHitPoint();
		_dt = Time.Delta;

		UpdateSpringAndDamper();
	}
	protected override void OnFixedUpdate()
	{
		if ( AutoSimulate )
		{
			Step();
		}
	}

	protected override void OnEnabled()
	{
		RegisterWithWheelControllerManager();
	}
	protected override void OnDisabled()
	{
		DeregisterWithWheelControllerManager();
	}
}
