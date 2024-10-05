namespace Sandbox.Car;

[Icon( "extension" )]
public sealed class Part : Component
{
	[Property] public string Name;

	private Model _current;
	[Property]
	public Model Current { get => _current; private set => _current = value; }

	private int _index;
	[Property]
	public int Index { get => _index; private set => _index = value; }

	private bool _rendering;
	[Property]
	public bool Rendering
	{
		get => _rendering;
		set
		{
			_rendering = _current is null ? false : value;
			Renderers.ForEach( x => x.Enabled = _rendering );
		}
	}

	[Property] public List<Model> Models = new();
	[Property] public List<GameObject> Objects = new();

	private List<ModelRenderer> Renderers = new();

	protected override void OnStart()
	{
		base.OnStart();

		// Find main ModelRenderer
		ModelRenderer main = Components.Get<ModelRenderer>( FindMode.InAncestors );

		// If this list is empty then self covered by rendering.
		if ( Objects.Count == 0 ) Objects.Add( GameObject );

		// Creating ModelRenderer`s
		foreach ( GameObject obj in Objects )
		{
			ModelRenderer renderer = obj.Components.Create<ModelRenderer>();

			renderer.Enabled = false;
			renderer.ClearMaterialOverrides();
			renderer.MaterialOverride = main.MaterialOverride;

			Renderers.Add( renderer );
		}

		Dress( _current );
	}

	public void Dress( int index )
	{
		if ( index < 0 ) return;
		if ( Models.Count == 0 ) return;
		Dress( Models[index] );
	}
	public void Dress( Model model )
	{

		Index = Models.IndexOf( model );
		Current = model;
		Rendering = _current is not null;
		if ( !Rendering ) return;

		foreach ( ModelRenderer renderer in Renderers )
		{
			renderer.Model = _current;
			Material material = renderer.MaterialOverride;
			renderer.ClearMaterialOverrides();
			renderer.MaterialOverride = material;
		}

	}

	public void Save()
	{
		ICarDresserEvent.Post( x => x.Save() );
	}
}
