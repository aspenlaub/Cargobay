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
using Aspenlaub.Net.GitHub.CSharp.Pegh.Extensions;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Vishizhukel.Application;
using Aspenlaub.Net.GitHub.CSharp.Vishizhukel.Interfaces.Application;
using Autofac;

namespace Aspenlaub.Net.GitHub.CSharp.Cargobay.Jobz {
    public class SubJobRunner : ISubJobRunner {
        private readonly CargoHelper vCargoHelper = new(new ContainerBuilder().UsePegh(new DummyCsArgumentPrompter()).Build().Resolve<IFolderResolver>());

        private void CreateCleanUpDetails(SubJob subJob, Job job, out string error) {
            var folder = CargoHelper.CombineFolders(job.AdjustedFolder, subJob.AdjustedFolder) + '\\';
            error = CargoHelper.CheckFolder(folder, false);
            if (error.Length != 0) {
                return;
            }

            var dirInfo = CargoHelper.DirInfo(folder, out error);
            Debug.Assert(dirInfo != null);
            Debug.Assert(error.Length == 0, error);
            foreach (var jobDetail in dirInfo.GetFiles(subJob.Wildcard).Select(f
                    => new SubJobDetail { FileName = f.Name, Description = string.Format(Properties.Resources.Deleting, f.Name) })) {
                subJob.SubJobDetails.Add(jobDetail);
            }
        }

        private void CreateTransferChangedDetails(SubJob subJob, Job job) {
            var folder = CargoHelper.CombineFolders(job.AdjustedFolder, subJob.AdjustedFolder) + '\\';
            if (!Directory.Exists(folder)) { return; }

            var destFolder = CargoHelper.CombineFolders(job.AdjustedFolder, subJob.AdjustedDestinationFolder) + '\\';
            if (!Directory.Exists(destFolder)) { return; }

            var dirInfo = CargoHelper.DirInfo(folder, out var error);
            Debug.Assert(error.Length == 0, error);
            var destDirInfo = CargoHelper.DirInfo(destFolder, out error);
            Debug.Assert(error.Length == 0, error);
            foreach (var fileInfo in dirInfo.GetFiles(subJob.Wildcard)) {
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

        private void CreateZipDetails(SubJob subJob, Job job) {
            var folder = CargoHelper.CombineFolders(job.AdjustedFolder, subJob.AdjustedFolder) + '\\';
            var destFolder = CargoHelper.CombineFolders(job.AdjustedFolder, subJob.AdjustedDestinationFolder) + '\\';
            CargoHelper.DirInfo(folder, out var error);
            Debug.Assert(error.Length == 0, error);
            CargoHelper.DirInfo(destFolder, out error);
            Debug.Assert(error.Length == 0, error);
            var jobDetail = new SubJobDetail {Description = string.Format(Properties.Resources.Zipping, folder)};
            subJob.SubJobDetails.Add(jobDetail);
        }

        private async Task CreateUploadDetailsAsync(SubJob subJob, Job job, IApplicationCommandExecutionContext context, Dictionary<string, Login> accessCodes, CargoString error) {
            var folder = CargoHelper.CombineFolders(job.AdjustedFolder, subJob.AdjustedFolder) + '\\';
            var dirInfo = CargoHelper.DirInfo(folder, out var errorMessage);
            Debug.Assert(errorMessage.Length == 0, errorMessage);
            var fileInfos = dirInfo.GetFiles(subJob.Wildcard).OrderByDescending(f => f.LastWriteTime).ToList();
            if (fileInfos.Count > 5) {
                ExecutionLogEntry(context, Properties.Resources.UploadReduced, Properties.Resources.UploadReducedToNewestFiveFiles);
                fileInfos = fileInfos.Take(5).ToList();
            }
            foreach (var fileInfo in fileInfos) {
                if (await vCargoHelper.CanUploadAsync(subJob.Url + fileInfo.Name, accessCodes, error)) {
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

        private async Task CreateDownloadDetailsAsync(SubJob subJob, Job job, CargoString error) {
            var folder = CargoHelper.CombineFolders(job.AdjustedFolder, subJob.AdjustedFolder) + '\\';
            var errorsAndInfos = new ErrorsAndInfos();
            var fileNames = await vCargoHelper.DownloadableAsync(subJob.Url, subJob.Wildcard, errorsAndInfos);
            if (errorsAndInfos.AnyErrors()) {
                error.Value = errorsAndInfos.ErrorsToString();
                return;
            }

            foreach (var fileName in fileNames) {
                if (File.Exists(folder + fileName)) {
                    continue;
                }

                var jobDetail = new SubJobDetail {
                    FileName = fileName,
                    Description = string.Format(Properties.Resources.DownloadingNewFile, fileName)
                };
                subJob.SubJobDetails.Add(jobDetail);
            }
        }

        private static void ExecutionLogEntry(IApplicationCommandExecutionContext context, string caption, string value) {
            context.Report(new FeedbackToApplication { Type = FeedbackType.LogInformation, Message = "    " + (caption + "        ").Substring(0, 8) + " : " + value });
        }

        public string SubJobName(SubJob subJob) {
            if (subJob.AdjustedDestinationFolder.Length != 0) {
                return subJob.AdjustedDestinationFolder + '\\' + subJob.Wildcard;
            }
            if (subJob.AdjustedFolder.Length != 0) {
                return subJob.AdjustedFolder + '\\' + subJob.Wildcard;
            }

            return subJob.Wildcard.Length != 0 ? subJob.Wildcard : "?";
        }

        public async Task PreviewAsync(SubJob subJob, Job job, bool forExecutionLog, IApplicationCommandExecutionContext context, ISubJobDetailRunner runner, Dictionary<string, Login> accessCodes) {
            var error = new CargoString();
            await CreateDetailsAsync(subJob, job, context, accessCodes, error);
            if (error.Value.Length != 0) {
                context.Report(new FeedbackToApplication { Type = FeedbackType.LogError, Message = "    " + error.Value });
                return;
            }

            if (subJob.SubJobDetails.Count == 0) {
                return;
            }

            context.Report(new FeedbackToApplication { Type = FeedbackType.LogInformation, Message = "    " });
            context.Report(new FeedbackToApplication { Type = FeedbackType.LogInformation, Message = "    Subjob" });
            var name = SubJobName(subJob);
            if (name.Length > 1) {
                context.Report(new FeedbackToApplication { Type = FeedbackType.LogInformation, Message = "    " + name });
            }
            if (forExecutionLog) {
                return;
            }

            if (subJob.AdjustedFolder.Length != 0) {
                ExecutionLogEntry(context, Properties.Resources.Folder, subJob.AdjustedFolder);
            }
            if (subJob.AdjustedDestinationFolder.Length != 0) {
                ExecutionLogEntry(context, Properties.Resources.Destination, subJob.AdjustedDestinationFolder);
            }
            if (subJob.Wildcard.Length != 0) {
                ExecutionLogEntry(context, Properties.Resources.Wildcard, subJob.Wildcard);
            }
            if (subJob.Url.Length != 0) {
                ExecutionLogEntry(context, Properties.Resources.Url, subJob.Url);
            }
            foreach (var jobDetail in subJob.SubJobDetails) {
                runner.Preview(jobDetail, context);
            }
        }

        private async Task CreateDetailsAsync(SubJob subJob, Job job, IApplicationCommandExecutionContext context, Dictionary<string, Login> accessCodes, CargoString error) {
            subJob.SubJobDetails.Clear();
            switch (job.JobType) {
                case CargoJobType.CleanUp: {
                    CreateCleanUpDetails(subJob, job, out var errorMessage);
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
                case CargoJobType.Download: {
                    await CreateDownloadDetailsAsync(subJob, job, error);
                }
                break;
            }
        }

        public async Task<bool> RunAsync(SubJob subJob, DateTime today, Job job, IApplicationCommandExecutionContext context, ISubJobDetailRunner runner, CrypticKey crypticKey, Dictionary<string, Login> accessCodes) {
            var error = new CargoString();
            await CreateDetailsAsync(subJob, job, context, accessCodes, error);
            if (error.Value.Length != 0) { return false; }

            foreach (var nextSubJobDetail in subJob.SubJobDetails) {
                if (!await runner.RunAsync(nextSubJobDetail, today, job, subJob, context, crypticKey, accessCodes)) { return false; }
            }

            return true;
        }
    }
}