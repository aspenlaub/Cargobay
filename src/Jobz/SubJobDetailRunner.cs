using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Cargobay.Entities;
using Aspenlaub.Net.GitHub.CSharp.Cargobay.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Components;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Entities;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Vishizhukel.Application;
using Aspenlaub.Net.GitHub.CSharp.Vishizhukel.Interfaces.Application;
using Autofac;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;

namespace Aspenlaub.Net.GitHub.CSharp.Cargobay.Jobz;

public class SubJobDetailRunner : ISubJobDetailRunner {
    private const string Indent = "    ";

    private readonly CargoHelper _CargoHelper;
    private readonly ISecretRepository _SecretRepository;

    public SubJobDetailRunner(ISecretRepository secretRepository) {
        _CargoHelper = new CargoHelper(new ContainerBuilder().UsePegh("Cargobay", new DummyCsArgumentPrompter()).Build().Resolve<IFolderResolver>());
        _SecretRepository = secretRepository;
    }

    private async Task ExecutionLogEntryAsync(IApplicationCommandExecutionContext context, string caption, string value) {
        await context.ReportAsync(new FeedbackToApplication { Type = FeedbackType.LogInformation, Message = Indent + (caption + "        ").Substring(0, 8) + " : " + value });
    }

    public async Task PreviewAsync(SubJobDetail subJobDetail, IApplicationCommandExecutionContext context) {
        if (subJobDetail.Description.Length != 0) {
            await context.ReportAsync(new FeedbackToApplication { Type = FeedbackType.LogInformation, Message = Indent + subJobDetail.Description });

        }
        if (subJobDetail.FileName.Length != 0 && subJobDetail.Description.IndexOf(subJobDetail.FileName, StringComparison.Ordinal) < 0) {
            await ExecutionLogEntryAsync(context, Properties.Resources.File, subJobDetail.FileName);
        }
    }

    private async Task<bool> CleanUpAsync(SubJobDetail subJobDetail, Job job, SubJob subJob, IApplicationCommandExecutionContext context) {
        var fullName = CargoHelper.CombineFolders(job.AdjustedFolder, subJob.AdjustedFolder) + '\\' + subJobDetail.FileName;
        Debug.Assert(File.Exists(fullName), "File not found: " + fullName);
        File.Delete(fullName);
        if (File.Exists(fullName)) {
            await context.ReportAsync(new FeedbackToApplication { Type = FeedbackType.LogError, Message = Indent + Properties.Resources.FileStillExists });
            return false;
        }

        await context.ReportAsync(new FeedbackToApplication { Type = FeedbackType.LogInformation, Message = Indent + Properties.Resources.FileDeleted });
        return true;
    }

    private async Task<bool> TransferChangedAsync(SubJobDetail subJobDetail, Job job, SubJob subJob, IApplicationCommandExecutionContext context) {
        var fileName = CargoHelper.CombineFolders(job.AdjustedFolder, subJob.AdjustedFolder) + '\\' + subJobDetail.FileName;
        var destFileName = CargoHelper.CombineFolders(job.AdjustedFolder, subJob.AdjustedDestinationFolder) + '\\' + subJobDetail.FileName;
        Debug.Assert(File.Exists(fileName), "File not found: " + fileName);
        if (File.Exists(destFileName)) {
            File.Delete(destFileName);
        }
        if (File.Exists(destFileName)) {
            await context.ReportAsync(new FeedbackToApplication { Type = FeedbackType.LogError, Message = Indent + Properties.Resources.DestinationFileStillExists });
            return false;
        }

        File.Copy(fileName, destFileName);
        if (File.Exists(destFileName)) {
            return true;
        }

        await context.ReportAsync(new FeedbackToApplication { Type = FeedbackType.LogError, Message = Indent + Properties.Resources.DestinationFileMissing });
        return false;
    }

    private async Task<bool> ZipAsync(DateTime today, Job job, SubJob subJob, IApplicationCommandExecutionContext context,
            CrypticKey crypticKey) {
        var folder = CargoHelper.CombineFolders(job.AdjustedFolder, subJob.AdjustedFolder) + '\\';
        CargoHelper.CheckFolder(folder, false, true);
        var destinationFolder = CargoHelper.CombineFolders(job.AdjustedFolder, subJob.AdjustedDestinationFolder) + '\\';
        CargoHelper.CheckFolder(destinationFolder, false, true);
        var fileName = subJob.Wildcard;
        const string ending = ".7zip";
        fileName = fileName.Replace(".*zip", ending);
        var date = today.Year % 100 * 10000 + today.Month * 100 + today.Day;
        fileName = fileName.Replace("*", date.ToString());
        if (!fileName.Contains(ending)) {
            fileName = fileName.Replace(".zip", ending);
        }
        if (!fileName.Contains(ending)) {
            return false;
        }

        var unwantedFoldersSecret = new UnwantedSubFoldersSecret();
        var errorsAndInfos = new ErrorsAndInfos();
        var unwantedFolders = (await _SecretRepository.GetAsync(unwantedFoldersSecret, errorsAndInfos)).Cast<IUnwantedSubFolder>().ToList();
        if (errorsAndInfos.AnyErrors()) {
            return false;
        }

        var fullFileName = destinationFolder + fileName;
        if (File.Exists(fullFileName)) {
            await context.ReportAsync(new FeedbackToApplication { Type = FeedbackType.LogInformation, Message = Indent + string.Format(Properties.Resources.Deleting, fileName) });
            File.Delete(fullFileName);
        }
        await context.ReportAsync(new FeedbackToApplication { Type = FeedbackType.LogInformation, Message = Indent + string.Format(Properties.Resources.Creating, fileName) });

        await using (var fileStream = File.Create(fullFileName)) {
            await using var zipStream = new ZipOutputStream(fileStream);
            zipStream.SetLevel(9);
            zipStream.Password = crypticKey.Key;
            var folderToCompress = new Folder(folder);
            if (!await CompressFolderAsync(folderToCompress, zipStream, folderToCompress.FullName.Length + 1, context, unwantedFolders)) {
                return false;
            }
            zipStream.IsStreamOwner = true;
            zipStream.Close();
        }

        if (!File.Exists(fullFileName)) {
            await context.ReportAsync(new FeedbackToApplication { Type = FeedbackType.LogError, Message = Properties.Resources.ZipFileMissing });
            return false;
        }

        var dirInfo = CargoHelper.DirInfo(destinationFolder, out var error);
        Debug.Assert(string.IsNullOrEmpty(error), error);
        var length = (await File.ReadAllBytesAsync(fullFileName)).Length;
        foreach (var fileInfo in dirInfo.GetFiles("*.*zip")
                     .Where(fileInfo => fileName != fileInfo.Name)
                     .Where(fileInfo => length == File.ReadAllBytes(destinationFolder + fileInfo.Name).Length)) {
            await context.ReportAsync(new FeedbackToApplication { Type = FeedbackType.LogInformation, Message = Indent + "Deleting " + fileName + " - identical to " + fileInfo.Name });
            File.Delete(fullFileName);
            File.SetLastWriteTime(destinationFolder + fileInfo.Name, DateTime.Now);
            return true;
        }

        await context.ReportAsync(new FeedbackToApplication { Type = FeedbackType.LogInformation, Message = Indent + string.Format(Properties.Resources.Created, fileName) });
        return true;
    }

    private async Task<bool> CompressFolderAsync(IFolder folderToCompress, ZipOutputStream zipStream, int folderOffset,
                IApplicationCommandExecutionContext context, IList<IUnwantedSubFolder> unwantedSubFolders) {
        if (folderToCompress.FullName.EndsWith(@"\packages")) { return true; }
        if (folderToCompress.FullName.EndsWith(@"\.git")) { return true; }
        if (folderToCompress.FullName.EndsWith(@"\.vs")) { return true; }
        if (folderToCompress.FullName.EndsWith(@"\tools")) { return true; }
        if (folderToCompress.FullName.EndsWith(@"\artifacts")) { return true; }
        if (folderToCompress.FullName.EndsWith(@"\obj")) { return true; }
        if (folderToCompress.FullName.EndsWith(@"\bin")) { return true; }
        if (folderToCompress.FullName.EndsWith(@"\temp")) { return true; }
        if (folderToCompress.FullName.EndsWith(@"\TestResults")) { return true; }
        if (unwantedSubFolders.Any(f => folderToCompress.FullName.Contains(f.SubFolder, StringComparison.CurrentCultureIgnoreCase))) { return true; }

        string[] files;
        try {
            files = Directory.GetFiles(folderToCompress.FullName, "*", SearchOption.TopDirectoryOnly);
        } catch (Exception e) {
            await context.ReportAsync(new FeedbackToApplication { Type = FeedbackType.LogError, Message = Indent + e.Message });
            return false;

        }
        foreach (var fileName in files) {
            if (fileName.Contains("nuget.exe")) { continue; }

            var fi = new FileInfo(fileName);
            var entryName = fileName.Substring(folderOffset);
            entryName = ZipEntry.CleanName(entryName);
            var newEntry = new ZipEntry(entryName) { DateTime = fi.LastWriteTime, Size = fi.Length };
            zipStream.PutNextEntry(newEntry);

            var buffer = new byte[4096];
            await using (var fsInput = File.OpenRead(fileName)) {
                StreamUtils.Copy(fsInput, zipStream, buffer);
            }
            zipStream.CloseEntry();
        }

        var folders = Directory.GetDirectories(folderToCompress.FullName, "*", SearchOption.TopDirectoryOnly);
        foreach (var folder in folders) {
            if (!await CompressFolderAsync(new Folder(folder), zipStream, folderOffset, context, unwantedSubFolders)) {
                return false;
            }
        }

        return true;
    }

    private async Task<bool> DownloadAsync(SubJobDetail subJobDetail, Job job, SubJob subJob, IApplicationCommandExecutionContext context, Dictionary<string, Login> accessCodes) {
        var folder = CargoHelper.CombineFolders(job.AdjustedFolder, subJob.AdjustedFolder) + '\\';
        var error = new CargoString();
        var couldConnect = new CargoBool();
        if (await _CargoHelper.DownloadAsync(subJob.Url + subJobDetail.FileName, folder + subJobDetail.FileName, false, accessCodes, error, couldConnect)) {
            await context.ReportAsync(new FeedbackToApplication { Type = FeedbackType.LogInformation, Message = Indent + Properties.Resources.DownloadSuccessful });
            return true;
        }

        if (!couldConnect.Value) {
            await context.ReportAsync(new FeedbackToApplication { Type = FeedbackType.LogError, Message = Indent + Properties.Resources.NoConnection });
            return false;
        }

        await context.ReportAsync(new FeedbackToApplication { Type = FeedbackType.LogError, Message = Indent + Properties.Resources.DownloadFailed });
        if (error.Value.Length != 0) {
            await context.ReportAsync(new FeedbackToApplication { Type = FeedbackType.LogError, Message = error.Value });
        }
        return false;
    }

    private async Task<bool> UploadAsync(SubJobDetail subJobDetail, Job job, SubJob subJob, IApplicationCommandExecutionContext context, Dictionary<string, Login> accessCodes) {
        var folder = CargoHelper.CombineFolders(job.AdjustedFolder, subJob.AdjustedFolder) + '\\';
        var error = new CargoString();
        if (await _CargoHelper.UploadAsync(subJob.Url + subJobDetail.FileName, folder + subJobDetail.FileName, accessCodes, error)) {
            await context.ReportAsync(new FeedbackToApplication { Type = FeedbackType.LogInformation, Message = Indent + Properties.Resources.UploadSuccessful });
            return true;
        }

        await context.ReportAsync(new FeedbackToApplication { Type = FeedbackType.LogError, Message = Indent + Properties.Resources.UploadFailed });
        if (error.Value.Length != 0) {
            await context.ReportAsync(new FeedbackToApplication { Type = FeedbackType.LogError, Message = Indent + error.Value });
        }
        return false;
    }

    public async Task<bool> RunAsync(SubJobDetail subJobDetail, DateTime today, Job job, SubJob subJob, IApplicationCommandExecutionContext context, CrypticKey crypticKey, Dictionary<string, Login> accessCodes) {
        await PreviewAsync(subJobDetail, context);
        switch (job.JobType) {
            case CargoJobType.CleanUp: {
                return await CleanUpAsync(subJobDetail, job, subJob, context);
            }
            case CargoJobType.TransferChanged: {
                return await TransferChangedAsync(subJobDetail, job, subJob, context);
            }
            case CargoJobType.Zip: {
                return await ZipAsync(today, job, subJob, context, crypticKey);
            }
            case CargoJobType.Upload: {
                return await UploadAsync(subJobDetail, job, subJob, context, accessCodes);
            }
            case CargoJobType.Download: {
                return await DownloadAsync(subJobDetail, job, subJob, context, accessCodes);
            }
        }

        return false;
    }
}