using System;
using DM.Car;
using Sandbox.UI;

namespace DM.UI;
public sealed class InteractiveCamera : Component
{
	public bool Focused { get; private set; }
	[Property] private bool MouseVisible { get; set; } = true;
	[Property] private GameObject Origin { get; set; }
	[Property] private CarSelector CarSelector { get; set; }
	[Property] public GameObject SelectedObject { get; private set; }
	public Vector3 TargetPosition { get; set; }
	public Rotation TargetRotation { get; set; }
	public float Distance { get; set; } = 230f;
	public Rotation ClampRotation { get; set; }
	public float Clamp { get; set; } = -1f;

	private Vector3 lerp_Position { get; set; }
	private Rotation lerp_Rotation { get; set; }
	private float lerp_Distance { get; set; }

	public Action<Vector3> OnFocusChanged { get; set; }

	protected override void OnStart()
	{
		TargetPosition = Transform.Position;
		TargetRotation = Transform.Rotation;
		base.OnStart();
		SetFocus( Origin.Transform.Position, Origin.Transform.Rotation, 0f );
	}

	void CarFocus( GameObject car )
	{
		var center = Vector3.Up * CarSelector.ActiveCarCenter.z / 1.5f;
		SetFocus( car.Transform.Position + center );
		car.Components.Get<PartNameManager>().RenderNames = true;
		Focused = true;
		SelectedObject = car;
		Clamp = -1f;
	}
	void CarFocus() => CarFocus( CarSelector.ActiveCar );
	public void FreeFocus()
	{
		SetFocus( Origin.Transform.Position, Origin.Transform.Rotation, 0f );
		Focused = false;
		if ( CarSelector.ActiveCar is not null )
			CarSelector.ActiveCar.Components.Get<PartNameManager>().RenderNames = false;
		Clamp = -1f;

		SelectedObject = null;
	}
	private void UnStepFocus()
	{
		if ( SelectedObject is null )
			return;


		if ( SelectedObject == CarSelector.ActiveCar )
		{
			FreeFocus();
			return;
		}
		else
		{
			var components = SelectedObject.Components;
			var part = components.Get<PartName>();
			if ( part is not null )
			{
				SelectedObject = part.ParentPart;
				part = SelectedObject.Components.Get<PartName>();
				if ( part is not null )
					SetFocus( part );
				else
					CarFocus();
			}
			else
				CarFocus();
		}
	}

	public void SetFocus( Vector3 position, Rotation rotation, float distance = 230f )
	{
		lerp_Position = position;
		lerp_Rotation = rotation;
		lerp_Distance = distance;
		OnFocusChanged?.Invoke( position );
	}
	public void SetFocus( Vector3 position, float distance = 230f ) => SetFocus( position, Transform.Rotation, distance );
	public void SetFocus( PartName obj )
	{
		SelectedObject = obj.GameObject;
		Clamp = obj.AngleClamp;
		ClampRotation = obj.RotationClamp;
		SetFocus( obj.Transform.Position, obj.Distance );
		CarSelector.ActiveCar.Components.Get<PartNameManager>().RenderNames = false;

	}


	protected override void OnUpdate()
	{
		base.OnUpdate();
		ProcessLerp();
		ProcessInputs();
		SetupCameraPosition();
	}

	private void ProcessInputs()
	{

		Mouse.Visible = MouseVisible;

		Ray ray = MouseWorldInput.Input.Ray;
		bool MouseLeftPressed = MouseWorldInput.Input.MouseLeftPressed;
		bool MouseRightPressed = MouseWorldInput.Input.MouseRightPressed;

		Vector3 forward = ray.Forward;
		Angles angles = forward.EulerAngles;

		if ( MouseVisible )
			TargetRotation = Rotation.Lerp( TargetRotation, angles, Time.Delta );

		if ( Input.EscapePressed )
			UnStepFocus();

		SceneTraceResult result = Scene.Trace.Ray( ray, Scene.Camera.ZFar ).WithTag( "car" ).Run();

		if ( !Focused && result.Hit && MouseLeftPressed )
			CarFocus();

		bool rightDown = Input.Down( "GearDown" );

		if ( Focused )
			MouseVisible = !rightDown;


		if ( rightDown )
		{
			angles = lerp_Rotation.Angles();
			angles += Input.AnalogLook;
			angles.pitch = MathX.Clamp( angles.pitch, 5f, 45f );
			angles.roll = 0f;
			lerp_Rotation = angles.ToRotation();
		}
		else
			lerp_Rotation = lerp_Rotation.Angles().WithRoll( 0 );


		if ( Clamp != -1f )
			lerp_Rotation = lerp_Rotation.Clamp( ClampRotation, Clamp );
	}

	private void ProcessLerp()
	{
		TargetRotation = Rotation.Slerp( TargetRotation, lerp_Rotation, Time.Delta * 8f );
		float dot = TargetRotation.Forward.Dot( lerp_Rotation.Forward );

		Distance = Distance.LerpTo( lerp_Distance, Time.Delta * 8f * (dot * dot) );
		TargetPosition = TargetPosition.LerpTo( lerp_Position, Time.Delta * 8f * (dot * dot) );
	}

	private void SetupCameraPosition()
	{
		Transform.Rotation = TargetRotation;
		Transform.Position = TargetPosition + Transform.Rotation.Backward * Distance;
	}
}
