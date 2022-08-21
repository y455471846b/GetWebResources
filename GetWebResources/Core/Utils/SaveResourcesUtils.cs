using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

using GetWebResources.Model;

using Newtonsoft.Json;

using Serilog;

namespace GetWebResources.Core.Utils
{
    public class SaveResourcesUtils
    {
        public static string ClassName = nameof(SaveResourcesUtils);
        public static List<string> ResourcesUrlList { get; set; } = new List<string>();
        public static List<string> ResourcesUrlBackList { get; set; } = new List<string>();

        public static List<string> ContainsHostList { get; set; } = new List<string>();

        public static string BasePath { get; set; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "out");


        public static bool OpenHostFilterState { get; set; } = false;

        private static string _configFileFolderPath { get; set; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config");
        private static string _configFileName { get; set; } = "settings.json";
        private static string _configFilePath { get; set; } = Path.Combine(_configFileFolderPath, _configFileName);

        private static HttpClient _httpClient { get; set; } = new HttpClient();

        public static ObservableCollection<string> HistoryList { get; set; } = new ObservableCollection<string>();
        private static string _historyListConfigPath = Path.Combine(_configFileFolderPath, "history.json");

        public static List<string> ExcludeKeyWordList { get; set; } = new List<string>() { };

        /// <summary>
        /// 初始化相关配置
        /// </summary>
        public static void InitConfig()
        {
            var settingStr = File.ReadAllText(_configFilePath);
            var settingJson = JsonConvert.DeserializeObject<ConfigModel>(settingStr);

            if (!string.IsNullOrEmpty(settingJson.BasePath))
            {
                BasePath = settingJson.BasePath;
            }

            ContainsHostList = settingJson.ContainsHostList;
            ExcludeKeyWordList = settingJson.ExcludeKeyWordList;
        }

        public static void SaveHistoryData()
        {
            var jsonData = JsonConvert.SerializeObject(HistoryList);
            File.WriteAllText(_historyListConfigPath, jsonData);
        }

        public static Action OnHistoryListChanged;

        public static void PushToHistoryList(string data)
        {
            if (!HistoryList.Contains(data))
            {
                HistoryList.Remove(data);
            }

            // 超过最大限制则移除最后一个.
            var maxCount = 10;
            if (HistoryList.Count > maxCount)
            {
                HistoryList.RemoveAt(HistoryList.Count - 1);
            }

            HistoryList.Insert(0, data);

            SaveHistoryData();
            // 调用委托
            OnHistoryListChanged?.Invoke();
        }

        public static void InitHistoryList()
        {
            if (File.Exists(_historyListConfigPath))
            {
                var historyStr = File.ReadAllText(_historyListConfigPath);
                HistoryList = JsonConvert.DeserializeObject<ObservableCollection<string>>(historyStr);
            }
        }

        public static void OpenConfigPath()
        {
            OpenFolderPath(_configFileFolderPath);
        }

        /// <summary>
        /// 使用 [资源管理器] 打开目录
        /// </summary>
        /// <param name="path"></param>
        public static void OpenFolderPath(string path)
        {
            var process = new Process();
            process.StartInfo.FileName = path;
            process.StartInfo.UseShellExecute = true;
            process.Start();
        }

        /// <summary>
        /// 过滤资源
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private static ResourcesModel filterResources(string url)
        {
            ResourcesModel result = null;

            // 域名
            var host = new Uri(url).Host;

            if (string.IsNullOrEmpty(host))
            {
                return null;
            }
            // 筛选域名 (不在列表中,不进行下载)
            if (OpenHostFilterState)
            {
                var isContains = ContainsHostList.Exists(item => item.Contains(host));
                if (!isContains)
                {
                    return null;
                }
            }

            var fileInfo = new FileInfo(url);

            var fileExt = fileInfo.Extension;

            FilterExt(ref fileExt);

            var fileName = fileInfo.Name;

            FilterExt(ref fileName);

            result = new ResourcesModel()
            {
                Url = url,
                Ext = fileExt,
                Host = host,
                Name = fileName
            };

            return result;
        }

        /// <summary>
        /// 保存资源
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static async Task<bool> SaveResourcesByUrl(string url)
        {
            var result = false;

            try
            {
                var resourcesInfo = filterResources(url);
                // 不符合条件则 返回false
                if (resourcesInfo == null)
                {
                    return result;
                }

                //  System.Net.Http.HttpRequestException:
                //  Response status code does not indicate success:
                //  404 (Not Found).

                var fileByteArray = await _httpClient.GetByteArrayAsync(resourcesInfo.Url);

                var baseFolderPath = Path.Combine(
                    BasePath,
                    //ProjectName,
                    resourcesInfo.Host,
                    resourcesInfo.Ext.Replace(".", "") // 使用 扩展名 创建一个文件夹
                    );

                // 创建文件夹
                if (!Directory.Exists(baseFolderPath))
                {
                    // 'E:\Work\CSharpProject\GetWebResources\GetWebResources\bin\Debug\net5.0-windows\out\
                    // 萌萌动物连连看,36\hm.baidu.com\js?3330fa9d0a26e10429592adcd844d18a'
                    Directory.CreateDirectory(baseFolderPath);
                }

                if (string.IsNullOrEmpty(resourcesInfo.Name))
                {
                    // 没有名字的,给一个默认的名字
                    resourcesInfo.Name = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-ffffff");
                }

                // 拼接上 文件名
                var fullPath = Path.Combine(baseFolderPath, resourcesInfo.Name);

                try
                {
                    // 写入文件
                    File.WriteAllBytes(fullPath, fileByteArray);
                }
                catch (Exception ex)
                {
                    var errorName = $"{ClassName}.{nameof(SaveResourcesByUrl)} WriteAllBytes 写入文件异常 ";
                    Log.Error(ex, errorName);
                }

                result = File.Exists(fullPath);

                return result;
            }
            catch (Exception ex)
            {
                var errorName = $"{ClassName}.{nameof(SaveResourcesByUrl)} 异常: ";
                Log.Error(ex, errorName);

                return result;
            }
        }

        /// <summary>
        /// 去除扩展名 多余的部分, 返回一个正常的 名称
        /// </summary>
        /// <param name="name"></param>
        public static void FilterExt(ref string name)
        {
            if (!string.IsNullOrEmpty(name))
            {
                //.js?3330fa9d0a26e10429592adcd844d18a
                //.html&1
                //.html#33

                // abc.js?123412

                // 处理 异形的扩展名
                foreach (var item in ExcludeKeyWordList)
                {
                    if (!CheckExt(name, item, out string ext))
                    {
                        name = ext;
                    }
                }
            }
        }

        /// <summary>
        /// 检查扩展名的格式,是否是正常的 (比如: .js .css   不正常的-> .js#123)
        /// </summary>
        /// <param name="extName">扩展名</param>
        /// <param name="keyWord">要检查的特殊关键字</param>
        /// <param name="oExt">如果不正常,则处理, 并通过 out 返回数据</param>
        /// <returns>true 正常,  false 不正常</returns>
        public static bool CheckExt(string extName, string keyWord, out string oExt)
        {
            var result = true;

            var index = extName.IndexOf(keyWord);
            if (index > 0)
            {
                result = false;
                extName = extName.Substring(0, index);
            }
            oExt = extName;
            return result;
        }

        /// <summary>
        /// 保存所有资源
        /// </summary>
        /// <returns>本地存放的目录</returns>
        public static async Task<string> SaveAllResourcesAsync()
        {
            Log.Information("开始获取资源");

            InitConfig();

            ResourcesUrlBackList.Clear();
            ResourcesUrlBackList.AddRange(ResourcesUrlList);

            var index = 1;
            var len = ResourcesUrlBackList.Count;
            foreach (var urlItem in ResourcesUrlBackList)
            {
                var tip = $"({index} / {len} )开始下载:" + urlItem;
                Log.Information(tip);

                await SaveResourcesByUrl(urlItem);

                index++;
            }

            var savedFolderPath = Path.Combine(BasePath);
            Log.Information("资源保存的路径:" + savedFolderPath);

            return savedFolderPath;
        }

        public static Action<int> OnResourcesListCountChanged;

        /// <summary>
        /// 向 ResourcesList 推送数据
        /// </summary>
        /// <param name="urlStr"></param>
        public static void PutUrlToResourcesList(string urlStr)
        {
            ResourcesUrlList.Add(urlStr);
            // 调用 委托
            OnResourcesListCountChanged?.Invoke(ResourcesUrlList.Count);
        }

        /// <summary>
        /// 清空 ResourcesList
        /// </summary>
        public static void ClearResourcesList()
        {
            ResourcesUrlList.Clear();
        }
    }
}