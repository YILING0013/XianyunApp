using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO.Compression;
using System.Linq;
using xianyun.MainPages;
using static xianyun.ViewModel.MainViewModel;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace xianyun.API
{
    public class NovelAiApiClient
    {
        private readonly HttpClient _httpClient;

        public NovelAiApiClient(string baseUrl, string apiKey)
        {
            var handler = new HttpClientHandler();
            _httpClient = new HttpClient(handler) { BaseAddress = new Uri(baseUrl) };

            // 设置 Authorization header
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(apiKey.Split(' ')[0], apiKey.Split(' ')[1]);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36 Edg/124.0.0.0");
        }

        /// <summary>
        /// 发送图像生成请求
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<string> GenerateImageAsync(NovelAiRequest request)
        {
            var json = JsonConvert.SerializeObject(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            string apiPath = "ai/generate-image"; // 默认接口路径
            var jsonObject = JObject.Parse(json);
            if (jsonObject.ContainsKey("req_type"))
            {
                string reqType = jsonObject["req_type"].ToString().ToLower();
                switch (reqType)
                {
                    default:
                        apiPath = "ai/augment-image";
                        break;
                }
            }

            LogPage.LogMessage($"Request URL: {_httpClient.BaseAddress}{apiPath}");
            LogPage.LogMessage($"Request Body: {json}");

            var response = await _httpClient.PostAsync(apiPath, content);

            if (response.IsSuccessStatusCode)
            {
                var responseData = await response.Content.ReadAsByteArrayAsync();
                string imageBase64 = await ExtractZipFileAsync(responseData);
                return imageBase64;
            }
            else
            {
                var errorData = await response.Content.ReadAsStringAsync();
                throw new Exception($"Request failed with status code {response.StatusCode}: {errorData}");
            }
        }

        /// <summary>
        /// 解压返回的ZIP文件，并返回图像的Base64编码
        /// </summary>
        /// <param name="fileData">压缩文件字节数组</param>
        /// <returns>解压后的图像文件的Base64编码</returns>
        private async Task<string> ExtractZipFileAsync(byte[] fileData)
        {
            using (var memoryStream = new MemoryStream(fileData))
            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Read))
            {
                // 获取ZIP中的第一个文件条目（假设只有一个图像文件）
                var entry = archive.Entries.FirstOrDefault();
                if (entry == null)
                {
                    throw new Exception("ZIP文件中不存在任何条目。");
                }

                using (var entryStream = entry.Open())
                using (var ms = new MemoryStream())
                {
                    await entryStream.CopyToAsync(ms);
                    byte[] imageBytes = ms.ToArray();
                    string base64Image = Convert.ToBase64String(imageBytes);
                    return base64Image;
                }
            }
        }
    }

    /// <summary>
    /// NovelAI API请求结构体
    /// </summary>
    public class NovelAiRequest
    {
        [JsonProperty("action", NullValueHandling = NullValueHandling.Ignore)]
        public string Action { get; set; }

        [JsonProperty("input", NullValueHandling = NullValueHandling.Ignore)]
        public string Input { get; set; }

        [JsonProperty("model", NullValueHandling = NullValueHandling.Ignore)]
        public string Model { get; set; }

        [JsonProperty("parameters", NullValueHandling = NullValueHandling.Ignore)]
        public NovelAiParameters Parameters { get; set; }

        [JsonProperty("height", NullValueHandling = NullValueHandling.Ignore)]
        public int? Height { get; set; }

        [JsonProperty("defry", NullValueHandling = NullValueHandling.Ignore)]
        public int? Defry { get; set; }

        [JsonProperty("image", NullValueHandling = NullValueHandling.Ignore)]
        public string Image { get; set; }

        [JsonProperty("prompt", NullValueHandling = NullValueHandling.Ignore)]
        public string Prompt { get; set; }

        [JsonProperty("req_type", NullValueHandling = NullValueHandling.Ignore)]
        public string ReqType { get; set; }

        [JsonProperty("width", NullValueHandling = NullValueHandling.Ignore)]
        public int? Width { get; set; }
    }

    /// <summary>
    /// NovelAI请求中的参数部分
    /// </summary>
    public class NovelAiParameters
    {
        [JsonProperty("add_original_image", NullValueHandling = NullValueHandling.Ignore)]
        public bool? AddOriginalImage { get; set; }

        [JsonProperty("cfg_rescale", NullValueHandling = NullValueHandling.Ignore)]
        public double? CfgRescale { get; set; }

        [JsonProperty("controlnet_condition", NullValueHandling = NullValueHandling.Ignore)]
        public string ControlnetCondition { get; set; }

        [JsonProperty("controlnet_model", NullValueHandling = NullValueHandling.Ignore)]
        public string ControlnetModel { get; set; }

        [JsonProperty("controlnet_strength", NullValueHandling = NullValueHandling.Ignore)]
        public double? ControlnetStrength { get; set; }

        [JsonProperty("deliberate_euler_ancestral_bug", NullValueHandling = NullValueHandling.Ignore)]
        public bool? DeliberateEulerAncestralBug { get; set; }

        [JsonProperty("dynamic_thresholding", NullValueHandling = NullValueHandling.Ignore)]
        public bool? DynamicThresholding { get; set; }

        [JsonProperty("extra_noise_seed", NullValueHandling = NullValueHandling.Ignore)]
        public uint? ExtraNoiseSeed { get; set; }

        [JsonProperty("height", NullValueHandling = NullValueHandling.Ignore)]
        public int? Height { get; set; }

        [JsonProperty("image", NullValueHandling = NullValueHandling.Ignore)]
        public string Image { get; set; }

        [JsonProperty("legacy", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Legacy { get; set; }

        [JsonProperty("legacy_v3_extend", NullValueHandling = NullValueHandling.Ignore)]
        public bool? LegacyV3Extend { get; set; }

        [JsonProperty("mask", NullValueHandling = NullValueHandling.Ignore)]
        public string Mask { get; set; }

        [JsonProperty("negative_prompt", NullValueHandling = NullValueHandling.Ignore)]
        public string NegativePrompt { get; set; }

        [JsonProperty("noise", NullValueHandling = NullValueHandling.Ignore)]
        public float? Noise { get; set; }

        [JsonProperty("noise_schedule", NullValueHandling = NullValueHandling.Ignore)]
        public string NoiseSchedule { get; set; }

        [JsonProperty("params_version", NullValueHandling = NullValueHandling.Ignore)]
        public int? ParamsVersion { get; set; }

        [JsonProperty("prefer_brownian", NullValueHandling = NullValueHandling.Ignore)]
        public bool? PreferBrownian { get; set; }

        [JsonProperty("prompt", NullValueHandling = NullValueHandling.Ignore)]
        public string Prompt { get; set; }

        [JsonProperty("qualityToggle", NullValueHandling = NullValueHandling.Ignore)]
        public bool? QualityToggle { get; set; }

        [JsonProperty("reference_image_multiple", NullValueHandling = NullValueHandling.Ignore)]
        public string[] ReferenceImageMultiple { get; set; }

        [JsonProperty("reference_information_extracted_multiple", NullValueHandling = NullValueHandling.Ignore)]
        public double[] ReferenceInformationExtractedMultiple { get; set; }

        [JsonProperty("reference_strength_multiple", NullValueHandling = NullValueHandling.Ignore)]
        public double[] ReferenceStrengthMultiple { get; set; }

        [JsonProperty("sampler", NullValueHandling = NullValueHandling.Ignore)]
        public string Sampler { get; set; }

        [JsonProperty("n_samples", NullValueHandling = NullValueHandling.Ignore)]
        public int? NSamples { get; set; }

        [JsonProperty("scale", NullValueHandling = NullValueHandling.Ignore)]
        public double? Scale { get; set; }

        [JsonProperty("seed", NullValueHandling = NullValueHandling.Ignore)]
        public uint? Seed { get; set; }

        [JsonProperty("skip_cfg_above_sigma", NullValueHandling = NullValueHandling.Ignore)]
        public int? SkipCfgAboveSigma { get; set; }

        [JsonProperty("sm", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Sm { get; set; }

        [JsonProperty("sm_dyn", NullValueHandling = NullValueHandling.Ignore)]
        public bool? SmDyn { get; set; }

        [JsonProperty("steps", NullValueHandling = NullValueHandling.Ignore)]
        public int? Steps { get; set; }

        [JsonProperty("strength", NullValueHandling = NullValueHandling.Ignore)]
        public float? Strength { get; set; }

        [JsonProperty("ucPreset", NullValueHandling = NullValueHandling.Ignore)]
        public int? UcPreset { get; set; }

        [JsonProperty("width", NullValueHandling = NullValueHandling.Ignore)]
        public int? Width { get; set; }

        [JsonProperty("characterPrompts", NullValueHandling = NullValueHandling.Ignore)]
        public List<CharacterPrompt> CharacterPrompts { get; set; }

        [JsonProperty("v4_prompt", NullValueHandling = NullValueHandling.Ignore)]
        public V4Prompt V4Prompt { get; set; }

        [JsonProperty("v4_negative_prompt", NullValueHandling = NullValueHandling.Ignore)]
        public V4NegativePrompt V4NegativePrompt { get; set; }

        [JsonProperty("use_coords", NullValueHandling = NullValueHandling.Ignore)]
        public bool? UseCoords { get; set; }
    }


    public class V4Prompt
    {
        [JsonProperty("caption")]
        public V4PromptCaption Caption { get; set; }

        [JsonProperty("use_coords")]
        public bool UseCoords { get; set; } = true;

        [JsonProperty("use_order")]
        public bool UseOrder { get; set; } = true;
    }

    public class V4NegativePrompt
    {
        [JsonProperty("caption")]
        public V4NegativePromptCaption Caption { get; set; }
    }

    public class V4PromptCaption
    {
        [JsonProperty("base_caption")]
        public string BaseCaption { get; set; }

        [JsonProperty("char_captions")]
        public List<CharCaption> CharCaptions { get; set; }
    }

    public class V4NegativePromptCaption
    {
        [JsonProperty("base_caption")]
        public string BaseCaption { get; set; }

        [JsonProperty("char_captions")]
        public List<CharCaption> CharCaptions { get; set; }
    }
}
