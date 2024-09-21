using DM.Car;

namespace DM.UI;
public sealed class InteractiveCamera : Component
{
	[Property] private GameObject Origin { get; set; }

	public bool IsLooking = false;
	public bool IsAnimated = false;

	[Property] private CarSelector CarSelector { get; set; }

	private Vector3 LookPosition;
	private Rotation LookRotation;
	private Vector3 CarCenter;
	private GameObject LookObject;
	protected override void OnStart()
	{
		base.OnStart();
		CarSelector.OnCarChanged += OnCarChanged;
	}

	void OnCarChanged( GameObject car )
	{

		LookObject = CarSelector.ActiveCar;
		CarCenter = Vector3.Up * CarSelector.ActiveCarCenter.z / 1.5f;
		if ( IsLooking )
			LookObject.Components.Get<PartNameManager>().RenderNames = true;
		else
			LookObject.Components.Get<PartNameManager>().RenderNames = false;

	}
	protected override void OnUpdate()
	{
		base.OnUpdate();

		if ( IsLooking )
		{
			if ( IsAnimated )
			{
				LookPosition = LookObject.Transform.Position + CarCenter + LookRotation.Backward * 230f;

				Transform.Position = Vector3.Lerp( Transform.Position, LookPosition, Time.Delta * 16f );
				Transform.Rotation = Rotation.Lerp( Transform.Rotation, LookRotation, Time.Delta * 16f );

				if ( Transform.Position.AlmostEqual( LookPosition, 1f ) )
					IsAnimated = false;
			}
			else
			{
				if ( Input.Pressed( "GearUp" ) )
				{
					IsLooking = false;
					LookObject.Components.Get<PartNameManager>().RenderNames = false;
					IsAnimated = true;
					LookRotation = (LookObject.Transform.Position + CarCenter - Origin.Transform.Position).EulerAngles.ToRotation();
					return;
				}

				bool left = Input.Down( "GearDown" );
				Mouse.Visible = !left;

				if ( left )
				{
					Angles angles = LookRotation.Angles();
					angles += Input.AnalogLook;
					angles.pitch = MathX.Clamp( angles.pitch, -5f, 35f );
					LookRotation = angles.ToRotation();
				}

				Transform.Rotation = Rotation.Lerp( Transform.Rotation, LookRotation, Time.Delta * 16f );
				Transform.Position = LookObject.Transform.Position + CarCenter + Transform.Rotation.Backward * 230f;
			}
		}
		else
		{
			Mouse.Visible = true;

			Ray ray = Scene.Camera.ScreenPixelToRay( Mouse.Position );
			if ( !IsAnimated )
			{
				SceneTraceResult result = Scene.Trace.Ray( ray, 65536f ).WithTag( "car" ).Run();
				if ( result.Hit && Input.Pressed( "GearUp" ) )
				{
					LookObject.Components.Get<PartNameManager>().RenderNames = true;
					IsLooking = true;
					IsAnimated = true;
					return;
				}
			}

			Vector3 forward = ray.Forward;
			Angles angles = forward.EulerAngles;
			LookRotation = Rotation.Lerp( Origin.Transform.Rotation, angles, 0.16f );

			angles = LookRotation.Angles();
			angles.roll = 0f;
			LookRotation = angles.ToRotation();

			Vector3 shift = ray.Forward - LookRotation.Forward;
			shift = shift * 24f + LookRotation.Backward * shift.Length * 24f;
			LookPosition = Origin.Transform.Position + shift;

			if ( IsAnimated )
			{
				Transform.Rotation = Rotation.Lerp( Transform.Rotation, LookRotation, Time.Delta * 12f );

				float dot = Transform.Rotation.Forward.Dot( LookRotation.Forward );
				if ( dot < 0.5f )
					Transform.Position = LookObject.Transform.Position + CarCenter + Transform.Rotation.Backward * 230f;
				else
				{
					Transform.Position = Vector3.Lerp( Transform.Position, LookPosition, Time.Delta * 11f );
					if ( dot >= 0.999f && Transform.Position.AlmostEqual( LookPosition, 1f ) ) IsAnimated = false;
				}
			}
			else
			{
				Transform.Position = Vector3.Lerp( Transform.Position, LookPosition, Time.Delta * 16f );
				Transform.Rotation = Rotation.Lerp( Transform.Rotation, LookRotation, Time.Delta * 16f );
			}
		}
	}
}
