using VectorDB.Vector;

namespace VectorTests
{
    [TestClass]
    public class ByteVectorTests
    {
        private readonly byte[] vectorData = new byte[4] { 65, 65, 0, 0 };

        [TestMethod]
        public void Constructor_Initialize_PropertiesAreInitializedProperly()
        {
            // Arrange

            // Act
            ByteVector vector = new ByteVector(vectorData);

            // Assert
            Assert.IsNotNull(vector);
            Assert.IsInstanceOfType(vector, typeof(ByteVector));
            CollectionAssert.AreEqual(vectorData, vector.Vector.ToArray());
            Assert.AreEqual(1, vector.SegmentByteLength);
            Assert.AreEqual(8, vector.SegmentBitLength);
            Assert.AreEqual(4, vector.VectorByteLength);
            Assert.AreEqual(32, vector.VectorBitLength);
        }

        [TestMethod]
        [DataRow(1, "N")]
        [DataRow(2, "NN")]
        [DataRow(3, "NNA")]
        [DataRow(4, "NNAA")]
        public void ToBucketString_ProduceByteString_ShouldEqualUTF8Representation(int stringLength, string expected)
        {
            // Arrange
            ByteVector vector = new ByteVector(vectorData);

            // Act
            var bucketString = vector.ToBucketString(stringLength);

            // Assert
            Assert.AreEqual(stringLength, bucketString.Length);
            Assert.AreEqual(expected, bucketString);
        }
    }
}