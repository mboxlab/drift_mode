namespace DM.Car;

[Icon( "extension" )]
public sealed class Part : Component
{
	[Property] public string Name;

	private Model _current;
	[Property] public Model Current
	{
		get => _current;
		set => Dress( value );
	}

	[Property] public int Index
	{
		get => Models.IndexOf( Current );
		set => Current = Models.ElementAtOrDefault( value );
	}

	private bool _rendering;
	[Property] public bool Rendering
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

		// fix
		Dress( _current );
	}

	public void Dress( int index ) => Index = index;
	public void Dress( Model model )
	{
		_current = model;
		Rendering = _current is not null;
		if ( _current is null ) return;

		foreach ( ModelRenderer renderer in Renderers )
		{
			renderer.Model = _current;
			Material material = renderer.MaterialOverride;
			renderer.ClearMaterialOverrides();
			renderer.MaterialOverride = material;
		}
	}
}
