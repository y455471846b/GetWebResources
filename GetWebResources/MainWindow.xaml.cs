using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using CefSharp;
using CefSharp.DevTools.Network;
using CefSharp.Handler;
using CefSharp.Wpf;

using GetWebResources.Handle;
using GetWebResources.Model;
using GetWebResources.Utils;

using Serilog;

namespace GetWebResources
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
                      

            // 重写 浏览器请求处理程序
            Web.RequestHandler = new MyRequestHandle();

            LogUtils.InitLog();

            // 显示console
            ConsoleUtils.Show();
            InitEnableOfButton();
            InitEvent();

            InitDataSource();
        }

        private void InitEnableOfButton()
        {

        }
        private void InitDataSource() 
        {
            SaveResourcesUtils.InitHistoryList();
            ComboBoxHistory.ItemsSource = SaveResourcesUtils.HistoryList;
        }


        private void InitEvent()
        {
            // 地址改变事件
            Web.AddressChanged += Web_AddressChanged;

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
        }

        private void ComboBoxHistory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            Web.Address = ComboBoxHistory.SelectedValue.ToString();
        }

        private void Web_AddressChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            TextBoxWebUrl.Text = Web.Address;
        }

        private void BtnLoad_Click(object sender, RoutedEventArgs e)
        {
              
            SaveResourcesUtils.ClearResourcesList();
            var url = TextBoxWebUrl.Text;

            if (Web.Address == url)
            {
                Web.Reload();
            }
            else
            {
                Web.Address = url;
                SaveResourcesUtils.PushToHistoryList(url);
            }

        }

        private void BtnGetResources_Click(object sender, RoutedEventArgs e)
        {
            var title = Web?.Title ?? "";
            Task.Run(async () =>
            {                 
                try
                {
                    MessageBox.Show("⏱️ 开始获取资源,请稍等...");
                    SetTip("⏱️ 正在获取资源,请稍等..");

                    if (string.IsNullOrEmpty(title))
                    {
                        title = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-ffffff");
                    }
                    else if (title.Length > 10)
                    {
                        title = title.Substring(0, 10);
                    }

                    SaveResourcesUtils.ProjectName = title;

                    // 保存所有资源
                    var path = await SaveResourcesUtils.SaveAllResourcesAsync();

                    if (Directory.Exists(path))
                    {
                        // 如果成功,则用 资源管理器 打开文件夹
                        SaveResourcesUtils.OpenFolderPath(path);
                    }

                    SetTip("✔️ 获取资源完成..");
                    MessageBox.Show("✔️ 获取资源完成..");
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "获取资源异常:");
                    SetTip("发生异常,请到Logs目录中查看详细信息");
                    ConsoleUtils.WriteLine("获取资源异常: " + ex);
                }
            });

        }

        private void SetTip(string data)
        {
            Dispatcher.Invoke(() =>
               {
                   LableTip.Content = data;
               });
        }

        private void BtnCheckCore_Click(object sender, RoutedEventArgs e)
        {
            Web.Address = "https://ie.icoa.cn/";
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
