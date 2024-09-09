using System;

/// <summary>
/// I've pulled this out into its own component because I feel like we'll want different camera behaviors when we have weapons, building, etc..
/// </summary>
public sealed class CameraController : Component
{
	/// <summary>
	/// The player
	/// </summary>
	[RequireComponent]
	public Car Player { get; set; }

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
	public Vector2 PitchLimits { get; set; } = new( -50f, 50f );

	[Property, Group( "Config" )]
	public float VelocityFOVScale { get; set; } = 50f;

	/// <summary>
	/// Bobbing cycle time
	/// </summary>
	[Property, Group( "Config" )]
	public float BobCycleTime { get; set; } = 5.0f;

	/// <summary>
	/// Bobbing direction
	/// </summary>
	[Property, Group( "Config" )]
	public Vector3 BobDirection { get; set; } = new Vector3( 0.0f, 1.0f, 0.5f );

	protected override void OnStart()
	{
		if ( !(Scene?.Camera?.IsValid() ?? false) )
		{
			var prefab = ResourceLibrary.Get<PrefabFile>( "prefabs/camera.prefab" );
			SceneUtility.GetPrefabScene( prefab ).Clone( position: CameraTarget.Transform.Position, rotation: CameraTarget.Transform.Rotation );
		}
	}

	/// <summary>
	/// Runs from <see cref="Player.OnUpdate"/>
	/// </summary>
	public void UpdateFromPlayer()
	{
		if ( Player.DevCam )
		{
			DoDevCam();
			return;
		}

		// Have an option for this later to scale?


		Player.EyeAngles += Input.AnalogLook;
		Player.EyeAngles = Player.EyeAngles.WithPitch( Player.EyeAngles.pitch.Clamp( PitchLimits.x, PitchLimits.y ) );

		Boom.Transform.Rotation = Player.EyeAngles.ToRotation();

		Boom.Transform.Position = Boom.Transform.Position.LerpTo( Player.Rigidbody.Transform.Position, Time.Delta * 20 );

		var targetFov = Preferences.FieldOfView + Player.Rigidbody.Velocity.Length / VelocityFOVScale;

		Scene.Camera.FieldOfView = Scene.Camera.FieldOfView.LerpTo( targetFov, Time.Delta * 10 );
		Scene.Camera.Transform.Position = CameraTarget.Transform.Position.WithZ( Scene.Camera.Transform.Position.z );
		Scene.Camera.Transform.Position = Scene.Camera.Transform.Position.LerpTo( CameraTarget.Transform.Position, Time.Delta * 12f );
		Scene.Camera.Transform.Rotation = CameraTarget.Transform.Rotation;
	}

	public float CalcRelativeYaw( float angle )
	{
		float length = CameraTarget.Transform.Rotation.Yaw() - angle;

		float d = MathX.UnsignedMod( Math.Abs( length ), 360 );
		float r = (d > 180) ? 360 - d : d;
		r *= (length >= 0 && length <= 180) || (length <= -180 && length >= -360) ? 1 : -1;

		return r;
	}


	Angles devCamAngles;

	private void DoDevCam()
	{
		Vector3 movement = Input.AnalogMove;

		float speed = 350.0f;

		if ( Input.Down( "attack2" ) )
		{
			Scene.Camera.FieldOfView += Input.MouseDelta.y * 0.1f;
			Scene.Camera.FieldOfView = Scene.Camera.FieldOfView.Clamp( 10.0f, 120.0f );
			return;
		}

		movement += Input.Down( "Jump" ) ? Vector3.Up : Vector3.Zero;
		movement -= Input.Down( "Duck" ) ? Vector3.Up : Vector3.Zero;

		devCamAngles += Input.AnalogLook * 0.5f;
		devCamAngles = devCamAngles.WithPitch( devCamAngles.pitch.Clamp( -89.0f, 89.0f ) );

		if ( Input.Down( "run" ) )
		{
			speed = 750.0f;
		}

		Scene.Camera.Transform.Position += devCamAngles.ToRotation() * movement * speed * Time.Delta;
		Scene.Camera.Transform.Rotation = devCamAngles.ToRotation();

	}
}
