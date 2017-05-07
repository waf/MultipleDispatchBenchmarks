using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DispatchBenchmark
{
    public partial class DispatchBenchmark
    {
        [Benchmark]
        public string[] IfElsePatternDispatch()
        {
            return shapes
                .Select(shape =>
                {
                    if (shape is Triangle t) return Describe(t);
                    else if (shape is Quadrangle q) return Describe(q);
                    else if (shape is Pentagon p) return Describe(p);
                    else if (shape is Hexagon h) return Describe(h);
                    else throw new Exception("Unexpected shape");
                })
                .ToArray();
        }
    }
}
