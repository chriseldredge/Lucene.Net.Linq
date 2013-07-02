using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Store;
using NUnit.Framework;
using Version = Lucene.Net.Util.Version;

namespace Lucene.Net.Linq.Tests.OriginalObjectLookup
{
    public class OriginalObjectLookupTests
    {
        private LuceneDataProvider _provider;
        private RAMDirectory _directory;
        private IDictionary<string, SomeObject> _objectRepository = new Dictionary<string, SomeObject>();
        private LookupDocumentMapper<SomeObject> _documentMapper;

        [SetUp]
        public void Setup()
        {
            _directory = new RAMDirectory();
            _provider = new LuceneDataProvider(_directory, Version.LUCENE_30);

            Func<string, SomeObject> findObjectByKey = s => _objectRepository[s]; //could be a db call, or something else 
            Func<SomeObject, string> findKeyFromObject = o => o.Key; //could be found using reflection or otherwise
            
            _documentMapper = new LookupDocumentMapper<SomeObject>(findObjectByKey, findKeyFromObject, Version.LUCENE_30);
        }

        [Test]
        public void CanFindAnOriginalObject()
        {
            var someObject = new SomeObject() { Name = "SomeName", Key = Guid.NewGuid().ToString() };
            _objectRepository.Add(someObject.Key, someObject);
            using (var session = _provider.OpenSession<SomeObject>(_documentMapper.Create, _documentMapper))
            {
                session.Add(someObject);
                session.Commit();
            }

            using (var session = _provider.OpenSession<SomeObject>(_documentMapper.Create, _documentMapper))
            {
                var located = session.Query().Single(st => st.Name == "SomeName");
                Assert.True(ReferenceEquals(someObject, located));
            }
        }

        [Test]
        public void ChangesToOriginalObjectAreTracked()
        {
            var someObject = new SomeObject() { Name = "SomeName", Key = Guid.NewGuid().ToString() };
            _objectRepository.Add(someObject.Key, someObject);
            using (var session = _provider.OpenSession<SomeObject>(_documentMapper.Create, _documentMapper))
            {
                session.Add(someObject);
                session.Commit();
            }

            using (var session = _provider.OpenSession<SomeObject>(_documentMapper.Create, _documentMapper))
            {
                var located = session.Query().Single(st => st.Name == "SomeName");
                located.Name = "SomeOtherName";
                session.Commit();
            }


            using (var session = _provider.OpenSession<SomeObject>(_documentMapper.Create, _documentMapper))
            {
                var located = session.Query().Single(st => st.Name == "SomeOtherName");
                Assert.True(ReferenceEquals(someObject, located));
            }
        }

        [Test]
        public void ChangesToObjectAreTrackedWhenNotUsingLookup()
        {
            var someObject = new SomeObject() { Name = "SomeName", Key = Guid.NewGuid().ToString() };
            using (var session = _provider.OpenSession<SomeObject>())
            {
                session.Add(someObject);
                session.Commit();
            }

            using (var session = _provider.OpenSession<SomeObject>())
            {
                var located = session.Query().Single(st => st.Name == "SomeName");
                located.Name = "SomeOtherName";
                session.Commit();
            }


            using (var session = _provider.OpenSession<SomeObject>())
            {
                var located = session.Query().Single(st => st.Name == "SomeOtherName");
            }
        }


    }
}
