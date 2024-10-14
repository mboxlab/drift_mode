using static Sandbox.Gizmo;

namespace Sandbox.UI;

[Category( "Interactive" )]
public sealed class InteractiveObject : Component
{
	[Property] public InteractiveObject Parent;

	[Property] public Vector3 OffsetPosition;
	[Property] public Angles LocalAngles;
	[Property] public float Distance;

	[Property]
	[Group( "Clamp" )]
	public bool Clamping = false;

	[Property]
	[Group( "Clamp" )]
	[ShowIf( "Clamping", true )]
	public bool ClampEqual = true;

	[Property]
	[Group( "Clamp" )]
	[ShowIf( "Clamping", true )]
	public Angles ClampMin
	{
		get => _clampMin;
		set
		{
			_clampMin = value;
			if ( ClampEqual ) _clampMax = new Angles( -value.pitch, -value.yaw, 0 );
		}
	}
	private Angles _clampMin;

	[Property]
	[Group( "Clamp" )]
	[ShowIf( "Clamping", true )]
	public Angles ClampMax
	{
		get => _clampMax;
		set
		{
			_clampMax = value;
			if ( ClampEqual ) _clampMin = new Angles( -value.pitch, -value.yaw, 0 );
		}
	}
	private Angles _clampMax;

	[Property]
	[Group( "Other" )]
	public bool TriggerAnimation = true;

	public bool IsLooked { get => InteractiveCamera.Target == this; }

	public Vector3 Position
	{
		get => WorldPosition + OffsetPosition * WorldRotation;
	}

	public Rotation Rotation
	{
		get => (LocalAngles + WorldRotation.Angles()).ToRotation();
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

	private float deg2rad = MathF.PI / 180;
	private Color Red = new Color( 223, 70, 45 );
	private Color Green = new Color( 171, 213, 87 );
	private (Vector3, Vector3) DrawCircleSegment( GizmoDraw draw, Vector3 position, Rotation rotation, float start, float end, float size = 64f, int count = 32 )
	{
		Vector3 first = Vector3.Zero;
		Vector3 second = Vector3.Zero;

		float prev = start;
		start = MathF.Min( start, end );
		end = MathF.Max( prev, end );

		float step = (end - start) / count;
		for ( float pi = start; pi < end; pi += step )
		{
			Vector3 from = position + new Vector3( -MathF.Cos( pi ), MathF.Sin( pi ), 0 ) * rotation * size;
			Vector3 to = position + new Vector3( -MathF.Cos( pi + step ), MathF.Sin( pi + step ), 0 ) * rotation * size;

			draw.Color = Green;
			draw.Line( new Line( from, to ) );

			if ( pi == start )
			{
				first = from;

				draw.Color = Color.Cyan;
				draw.Line( new Line( position, first ) );
			}
			else if ( pi > end - step * 1f )
			{
				second = to;

				draw.Color = Color.Cyan;
				draw.Line( new Line( position, second ) );
			}
		}

		return (first, second);
	}

	protected override void DrawGizmos()
	{
		base.DrawGizmos();

		if ( !Gizmo.IsSelected )
			return;

		GizmoDraw draw = Gizmo.Draw;
		Gizmo.Transform = Scene.Transform.World;

		if ( Clamping )
		{
			float start = ClampMin.yaw * deg2rad;
			float end = ClampMax.yaw * deg2rad;

			(Vector3 p1, Vector3 p2) = DrawCircleSegment( draw, Position, Rotation * Rotation.FromPitch( ClampMin.pitch ), start, end, Distance );
			(Vector3 p3, Vector3 p4) = DrawCircleSegment( draw, Position, Rotation * Rotation.FromPitch( ClampMax.pitch ), start, end, Distance );

			draw.Color = Red;
			draw.Line( new Line( p1, p3 ) );
			draw.Line( new Line( p2, p4 ) );
		}
		else
		{
			draw.Color = Color.Cyan.WithAlphaMultiplied( 0.2f );
			draw.SolidSphere( Position, Distance, 64, 64 );
		}
	}
}
