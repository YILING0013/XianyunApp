using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using xianyun.Common;

namespace xianyun.Model 
{
    // LoginModel 类继承自 NotifyBase，代表登录模型
    public class LoginModel : NotifyBase
    {
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
    }
}
