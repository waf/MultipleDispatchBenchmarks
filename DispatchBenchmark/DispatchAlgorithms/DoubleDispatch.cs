using BenchmarkDotNet.Attributes;
using System;
using System.Linq;

namespace DispatchBenchmark
{
    using YSharp.Design.DoubleDispatch;
    using YSharp.Design.DoubleDispatch.Extensions;

    public partial class DispatchBenchmark
    {
        private DoubleDispatchObject dispatchObject;

        public string Describe(Shape shape) =>
            this.EnsureThreadSafe(ref dispatchObject)
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