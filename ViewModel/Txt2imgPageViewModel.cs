using CommunityToolkit.Mvvm.Input;
using HandyControl.Controls;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using xianyun.API;
using xianyun.Common;
using xianyun.Model;
using xianyun.UserControl;

namespace xianyun.ViewModel
{
    public class Txt2imgPageViewModel : NotifyBase
    {
        
        public System.Windows.Controls.ScrollViewer ImgPreviewArea { get; set; }
        public StackPanel ImageStackPanel { get; set; }
        public ImageViewer ImageViewerControl { get; set; }
        public Txt2imgPageModel Txt2ImgPageModel { get; set; }
        public ICommand GenerateImageCommand { get; set; }
        private readonly string _secretKey = "fGCGrffh$*hdr#(7904-(cSGDTJGTCLOPIYSWQSFESADZFDBJ%+)):m;(&@1#+$*hHBBB23$c(&#46&(890*@2$%&c#5$#2147905*&/MJMMLLPwr#fhdefts&dcr24x#g4*r@3&(uourw1fcgd-5cdgc$-4fhfxf+dvhvd#d*xe#&frzxhg&efxgthd@2vdffhr*ts2#g4cr#f3xffde@3$ffsxdvh4swa$gr$grOJHNBUGVCDAssddd$ss4+f5$s23xgv(/njvd1d4+g7g$213yfdh*$j*QZDEHg";
        public Txt2imgPageViewModel()
        {
            GenerateImageCommand = new AsyncRelayCommand(OnGenerateButtonClick);
        }

        private async Task OnGenerateButtonClick()
        {
            try
            {
                var apiClient = new XianyunApiClient("https://nai3.xianyun.cool", SessionManager.Session);
                Console.WriteLine(SessionManager.Session);

                var imageRequest = new ImageGenerationRequest
                {
                    Model = Txt2ImgPageModel.Model, // 绑定模型
                    PositivePrompt = Txt2ImgPageModel.PositivePrompt, // 示例正面提示词
                    NegativePrompt = Txt2ImgPageModel.NegitivePrompt, // 示例负面提示词
                    Scale = Txt2ImgPageModel.GuidanceScale, // 绑定比例
                    Steps = Txt2ImgPageModel.Steps, // 绑定步数
                    Width = Txt2ImgPageModel.Width, // 绑定宽度
                    Height = Txt2ImgPageModel.Height, // 绑定高度
                    PromptGuidanceRescale = Txt2ImgPageModel.GuidanceRescale, // 绑定引导重缩放
                    NoiseSchedule = Txt2ImgPageModel.NoiseSchedule, // 绑定噪声调度
                    Seed = Txt2ImgPageModel.Seed?.ToString() ?? "0", // 绑定种子值
                    Sampler = Txt2ImgPageModel.ActualSamplingMethod, // 绑定采样方法
                    Sm = Txt2ImgPageModel.IsSMEA, // 绑定 SMEA 参数
                    SmDyn = Txt2ImgPageModel.IsDYN, // 绑定 DYN 参数
                    PictureId = TotpGenerator.GenerateTotp(_secretKey)
                };

                var (jobId, queuePosition) = await apiClient.GenerateImageAsync(imageRequest);
                Console.WriteLine($"Job submitted with ID: {jobId}, queue position: {queuePosition}");

                string imageBase64 = await apiClient.CheckResultAsync(jobId);
                Console.WriteLine("Image generated successfully!");

                // 这里将 imageBase64 转换为图像显示在UI中
                var bitmapFrame = ConvertBase64ToBitmapFrame(imageBase64);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    // 创建 ImgPreview 控件
                    var imgPreview = new ImgPreview(imageBase64);
                    imgPreview.ImageClicked += OnImageClicked;

                    // 添加到 ImageStackPanel
                    ImageStackPanel.Children.Add(imgPreview);

                    // 更新 ImageViewer 的图像源
                    ImageViewerControl.ImageSource = bitmapFrame;
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                // 将错误信息显示在UI的代码
            }
        }
        private BitmapImage ConvertBase64ToBitmapImage(string base64String)
        {
            byte[] imageBytes = Convert.FromBase64String(base64String);
            BitmapImage bitmapImage = new BitmapImage();
            using (MemoryStream ms = new MemoryStream(imageBytes))
            {
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = ms;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
            }
            return bitmapImage;
        }
        private void OnImageClicked(object sender, string base64Image)
        {
            // 在点击图片预览时更新 ImageViewer 的图像源
            var bitmapFrame = ConvertBase64ToBitmapFrame(base64Image);
            ImageViewerControl.ImageSource = bitmapFrame;
        }
        private BitmapFrame ConvertBase64ToBitmapFrame(string base64String)
        {
            byte[] imageBytes = Convert.FromBase64String(base64String);
            BitmapImage bitmapImage = new BitmapImage();
            using (MemoryStream ms = new MemoryStream(imageBytes))
            {
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = ms;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
            }
            return BitmapFrame.Create(bitmapImage);
        }

    }
}
