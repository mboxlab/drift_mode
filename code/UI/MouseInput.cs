public sealed class MouseInput : Component
{
	private static Vector2 Position;
	private static bool Visible;

	public static Ray Ray { get; private set; }
	public static bool LeftPressed { get; private set; }
	public static bool RightPressed { get; private set; }

	public static Sandbox.UI.WorldInput Input { get; } = new();

	protected override void OnUpdate()
	{
		if ( Mouse.Visible != Visible )
			Mouse.Position = Position;

		else if ( Visible )
			Position = Mouse.Position;

		Visible = Mouse.Visible;

		Ray = Scene.Camera.ScreenPixelToRay( Position );
		LeftPressed = Sandbox.Input.Pressed( "Attack1" );
		RightPressed = Sandbox.Input.Pressed( "Attack2" );

		Input.Ray = Ray;
		Input.MouseLeftPressed = LeftPressed;
		Input.MouseRightPressed = RightPressed;
	}
}
