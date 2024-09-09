using DM.Vehicle;
using Sandbox;
[CustomEditor( typeof( Wheel ) )]
public class WheelControlWidget : ControlWidget
{
	public override bool SupportsMultiEdit => true;

	public WheelControlWidget( SerializedProperty property ) : base( property )
	{
		if ( !property.TryGetAsObject( out var obj ) )
		{
			Log.Error( "Couldn't get Wheel" );
		}
		Layout = Layout.Column();
		Layout.Margin = 4f;
		Layout.Add( new Label( "Properties" ) );
		Layout.Add( new Separator( 4 ) );
		Layout.Add( new ComponentControlWidget( obj.GetProperty( "Visual" ) ) { ToolTip = "Visual" }, 1 );
		Layout.Add( new Separator( 4 ) );

		var phys = Layout.AddRow();
		phys.Add( new FloatControlWidget( obj.GetProperty( "Mass" ) ) { Label = "M", Icon = "functions", ToolTip = "Mass", HighlightColor = Theme.Green }, 1 );
		phys.Add( new FloatControlWidget( obj.GetProperty( "Radius" ) ) { Label = "R", Icon = "functions", ToolTip = "Radius", HighlightColor = Theme.Green }, 1 );
		phys.Add( new FloatControlWidget( obj.GetProperty( "Width" ) ) { Label = "W", Icon = "functions", ToolTip = "Width", HighlightColor = Theme.Green }, 1 );
	}

	protected override void PaintUnder()
	{
		Paint.ClearPen();

		Rect localRect = LocalRect;
		Paint.SetBrush( ControlColor.Lighten( 0.25f ) );
		Paint.DrawRect( in localRect, ControlRadius );
	}
}
