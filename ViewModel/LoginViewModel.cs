using HandyControl.Controls;       // 引入 HandyControl 库，用于控件和窗口处理
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;         // 引入 ICommand 接口
using xianyun.Common;
using Newtonsoft.Json;  // 用于序列化和反序列化 JSON
using CommunityToolkit.Mvvm.Input;
using WpfWindow = System.Windows.Window;
using xianyun.View;
using System.Linq;
using IniParser.Model;
using IniParser;

namespace xianyun.ViewModel
{
    // 这是用于登录窗口的 ViewModel，包含登录数据和关闭窗口的命令
    public class LoginViewModel : NotifyBase
    {
        private const string ConfigFilePath = "account.ini";
        private const string Section = "LoginInfo";
        private const string UserNameKey = "UserName";
        private const string PasswordKey = "Password";
        // 定义一个关闭窗口的命令
        public ICommand CloseWindowCommand { get; }
        public ICommand LoginCommand { get; }

        private string _Message;
        public string Message
        {
            get { return _Message; }
            set { _Message = value; this.DoNotify(); }
        }

        // 登录模型中的字段移至 ViewModel 中作为属性
        private string _userName;
        public string UserName
        {
            get => _userName;
            set
            {
                _userName = value;
                this.DoNotify(); // 触发属性更改通知
            }
        }

        private string _password;
        public string Password
        {
            get => _password;
            set
            {
                _password = value;
                this.DoNotify(); // 触发属性更改通知
            }
        }

        private string _accessToken;
        public string AccessToken
        {
            get => _accessToken;
            set
            {
                _accessToken = value;
                this.DoNotify(); // 触发属性更改通知
            }
        }

        private bool _isLoginButtonEnabled = true;
        public bool IsLoginButtonEnabled
        {
            get => _isLoginButtonEnabled;
            set
            {
                _isLoginButtonEnabled = value;
                DoNotify();  // 通知UI更新
            }
        }

        // LoginViewModel 的构造函数，初始化登录模型和关闭窗口的命令
        public LoginViewModel()
        {
            // 尝试从配置文件中加载用户名和密码
            LoadLoginInfo();
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
        // 保存用户名和密码到 ini 文件
        private void SaveLoginInfo()
        {
            var parser = new FileIniDataParser();
            IniData data = new IniData();

            // 如果配置文件已经存在，加载旧数据
            if (File.Exists(ConfigFilePath))
            {
                data = parser.ReadFile(ConfigFilePath);
            }

            // 更新用户名和密码
            data[Section][UserNameKey] = UserName;
            data[Section][PasswordKey] = Password;

            // 保存到配置文件
            parser.WriteFile(ConfigFilePath, data);
        }

        // 从 ini 文件加载用户名和密码
        private void LoadLoginInfo()
        {
            if (File.Exists(ConfigFilePath))
            {
                var parser = new FileIniDataParser();
                IniData data = parser.ReadFile(ConfigFilePath);

                // 从配置文件中读取用户名和密码
                if (data.Sections.ContainsSection(Section))
                {
                    UserName = data[Section][UserNameKey];
                    Password = data[Section][PasswordKey];
                }
            }
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
            IsLoginButtonEnabled = false;

            if (string.IsNullOrEmpty(AccessToken))
            {
                // 如果 AccessToken 为空，执行原来的登录逻辑
                if (string.IsNullOrEmpty(UserName) || string.IsNullOrEmpty(Password))
                {
                    this.Message = "用户名和密码不能为空";
                    IsLoginButtonEnabled = true;
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
                        string url = "https://nai3.idlecloud.cc/auth/login";
                        var loginData = new
                        {
                            username = UserName,
                            password = Password
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
                                    // 保存登录信息
                                    SaveLoginInfo();
                                    OpenMainWindowAndCloseLoginWindow();  // 登录成功后打开主窗口
                                }
                                IsLoginButtonEnabled = true;
                            }
                            else
                            {
                                this.Message = responseData.message.ToString();
                                IsLoginButtonEnabled = true;
                            }
                        }
                        else
                        {
                            this.Message = "服务器错误，请稍后再试。";
                            IsLoginButtonEnabled = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        this.Message = $"登录失败：{ex.Message}";
                        IsLoginButtonEnabled = true;
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
                        string token = AccessToken;
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
                                IsLoginButtonEnabled = true;
                            }
                            else if (responseData.information.banStatus == "banned")
                            {
                                this.Message = $"Account banned: {responseData.information.banMessage}";
                                IsLoginButtonEnabled = true;
                            }
                        }
                        else
                        {
                            this.Message = "无法获取用户数据，请检查 AccessToken。";
                            IsLoginButtonEnabled = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        this.Message = $"请求失败：{ex.Message}";
                        IsLoginButtonEnabled = true;
                    }
                }
            }
        }
    }
}
