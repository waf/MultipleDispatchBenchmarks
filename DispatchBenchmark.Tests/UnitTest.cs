using System;
using Xunit;

namespace DispatchBenchmark.Tests
{
    public class UnitTest
    {
        private readonly DispatchBenchmark benchmark;
        private readonly string[] expected;

        public UnitTest()
        {
            this.benchmark = new DispatchBenchmark()
            {
                shapes = new Shape[]
                {
                    new Hexagon(), new Pentagon(),
                    new Quadrangle(), new Triangle()
                },
                visiteeShapes = new IVisitee[]
                {
                    new HexagonVisitee(), new PentagonVisitee(),
                    new QuadrangleVisitee(), new TriangleVisitee()
                }
            };
            this.expected = new[]
            {
                this.benchmark.Describe(new Hexagon()),
                this.benchmark.Describe(new Pentagon()),
                this.benchmark.Describe(new Quadrangle()),
                this.benchmark.Describe(new Triangle()),
            };
        }

        [Fact]
        public void Dictionary_CorrectlyDispatches()
        {
            var results = this.benchmark.DictionaryDispatch();
            Assert.Equal(expected, results);
        }

        [Fact]
        public void Dynamic_CorrectlyDispatches()
        {
            var results = this.benchmark.DynamicDispatch();
            Assert.Equal(expected, results);
        }

        [Fact]
        public void IfElsePattern_CorrectlyDispatches()
        {
            var results = this.benchmark.IfElsePatternDispatch();
            Assert.Equal(expected, results);
        }

        [Fact]
        public void SwitchCasePattern_CorrectlyDispatches()
        {
            var results = this.benchmark.SwitchCasePatternDispatch();
            Assert.Equal(expected, results);
        }

        [Fact]
        public void Visitor_CorrectlyDispatches()
        {
            var results = this.benchmark.VisitorDispatch();
            Assert.Equal(expected, results);
        }
    }
}
