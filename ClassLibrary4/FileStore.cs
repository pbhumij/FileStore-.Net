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

   
    }
}
