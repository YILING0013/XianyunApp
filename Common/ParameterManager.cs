using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using xianyun.Model;
using System.IO;
using System.Diagnostics;

namespace xianyun.Common
{
    public class ParameterManager
    {
        private readonly string _filePath;

        public ParameterManager(string filePath)
        {
            _filePath = filePath;
        }

        // 保存参数到文件
        public void SaveParameters(Txt2imgPageModel parameters)
        {
            Debug.WriteLine("Saving parameters to " + _filePath);
            var json = JsonConvert.SerializeObject(parameters, Formatting.Indented);
            File.WriteAllText(_filePath, json);
            Debug.WriteLine("Parameters saved successfully.");
        }

        // 从文件加载参数
        public Txt2imgPageModel LoadParameters()
        {
            Debug.WriteLine("Loading parameters from " + _filePath);
            if (File.Exists(_filePath))
            {
                var json = File.ReadAllText(_filePath);
                Debug.WriteLine("Parameters loaded successfully.");
                return JsonConvert.DeserializeObject<Txt2imgPageModel>(json);
            }

            Debug.WriteLine("Parameter file does not exist.");
            return null; // 如果没有找到文件，返回null
        }
    }
}
