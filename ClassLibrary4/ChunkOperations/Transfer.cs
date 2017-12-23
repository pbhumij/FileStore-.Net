using System;
using System.Collections.Generic;
using System.Text;

namespace FileStoreClassLibrary.ChunkOperations
{
    class Transfer
    {
        public Boolean TransferStatus = false;
        public Boolean ResumeStatus = false;

        public void UpdateResumeStatus(List<FileChunkObject> chunkList)
        {
            foreach (FileChunkObject chunk in chunkList)
            {
                if (!chunk.ChunkStatus)
                {
                    ResumeStatus = true;
                    break;
                }
            }
        }

        public void UpdateTransferStatus(List<FileChunkObject> chunkList)
        {
            foreach (FileChunkObject chunk in chunkList)
            {
                if (!chunk.ChunkStatus)
                {
                    TransferStatus = true;
                    break;
                }
                else
                {
                    TransferStatus = false;
                }
            }
        }

    }
}
