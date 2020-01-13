using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

namespace Aspenlaub.Net.GitHub.CSharp.Cargobay.Jobz {
    public class SubJobDetailRunner : ISubJobDetailRunner {
        private const string Indent = "    ";

        private readonly CargoHelper vCargoHelper;

        public SubJobDetailRunner() {
            vCargoHelper = new CargoHelper(new ContainerBuilder().UsePegh(new DummyCsArgumentPrompter()).Build().Resolve<IFolderResolver>());
        }

        private void ExecutionLogEntry(IApplicationCommandExecutionContext context, string caption, string value) {
            context.Report(new FeedbackToApplication { Type = FeedbackType.LogInformation, Message = Indent + (caption + "        ").Substring(0, 8) + " : " + value });
        }

        public void Preview(SubJobDetail subJobDetail, IApplicationCommandExecutionContext context) {
            if (subJobDetail.Description.Length != 0) {
                context.Report(new FeedbackToApplication { Type = FeedbackType.LogInformation, Message = Indent + subJobDetail.Description });

            }
            if (subJobDetail.FileName.Length != 0 && subJobDetail.Description.IndexOf(subJobDetail.FileName, StringComparison.Ordinal) < 0) {
                ExecutionLogEntry(context, Properties.Resources.File, subJobDetail.FileName);
            }
        }

        private bool CleanUp(SubJobDetail subJobDetail, Job job, SubJob subJob, IApplicationCommandExecutionContext context) {
            var fullName = CargoHelper.CombineFolders(job.AdjustedFolder, subJob.AdjustedFolder) + '\\' + subJobDetail.FileName;
            Debug.Assert(File.Exists(fullName), "File not found: " + fullName);
            File.Delete(fullName);
            if (File.Exists(fullName)) {
                context.Report(new FeedbackToApplication { Type = FeedbackType.LogError, Message = Indent + Properties.Resources.FileStillExists });
                return false;
            }

            context.Report(new FeedbackToApplication { Type = FeedbackType.LogInformation, Message = Indent + Properties.Resources.FileDeleted });
            return true;
        }

        private bool TransferChanged(SubJobDetail subJobDetail, Job job, SubJob subJob, IApplicationCommandExecutionContext context) {
            var fileName = CargoHelper.CombineFolders(job.AdjustedFolder, subJob.AdjustedFolder) + '\\' + subJobDetail.FileName;
            var destFileName = CargoHelper.CombineFolders(job.AdjustedFolder, subJob.AdjustedDestinationFolder) + '\\' + subJobDetail.FileName;
            Debug.Assert(File.Exists(fileName), "File not found: " + fileName);
            if (File.Exists(destFileName)) {
                File.Delete(destFileName);
            }
            if (File.Exists(destFileName)) {
                context.Report(new FeedbackToApplication { Type = FeedbackType.LogError, Message = Indent + Properties.Resources.DestinationFileStillExists });
                return false;
            }

            File.Copy(fileName, destFileName);
            if (File.Exists(destFileName)) {
                return true;
            }

            context.Report(new FeedbackToApplication { Type = FeedbackType.LogError, Message = Indent + Properties.Resources.DestinationFileMissing });
            return false;
        }

        private bool Zip(DateTime today, Job job, SubJob subJob, IApplicationCommandExecutionContext context, CrypticKey crypticKey) {
            var folder = CargoHelper.CombineFolders(job.AdjustedFolder, subJob.AdjustedFolder) + '\\';
            CargoHelper.CheckFolder(folder, false);
            var destinationFolder = CargoHelper.CombineFolders(job.AdjustedFolder, subJob.AdjustedDestinationFolder) + '\\';
            CargoHelper.CheckFolder(destinationFolder, false);
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

            var fullFileName = destinationFolder + fileName;
            if (File.Exists(fullFileName)) {
                context.Report(new FeedbackToApplication { Type = FeedbackType.LogInformation, Message = Indent + string.Format(Properties.Resources.Deleting, fileName) });
                File.Delete(fullFileName);
            }
            context.Report(new FeedbackToApplication { Type = FeedbackType.LogInformation, Message = Indent + string.Format(Properties.Resources.Creating, fileName) });

            using (var fileStream = File.Create(fullFileName)) {
                using var zipStream = new ZipOutputStream(fileStream);
                zipStream.SetLevel(9);
                zipStream.Password = crypticKey.Key;
                var folderToCompress = new Folder(folder);
                CompressFolder(folderToCompress, zipStream, folderToCompress.FullName.Length + 1);
                zipStream.IsStreamOwner = true;
                zipStream.Close();
            }

            if (!File.Exists(fullFileName)) {
                context.Report(new FeedbackToApplication { Type = FeedbackType.LogError, Message = Properties.Resources.ZipFileMissing });
                return false;
            }

            var dirInfo = CargoHelper.DirInfo(destinationFolder, out var error);
            Debug.Assert(error.Length == 0, error);
            var length = File.ReadAllBytes(fullFileName).Length;
            foreach (var fileInfo in dirInfo.GetFiles("*.*zip")
                .Where(fileInfo => fileName != fileInfo.Name)
                    .Where(fileInfo => length == File.ReadAllBytes(destinationFolder + fileInfo.Name).Length)) {
                context.Report(new FeedbackToApplication { Type = FeedbackType.LogInformation, Message = Indent + "Deleting " + fileName + " - identical to " + fileInfo.Name });
                File.Delete(fullFileName);
                File.SetLastWriteTime(destinationFolder + fileInfo.Name, DateTime.Now);
                return true;
            }

            context.Report(new FeedbackToApplication { Type = FeedbackType.LogInformation, Message = Indent + string.Format(Properties.Resources.Created, fileName) });
            return true;
        }

        private void CompressFolder(IFolder folderToCompress, ZipOutputStream zipStream, int folderOffset) {
            if (folderToCompress.FullName.EndsWith(@"\packages")) { return; }
            if (folderToCompress.FullName.EndsWith(@"\.git")) { return; }
            if (folderToCompress.FullName.EndsWith(@"\.vs")) { return; }
            if (folderToCompress.FullName.EndsWith(@"\tools")) { return; }
            if (folderToCompress.FullName.EndsWith(@"\artifacts")) { return; }
            if (folderToCompress.FullName.EndsWith(@"\obj")) { return; }
            if (folderToCompress.FullName.EndsWith(@"\bin")) { return; }
            if (folderToCompress.FullName.EndsWith(@"\temp")) { return; }
            if (folderToCompress.FullName.EndsWith(@"\TestResults")) { return; }

            var files = Directory.GetFiles(folderToCompress.FullName, "*", SearchOption.TopDirectoryOnly);
            foreach (var fileName in files) {
                if (fileName.Contains("nuget.exe")) { continue; }

                var fi = new FileInfo(fileName);
                var entryName = fileName.Substring(folderOffset);
                entryName = ZipEntry.CleanName(entryName);
                var newEntry = new ZipEntry(entryName) { DateTime = fi.LastWriteTime, Size = fi.Length };
                zipStream.PutNextEntry(newEntry);

                var buffer = new byte[4096];
                using (var fsInput = File.OpenRead(fileName)) {
                    StreamUtils.Copy(fsInput, zipStream, buffer);
                }
                zipStream.CloseEntry();
            }

            var folders = Directory.GetDirectories(folderToCompress.FullName, "*", SearchOption.TopDirectoryOnly);
            foreach (var folder in folders) {
                CompressFolder(new Folder(folder), zipStream, folderOffset);
            }
        }

        private bool Download(SubJobDetail subJobDetail, Job job, SubJob subJob, IApplicationCommandExecutionContext context, Dictionary<string, Login> accessCodes) {
            var folder = CargoHelper.CombineFolders(job.AdjustedFolder, subJob.AdjustedFolder) + '\\';
            if (vCargoHelper.Download(subJob.Url + subJobDetail.FileName, folder + subJobDetail.FileName, false, accessCodes, out var error, out var couldConnect)) {
                context.Report(new FeedbackToApplication { Type = FeedbackType.LogInformation, Message = Indent + Properties.Resources.DownloadSuccessful });
                return true;
            }

            if (!couldConnect) {
                context.Report(new FeedbackToApplication { Type = FeedbackType.LogError, Message = Indent + Properties.Resources.NoConnection });
                return false;
            }

            context.Report(new FeedbackToApplication { Type = FeedbackType.LogError, Message = Indent + Properties.Resources.DownloadFailed });
            if (error.Length != 0) {
                context.Report(new FeedbackToApplication { Type = FeedbackType.LogError, Message = error });
            }
            return false;
        }

        private bool Upload(SubJobDetail subJobDetail, Job job, SubJob subJob, IApplicationCommandExecutionContext context, Dictionary<string, Login> accessCodes) {
            var folder = CargoHelper.CombineFolders(job.AdjustedFolder, subJob.AdjustedFolder) + '\\';
            if (vCargoHelper.Upload(subJob.Url + subJobDetail.FileName, folder + subJobDetail.FileName, accessCodes, out var error)) {
                context.Report(new FeedbackToApplication { Type = FeedbackType.LogInformation, Message = Indent + Properties.Resources.UploadSuccessful });
                return true;
            }

            context.Report(new FeedbackToApplication { Type = FeedbackType.LogError, Message = Indent + Properties.Resources.UploadFailed });
            if (error.Length != 0) {
                context.Report(new FeedbackToApplication { Type = FeedbackType.LogError, Message = Indent + error });
            }
            return false;
        }

        public bool Run(SubJobDetail subJobDetail, DateTime today, Job job, SubJob subJob, IApplicationCommandExecutionContext context, CrypticKey crypticKey, Dictionary<string, Login> accessCodes) {
            Preview(subJobDetail, context);
            switch (job.JobType) {
                case CargoJobType.CleanUp: {
                        return CleanUp(subJobDetail, job, subJob, context);
                    }
                case CargoJobType.TransferChanged: {
                        return TransferChanged(subJobDetail, job, subJob, context);
                    }
                case CargoJobType.Zip: {
                        return Zip(today, job, subJob, context, crypticKey);
                    }
                case CargoJobType.Upload: {
                        return Upload(subJobDetail, job, subJob, context, accessCodes);
                    }
                case CargoJobType.Download: {
                        return Download(subJobDetail, job, subJob, context, accessCodes);
                    }
            }

            return false;
        }
    }
}
