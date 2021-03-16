using System;
using System.Collections.Generic;
using System.Text;

using CefSharp;
using CefSharp.Handler;

using GetWebResources.Utils;

namespace GetWebResources.Handle
{
    public class MyRequestHandle : RequestHandler
    {

        // 浏览之前触发.
        protected override bool OnBeforeBrowse(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, bool userGesture, bool isRedirect)
        {
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
