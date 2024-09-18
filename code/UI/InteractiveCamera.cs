using System.Diagnostics;
using static Sandbox.PhysicsContact;

namespace DM.UI;
public sealed class InteractiveCamera : Component
{
	private Rotation TargetRotation;
	private Rotation _lerpedRotation;

	protected override void OnStart()
	{
		TargetRotation = Transform.Rotation;
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		Mouse.Visible = true;
		_lerpedRotation = Rotation.Lerp( _lerpedRotation, Scene.Camera.ScreenPixelToRay( Mouse.Position ).Forward.EulerAngles, 0.15f );
		Transform.Rotation = Rotation.Lerp( TargetRotation, _lerpedRotation, 0.08f );
	}
}
