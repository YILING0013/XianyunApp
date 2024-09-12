using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using xianyun.Common;

namespace xianyun.API
{
    public class XianyunApiClient
    {
        private readonly HttpClient _httpClient;

        public XianyunApiClient(string baseUrl, string sessionCookie)
        {
            var handler = new HttpClientHandler();
            handler.CookieContainer = new CookieContainer();
            var uri = new Uri(baseUrl);
            handler.CookieContainer.Add(uri, new Cookie("session", sessionCookie));

            _httpClient = new HttpClient(handler) { BaseAddress = uri };
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36 Edg/124.0.0.0");
        }

        // 生成图像请求
        public async Task<(string jobId, int queuePosition)> GenerateImageAsync(ImageGenerationRequest request)
        {
            var json = JsonConvert.SerializeObject(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // 输出请求信息
            Console.WriteLine("Request URL: " + _httpClient.BaseAddress + "api/generate_image");
            Console.WriteLine("Request Headers: ");
            foreach (var header in _httpClient.DefaultRequestHeaders)
            {
                Console.WriteLine($"{header.Key}: {string.Join(",", header.Value)}");
            }
            Console.WriteLine("Request Body: " + json);

            var response = await _httpClient.PostAsync("api/generate_image", content);

            if (response.IsSuccessStatusCode)
            {
                var responseData = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<GenerateImageResponse>(responseData);
                return (result.JobId, result.QueuePosition);
            }
            else
            {
                var errorData = await response.Content.ReadAsStringAsync();
                throw new Exception($"Request failed with status code {response.StatusCode}: {errorData}");
            }
        }
        // 检查图像生成结果
        public async Task<(string status, string imageBase64, int queuePosition)> CheckResultAsync(string jobId)
        {
            var response = await _httpClient.GetAsync($"api/get_result/{jobId}");
            if (response.IsSuccessStatusCode)
            {
                var responseData = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<GetResultResponse>(responseData);

                if (result.Status == "completed")
                {
                    // 返回生成完成的状态及图像数据
                    return (result.Status, result.ImageBase64, 0);
                }
                else if (result.Status == "queued")
                {
                    // 返回队列中的位置
                    return (result.Status, null, result.QueuePosition);
                }
                else if (result.Status == "processing")
                {
                    // 返回正在处理的状态
                    return (result.Status, null, 0); // 此时QueuePosition为0
                }
                else if (result.Status == "failed")
                {
                    throw new Exception("图像生成失败。");
                }
            }

            throw new Exception($"检查结果时发生错误: {response.StatusCode}");
        }
    }
    // 请求结构体
    public class ImageGenerationRequest
    {
        [JsonProperty("model")]
        public string Model { get; set; }

        [JsonProperty("positivePrompt")]
        public string PositivePrompt { get; set; }

        [JsonProperty("negativePrompt")]
        public string NegativePrompt { get; set; }

        [JsonProperty("scale")]
        public double Scale { get; set; }

        [JsonProperty("steps")]
        public int Steps { get; set; }

        [JsonProperty("width")]
        public int Width { get; set; }

        [JsonProperty("height")]
        public int Height { get; set; }

        [JsonProperty("promptGuidanceRescale")]
        public double PromptGuidanceRescale { get; set; }

        [JsonProperty("noise_schedule")]
        public string NoiseSchedule { get; set; }

        [JsonProperty("seed")]
        public string Seed { get; set; }

        [JsonProperty("sampler")]
        public string Sampler { get; set; }

        [JsonProperty("sm")]
        public bool Sm { get; set; }

        [JsonProperty("sm_dyn")]
        public bool SmDyn { get; set; }

        [JsonProperty("pictureid")]
        public string PictureId { get; set; }

        [JsonProperty("reference_image_multiple", NullValueHandling = NullValueHandling.Ignore)]
        public string[] ReferenceImage { get; set; }

        [JsonProperty("reference_information_extracted_multiple", NullValueHandling = NullValueHandling.Ignore)]
        public double[] InformationExtracted { get; set; }

        [JsonProperty("reference_strength_multiple", NullValueHandling = NullValueHandling.Ignore)]
        public double[] ReferenceStrength { get; set; }
    }

    // 生成图像响应结构体
    public class GenerateImageResponse
    {
        [JsonProperty("job_id")]
        public string JobId { get; set; }

        [JsonProperty("queue_position")]
        public int QueuePosition { get; set; }
    }

    // 获取结果响应结构体
    public class GetResultResponse
    {
        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("image_base64")]
        public string ImageBase64 { get; set; }

        [JsonProperty("completed_at")]
        public string CompletedAt { get; set; }
        [JsonProperty("queue_position")]
        public int QueuePosition { get; set; }
    }
}
