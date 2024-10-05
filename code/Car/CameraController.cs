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
	/// The boom arm for the character's camera.
	/// </summary>
	[Property, Group( "Setup" )]
	public GameObject Boom { get; set; }
	
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

		base.OnUpdate();

		CameraComponent camera = Scene.Camera;

		Vector3 velocity = Body.Velocity;
		Vector3 direction = velocity.Normal;

		float targetFov = Math.Min( Preferences.FieldOfView + 24f, Preferences.FieldOfView + Body.Velocity.Length / 192f );
		camera.FieldOfView = camera.FieldOfView.LerpTo( targetFov, Time.Delta * 4f );

		Rotation rotation = Body.WorldRotation;

		bool front = Input.Down( "Front View" );
		bool left = Input.Down( "Left View" );
		bool right = Input.Down( "Right View" );

		float degrees = front ? 180f : left ? -90f : right ? 90f : 0;
		rotation = rotation.RotateAroundAxis( Vector3.Up, degrees );
		rotation = rotation.RotateAroundAxis( Vector3.Left, 10f );

		if ( !(front || left || right) )
			rotation = Rotation.Lerp( rotation, direction.EulerAngles, velocity.Length / 8192f );

		EyeAngles = rotation;
		camera.WorldRotation = Rotation.Lerp( camera.WorldRotation, EyeAngles, Time.Delta * 8f );

		Vector3 position = Boom.WorldPosition + camera.WorldRotation.Backward * 192f;
		camera.WorldPosition = position;
	}

	[Sync]
	bool FlyMode { get; set; }
}
