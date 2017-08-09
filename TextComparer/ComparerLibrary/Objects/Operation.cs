using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Comparer.Utils.Objects
{

    /**
   * The data structure representing a diff is a List of Diff objects:
   * {Diff(Operation.DELETE, "Hello"), Diff(Operation.INSERT, "Goodbye"),
   *  Diff(Operation.EQUAL, " world.")}
   * which means: delete "Hello", add "Goodbye" and keep " world."
   */
    public enum Operation
    {
        DELETE, INSERT, EQUAL
    }
}
