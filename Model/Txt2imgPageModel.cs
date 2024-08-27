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
        private float _guidanceScale = 5.0f;
        private float _guidanceRescale = 0.0f;
        private string _samplingMethod;
        private string _resolution;
        private string _noiseSchedule;

        public List<string> Models { get; set; } = new List<string> { "nai-diffusion-3", "nai-diffusion-furry-3" };
        public List<string> SamplingMethods { get; set; } = new List<string> { "Euler", "Euler Ancestral", "DPM++ 2S Ancestral", "DPM++ SDE", "DPM++ 2M", "DDIM" };
        public List<string> Resolutions { get; set; } = new List<string> { "1024*1024", "1216*832", "832*1216" };
        public List<string> NoiseSchedules { get; set; } = new List<string> { "native", "karras", "exponential", "polyexponential" };

        public Txt2imgPageModel()
        {
            Model = Models[0];
            SamplingMethod = SamplingMethods[0];
            Resolution = Resolutions[0];
            NoiseSchedule = NoiseSchedules[0];
        }
        public int DrawingFrequency
        {
            get => _drawingFrequency;
            set
            {
                _drawingFrequency = value;
                this.DoNotify();
            }
        }
        public int Steps
        {
            get => _steps;
            set
            {
                _steps = value;
                this.DoNotify();
            }
        }
        public long? Seed
        {
            get => _seed;
            set
            {
                _seed = value;
                this.DoNotify();
                System.Diagnostics.Debug.WriteLine("Seed: " + _seed);
            }
        }
        public int Width
        {
            get => _width;
            set
            {
                _width = value;
                this.DoNotify();
            }
        }
        public int Height
        {
            get => _height;
            set
            {
                _height = value;
                this.DoNotify();
            }
        }
        public bool IsConvenientResolution
        {
            get => _isConvenientResolution;
            set
            {
                _isConvenientResolution = value;
                this.DoNotify();
            }
        }
        public bool IsSMEA
        {
            get => _isSMEA;
            set
            {
                _isSMEA = value;
                this.DoNotify();
            }
        }
        public bool IsDYN
        {
            get => _isDYN;
            set
            {
                _isDYN = value;
                this.DoNotify();
            }
        }
        public float GuidanceScale
        {
            get => _guidanceScale;
            set
            {
                _guidanceScale = value;
                this.DoNotify();
            }
        }
        public float GuidanceRescale
        {
            get => _guidanceRescale;
            set
            {
                _guidanceRescale = value;
                this.DoNotify();
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
                    this.DoNotify();
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
                    this.DoNotify();
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
                    this.DoNotify();
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
                    this.DoNotify();
                }
            }
        }
    }
}
