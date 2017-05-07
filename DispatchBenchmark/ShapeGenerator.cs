using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DispatchBenchmark
{
    /// <summary>
    /// Generates shape objects for the algorithms to dispatch.
    /// </summary>
    class ShapeGenerator
    {
        public static Shape[] BuildRandomInput(int length)
        {
            var random = new Random();
            return Enumerable.Range(1, length)
                             .Select(_ =>
                             {
                                 var r = random.Next(3);
                                 return r == 0 ? new Triangle() :
                                        r == 1 ? new Quadrangle() :
                                                 new Pentagon() as Shape;
                                                 
                             })
                             .ToArray();
        }

        /// <summary>
        /// Converts Shape objects to IVisitee objects for the <see cref="VisitorDispatch"/> algorithm.
        /// </summary>
        public static IVisitee[] MapToVisitees(Shape[] shapes)
        {
            return shapes.Select(
                shape => shape is Triangle   ? new TriangleVisitee() :
                         shape is Quadrangle ? new QuadrangleVisitee() :
                         shape is Pentagon   ? new PentagonVisitee() as IVisitee :
                                               throw new Exception("Unexpected shape")
            ).ToArray();
        }

    }
}
