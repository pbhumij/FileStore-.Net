using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FileStoreClassLibrary.ChunkOperations
{
    public class FileChunkListContainer
    {
        public Boolean status { get; set; }
        public String CloudFileName { get; set; }
        public int FileSize { get; set; }
        public List<FileChunkObject> chunkList { get; set; }

    }
}
