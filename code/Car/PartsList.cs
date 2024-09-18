using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DM.Car;

public sealed class PartsList : Component
{
	[Property] public List<Model> Parts;
}
