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
        [JsonProperty("action", NullValueHandling = NullValueHandling.Ignore)]
        public bool Action { get; set; }

        [JsonProperty("model", NullValueHandling = NullValueHandling.Ignore)]
        public string Model { get; set; }

        [JsonProperty("positivePrompt", NullValueHandling = NullValueHandling.Ignore)]
        public string PositivePrompt { get; set; }

        [JsonProperty("negativePrompt", NullValueHandling = NullValueHandling.Ignore)]
        public string NegativePrompt { get; set; }

        [JsonProperty("scale", NullValueHandling = NullValueHandling.Ignore)]
        public double Scale { get; set; }

        [JsonProperty("steps", NullValueHandling = NullValueHandling.Ignore)]
        public int Steps { get; set; }

        [JsonProperty("width", NullValueHandling = NullValueHandling.Ignore)]
        public int Width { get; set; }

        [JsonProperty("height", NullValueHandling = NullValueHandling.Ignore)]
        public int Height { get; set; }

        [JsonProperty("promptGuidanceRescale", NullValueHandling = NullValueHandling.Ignore)]
        public double PromptGuidanceRescale { get; set; }

        [JsonProperty("noise_schedule", NullValueHandling = NullValueHandling.Ignore)]
        public string NoiseSchedule { get; set; }

        [JsonProperty("strength", NullValueHandling = NullValueHandling.Ignore)]
        public double Strength { get; set; }

        [JsonProperty("noise", NullValueHandling = NullValueHandling.Ignore)]
        public float Noise { get; set; }

        [JsonProperty("seed", NullValueHandling = NullValueHandling.Ignore)]
        public string Seed { get; set; }

        [JsonProperty("sampler", NullValueHandling = NullValueHandling.Ignore)]
        public string Sampler { get; set; }

        [JsonProperty("decrisp", NullValueHandling = NullValueHandling.Ignore)]
        public bool Decrisp { get; set; }

        [JsonProperty("variety", NullValueHandling = NullValueHandling.Ignore)]
        public bool Variety { get; set; }

        [JsonProperty("sm", NullValueHandling = NullValueHandling.Ignore)]
        public bool Sm { get; set; }

        [JsonProperty("sm_dyn", NullValueHandling = NullValueHandling.Ignore)]
        public bool SmDyn { get; set; }

        [JsonProperty("pictureid", NullValueHandling = NullValueHandling.Ignore)]
        public string PictureId { get; set; }

        [JsonProperty("reference_image_multiple", NullValueHandling = NullValueHandling.Ignore)]
        public string[] ReferenceImage { get; set; }

        [JsonProperty("reference_information_extracted_multiple", NullValueHandling = NullValueHandling.Ignore)]
        public double[] InformationExtracted { get; set; }

        [JsonProperty("reference_strength_multiple", NullValueHandling = NullValueHandling.Ignore)]
        public double[] ReferenceStrength { get; set; }

        [JsonProperty("req_type", NullValueHandling = NullValueHandling.Ignore)]
        public string ReqType { get; set; }

        [JsonProperty("image",NullValueHandling = NullValueHandling.Ignore)]
        public string Image { get; set; }

        [JsonProperty("mask", NullValueHandling = NullValueHandling.Ignore)]
        public string Mask { get; set; }

        [JsonProperty("prompt", NullValueHandling = NullValueHandling.Ignore)]
        public string Prompt { get; set; }

        [JsonProperty("defry", NullValueHandling = NullValueHandling.Ignore)]
        public int Defry { get; set; }
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
