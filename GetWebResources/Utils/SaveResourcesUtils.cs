using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

using GetWebResources.Model;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using Serilog;

namespace GetWebResources.Utils
{
    public class SaveResourcesUtils
    {
        public static string ClassName = nameof(SaveResourcesUtils);
        public static List<string> ResourcesUrlList { get; set; } = new List<string>();
        public static List<string> ResourcesUrlBackList { get; set; } = new List<string>();

        public static List<string> ContainsHostList { get; set; } = new List<string>();

        public static string BasePath { get; set; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "out");

        public static string ProjectName { get; set; } = string.Empty;

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

            // 文件扩展名
            var fileExt = new FileInfo(url).Extension;
            result = new ResourcesModel() { Url = url, Ext = fileExt, Host = host };

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

                var baseFolderPath = Path.Combine(BasePath, ProjectName, resourcesInfo.Host);

                // 判断 扩展名 非空
                if (!string.IsNullOrEmpty(resourcesInfo.Ext))
                {
                    //.js?3330fa9d0a26e10429592adcd844d18a
                    //.html&1
                    //.html#33

                    // 处理 异形的扩展名
                    string ext;
                    foreach (var item in ExcludeKeyWordList)
                    {
                        if (!CheckExt(resourcesInfo.Ext, item, out ext))
                        {
                            resourcesInfo.Ext = ext;
                        }
                    }

                    baseFolderPath = Path.Combine(baseFolderPath, resourcesInfo.Ext.Replace(".", ""));
                }


                // 创建文件夹
                if (!Directory.Exists(baseFolderPath))
                {
                    // 'E:\Work\CSharpProject\GetWebResources\GetWebResources\bin\Debug\net5.0-windows\out\
                    // 萌萌动物连连看,36\hm.baidu.com\js?3330fa9d0a26e10429592adcd844d18a'
                    Directory.CreateDirectory(baseFolderPath);
                }

                var fullPath = Path.Combine(baseFolderPath, DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-ffffff") + resourcesInfo.Ext);

                try
                {
                    // 写入文件
                    File.WriteAllBytes(fullPath, fileByteArray);
                }
                catch (Exception ex)
                {
                    var errorName = $"{ClassName}.{nameof(SaveResourcesByUrl)} WriteAllBytes 写入文件异常 ";
                    Log.Error(ex, errorName);
                    ConsoleUtils.WriteLine(errorName + ex);
                }

                result = File.Exists(fullPath);

                return result;
            }
            catch (Exception ex)
            {
                var errorName = $"{ClassName}.{nameof(SaveResourcesByUrl)} 异常: ";
                Log.Error(ex, errorName);
                ConsoleUtils.WriteLine(errorName + ex);

                return result;
            }
        }

        public static bool CheckExt(string Ext, string keyWord, out string oExt)
        {
            var result = true;

            var index = Ext.IndexOf(keyWord);
            if (index > 0)
            {
                result = false;
                Ext = Ext.Substring(0, index);
            }
            oExt = Ext;
            return result;
        }

        /// <summary>
        /// 保存所有资源
        /// </summary>
        /// <returns>本地存放的目录</returns>
        public static async Task<string> SaveAllResourcesAsync()
        {
            Log.Information("开始获取资源: " + SaveResourcesUtils.ProjectName);

            InitConfig();

            ResourcesUrlBackList.Clear();
            ResourcesUrlBackList.AddRange(ResourcesUrlList);

            foreach (var urlItem in ResourcesUrlBackList)
            {
                await SaveResourcesByUrl(urlItem);
            }

            var savedFolderPath = Path.Combine(BasePath, ProjectName);
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
