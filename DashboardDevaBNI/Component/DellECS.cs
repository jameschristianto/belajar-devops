using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;

namespace DashboadVaBPDLH.Component
{
    public class DellECS
    {
        private static IConfiguration _configuration;

        static async Task Main(string[] args)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");

            _configuration = builder.Build();

            var s3Client = new AmazonS3Client(
                _configuration["AWS:AccessKey"],
                _configuration["AWS:SecretKey"],
                new AmazonS3Config
                {
                    ServiceURL = _configuration["AWS:ServiceURL"],
                    ForcePathStyle = true
                });

            // Example: Create a bucket
            var bucketName = "my-ecs-bucket";
            await CreateBucketAsync(s3Client, bucketName);

            // Example: Upload a file
            var filePath = "path/to/your/file.txt";
            await UploadFileAsync(s3Client, bucketName, filePath);

            // Example: List objects
            await ListObjectsAsync(s3Client, bucketName);

            // Example: Download a file
            var downloadPath = "path/to/download/location/file.txt";
            await DownloadFileAsync(s3Client, bucketName, "file.txt", downloadPath);
        }

        private static async Task CreateBucketAsync(IAmazonS3 client, string bucketName)
        {
            try
            {
                var putBucketRequest = new PutBucketRequest
                {
                    BucketName = bucketName
                };

                var response = await client.PutBucketAsync(putBucketRequest);
                Console.WriteLine($"Bucket created: {bucketName}");
            }
            catch (AmazonS3Exception e)
            {
                Console.WriteLine($"Error encountered on server. Message:'{e.Message}' when writing an object");
            }
        }

        private static async Task UploadFileAsync(IAmazonS3 client, string bucketName, string filePath)
        {
            try
            {
                var fileTransferUtility = new TransferUtility(client);
                await fileTransferUtility.UploadAsync(filePath, bucketName);
                Console.WriteLine($"File uploaded: {filePath}");
            }
            catch (AmazonS3Exception e)
            {
                Console.WriteLine($"Error encountered on server. Message:'{e.Message}' when writing an object");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Unknown encountered on server. Message:'{e.Message}' when writing an object");
            }
        }

        private static async Task ListObjectsAsync(IAmazonS3 client, string bucketName)
        {
            try
            {
                var request = new ListObjectsV2Request
                {
                    BucketName = bucketName,
                };

                var response = await client.ListObjectsV2Async(request);

                foreach (S3Object entry in response.S3Objects)
                {
                    Console.WriteLine($"Object - {entry.Key} Size - {entry.Size}");
                }
            }
            catch (AmazonS3Exception e)
            {
                Console.WriteLine($"Error encountered on server. Message:'{e.Message}' when listing objects");
            }
        }

        private static async Task DownloadFileAsync(IAmazonS3 client, string bucketName, string keyName, string filePath)
        {
            try
            {
                var fileTransferUtility = new TransferUtility(client);
                await fileTransferUtility.DownloadAsync(filePath, bucketName, keyName);
                Console.WriteLine($"File downloaded: {filePath}");
            }
            catch (AmazonS3Exception e)
            {
                Console.WriteLine($"Error encountered on server. Message:'{e.Message}' when downloading an object");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Unknown encountered on server. Message:'{e.Message}' when downloading an object");
            }
        }
    }
}

