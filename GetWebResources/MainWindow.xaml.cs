using System;
using System.Collections.Generic;
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

namespace GetWebResources
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // 重写 浏览器请求处理程序
            Web.RequestHandler = new MyRequestHandle();

            // 显示console
            ConsoleUtils.Show();

            SaveResourcesUtils.ListenResourcesListCountChange((num) =>
            {
                Dispatcher.Invoke(() =>
                {
                    LableResourcesCount.Content = num;
                });
            });
        }

        private void BtnLoad_Click(object sender, RoutedEventArgs e)
        {
            SaveResourcesUtils.ClearResourcesList();
            var url = TextBoxWebUrl.Text;
            Web.Address = url;
        }

        private void BtnGetResources_Click(object sender, RoutedEventArgs e)
        {

            var title = Web?.Title ?? "";
            

            Task.Run(() =>
            {
                SetTip("⏱️ 正在获取资源,请稍等..");
                if (string.IsNullOrEmpty(title))
                {
                    title = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-ffffff");
                }

                if (title.Length > 10)
                {
                    title = title.Substring(0, 10);
                }

                // 初始化 必要参数
                SaveResourcesUtils.BasePath = @"E:\Desktop\img4399_new";
                SaveResourcesUtils.ProjectName = title;

                // 保存所有资源
                var path = SaveResourcesUtils.SaveAllResources();

                
                if (Directory.Exists(path))
                {
                    // 如果成功,则用 资源管理器 打开文件夹
                    var process = new Process();
                    process.StartInfo.FileName = path;
                    process.StartInfo.UseShellExecute = true;
                    process.Start();
                }

                SetTip("✔️ 获取资源完成..");
                MessageBox.Show("✔️ 获取资源完成..");
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
    }
}
