using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace xianyun.API
{
    public class CharacterPrompt
    {
        [JsonProperty("prompt")]
        public string Prompt { get; set; }

        [JsonProperty("uc")]
        public string Uc { get; set; }

        [JsonProperty("center")]
        public Center Center { get; set; }
    }

    public class CharCaption
    {
        [JsonProperty("char_caption")]
        public string CharCaptionText { get; set; }

        [JsonProperty("centers")]
        public List<Center> Centers { get; set; }
    }

    public class Center
    {
        [JsonProperty("x")]
        public double X { get; set; }

        [JsonProperty("y")]
        public double Y { get; set; }
    }
}
