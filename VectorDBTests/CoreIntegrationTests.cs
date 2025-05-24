using Microsoft.VisualStudio.TestTools.UnitTesting;
using VectorDB; // For VectorDBEngine, Options
using VectorDB.Vector.Interface;
using VectorDB.Vectors; // For ImmutableFloatVector, DistanceMetric
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace VectorDBTests
{
    [TestClass]
    public class CoreIntegrationTests
    {
        private string _testDbDataPath;
        private string _testDbName = "testdb.vdb";

        [TestInitialize]
        public void TestInitialize()
        {
            // Ensure a clean state for each test by deleting existing DB files
            _testDbDataPath = Path.Combine(Path.GetTempPath(), _testDbName);
            if (File.Exists(_testDbDataPath))
            {
                File.Delete(_testDbDataPath);
            }
            Console.WriteLine($"Using test database file: {_testDbDataPath}");
        }

        [TestCleanup]
        public void TestCleanup()
        {
            // Clean up database files after tests
            if (File.Exists(_testDbDataPath))
            {
                // File.Delete(_testDbDataPath);
                // For debugging, you might want to comment out the delete
                Console.WriteLine($"Test database file cleanup: {_testDbDataPath} (manual delete if needed for inspection).");
            }
        }

        private Options GetTestDbOptions()
        {
            return new Options
            {
                DataFilePath = _testDbDataPath,
                VectorBitSize = 2 * 32, // 2 floats, 64 bits total
                // Other options can be default or set if needed for specific tests
            };
        }

        [TestMethod]
        public void Test_01_CreateAndOpenDatabase()
        {
            var options = GetTestDbOptions();
            using (var db = new VectorDBEngine(options))
            {
                db.CreateOrOpen();
                // Check if data file was created
                Assert.IsTrue(File.Exists(options.DataFilePath), "Data file should be created.");
                Console.WriteLine("Test_01: Database file created.");
            } // Dispose will be called here

            // Try opening existing
            using (var db = new VectorDBEngine(options))
            {
                db.CreateOrOpen();
                Assert.IsTrue(File.Exists(options.DataFilePath), "Data file should still exist for opening.");
                Console.WriteLine("Test_01: Existing database opened.");
            }
        }

        [TestMethod]
        public void Test_02_AddSingleVectorAndSearch()
        {
            var options = GetTestDbOptions();
            var vec1Data = new float[] { 1.0f, 2.0f };
            IVector<float> vec1 = new ImmutableFloatVector(vec1Data);

            using (var db = new VectorDBEngine(options))
            {
                db.CreateOrOpen();
                db.AddVector(vec1, "vec1");
                Console.WriteLine("Test_02: Vector added.");

                // Search for the added vector
                var results = db.FindSimilarVectors(vec1, 1, DistanceMetric.EuclideanDistance);
                Assert.AreEqual(1, results.Count, "Should find 1 vector.");
                Assert.AreEqual(0, results[0].Score, 0.001, "Distance to itself should be ~0.");
                Console.WriteLine($"Test_02: Found vector with score {results[0].Score}. ID: {results[0].Id}");
            }
        }

        [TestMethod]
        public void Test_03_Persistence_AddCloseOpenSearch()
        {
            var options = GetTestDbOptions();
            var vec1Data = new float[] { 3.0f, 4.0f };
            var vec2Data = new float[] { 5.0f, 6.0f };
            IVector<float> vec1 = new ImmutableFloatVector(vec1Data);
            IVector<float> vec2 = new ImmutableFloatVector(vec2Data);

            using (var db = new VectorDBEngine(options))
            {
                db.CreateOrOpen();
                db.AddVector(vec1, "p_vec1");
                db.AddVector(vec2, "p_vec2");
                Console.WriteLine("Test_03: Vectors added.");
            } // DB is closed and disposed, data should be persisted

            Console.WriteLine("Test_03: Reopening database...");
            using (var db = new VectorDBEngine(options))
            {
                db.CreateOrOpen();
                
                // Search for vec1
                var results1 = db.FindSimilarVectors(vec1, 1, DistanceMetric.EuclideanDistance);
                Assert.AreEqual(1, results1.Count, "Should find vec1 after reopen.");
                Assert.AreEqual(0, results1[0].Score, 0.001, "Distance to vec1 should be ~0 after reopen.");
                Console.WriteLine($"Test_03: Found vec1 after reopen. Score: {results1[0].Score}, ID: {results1[0].Id}");

                // Search for vec2
                var results2 = db.FindSimilarVectors(vec2, 1, DistanceMetric.CosineSimilarity);
                Assert.AreEqual(1, results2.Count, "Should find vec2 after reopen.");
                Assert.AreEqual(1.0f, results2[0].Score, 0.001, "Cosine similarity to vec2 should be ~1 after reopen.");
                 Console.WriteLine($"Test_03: Found vec2 after reopen. Score: {results2[0].Score}, ID: {results2[0].Id}");
            }
        }

        [TestMethod]
        public void Test_04_AddMultipleVectors_SearchTopK()
        {
            var options = GetTestDbOptions();
            // Must match options.VectorBitSize = 2 * 32
            var vectors = new List<Tuple<string, IVector<float>>>
            {
                Tuple.Create("vA", (IVector<float>)new ImmutableFloatVector(new float[] { 1f, 1f })),
                Tuple.Create("vB", (IVector<float>)new ImmutableFloatVector(new float[] { 1f, 2f })),
                Tuple.Create("vC", (IVector<float>)new ImmutableFloatVector(new float[] { 10f, 10f })),
                Tuple.Create("vD", (IVector<float>)new ImmutableFloatVector(new float[] { 10f, 11f })),
                Tuple.Create("vE", (IVector<float>)new ImmutableFloatVector(new float[] { 2f, 1f }))
            };

            using (var db = new VectorDBEngine(options))
            {
                db.CreateOrOpen();
                foreach (var entry in vectors)
                {
                    db.AddVector(entry.Item2, entry.Item1);
                }
                Console.WriteLine($"Test_04: Added {vectors.Count} vectors.");
            } // Close and save

            using (var db = new VectorDBEngine(options))
            {
                db.CreateOrOpen(); // Reopen

                // Query vector close to vA and vB
                IVector<float> queryVec = new ImmutableFloatVector(new float[] { 1f, 1.6f });
                
                // Test with Euclidean Distance
                var euclideanResults = db.FindSimilarVectors(queryVec, 2, DistanceMetric.EuclideanDistance);
                Assert.AreEqual(2, euclideanResults.Count, "Should get 2 results for Euclidean.");
                // Expected order: vB (1,2) is closer than vA (1,1) or vE (2,1) to (1,1.6)
                // Dist(query, vA) = sqrt((1-1)^2 + (1.6-1)^2) = sqrt(0 + 0.36) = 0.6
                // Dist(query, vB) = sqrt((1-1)^2 + (1.6-2)^2) = sqrt(0 + 0.16) = 0.4
                // Dist(query, vE) = sqrt((1-2)^2 + (1.6-1)^2) = sqrt(1 + 0.36) = sqrt(1.36) approx 1.16
                Console.WriteLine($"Test_04: Euclidean results - 1st ID: {euclideanResults[0].Id}, Score: {euclideanResults[0].Score}");
                Console.WriteLine($"Test_04: Euclidean results - 2nd ID: {euclideanResults[1].Id}, Score: {euclideanResults[1].Score}");
                // vB (item1) is expected to be first with score ~0.4
                // vA (item0) is expected to be second with score ~0.6
                Assert.AreEqual(0.4f, euclideanResults[0].Score, 0.001f, "vB (item1) should be closest (Euclidean score ~0.4).");
                Assert.AreEqual(0.6f, euclideanResults[1].Score, 0.001f, "vA (item0) should be second closest (Euclidean score ~0.6).");

                // Test with Cosine Similarity
                // Cosine similarity is higher for vectors pointing in the same direction.
                // Query (1, 1.6)
                // vA (1,1) -> dot=2.6, magQ=sqrt(1+2.56)=1.88, magA=sqrt(2)=1.41. Sim = 2.6/(1.88*1.41) ~ 0.98
                // vB (1,2) -> dot=4.2, magQ=1.88, magB=sqrt(5)=2.23. Sim = 4.2/(1.88*2.23) ~ 1.0 (approx)
                var cosineResults = db.FindSimilarVectors(queryVec, 2, DistanceMetric.CosineSimilarity);
                Assert.AreEqual(2, cosineResults.Count, "Should get 2 results for Cosine.");
                Console.WriteLine($"Test_04: Cosine results - 1st ID: {cosineResults[0].Id}, Score: {cosineResults[0].Score}"); // Expect item1 (vB)
                Console.WriteLine($"Test_04: Cosine results - 2nd ID: {cosineResults[1].Id}, Score: {cosineResults[1].Score}"); // Expect item3 (vD)
                // vB (item1) score is ~0.99549
                // vD (item3) score is ~0.98398
                Assert.AreEqual(0.99549f, cosineResults[0].Score, 0.001f, "vB (item1) should be most similar (Cosine score ~0.995).");
                Assert.AreEqual(0.98398f, cosineResults[1].Score, 0.001f, "vD (item3) should be second most similar (Cosine score ~0.984).");
            }
        }
         // TODO: Add tests for edge cases:
         // - Empty database search
         // - k > number of items in DB
         // - Adding a vector that doesn't match options.VectorBitSize (should throw)
         // - Searching with a query vector that doesn't match options.VectorBitSize (should throw)
    }
}
