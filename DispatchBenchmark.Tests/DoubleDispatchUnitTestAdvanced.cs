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

    #region Classic Space Object Collision Example
    /*
     * See https://en.wikipedia.org/wiki/Multiple_dispatch#Examples
     */
    public class SpaceObject
    {
        private DoubleDispatchObject doubleDispatchObject;

        public string CollideWith(SpaceObject other) =>
            this.EnsureThreadSafe(ref doubleDispatchObject)
            .Via(nameof(CollideWith), other, () => default(string) ?? throw new NotImplementedException());
    }

    // When "this" is more massive, it obliterates the other space object,
    // otherwise, two space objects of the same type destroy each other
    public class TelecomSatellite : SpaceObject
    {
        protected string CollideWith(TelecomSatellite other) =>
            "the satellites destroy each other";

        protected string CollideWith(Spaceship spaceship) =>
            spaceship.CollideWith(this);

        protected string CollideWith(Asteroid asteroid) =>
            asteroid.CollideWith(this);

        protected string CollideWith(Planet planet) =>
            planet.CollideWith(this);
    }

    public class Spaceship : SpaceObject
    {
        protected string CollideWith(TelecomSatellite satellite) =>
            "the spaceship obliterates the satellite";

        protected string CollideWith(Spaceship spaceship) =>
            "the spaceships destroy each other";

        protected string CollideWith(Asteroid asteroid) =>
            asteroid.CollideWith(this);

        protected string CollideWith(Planet planet) =>
            planet.CollideWith(this);
    }

    public class Asteroid : SpaceObject
    {
        protected string CollideWith(TelecomSatellite satellite) =>
            "the asteroid obliterates the satellite";

        protected string CollideWith(Spaceship spaceship) =>
            "the asteroid obliterates the spaceship";

        protected string CollideWith(Asteroid asteroid) =>
            "the asteroids destroy each other";

        protected string CollideWith(Planet planet) =>
            planet.CollideWith(this);
    }

    public class Planet : SpaceObject
    {
        protected string CollideWith(TelecomSatellite satellite) =>
            "the planet obliterates the satellite";

        protected string CollideWith(Spaceship spaceship) =>
            "the planet obliterates the spaceship";

        protected string CollideWith(Asteroid asteroid) =>
            "the planet obliterates the asteroid";

        protected string CollideWith(Planet planet) =>
            "the planets destroy each other";
    }
    #endregion

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

        #region Classic Space Object Collision Example
        /*
         * See https://en.wikipedia.org/wiki/Multiple_dispatch#Examples
         */
        [Fact]
        public void ClassicSpaceObjectCollisionExample_CanDispatchAsExpected()
        {
            var planet1 = new Planet();
            var planet2 = new Planet();
            var asteroid1 = new Asteroid();
            var asteroid2 = new Asteroid();
            var spaceship1 = new Spaceship();
            var spaceship2 = new Spaceship();
            var satellite1 = new TelecomSatellite();
            var satellite2 = new TelecomSatellite();

            Assert.Equal(planet1.CollideWith(planet2), "the planets destroy each other");
            Assert.Equal(planet1.CollideWith(asteroid1), "the planet obliterates the asteroid");
            Assert.Equal(asteroid1.CollideWith(planet1), "the planet obliterates the asteroid");
            Assert.Equal(planet1.CollideWith(spaceship1), "the planet obliterates the spaceship");
            Assert.Equal(spaceship1.CollideWith(planet1), "the planet obliterates the spaceship");
            Assert.Equal(planet1.CollideWith(satellite1), "the planet obliterates the satellite");
            Assert.Equal(satellite1.CollideWith(planet1), "the planet obliterates the satellite");

            Assert.Equal(asteroid1.CollideWith(asteroid2), "the asteroids destroy each other");
            Assert.Equal(asteroid1.CollideWith(spaceship1), "the asteroid obliterates the spaceship");
            Assert.Equal(spaceship1.CollideWith(asteroid1), "the asteroid obliterates the spaceship");
            Assert.Equal(asteroid1.CollideWith(satellite1), "the asteroid obliterates the satellite");
            Assert.Equal(satellite1.CollideWith(asteroid1), "the asteroid obliterates the satellite");

            Assert.Equal(spaceship1.CollideWith(spaceship2), "the spaceships destroy each other");
            Assert.Equal(spaceship1.CollideWith(satellite1), "the spaceship obliterates the satellite");
            Assert.Equal(satellite1.CollideWith(spaceship1), "the spaceship obliterates the satellite");

            Assert.Equal(satellite1.CollideWith(satellite2), "the satellites destroy each other");
        }
        #endregion
    }
}