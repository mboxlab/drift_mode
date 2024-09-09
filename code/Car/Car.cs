
using System;
using DM.Car;
public sealed class Car : Component
{
	[RequireComponent] public Rigidbody Rigidbody { get; set; }
	[RequireComponent] public CameraController CameraController { get; set; }

	[Property, Group( "Vehicle" )] public float Torque { get; set; } = 15000f;
	[Property, Group( "Vehicle" )] public float AccelerationRate { get; set; } = 1.0f;
	[Property, Group( "Vehicle" )] public float DecelerationRate { get; set; } = 0.5f;
	[Property, Group( "Vehicle" )] public float BrakingRate { get; set; } = 2.0f;


	/// <summary>
	/// The player's nametag
	/// </summary>
	[Property, Group( "Components" ), Sync]
	public Nametag Nametag { get; set; }

	public string CharacterName { get; set; } = "player";

	private List<Wheel> _wheels;
	public Angles EyeAngles { get; set; }
	public bool IsBraking { get; set; }

	private float _currentTorque;

	protected override void OnEnabled()
	{
		if ( Nametag?.GameObject?.IsValid() ?? false )
		{
			Nametag.GameObject.Enabled = IsProxy;
		}
		CharacterName = Connection.Local.DisplayName;

		_wheels = Components.GetAll<Wheel>( FindMode.EverythingInSelfAndDescendants ).ToList();
	}

	protected override void OnFixedUpdate()
	{
		if ( IsProxy ) return;

		float verticalInput = Input.AnalogMove.x;

		float horizontalInput = Input.AnalogMove.y;
		float targetTorque = verticalInput * Torque;


		IsBraking = Math.Sign( verticalInput * _currentTorque ) == -1 || Input.Down( "Brake" );
		bool isDecelerating = verticalInput == 0;

		float lerpRate = AccelerationRate;
		if ( IsBraking )
			lerpRate = BrakingRate;
		else if ( isDecelerating )
			lerpRate = DecelerationRate;

		_currentTorque = _currentTorque.LerpTo( targetTorque, lerpRate * Time.Delta );

		foreach ( Wheel wheel in _wheels )
		{
			//Log.Info( _currentTorque );
			wheel.ApplyMotorTorque( _currentTorque );

		}

		var groundVel = Rigidbody.Velocity.WithZ( 0f );
		if ( verticalInput == 0f && groundVel.Length < 32f )
		{
			var z = Rigidbody.Velocity.z;
			Rigidbody.Velocity = Vector3.Zero.WithZ( z );
		}


		if ( FlyMode ) DoFlyMode();

	}
	protected override void OnUpdate()
	{
		if ( IsProxy ) return;

		base.OnUpdate();

		if ( Game.IsEditor )
		{
			if ( Input.Pressed( "devcam" ) )
			{
				DevCam = !DevCam;
			}

			if ( Input.Pressed( "flymode" ) )
			{
				FlyMode = !FlyMode;

				Rigidbody.PhysicsBody.MotionEnabled = !FlyMode;
			}

			if ( Input.Pressed( "reset" ) )
			{
				Transform.Rotation = Transform.Rotation.Angles().WithRoll( 0 ).ToRotation();
			}
		}
		CameraController.UpdateFromPlayer();
	}
	void DoFlyMode()
	{
		if ( DevCam ) return;

		Vector3 movement = Input.AnalogMove;

		float speed = 350.0f;

		if ( Input.Down( "run" ) )
		{
			speed = 750.0f;
		}

		GameObject.Transform.Position += EyeAngles.ToRotation() * movement * speed * Time.Delta;

		Rigidbody.Velocity = EyeAngles.ToRotation() * movement * speed;

		return;

	}

	[Sync]
	bool FlyMode { get; set; }

	public bool DevCam { get; set; } = false;
}
