using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using xianyun.Common;
using xianyun.MainPages;

namespace xianyun.API
{
    public class GetSubscription
    {
        public static async Task GetSubscriptionInfoAsync()
        {
            using (var httpClient = new HttpClient())
            {
                // 设置请求头，包括 Authorization
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(SessionManager.Token.Split(' ')[0], SessionManager.Token.Split(' ')[1]);

                // 发送 GET 请求
                var response = await httpClient.GetAsync("https://api.novelai.net/user/subscription");
                LogPage.LogMessage(LogLevel.INFO, $"GET https://api.novelai.net/user/subscription {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    // 读取响应内容
                    var jsonResponse = await response.Content.ReadAsStringAsync();

                    // 使用 Newtonsoft.Json 解析 JSON 数据
                    var subscriptionInfo = JsonConvert.DeserializeObject<SubscriptionInfo>(jsonResponse);

                    // 计算并赋值给 SessionManager.Opus
                    SessionManager.Opus = subscriptionInfo.TrainingStepsLeft.FixedTrainingStepsLeft + subscriptionInfo.TrainingStepsLeft.PurchasedTrainingSteps;
                }
                else
                {
                    // 处理请求失败的情况
                    LogPage.LogMessage(LogLevel.ERROR, $"Failed to get subscription info: {response.StatusCode}");
                }
            }
        }

        // 定义 SubscriptionInfo 类来映射 JSON 响应
        public class SubscriptionInfo
        {
            public TrainingStepsLeft TrainingStepsLeft { get; set; }
        }

        public class TrainingStepsLeft
        {
            public int FixedTrainingStepsLeft { get; set; }
            public int PurchasedTrainingSteps { get; set; }
        }
    }
}
