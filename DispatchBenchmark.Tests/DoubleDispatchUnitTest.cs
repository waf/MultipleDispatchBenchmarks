using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;

namespace DispatchBenchmark.Tests
{
    using YSharp.Design.DoubleDispatch;
    using YSharp.Design.DoubleDispatch.Extensions;

    public class DoubleDispatchUnitTest
    {
        public class POCOEntity
        {
            protected POCOEntity(string id) =>
                Id = id;
            public override string ToString() =>
                $"{GetType().Name}({Id ?? string.Empty})";
            public string Id { get; private set; }
        }
        public class POCOItem : POCOEntity { protected POCOItem(string id) : base(id) { } }
        public class POCOFile : POCOItem { public POCOFile(string id) : base(id) { } }
        public class POCOLink : POCOItem { public POCOLink(string id) : base(id) { } }
        public class POCOSymLink : POCOItem { public POCOSymLink(string id) : base(id) { } }
        public class POCOFolder : POCOEntity { public POCOFolder(string id) : base(id) { } }
        public class POCOUnsupported : POCOEntity { public POCOUnsupported(string id) : base(id) { } }

        public class POCOServiceBase
        {
            protected readonly StringBuilder accumulator = new StringBuilder();

            public override string ToString() =>
                accumulator.ToString();

            public virtual void Handle(POCOEntity entity)
            {
                // No-op
            }

            // Ignored, unless statically bound at some call site somewhere, or
            // indirectly dispatched-to via a surrogate of a delegate bound to "this"
            public virtual void Handle(POCOFile file)
            {
                accumulator.AppendLine($"item: {file}");
            }
        }

        // DoubleDispatchObject usage through composition
        public class POCOServiceDerived : POCOServiceBase
        {
            private DoubleDispatchObject dispatch;

            public override void Handle(POCOEntity entity) =>
                this.EnsureThreadSafe(ref dispatch)
                .Via(nameof(Handle), entity,
                    () =>
                    {
                        accumulator.AppendLine($"unsupported entity: {(entity != null ? entity.ToString() : "<null>")}");
                    }
                );

            public override void Handle(POCOFile file)
            {
                accumulator.AppendLine($"file: {file}");
            }

            public void Handle(POCOLink link)
            {
                accumulator.AppendLine($"link: {link}");
            }

            public void Handle(POCOFolder folder)
            {
                accumulator.AppendLine($"folder: {folder}");
            }
        }

        // DoubleDispatchObject usage through inheritance
        public class ServiceV2 : DoubleDispatchObject
        {
            protected readonly StringBuilder accumulator = new StringBuilder();

            public override string ToString() =>
                accumulator.ToString();

            public void Handle(POCOEntity entity) =>
                Via(nameof(Handle), entity,
                    () =>
                    {
                        accumulator.AppendLine($"unsupported entity: {(entity != null ? entity.ToString() : "<null>")}");
                    }
                );

            public void Handle(POCOFile file)
            {
                accumulator.AppendLine($"file: {file}");
            }

            public void Handle(POCOLink link)
            {
                accumulator.AppendLine($"link: {link}");
            }

            public void Handle(POCOFolder folder)
            {
                accumulator.AppendLine($"folder: {folder}");
            }

            public void Handle(POCOItem item)
            {
                accumulator.AppendLine($"item: {item}");
            }
        }

        public class OpaqueValueResult
        {
            protected OpaqueValueResult(Guid guid) =>
                UniqueId = guid;

            public Guid UniqueId { get; private set; }
        }

        public class OpaqueValueOperation1 { }
        public class OpaqueValueResult1 : OpaqueValueResult
        {
            public OpaqueValueResult1(Guid guid) : base(guid) { }
        }

        public class OpaqueValueOperation2 { }
        public class OpaqueValueResult2 : OpaqueValueResult
        {
            public OpaqueValueResult2(Guid guid) : base(guid) { }
        }

        public struct OpaqueValue
        {
            private readonly Guid guid;

            public OpaqueValue(Guid guid) =>
                this.guid = guid;

            public object Apply(object operation) =>
                throw new NotImplementedException("Please use an overload or a surrogate");

            public OpaqueValueResult1 Apply(OpaqueValueOperation1 operation) =>
                new OpaqueValueResult1(guid);

            public OpaqueValueResult2 Apply(OpaqueValueOperation2 operation) =>
                new OpaqueValueResult2(guid);
        }

        [Fact]
        public void POCOServiceBase_OnlyImplementsNoops()
        {
            var service = new POCOServiceBase();

            POCOEntity entity = new POCOFile("file1");
            service.Handle(entity);
            entity = new POCOLink("link1");
            service.Handle(entity);
            entity = new POCOFolder("folder1");
            service.Handle(entity);
            entity = new POCOFile("file2");
            service.Handle(entity);
            entity = new POCOFolder("folder2");
            service.Handle(entity);

            var serviceText = service.ToString();

            Assert.Equal(string.Empty, serviceText);
        }

        [Fact]
        public void DoubleDispatchObject_CanDispatch_OverPOCOServiceDerived()
        {
            POCOServiceBase service = new POCOServiceDerived();

            POCOEntity entity = new POCOFile("file1");
            service.Handle(entity);
            entity = new POCOLink("link1");
            service.Handle(entity);
            entity = new POCOFolder("folder1");
            service.Handle(entity);
            entity = new POCOFile("file2");
            service.Handle(entity);
            entity = new POCOFolder("folder2");
            service.Handle(entity);

            var serviceText = service.ToString();
            var reader = new StringReader(serviceText);
            var lines = new List<string>();
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                lines.Add(line);
            }

            Assert.Equal(5, lines.Count);
            Assert.Equal
            (
                new[]
                {
                    "file: POCOFile(file1)",
                    "link: POCOLink(link1)",
                    "folder: POCOFolder(folder1)",
                    "file: POCOFile(file2)",
                    "folder: POCOFolder(folder2)"
                },
                lines.ToArray()
            );
        }

        [Fact]
        public void DoubleDispatchObject_CanDispatch_OverServiceV2()
        {
            var service = new ServiceV2();

            POCOEntity entity = new POCOFile("file1");
            service.Handle(entity);
            entity = new POCOLink("link1");
            service.Handle(entity);
            entity = new POCOFolder("folder1");
            service.Handle(entity);
            entity = new POCOFile("file2");
            service.Handle(entity);
            entity = new POCOFolder("folder2");
            service.Handle(entity);

            var serviceText = service.ToString();
            var reader = new StringReader(serviceText);
            var lines = new List<string>();
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                lines.Add(line);
            }

            Assert.Equal(5, lines.Count);
            Assert.Equal
            (
                new[]
                {
                    "file: POCOFile(file1)",
                    "link: POCOLink(link1)",
                    "folder: POCOFolder(folder1)",
                    "file: POCOFile(file2)",
                    "folder: POCOFolder(folder2)"
                },
                lines.ToArray()
            );
        }

        [Fact]
        public void DoubleDispatchObject_CanDispatch_ToOverloadWithBaseClassTypeOfTheArgument()
        {
            var service = new ServiceV2();

            POCOEntity entity = new POCOSymLink("shortcut1");
            service.Handle(entity);

            var serviceText = service.ToString();
            var reader = new StringReader(serviceText);
            var lines = new List<string>();
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                lines.Add(line);
            }

            Assert.Equal(1, lines.Count);
            Assert.Equal(new[] { "item: POCOSymLink(shortcut1)" }, lines.ToArray());
        }

        [Fact]
        public void DoubleDispatchObject_CanHonor_OrElseActionInServiceV2()
        {
            var service = new ServiceV2();

            POCOEntity entity = new POCOUnsupported("oops");
            service.Handle(null as POCOEntity);
            service.Handle(entity);

            var serviceText = service.ToString();
            var reader = new StringReader(serviceText);
            var lines = new List<string>();
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                lines.Add(line);
            }

            Assert.Equal(2, lines.Count);
            Assert.Equal
            (
                new[]
                {
                    "unsupported entity: <null>",
                    "unsupported entity: POCOUnsupported(oops)"
                },
                lines.ToArray()
            );
        }

        [Fact]
        public void Surrogate_CanDispatch_OverPOCOServiceBase()
        {
            var service = new POCOServiceBase();

            POCOEntity entity = new POCOFile("file1");

            // Equivalent to:
            // Action<POCOEntity> surrogate = DoubleDispatchObject.CreateSurrogate(service.Handle, default(POCOEntity));
            // surrogate.Invoke(entity);
            service.SurrogateInvoke(service.Handle, entity);

            entity = new POCOLink("link1");
            service.SurrogateInvoke(service.Handle, entity);
            entity = new POCOFolder("folder1");
            service.SurrogateInvoke(service.Handle, entity);
            entity = new POCOFile("file2");
            service.SurrogateInvoke(service.Handle, entity);
            entity = new POCOFolder("folder2");
            service.SurrogateInvoke(service.Handle, entity);

            var serviceText = service.ToString();
            var reader = new StringReader(serviceText);
            var lines = new List<string>();
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                lines.Add(line);
            }

            Assert.Equal(2, lines.Count);
            Assert.Equal
            (
                new[]
                {
                    "item: POCOFile(file1)",
                    "item: POCOFile(file2)"
                },
                lines.ToArray()
            );
        }

        public class SampleClass
        {
            // Best instance overload for int-to-(boxed) int expectation
            public object GetValue(int value) =>
                value;

            // Best instance overload for SampleClass-to-decimal expectation
            public decimal GetValue(SampleClass instance) =>
                Convert.ToDecimal(instance.NumberOfQuarters) / 4;

            // Best instance overload for double-to-decimal expectation
            public decimal GetValue(object value) =>
                Convert.ToDecimal((double)value);

            // Best static overload for int-to-(boxed) int expectation
            public static object StaticGetValue(int value) =>
                value;

            // Best static overload for SampleClass-to-decimal expectation
            public static decimal StaticGetValue(SampleClass instance) =>
                Convert.ToDecimal(instance.NumberOfQuarters) / 4;

            // Best static overload for double-to-decimal expectation
            public static decimal StaticGetValue(object value) =>
                Convert.ToDecimal((double)value);

            public int NumberOfQuarters { get; set; }
        }

        [Fact]
        public void Surrogate_CanDispatch_ToOverloadsWithBaseClassTypesOfArgumentAndHonorContraCovariance()
        {
            // Instance method surrogates
            var sample = new SampleClass { NumberOfQuarters = 6 };

            var someCount = sample.SurrogateInvoke(sample.GetValue, 123);
            Assert.Equal(123, someCount);

            var someDollarAmount = sample.SurrogateInvoke(sample.GetValue, sample);
            Assert.Equal(1.5m, someDollarAmount);

            var anotherAmount = sample.SurrogateInvoke(sample.GetValue, (object)4.5, default(decimal));
            Assert.Equal(4.5m, anotherAmount);

            // Static method surrogates
            var someCount2 = default(object).SurrogateInvoke(typeof(SampleClass), nameof(SampleClass.StaticGetValue), 123);
            Assert.Equal(123, someCount2);

            var someDollarAmount2 =
                default(decimal)
                .SurrogateInvoke
                (
                    typeof(SampleClass),
                    nameof(SampleClass.StaticGetValue),
                    new SampleClass { NumberOfQuarters = 6 }
                );
            Assert.Equal(1.5m, someDollarAmount2);

            var anotherAmount2 = default(decimal).SurrogateInvoke(typeof(SampleClass), nameof(SampleClass.StaticGetValue), 4.5);
            Assert.Equal(4.5m, anotherAmount2);
        }

        [Fact]
        public void Surrogate_CanDispatch_OverValueTypes()
        {
            Func<OpaqueValue, object, object> takeAnOpaqueValueAndOperationAndDispatch =
                (opaqueValue, someOperation) =>
                    opaqueValue.SurrogateInvoke(opaqueValue.Apply, someOperation);

            var myGuid = Guid.NewGuid();
            var myOpaqueValue = new OpaqueValue(myGuid);
            var result1 = takeAnOpaqueValueAndOperationAndDispatch(myOpaqueValue, new OpaqueValueOperation1());
            var result2 = takeAnOpaqueValueAndOperationAndDispatch(myOpaqueValue, new OpaqueValueOperation2());

            Assert.IsType(typeof(OpaqueValueResult1), result1);
            Assert.Equal(myGuid, (result1 as OpaqueValueResult).UniqueId);
            Assert.IsType(typeof(OpaqueValueResult2), result2);
            Assert.Equal(myGuid, (result2 as OpaqueValueResult).UniqueId);
        }

        [Fact]
        public void Surrogate_Requires_ReferenceTypeWithSameTarget()
        {
            var serviceV2_1 = new ServiceV2();
            var serviceV2_2 = new ServiceV2();

            Action tryBadSurrogateInvoke =
                () =>
                {
                    POCOEntity entity = new POCOFile("file1");
                    serviceV2_2.SurrogateInvoke(serviceV2_1.Handle, entity);
                };

            Exception error = null;
            try
            {
                tryBadSurrogateInvoke();
            }
            catch (Exception ex)
            {
                error = ex;
            }

            Assert.NotNull(error);
            Assert.IsType(typeof(InvalidOperationException), error);
        }
    }
}