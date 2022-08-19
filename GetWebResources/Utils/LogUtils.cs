using Serilog;

namespace GetWebResources.Utils
{
    public class LogUtils
    {
        /// <summary>
        /// 初始化Log  (使用Log记录日志)
        /// </summary>
        public static void InitLog()
        {
            Log.Logger = new LoggerConfiguration()
                   .MinimumLevel.Debug()
                   .WriteTo.File("Logs/app-log.txt", rollingInterval: RollingInterval.Day)
                   .CreateLogger();
        }
    }
}