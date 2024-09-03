using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using xianyun.Common;

namespace xianyun.Model
{
    public class Txt2imgPageModel : NotifyBase
    {
        private string _model;
        private int _drawingFrequency = 1;
        private int _steps = 28;
        private long? _seed=null;
        private int _width = 1024;
        private int _height = 1024;
        private bool _isConvenientResolution = false;
        private bool _isSMEA = false;
        private bool _isDYN = false;
        private bool _isDYNEnabled = false;
        private float _guidanceScale = 5.0f;
        private float _guidanceRescale = 0.0f;
        private string _samplingMethod;
        private string _resolution;
        private string _noiseSchedule;
        private bool _selectedLineArt;
        private bool _selectedSketch;
        private bool _selectedDeclutter;
        private bool _selectedEmotion;
        private bool _selectedColorize;
        private string _negitivePrompt;

        private readonly Dictionary<string, string> _samplingMethodMapping = new Dictionary<string, string>
        {
            { "Euler", "k_euler" },
            { "Euler Ancestral", "k_euler_ancestral" },
            { "DPM++ 2S Ancestral", "k_dpmpp_2s_ancestral" },
            { "DPM++ SDE", "k_dpmpp_sde" },
            { "DPM++ 2M", "k_dpmpp_2m" },
            { "DDIM", "ddim_v3" }
        };
        // 反向映射字典
        private readonly Dictionary<string, string> _reverseSamplingMethodMapping;

        public List<string> Models { get; set; } = new List<string> { "nai-diffusion-3", "nai-diffusion-furry-3" };
        public List<string> SamplingMethods { get; set; } = new List<string> { "Euler", "Euler Ancestral", "DPM++ 2S Ancestral", "DPM++ SDE", "DPM++ 2M", "DDIM" };
        public List<string> Resolutions { get; set; } = new List<string> { "1024*1024", "1216*832", "832*1216" };
        public List<string> NoiseSchedules { get; set; } = new List<string> { "native", "karras", "exponential", "polyexponential" };

        public Txt2imgPageModel()
        {
            // 初始化反向映射字典
            _reverseSamplingMethodMapping = _samplingMethodMapping.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);
            Model = Models[0];
            SamplingMethod = SamplingMethods[0];
            Resolution = Resolutions[0];
            NoiseSchedule = NoiseSchedules[0];
        }
        // 获取实际的采样方法
        public string ActualSamplingMethod => _samplingMethodMapping.TryGetValue(SamplingMethod, out var actualMethod) ? actualMethod : null;

        // 设置实际的采样方法，并更新 UI 显示选项
        public void SetSamplingMethodFromActual(string actualMethod)
        {
            if (_reverseSamplingMethodMapping.TryGetValue(actualMethod, out var uiMethod))
            {
                SamplingMethod = uiMethod;
            }
        }
        public string NegitivePrompt
        {
            get => _negitivePrompt;
            set
            {
                if (_negitivePrompt != value)
                {
                    _negitivePrompt = value;
                    DoNotify();
                }
            }
        }
        public bool SelectedLineArt
        {
            get => _selectedLineArt;
            set
            {
                if (_selectedLineArt != value || value == false)
                {
                    _selectedLineArt = value;
                    DoNotify();
                }
                else { _selectedLineArt = !value; DoNotify(); }
            }
        }
        public bool SelectedSketch
        {
            get => _selectedSketch;
            set
            {
                if (_selectedSketch != value || value == false)
                {
                    _selectedSketch = value;
                    DoNotify();
                }else { _selectedSketch = !value; DoNotify(); }
            }
        }
        public bool SelectedDeclutter
        {
            get => _selectedDeclutter;
            set
            {
                if (_selectedDeclutter != value || value == false)
                {
                    _selectedDeclutter = value;
                    DoNotify();
                }
                else { _selectedDeclutter = !value; DoNotify(); }
            }
        }
        public bool SelectedEmotion
        {
            get => _selectedEmotion;
            set
            {
                if (_selectedEmotion != value || value == false)
                {
                    _selectedEmotion = value;
                    DoNotify();
                }
                else { _selectedEmotion = !value; DoNotify(); }
            }
        }
        public bool SelectedColorize
        {
            get => _selectedColorize;
            set
            {
                if (_selectedColorize != value || value == false)
                {
                    _selectedColorize = value;
                    DoNotify();
                }
                else { _selectedColorize = !value; DoNotify(); }
            }
        }
        public int DrawingFrequency
        {
            get => _drawingFrequency;
            set
            {
                _drawingFrequency = value;
                DoNotify();
            }
        }
        public int Steps
        {
            get => _steps;
            set
            {
                _steps = value;
                DoNotify();
            }
        }
        public long? Seed
        {
            get => _seed;
            set
            {
                _seed = value;
                DoNotify();
                System.Diagnostics.Debug.WriteLine("Seed: " + _seed);
            }
        }
        public int Width
        {
            get => _width;
            set
            {
                _width = value;
                DoNotify();
            }
        }
        public int Height
        {
            get => _height;
            set
            {
                _height = value;
                DoNotify();
            }
        }
        public bool IsConvenientResolution
        {
            get => _isConvenientResolution;
            set
            {
                _isConvenientResolution = value;
                DoNotify();
            }
        }
        public bool IsSMEA
        {
            get => _isSMEA;
            set
            {
                _isSMEA = value;
                IsDYNEnabled = _isSMEA;
                if (!_isSMEA)
                {
                    IsDYN = false;
                }
                DoNotify();
            }
        }
        public bool IsDYNEnabled
        {
            get => _isDYNEnabled;
            set
            {
                _isDYNEnabled = value;
                DoNotify();
            }
        }
        public bool IsDYN
        {
            get => _isDYN;
            set
            {
                _isDYN = value;
                DoNotify();
            }
        }
        public float GuidanceScale
        {
            get => _guidanceScale;
            set
            {
                _guidanceScale = (float)Math.Round(value, 2); // 保留两位小数
                DoNotify();
            }
        }
        public float GuidanceRescale
        {
            get => _guidanceRescale;
            set
            {
                _guidanceRescale = (float)Math.Round(value, 2); // 保留两位小数
                DoNotify();
            }
        }
        public string Model
        {
            get => _model;
            set
            {
                if (_model != value)
                {
                    _model = value;
                    DoNotify();
                }
            }
        }

        public string SamplingMethod
        {
            get => _samplingMethod;
            set
            {
                if (_samplingMethod != value)
                {
                    _samplingMethod = value;
                    DoNotify();
                }
            }
        }

        public string Resolution
        {
            get => _resolution;
            set
            {
                if (_resolution != value)
                {
                    _resolution = value;
                    DoNotify();
                }
            }
        }

        public string NoiseSchedule
        {
            get => _noiseSchedule;
            set
            {
                if (_noiseSchedule != value)
                {
                    _noiseSchedule = value;
                    DoNotify();
                }
            }
        }
    }
}
