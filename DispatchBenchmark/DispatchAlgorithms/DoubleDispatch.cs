using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DispatchBenchmark
{
    using YSharp.Design.DoubleDispatch;

    public partial class DispatchBenchmark
    {
        private DoubleDispatchObject dispatch;

        public string Describe(Shape shape) =>
            (dispatch = dispatch ?? new DoubleDispatchObject(this))
            .Via(Describe, shape, () => throw new Exception("Unexpected shape"));

        [Benchmark]
        public string[] DoubleDispatch()
        {
            return shapes
                .Select(shape =>
                {
                    return Describe(shape);
                })
                .ToArray();
        }
    }
}