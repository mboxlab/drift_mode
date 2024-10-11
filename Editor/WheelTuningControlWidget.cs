//using Sandbox;
//using Sandbox.Tuning;
//using Sandbox.UI;
//[CustomEditor( typeof( Tuning ) )]
//public class WheelTuningControlWidget : ControlWidget
//{
//	public override bool SupportsMultiEdit => true;
//	public override bool IncludeLabel => false;

//	public WheelTuningControlWidget( SerializedProperty property ) : base( property )
//	{
//		if ( !property.TryGetAsObject( out var obj ) )
//		{
//			Log.Error( "Couldn't get WheelTuning" );
//		}
//		Layout = Layout.Column();
//		Layout.Margin = new Margin( 8f, 0f, 0f, 0f );
//		var row = Layout.AddRow();
//		Log.Info( obj.GetProperty( "Value" ) );
//		row.Add( new Editor.Label( obj.GetProperty( "Name" ).GetValue<string>() ), 1 );
//		row.Add( new FloatControlWidget( obj.GetProperty( "Value" ) ) { Label = "V", ToolTip = "Value", }, 5 );
//	}

//	protected override void PaintUnder()
//	{
//		Paint.ClearPen();

//		Rect localRect = LocalRect;
//		Paint.SetBrush( ControlColor.Lighten( 0.25f ) );
//		Paint.DrawRect( in localRect, ControlRadius );
//	}
//}
