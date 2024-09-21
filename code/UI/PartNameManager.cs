
namespace DM.UI;

public sealed class PartNameManager : Component
{
	[Property] public List<PartName> PartNames;
	private bool rendernames = false;

	protected override void OnAwake()
	{
		PartNames = Components.GetAll<PartName>( FindMode.InDescendants ).ToList();
		foreach ( var item in PartNames )
			item.Enabled = rendernames;
	}
	public bool RenderNames
	{
		get => rendernames;
		set
		{
			rendernames = value;
			foreach ( var item in PartNames )
				item.Enabled = rendernames;
		}
	}
}
