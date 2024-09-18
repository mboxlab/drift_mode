using System.Diagnostics;
using static Sandbox.PhysicsContact;

namespace DM.UI;
public sealed class InteractiveCamera : Component
{
	private Rotation TargetRotation;

	protected override void OnStart()
	{
		TargetRotation = Transform.Rotation;
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		Mouse.Visible = true;

		Transform.Rotation = Rotation.Lerp( TargetRotation, Scene.Camera.ScreenPixelToRay( Mouse.Position ).Forward.EulerAngles, 0.02f );
	}
}
