using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

using GetWebResources.Model;

using Newtonsoft.Json;

using Serilog;

namespace GetWebResources.Utils
{
    public class SaveResourcesUtils
    {
        public static List<string> ResourcesUrlList { get; set; } = new List<string>();

        public static List<string> ContainsHostList { get; set; } = new List<string>();

        public static string BasePath { get; set; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "out");

        public static string ProjectName { get; set; } = string.Empty;

        public static bool OpenHostFilterState { get; set; } = false;

        public static string ConfigFileFolderPath { get; set; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config");
        public static string ConfigFileName { get; set; } = "settings.json";
        public static string ConfigFilePath { get; set; } = Path.Combine(ConfigFileFolderPath, ConfigFileName);

        /// <summary>
        /// 初始化相关配置
        /// </summary>
        public static void InitConfig()
        {
            var settingStr = File.ReadAllText(ConfigFilePath);
            var settingJson = JsonConvert.DeserializeObject<ConfigModel>(settingStr);

            if (!string.IsNullOrEmpty(settingJson.BasePath))
            {
                BasePath = settingJson.BasePath;
            }

            ContainsHostList = settingJson.ContainsHostList;
        }

        public static void OpenConfigPath()
        {
            OpenFolderPath(ConfigFileFolderPath);
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

            var resourcesInfo = filterResources(url);
            // 不符合条件则 返回false
            if (resourcesInfo == null)
            {
                return result;
            }

            var fileByteArray = await new HttpClient().GetByteArrayAsync(resourcesInfo.Url);

            var baseFolderPath = Path.Combine(BasePath, ProjectName, resourcesInfo.Host);

            // 判断 扩展名 非空
            if (!string.IsNullOrEmpty(resourcesInfo.Ext))
            {
                baseFolderPath = Path.Combine(baseFolderPath, resourcesInfo.Ext.Replace(".", ""));
            }

            // 创建文件夹
            if (!Directory.Exists(baseFolderPath))
            {
                Directory.CreateDirectory(baseFolderPath);
            }
            // 写入文件
            var fullPath = Path.Combine(baseFolderPath, DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-ffffff"), resourcesInfo.Ext);
            File.WriteAllBytes(fullPath, fileByteArray);

            result = File.Exists(fullPath);

            return result;
        }

        /// <summary>
        /// 保存所有资源
        /// </summary>
        /// <returns>本地存放的目录</returns>
        public static string SaveAllResources()
        {
            Log.Information("开始获取资源: " + SaveResourcesUtils.ProjectName);
            
            InitConfig();

            foreach (var urlItem in ResourcesUrlList)
            {
                _ = SaveResourcesByUrl(urlItem);
            }

            var savedFolderPath = Path.Combine(BasePath, ProjectName);
            Log.Information("资源保存的路径:" + savedFolderPath);

            return savedFolderPath;
        }

        private static Action<int> _onResourcesListCountChange;
        public static void ListenResourcesListCountChange(Action<int> OnResourcesListCountChange)
        {
            _onResourcesListCountChange = OnResourcesListCountChange;
        }

        /// <summary>
        /// 向 ResourcesList 推送数据
        /// </summary>
        /// <param name="urlStr"></param>
        public static void PutUrlToResourcesList(string urlStr)
        {
            if (_onResourcesListCountChange != null)
            {
                // 触发 委托
                _onResourcesListCountChange(ResourcesUrlList.Count);
            }

            ResourcesUrlList.Add(urlStr);
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
