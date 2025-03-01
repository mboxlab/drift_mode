﻿namespace Sandbox.GamePlay;

public class Checkpoint : Component
{
	[Property] public Checkpoint Next { get; set; }
	[Property] public Collider Trigger { get; set; }

	protected override void DrawGizmos()
	{
		if ( Gizmo.IsSelected )
			if ( Next != null )
			{
				Gizmo.Transform = Scene.Transform.World;
				Gizmo.Draw.IgnoreDepth = true;

				Gizmo.Draw.Arrow( WorldPosition, Next.WorldPosition, 220, 50 );
			}

	}
}
