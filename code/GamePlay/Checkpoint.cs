using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sandbox.GamePlay;

public class Checkpoint : Component
{
	[Property] public Checkpoint Next { get; set; }
	[Property] public Collider Trigger { get; set; }

	protected override void DrawGizmos()
	{
		if ( Next != null )
		{
			Gizmo.Transform = Scene.Transform.World;
			Gizmo.Draw.IgnoreDepth = true;
			Gizmo.Draw.Arrow( Transform.Position, Next.Transform.Position, 220, 150 );
		}

	}
}
