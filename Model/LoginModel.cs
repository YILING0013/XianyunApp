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
        // 私有字段，用于存储用户名
        private string _userName;

        // 公有属性，访问或设置用户名
        public string UserName
        {
            get => _userName; // 获取 _userName 的值
            set
            {
                _userName = value; // 设置 _userName 的值
                this.DoNotify(); // 触发属性更改通知
            }
        }

        // 私有字段，用于存储密码
        private string _password;

        // 公有属性，访问或设置密码
        public string Password
        {
            get => _password; // 获取 _password 的值
            set
            {
                _password = value; // 设置 _password 的值
                this.DoNotify(); // 触发属性更改通知
            }
        }

        // 私有字段，用于存储验证码
        private string _validationCode;

        // 公有属性，访问或设置验证码
        public string ValidationCode
        {
            get => _validationCode; // 获取 _validationCode 的值
            set
            {
                _validationCode = value; // 设置 _validationCode 的值
                this.DoNotify(); // 触发属性更改通知
            }
        }
    }
}
