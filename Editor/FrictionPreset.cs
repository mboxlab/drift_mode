//using DM.Vehicle;
//using Editor.GraphicsItems;
//using Sandbox;

//[CustomEditor( typeof( FrictionPreset ) )]
//public class FrictionPresetControlWidget : ControlWidget
//{
//	public override bool SupportsMultiEdit => true;

//	public FrictionPresetControlWidget( SerializedProperty property ) : base( property )
//	{
//		if ( !property.TryGetAsObject( out var obj ) )
//		{
//			Log.Error( "Couldn't get Wheel Friction Info" );
//		}

//		Layout.Add( new VectorControlWidget ( obj.GetProperty( "BCDE" ) ) );
//		Layout.Add( new CurveControlWidget( obj.GetProperty( "Curve" ) ) );
//	}

//	protected override void PaintUnder()
//	{
//		Paint.ClearPen();

//		Rect localRect = LocalRect;
//		Paint.SetBrush( ControlColor.Lighten( 0.25f ) );
//		Paint.DrawRect( in localRect, ControlRadius );
//	}
//}
