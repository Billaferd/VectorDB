using VectorDB.Vector;

namespace VectorTests
{
    [TestClass]
    public class FloatVectorTests
    {
        private readonly float[] vectorData = new float[4] { 65, 65, 0, 0 };

        [TestMethod]
        public void Constructor_Initialize_PropertiesAreInitializedProperly()
        {
            // Arrange

            // Act
            ImmutableFloatVector vector = new ImmutableFloatVector(vectorData);

            // Assert
            Assert.IsNotNull(vector);
            Assert.IsInstanceOfType(vector, typeof(ImmutableFloatVector));
            CollectionAssert.AreEqual(vectorData, vector.Vector.ToArray());
            Assert.AreEqual(4, vector.SegmentByteLength);
            Assert.AreEqual(32, vector.SegmentBitLength);
            Assert.AreEqual(16, vector.VectorByteLength);
            Assert.AreEqual(128, vector.VectorBitLength);
        }

        [TestMethod]
        [DataRow(1, "A")]
        [DataRow(2, "AA")]
        [DataRow(3, "AAA")]
        [DataRow(4, "AAAO")]
        public void ToBucketString_ProduceByteString_ShouldEqualUTF8Representation(int stringLength, string expected)
        {
            // Arrange
            ImmutableFloatVector vector = new ImmutableFloatVector(vectorData);

            // Act
            var bucketString = vector.GetBucketHash(stringLength);

            // Assert
            Assert.AreEqual(stringLength, bucketString.Length);
            Assert.AreEqual(expected, bucketString);
        }
    }
}