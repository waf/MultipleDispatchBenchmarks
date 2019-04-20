using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;

namespace DispatchBenchmark.Tests
{
    using YSharp.Design.DoubleDispatch;

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
                (dispatch = dispatch ?? new DoubleDispatchObject(this))
                .Via(Handle, entity,
                    () =>
                    {
                        accumulator.AppendLine($"unsupported entity: {(entity != null ? entity.ToString() : "<null>")}");
                    }
                );

            public override void Handle(POCOFile file)
            {
                accumulator.AppendLine($"item: {file}");
            }

            public void Handle(POCOLink link)
            {
                accumulator.AppendLine($"item: {link}");
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
                Via(Handle, entity,
                    () =>
                    {
                        accumulator.AppendLine($"unsupported entity: {(entity != null ? entity.ToString() : "<null>")}");
                    }
                );

            public void Handle(POCOFile file)
            {
                accumulator.AppendLine($"item: {file}");
            }

            public void Handle(POCOLink link)
            {
                accumulator.AppendLine($"item: {link}");
            }

            public void Handle(POCOFolder folder)
            {
                accumulator.AppendLine($"folder: {folder}");
            }
        }

        [Fact]
        public void POCOServiceBaseOnlyImplementsNoops()
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
        public void DoubleDispatchObject_CanCorrectlyDispatchInPOCOServiceDerived()
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
                new string[]
                {
                    "item: POCOFile(file1)",
                    "item: POCOLink(link1)",
                    "folder: POCOFolder(folder1)",
                    "item: POCOFile(file2)",
                    "folder: POCOFolder(folder2)"
                },
                lines.ToArray()
            );
        }

        [Fact]
        public void DoubleDispatchObject_CanCorrectlyDispatchInServiceV2()
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
                new string[]
                {
                    "item: POCOFile(file1)",
                    "item: POCOLink(link1)",
                    "folder: POCOFolder(folder1)",
                    "item: POCOFile(file2)",
                    "folder: POCOFolder(folder2)"
                },
                lines.ToArray()
            );
        }

        [Fact]
        public void DoubleDispatchObject_SurrogateCanCorrectlyDispatchOverPOCOServiceBase()
        {
            var service = new POCOServiceBase();
            var/*(Action<POCOEntity>)*/ surrogate = DoubleDispatchObject.CreateSurrogate(service.Handle, default(POCOEntity));

            POCOEntity entity = new POCOFile("file1");
            surrogate.Invoke(entity);
            entity = new POCOLink("link1");
            surrogate.Invoke(entity);
            entity = new POCOFolder("folder1");
            surrogate.Invoke(entity);
            entity = new POCOFile("file2");
            surrogate.Invoke(entity);
            entity = new POCOFolder("folder2");
            surrogate.Invoke(entity);

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
                new string[]
                {
                    "item: POCOFile(file1)",
                    "item: POCOFile(file2)"
                },
                lines.ToArray()
            );
        }
    }
}