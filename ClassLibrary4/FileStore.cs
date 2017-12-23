using Amazon.S3;
using Amazon.S3.Model;
using System;
using System.Threading.Tasks;
using FileStoreClassLibrary.ChunkOperations;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Text;

namespace FileStoreClassLibrary
{
    public class FileStore
    {
        private readonly AmazonS3Client client;
        private readonly String secretKey = "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY";
        private readonly String accessKey = "AKIAIOSFODNN7EXAMPLE";

        public FileStore()
        {
            Amazon.AWSConfigsS3.UseSignatureVersion4 = false;
            AmazonS3Config config = new AmazonS3Config
            {
                ServiceURL = "http://localhost:8080",
                SignatureVersion = "s3",
                ForcePathStyle = true,

            };
            client = new AmazonS3Client(accessKey, secretKey, config);
        }

        public async Task NewBucket(String bucketName)
        {
            PutBucketResponse response = null;
            try
            {
                PutBucketRequest putRequest = new PutBucketRequest
                {
                    BucketName = bucketName,
                    UseClientRegion = true
                };
                bool found = await IfBucketExist(bucketName);
                if (!found)
                {
                    response = await client.PutBucketAsync(putRequest);
                    if (response.HttpStatusCode.ToString() == "OK")
                    {
                        Console.WriteLine("New bucket " + bucketName + " was created");
                    }
                }
                else
                {
                    Console.WriteLine("Bucket already exists");
                }
            }
            catch (AmazonS3Exception amazonS3Exception)
            {
                if (amazonS3Exception.ErrorCode != null &&
                    (amazonS3Exception.ErrorCode.Equals("InvalidAccessKeyId")
                    ||
                    amazonS3Exception.ErrorCode.Equals("InvalidSecurity")))
                {
                    Console.WriteLine("Check the provided AWS Credentials.");
                    Console.WriteLine(
                        "For service sign up go to http://aws.amazon.com/s3");
                }
                else
                {
                    Console.WriteLine(
                        "Error occurred. Message:'{0}' when writing an object"
                        , amazonS3Exception.Message);
                }
            }
        }

        private async Task<bool> IfBucketExist(String bucketName)
        {
            ListBucketsResponse list = await client.ListBucketsAsync();
            bool found = false;
            Parallel.ForEach(list.Buckets, (S3Bucket bucket) =>
            {
                if (bucket.BucketName == bucketName)
                {
                    found = true;
                }
            });
            return found;
        }

        public async Task DeleteBucket(String bucketName)
        {
            DeleteBucketResponse response = null;
            DeleteBucketRequest request = new DeleteBucketRequest
            {
                BucketName = bucketName
            };
            bool found = await IfBucketExist(bucketName);
            if (!found)
            {
                Console.WriteLine("bucket not found");
            }
            else
            {
                try
                {
                    response = await client.DeleteBucketAsync(request);
                    if (response.HttpStatusCode.ToString() == "OK")
                    {
                        Console.WriteLine("Bucket " + bucketName + " was deleted");
                    }
                }
                catch (AmazonS3Exception e)
                {
                    Console.WriteLine("Bucket is not empty.. Emptying bucket");
                    Console.WriteLine(e.ToString());
                }
            }

        }

        public async Task<ListBucketsResponse> ListBuckets()
        {
            ListBucketsResponse list = null;
            try
            {
                list = await client.ListBucketsAsync();
                foreach (S3Bucket bucket in list.Buckets)
                {
                    Console.WriteLine(bucket.BucketName);
                }
            }
            catch (AmazonS3Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return list;
        }

        public async Task<FileChunkListContainer> UploadFile(String filePath)
        {
            FileChunkListContainer container = new FileChunkListContainer();
            SetInitialContainerProperties(container, filePath);
            if (File.Exists(filePath))
            {
                try
                {
                    MemoryMappedFile memoryMappedFile = memoryMappedFile = MemoryMappedFile.CreateFromFile(filePath, FileMode.Open, "mappedfile");
                    MemoryMappedViewAccessor accessor = null;
                    Transfer transferObj = new Transfer();
                    transferObj.TransferStatus = true;
                    while (transferObj.TransferStatus)
                    {
                        if (!transferObj.ResumeStatus)
                        {
                            accessor = memoryMappedFile.CreateViewAccessor();
                            FileInfo file = new FileInfo(filePath);
                            int length = (int)file.Length;
                            int totalChunks = (length / (1024 * 1024)) + 1;
                            Parallel.For(0, totalChunks, (j) =>
                            {
                                FileChunkObject chunk = new FileChunkObject();
                                SetInitialChunkProperties(chunk);
                                chunk.Offset = 1024 * 1024 * j;
                                UploadChunk(accessor, chunk).Wait();
                                container.chunkList.Add(chunk);
                            });
                            accessor.Dispose();
                            UpdateContainerProperties(container);
                            transferObj.UpdateResumeStatus(container.chunkList);
                        }
                        else
                        {
                            Console.WriteLine("resuming upload..");
                            Parallel.ForEach(container.chunkList, (FileChunkObject chunk) =>
                            {
                                if (!chunk.ChunkStatus)
                                {
                                    accessor = memoryMappedFile.CreateViewAccessor();
                                    UploadChunk(accessor, chunk).Wait();
                                }
                            });
                            UpdateContainerProperties(container);
                            transferObj.UpdateResumeStatus(container.chunkList);

                        }
                        transferObj.UpdateTransferStatus(container.chunkList);
                    }
                }
                catch (Exception e)
                {
                    throw new Exception(e.Message);
                }
            }
            return container;
        }

        public async Task<Boolean> DownloadFile(FileChunkListContainer container, String downloadLocation)
        {
            Boolean downloadStatus = false;
            int sizeOfFileToDownload = container.FileSize;
            SetInitialContainerProperties(container);
            string downloadPath = downloadLocation + container.CloudFileName;
            if (File.Exists(downloadPath))
            {
                downloadPath = @"C:\\Users\\Quarx\\Music\\" + container.CloudFileName + "copy";
            }
            try
            {
                MemoryMappedFile memoryMappedFile = MemoryMappedFile.CreateFromFile(downloadPath, FileMode.CreateNew, "log-map", sizeOfFileToDownload);
                Transfer transferObj = new Transfer();
                transferObj.TransferStatus = true;
                while (transferObj.TransferStatus)
                {
                    if (!transferObj.ResumeStatus)
                    {
                        Parallel.ForEach(container.chunkList, (FileChunkObject chunk) =>
                        {
                            DownloadChunk(memoryMappedFile, chunk).Wait();

                        });
                        Parallel.ForEach(container.chunkList, (FileChunkObject chunk) =>
                        {
                            Console.WriteLine("offset: " + chunk.Offset + " status: " + chunk.ChunkStatus + " size: " + chunk.size);
                        });
                        UpdateContainerProperties(container);
                        transferObj.UpdateResumeStatus(container.chunkList);
                    }
                    else
                    {
                        Parallel.ForEach(container.chunkList, (FileChunkObject chunk) =>
                        {
                            if (!chunk.ChunkStatus)
                            {
                                DownloadChunk(memoryMappedFile, chunk).Wait();

                            }
                        });
                        UpdateContainerProperties(container);
                        transferObj.UpdateResumeStatus(container.chunkList);
                    }
                    transferObj.UpdateTransferStatus(container.chunkList);
                }
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
           
            downloadStatus = container.status;
            return downloadStatus;
        }

        private async Task UploadChunk(MemoryMappedViewAccessor accessor, FileChunkObject chunk)
        {
            byte[] bytes = new byte[1024 * 1024 * 1];
            int readBytes = accessor.ReadArray(chunk.Offset, bytes, 0, bytes.Length);
            Stream uploadStream = new MemoryStream(readBytes);
            uploadStream.Write(bytes, 0, readBytes);
            ChunkOperation newUpload = new ChunkOperation(client, chunk, uploadStream);
            Task<Boolean> uploadTask = newUpload.Upload();
            Boolean uploadStatus = await uploadTask;
            UpdateChunkProperties(chunk, uploadStatus, readBytes);
        }

        // For upload
        private void SetInitialContainerProperties(FileChunkListContainer container, String filePath)
        {
            container.CloudFileName = Path.GetFileName(filePath);
            container.status = false;
            container.FileSize = 0;
            container.chunkList = new List<FileChunkObject>();
        }

        // For upload
        private void UpdateContainerProperties(FileChunkListContainer container)
        {
            Boolean status = true;
            container.FileSize = 0;
            foreach (FileChunkObject c in container.chunkList)
            {
                container.FileSize += c.size;
            }
            foreach (FileChunkObject c in container.chunkList)
            {
                if (!c.ChunkStatus)
                {
                    status = false;
                    break;
                }
            }
            container.status = status;
        }

        // For download
        private void SetInitialContainerProperties(FileChunkListContainer container)
        {
            container.status = false;
            container.FileSize = 0;
            foreach (FileChunkObject c in container.chunkList)
            {
                c.ChunkStatus = false;
                c.size = 0;
            }
        }

        // For upload
        private void SetInitialChunkProperties(FileChunkObject chunk)
        {
            chunk.FileName = Guid.NewGuid().ToString();
            chunk.ChunkStatus = false;
            chunk.size = 0;
            chunk.Offset = 0;
        }

        // For upload
        private void UpdateChunkProperties(FileChunkObject chunk, Boolean status, int readBytes)
        {
            chunk.ChunkStatus = status;
            chunk.size = readBytes;
        }

        private async Task DownloadChunk(MemoryMappedFile mmf, FileChunkObject chunk)
        {
            try
            {
                MemoryMappedViewAccessor accessor = mmf.CreateViewAccessor(chunk.Offset, chunk.size);
                ChunkOperation toDownload = new ChunkOperation(client, chunk, accessor);
                Task<MemoryStream> task = toDownload.Download();
                MemoryStream downloadStream = await task;
                downloadStream.Position = 0;
                Byte[] bytes = new Byte[1024 * 1024 * 1];
                int readBytes = downloadStream.Read(bytes, 0, bytes.Length);
                accessor.WriteArray(0, bytes, 0, readBytes);
                chunk.ChunkStatus = true;
                chunk.size = readBytes;
                accessor.Dispose();
                downloadStream.Dispose();
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }
    }
}
