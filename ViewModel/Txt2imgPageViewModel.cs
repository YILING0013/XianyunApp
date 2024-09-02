using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;
using System.Windows.Input;
using xianyun.API;
using xianyun.Common;
using xianyun.Model;

namespace xianyun.ViewModel
{
    public class Txt2imgPageViewModel : NotifyBase
    {
        public Txt2imgPageModel Txt2ImgPageModel { get; set; }
        public ICommand GenerateImageCommand { get; set; }
        private readonly string _secretKey = "fGCGrffh$*hdr#(7904-(cSGDTJGTCLOPIYSWQSFESADZFDBJ%+)):m;(&@1#+$*hHBBB23$c(&#46&(890*@2$%&c#5$#2147905*&/MJMMLLPwr#fhdefts&dcr24x#g4*r@3&(uourw1fcgd-5cdgc$-4fhfxf+dvhvd#d*xe#&frzxhg&efxgthd@2vdffhr*ts2#g4cr#f3xffde@3$ffsxdvh4swa$gr$grOJHNBUGVCDAssddd$ss4+f5$s23xgv(/njvd1d4+g7g$213yfdh*$j*QZDEHg";
        public Txt2imgPageViewModel()
        {
            Txt2ImgPageModel = new Txt2imgPageModel();
            GenerateImageCommand = new AsyncRelayCommand(OnGenerateButtonClick);
        }

        private async Task OnGenerateButtonClick()
        {
            try
            {
                var apiClient = new XianyunApiClient("http://127.0.0.1:5000", SessionManager.Session);
                Console.WriteLine(SessionManager.Session);

                var imageRequest = new ImageGenerationRequest
                {
                    Model = "nai-diffusion-3",
                    PositivePrompt = "1girl, amazing quality, very aesthetic, absurdres",
                    NegativePrompt = "lowres, {bad}, error",
                    Scale = 4.96,
                    Steps = 1,
                    Width = 1088,
                    Height = 960,
                    PromptGuidanceRescale = 0,
                    NoiseSchedule = "native",
                    Seed = "189623948",
                    Sampler = "k_euler",
                    Sm = false,
                    SmDyn = false,
                    PictureId = TotpGenerator.GenerateTotp(_secretKey)
                };

                var (jobId, queuePosition) = await apiClient.GenerateImageAsync(imageRequest);
                Console.WriteLine($"Job submitted with ID: {jobId}, queue position: {queuePosition}");

                string imageBase64 = await apiClient.CheckResultAsync(jobId);
                Console.WriteLine("Image generated successfully!");

                // 这里将 imageBase64 转换为图像显示在UI中
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                // 你可以在这里增加将错误信息显示在UI的代码
            }
        }
    }
}
