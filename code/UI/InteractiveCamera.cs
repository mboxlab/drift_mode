
using Sandbox.Utils;
using System.Diagnostics;

namespace Sandbox.UI;

[Icon( "visibility" )]
[Category( "Interactive" )]
public sealed class InteractiveCamera : Component
{
	public static InteractiveObject Origin { get => Instance._origin; }
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

	public static InteractiveObject Target { get; private set; }
	public Action<bool> OnMouseDrag;

	public static bool IsOnOrigin { get => Target == Origin; }

	private Vector3 TargetPosition;
	private Rotation TargetRotation;
	private float TargetDistance;

	private readonly Stack<InteractiveObject> History = new();
	private readonly Stack<Vector3> PositionHistory = new();
	private readonly Stack<Rotation> RotationHistory = new();


	private bool InAnimation = false;

	[Property] public Curve AnimationCurve { get; set; }

	private void SetTarget( InteractiveObject obj, Vector3 position, Rotation rotation, bool animate = true )
	{
		Target = obj;
		StartAnimation( position, rotation, Target.Distance );
	}

	private void SetTarget( InteractiveObject obj )
	{
		SetTarget( obj, obj.Position, obj.Clamp( TargetRotation ), obj.TriggerAnimation );
	}

	public void Focus( InteractiveObject obj )
	{
		if ( !obj.IsValid() || obj == Target ) return;

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
			History.TryPop( out InteractiveObject prev );
			if ( prev is null ) return;

			Vector3 position = PositionHistory.Pop();
			Rotation rotation = RotationHistory.Pop();

			SetTarget( prev, position, prev.Clamp( rotation ), Target.TriggerAnimation );
		}
	}

	private HighlightOutline HighlightOutline;
	private void Management()
	{
		if ( InAnimation )
		{
			if ( HighlightOutline.IsValid() ) HighlightOutline.Enabled = false;
			return;
		}

		if ( Input.EscapePressed )
		{
			Input.EscapePressed = false;
			if ( Target != Origin ) Defocus();
		}

		if ( IsOnOrigin )
			Mouse.Visible = true;
		else
		{
			bool isDown = Input.Down( "Attack2" );
			if ( !isDown != Mouse.Visible )
				OnMouseDrag?.Invoke( isDown );

			Mouse.Visible = !isDown;

			if ( !isDown )
				return;
			Angles angles = TargetRotation.Angles();
			angles = Target.Clamp( angles + Input.AnalogLook );
			TargetRotation = angles.ToRotation();
		}

		SceneTraceResult result = Scene.Trace.Ray( MouseInput.Ray, Scene.Camera.ZFar ).IgnoreGameObject( Target.GameObject ).Run();
		GameObject obj = result.GameObject;
		if ( !obj.IsValid() ) return;

		if ( Input.Pressed( "Attack1" ) ) Focus( obj );
		if ( IsOnOrigin )
		{
			HighlightOutline highlight = obj.Components.Get<HighlightOutline>( FindMode.EverythingInSelfAndChildren );
			if ( HighlightOutline.IsValid() ) HighlightOutline.Enabled = highlight.IsValid();
			HighlightOutline = highlight;
		}
	}

	private void UpdatePosition()
	{
		Ray ray = new Ray( TargetPosition, TargetRotation.Backward );
		SceneTraceResult result = Scene.Trace.Sphere( 4f, ray, TargetDistance ).IgnoreGameObject( Target.GameObject.Root ).Run();

		WorldPosition = result.EndPosition;
		WorldRotation = TargetRotation;
	}

	private void StartAnimation( Vector3 endPos, Rotation endRot, float distance )
	{
		var anim = Components.Create<Animation>( true );
		anim.Length = 0.4f;
		anim.OnAnimationStart += OnAnimationStart;
		anim.OnAnimationEnd += OnAnimationEnd;
		if ( AnimationCurve.Length > 1 )
			anim.Curve = AnimationCurve;

		Vector3 lerpPos = TargetPosition;
		Rotation lerpRot = TargetRotation;
		float lerpDist = TargetDistance;
		anim.OnAnimationProgress += ( float delta ) =>
		{
			TargetPosition = lerpPos.LerpTo( endPos, delta );
			TargetRotation = Rotation.Lerp( lerpRot, endRot, delta );
			TargetDistance = lerpDist.LerpTo( distance, delta );
		};

	}

	private void OnAnimationStart() => InAnimation = true;
	private void OnAnimationEnd() => InAnimation = false;

	protected override void OnStart()
	{
		base.OnStart();

		Target = Origin;
		TargetPosition = Origin.Position;
		TargetRotation = Origin.Rotation;
	}


	protected override void OnUpdate()
	{
		base.OnUpdate();

		// fix: if GameObject is not valid then back to origin
		if ( !Target.IsValid() ) Defocus( true );

		Management();
		UpdatePosition();
	}
}
