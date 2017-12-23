using System;
using System.Collections.Generic;
using System.Text;

namespace FileStoreClassLibrary.ChunkOperations
{
   public class FileChunkObject
    {
        public String FileName { get; set; }
        public int Offset { get; set; }
        public int size { get; set; }
        public Boolean ChunkStatus { get; set; }
    }
}
