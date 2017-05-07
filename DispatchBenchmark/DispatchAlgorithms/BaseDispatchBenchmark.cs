using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DispatchBenchmark
{
    public abstract class Shape { }
    public class Triangle : Shape { }
    public class Quadrangle : Shape { }
    public class Pentagon : Shape { }
    public class Hexagon : Shape { }

    public partial class DispatchBenchmark
    {
        public Shape[] shapes;
        public IVisitee[] visiteeShapes;

        public DispatchBenchmark()
        {
            this.shapes = ShapeGenerator.BuildRandomInput(1000);
            // special shapes that implement the visitor pattern
            this.visiteeShapes = ShapeGenerator.MapToVisitees(shapes);
            Init();
        }

        public string Describe(Triangle shape) => "Three pointy angles";
        public string Describe(Quadrangle shape) => "Four angles";
        public string Describe(Pentagon shape) => "Looks kind of like a house.";
        public string Describe(Hexagon shape) => "Has six thingies";
    }
}
