using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

using GetWebResources.Utils;

using Microsoft.Web.WebView2.Core;

using Serilog;

// WebView2 官方中文文档
// https://docs.microsoft.com/zh-cn/microsoft-edge/webview2/concepts/overview-features-apis?tabs=dotnetcsharp

namespace GetWebResources
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            ComboBoxHistory.SelectionChanged += ComboBoxHistory_SelectionChanged;

            // 资源List 数量改变回调
            SaveResourcesUtils.OnResourcesListCountChanged += (int num) =>
            {
                Dispatcher.Invoke(() =>
                {
                    LableResourcesCount.Content = num;
                });
            };

            SaveResourcesUtils.OnHistoryListChanged += () =>
            {
                ComboBoxHistory.ItemsSource = SaveResourcesUtils.HistoryList;
            };

            SaveResourcesUtils.InitHistoryList();
            ComboBoxHistory.ItemsSource = SaveResourcesUtils.HistoryList;

            Web.CoreWebView2InitializationCompleted += Web_CoreWebView2InitializationCompleted;
            //Web.ConsoleMessage += Web_ConsoleMessage;

            //Web.LoadingStateChanged += Web_LoadingStateChanged;

        }              

        /// <summary>
        /// WebView2 core 初始化完成 (可以获取到 CoreWebView2 对象)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Web_CoreWebView2InitializationCompleted(object sender, CoreWebView2InitializationCompletedEventArgs e)
        {
            Web.CoreWebView2.SourceChanged += CoreWebView2_SourceChanged;

            Web.CoreWebView2.WebResourceResponseReceived += CoreWebView2_WebResourceResponseReceived;
            Web.CoreWebView2.DOMContentLoaded += CoreWebView2_DOMContentLoaded;          

        }

        private void CoreWebView2_DOMContentLoaded(object sender, CoreWebView2DOMContentLoadedEventArgs e)
        {
            
            Log.Information("dom 加载完毕");
        }

        /// <summary>
        /// 监听资源的 请求与响应
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CoreWebView2_WebResourceResponseReceived(object sender, CoreWebView2WebResourceResponseReceivedEventArgs e)
        {
            Log.Information($"资源地址: {e.Request.Uri}");
            SaveResourcesUtils.PutUrlToResourcesList(e.Request.Uri);
        }

        private void CoreWebView2_SourceChanged(object sender, CoreWebView2SourceChangedEventArgs e)
        {
            TextBoxWebUrl.Text = Web.Source.ToString();
        }             
              
        private void ComboBoxHistory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Web.CoreWebView2.Navigate(ComboBoxHistory.SelectedValue.ToString());
        }

        private void BtnLoad_Click(object sender, RoutedEventArgs e)
        {
            SaveResourcesUtils.ClearResourcesList();
            var url = TextBoxWebUrl.Text;

            if (Web.Source.ToString() == url)
            {
                Web.Reload();
            }
            else
            {
                Web.CoreWebView2.Navigate(url);
                SaveResourcesUtils.PushToHistoryList(url);
            }
        }

        private void BtnGetResources_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(async () =>
            {
                try
                {
                    Log.Information("⏱️ 正在获取资源,请稍等..");

                    // 保存所有资源
                    var path = await SaveResourcesUtils.SaveAllResourcesAsync();

                    if (Directory.Exists(path))
                    {
                        // 如果成功,则用 资源管理器 打开文件夹
                        SaveResourcesUtils.OpenFolderPath(path);
                    }

                    Log.Information("✔️ 获取资源完成..");
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "获取资源异常:");
                    Log.Information("发生异常,请到Logs目录中查看详细信息");
                    Log.Information("获取资源异常: " + ex);

                    //打开log目录
                    var basePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
                    SaveResourcesUtils.OpenFolderPath(basePath);
                }
            });
        }

        private void BtnCheckCore_Click(object sender, RoutedEventArgs e)
        {
            Web.CoreWebView2.Navigate("https://ie.icoa.cn/");
        }

        private void CheckBoxOpenHostFilter_Checked(object sender, RoutedEventArgs e)
        {
            SaveResourcesUtils.OpenHostFilterState = true;
        }

        private void CheckBoxOpenHostFilter_Unchecked(object sender, RoutedEventArgs e)
        {
            SaveResourcesUtils.OpenHostFilterState = false;
        }

        private void BtnOpenConfig_Click(object sender, RoutedEventArgs e)
        {
            SaveResourcesUtils.OpenConfigPath();
        }
    }
}