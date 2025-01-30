using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using Path = System.IO.Path;

namespace xianyun.MainPages
{
    public partial class RandomGenerated : Page
    {
        private readonly Dictionary<string, Dictionary<string, Dictionary<string, string>>> _jsonData = new();
        private readonly Random _rnd = new Random();

        public RandomGenerated()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            LoadAllJsonFiles();
        }

        private void LoadAllJsonFiles()
        {
            string jsonFolder = Path.Combine(Directory.GetCurrentDirectory(), "json_files");
            foreach (var file in Directory.GetFiles(jsonFolder, "*.json"))
            {
                try
                {
                    var categoryName = Path.GetFileNameWithoutExtension(file);
                    var jsonContent = File.ReadAllText(file);
                    var data = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(jsonContent);
                    _jsonData[categoryName] = data;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"加载文件 {Path.GetFileName(file)} 失败: {ex.Message}");
                }
            }
        }

        private void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Output.Text = ProcessSyntax(Input.Text);
            }
            catch (Exception ex)
            {
                Output.Text = $"生成错误: {ex.Message}";
            }
        }

        private string ProcessSyntax(string input)
        {
            var result = Regex.Replace(input, @"\$\$(.*?)\$\$", match =>
            {
                var innerContent = ProcessTags(match.Groups[1].Value);
                return ShuffleContent(innerContent);
            }, RegexOptions.Singleline);

            result = ProcessTags(result);
            result = Regex.Replace(result, @"\+\""(.*?)\""", m => m.Groups[1].Value);
            return result;
        }

        private string ProcessTags(string input)
        {
            return Regex.Replace(input, @"<([^>]+)>", match =>
            {
                var tagContent = match.Groups[1].Value;
                var paramRegex = new Regex(
                    @"^(?<category>[\p{L}0-9_]+)" +
                    @"(?:-(?<subcategory>[\p{L}0-9_]+))?" +
                    @"(?:\?(?<params>[^=]*))?" +
                    @"(?:=(?<params>.*))?$",
                    RegexOptions.ExplicitCapture);
                var paramMatch = paramRegex.Match(tagContent);

                var category = paramMatch.Groups["category"].Value;
                var subCategory = paramMatch.Groups["subcategory"].Value;
                var parameters = ParseParameters(paramMatch.Groups["params"].Value);

                var selectedItems = SelectItems(category, subCategory, parameters);
                return WrapItems(selectedItems, parameters);
            });
        }

        private Dictionary<string, string> ParseParameters(string paramString)
        {
            var parameters = new Dictionary<string, string>
            {
                ["n"] = "1",
                ["w"] = "0",
                ["d"] = "0",
                ["r"] = "0"
            };

            var parts = paramString.Split(',');
            foreach (var part in parts)
            {
                var kv = part.Split(':');
                if (kv.Length == 2 && parameters.ContainsKey(kv[0]))
                {
                    parameters[kv[0]] = kv[1];
                }
            }
            return parameters;
        }

        private List<string> SelectItems(string category, string subCategory, Dictionary<string, string> parameters)
        {
            var items = new List<string>();

            if (_jsonData.TryGetValue(category, out var subCategories))
            {
                if (string.IsNullOrEmpty(subCategory))
                {
                    foreach (var sc in subCategories.Values)
                    {
                        items.AddRange(sc.Values);
                    }
                }
                else if (subCategories.TryGetValue(subCategory, out var entries))
                {
                    items.AddRange(entries.Values);
                }
            }

            var count = int.Parse(parameters["n"]);
            return items.OrderBy(x => Guid.NewGuid()).Take(count).ToList();
        }

        private string WrapItems(List<string> items, Dictionary<string, string> parameters)
        {
            var wrapped = new List<string>();
            foreach (var item in items)
            {
                var sb = new StringBuilder(item);
                int weightCount = int.Parse(parameters["w"]);
                int downCount = int.Parse(parameters["d"]);
                int rCount = int.Parse(parameters["r"]);

                if (weightCount == 0 && downCount == 0 && rCount > 0)
                {
                    (weightCount, downCount) = CalculateWeights(rCount);
                }

                for (int i = 0; i < weightCount; i++) sb.Insert(0, '{').Append('}');
                for (int i = 0; i < downCount; i++) sb.Insert(0, '[').Append(']');

                wrapped.Add(sb.ToString());
            }
            return string.Join(",", wrapped);
        }

        private (int weight, int down) CalculateWeights(int r)
        {
            int weight = 0, down = 0;
            bool weightMode = false, downMode = false;

            for (int i = 0; i < r; i++)
            {
                if (i == 0)
                {
                    switch (_rnd.Next(3))
                    {
                        case 0:
                            weight++;
                            weightMode = true;
                            break;
                        case 1:
                            down++;
                            downMode = true;
                            break;
                        case 2:
                            return (weight, down);
                    }
                }
                else
                {
                    if (weightMode)
                    {
                        if (_rnd.Next(2) == 0) weight++;
                        else return (weight, down);
                    }
                    else if (downMode)
                    {
                        if (_rnd.Next(2) == 0) down++;
                        else return (weight, down);
                    }
                }
            }
            return (weight, down);
        }

        private string ShuffleContent(string content)
        {
            var segments = content.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            return string.Join(",", segments.OrderBy(x => _rnd.Next()));
        }
    }
}