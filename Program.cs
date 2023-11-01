using System;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using DotNetEnv;
using Newtonsoft.Json;
using System.IO;
using System.Text;

namespace S3Uploader
{
    class Program
    {
        static void Main(string[] args)
        {
            Env.Load();

            var accessKey = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID");
            var secretKey = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY");
            var regionString = Environment.GetEnvironmentVariable("AWS_REGION");
            var bucketName = Environment.GetEnvironmentVariable("BUCKET_NAME");

            if (string.IsNullOrEmpty(accessKey) ||
                string.IsNullOrEmpty(secretKey) ||
                string.IsNullOrEmpty(regionString) ||
                string.IsNullOrEmpty(bucketName))
            {
                Console.Error.WriteLine("One or more environment variables are not set.");
                return;
            }
            var region = GetRegionFromEnv();

            var data = new
            {
                latest = "1.2.0",
                versions = new Dictionary<string, string>
                {
                    {"1.0.0", "/apks/test-1.0.0.apk"},
                    {"1.0.1", "/apks/test-1.0.1.apk"},
                    {"1.1.0", "/apks/test-1.1.0.apk"},
                    {"1.1.1", "/apks/test-1.1.1.apk"},
                    {"1.2.0", "/apks/test-1.2.0.apk"},
                },
                changelog = new Dictionary<string, string>
                {
                    {"1.0.0", "Initial release"},
                    {"1.0.1", "Bug fixes"},
                    {"1.1.0", "Added new features"},
                    {"1.1.1", "Performance improvements"},
                    {"1.2.0", "Updated UI and fixed some issues"},
                }
            };

            using (var s3Client = new AmazonS3Client(accessKey, secretKey, region))
            {
                UploadFile(s3Client, bucketName,
                    () => File.OpenRead("test-file.txt"),
                    $"apks/test-{data.latest}.apk");
                UploadFile(s3Client, bucketName,
                    () => new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data, Formatting.Indented))),
                    "version-test.json",
                    "application/json");
            }
        }

        static RegionEndpoint GetRegionFromEnv()
        {
            var regionString = Environment.GetEnvironmentVariable("AWS_REGION");
            return RegionEndpoint.GetBySystemName(regionString);
        }

        static void UploadFile(AmazonS3Client s3Client, string bucketName, Func<Stream> dataProvider, string destinationKey, string contentType = "application/octet-stream")
        {
            using var dataStream = dataProvider();

            try
            {
                var request = new PutObjectRequest
                {
                    BucketName = bucketName,
                    InputStream = dataStream,
                    Key = destinationKey,
                    ContentType = contentType
                };

                var response = s3Client.PutObjectAsync(request).Result;
                Console.WriteLine("Upload successful!");
            }
            catch (AmazonS3Exception amazonS3Exception)
            {
                Console.WriteLine($"Error encountered on server. Message: '{amazonS3Exception.Message}' when writing an object");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Unknown encountered on server. Message:'{e.Message}' when writing an object");
            }
        }
    }
}
