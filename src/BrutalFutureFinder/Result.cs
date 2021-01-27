using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrutalFutureFinder
{
    public class Result
    {
        public int FirstIndex;
        public double Corellation;

        public Result(int firstIndex, double corellation)
        {
            FirstIndex = firstIndex;
            Corellation = corellation;
        }
    }
}
