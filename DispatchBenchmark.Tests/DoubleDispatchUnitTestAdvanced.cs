using System;
using System.Collections.Generic;
using Xunit;

namespace DispatchBenchmark.Tests
{
    using YSharp.Design.DoubleDispatch;
    using YSharp.Design.DoubleDispatch.Extensions;

    public class MemoizingDispatchObject : DoubleDispatchObject
    {
        private readonly IDictionary<object, object> _memoization = new Dictionary<object, object>();

        public MemoizingDispatchObject(object target) : base(target) { }

        public TOut GetOrCache<TIn, TOut>(TIn input, Func<TIn, TOut> compute)
            where TIn : class
            where TOut : class
        {
            if (!_memoization.TryGetValue(input, out var output))
            {
                _memoization.Add(input, output = compute(input));
            }
            return (TOut)output;
        }
    }

    public class Pattern { }
    public class Pattern1 : Pattern { }
    public class Pattern2 : Pattern { }

    public class Computation { }
    public class Computation<TInput> : Computation
    {
        protected Computation(TInput input) =>
            Input = input;

        public TInput Input { get; private set; }
    }
    public class Computation1 : Computation<Pattern1> { public Computation1(Pattern1 input) : base(input) { } }
    public class Computation2 : Computation<Pattern2> { public Computation2(Pattern2 input) : base(input) { } }

    public class MemoizingPatternMatcher
    {
        private MemoizingDispatchObject dispatchObject;

        public Computation Match(Pattern pattern) =>
            this.EnsureThreadSafe(ref dispatchObject, target => new MemoizingDispatchObject(target))
            .Via(nameof(Match), pattern, default(Computation));

        public Computation1 Match(Pattern1 pattern) =>
            dispatchObject.GetOrCache(pattern, input => new Computation1(input));

        public Computation2 Match(Pattern2 pattern) =>
            dispatchObject.GetOrCache(pattern, input => new Computation2(input));
    }

    public class DoubleDispatchUnitTestAdvanced
    {
        [Fact]
        public void MemoizingPatternMatcher_CanMatchAndMemoize()
        {
            var match = (Func<Pattern, Computation>)new MemoizingPatternMatcher().Match;

            var pattern1_1 = new Pattern1();
            var pattern1_2 = new Pattern1();
            var pattern2 = new Pattern2();

            var computation1_1 = match(pattern1_1);
            var computation1_2 = match(pattern1_2);
            var computation1_3 = match(pattern1_1);
            var computation2 = match(pattern2);

            Assert.Equal(false, ReferenceEquals(computation1_1, computation2));
            Assert.Equal(false, ReferenceEquals(computation1_2, computation2));
            Assert.Equal(false, ReferenceEquals(computation1_3, computation2));

            Assert.IsType(typeof(Computation1), computation1_1);
            Assert.IsType(typeof(Computation1), computation1_2);
            Assert.IsType(typeof(Computation1), computation1_3);
            Assert.IsType(typeof(Computation2), computation2);

            Assert.Equal(false, ReferenceEquals(computation1_1, computation1_2));
            Assert.Equal(true, ReferenceEquals(computation1_1, computation1_3));
        }
    }
}