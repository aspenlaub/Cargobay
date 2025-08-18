using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Cargobay.Entities;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Entities;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Extensions;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;
using Renci.SshNet;
using Resources = Aspenlaub.Net.GitHub.CSharp.Cargobay.Properties.Resources;

#pragma warning disable SYSLIB0014

namespace Aspenlaub.Net.GitHub.CSharp.Cargobay.Jobz;

public class CargoHelper(IFolderResolver folderResolver) {
    public const string Sha1 = "c0363ff90d9e6e9cd905eb9c9cf84b20fbe9b636";
    public const string Clue = "Cargobay encryption";

    public static string CheckFolder(string folder, bool test, bool createIfMissing) {
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

        if (!createIfMissing) {
            return string.Format(Resources.FolderDoesNotExist, folder);
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
        error = CheckFolder(folder, false, false);
        return string.IsNullOrEmpty(error) ? new DirectoryInfo(folder) : null;
    }

    public async Task<List<string>> DownloadableAsync(string url, string wildcard, IErrorsAndInfos errorsAndInfos) {
        var fileNames = new List<string>();
        if (url.Substring(0, 20) != "ftp://ftp.localhost/") {
            return fileNames;
        }

        string wampFolder = (await folderResolver.ResolveAsync(@"$(GitHub)\Cargobay\src\Samples\FileSystem\Traveller\Wamp", errorsAndInfos)).FullName + "\\"
            + url.Remove(0, 20).Replace('/', '\\');
        DirectoryInfo dirInfo = DirInfo(wampFolder, out string error);
        if (!string.IsNullOrEmpty(error)) {
            return fileNames;
        }

        fileNames.AddRange(dirInfo.GetFiles(wildcard).Select(fileInfo => fileInfo.Name));

        return fileNames;
    }

    protected static NetworkCredential LookUpCredentials(string url, Dictionary<string, Login> accessCodes, out string error) {
        error = "";
        SiteAndUserId(url, out string site, out string _, out bool validUrl);
        if (!validUrl) { return new NetworkCredential(); }
        if (accessCodes.ContainsKey(site) && accessCodes[site].Identification.Length != 0) {
            return new NetworkCredential(accessCodes[site].Identification, accessCodes[site].Password);
        }

        error = string.Format(Resources.NoAccessTo, site);
        return new NetworkCredential();
    }

    public static void SiteAndUserId(string url, out string site, out string userId, out bool validUrl) {
        site = "";
        userId = "";
        validUrl = true;
        int pos = url.IndexOf("://", StringComparison.Ordinal);
        if (pos < 0) {
            validUrl = false;
            return;
        }

        int pos2 = url.Remove(0, pos + 3).IndexOf('/');
        if (pos2 < 0) {
            validUrl = false;
            return;
        }

        site = url.Substring(pos + 3, pos2);
        int pos3 = site.IndexOf('@');
        if (pos3 >= 0) {
            userId = site.Substring(0, pos3);
            site = site.Substring(pos3 + 1);
        }
    }

    public static bool CreateFtpWebRequest(string url, string method, Dictionary<string, Login> accessCodes, out string error, out FtpWebRequest request) {
        request = null;
        if (url.Substring(0, 10) != "ftp://ftp.") {
            error = Resources.InvalidUri;
            return false;
        }

        request = (FtpWebRequest)WebRequest.Create(new Uri(url));
        request.UseBinary = true;
        request.Method = method;
        request.Credentials = LookUpCredentials(url, accessCodes, out error);
        if (!string.IsNullOrEmpty(error)) { return false; }

        if (method != WebRequestMethods.Ftp.UploadFile) {
            return true;
        }

        request.Timeout = int.MaxValue;
        request.ReadWriteTimeout = int.MaxValue;
        return true;
    }

    public async Task<bool> DownloadUsingFtpAsync(string url, string localFileFullName, bool checkOnly, Dictionary<string, Login> accessCodes, CargoString error, CargoBool couldConnect) {
        const int bufferSize = 2048;
        byte[] buffer = new byte[bufferSize + 256];

        error.Value = string.Empty;
        couldConnect.Value = false;
        if (url.Substring(0, 20) == "ftp://ftp.localhost/") {
            couldConnect.Value = true;
            var errorsAndInfos = new ErrorsAndInfos();
            string wampFile = (await folderResolver.ResolveAsync(@"$(GitHub)\Cargobay\src\Samples\FileSystem\Traveller\Wamp", errorsAndInfos)).FullName + "\\"
                + url.Remove(0, 20).Replace('/', '\\');
            if (errorsAndInfos.AnyErrors()) {
                error.Value = errorsAndInfos.ErrorsToString();
                return false;
            }
            if (checkOnly) {
                return File.Exists(wampFile);
            }

            File.Copy(wampFile, localFileFullName);
            return true;
        }

        if (!CreateFtpWebRequest(url, WebRequestMethods.Ftp.DownloadFile, accessCodes, out string errorMessage, out FtpWebRequest request)) {
            error.Value = errorMessage;
            return false;
        }

        try {
            var response = (FtpWebResponse)request.GetResponse();
            couldConnect.Value = response.IsMutuallyAuthenticated;
            Stream ftpStream = response.GetResponseStream();
            var outputStream = new FileStream(localFileFullName, FileMode.Create);
            int readCount = await ftpStream.ReadAsync(buffer, 0, bufferSize);
            if (!checkOnly) {
                while (readCount > 0) {
                    outputStream.Write(buffer, 0, readCount);
                    readCount = await ftpStream.ReadAsync(buffer, 0, bufferSize);
                }
            }

            ftpStream.Close();
            outputStream.Close();
            response.Close();
        } catch (WebException e) {
            error.Value = e.Message;
            return false;
        }

        return true;
    }

    public async Task<bool> DownloadUsingSftpAsync(string url, string localFileFullName, bool checkOnly, Dictionary<string, Login> accessCodes, CargoString error, CargoBool couldConnect) {
        error.Value = "Not implemented yet";
        return await Task.FromResult(false);
    }

    public async Task<bool> UploadUsingFtpAsync(string url, string localFileFullName, Dictionary<string, Login> accessCodes, CargoString error) {
        FtpWebResponse response;

        error.Value = string.Empty;
        if (url.Substring(0, 20) == "ftp://ftp.localhost/") {
            var errorsAndInfos = new ErrorsAndInfos();
            string wampFile = (await folderResolver.ResolveAsync(@"$(GitHub)\Cargobay\src\Samples\FileSystem\Traveller\Wamp", errorsAndInfos)).FullName + "\\"
                + url.Remove(0, 20).Replace('/', '\\');
            if (errorsAndInfos.AnyErrors()) {
                error.Value = errorsAndInfos.ErrorsToString();
                return false;
            }
            File.Copy(localFileFullName, wampFile);
            return true;
        }

        if (!CreateFtpWebRequest(url, WebRequestMethods.Ftp.UploadFile, accessCodes, out string errorMessage, out FtpWebRequest request)) {
            error.Value = errorMessage;
            return false;
        }

        request.UsePassive = true;
        request.KeepAlive = false;
        try {
            var fileStream = new FileStream(localFileFullName, FileMode.Open, FileAccess.Read);
            Stream requestStream = request.GetRequestStream();
            byte[] buffer = new byte[8092];
            int read;
            while ((read = fileStream.Read(buffer, 0, buffer.Length)) != 0) {
                await requestStream.WriteAsync(buffer, 0, read);
            }

            await requestStream.FlushAsync();
            requestStream.Close();
            response = (FtpWebResponse)request.GetResponse();
        } catch (Exception e) {
            error.Value = e.ToString();
            return false;
        }

        string status = response.StatusDescription;
        response.Close();
        request.Abort();
        if (status?.Substring(0, 3) == "226" && !status.Contains("Transfer aborted")) { return true; }
        if (!CreateFtpWebRequest(url, WebRequestMethods.Ftp.DeleteFile, accessCodes, out errorMessage, out request)) {
            error.Value = errorMessage;
            return false;
        }

        response = (FtpWebResponse)request.GetResponse();
        response.Close();
        return false;
    }

    public async Task<bool> UploadUsingSftpAsync(string url, string localFileFullName, Dictionary<string, Login> accessCodes, CargoString error) {
        error.Value = "Not implemented yet";
        return await Task.FromResult(false);
    }

    public async Task<bool> FileExistsUsingFtpAsync(string url, Dictionary<string, Login> accessCodes, CargoBool couldConnect, CargoString error) {
        error.Value = "";
        if (url.Substring(0, 20) == "ftp://ftp.localhost/") {
            var errorsAndInfos = new ErrorsAndInfos();
            couldConnect.Value = true;
            string wampFile = (await folderResolver.ResolveAsync(@"$(GitHub)\Cargobay\src\Samples\FileSystem\Traveller\Wamp", errorsAndInfos)).FullName + "\\"
                + url.Remove(0, 20).Replace('/', '\\');
            if (!errorsAndInfos.AnyErrors()) {
                return File.Exists(wampFile);
            }

            error.Value = errorsAndInfos.ErrorsToString();
            return false;
        }

        couldConnect.Value = false;
        int pos = url.LastIndexOf('/');
        if (pos < 0) {
            return false;
        }

        string directory = url.Substring(0, pos + 1);
        string fileName = url.Remove(0, pos + 1);
        if (!CreateFtpWebRequest(directory, WebRequestMethods.Ftp.ListDirectory, accessCodes, out _, out FtpWebRequest request)) {
            return false;
        }

        try {
            var response = (FtpWebResponse)request.GetResponse();
            Stream ftpStream = response.GetResponseStream();
            var ftpStreamReader = new StreamReader(ftpStream);
            bool found;
            string line;
            do {
                line = await ftpStreamReader.ReadLineAsync();
                found = fileName == line;
            } while (!found && line != null && !ftpStreamReader.EndOfStream);

            ftpStream.Close();
            response.Close();
            couldConnect.Value = true;
            return found;
        } catch {
            return false;
        }

    }

    public bool FileExistsUsingSftp(string url, Dictionary<string, Login> accessCodes, CargoBool couldConnect, CargoString error) {
        error.Value = "";
        couldConnect.Value = false;

        SiteAndUserId(url, out string site, out string _, out bool validUrl);
        if (!validUrl) {
            error.Value = "Invalid URL";
            return false;
        }

        string directoryAndFileName = url.Substring(url.IndexOf(site, StringComparison.InvariantCulture) + site.Length);
        int pos = directoryAndFileName.LastIndexOf('/');
        string directory = directoryAndFileName.Substring(0, pos + 1);
        string fileName = directoryAndFileName.Substring(pos + 1);
        NetworkCredential credentials = LookUpCredentials(url, accessCodes, out string error2);
        if (error2 != "") {
            error.Value = error2;
            return false;
        }
        using var client = new SftpClient(site, credentials.UserName, credentials.Password);
        client.Connect();

        return client.ListDirectory(directory).Any(file => file.Name == fileName);
    }

    public async Task<bool> CanUploadAsync(string url, Dictionary<string, Login> accessCodes, CargoString error) {
        var couldConnect = new CargoBool();
        if (url.StartsWith("sftp")) {
            if (FileExistsUsingSftp(url, accessCodes, couldConnect, error)) {
                return false;
            }
        } else  if (await FileExistsUsingFtpAsync(url, accessCodes, couldConnect, error)) {
            return false;
        }
        if (couldConnect.Value) { return true; }

        error.Value = string.Format(Resources.NoAccessTo, url);
        return false;
    }
}