using System;

namespace DM.Car;

/// <summary>
/// I've pulled this out into its own component because I feel like we'll want different camera behaviors when we have weapons, building, etc..
/// </summary>
[Category( "Vehicles" )]
public sealed class CameraController : Component
{
	/// <summary>
	/// The player
	/// </summary>
	[RequireComponent]
	public Rigidbody Body { get; set; }

	/// <summary>
	/// The camera
	/// </summary>
	[Property, Group( "Setup" )]
	public GameObject CameraTarget { get; set; }

	/// <summary>
	/// The boom arm for the character's camera.
	/// </summary>
	[Property, Group( "Setup" )]
	public GameObject Boom { get; set; }

	/// <summary>
	/// How far can we look up / down?
	/// </summary>
	[Property, Group( "Config" )]
	public Vector2 PitchLimits { get; set; } = new( -10f, 50f );

	[Property, Group( "Config" )]
	public float VelocityFOVScale { get; set; } = 500f;
	public Angles EyeAngles { get; set; }
	public TimeSince ReturnCameraTime { get; set; }
	protected override void OnStart()
	{
		if ( IsProxy )
			return;

		if ( !(Scene?.Camera?.IsValid() ?? false) )
		{
			var prefab = ResourceLibrary.Get<PrefabFile>( "prefabs/camera.prefab" );
			SceneUtility.GetPrefabScene( prefab ).Clone( position: CameraTarget.Transform.Position, rotation: CameraTarget.Transform.Rotation );
		}
	}

	protected override void OnUpdate()
	{
		if ( IsProxy )
			return;

		base.OnUpdate();


		CameraTarget.Transform.LocalPosition = CameraTarget.Transform.LocalPosition.WithX( Math.Min( -120, CameraTarget.Transform.LocalPosition.x + Input.MouseWheel.y * 10 ) );

		EyeAngles += Input.AnalogLook;
		EyeAngles = EyeAngles.WithPitch( EyeAngles.pitch.Clamp( PitchLimits.x, PitchLimits.y ) );

		if ( !Input.AnalogLook.IsNearlyZero() )
			ReturnCameraTime = 0;
		if ( ReturnCameraTime > 3 )
			EyeAngles = EyeAngles.LerpTo( Body.Transform.Rotation.RotateAroundAxis( Vector3.Right, -25f ), Time.Delta * 5f );
		EyeAngles = EyeAngles.WithRoll( 0 );
		Boom.Transform.Rotation = EyeAngles.ToRotation();

		Boom.Transform.Position = Boom.Transform.Position.LerpTo( Body.Transform.Position, Time.Delta * 20 );

		float targetFov = Preferences.FieldOfView + Body.Velocity.Length / VelocityFOVScale;
		Scene.Camera.FieldOfView = Scene.Camera.FieldOfView.LerpTo( MathX.Clamp( targetFov, 10, 100 ), Time.Delta * 10 );
		Scene.Camera.Transform.Rotation = CameraTarget.Transform.Rotation;

		Scene.Camera.Transform.Position = CameraTarget.Transform.Position.WithZ( Scene.Camera.Transform.Position.z );

		Scene.Camera.Transform.Position = Scene.Camera.Transform.Position.LerpTo( CameraTarget.Transform.Position, Time.Delta * 12f );

		if ( Input.Pressed( "flymode" ) )
		{
			FlyMode = !FlyMode;

			Body.MotionEnabled = !FlyMode;
		}

		if ( FlyMode ) DoFlyMode();

	}
	void DoFlyMode()
	{

		Vector3 movement = Input.AnalogMove;

		float speed = 350.0f;

		if ( Input.Down( "Clutch" ) )
		{
			speed = 750.0f;
		}

		GameObject.Transform.Position += EyeAngles.ToRotation() * movement * speed * Time.Delta;

		Body.Velocity = EyeAngles.ToRotation() * movement * speed;

		return;

	}

	[Sync]
	bool FlyMode { get; set; }
}
