﻿using System;
using System.Collections.Generic;
using System.Linq;
﻿using System.Threading.Tasks;
using Couchbase.Configuration.Client;
using Couchbase.Core;
using Couchbase.IntegrationTests.Utils;
using Couchbase.IO;
using Couchbase.Utils;
using NUnit.Framework;

namespace Couchbase.IntegrationTests
{
    [TestFixture]
    public class CouchbaseBucketKeyValueTests
    {
        private ICluster _cluster;
        private IBucket _bucket;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _cluster = new Cluster(Utils.TestConfiguration.GetCurrentConfiguration());
            _bucket = _cluster.OpenBucket();
        }

        [Test]
        public async Task Test_GetAsync()
        {
            var key = "thekey";
            var value = "thevalue";

            await _bucket.RemoveAsync(key);
            await _bucket.InsertAsync(key, value);
            var result = await _bucket.GetAsync<string>(key);
            Assert.AreEqual(ResponseStatus.Success, result.Status);
        }

        [Test]
        public async Task Test_UpsertAsync()
        {
            var key = "thekey";
            var value = "thevalue";

            await _bucket.RemoveAsync(key);
            var result = await _bucket.UpsertAsync(key, value);
            Assert.AreEqual(ResponseStatus.Success, result.Status);
        }

        [Test]
        public async Task Test_InsertAsync()
        {
            var key = "thekey";
            var value = "thevalue";

            await _bucket.RemoveAsync(key);
            var result = await _bucket.InsertAsync(key, value);
            Assert.AreEqual(ResponseStatus.Success, result.Status);
        }

        [Test]
        public async Task Test_RemoveAsync()
        {
            var key = "thekey";
            var value = "thevalue";

            await _bucket.RemoveAsync(key);
            var result = await _bucket.GetAsync<string>(key);
            Assert.AreEqual(ResponseStatus.KeyNotFound, result.Status);
        }

        [Test]
        public void Test_Get()
        {
            var key = "thekey";
            var value = "thevalue";

            _bucket.Remove(key);
            _bucket.Insert(key, value);
            var result = _bucket.Get<string>(key);
            Assert.AreEqual(ResponseStatus.Success, result.Status);
        }

        [Test]
        public void Get_IdIsInOperationResult()
        {
            var key = "thekey";
            var value = "thevalue";

            _bucket.Remove(key);
            _bucket.Insert(key, value);
            var result = _bucket.Get<string>(key);
            Assert.AreEqual(key, result.Id);
        }

        [Test]
        public void GetDocument_IdIsInOperationResult()
        {
            var key = "thekey";
            var value = "thevalue";

            _bucket.Remove(key);
            _bucket.Insert(key, value);
            var result = _bucket.GetDocument<string>(key);
            Assert.AreEqual(key, result.Id);
        }

        [Test]
        public async Task GetDocumentAsync_IdIsInOperationResult()
        {
            var key = "thekey";
            var value = "thevalue";

            _bucket.Remove(key);
            _bucket.Insert(key, value);
            var result = await _bucket.GetDocumentAsync<string>(key).ConfigureAwait(false);
            Assert.AreEqual(key, result.Id);
        }

        [Test]
        public void Test_Upsert()
        {
            var key = "thekey";
            var value = "thevalue";

            _bucket.Remove(key);
            var result = _bucket.Upsert(key, value);
            Assert.AreEqual(ResponseStatus.Success, result.Status);
        }

        [Test]
        public void Test_Upsert_GetsMutationTokenWithBucketRef()
        {
            // https://issues.couchbase.com/browse/NCBC-1119

            var key = "thekey";
            var value = "thevalue";

            _bucket.Remove(key);
            var result = _bucket.Upsert(key, value);

            Assert.AreEqual(ResponseStatus.Success, result.Status);
            Assert.IsNotNull(result.Token);
            Assert.IsTrue(result.Token.IsSet);
            Assert.AreEqual(_bucket.Name, result.Token.BucketRef);
        }

        [Test]
        public void Test_Insert()
        {
            var key = "thekey";
            var value = "thevalue";

            _bucket.Remove(key);
            var result = _bucket.Insert(key, value);
            Assert.AreEqual(ResponseStatus.Success, result.Status);
        }

        [Test]
        public void Test_Remove()
        {
            var key = "thekey";
            var value = "thevalue";

            _bucket.Remove(key);
            var result = _bucket.Get<string>(key);
            Assert.AreEqual(ResponseStatus.KeyNotFound, result.Status);
        }

        [Test]
        public void GetWithLock_WhenKeyIsLocked_ResponseStatusIsTemporaryFailure_And_MessageIsLOCK_ERROR()
        {
            _bucket.Upsert("roikatz", "bbbb");
            var res1 = _bucket.GetAndLock<string>("roikatz", 30);
            Assert.IsTrue(res1.Success);

            var res2 = _bucket.GetAndLock<string>("roikatz", 30);
            Assert.IsFalse(res2.Success);
            Assert.AreEqual(ResponseStatus.TemporaryFailure, res2.Status);
        }

        #region Batch Async Operations

        [Test]
        public async Task UpsertAsync_Batch()
        {
            var documents = new List<IDocument<object>>
            {
                new Document<object>
                {
                    Id = "UpsertAsync_Batch_doc1",
                    Content = new {Name = "bob", Species = "Cat", Age = 5}
                },
                new Document<object> {Id = "UpsertAsync_Batch_doc2", Content = 10},
                new Document<object> {Id = "UpsertAsync_Batch_doc3", Content = new Cat {Name = "Cleo", Age = 10}}
            };
            var results = await _bucket.UpsertAsync(documents).ConfigureAwait(false);
            Assert.AreEqual(3, results.Length);
            var trueForAll = results.ToList().TrueForAll(x => x.Success);
            Assert.IsTrue(trueForAll);
        }

        [Test]
        public async Task ReplaceAsync_Batch()
        {
            var documents = new List<IDocument<object>>
            {
                new Document<object>
                {
                    Id = "ReplaceAsync_Batch_doc1",
                    Content = new {Name = "bob", Species = "Cat", Age = 5}
                },
                new Document<object> {Id = "ReplaceAsync_Batch_doc2", Content = 10},
                new Document<object> {Id = "ReplaceAsync_Batch_doc3", Content = new Cat {Name = "Cleo", Age = 10}}
            };
            var results = await _bucket.UpsertAsync(documents).ConfigureAwait(false);
            var resultsReplaced = await _bucket.ReplaceAsync(documents).ConfigureAwait(false);
            Assert.AreEqual(3, results.Length);
            var trueForAll = resultsReplaced.ToList().TrueForAll(x => x.Success);
            Assert.IsTrue(trueForAll);
        }

        [Test]
        public async Task RemoveAsync_Batch()
        {
            var documents = new List<IDocument<object>>
            {
                new Document<object>
                {
                    Id = "RemoveAsync_Batch_doc1",
                    Content = new {Name = "bob", Species = "Cat", Age = 5}
                },
                new Document<object> {Id = "RemoveAsync_Batch_doc2", Content = 10},
                new Document<object> {Id = "RemoveAsync_Batch_doc3", Content = new Cat {Name = "Cleo", Age = 10}}
            };
            var results = await _bucket.UpsertAsync(documents).ConfigureAwait(false);
            var resultsRemoved = await _bucket.RemoveAsync(documents).ConfigureAwait(false);
            Assert.AreEqual(3, results.Length);
            var trueForAll = resultsRemoved.ToList().TrueForAll(x => x.Success);
            Assert.IsTrue(trueForAll);
        }

        [Test]
        public async Task InsertAsync_Batch()
        {
            var documents = new List<IDocument<object>>
            {
                new Document<object>
                {
                    Id = "InsertAsync_Batch_doc1",
                    Content = new {Name = "bob", Species = "Cat", Age = 5}
                },
                new Document<object> {Id = "InsertAsync_Batch_doc2", Content = 10},
                new Document<object> {Id = "InsertAsync_Batch_doc3", Content = new Cat {Name = "Cleo", Age = 10}}
            };
            var results = await _bucket.RemoveAsync(documents).ConfigureAwait(false);
            var resultsInsert = await _bucket.InsertAsync(documents).ConfigureAwait(false);
            Assert.AreEqual(3, results.Length);
            var trueForAll = resultsInsert.ToList().TrueForAll(x => x.Success);
            Assert.IsTrue(trueForAll);
        }


        [Test]
        public async Task GetAsync_Batch()
        {
            var documents = new List<IDocument<object>>
            {
                new Document<object>
                {
                    Id = "GetAsync_Batch_doc1",
                    Content = new {Name = "bob", Species = "Cat", Age = 5}
                },
                new Document<object> {Id = "GetAsync_Batch_doc2", Content = 10},
                new Document<object> {Id = "GetAsync_Batch_doc3", Content = new Cat {Name = "Cleo", Age = 10}}
            };
            await _bucket.UpsertAsync(documents).ConfigureAwait(false);
            var resultsGet = await _bucket.GetDocumentsAsync<object>(documents.Select(x=>x.Id)).ConfigureAwait(false);
            Assert.AreEqual(3, resultsGet.Length);
            var trueForAll = resultsGet.ToList().TrueForAll(x => x.Success);
            Assert.IsTrue(trueForAll);
        }

        public class Cat
        {
            public string Name { get; set; }
            public int Age { get; set; }
        }

        #endregion

        [Test]
        [Description("This specifically tests for deadlocks in MultiplexingConnection and UseEnhancedDurability is true - seqno based observe.")]
        public async Task Insert_WithObserve_DocumentMutationException_IsNotThrown()
        {
            using (var cluster = new Cluster(TestConfiguration.GetConfiguration("multiplexio")))
            {
                using (var bucket = cluster.OpenBucket("default"))
                {
                    var inserts = new List<Task<IOperationResult<string>>>();
                    var deletes = new List<string>();
                    for (var i = 0; i < 10; i++)
                    {
                        deletes.Add("key" + i);
                        inserts.Add(bucket.InsertAsync("key" + i, "{\"data\":" + i + "}", ReplicateTo.Zero,
                            PersistTo.One));
                    }
                    bucket.Remove(deletes);
                    var results = await Task.WhenAll(inserts).ConfigureAwait(false);
                    Assert.IsTrue(results.ToList().TrueForAll(x => x.Status == ResponseStatus.Success));
                }
            }
        }

        [Test]
        [Description("This specifically tests a configuration where UseEnhancedDurability is false - CAS based observe.")]
        public async Task Insert_WithObserve_DocumentMutationDetected_IsFound()
        {
            using (var cluster = new Cluster(TestConfiguration.GetConfiguration("observeConfig")))
            {
                using (var bucket = cluster.OpenBucket("default"))
                {
                    var insertsAndUpdates = new List<Task<IOperationResult<string>>>();
                    var deletes = new List<string>();
                    for (var i = 0; i < 10; i++)
                    {
                        deletes.Add("key" + i);
                        insertsAndUpdates.Add(bucket.InsertAsync("key" + i, "{\"data\":" + i + "}", ReplicateTo.Zero,
                            PersistTo.One));
                        insertsAndUpdates.Add(bucket.UpsertAsync("key" + i, "{\"updatad\":" + i + "}"));
                    }
                    bucket.Remove(deletes);
                    var results = await Task.WhenAll(insertsAndUpdates).ConfigureAwait(false);
                    Assert.IsTrue(results.ToList().Exists(x => x.Status == ResponseStatus.DocumentMutationDetected));
                }
            }
        }

        [Test]
        public async Task InsertAsync_ReturnsDocument()
        {
            var key = "InsertAsync_ReturnsDocument";
            var doc = new Document<dynamic>
            {
                Id = key,
                Content = new {Name = "foo"}
            };

            await _bucket.RemoveAsync(key);
            var result = await _bucket.InsertAsync(doc);

            Assert.AreEqual(doc.Content, result.Content);
        }

        [Test]
        public async Task InsertAsync_ReturnsId()
        {
            var key = "InsertAsync_ReturnsDocument";
            var doc = new Document<dynamic>
            {
                Id = key,
                Content = new {Name = "foo"}
            };

            await _bucket.RemoveAsync(key);
            var result = await _bucket.InsertAsync(doc);

            Assert.AreEqual(doc.Id, result.Id);
        }

        public void Replace_DocumentDoesNotExistException()
        {
            //setup

            var key = "Replace_DocumentDoesNotExistException";
            _bucket.Remove(new Document<dynamic> {Id = key});

            //act
            var result = _bucket.Replace(new Document<dynamic> {Id = key, Content = new {name="foo"}});

            //assert
            Assert.AreEqual(result.Exception.GetType(), typeof(DocumentDoesNotExistException));
        }

        [Test]
        public async Task ReplaceAsync_DocumentDoesNotExistException()
        {
            //setup
            var key = "ReplaceAsync_DocumentDoesNotExistException";
            _bucket.Remove(new Document<dynamic> { Id = key });

            //act
            var result = await _bucket.ReplaceAsync(new Document<dynamic> { Id = key, Content = new { name = "foo" } }).ContinueOnAnyContext();

            //assert
            Assert.AreEqual(result.Exception.GetType(), typeof(DocumentDoesNotExistException));
        }

        [Test]
        public void Insert_DocumentAlreadyExistsException()
        {
            //setup
            var key = "Insert_DocumentAlreadyExistsException";
            _bucket.Remove(new Document<dynamic> { Id = key });
            _bucket.Insert(new Document<dynamic> { Id = key, Content = new { name = "foo" } });

            //act
            var result = _bucket.Insert(new Document<dynamic> { Id = key, Content = new { name = "foo" } });

            //assert
            Assert.AreEqual(result.Exception.GetType(), typeof(DocumentAlreadyExistsException));
        }

        [Test]
        public async Task InsertAsync_DocumentAlreadyExistsException()
        {
            //setup
            var key = "Insert_DocumentAlreadyExistsException";
            _bucket.Remove(new Document<dynamic> { Id = key });
            _bucket.Insert(new Document<dynamic> { Id = key, Content = new { name = "foo" } });

            //act
            var result = await _bucket.InsertAsync(new Document<dynamic> { Id = key, Content = new { name = "foo" } }).ContinueOnAnyContext();

            //assert
            Assert.AreEqual(result.Exception.GetType(), typeof(DocumentAlreadyExistsException));
        }

        [Test]
        public void GetAndLock_TemporaryLockFailureException()
        {
            //setup
            var key = "GetAndLock_TemporaryLockFailureException";
            _bucket.Remove(new Document<dynamic> { Id = key });
            _bucket.Insert(new Document<dynamic> { Id = key, Content = new { name = "foo" } });
            _bucket.GetAndLock<dynamic>(key, new TimeSpan(0, 0, 0, 5));

            //act
            var result = _bucket.GetAndLock<dynamic>(key, new TimeSpan(0, 0, 0, 5));

            //assert
            Assert.AreEqual(result.Exception.GetType(), typeof(TemporaryLockFailureException));
        }

        [Test]
        public void Replace_WithCasAndMutated_CasMismatchException()
        {
            //setup
            var key = "ReplaceWithCas_CasMismatchException";
            _bucket.Remove(new Document<dynamic> { Id = key });

            var docWithCas = _bucket.Insert(new Document<dynamic> { Id = key, Content = new { name = "foo" } });
            _bucket.Upsert(new Document<dynamic> {Id = key, Content = new {name = "foochanged!"}});

            //act
            var result = _bucket.Replace(new Document<dynamic> { Id = key,
                Content = new { name = "foobarr" }, Cas = docWithCas.Document.Cas});

            //assert
            Assert.AreEqual(result.Exception.GetType(), typeof(CasMismatchException));
        }

        [Test]
        public async Task ReplaceAsync_WithCasAndMutated_CasMismatchException()
        {
            //setup
            var key = "ReplaceWithCas_CasMismatchException";
            _bucket.Remove(new Document<dynamic> { Id = key });

            var docWithCas = _bucket.Insert(new Document<dynamic> { Id = key, Content = new { name = "foo" } });
            _bucket.Upsert(new Document<dynamic> { Id = key, Content = new { name = "foochanged!" } });

            //act
            var result = await _bucket.ReplaceAsync(new Document<dynamic> {
                Id = key, Content = new { name = "foobarr" },
                Cas = docWithCas.Document.Cas }).ContinueOnAnyContext();

            //assert
            Assert.AreEqual(result.Exception.GetType(), typeof(CasMismatchException));
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            _cluster.CloseBucket(_bucket);
            _cluster.Dispose();
        }
    }
}

#region [ License information          ]

/* ************************************************************
 *
 *    @author Couchbase <info@couchbase.com>
 *    @copyright 2015 Couchbase, Inc.
 *
 *    Licensed under the Apache License, Version 2.0 (the "License");
 *    you may not use this file except in compliance with the License.
 *    You may obtain a copy of the License at
 *
 *        http://www.apache.org/licenses/LICENSE-2.0
 *
 *    Unless required by applicable law or agreed to in writing, software
 *    distributed under the License is distributed on an "AS IS" BASIS,
 *    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *    See the License for the specific language governing permissions and
 *    limitations under the License.
 *
 * ************************************************************/

#endregion