﻿using GetStoreApp.Helpers.Root;
using GetStoreApp.Models.Controls.Store;
using GetStoreApp.Services.Controls.Settings;
using GetStoreApp.Services.Root;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.Marshalling;
using System.Threading;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Data.Xml.Dom;
using Windows.Foundation;
using Windows.Foundation.Diagnostics;
using Windows.Security.Cryptography;
using Windows.System.Threading;
using Windows.Web.Http;
using Windows.Web.Http.Headers;

namespace GetStoreApp.Helpers.Controls.Store
{
    /// <summary>
    /// 查询链接辅助类
    /// </summary>
    public static class QueryLinksHelper
    {
        private static readonly Uri cookieUri = new("https://fe3.delivery.mp.microsoft.com/ClientWebService/client.asmx");
        private static readonly Uri fileListXmlUri = new("https://fe3.delivery.mp.microsoft.com/ClientWebService/client.asmx");
        private static readonly Uri urlUri = new("https://fe3.delivery.mp.microsoft.com/ClientWebService/client.asmx/secured");

        /// <summary>
        /// 解析输入框输入的字符串
        /// </summary>
        public static string ParseRequestContent(string requestContent)
        {
            if (requestContent.Contains('/'))
            {
                requestContent = requestContent.Remove(0, requestContent.LastIndexOf('/') + 1);
            }
            if (requestContent.Contains('?'))
            {
                requestContent = requestContent.Remove(requestContent.IndexOf('?'));
            }
            return requestContent;
        }

        /// <summary>
        /// 获取微软商店服务器储存在用户本地终端上的数据
        /// </summary>
        public static string GetCookie()
        {
            string cookieResult = string.Empty;

            try
            {
                AutoResetEvent autoResetEvent = new(false);

                byte[] cookieByteArray = ResourceService.GetEmbeddedData("Files/Assets/Embed/cookie.xml");
                HttpStringContent httpStringContent = new(CryptographicBuffer.ConvertBinaryToString(BinaryStringEncoding.Utf8, CryptographicBuffer.CreateFromByteArray(cookieByteArray)));
                httpStringContent.TryComputeLength(out ulong length);
                httpStringContent.Headers.Expires = DateTime.Now;
                httpStringContent.Headers.ContentType = new HttpMediaTypeHeaderValue("application/soap+xml");
                httpStringContent.Headers.ContentLength = length;
                httpStringContent.Headers.ContentType.CharSet = "utf-8";

                HttpClient httpClient = new();
                IAsyncOperationWithProgress<HttpResponseMessage, HttpProgress> httpPostProgress = httpClient.PostAsync(cookieUri, httpStringContent);

                // 添加超时设置（半分钟后停止获取）
                ThreadPoolTimer threadPoolTimer = ThreadPoolTimer.CreateTimer((_) => { }, TimeSpan.FromSeconds(30), (_) =>
                {
                    try
                    {
                        if (httpPostProgress is not null && (httpPostProgress.Status is not AsyncStatus.Canceled || httpPostProgress.Status is not AsyncStatus.Completed || httpPostProgress.Status is not AsyncStatus.Error))
                        {
                            httpPostProgress.Cancel();
                        }
                    }
                    catch (Exception e)
                    {
                        LogService.WriteLog(LoggingLevel.Warning, "Cookie request cancel task failed", e);
                    }
                });

                // HTTP POST 请求过程已完成
                httpPostProgress.Completed += async (result, status) =>
                {
                    httpPostProgress = null;
                    try
                    {
                        // 获取 POST 请求已完成
                        if (status is AsyncStatus.Completed)
                        {
                            HttpResponseMessage responseMessage = result.GetResults();

                            // 请求成功
                            if (responseMessage.IsSuccessStatusCode)
                            {
                                Dictionary<string, string> responseDict = new()
                                {
                                    { "Status code", responseMessage.StatusCode.ToString() },
                                    { "Headers", responseMessage.Headers is null ? string.Empty : responseMessage.Headers.ToString().Replace('\r', ' ').Replace('\n', ' ') },
                                    { "Response message:", responseMessage.RequestMessage is null ? string.Empty : responseMessage.RequestMessage.ToString().Replace('\r', ' ').Replace('\n', ' ') }
                                };

                                LogService.WriteLog(LoggingLevel.Information, "Cookie request successfully.", responseDict);

                                string responseString = await responseMessage.Content.ReadAsStringAsync();
                                httpClient.Dispose();
                                responseMessage.Dispose();

                                XmlDocument responseStringDocument = new();
                                responseStringDocument.LoadXml(responseString);

                                XmlNodeList encryptedDataList = responseStringDocument.GetElementsByTagName("EncryptedData");
                                if (encryptedDataList.Count > 0)
                                {
                                    cookieResult = encryptedDataList[0].InnerText;
                                }
                            }
                            else
                            {
                                httpClient.Dispose();
                                responseMessage.Dispose();
                            }
                        }
                        // 获取 POST 请求由于超时而被用户取消
                        else if (status is AsyncStatus.Canceled)
                        {
                            LogService.WriteLog(LoggingLevel.Information, "Cookie request timeout", result.ErrorCode);
                        }
                        // 获取 POST 请求发生错误
                        else if (status is AsyncStatus.Error)
                        {
                            // 捕捉因为网络失去链接获取信息时引发的异常和可能存在的其他异常
                            LogService.WriteLog(LoggingLevel.Information, "Cookie request failed", result.ErrorCode);
                        }
                    }
                    catch (Exception)
                    {
                        LogService.WriteLog(LoggingLevel.Warning, "Cookie request unknown exception", result.ErrorCode);
                    }
                    finally
                    {
                        result.Close();
                        autoResetEvent.Set();
                    }
                };

                autoResetEvent.WaitOne();
                autoResetEvent.Dispose();
                autoResetEvent = null;
            }
            // 其他异常
            catch (Exception e)
            {
                LogService.WriteLog(LoggingLevel.Warning, "Cookie request unknown exception", e);
            }

            return cookieResult;
        }

        /// <summary>
        /// 获取应用信息
        /// </summary>
        /// <param name="productId">应用的产品 ID</param>
        /// <returns>打包应用：有，非打包应用：无</returns>
        public static Tuple<bool, AppInfoModel> GetAppInformation(string productId)
        {
            // 添加超时设置（半分钟后停止获取）

            Tuple<bool, AppInfoModel> appInformationResult = null;
            AppInfoModel appInfoModel = new();

            try
            {
                string categoryIDAPI = string.Format("https://storeedgefd.dsx.mp.microsoft.com/v9.0/products/{0}?market={1}&locale={2}&deviceFamily=Windows.Desktop", productId, StoreRegionService.StoreRegion.CodeTwoLetter, LanguageService.AppLanguage.Key);

                AutoResetEvent autoResetEvent = new(false);

                HttpClient httpClient = new();
                IAsyncOperationWithProgress<HttpResponseMessage, HttpProgress> httpPostProgress = httpClient.GetAsync(new(categoryIDAPI));

                // 添加超时设置（半分钟后停止获取）
                ThreadPoolTimer threadPoolTimer = ThreadPoolTimer.CreateTimer((_) => { }, TimeSpan.FromSeconds(30), (_) =>
                {
                    try
                    {
                        if (httpPostProgress is not null && (httpPostProgress.Status is not AsyncStatus.Canceled || httpPostProgress.Status is not AsyncStatus.Completed || httpPostProgress.Status is not AsyncStatus.Error))
                        {
                            httpPostProgress.Cancel();
                        }
                    }
                    catch (Exception e)
                    {
                        LogService.WriteLog(LoggingLevel.Warning, "App Information request cancel task failed", e);
                    }
                });

                // HTTP POST 请求过程已完成
                httpPostProgress.Completed += async (result, status) =>
                {
                    httpPostProgress = null;
                    try
                    {
                        // 获取 POST 请求已完成
                        if (status is AsyncStatus.Completed)
                        {
                            HttpResponseMessage responseMessage = result.GetResults();

                            // 请求成功
                            if (responseMessage.IsSuccessStatusCode)
                            {
                                Dictionary<string, string> responseDict = new()
                                {
                                    { "Status code", responseMessage.StatusCode.ToString() },
                                    { "Headers", responseMessage.Headers is null ? string.Empty : responseMessage.Headers.ToString().Replace('\r', ' ').Replace('\n', ' ') },
                                    { "Response message:", responseMessage.RequestMessage is null ? string.Empty : responseMessage.RequestMessage.ToString().Replace('\r', ' ').Replace('\n', ' ') }
                                };

                                LogService.WriteLog(LoggingLevel.Information, "App Information request successfully.", responseDict);

                                string responseString = await responseMessage.Content.ReadAsStringAsync();
                                httpClient.Dispose();
                                responseMessage.Dispose();

                                if (JsonObject.TryParse(responseString, out JsonObject responseStringObject))
                                {
                                    JsonObject payLoadObject = responseStringObject.GetNamedValue("Payload").GetObject();

                                    appInfoModel.Name = payLoadObject.GetNamedString("Title");
                                    appInfoModel.Publisher = payLoadObject.GetNamedString("PublisherName");
                                    appInfoModel.Description = payLoadObject.GetNamedString("Description");
                                    appInfoModel.CategoryID = string.Empty;
                                    appInfoModel.ProductID = productId;

                                    JsonArray skusArray = payLoadObject.GetNamedArray("Skus");

                                    if (skusArray.Count > 0)
                                    {
                                        appInfoModel.CategoryID = string.Empty;
                                        JsonObject jsonObject = skusArray[0].GetObject();
                                        if (jsonObject.TryGetValue("FulfillmentData", out IJsonValue jsonValue))
                                        {
                                            string fulfillmentData = jsonValue.GetString();
                                            if (JsonObject.TryParse(fulfillmentData, out JsonObject fulfillmentDataObject))
                                            {
                                                appInfoModel.CategoryID = fulfillmentDataObject.GetNamedString("WuCategoryId");
                                            }
                                        }
                                        appInformationResult = new Tuple<bool, AppInfoModel>(true, appInfoModel);
                                    }
                                }
                            }
                            else
                            {
                                httpClient.Dispose();
                                responseMessage.Dispose();
                            }
                        }
                        // 获取 POST 请求由于超时而被用户取消
                        else if (status is AsyncStatus.Canceled)
                        {
                            LogService.WriteLog(LoggingLevel.Information, "App Information request timeout", result.ErrorCode);
                        }
                        // 获取 POST 请求发生错误
                        else if (status is AsyncStatus.Error)
                        {
                            // 捕捉因为网络失去链接获取信息时引发的异常和可能存在的其他异常
                            LogService.WriteLog(LoggingLevel.Information, "App Information request failed", result.ErrorCode);
                        }
                    }
                    catch (Exception e)
                    {
                        LogService.WriteLog(LoggingLevel.Warning, "App Information request unknown exception", e);
                    }
                    finally
                    {
                        result.Close();
                        autoResetEvent.Set();
                    }
                };

                autoResetEvent.WaitOne();
                autoResetEvent.Dispose();
                autoResetEvent = null;
            }
            // 其他异常
            catch (Exception e)
            {
                LogService.WriteLog(LoggingLevel.Warning, "App Information request unknown exception", e);
            }

            return appInformationResult is null ? new Tuple<bool, AppInfoModel>(false, appInfoModel) : appInformationResult;
        }

        /// <summary>
        /// 获取文件信息字符串
        /// </summary>
        /// <param name="cookie">cookie 数据</param>
        /// <param name="categoryId">category ID</param>
        /// <param name="ring">通道</param>
        /// <returns>文件信息的字符串</returns>
        public static string GetFileListXml(string cookie, string categoryId, string ring)
        {
            string fileListXmlResult = string.Empty;

            try
            {
                AutoResetEvent autoResetEvent = new(false);

                byte[] wubyteArray = ResourceService.GetEmbeddedData("Files/Assets/Embed/wu.xml");
                string fileListXml = CryptographicBuffer.ConvertBinaryToString(BinaryStringEncoding.Utf8, CryptographicBuffer.CreateFromByteArray(wubyteArray)).Replace("{1}", cookie).Replace("{2}", categoryId).Replace("{3}", ring);

                HttpStringContent httpStringContent = new(fileListXml);
                httpStringContent.TryComputeLength(out ulong length);
                httpStringContent.Headers.Expires = DateTime.Now;
                httpStringContent.Headers.ContentType = new HttpMediaTypeHeaderValue("application/soap+xml");
                httpStringContent.Headers.ContentLength = length;
                httpStringContent.Headers.ContentType.CharSet = "utf-8";

                HttpClient httpClient = new();
                IAsyncOperationWithProgress<HttpResponseMessage, HttpProgress> httpPostProgress = httpClient.PostAsync(fileListXmlUri, httpStringContent);

                // 添加超时设置（半分钟后停止获取）
                ThreadPoolTimer threadPoolTimer = ThreadPoolTimer.CreateTimer((_) => { }, TimeSpan.FromSeconds(30), (_) =>
                {
                    try
                    {
                        if (httpPostProgress is not null && (httpPostProgress.Status is not AsyncStatus.Canceled || httpPostProgress.Status is not AsyncStatus.Completed || httpPostProgress.Status is not AsyncStatus.Error))
                        {
                            httpPostProgress.Cancel();
                        }
                    }
                    catch (Exception e)
                    {
                        LogService.WriteLog(LoggingLevel.Information, "FileListXml request task cancel failed", e);
                    }
                });

                // HTTP POST 请求过程已完成
                httpPostProgress.Completed += async (result, status) =>
                {
                    httpPostProgress = null;
                    try
                    {
                        // 获取 POST 请求已完成
                        if (status is AsyncStatus.Completed)
                        {
                            HttpResponseMessage responseMessage = result.GetResults();

                            // 请求成功
                            if (responseMessage.IsSuccessStatusCode)
                            {
                                Dictionary<string, string> responseDict = new()
                                {
                                    { "Status code", responseMessage.StatusCode.ToString() },
                                    { "Headers", responseMessage.Headers is null ? string.Empty : responseMessage.Headers.ToString().Replace('\r', ' ').Replace('\n', ' ') },
                                    { "Response message:", responseMessage.RequestMessage is null ? string.Empty : responseMessage.RequestMessage.ToString().Replace('\r', ' ').Replace('\n', ' ') }
                                };

                                LogService.WriteLog(LoggingLevel.Information, "FileListXml request successfully.", responseDict);

                                string responseString = await responseMessage.Content.ReadAsStringAsync();
                                httpClient.Dispose();
                                responseMessage.Dispose();

                                fileListXmlResult = responseString.Replace("&lt;", "<").Replace("&gt;", ">");
                            }
                            else
                            {
                                httpClient.Dispose();
                                responseMessage.Dispose();
                            }
                        }
                        // 获取 POST 请求由于超时而被用户取消
                        else if (status is AsyncStatus.Canceled)
                        {
                            LogService.WriteLog(LoggingLevel.Information, "FileListXml request timeout", result.ErrorCode);
                        }
                        // 获取 POST 请求发生错误
                        else if (status is AsyncStatus.Error)
                        {
                            // 捕捉因为网络失去链接获取信息时引发的异常和可能存在的其他异常
                            LogService.WriteLog(LoggingLevel.Information, "FileListXml request failed", result.ErrorCode);
                        }
                    }
                    catch (Exception e)
                    {
                        LogService.WriteLog(LoggingLevel.Warning, "FileListXml request unknown exception", e);
                    }
                    finally
                    {
                        result.Close();
                        autoResetEvent.Set();
                    }
                };

                autoResetEvent.WaitOne();
                autoResetEvent.Dispose();
                autoResetEvent = null;
            }
            // 其他异常
            catch (Exception e)
            {
                LogService.WriteLog(LoggingLevel.Warning, "FileListXml request unknown exception", e);
            }

            return fileListXmlResult;
        }

        /// <summary>
        /// 获取商店应用安装包
        /// </summary>
        /// <param name="fileListXml">文件信息的字符串</param>
        /// <param name="ring">通道</param>
        /// <returns>带解析后文件信息的列表</returns>
        public static List<QueryLinksModel> GetAppxPackages(string fileListXml, string ring)
        {
            List<QueryLinksModel> appxPackagesList = [];

            try
            {
                XmlDocument fileListDocument = new();
                fileListDocument.LoadXml(fileListXml);

                Dictionary<string, Tuple<string, string, string>> appxPackagesInfoDict = [];
                XmlNodeList fileList = fileListDocument.GetElementsByTagName("File");

                foreach (IXmlNode fileNode in fileList)
                {
                    if (fileNode.Attributes.GetNamedItem("InstallerSpecificIdentifier") is not null)
                    {
                        string name = fileNode.Attributes.GetNamedItem("InstallerSpecificIdentifier").InnerText;
                        string extension = fileNode.Attributes.GetNamedItem("FileName").InnerText.Remove(0, fileNode.Attributes.GetNamedItem("FileName").InnerText.LastIndexOf('.'));
                        string size = fileNode.Attributes.GetNamedItem("Size").InnerText;
                        string digest = fileNode.Attributes.GetNamedItem("Digest").InnerText;

                        if (!appxPackagesInfoDict.ContainsKey(name))
                        {
                            appxPackagesInfoDict.Add(name, new Tuple<string, string, string>(extension, size, digest));
                        }
                    }
                }

                Lock appxPackagesLock = new();
                XmlNodeList securedFragmentList = fileListDocument.DocumentElement.GetElementsByTagName("SecuredFragment");
                CountdownEvent countdownEvent = new(securedFragmentList.Count);

                foreach (IXmlNode securedFragmentNode in securedFragmentList)
                {
                    IXmlNode securedFragmentCloneNode = securedFragmentNode;
                    Task.Run(() =>
                    {
                        IXmlNode xmlNode = securedFragmentCloneNode.ParentNode.ParentNode;

                        if (xmlNode.GetElementsByName("ApplicabilityRules").GetElementsByName("Metadata").GetElementsByName("AppxPackageMetadata").GetElementsByName("AppxMetadata").Attributes.GetNamedItem("PackageMoniker") is not null)
                        {
                            string name = xmlNode.GetElementsByName("ApplicabilityRules").GetElementsByName("Metadata").GetElementsByName("AppxPackageMetadata").GetElementsByName("AppxMetadata").Attributes.GetNamedItem("PackageMoniker").InnerText;

                            if (appxPackagesInfoDict.TryGetValue(name, out Tuple<string, string, string> value))
                            {
                                string fileName = name + value.Item1;
                                string fileSize = value.Item2;
                                string digest = value.Item3;
                                string revisionNumber = xmlNode.GetElementsByName("UpdateIdentity").Attributes.GetNamedItem("RevisionNumber").InnerText;
                                string updateID = xmlNode.GetElementsByName("UpdateIdentity").Attributes.GetNamedItem("UpdateID").InnerText;
                                string uri = GetAppxUrl(updateID, revisionNumber, ring, digest);
                                string fileSizeString = FileSizeHelper.ConvertFileSizeToString(double.TryParse(fileSize, out double size) ? size : 0);

                                appxPackagesLock.Enter();

                                try
                                {
                                    appxPackagesList.Add(new QueryLinksModel()
                                    {
                                        FileName = fileName,
                                        FileLink = uri,
                                        FileSize = fileSizeString,
                                        IsSelected = false,
                                        IsSelectMode = false
                                    });
                                    countdownEvent.Signal();
                                }
                                catch (Exception e)
                                {
                                    ExceptionAsVoidMarshaller.ConvertToUnmanaged(e);
                                }
                                finally
                                {
                                    appxPackagesLock.Exit();
                                }
                            }
                            else
                            {
                                countdownEvent.Signal();
                            }
                        }
                    });
                }

                countdownEvent.Wait();
                countdownEvent.Dispose();
            }
            catch (Exception e)
            {
                LogService.WriteLog(LoggingLevel.Information, "FileListXml parse failed", e);
            }

            return appxPackagesList;
        }

        /// <summary>
        /// 获取商店应用安装包对应的下载链接
        /// </summary>
        /// <returns>商店应用安装包对应的下载链接</returns>
        private static string GetAppxUrl(string updateID, string revisionNumber, string ring, string digest)
        {
            string urlResult = string.Empty;

            try
            {
                AutoResetEvent autoResetEvent = new(false);

                byte[] urlbyteArray = ResourceService.GetEmbeddedData("Files/Assets/Embed/url.xml");
                string url = CryptographicBuffer.ConvertBinaryToString(BinaryStringEncoding.Utf8, CryptographicBuffer.CreateFromByteArray(ResourceService.GetEmbeddedData("Files/Assets/Embed/url.xml"))).Replace("{1}", updateID).Replace("{2}", revisionNumber).Replace("{3}", ring);

                HttpStringContent httpContent = new(url);
                httpContent.Headers.Expires = DateTime.Now;
                httpContent.Headers.ContentType = new HttpMediaTypeHeaderValue("application/soap+xml");
                httpContent.Headers.ContentLength = Convert.ToUInt64(url.Length);
                httpContent.Headers.ContentType.CharSet = "utf-8";

                HttpClient httpClient = new();
                IAsyncOperationWithProgress<HttpResponseMessage, HttpProgress> httpPostProgress = httpClient.PostAsync(urlUri, httpContent);

                // 添加超时设置（半分钟后停止获取）
                ThreadPoolTimer threadPoolTimer = ThreadPoolTimer.CreateTimer((_) => { }, TimeSpan.FromSeconds(30), (_) =>
                {
                    try
                    {
                        if (httpPostProgress is not null && (httpPostProgress.Status is not AsyncStatus.Canceled || httpPostProgress.Status is not AsyncStatus.Completed || httpPostProgress.Status is not AsyncStatus.Error))
                        {
                            httpPostProgress.Cancel();
                        }
                    }
                    catch (Exception e)
                    {
                        LogService.WriteLog(LoggingLevel.Information, "Appx Url request task cancel failed", e);
                    }
                });

                // HTTP POST 请求过程已完成
                httpPostProgress.Completed += async (result, status) =>
                {
                    httpPostProgress = null;
                    try
                    {
                        // 获取 POST 请求已完成
                        if (status is AsyncStatus.Completed)
                        {
                            HttpResponseMessage responseMessage = result.GetResults();

                            // 请求成功
                            if (responseMessage.IsSuccessStatusCode)
                            {
                                Dictionary<string, string> responseDict = new()
                                {
                                    { "Status code", responseMessage.StatusCode.ToString() },
                                    { "Headers", responseMessage.Headers is null ? string.Empty : responseMessage.Headers.ToString().Replace('\r', ' ').Replace('\n', ' ') },
                                    { "Response message:", responseMessage.RequestMessage is null ? string.Empty : responseMessage.RequestMessage.ToString().Replace('\r', ' ').Replace('\n', ' ') }
                                };

                                LogService.WriteLog(LoggingLevel.Information, "Appx Url request successfully.", responseDict);

                                string responseString = await responseMessage.Content.ReadAsStringAsync();
                                httpClient.Dispose();
                                responseMessage.Dispose();

                                XmlDocument responseStringDocument = new();
                                responseStringDocument.LoadXml(responseString);

                                XmlNodeList fileLocationList = responseStringDocument.DocumentElement.GetElementsByTagName("FileLocation");

                                foreach (IXmlNode fileLocationNode in fileLocationList)
                                {
                                    if (Equals(fileLocationNode.GetElementsByName("FileDigest").InnerText, digest))
                                    {
                                        urlResult = fileLocationNode.GetElementsByName("Url").InnerText;
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                httpClient.Dispose();
                                responseMessage.Dispose();
                            }
                        }
                        // 获取 POST 请求由于超时而被用户取消
                        else if (status is AsyncStatus.Canceled)
                        {
                            LogService.WriteLog(LoggingLevel.Information, "Appx Url request timeout", result.ErrorCode);
                        }
                        // 获取 POST 请求发生错误
                        else if (status is AsyncStatus.Error)
                        {
                            // 捕捉因为网络失去链接获取信息时引发的异常和可能存在的其他异常
                            LogService.WriteLog(LoggingLevel.Information, "Appx Url request failed", result.ErrorCode);
                        }
                    }
                    catch (Exception e)
                    {
                        LogService.WriteLog(LoggingLevel.Warning, "Appx Url request unknown exception", e);
                    }
                    finally
                    {
                        result.Close();
                        autoResetEvent.Set();
                    }
                };

                autoResetEvent.WaitOne();
                autoResetEvent.Dispose();
                autoResetEvent = null;
            }
            // 其他异常
            catch (Exception e)
            {
                LogService.WriteLog(LoggingLevel.Warning, "Appx Url request unknown exception", e);
            }

            return urlResult;
        }

        /// <summary>
        /// 获取非商店应用的安装包
        /// </summary>
        /// <param name="productId">产品 ID</param>
        /// <returns>带解析后文件信息的列表</returns>
        public static List<QueryLinksModel> GetNonAppxPackages(string productId)
        {
            List<QueryLinksModel> nonAppxPackagesList = [];

            try
            {
                string url = string.Format("https://storeedgefd.dsx.mp.microsoft.com/v9.0/packageManifests/{0}?market={1}", productId, StoreRegionService.StoreRegion.CodeTwoLetter);

                AutoResetEvent autoResetEvent = new(false);

                HttpClient httpClient = new();
                IAsyncOperationWithProgress<HttpResponseMessage, HttpProgress> httpPostProgress = httpClient.GetAsync(new(url));

                // 添加超时设置（半分钟后停止获取）
                ThreadPoolTimer threadPoolTimer = ThreadPoolTimer.CreateTimer((_) => { }, TimeSpan.FromSeconds(30), (_) =>
                {
                    try
                    {
                        if (httpPostProgress is not null && (httpPostProgress.Status is not AsyncStatus.Canceled || httpPostProgress.Status is not AsyncStatus.Completed || httpPostProgress.Status is not AsyncStatus.Error))
                        {
                            httpPostProgress.Cancel();
                        }
                    }
                    catch (Exception e)
                    {
                        LogService.WriteLog(LoggingLevel.Information, "Non Appx Url request task cancel failed", e);
                    }
                });

                // HTTP GET 请求过程已完成
                httpPostProgress.Completed += async (result, status) =>
                {
                    httpPostProgress = null;
                    try
                    {
                        // 获取 GET 请求已完成
                        if (status is AsyncStatus.Completed)
                        {
                            HttpResponseMessage responseMessage = result.GetResults();

                            // 请求成功
                            if (responseMessage.IsSuccessStatusCode)
                            {
                                Dictionary<string, string> responseDict = new()
                                {
                                    { "Status code", responseMessage.StatusCode.ToString() },
                                    { "Headers", responseMessage.Headers is null ? string.Empty : responseMessage.Headers.ToString().Replace('\r', ' ').Replace('\n', ' ') },
                                    { "Response message:", responseMessage.RequestMessage is null ? string.Empty : responseMessage.RequestMessage.ToString().Replace('\r', ' ').Replace('\n', ' ') }
                                };

                                LogService.WriteLog(LoggingLevel.Information, "Non Appx Url request successfully.", responseDict);

                                string responseString = await responseMessage.Content.ReadAsStringAsync();
                                httpClient.Dispose();
                                responseMessage.Dispose();

                                if (JsonObject.TryParse(responseString, out JsonObject responseStringObject))
                                {
                                    JsonObject dataObject = responseStringObject.GetNamedValue("Data").GetObject();
                                    JsonObject versionsObject = dataObject.GetNamedValue("Versions").GetArray()[0].GetObject();
                                    JsonArray installersArray = versionsObject.GetNamedValue("Installers").GetArray();

                                    Lock nonAppxPackagesLock = new();
                                    CountdownEvent countdownEvent = new(installersArray.Count);

                                    foreach (IJsonValue installer in installersArray)
                                    {
                                        JsonObject installerObject = installer.GetObject();

                                        string installerType = installerObject.GetNamedString("InstallerType");
                                        string installerUrl = installerObject.GetNamedString("InstallerUrl");
                                        string fileSize = GetNonAppxPackageFileSize(installerUrl);
                                        string fileSizeString = FileSizeHelper.ConvertFileSizeToString(double.TryParse(fileSize, out double size) ? size : 0);

                                        if (string.IsNullOrEmpty(installerType) || installerUrl.ToLower().EndsWith(".exe") || installerUrl.ToLower().EndsWith(".msi"))
                                        {
                                            nonAppxPackagesLock.Enter();

                                            try
                                            {
                                                nonAppxPackagesList.Add(new QueryLinksModel()
                                                {
                                                    FileName = installerUrl.Remove(installerUrl.LastIndexOf('.')).Remove(0, installerUrl.LastIndexOf('/') + 1),
                                                    FileLink = installerUrl,
                                                    FileSize = fileSizeString,
                                                    IsSelected = false,
                                                    IsSelectMode = false,
                                                });
                                                countdownEvent.Signal();
                                            }
                                            catch (Exception e)
                                            {
                                                ExceptionAsVoidMarshaller.ConvertToUnmanaged(e);
                                            }
                                            finally
                                            {
                                                nonAppxPackagesLock.Exit();
                                            }
                                        }
                                        else
                                        {
                                            string name = installerUrl.Split('/')[^1];

                                            nonAppxPackagesLock.Enter();

                                            try
                                            {
                                                nonAppxPackagesList.Add(new QueryLinksModel()
                                                {
                                                    FileName = string.Format("{0} ({1}).{2}", name, installerObject.GetNamedString("InstallerLocale"), installerType),
                                                    FileLink = installerUrl,
                                                    FileSize = fileSizeString,
                                                    IsSelected = false,
                                                    IsSelectMode = false,
                                                });
                                                countdownEvent.Signal();
                                            }
                                            catch (Exception e)
                                            {
                                                ExceptionAsVoidMarshaller.ConvertToUnmanaged(e);
                                            }
                                            finally
                                            {
                                                nonAppxPackagesLock.Exit();
                                            }
                                        }
                                    }

                                    countdownEvent.Wait();
                                    countdownEvent.Dispose();
                                }
                            }
                            else
                            {
                                httpClient.Dispose();
                                responseMessage.Dispose();
                            }
                        }
                        // 获取 GET 请求由于超时而被用户取消
                        else if (status is AsyncStatus.Canceled)
                        {
                            LogService.WriteLog(LoggingLevel.Information, "Non Appx Url request timeout", result.ErrorCode);
                        }
                        // 获取 GET 请求发生错误
                        else if (status is AsyncStatus.Error)
                        {
                            // 捕捉因为网络失去链接获取信息时引发的异常和可能存在的其他异常
                            LogService.WriteLog(LoggingLevel.Information, "Non Appx Url request failed", result.ErrorCode);
                        }
                    }
                    catch (Exception e)
                    {
                        LogService.WriteLog(LoggingLevel.Warning, "Non Appx Url request unknown exception", e);
                    }
                    finally
                    {
                        result.Close();
                        autoResetEvent.Set();
                    }
                };

                autoResetEvent.WaitOne();
                autoResetEvent.Dispose();
                autoResetEvent = null;
            }
            // 其他异常
            catch (Exception e)
            {
                LogService.WriteLog(LoggingLevel.Warning, "Non Appx Url request unknown exception", e);
            }

            return nonAppxPackagesList;
        }

        /// <summary>
        /// 获取非商店应用下载文件的大小
        /// </summary>
        private static string GetNonAppxPackageFileSize(string url)
        {
            string fileSizeResult = "0";

            try
            {
                AutoResetEvent autoResetEvent = new(false);

                HttpClient httpClient = new();
                HttpRequestMessage requestMessage = new(HttpMethod.Head, new(url));
                IAsyncOperationWithProgress<HttpResponseMessage, HttpProgress> httpPostProgress = httpClient.SendRequestAsync(requestMessage);

                // 添加超时设置（半分钟后停止获取）
                ThreadPoolTimer threadPoolTimer = ThreadPoolTimer.CreateTimer((_) => { }, TimeSpan.FromSeconds(30), (_) =>
                {
                    try
                    {
                        if (httpPostProgress is not null && (httpPostProgress.Status is not AsyncStatus.Canceled || httpPostProgress.Status is not AsyncStatus.Completed || httpPostProgress.Status is not AsyncStatus.Error))
                        {
                            httpPostProgress.Cancel();
                        }
                    }
                    catch (Exception e)
                    {
                        LogService.WriteLog(LoggingLevel.Information, "FileListXml request task cancel failed", e);
                    }
                });

                // HTTP POST 请求过程已完成
                httpPostProgress.Completed += (result, status) =>
                {
                    httpPostProgress = null;
                    try
                    {
                        // 获取 POST 请求已完成
                        if (status is AsyncStatus.Completed)
                        {
                            HttpResponseMessage responseMessage = result.GetResults();

                            // 请求成功
                            if (responseMessage.IsSuccessStatusCode)
                            {
                                Dictionary<string, string> responseDict = new()
                                {
                                    { "Status code", responseMessage.StatusCode.ToString() },
                                    { "Headers", responseMessage.Headers is null ? string.Empty : responseMessage.Headers.ToString().Replace('\r', ' ').Replace('\n', ' ') },
                                    { "Response message:", responseMessage.RequestMessage is null ? string.Empty : responseMessage.RequestMessage.ToString().Replace('\r', ' ').Replace('\n', ' ') }
                                };

                                LogService.WriteLog(LoggingLevel.Information, "Non appx package file size request successfully.", responseDict);

                                fileSizeResult = Convert.ToString(responseMessage.Content.Headers.ContentLength);
                                httpClient.Dispose();
                                responseMessage.Dispose();
                            }
                            else
                            {
                                httpClient.Dispose();
                                responseMessage.Dispose();
                            }
                        }
                        // 获取 POST 请求由于超时而被用户取消
                        else if (status is AsyncStatus.Canceled)
                        {
                            LogService.WriteLog(LoggingLevel.Information, "Non appx package file size request timeout", result.ErrorCode);
                        }
                        // 获取 POST 请求发生错误
                        else if (status is AsyncStatus.Error)
                        {
                            // 捕捉因为网络失去链接获取信息时引发的异常和可能存在的其他异常
                            LogService.WriteLog(LoggingLevel.Information, "Non appx package file size request failed", result.ErrorCode);
                        }
                    }
                    catch (Exception e)
                    {
                        LogService.WriteLog(LoggingLevel.Warning, "Non appx package file size request unknown exception", e);
                    }
                    finally
                    {
                        result.Close();
                        autoResetEvent.Set();
                    }
                };

                autoResetEvent.WaitOne();
                autoResetEvent.Dispose();
                autoResetEvent = null;
            }
            // 其他异常
            catch (Exception e)
            {
                LogService.WriteLog(LoggingLevel.Warning, "Non appx package file size request unknown exception", e);
            }

            return fileSizeResult;
        }

        private static IXmlNode GetElementsByName(this IXmlNode xmlNode, string name)
        {
            foreach (IXmlNode node in xmlNode.ChildNodes)
            {
                if (Equals(node.NodeName, name))
                {
                    return node;
                }
            }

            return null;
        }
    }
}
