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
        /// <summary>
        /// 保存用户名和密码到配置文件
        /// </summary>
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

        /// <summary>
        /// 从配置文件中加载用户名和密码
        /// </summary>
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
                // 检查用户名是否为邮箱格式
                if (IsEmailFormat(UserName))
                {
                    // 账号为邮箱格式，使用 novelai 登录逻辑
                    await LoginWithEmail(UserName, Password);
                }
                else
                {
                    // 普通用户名和密码登录
                    await LoginWithUsernamePassword(UserName, Password);
                }
            }
            else
            {
                // 如果 AccessToken 不为空，执行 API 请求
                await GetUserDataWithToken(AccessToken);
            }
        }

        /// <summary>
        /// 判断是否为邮件格式账号
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private bool IsEmailFormat(string input)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(input);
                return addr.Address == input;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 使用邮箱和密码登录
        /// </summary>
        /// <param name="email"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        private async Task LoginWithEmail(string email, string password)
        {
            try
            {
                string key = Common.LoginHelper.GetAccessKey(email,password);

                using (var client = new HttpClient())
                {
                    string url = "https://api.novelai.net/user/login";
                    var loginData = new { key = key };
                    string jsonData = JsonConvert.SerializeObject(loginData);
                    var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await client.PostAsync(url, content);

                    if (response.IsSuccessStatusCode)
                    {
                        string result = await response.Content.ReadAsStringAsync();
                        var responseData = JsonConvert.DeserializeObject<dynamic>(result);

                        if (responseData.accessToken != null)
                        {
                            AccessToken = responseData.accessToken.ToString();
                            await GetUserDataWithToken(AccessToken);
                            SaveLoginInfo();
                        }
                        else
                        {
                            this.Message = "获取 AccessToken 失败";
                        }
                    }
                    else
                    {
                        this.Message = "邮箱登录失败，请检查输入的邮箱和密码";
                    }
                }
            }
            catch (Exception ex)
            {
                this.Message = $"登录失败：{ex.Message}";
            }
            IsLoginButtonEnabled = true;
        }

        /// <summary>
        /// 使用idlecloud的用户名和密码登录
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        private async Task LoginWithUsernamePassword(string userName, string password)
        {
            if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password))
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
                    string url = "https://nocaptchauri.idlecloud.cc/api/login_no_captcha";
                    var loginData = new
                    {
                        username = userName,
                        password = password
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
                                SaveLoginInfo();
                                OpenMainWindowAndCloseLoginWindow();  // 登录成功后打开主窗口
                            }
                        }
                        else
                        {
                            this.Message = responseData.message.ToString();
                        }
                    }
                    else
                    {
                        this.Message = "服务器错误，请稍后再试。";
                    }
                }
                catch (Exception ex)
                {
                    this.Message = $"登录失败：{ex.Message}";
                }
                IsLoginButtonEnabled = true;
            }
        }

        // 使用 AccessToken 获取用户数据
        private async Task GetUserDataWithToken(string token)
        {
            using (var client = new HttpClient())
            {
                try
                {
                    string url = "https://api.novelai.net/user/data";

                    // 检查并补全 Bearer 头
                    string finalToken = token;
                    if (!finalToken.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    {
                        finalToken = "Bearer " + finalToken;
                    }

                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", finalToken.Substring("Bearer ".Length));

                    HttpResponseMessage response = await client.GetAsync(url);

                    if (response.IsSuccessStatusCode)
                    {
                        string result = await response.Content.ReadAsStringAsync();
                        var responseData = JsonConvert.DeserializeObject<dynamic>(result);

                        if (responseData.information.banStatus == "not_banned" && responseData.information.banMessage == null)
                        {
                            Common.SessionManager.Token = finalToken;
                            int fixedTrainingStepsLeft = responseData.subscription.trainingStepsLeft.fixedTrainingStepsLeft;
                            int purchasedTrainingSteps = responseData.subscription.trainingStepsLeft.purchasedTrainingSteps;
                            bool active = responseData.subscription.active;
                            Common.SessionManager.Opus = fixedTrainingStepsLeft + purchasedTrainingSteps;
                            this.Message = $"Training Steps Left: {fixedTrainingStepsLeft}, Active: {active}";
                            OpenMainWindowAndCloseLoginWindow();  // 登录成功后打开主窗口
                        }
                        else if (responseData.information.banStatus == "banned")
                        {
                            this.Message = $"Account banned: {responseData.information.banMessage}";
                        }
                    }
                    else
                    {
                        this.Message = "无法获取用户数据，请检查 AccessToken。";
                    }
                }
                catch (Exception ex)
                {
                    this.Message = $"请求失败：{ex.Message}";
                }
                IsLoginButtonEnabled = true;
            }
        }
    }
}
