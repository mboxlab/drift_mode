
using System;

namespace DM.Car;
public sealed class Car : Component
{
	[RequireComponent] public Rigidbody Rigidbody { get; set; }
	[RequireComponent] public CameraController CameraController { get; set; }

	[Property, Group( "Vehicle" )] public float MotorTorque { get; set; } = 1000f;
	[Property, Group( "Vehicle" )] public float BrakeTorque { get; set; } = 3000f;

	/// <summary>
	/// The player's nametag
	/// </summary>
	[Property, Group( "Components" ), Sync]
	public Nametag Nametag { get; set; }

	public string CharacterName { get; set; } = "player";

	private List<Wheel> _wheels;
	public Angles EyeAngles { get; set; }
	public bool IsBraking { get; set; }

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
		if ( IsProxy )
			return;

		IsBraking = Input.Down( "Brake" );

		foreach ( Wheel wheel in _wheels )
			wheel.ApplyBrakeTorque( IsBraking ? BrakeTorque : 0f );

		if ( FlyMode ) DoFlyMode();

	}
	protected override void OnUpdate()
	{
		if ( IsProxy )
			return;

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

				Rigidbody.MotionEnabled = !FlyMode;
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

		if ( Input.Down( "Clutch" ) )
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
