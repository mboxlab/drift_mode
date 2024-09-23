using System;
using System.Threading;

namespace DM.Car;

[Icon( "checkroom" )]
public sealed class CarDresser : Component, Component.INetworkListener
{
	[Property] public List<Part> Parts = new();

	protected override void OnStart()
	{
		base.OnStart();

		Parts = Components.GetAll<Part>( FindMode.InDescendants ).ToList();
	}

	// TODO 
	// save/load system in garage
	//[Property] public ModelRenderer BodyRenderer { get; set; }
	//[Property] public List<GameObject> Poses { get; set; }
	//public void OnNetworkSpawn( Connection owner )
	//{
	//int partid = 0;
	//Material mat = BodyRenderer.MaterialOverride;

	//foreach ( GameObject pos in Poses )
	//{
	//	PartsList parts = pos.Components.Get<PartsList>();
	//	if ( parts.Parts[partid] is null )
	//		continue;
	//	GameObject part = new( true, "Model" )
	//	{
	//		Parent = pos
	//	};
	//	var partrenderer = part.Components.Create<ModelRenderer>();
	//	partrenderer.Model = parts.Parts[partid];
	//	partrenderer.MaterialOverride = mat;

	//}
	//}
	//protected override void OnStart()
	//{
	//if ( IsProxy ) return;
	//int partid = 0;
	//Material mat = BodyRenderer.MaterialOverride;

	//foreach ( GameObject pos in Poses )
	//{
	//	PartsList parts = pos.Components.Get<PartsList>();
	//	if ( parts.Parts[partid] is null )
	//		continue;
	//	GameObject part = new( true, "Model" )
	//	{
	//		Parent = pos
	//	};
	//	var partrenderer = part.Components.Create<ModelRenderer>();
	//	partrenderer.Model = parts.Parts[partid];
	//	partrenderer.MaterialOverride = mat;

	//}
	//}
}
