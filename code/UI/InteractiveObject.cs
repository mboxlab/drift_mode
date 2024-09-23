using static Sandbox.Gizmo;

namespace DM.UI;

[Category( "Interactive" )]
public sealed class InteractiveObject : Component
{
	[Property] public InteractiveObject Parent;

	[Property] public Vector3 LocalPosition;
	[Property] public Angles LocalAngles;
	[Property] public float Distance;

	// ToggleGroup не работает :rage:
	[Property, Group( "Clamp" )] public bool Clamping = false;
	[Property, Group( "Clamp" )] public bool ClampEqual = true;

	private Angles _clampMin;
	[Property, Group( "Clamp" )] public Angles ClampMin
	{
		get => _clampMin;
		set
		{
			_clampMin = value;
			if ( ClampEqual ) _clampMax = new Angles( -value.pitch, -value.yaw, 0 );
		}
	}

	private Angles _clampMax;
	[Property, Group( "Clamp" )]
	public Angles ClampMax
	{
		get => _clampMax;
		set
		{
			_clampMax = value;
			if ( ClampEqual ) _clampMin = new Angles( -value.pitch, -value.yaw, 0 );
		}
	}

	public bool IsLooked { get => InteractiveCamera.Target == this; }

	public Vector3 Position
	{
		get => Transform.Position + LocalPosition * Transform.Rotation;
	}

	public Rotation Rotation
	{
		get => (LocalAngles + Transform.Rotation.Angles()).ToRotation();
	}

	public Angles Clamp( Angles angles )
	{
		if ( !Clamping ) return new Angles( angles );

		angles = angles - Rotation.Angles();
		angles = angles.Normal;

		angles.pitch = MathX.Clamp( angles.pitch, ClampMin.pitch, ClampMax.pitch );
		angles.yaw = MathX.Clamp( angles.yaw, ClampMin.yaw, ClampMax.yaw );

		return angles + Rotation.Angles();
	}

	public Rotation Clamp( Rotation rotation )
	{
		return Clamp( rotation.Angles() ).ToRotation();
	}

	protected override void DrawGizmos()
	{
		base.DrawGizmos();

		if ( !Gizmo.IsSelected )
			return;

		GizmoDraw draw = Gizmo.Draw;
		Gizmo.Transform = Scene.Transform.World;

		Vector3 position = Position;
		Vector3 from = position + Vector3.Backward * Rotation * Distance;

		draw.Arrow( from, position, 3f, 1f );

		draw.Arrow( from, position, 3f, 1f );

		if ( !Clamping ) return;

		from = Position;

		Ray tl = new Ray( from, (Rotation * Rotation.FromPitch( ClampMin.pitch ) * Rotation.FromYaw( ClampMax.yaw )).Backward );
		Ray tr = new Ray( from, (Rotation * Rotation.FromPitch( ClampMax.pitch ) * Rotation.FromYaw( ClampMax.yaw )).Backward );
		Ray br = new Ray( from, (Rotation * Rotation.FromPitch( ClampMax.pitch ) * Rotation.FromYaw( ClampMin.yaw )).Backward );
		Ray bl = new Ray( from, (Rotation * Rotation.FromPitch( ClampMin.pitch ) * Rotation.FromYaw( ClampMin.yaw )).Backward );

		Frustum frustum = Frustum.FromCorners( tl, tr, br, bl, 0f, Distance );

		draw.LineFrustum( frustum );

	}
}
