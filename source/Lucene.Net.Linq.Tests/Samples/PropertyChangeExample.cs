using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Lucene.Net.Documents;
using Lucene.Net.Linq;
using Lucene.Net.Linq.Fluent;
using Lucene.Net.Linq.Mapping;
using Lucene.Net.Linq.Tests.Integration;
using Lucene.Net.Store;
using Lucene.Net.Util;
using NUnit.Framework;

namespace Sample
{
    public class PropertyChangeExample
    {
        /// <summary>
        /// Note: to work propery the PropertyChanged event is only fired when
        /// a property changes from a non-null value. This way the event will not
        /// fire when the entity is first being instantiated.
        /// </summary>
        public class ExampleEntity : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;

            private string _id;
            private string _name;

            public string Id
            {
                get { return _id; }
                set
                {
                    var prev = _id;
                    _id = value;
                    if (prev != null && !Equals(value, prev))
                    {
                        OnPropertyChanged("Id");
                    }
                }
            }

            public string Name
            {
                get { return _name; }
                set
                {
                    var prev = _name;
                    _name = value;
                    if (prev != null && !Equals(value, prev))
                    {
                        OnPropertyChanged("Name");
                    }
                }
            }

            protected virtual void OnPropertyChanged(string propertyName)
            {
                var handler = PropertyChanged;
                if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public class PropertyChangedModificationDetector<T> : IDocumentModificationDetector<T> where T : INotifyPropertyChanged, new()
        {
            private readonly ISet<T> dirtyItems = new HashSet<T>();

            public bool IsModified(T item, Document document)
            {
                return dirtyItems.Contains(item);
            }

            public T Factory()
            {
                var entity = new T();
                entity.PropertyChanged += MarkDirty;
                return entity;
            }

            public IEnumerable<T> DirtyItems
            {
                get { return dirtyItems; }
            }

            private void MarkDirty(object sender, PropertyChangedEventArgs e)
            {
                dirtyItems.Add((T)sender);
            }
        }

        [TestFixture]
        public class Tests : IntegrationTestBase
        {
            private PropertyChangedModificationDetector<ExampleEntity> modificationDetector;
            private ClassMap<ExampleEntity> map;

            [SetUp]
            public override void InitializeLucene()
            {
                directory = new RAMDirectory();
                provider = new LuceneDataProvider(directory, version);

                modificationDetector = new PropertyChangedModificationDetector<ExampleEntity>();
                map = new ClassMap<ExampleEntity>(Version.LUCENE_30);
                map.Key(e => e.Id);
                map.Property(e => e.Name);

                AddDocument(new ExampleEntity { Id = "entity 1", Name = "default" });
            }

            [Test]
            public void FlushModifiedDocument()
            {
                var session = provider.OpenSession(modificationDetector.Factory, map.ToDocumentMapper(),
                                                   modificationDetector);

                using (session)
                {
                    session.Query().Single().Name = "updated";
                }

                Assert.That(provider.AsQueryable<ExampleEntity>().Single().Name, Is.EqualTo("updated"));
            }

            [Test]
            public void DontFlushUnmodifiedDocument()
            {
                var session = provider.OpenSession(modificationDetector.Factory, map.ToDocumentMapper(), modificationDetector);

                using (session)
                {
                    Assert.That(session.Query().First().Name, Is.EqualTo("default"));
                }

                Assert.That(modificationDetector.DirtyItems, Is.Empty);
            }
        }
    }
}