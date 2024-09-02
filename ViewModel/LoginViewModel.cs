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
using WpfWindow = System.Windows.Window;
using xianyun.View;

namespace xianyun.ViewModel
{
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
        private void OpenMainWindowAndCloseLoginWindow()
        {
            var currentWindow = System.Windows.Application.Current.Windows.OfType<WpfWindow>().SingleOrDefault(w => w.IsActive);

            if (currentWindow != null)
            {
                MainWindow mainWindow = new MainWindow();
                System.Windows.Application.Current.MainWindow = mainWindow; // 设置新的主窗口
                mainWindow.Show();
                currentWindow.Close();
            }
        }
        private async void Login()
        {
            this.Message = string.Empty;
            LoginModel.IsLoginButtonEnabled = false;

            if (string.IsNullOrEmpty(LoginModel.AccessToken))
            {
                // 如果 AccessToken 为空，执行原来的登录逻辑
                if (string.IsNullOrEmpty(LoginModel.UserName) || string.IsNullOrEmpty(LoginModel.Password))
                {
                    this.Message = "用户名和密码不能为空";
                    LoginModel.IsLoginButtonEnabled = true;
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
                        string url = "http://127.0.0.1:5000/auth/login";
                        var loginData = new
                        {
                            username = LoginModel.UserName,
                            password = LoginModel.Password
                        };

                        string jsonData = JsonConvert.SerializeObject(loginData);
                        var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                        HttpResponseMessage response = await client.PostAsync(url, content);

                        if (response.IsSuccessStatusCode)
                        {
                            string result = await response.Content.ReadAsStringAsync();
                            var responseData = JsonConvert.DeserializeObject<dynamic>(result);

                            if (responseData.success == true)
                            {
                                var cookies = handler.CookieContainer.GetCookies(new Uri(url));
                                var sessionCookie = cookies["session"];
                                if (sessionCookie != null)
                                {
                                    Common.SessionManager.Session = sessionCookie.Value;
                                    this.Message = "登录成功";
                                    OpenMainWindowAndCloseLoginWindow();  // 登录成功后打开主窗口
                                }
                                LoginModel.IsLoginButtonEnabled = true;
                            }
                            else
                            {
                                this.Message = responseData.message.ToString();
                                LoginModel.IsLoginButtonEnabled = true;
                            }
                        }
                        else
                        {
                            this.Message = "服务器错误，请稍后再试。";
                            LoginModel.IsLoginButtonEnabled = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        this.Message = $"登录失败：{ex.Message}";
                        LoginModel.IsLoginButtonEnabled = true;
                    }
                }
            }
            else
            {
                // 如果 AccessToken 不为空，执行 API 请求
                using (var client = new HttpClient())
                {
                    try
                    {
                        string url = "https://api.novelai.net/user/data";

                        // 检查并补全 Bearer 头
                        string token = LoginModel.AccessToken;
                        if (!token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                        {
                            token = "Bearer " + token;
                        }

                        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Substring("Bearer ".Length));

                        HttpResponseMessage response = await client.GetAsync(url);

                        if (response.IsSuccessStatusCode)
                        {
                            string result = await response.Content.ReadAsStringAsync();
                            var responseData = JsonConvert.DeserializeObject<dynamic>(result);

                            // 处理响应
                            if (responseData.information.banStatus == "not_banned" && responseData.information.banMessage == null)
                            {
                                Common.SessionManager.Token = token;
                                int fixedTrainingStepsLeft = responseData.subscription.trainingStepsLeft.fixedTrainingStepsLeft;
                                bool active = responseData.subscription.active;
                                this.Message = $"Training Steps Left: {fixedTrainingStepsLeft}, Active: {active}";
                                OpenMainWindowAndCloseLoginWindow();  // 登录成功后打开主窗口
                                LoginModel.IsLoginButtonEnabled = true;
                            }
                            else if (responseData.information.banStatus == "banned")
                            {
                                this.Message = $"Account banned: {responseData.information.banMessage}";
                                LoginModel.IsLoginButtonEnabled = true;
                            }
                        }
                        else
                        {
                            this.Message = "无法获取用户数据，请检查 AccessToken。";
                            LoginModel.IsLoginButtonEnabled = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        this.Message = $"请求失败：{ex.Message}";
                        LoginModel.IsLoginButtonEnabled = true;
                    }
                }
            }
        }
    }
}
