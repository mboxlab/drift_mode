namespace Sandbox.Car;

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
	/// The boom arm for the character's camera.
	/// </summary>
	[Property, Group( "Setup" )]
	public GameObject Boom { get; set; }

	[Property] public float CameraDistance { get; set; } = 224f;

	public Angles EyeAngles { get; set; }

	protected override void OnStart()
	{
		if ( IsProxy )
			return;

		Boom.LocalPosition = new Vector3( 0f, 0f, GameObject.Parent.GetBounds().Size.z / 1.5f );

		if ( !(Scene?.Camera?.IsValid() ?? false) )
		{
			var prefab = ResourceLibrary.Get<PrefabFile>( "prefabs/camera.prefab" );
			SceneUtility.GetPrefabScene( prefab ).Clone();
		}
	}

	protected override void OnUpdate()
	{
		if ( IsProxy )
			return;

		CameraComponent camera = Scene.Camera;

		Vector3 velocity = Body.Velocity;
		Vector3 direction = velocity.Normal;

		float targetFov = Math.Min( Preferences.FieldOfView + 24f, Preferences.FieldOfView + Body.Velocity.Length / 192f );
		camera.FieldOfView = camera.FieldOfView.LerpTo( targetFov, Time.Delta * 4f );

		Rotation rotation = Body.WorldRotation;

		float front = (Input.Down( "Front View" ) ? 1 : 0) - (Input.Down( "Back View" ) ? 1 : 0);
		float side = (Input.Down( "Right View" ) ? 1 : 0) - (Input.Down( "Left View" ) ? 1 : 0);

		float RightStickX = -Input.GetAnalog( InputAnalog.RightStickX );
		float RightStickY = -Input.GetAnalog( InputAnalog.RightStickY );

		float degressLook = MathF.Atan2( side + RightStickX, front + RightStickY ) * 180.0f / MathF.PI;

		rotation = rotation.RotateAroundAxis( Vector3.Up, degressLook );
		rotation = rotation.RotateAroundAxis( Vector3.Left, 10f );

		if ( degressLook != 0 )
			rotation = Rotation.Lerp( rotation, direction.EulerAngles, velocity.Length / 8192f );

		EyeAngles = rotation;
		camera.WorldRotation = Rotation.Lerp( camera.WorldRotation, EyeAngles, Time.Delta * 8f );

		Vector3 position = Boom.WorldPosition + camera.WorldRotation.Backward * CameraDistance;
		camera.WorldPosition = position;
	}

	[Sync]
	bool FlyMode { get; set; }
}
