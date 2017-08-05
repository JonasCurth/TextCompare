using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace utils
{
    //public class Patch
    //{
    //    public List<Difference> diffs = new List<Difference>();
    //    public int start1, start2;
    //    public int length1, length2;

    //    public override string ToString()
    //    {
    //        string coords1, coords2;
    //        if (0 == this.length1)
    //        {
    //            coords1 = $"{this.start1},0";
    //        }
    //        else if (1 == this.length1)
    //        {
    //            coords1 = Convert.ToString(this.start1 + 1);
    //        }else
    //        {
    //            coords1 = $"{this.start1 + 1},{this.length1}";
    //        }

    //        if (0 == this.length2)
    //        {
    //            coords2 = $"{this.start2},0";
    //        }
    //        else if (1 == this.length2)
    //        {
    //            coords2 = Convert.ToString(this.start2 + 1);
    //        }
    //        else
    //        {
    //            coords2 = $"{this.start2 + 1},{this.length2}";
    //        }

    //        StringBuilder text = new StringBuilder();
    //        text.Append("@@ -")
    //            .Append(coords1)
    //            .Append(" +")
    //            .Append(coords2)
    //            .Append(" @@\n");

    //        foreach (var diff in this.diffs)
    //        {
    //            switch (diff.Operation)
    //            {
    //                case Operation.DELETE:
    //                    text.Append("-");
    //                    break;
    //                case Operation.INSERT:
    //                    text.Append("+");
    //                    break;
    //                case Operation.EQUAL:
    //                    text.Append(" ");
    //                    break;
    //            }
    //        }
    //    }
    //}
}
