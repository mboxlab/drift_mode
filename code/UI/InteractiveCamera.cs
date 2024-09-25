using System;

namespace DM.UI;

[Icon( "visibility" )] 
[Category( "Interactive" )]
public sealed class InteractiveCamera : Component
{
	public static InteractiveObject Origin { get => InteractiveCamera.Instance._origin; }
	[Property] public InteractiveObject _origin;

	private static InteractiveCamera _instance;
	public static InteractiveCamera Instance
	{
		get
		{
			if ( !_instance.IsValid() )
				_instance = Game.ActiveScene.Components.GetAll<InteractiveCamera>().First();

			return _instance;
		}
	}

	public static InteractiveObject Target;

	private Stack<InteractiveObject> History = new();

	public static bool IsOnOrigin { get => Target == Origin; }

	private Vector3 TargetPosition;
	private Rotation TargetRotation;

	private Vector3 LerpPosition;
	private Rotation LerpRotation;

	private Vector3 LastPosition;
	private Rotation LastRotation;

	private Stack<Vector3> PositionHistory = new();
	private Stack<Rotation> RotationHistory = new();

	private BBox Box = BBox.FromPositionAndSize( Vector3.Zero, 8f );
	private float Now = -1;
	
	private Vector3 Slerp( Vector3 a, Vector3 b, float frac, bool clamp = true )
	{
		if ( clamp ) frac = frac.Clamp( 0, 1f );

		float dot = Vector3.Dot( a, b ).Clamp( -1f, 1f );
		float theta = MathF.Asin( dot );

		return a * MathF.Pow( MathF.Cos( frac * theta ), 1.5f ) + b * MathF.Pow( dot * MathF.Sin( frac * theta ), 1.5f );
	}

	private void SetTarget( InteractiveObject obj, Vector3 position, Rotation rotation, bool animate = true )
	{
		Now = animate ? Time.Now : Now;
		LastPosition = Transform.Position;
		LastRotation = Transform.Rotation;

		Target = obj;
		TargetPosition = position;
		TargetRotation = rotation;
	}

	private void SetTarget( InteractiveObject obj )
	{
		SetTarget( obj, obj.Position, obj.Clamp( TargetRotation ), obj.TriggerAnimation );
	}

	public void Focus( InteractiveObject obj )
	{
		if ( !obj.IsValid() || obj == Target ) return;
		if ( Time.Now - Now < 0.5f ) return; // fix

		InteractiveObject parent = obj.Parent;
		if ( IsOnOrigin && parent is not null && parent != Origin ) return;
		if ( !IsOnOrigin && parent != Target ) return;
		
		History.Push( Target );
		PositionHistory.Push( TargetPosition );
		RotationHistory.Push( TargetRotation );

		SetTarget( obj );
	}

	public void Focus( GameObject obj )
	{
		Focus( obj.Components.Get<InteractiveObject>() );
	}

	public void Defocus( bool toOrigin = false )
	{
		if ( toOrigin )
		{
			History.Clear();
			PositionHistory.Clear();
			RotationHistory.Clear();
			SetTarget( Origin );
		}
		else
		{
			InteractiveObject prev;
			History.TryPop( out prev );
			if ( prev is null ) return;

			Vector3 position = PositionHistory.Pop();
			Rotation rotation = RotationHistory.Pop();

			if ( Target.TriggerAnimation )
				SetTarget( prev, position, rotation );
			else
				SetTarget( prev, position, prev.Clamp( LerpRotation ), false );
		}
	}

	private void Management()
	{
		if ( Input.EscapePressed ) Defocus();
		if ( Input.Pressed( "Attack1" ) )
		{
			SceneTraceResult result = Scene.Trace.Ray( MouseInput.Ray, Scene.Camera.ZFar ).IgnoreGameObject( Target.GameObject ).Run();
			if ( result.Hit ) Focus( result.GameObject );
		}

		if ( IsOnOrigin )
			Mouse.Visible = true;

		else
		{
			Mouse.Visible = !Input.Down( "Attack2" );
			if ( Mouse.Visible ) return;

			Angles angles = TargetRotation.Angles();
			angles = Target.Clamp( angles + Input.AnalogLook );
			TargetRotation = angles.ToRotation();
		}
	}

	private void Move()
	{
		float animation = Time.Now - Now;
		if ( animation < 1 )
		{
			animation = MathF.Sin( animation * MathF.PI / 2 );

			LerpRotation = Rotation.Lerp( LastRotation, TargetRotation, animation );
			LerpPosition = Slerp( LastPosition, TargetPosition + LerpRotation.Backward * Target.Distance, animation );
		}
		else
		{
			LerpRotation = Rotation.Lerp( LerpRotation, TargetRotation, Time.Delta * 8f );
			LerpPosition = TargetPosition + LerpRotation.Backward * Target.Distance;
		}

		Transform.Position = LerpPosition;
		Transform.Rotation = LerpRotation;
	}

	protected override void OnStart()
	{
		base.OnStart();

		Target = Origin;

		TargetPosition = Origin.Position;
		TargetRotation = Origin.Rotation;

		LerpPosition = TargetPosition;
		LerpRotation = TargetRotation;
	}
	
	protected override void OnUpdate()
	{
		base.OnUpdate();

		// fix: if GameObject is not valid then back to origin
		if ( !Target.IsValid() ) Defocus( true );

		Management();
		Move();
	}
}
