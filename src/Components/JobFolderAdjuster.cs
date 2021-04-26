using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Cargobay.Entities;
using Aspenlaub.Net.GitHub.CSharp.Cargobay.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Cargobay.Components {
    public class JobFolderAdjuster : IJobFolderAdjuster {
        private readonly IFolderResolver vFolderResolver;

        public JobFolderAdjuster(IFolderResolver folderResolver) {
            vFolderResolver = folderResolver;
        }

        public async Task AdjustJobAndSubFoldersAsync(Job job, IErrorsAndInfos errorsAndInfos) {
            var driveToReplaceCWith = (await vFolderResolver.ResolveAsync("$(GitHub)", errorsAndInfos)).FullName;
            driveToReplaceCWith = driveToReplaceCWith.Substring(0, 1);

            job.FolderAdjustmentState = FolderAdjustmentState.Adjusting;
            job.AdjustedFolder = await AdjustedFolderFullNameAsync(job.LogicalFolder, driveToReplaceCWith, errorsAndInfos);
            job.AdjustedDestinationFolder = await AdjustedFolderFullNameAsync(job.LogicalDestinationFolder, driveToReplaceCWith, errorsAndInfos);
            job.FolderAdjustmentState = errorsAndInfos.AnyErrors() ? FolderAdjustmentState.NotAdjusted : FolderAdjustmentState.Adjusted;
            foreach (var subJob in job.SubJobs) {
                subJob.FolderAdjustmentState = FolderAdjustmentState.Adjusting;
                subJob.AdjustedFolder = await AdjustedFolderFullNameAsync(subJob.LogicalFolder, driveToReplaceCWith, errorsAndInfos);
                subJob.AdjustedDestinationFolder = await AdjustedFolderFullNameAsync(subJob.LogicalDestinationFolder, driveToReplaceCWith, errorsAndInfos);
                subJob.FolderAdjustmentState = errorsAndInfos.AnyErrors() ? FolderAdjustmentState.NotAdjusted : FolderAdjustmentState.Adjusted;
            }
        }

        private async Task<string> AdjustedFolderFullNameAsync(string folder, string driveToReplaceCWith, IErrorsAndInfos errorsAndInfos) {
            if (folder.Length < 2) {
                return folder;
            }
            if (folder.Substring(0, 2).ToLower() == "c:") {
                folder = driveToReplaceCWith + folder.Substring(1);
            }
            return (await vFolderResolver.ResolveAsync(folder, errorsAndInfos)).FullName;
        }
    }
}
