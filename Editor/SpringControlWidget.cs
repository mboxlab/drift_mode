using Sandbox;
using DM.Car;
[CustomEditor( typeof( Spring ) )]
public class SpringControlWidget : ControlWidget
{
	public override bool SupportsMultiEdit => true;
	public override bool IncludeLabel => false;

	public SpringControlWidget( SerializedProperty property ) : base( property )
	{
		if ( !property.TryGetAsObject( out var obj ) )
		{
			Log.Error( "Couldn't get Spring" );
		}
		Layout = Layout.Column();
		Layout.Margin = 8f;
		Layout.Add( new Label.Body( $" {ToolTip}" ) { Color = Color.White } );

		Layout.Add( new Label( "Properties" ) );
		Layout.Add( new Separator( 4 ) );
		Layout.Add( new EnumControlWidget( obj.GetProperty( "State" ) ) { ToolTip = "Extension State", ReadOnly = true } );
		Layout.Add( new Separator( 4 ) );
		Layout.Add( new FloatControlWidget( obj.GetProperty( "Length" ) ) { ToolTip = "Length", ReadOnly = true } );
		Layout.Add( new Separator( 4 ) );
		Layout.Add( new CurveControlWidget( obj.GetProperty( "ForceCurve" ) ) { ToolTip = "Curve" } );
		Layout.Add( new Separator( 4 ) );

		var phys = Layout.AddRow();
		phys.Add( new FloatControlWidget( obj.GetProperty( "MaxForce" ) ) { Label = "F", Icon = "functions", ToolTip = "Max Force", HighlightColor = Theme.Green }, 1 );
		phys.Add( new FloatControlWidget( obj.GetProperty( "MaxLength" ) ) { Label = "L", Icon = "functions", ToolTip = "Max Length", HighlightColor = Theme.Green }, 1 );
	}

	protected override void PaintUnder()
	{
		Paint.ClearPen();

		Rect localRect = LocalRect;
		Paint.SetBrush( ControlColor.Lighten( 0.25f ) );
		Paint.DrawRect( in localRect, ControlRadius );
	}
}
