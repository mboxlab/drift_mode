
namespace DM.Car;

using System;
using DM.UI;

public sealed class CarSelector : Component
{
	[Property] public List<GameObject> Cars { get; set; }
	public Action<GameObject> OnCarChanged { get; set; }
	public static GameObject ActiveCar { get; set; }
	public BBox ActiveCarBounds { get; set; }
	public Vector3 ActiveCarCenter { get; set; }
	public int car { get; set; }

	protected override void OnStart()
	{
		base.OnStart();

		ChangeCar( Cars.First() );
	}
	
	protected override void OnUpdate()
	{
		//if ( MouseWorldInput.Input.MouseLeftPressed )
		//{
		//	Ray ray = MouseWorldInput.Input.Ray;
		//	SceneTraceResult result = Scene.Trace.Ray( ray, Scene.Camera.ZFar ).WithTag( "car" ).Run();
		//	if ( result.Hit ) InteractiveCamera.Instance.Focus( ActiveCar );
		//}

		//if ( Input.Pressed( "Left" ) )
		//{
		//	car = (car + 1) % Cars.Count;
		//	if ( ActiveCar.IsValid() )
		//	{
		//		ChangeCar( Cars[car] );
		//	}
		//}
	}

	private void ChangeCar( GameObject newCar )
	{
		ActiveCar?.Destroy();
		GameObject car = newCar.Clone();
		car.Name = newCar.Name;
		car.Transform.Position = Transform.Position;
		ActiveCar = car;
		ActiveCarBounds = car.GetBounds();
		ActiveCarCenter = ActiveCarBounds.Center;
		//ActiveCar.Components.Get<InteractiveObject>().LocalPosition = new Vector3( 0, 0, ActiveCarBounds.Center.z );
		OnCarChanged?.Invoke( car );
	}
}
