using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Aspenlaub.Net.GitHub.CSharp.Cargobay.Entities;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Entities;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Extensions;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;
using Resources = Aspenlaub.Net.GitHub.CSharp.Cargobay.Properties.Resources;

namespace Aspenlaub.Net.GitHub.CSharp.Cargobay.Jobz {
    public class CargoHelper {
        public const string Sha1 = "c0363ff90d9e6e9cd905eb9c9cf84b20fbe9b636";
        public const string Clue = "Cargobay encryption";

        private readonly IFolderResolver vFolderResolver;

        public CargoHelper(IFolderResolver folderResolver) {
            vFolderResolver = folderResolver;
        }

        public static string CheckFolder(string folder, bool test) {
            if (folder[^1] != '\\') {
                return string.Format(Resources.FolderDoesNotEndWithBackslash, folder);
            }
            if (folder.Contains(@"\\")) {
                return string.Format(Resources.FolderEndsWithTwoBackslashes, folder);
            }
            if (test && !folder.Contains("Samples")) {
                return string.Format(Resources.FolderOutsideSampleArea, folder);
            }

            if (Directory.Exists(folder)) {
                return string.Empty;
            }

            Directory.CreateDirectory(folder);
            return string.Empty;
        }

        public static string CombineFolders(string folder1, string folder2) {
            if (folder1.Length == 0 || folder2.Contains(@":\")) {
                return folder2;
            }
            if (folder2.Length == 0) {
                return folder1;
            }

            return folder1 + '\\' + folder2;
        }

        public static DirectoryInfo DirInfo(string folder, out string error) {
            error = CheckFolder(folder, false);
            return new DirectoryInfo(folder);
        }

        public List<string> Downloadable(string url, string wildcard, IErrorsAndInfos errorsAndInfos) {
            var fileNames = new List<string>();
            if (url.Substring(0, 20) != "ftp://ftp.localhost/") {
                return fileNames;
            }

            var wampFolder = vFolderResolver.Resolve(@"$(GitHub)\Cargobay\src\Samples\FileSystem\Traveller\Wamp", errorsAndInfos).FullName + "\\"
                             + url.Remove(0, 20).Replace('/', '\\');
            var dirInfo = DirInfo(wampFolder, out var error);
            if (error.Length != 0) {
                return fileNames;
            }

            fileNames.AddRange(dirInfo.GetFiles(wildcard).Select(fileInfo => fileInfo.Name));

            return fileNames;
        }

        protected static NetworkCredential LookUpCredentials(string ur, Dictionary<string, Login> accessCodes, out string error) {
            error = "";
            Site(ur, out var site, out var validUr);
            if (!validUr) { return new NetworkCredential(); }
            if (accessCodes.ContainsKey(site) && accessCodes[site].Identification.Length != 0) {
                return new NetworkCredential(accessCodes[site].Identification, accessCodes[site].Password);
            }

            error = string.Format(Resources.NoAccessTo, site);
            return new NetworkCredential();
        }

        public static void Site(string ur, out string site, out bool validUr) {
            site = "";
            validUr = true;
            var pos = ur.IndexOf("://", StringComparison.Ordinal);
            if (pos < 0) {
                validUr = false;
            } else {
                var pos2 = ur.Remove(0, pos + 3).IndexOf('/');
                if (pos2 < 0) {
                    validUr = false;
                } else {
                    site = ur.Substring(0, pos + 3 + pos2);
                }
            }
        }

        public static bool CreateWebRequest(string uri, string method, Dictionary<string, Login> accessCodes, out string error, out FtpWebRequest request) {
            request = null;
            if (uri.Substring(0, 10) != "ftp://ftp.") {
                error = Resources.InvalidUri;
                return false;
            }

            request = (FtpWebRequest)WebRequest.Create(new Uri(uri));
            request.UseBinary = true;
            request.Method = method;
            request.Credentials = LookUpCredentials(uri, accessCodes, out error);
            if (error.Length != 0) { return false; }

            if (method != WebRequestMethods.Ftp.UploadFile) {
                return true;
            }

            request.Timeout = int.MaxValue;
            request.ReadWriteTimeout = int.MaxValue;
            return true;
        }

        public bool Download(string uri, string localFileFullName, bool checkOnly, Dictionary<string, Login> accessCodes, out string error, out bool couldConnect) {
            const int bufferSize = 2048;
            var buffer = new byte[bufferSize + 256];

            error = string.Empty;
            couldConnect = false;
            if (uri.Substring(0, 20) == "ftp://ftp.localhost/") {
                couldConnect = true;
                var errorsAndInfos = new ErrorsAndInfos();
                var wampFile = vFolderResolver.Resolve(@"$(GitHub)\Cargobay\src\Samples\FileSystem\Traveller\Wamp", errorsAndInfos).FullName + "\\"
                    + uri.Remove(0, 20).Replace('/', '\\');
                if (errorsAndInfos.AnyErrors()) {
                    error = errorsAndInfos.ErrorsToString();
                    return false;
                }
                if (checkOnly) {
                    return File.Exists(wampFile);
                }

                File.Copy(wampFile, localFileFullName);
                return true;
            }

            if (!CreateWebRequest(uri, WebRequestMethods.Ftp.DownloadFile, accessCodes, out error, out var request)) {
                return false;
            }

            try {
                var response = (FtpWebResponse)request.GetResponse();
                couldConnect = response.IsMutuallyAuthenticated;
                var ftpStream = response.GetResponseStream();
                if (ftpStream == null) {
                    error = Resources.CouldNotCreateFileStream;
                    return false;
                }
                var outputStream = new FileStream(localFileFullName, FileMode.Create);
                var readCount = ftpStream.Read(buffer, 0, bufferSize);
                if (!checkOnly) {
                    while (readCount > 0) {
                        outputStream.Write(buffer, 0, readCount);
                        readCount = ftpStream.Read(buffer, 0, bufferSize);
                    }
                }

                ftpStream.Close();
                outputStream.Close();
                response.Close();
            } catch (WebException e) {
                error = e.Message;
                return false;
            }

            return true;
        }

        public bool Upload(string uri, string localFileFullName, Dictionary<string, Login> accessCodes, out string error) {
            FtpWebResponse response;

            error = string.Empty;
            if (uri.Substring(0, 20) == "ftp://ftp.localhost/") {
                var errorsAndInfos = new ErrorsAndInfos();
                var wampFile = vFolderResolver.Resolve(@"$(GitHub)\Cargobay\src\Samples\FileSystem\Traveller\Wamp", errorsAndInfos).FullName + "\\"
                                  + uri.Remove(0, 20).Replace('/', '\\');
                if (errorsAndInfos.AnyErrors()) {
                    error = errorsAndInfos.ErrorsToString();
                    return false;
                }
                File.Copy(localFileFullName, wampFile);
                return true;
            }

            if (!CreateWebRequest(uri, WebRequestMethods.Ftp.UploadFile, accessCodes, out error, out var request)) {
                return false;
            }

            request.UsePassive = true;
            request.KeepAlive = false;
            try {
                var fileStream = new FileStream(localFileFullName, FileMode.Open, FileAccess.Read);
                var requestStream = request.GetRequestStream();
                var buffer = new byte[8092];
                int read;
                while ((read = fileStream.Read(buffer, 0, buffer.Length)) != 0) {
                    requestStream.Write(buffer, 0, read);
                }

                requestStream.Flush();
                requestStream.Close();
                response = (FtpWebResponse)request.GetResponse();
            } catch (Exception e) {
                error = e.ToString();
                return false;
            }

            var status = response.StatusDescription;
            response.Close();
            request.Abort();
            if (status.Substring(0, 3) == "226" && !status.Contains("Transfer aborted")) { return true; }
            if (!CreateWebRequest(uri, WebRequestMethods.Ftp.DeleteFile, accessCodes, out error, out request)) {
                return false;
            }

            response = (FtpWebResponse)request.GetResponse();
            response.Close();
            return false;
        }

        public bool FileExists(string uri, Dictionary<string, Login> accessCodes, out bool couldConnect, out string error) {
            error = "";
            if (uri.Substring(0, 20) == "ftp://ftp.localhost/") {
                var errorsAndInfos = new ErrorsAndInfos();
                couldConnect = true;
                var wampFile = vFolderResolver.Resolve(@"$(GitHub)\Cargobay\src\Samples\FileSystem\Traveller\Wamp", errorsAndInfos).FullName + "\\"
                    + uri.Remove(0, 20).Replace('/', '\\');
                if (!errorsAndInfos.AnyErrors()) {
                    return File.Exists(wampFile);
                }

                error = errorsAndInfos.ErrorsToString();
                return false;
            }

            couldConnect = false;
            var pos = uri.LastIndexOf('/');
            if (pos < 0) {
                return false;
            }

            var directory = uri.Substring(0, pos + 1);
            var fileName = uri.Remove(0, pos + 1);
            if (!CreateWebRequest(directory, WebRequestMethods.Ftp.ListDirectory, accessCodes, out _, out var request)) {
                return false;
            }

            try {
                var response = (FtpWebResponse)request.GetResponse();
                var ftpStream = response.GetResponseStream();
                if (ftpStream == null) {
                    return false;
                }
                var ftpStreamReader = new StreamReader(ftpStream);
                bool found;
                do {
                    found = fileName == ftpStreamReader.ReadLine();
                } while (!found && !ftpStreamReader.EndOfStream);

                ftpStream.Close();
                response.Close();
                couldConnect = true;
                return found;
            } catch {
                return false;
            }

        }

        public bool CanUpload(string uri, Dictionary<string, Login> accessCodes, out string error) {
            if (FileExists(uri, accessCodes, out var couldConnect, out error)) {
                return false;
            }
            if (couldConnect) { return true; }

            error = string.Format(Resources.NoAccessTo, uri);
            return false;
        }
    }
}

