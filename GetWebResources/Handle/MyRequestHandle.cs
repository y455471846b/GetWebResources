using CefSharp;
using CefSharp.Handler;

using GetWebResources.Utils;

using Serilog;

namespace GetWebResources.Handle
{
    public class MyRequestHandle : RequestHandler
    {
        // 浏览之前触发.
        protected override bool OnBeforeBrowse(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, bool userGesture, bool isRedirect)
        {
            Log.Information("请求的地址: " + request.Url);
            return base.OnBeforeBrowse(chromiumWebBrowser, browser, frame, request, userGesture, isRedirect);
        }

        // 请求资源时触发
        protected override IResourceRequestHandler GetResourceRequestHandler(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, bool isNavigation, bool isDownload, string requestInitiator, ref bool disableDefaultHandling)
        {
            // 记录 ResourcesUrl
            SaveResourcesUtils.PutUrlToResourcesList(request.Url);

            return base.GetResourceRequestHandler(chromiumWebBrowser, browser, frame, request, isNavigation, isDownload, requestInitiator, ref disableDefaultHandling);
        }
    }
}