using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Cargobay.Entities;
using Aspenlaub.Net.GitHub.CSharp.Cargobay.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Components;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Vishizhukel.Application;
using Aspenlaub.Net.GitHub.CSharp.Vishizhukel.Interfaces.Application;
using Autofac;

namespace Aspenlaub.Net.GitHub.CSharp.Cargobay.Jobz;

public class SubJobRunner : ISubJobRunner {
    private readonly CargoHelper _CargoHelper = new(new ContainerBuilder().UsePegh("Cargobay").Build().Resolve<IFolderResolver>());

    private static void CreateCleanUpDetails(SubJob subJob, Job job, out string error) {
        string folder = CargoHelper.CombineFolders(job.AdjustedFolder, subJob.AdjustedFolder) + '\\';
        error = CargoHelper.CheckFolder(folder, false, false);
        if (!string.IsNullOrEmpty(error)) {
            error = "";
            return;
        }

        DirectoryInfo dirInfo = CargoHelper.DirInfo(folder, out error);
        Debug.Assert(dirInfo != null);
        Debug.Assert(string.IsNullOrEmpty(error), error);
        foreach (SubJobDetail jobDetail in dirInfo.GetFiles(subJob.Wildcard).Select(f
                                                                                        => new SubJobDetail { FileName = f.Name, Description = string.Format(Properties.Resources.Deleting, f.Name) })) {
            subJob.SubJobDetails.Add(jobDetail);
        }
    }

    private static void CreateTransferChangedDetails(SubJob subJob, Job job) {
        string folder = CargoHelper.CombineFolders(job.AdjustedFolder, subJob.AdjustedFolder) + '\\';
        if (!Directory.Exists(folder)) { return; }

        string destFolder = CargoHelper.CombineFolders(job.AdjustedFolder, subJob.AdjustedDestinationFolder) + '\\';
        if (!Directory.Exists(destFolder)) { return; }

        DirectoryInfo dirInfo = CargoHelper.DirInfo(folder, out string error);
        Debug.Assert(string.IsNullOrEmpty(error), error);
        DirectoryInfo destDirInfo = CargoHelper.DirInfo(destFolder, out error);
        Debug.Assert(string.IsNullOrEmpty(error), error);
        foreach (FileInfo fileInfo in dirInfo.GetFiles(subJob.Wildcard)) {
            SubJobDetail jobDetail;
            if (destDirInfo.GetFiles(fileInfo.Name).Length == 0) {
                jobDetail = new SubJobDetail {
                    FileName = fileInfo.Name,
                    Description = string.Format(Properties.Resources.TransferingNewFile, fileInfo.Name)
                };
                subJob.SubJobDetails.Add(jobDetail);
                return;
            }

            if (!destDirInfo.GetFiles(fileInfo.Name).Any(f => fileInfo.LastWriteTime > f.LastWriteTime)) {
                continue;
            }

            jobDetail = new SubJobDetail {
                FileName = fileInfo.Name,
                Description = string.Format(Properties.Resources.TransferingChangedFile, fileInfo.Name)
            };
            subJob.SubJobDetails.Add(jobDetail);
        }
    }

    private static void CreateZipDetails(SubJob subJob, Job job) {
        string folder = CargoHelper.CombineFolders(job.AdjustedFolder, subJob.AdjustedFolder) + '\\';
        string destFolder = CargoHelper.CombineFolders(job.AdjustedFolder, subJob.AdjustedDestinationFolder) + '\\';
        CargoHelper.DirInfo(folder, out string error);
        Debug.Assert(string.IsNullOrEmpty(error), error);
        CargoHelper.DirInfo(destFolder, out error);
        Debug.Assert(string.IsNullOrEmpty(error), error);
        var jobDetail = new SubJobDetail {Description = string.Format(Properties.Resources.Zipping, folder)};
        subJob.SubJobDetails.Add(jobDetail);
    }

    private async Task CreateUploadDetailsAsync(SubJob subJob, Job job, IApplicationCommandExecutionContext context, Dictionary<string, Login> accessCodes, CargoString error) {
        string folder = CargoHelper.CombineFolders(job.AdjustedFolder, subJob.AdjustedFolder) + '\\';
        DirectoryInfo dirInfo = CargoHelper.DirInfo(folder, out string errorMessage);
        Debug.Assert(errorMessage.Length == 0, errorMessage);
        var fileInfos = dirInfo.GetFiles(subJob.Wildcard).OrderByDescending(f => f.LastWriteTime).ToList();
        if (fileInfos.Count > 5) {
            await context.ReportAsync(new FeedbackToApplication {
                Type = FeedbackType.LogInformation, Message = (Properties.Resources.Upload + "        ").Substring(0, 12) + " : " + Properties.Resources.UploadReducedToNewestFiveFiles
            });
            fileInfos = [.. fileInfos.Take(5)];
        }
        foreach (FileInfo fileInfo in fileInfos) {
            if (await _CargoHelper.CanUploadAsync(subJob.Url + fileInfo.Name, accessCodes, error)) {
                var jobDetail = new SubJobDetail {
                    FileName = fileInfo.Name,
                    Description = string.Format(Properties.Resources.UploadingNewFile, fileInfo.Name)
                };
                subJob.SubJobDetails.Add(jobDetail);
            } else if (error.Value.Length != 0) {
                return;
            }
        }
    }

    private static async Task ExecutionLogEntryAsync(IApplicationCommandExecutionContext context, string caption, string value) {
        await context.ReportAsync(new FeedbackToApplication { Type = FeedbackType.LogInformation, Message = "    " + (caption + "        ").Substring(0, 8) + " : " + value });
    }

    public static string SubJobName(SubJob subJob) {
        return subJob.AdjustedDestinationFolder.Length != 0
            ? subJob.AdjustedDestinationFolder + '\\' + subJob.Wildcard
            : subJob.AdjustedFolder.Length != 0
                ? subJob.AdjustedFolder + '\\' + subJob.Wildcard
                : subJob.Wildcard.Length != 0
                    ? subJob.Wildcard
                    : "?";
    }

    public async Task PreviewAsync(SubJob subJob, Job job, bool forExecutionLog, IApplicationCommandExecutionContext context, ISubJobDetailRunner runner, Dictionary<string, Login> accessCodes) {
        var error = new CargoString();
        await CreateDetailsAsync(subJob, job, context, accessCodes, error);
        if (error.Value.Length != 0) {
            await context.ReportAsync(new FeedbackToApplication { Type = FeedbackType.LogError, Message = "    " + error.Value });
            return;
        }

        if (subJob.SubJobDetails.Count == 0) {
            return;
        }

        await context.ReportAsync(new FeedbackToApplication { Type = FeedbackType.LogInformation, Message = "    " });
        await context.ReportAsync(new FeedbackToApplication { Type = FeedbackType.LogInformation, Message = "    Subjob" });
        string name = SubJobName(subJob);
        if (name.Length > 1) {
            await context.ReportAsync(new FeedbackToApplication { Type = FeedbackType.LogInformation, Message = "    " + name });
        }
        if (forExecutionLog) {
            return;
        }

        if (subJob.AdjustedFolder.Length != 0) {
            await ExecutionLogEntryAsync(context, Properties.Resources.Folder, subJob.AdjustedFolder);
        }
        if (subJob.AdjustedDestinationFolder.Length != 0) {
            await ExecutionLogEntryAsync(context, Properties.Resources.Destination, subJob.AdjustedDestinationFolder);
        }
        if (subJob.Wildcard.Length != 0) {
            await ExecutionLogEntryAsync(context, Properties.Resources.Wildcard, subJob.Wildcard);
        }
        if (subJob.Url.Length != 0) {
            await ExecutionLogEntryAsync(context, Properties.Resources.Url, subJob.Url);
        }
        foreach (SubJobDetail jobDetail in subJob.SubJobDetails) {
            await runner.PreviewAsync(jobDetail, context);
        }
    }

    private async Task CreateDetailsAsync(SubJob subJob, Job job, IApplicationCommandExecutionContext context, Dictionary<string, Login> accessCodes, CargoString error) {
        subJob.SubJobDetails.Clear();
        switch (job.JobType) {
            case CargoJobType.CleanUp: {
                CreateCleanUpDetails(subJob, job, out string errorMessage);
                error.Value = errorMessage;
            }
                break;
            case CargoJobType.TransferChanged: {
                CreateTransferChangedDetails(subJob, job);
            }
                break;
            case CargoJobType.Zip: {
                CreateZipDetails(subJob, job);
            }
                break;
            case CargoJobType.Upload: {
                await CreateUploadDetailsAsync(subJob, job, context, accessCodes, error);
            }
                break;
            case CargoJobType.None:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public async Task<bool> RunAsync(SubJob subJob, DateTime today, Job job, IApplicationCommandExecutionContext context, ISubJobDetailRunner runner, CrypticKey crypticKey, Dictionary<string, Login> accessCodes) {
        var error = new CargoString();
        await CreateDetailsAsync(subJob, job, context, accessCodes, error);
        if (error.Value.Length != 0) { return false; }

        foreach (SubJobDetail nextSubJobDetail in subJob.SubJobDetails) {
            if (!await runner.RunAsync(nextSubJobDetail, today, job, subJob, context, crypticKey, accessCodes)) { return false; }
        }

        return true;
    }
}