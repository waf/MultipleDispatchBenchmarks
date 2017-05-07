using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DispatchBenchmark
{
    public partial class DispatchBenchmark : IVisitor
    {
        [Benchmark]
        public string[] VisitorDispatch()
        {
            return visiteeShapes
                .Select(shape =>
                {
                    return shape.Accept(this);
                })
                .ToArray();
        }
    }

    public interface IVisitor
    {
        string Describe(Triangle shape);
        string Describe(Quadrangle shape);
        string Describe(Pentagon shape);
        string Describe(Hexagon shape);
    }
    public interface IVisitee
    {
        string Accept(IVisitor visitor);
    }
    public class TriangleVisitee : Triangle, IVisitee
    {
        public string Accept(IVisitor visitor) => visitor.Describe(this);
    }
    public class QuadrangleVisitee : Quadrangle, IVisitee
    {
        public string Accept(IVisitor visitor) => visitor.Describe(this);
    }
    public class PentagonVisitee : Pentagon, IVisitee
    {
        public string Accept(IVisitor visitor) => visitor.Describe(this);
    }
    public class HexagonVisitee : Hexagon, IVisitee
    {
        public string Accept(IVisitor visitor) => visitor.Describe(this);
    }
}
