
namespace DM.Car;

public sealed class CarDresser : Component, Component.INetworkSpawn
{
	// TODO 
	// save/load system in garage
	[Property] public ModelRenderer BodyRenderer { get; set; }
	[Property] public List<GameObject> Poses { get; set; }
	public void OnNetworkSpawn( Connection owner )
	{
		int partid = 0;
		Material mat = BodyRenderer.MaterialOverride;

		foreach ( GameObject pos in Poses )
		{
			PartsList parts = pos.Components.Get<PartsList>();
			if ( parts.Parts[partid] is null )
				continue;
			GameObject part = new( true, "Model" )
			{
				Parent = pos
			};
			var partrenderer = part.Components.Create<ModelRenderer>();
			partrenderer.Model = parts.Parts[partid];
			partrenderer.MaterialOverride = mat;

		}
	}
}
