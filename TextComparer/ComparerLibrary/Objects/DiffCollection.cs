using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Comparer.Utils.Objects
{
    public class DiffCollection : List<Diff>
    {
        public DiffCollection()
            : base() { }

        DiffCollection(IEnumerable<Diff> collection) 
            : base(collection) { }
    }
}
