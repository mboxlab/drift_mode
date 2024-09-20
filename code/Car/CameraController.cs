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
	public Vector2 PitchLimits { get; set; } = new( -10f, 50f );

	[Property, Group( "Config" )]
	public float VelocityFOVScale { get; set; } = 500f;

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

		if ( Player.DevCam )
		{
			DoDevCam();
			return;
		}


		CameraTarget.Transform.LocalPosition = CameraTarget.Transform.LocalPosition.WithX( Math.Min( -120, CameraTarget.Transform.LocalPosition.x + Input.MouseWheel.y * 10 ) );


		Player.EyeAngles += Input.AnalogLook;
		Player.EyeAngles = Player.EyeAngles.WithPitch( Player.EyeAngles.pitch.Clamp( PitchLimits.x, PitchLimits.y ) );

		Boom.Transform.Rotation = Player.EyeAngles.ToRotation();

		Boom.Transform.Position = Boom.Transform.Position.LerpTo( Player.Rigidbody.Transform.Position, Time.Delta * 20 );

		float targetFov = Preferences.FieldOfView + Player.Rigidbody.Velocity.Length / VelocityFOVScale;

		//Vector3 startPos = Boom.Transform.Position.WithZ( Boom.Transform.Position.z + 10 );
		//SceneTraceResult trace = Scene.Trace
		//	.Sphere( 8f, startPos, CameraTarget.Transform.Position )
		//	.IgnoreGameObjectHierarchy( GameObject.Root )
		//	.Run();
		Scene.Camera.FieldOfView = Scene.Camera.FieldOfView.LerpTo( MathX.Clamp( targetFov, 10, 100 ), Time.Delta * 10 );
		Scene.Camera.Transform.Rotation = CameraTarget.Transform.Rotation;

		Scene.Camera.Transform.Position = CameraTarget.Transform.Position.WithZ( Scene.Camera.Transform.Position.z );
		//if ( trace.Hit && !trace.StartedSolid )
		//	Scene.Camera.Transform.Position = trace.EndPosition;
		//else
		Scene.Camera.Transform.Position = Scene.Camera.Transform.Position.LerpTo( CameraTarget.Transform.Position, Time.Delta * 12f );



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
