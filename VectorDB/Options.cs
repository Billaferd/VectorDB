using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VectorDB
{
    public class Options
    {
        /// <summary>
        /// Specifies the path to be used for the data files.
        /// </summary>
        public string DataFilePath { get; internal set; } = string.Empty;

        /// <summary>
        /// Specifies the path to be used for the index files.
        /// </summary>
        public string IndexFilePath { get; internal set; } = string.Empty;

        /// <summary>
        /// The number of vectors allowed to be added to the file.
        /// </summary>
        public int BucketSize { get; internal set; } = 256;

        /// <summary>
        /// The number of starting bytes to be used for the bucket names.
        /// </summary>
        /// <remarks>
        /// The higher this number is the more files that will be created.
        /// </remarks>
        public int BucketByteSplit { get; internal set; } = 2;

        /// <summary>
        /// The size of the vectors in bits
        /// </summary>
        /// <remarks>
        /// The vectors should all be the same size.
        /// </remarks>
        public int VectorBitSize { get; private set; } = 256;

        /// <summary>
        /// The size of the vectors in bytes
        /// </summary>
        /// <remarks>
        /// The vectors should all be the same size.
        /// </remarks>
        public int VectorByteSize { get; internal set; } = 256 / 8;
    }
}
