using HandyControl.Controls;       // 引入 HandyControl 库，用于控件和窗口处理
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;         // 引入 ICommand 接口
using xianyun.Common;
using xianyun.Model;
using Newtonsoft.Json;  // 用于序列化和反序列化 JSON
using CommunityToolkit.Mvvm.Input;

namespace xianyun.ViewModel
{
    // 这是用于主窗口的 ViewModel，目前是一个空类，可以在未来扩展
    public class MainWindowViewModel
    {

    }

    // 这是用于登录窗口的 ViewModel，包含登录数据和关闭窗口的命令
    public class LoginViewModel : NotifyBase
    {
        // 定义一个关闭窗口的命令
        public ICommand CloseWindowCommand { get; }

        public ICommand LoginCommand { get; }

        private string _Message;

        public string Message
        {
            get { return _Message; }
            set { _Message = value; this.DoNotify(); }
        }

        // 定义登录数据模型的属性
        public LoginModel LoginModel { get; set; } = new LoginModel();

        // LoginViewModel 的构造函数，初始化登录模型和关闭窗口的命令
        public LoginViewModel()
        {
            // 初始化 CloseWindowCommand 命令，使用 RelayCommand<System.Windows.Window>，允许关闭传入的窗口
            CloseWindowCommand = new RelayCommand<System.Windows.Window>(async window =>
            {
                if (window != null)
                {
                    // 异步任务关闭窗口
                    await Task.Run(() =>
                    {
                        // 确保在 UI 线程上关闭窗口
                        window.Dispatcher.Invoke(() => window.Close());
                    });
                }
            });
            LoginCommand = new RelayCommand(Login);
        }
        private async void Login()
        {
            this.Message = string.Empty;

            // 检查用户名和密码是否为空
            if (string.IsNullOrEmpty(LoginModel.UserName) || string.IsNullOrEmpty(LoginModel.Password))
            {
                this.Message = "用户名和密码不能为空";
                return;
            }

            // 使用 HttpClientHandler 处理 cookies
            var handler = new HttpClientHandler();
            handler.CookieContainer = new System.Net.CookieContainer();

            // 创建一个 HttpClient 实例
            using (var client = new HttpClient(handler))
            {
                try
                {
                    // 设置请求的 URL
                    string url = "http://localhost:5000/auth/login";

                    // 创建请求数据
                    var loginData = new
                    {
                        username = LoginModel.UserName,
                        password = LoginModel.Password
                    };

                    // 序列化请求数据为 JSON
                    string jsonData = JsonConvert.SerializeObject(loginData);
                    var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                    // 发送 POST 请求
                    HttpResponseMessage response = await client.PostAsync(url, content);

                    // 确保请求成功
                    if (response.IsSuccessStatusCode)
                    {
                        // 读取响应内容
                        string result = await response.Content.ReadAsStringAsync();
                        var responseData = JsonConvert.DeserializeObject<dynamic>(result);

                        if (responseData.success == true)
                        {
                            // 获取并显示 Session 信息
                            var cookies = handler.CookieContainer.GetCookies(new Uri(url));
                            var sessionCookie = cookies["session"];
                            if (sessionCookie != null)
                            {
                                this.Message=$"Session: {sessionCookie.Value}";
                            }

                            // 重定向到 main 页面或其他逻辑
                            string redirectUrl = responseData.redirect.ToString();
                            // 你可以在这里处理重定向
                        }
                        else
                        {
                            // 登录失败，显示错误消息
                            this.Message = responseData.message.ToString();
                        }
                    }
                    else
                    {
                        // 处理服务器返回的非成功状态码
                        this.Message = "服务器错误，请稍后再试。";
                    }
                }
                catch (Exception ex)
                {
                    // 处理请求异常
                    this.Message = $"登录失败：{ex.Message}";
                }
            }
        }
    }
}
