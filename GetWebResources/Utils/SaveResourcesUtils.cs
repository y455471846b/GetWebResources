using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using GetWebResources.Model;

namespace GetWebResources.Utils
{
    public class SaveResourcesUtils
    {
        public static List<string> ResourcesUrlList = new List<string>();

        public static List<string> ContainsHostList = new List<string>{
            "sda.4399.com"
            };

        public static string BasePath = string.Empty;

        public static string ProjectName = string.Empty;

        public static bool OpenHostFilterState = false;

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

            var baseFolderPath = $@"{BasePath}\{ProjectName}\{resourcesInfo.Host}";

            // 判断 扩展名 非空
            if (!string.IsNullOrEmpty(resourcesInfo.Ext))
            {
                baseFolderPath += $@"\{resourcesInfo.Ext.Replace(".", "")}";
            }

            // 创建文件夹
            if (!Directory.Exists(baseFolderPath))
            {
                Directory.CreateDirectory(baseFolderPath);
            }
            // 写入文件
            var fullPath = $@"{ baseFolderPath}\{ DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-ffffff")}{ resourcesInfo.Ext}";
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
            foreach (var urlItem in ResourcesUrlList)
            {
                _ = SaveResourcesByUrl(urlItem);
            }
            return BasePath + "\\" + ProjectName;

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
