using Amazon.S3;
using Amazon.S3.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Text;
using System.Threading.Tasks;

namespace FileStoreClassLibrary.ChunkOperations
{
    class ChunkOperation
    {
        private readonly AmazonS3Client client;
        private readonly FileChunkObject chunk;
        private readonly Stream uploadStream;
        
        public ChunkOperation(AmazonS3Client client, FileChunkObject chunk, Stream uploadStream)
        {
            this.client = client;
            this.chunk = chunk;
            this.uploadStream = uploadStream;
        }

        public ChunkOperation(AmazonS3Client client, FileChunkObject chunk, MemoryMappedViewAccessor accessor)
        {
            this.client = client;
            this.chunk = chunk;
        }

        public async Task<Boolean> Upload()
        {
            Boolean uploadStatus = false;
            PutObjectResponse response;
            PutObjectRequest request = new PutObjectRequest
                {
                    BucketName = "ziroh",
                    Key = chunk.FileName,
                    InputStream = uploadStream
                };
            Task<PutObjectResponse> objectResponse = client.PutObjectAsync(request);
            response = await objectResponse;
            if (response.HttpStatusCode.ToString() == "OK")
                {
                    uploadStatus = true;
                }
            uploadStream.Flush();
            uploadStream.Dispose();
            return uploadStatus;
        }

        public async Task<MemoryStream> Download()
        {
            GetObjectResponse response;
            GetObjectRequest request = new GetObjectRequest
            {
                 BucketName = "ziroh",
                 Key = chunk.FileName
            };
            Task<GetObjectResponse> getResponse = client.GetObjectAsync(request);
            MemoryStream memStream = new MemoryStream(chunk.size);
            response = await getResponse;
            if (response.HttpStatusCode.ToString() == "OK")
            {
                try
                {
                    response.ResponseStream.CopyTo(memStream);
                }
                catch (AmazonS3Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }
            return memStream;
        }
    }
}
