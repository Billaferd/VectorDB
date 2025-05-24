using System;
using System.Linq;
using VectorDB.Vector.Interface; // For IVector

namespace VectorDB.Vectors
{
    public static class VectorMath
    {
        public static float EuclideanDistance(IVector<float> vec1, IVector<float> vec2)
        {
            if (vec1.VectorBitLength != vec2.VectorBitLength)
                throw new ArgumentException("Vectors must have the same dimensions.");

            // Assuming vec1.Vector and vec2.Vector are IReadOnlyList<float>
            float sumOfSquares = 0;
            var v1Array = vec1.Vector.ToArray(); // Materialize to array for indexing
            var v2Array = vec2.Vector.ToArray(); // Materialize to array for indexing

            for (int i = 0; i < v1Array.Length; i++)
            {
                sumOfSquares += (v1Array[i] - v2Array[i]) * (v1Array[i] - v2Array[i]);
            }
            return (float)Math.Sqrt(sumOfSquares);
        }

        public static float CosineSimilarity(IVector<float> vec1, IVector<float> vec2)
        {
            if (vec1.VectorBitLength != vec2.VectorBitLength)
                throw new ArgumentException("Vectors must have the same dimensions.");

            var v1Array = vec1.Vector.ToArray();
            var v2Array = vec2.Vector.ToArray();

            float dotProduct = 0;
            float magnitude1 = 0;
            float magnitude2 = 0;

            for (int i = 0; i < v1Array.Length; i++)
            {
                dotProduct += v1Array[i] * v2Array[i];
                magnitude1 += v1Array[i] * v1Array[i];
                magnitude2 += v2Array[i] * v2Array[i];
            }

            magnitude1 = (float)Math.Sqrt(magnitude1);
            magnitude2 = (float)Math.Sqrt(magnitude2);

            if (magnitude1 == 0 || magnitude2 == 0)
                return 0; // Or handle as an error/special case (e.g., throw exception or return NaN)

            return dotProduct / (magnitude1 * magnitude2);
        }
    }
}
