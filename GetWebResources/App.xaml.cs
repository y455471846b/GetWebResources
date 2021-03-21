using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

using GetWebResources.Utils;

using Serilog;
using System.Windows.Threading;

namespace GetWebResources
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        protected override void OnStartup(StartupEventArgs e)
        {
            ConsoleUtils.Show();
            LogUtils.InitLog();

            base.OnStartup(e);

            // 捕获 全局异常
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            var ex = e.Exception;
            Log.Error(ex, "App_DispatcherUnhandledException 发生异常:");
            ConsoleUtils.WriteLine("CurrentDomain_UnhandledException 发生异常:" + ex);
            e.Handled = true;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;
            Log.Error(ex, "App 发生异常:");
            ConsoleUtils.WriteLine("App 发生异常:" + ex);
        }
    }
}
