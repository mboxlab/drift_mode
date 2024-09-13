


using DM.Ground;
using Sandbox;

namespace DM;

[CustomEditor( typeof( FrictionPreset ) )]
public class FrictionPresetControlWidget : ControlWidget
{
	public override bool SupportsMultiEdit => true;
	public override bool IsWideMode => true;
	public override bool IncludeLabel => false;

	private readonly CurveControlWidget CurveControl;
	public FrictionPresetControlWidget( SerializedProperty property ) : base( property )
	{
		if ( !property.TryGetAsObject( out var obj ) )
		{
			Log.Error( "Couldn't get Friction Preset" );
		}
		Layout = Layout.Column();
		Layout.Margin = 8f;

		Layout.Add( new Label.Body( $" {ToolTip}" ) { Color = Color.White } );

		Layout.Add( new Label( "Pacejka Parameters" ) );
		Layout.Add( new Separator( 4 ) );
		Layout.Add( new FloatControlWidget( obj.GetProperty( "Stiffnes" ) ) { Label = "B", MouseMove = Update } );
		Layout.Add( new Separator( 4 ) );
		Layout.Add( new FloatControlWidget( obj.GetProperty( "ShapeFactor" ) ) { Label = "C", MouseMove = Update } );
		Layout.Add( new Separator( 4 ) );
		Layout.Add( new FloatControlWidget( obj.GetProperty( "PeakValue" ) ) { Label = "D", MouseMove = Update } );
		Layout.Add( new Separator( 4 ) );
		Layout.Add( new FloatControlWidget( obj.GetProperty( "CurvatureFactor" ) ) { Label = "E", MouseMove = Update } );
		Layout.Add( new Separator( 8 ) );
		Layout.Add( new Label( "Friction Curve Preview" ) );
		Layout.Add( new Separator( 4 ) );

		CurveControl = Layout.Add( new CurveControlWidget( obj.GetProperty( "Curve" ) ) { MinimumHeight = 80f, Enabled = false } );
		OnChildValuesChanged += ValuesChanged;

	}

	private void ValuesChanged( Widget widget )
	{
		if ( widget == CurveControl )
			return;

		CurveControl.Update();
	}
	private void Update( Vector2 _ )
	{
		CurveControl.Update();
	}
	protected override void PaintUnder()
	{
		Paint.ClearPen();

		Rect localRect = LocalRect;
		Paint.SetBrush( ControlColor.Lighten( 0.25f ) );
		Paint.DrawRect( in localRect, ControlRadius );
	}
}
