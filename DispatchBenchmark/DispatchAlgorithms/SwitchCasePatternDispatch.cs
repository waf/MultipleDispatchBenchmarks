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
        public string[] SwitchCasePatternDispatch()
        {
            return shapes
                .Select(shape =>
                {
                    switch (shape)
                    {
                        case Triangle t:
                            return Describe(t);
                        case Quadrangle q:
                            return Describe(q);
                        case Pentagon p:
                            return Describe(p);
                        case Hexagon h:
                            return Describe(h);
                        default:
                            throw new Exception("Unexpected shape");
                    }
                })
                .ToArray();
        }
    }
}
