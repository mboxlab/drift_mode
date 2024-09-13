public sealed class Test : Component
{
	protected override void OnUpdate()
	{
		Vector3 start = Transform.Position;
		Vector3 end = start + Vector3.Down * 12;

		var traceResult = Scene.Trace.Sphere( 32, start, end ).Run();

		/* Draw end position of sphere */
		Gizmo.Draw.IgnoreDepth = true;
		Gizmo.Draw.Color = Color.Red;
		Gizmo.Draw.LineSphere( traceResult.EndPosition, 32 );

		/* If there's a hit, draw the hit position */
		if ( traceResult.Hit )
		{
			Gizmo.Draw.Color = Color.Green;
			Gizmo.Draw.SolidSphere( traceResult.HitPosition, 2.0f );

		}
	}
}
