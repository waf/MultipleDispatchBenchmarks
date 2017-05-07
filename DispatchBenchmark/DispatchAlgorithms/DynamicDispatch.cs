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
        public string[] DynamicDispatch()
        {
            return shapes
                .Select(shape =>
                {
                    return (string)Describe((dynamic)shape);
                })
                .ToArray();
        }
    }
}
