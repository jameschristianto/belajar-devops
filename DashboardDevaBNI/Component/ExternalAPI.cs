using Microsoft.IdentityModel.Tokens;
using Minio.DataModel.Args;
using Minio;
using Newtonsoft.Json;
using System.ComponentModel;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Xml;
using DashboardDevaBNI.ViewModels;
using System.IO;
using DashboardDevaBNI.Component;
using System.Security.Cryptography;

namespace Trustee.Component
{
    public class ExternalAPI
    {

        private static string GenerateRandomString(int length)
        {
            using (var randomGenerator = RandomNumberGenerator.Create())
            {
                byte[] data = new byte[4]; // 4 bytes is enough to store a 32-bit integer
                randomGenerator.GetBytes(data);

                // Convert the byte array to an integer
                int value = BitConverter.ToInt32(data, 0) & 0x7FFFFFFF; // Ensure positive number

                // Restrict the integer to a 5-digit number range
                int fiveDigitNumber = value % 90000 + 10000; // Range: 10000 - 99999

                return fiveDigitNumber.ToString();
            }
        }

        //private static string GenerateRandomString(int length)
        //{
        //    const string chars = "0123456789";
        //    StringBuilder randomString = new StringBuilder();

        //    Random random = new Random(); 
        //    for (int i = 0; i < length; i++)
        //    {
        //        randomString.Append(chars[random.Next(chars.Length)]);
        //    }

        //    return randomString.ToString();
        //}

        public async static Task<bool> UploadMinio(MemoryStream file, string fileName)
        {
            try
            {
                var minioEndpoint = GetConfig.AppSetting["Minio:Endpoint"];
                var accessKey = GetConfig.AppSetting["Minio:AccessKey"];
                var secretKey = GetConfig.AppSetting["Minio:SecretKey"];
                var secure = false;
                var bucket = GetConfig.AppSetting["Minio:Bucket"];

                var _minio = new MinioClient().WithEndpoint(minioEndpoint).WithCredentials(accessKey, secretKey).WithSSL(secure).Build();
                bool found = await _minio.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucket));
                if (!found)
                {
                    await _minio.MakeBucketAsync(new MakeBucketArgs().WithBucket(bucket));
                }

                var poa = new PutObjectArgs().WithBucket(GetConfig.AppSetting["Minio:Bucket"])
                                                            .WithObject(fileName)
                                                            .WithStreamData(new MemoryStream(file.ToArray()))
                                                            .WithObjectSize(file.Length)
                                                            .WithContentType("application/octet-stream");
                await _minio.PutObjectAsync(poa);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async static Task<bool> CheckMinio(string fileName)
        {
            try
            {
                var minioEndpoint = GetConfig.AppSetting["Minio:Endpoint"];
                var accessKey = GetConfig.AppSetting["Minio:AccessKey"];
                var secretKey = GetConfig.AppSetting["Minio:SecretKey"];
                var secure = false;
                var bucket = GetConfig.AppSetting["Minio:Bucket"];

                var _minio = new MinioClient().WithEndpoint(minioEndpoint).WithCredentials(accessKey, secretKey).WithSSL(secure).Build();

                var poa = new StatObjectArgs().WithBucket(GetConfig.AppSetting["Minio:Bucket"])
                                                            .WithObject(fileName);
                await _minio.StatObjectAsync(poa);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async static Task<byte[]> DownloadMinio(string fileName)
        {
            try
            {
                var minioEndpoint = GetConfig.AppSetting["Minio:Endpoint"];
                var accessKey = GetConfig.AppSetting["Minio:AccessKey"];
                var secretKey = GetConfig.AppSetting["Minio:SecretKey"];
                var secure = false;
                var bucket = GetConfig.AppSetting["Minio:Bucket"];

                var _minio = new MinioClient().WithEndpoint(minioEndpoint).WithCredentials(accessKey, secretKey).WithSSL(secure).Build();
                bool found = await _minio.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucket));
                if (!found)
                {
                    await _minio.MakeBucketAsync(new MakeBucketArgs().WithBucket(bucket));
                    return null;
                }

                var downloadFile = new MemoryStream();

                var poa = new GetObjectArgs().WithBucket(GetConfig.AppSetting["Minio:Bucket"])
                                                            .WithObject(fileName)
                                                            .WithCallbackStream((stream) =>
                                                            {
                                                                stream.CopyTo(downloadFile);
                                                            });
                await _minio.GetObjectAsync(poa);
                return downloadFile.ToArray();
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
