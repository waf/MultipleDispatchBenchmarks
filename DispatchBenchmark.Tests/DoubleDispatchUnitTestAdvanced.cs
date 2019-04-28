using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace DispatchBenchmark.Tests
{
    using System.Text;
    using System.Xml;
    using YSharp.Design.DoubleDispatch;
    using YSharp.Design.DoubleDispatch.Extensions;

    #region Memoizing Pattern Matcher Sample / Test
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
    #endregion

    #region Classic Space Object Collision Sample / Test
    /*
     * See https://en.wikipedia.org/wiki/Multiple_dispatch#Examples
     */
    public class SpaceObject
    {
        private DoubleDispatchObject dispatch;

        public string CollideWith(SpaceObject other) =>
            this.EnsureThreadSafe(ref dispatch)
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

    #region Double Dispatch Over Static Method Overloads (Several Static Classes) Sample / Test
    public struct IAmNotANumber { }

    public static class MathV2
    {
        // Implements the double dispatch of this MathV2's static methods, be they novelty ones or defaults
        private static DoubleDispatchObject dispatch;

        // Caches the surrogate used to implement double dispatch over the System.Math.Abs(?) method's overloads
        private static readonly Func<object, object> system_math_abs =
            default(object)
            .CreateSurrogate
            (
                typeof(Math),
                nameof(Math.Abs),
                default(object),
                () =>
                    throw new NotImplementedException()
            );

        // Invokes the best System.Math.Abs(?) overload through number's TNumber runtime type
        private static TNumber MathAbs<TNumber>(TNumber number) =>
            (TNumber)system_math_abs(number);

        // Makes this MathV2.Abs<TNumber>(TNumber) default to double dispatch over the System.Math.Abs(?) method's overloads
        // when the TNumber argument doesn't match our novelty signature (see below)
        public static TNumber Abs<TNumber>(TNumber number)
            where TNumber : struct =>
            MathAbs(number);

        // This MathV2's novelty signature
        public static TNumber[] Abs<TNumber>(params TNumber[] numbers)
            where TNumber : struct =>
            numbers?
            .Select
            (
                number =>
                    typeof(MathV2).EnsureThreadSafe(ref dispatch) // as usual
                    .Via(nameof(Math.Abs), number, MathAbs(number)/* <- Same default as the above */)
            )
            .ToArray();
    }
    #endregion

    #region Double Dispatch Through Contra-Covariant Delegate Types Parameters Sample / Test
    // The driver function to be passed in the below ETLProcess<TModel>.Execute(...) method
    public delegate bool ETLDriver<TSource, TModel, TData>(
        TSource source, ETLProcess<TModel> process, TData data
    );

    public abstract class ETLProcess<TModel>
    {
        private readonly StringBuilder logger = new StringBuilder();
        private DoubleDispatchObject dispatch;

        public override string ToString() =>
            logger.ToString();

        public void LogWork(string message) =>
            logger.Append(message);

        /// <summary>
        /// Takes a domain model and transforms it into another;
        /// doesn't know (or care about) how the input model
        /// is obtained, or how to persist the resulting one
        /// </summary>
        public abstract TModel Transform(TModel model);

        /// <summary>
        /// Invokes a driver function typed by a source (TSource) and a persistable representation (TData)
        /// of its domain model (TModel), providing it with,
        /// 
        /// a) a new TSource instance,
        /// b) self,
        /// c) and a new TData instance
        /// 
        /// that the driver function uses to determine the outcome (bool : success vs failure)
        /// of the overall ETL (Extract-Transform-Load) process, that is,
        /// 
        /// 1) Extract an input TModel from TSource
        /// 2) Transform the input TModel of (1) into another
        /// 3) Load the resulting TModel of (2) into a TData store
        /// 
        /// Thus, the driver function does not know how to transform a TModel
        /// (let alone what the transform is supposed to be about), but
        /// it does know how to call TSource to obtain the input TModel,
        /// how to call back this process' Transform(TModel) function with it,
        /// and how to call TData to persist the transform's resulting TModel
        /// </summary>
        public bool Execute<TSource, TData>(ETLDriver<TSource, TModel, TData> driver) =>
            this.EnsureThreadSafe(ref dispatch) // as usual
            .Via
            (
                nameof(Execute),
                driver,
                (Func<bool>)(() => throw new NotImplementedException($"unsupported source ({typeof(TSource).Name}) &/or data ({typeof(TData).Name})"))
            );
    }

    public class FileSystemMock { public XmlDocument LoadXmlFromFile() => new XmlDocument(); }
    public class WebResourceMock { public XmlDocument GetXmlResponse() => new XmlDocument(); }
    public class MongoDBMock { public void Save(XmlDocument model) { } }
    public class SQLServerDBMock { public void BulkInsert(XmlDocument model) { } }

    public class ProcessXML : ETLProcess<XmlDocument>
    {
        public override XmlDocument Transform(XmlDocument model) =>
            model; // (Simply the identity function for test/demo purposes)

        protected bool Execute(ETLDriver<FileSystemMock, XmlDocument, MongoDBMock> driver) =>
            driver(new FileSystemMock(), this, new MongoDBMock());

        protected bool Execute(ETLDriver<FileSystemMock, XmlDocument, SQLServerDBMock> driver) =>
            driver(new FileSystemMock(), this, new SQLServerDBMock());

        protected bool Execute(ETLDriver<WebResourceMock, XmlDocument, MongoDBMock> driver) =>
            driver(new WebResourceMock(), this, new MongoDBMock());

        //Missing:
        //protected bool Execute(ETLDriver<WebResourceMock, XmlDocument, SQLServerDBMock> driver) =>
        //    driver(new WebResourceMock(), this, new SQLServerDBMock());
    }
    #endregion

    public class DoubleDispatchUnitTestAdvanced
    {
        #region Memoizing Pattern Matcher Sample / Test
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

            Assert.False(ReferenceEquals(computation1_1, computation2));
            Assert.False(ReferenceEquals(computation1_2, computation2));
            Assert.False(ReferenceEquals(computation1_3, computation2));

            Assert.IsType(typeof(Computation1), computation1_1);
            Assert.IsType(typeof(Computation1), computation1_2);
            Assert.IsType(typeof(Computation1), computation1_3);
            Assert.IsType(typeof(Computation2), computation2);

            Assert.False(ReferenceEquals(computation1_1, computation1_2));
            Assert.True(ReferenceEquals(computation1_1, computation1_3));
        }
        #endregion

        #region Classic Space Object Collision Sample / Test
        /*
         * See https://en.wikipedia.org/wiki/Multiple_dispatch#Examples
         */
        [Fact]
        public void ClassicSpaceObjectCollision_CanDispatchAsExpected()
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

        #region Double Dispatch Over Static Method Overloads (Several Static Classes) Sample / Test
        [Fact]
        public void MathV2_CanDispatchAsExpected()
        {
            int ainteger = MathV2.Abs(-123);
            double adouble = MathV2.Abs(-456.0);
            decimal adecimal = MathV2.Abs(-789m);

            int[] aintegervect = MathV2.Abs(123, -456, 789);
            double[] adoublevect = MathV2.Abs(123.0, -456.0, 789.0);
            decimal[] adecimalvect = MathV2.Abs(123m, -456m, 789m);

            Assert.Equal(123, ainteger);
            Assert.Equal(456.0, adouble);
            Assert.Equal(789m, adecimal);

            Assert.Equal(new[] { 123, 456, 789 }, aintegervect);
            Assert.Equal(new[] { 123.0, 456.0, 789.0 }, adoublevect);
            Assert.Equal(new[] { 123m, 456m, 789m }, adecimalvect);
        }

        [Fact]
        public void MathV2_ShouldThrowNotImplementedException_WhenDispatchCompletelyFails()
        {
            Action tryBadDoubleDispatchOverMathV2 =
                () =>
                {
                    var neverSet = MathV2.Abs(new IAmNotANumber());
                };

            Exception error = null;
            try
            {
                tryBadDoubleDispatchOverMathV2();
            }
            catch (Exception ex)
            {
                error = ex;
            }

            Assert.NotNull(error);
            Assert.IsType(typeof(NotImplementedException), error);
        }
        #endregion

        #region Double Dispatch Through Contra-Covariant Delegate Types Parameters Sample / Test
        [Fact]
        public void ProcessXML_CanDispatchDriverFunctionsAsExpected()
        {
            ETLDriver<FileSystemMock, XmlDocument, MongoDBMock> fromFileSystem_ToXML_ToMongoDB =
                (fs, process, mongo) =>
                {
                    // Only the driver knows about that:
                    var input = fs.LoadXmlFromFile();

                    // Common to all ETL processes:
                    var result = process.Transform(input);

                    // Only the driver knows about that:
                    mongo.Save(result);

                    process.LogWork("ETL: Extract from file system, Transform, and Load into MongoDB");
                    return true;
                };

            ETLDriver<WebResourceMock, XmlDocument, MongoDBMock> fromWeb_ToXML_ToMongoDB =
                (web, process, mongo) =>
                {
                    // Only the driver knows about that:
                    var input = web.GetXmlResponse();

                    // Common to all ETL processes:
                    var result = process.Transform(input);

                    // Only the driver knows about that:
                    mongo.Save(result);

                    process.LogWork("ETL: Extract from web resource, Transform, and Load into MongoDB");
                    return true;
                };

            ETLDriver<FileSystemMock, XmlDocument, SQLServerDBMock> fromFileSystem_ToXML_ToSQLServer =
                (fs, process, sql) =>
                {
                    // Only the driver knows about that:
                    var input = fs.LoadXmlFromFile();

                    // Common to all ETL processes:
                    var result = process.Transform(input);

                    // Only the driver knows about that:
                    sql.BulkInsert(result);

                    process.LogWork("ETL: Extract from file system, Transform, and Load into SQLServer");
                    return true;
                };

            var etlProcess1 = new ProcessXML();
            var outcome1 = etlProcess1.Execute(fromFileSystem_ToXML_ToMongoDB);

            var etlProcess2 = new ProcessXML();
            var outcome2 = etlProcess2.Execute(fromWeb_ToXML_ToMongoDB);

            var etlProcess3 = new ProcessXML();
            var outcome3 = etlProcess3.Execute(fromFileSystem_ToXML_ToSQLServer);

            Assert.True(outcome1);
            Assert.Equal("ETL: Extract from file system, Transform, and Load into MongoDB", $"{etlProcess1}");

            Assert.True(outcome2);
            Assert.Equal("ETL: Extract from web resource, Transform, and Load into MongoDB", $"{etlProcess2}");

            Assert.True(outcome3);
            Assert.Equal("ETL: Extract from file system, Transform, and Load into SQLServer", $"{etlProcess3}");
        }

        [Fact]
        public void ProcessXML_ShouldThrowNotImplementedException_WhenDispatchCompletelyFails()
        {
            ETLDriver<WebResourceMock, XmlDocument, SQLServerDBMock> fromWebResource_ToXML_ToSQLServer =
                (web, process, sql) =>
                {
                    // Only the driver knows about that:
                    var input = web.GetXmlResponse();

                    // Common to all ETL processes:
                    var result = process.Transform(input);

                    // Only the driver knows about that:
                    sql.BulkInsert(result);

                    process.LogWork("ETL: Extract from web resource, Transform, and Load into SQLServer");
                    return true;
                };

            Action tryBadDoubleDispatchOverProcessXML =
                () =>
                {
                    var neverSet = new ProcessXML().Execute(fromWebResource_ToXML_ToSQLServer);
                };

            Exception error = null;
            try
            {
                tryBadDoubleDispatchOverProcessXML();
            }
            catch (Exception ex)
            {
                error = ex;
            }

            Assert.NotNull(error);
            Assert.IsType(typeof(NotImplementedException), error);
            Assert.Equal("unsupported source (WebResourceMock) &/or data (SQLServerDBMock)", error.Message);
        }
        #endregion
    }
}