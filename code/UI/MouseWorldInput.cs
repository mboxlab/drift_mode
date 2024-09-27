public sealed class MouseWorldInput : Component
{
	private Vector2 _mousePosition { get; set; }
	private bool _mouseVisible { get; set; }

	public Vector2 MousePosition { get { return _mousePosition; } }
	public static Sandbox.UI.WorldInput Input { get; } = new();
	protected override void OnUpdate()
	{
		if ( Mouse.Visible != _mouseVisible )
		{
			Mouse.Position = _mousePosition;
		}

		if ( _mouseVisible && Mouse.Visible == _mouseVisible )
		{
			_mousePosition = Mouse.Position;
		}
		_mouseVisible = Mouse.Visible;


		Input.Ray = Scene.Camera.ScreenPixelToRay( _mousePosition );
		Input.MouseLeftPressed = Sandbox.Input.Pressed( "Attack1" );
		Input.MouseRightPressed = Sandbox.Input.Pressed( "Attack2" );
	}
}
