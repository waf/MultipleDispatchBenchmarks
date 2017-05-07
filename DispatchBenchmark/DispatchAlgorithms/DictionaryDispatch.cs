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
        private IReadOnlyDictionary<Type, Func<Shape, string>> Functions;

        public void Init()
        {
            this.Functions = new Dictionary<Type, Func<Shape, string>>()
            {
                { typeof(Triangle), t => Describe((Triangle)t) },
                { typeof(Quadrangle), q => Describe((Quadrangle)q) },
                { typeof(Pentagon), p => Describe((Pentagon)p) },
                { typeof(Hexagon), p => Describe((Hexagon)p) }
            };
        }

        [Benchmark]
        public string[] DictionaryDispatch()
        {
            return shapes
                .Select(shape =>
                {
                    return Functions[shape.GetType()].Invoke(shape);
                })
                .ToArray();
        }
    }
}
