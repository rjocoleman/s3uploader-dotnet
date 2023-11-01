using System;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using DotNetEnv;
using Newtonsoft.Json;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;

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

            using (var s3Client = new AmazonS3Client(accessKey, secretKey, region))
            {
                // Fetch existing version.json
                var getObjectRequest = new GetObjectRequest
                {
                    BucketName = bucketName,
                    Key = "version.json"
                };
                var getObjectResponse = s3Client.GetObjectAsync(getObjectRequest).Result;
                using (var reader = new StreamReader(getObjectResponse.ResponseStream))
                {
                    var content = reader.ReadToEnd();
                    var versionData = JsonConvert.DeserializeObject<VersionData>(content);

                    versionData.latest = "1.3.0";
                    versionData.versions["1.3.0"] = $"/apks/test-1.3.0.apk";
                    versionData.changelog["1.3.0"] = "The changelog for 1.3.0";

                    UploadFile(s3Client, bucketName,
                        () => File.OpenRead("test-file.txt"),
                        $"apks/test-{versionData.latest}.apk");
                    UploadFile(s3Client, bucketName,
                            () => new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(versionData, Formatting.Indented))),
                            "version-test.json",
                            "application/json");
                }
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

        public class VersionData
        {
            public string latest { get; set; } = string.Empty;
            public Dictionary<string, string> versions { get; set; } = new Dictionary<string, string>();
            public Dictionary<string, string> changelog { get; set; } = new Dictionary<string, string>();
        }
    }
}
