using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Cargobay.Components;
using Aspenlaub.Net.GitHub.CSharp.Cargobay.Entities;
using Aspenlaub.Net.GitHub.CSharp.Cargobay.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Entities;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Extensions;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;
using Autofac;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[assembly: DoNotParallelize]
namespace Aspenlaub.Net.GitHub.CSharp.Cargobay.Test.Components;

[TestClass]
public class JobFolderAdjusterTest {
    [TestMethod]
    public async Task CanAdjustJobFolders() {
        var container = new ContainerBuilder().UseCargobay().Build();
        var sut = container.Resolve<IJobFolderAdjuster>();
        var job = new Job {
            LogicalFolder = "$(CSharp)",
            LogicalDestinationFolder = @"$(CSharp)\Cargobay",
            SubJobs = new ObservableCollection<SubJob> {
                new() {
                    LogicalFolder = @"$(CSharp)\Cargobay\Components",
                    LogicalDestinationFolder = @"$(CSharp)\Cargobay\Interfaces"
                },

                new() {
                    LogicalFolder = @"c:\temp\",
                    LogicalDestinationFolder = @"d:\temp\"
                }
            }
        };
        Assert.AreEqual(FolderAdjustmentState.NotAdjusted, job.FolderAdjustmentState);
        Assert.AreEqual(FolderAdjustmentState.NotAdjusted, job.SubJobs[0].FolderAdjustmentState);
        var errorsAndInfos = new ErrorsAndInfos();
        await sut.AdjustJobAndSubFoldersAsync(job, errorsAndInfos);
        Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsToString());
        var resolver = container.Resolve<IFolderResolver>();
        var cSharpFolder = await resolver.ResolveAsync("$(CSharp)", errorsAndInfos);
        Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsToString());
        Assert.AreEqual(FolderAdjustmentState.Adjusted, job.FolderAdjustmentState);
        Assert.AreEqual(FolderAdjustmentState.Adjusted, job.SubJobs[0].FolderAdjustmentState);
        Assert.AreEqual(FolderAdjustmentState.Adjusted, job.SubJobs[1].FolderAdjustmentState);
        Assert.AreEqual(cSharpFolder.FullName, job.AdjustedFolder);
        Assert.AreEqual(cSharpFolder.FullName + @"\Cargobay", job.AdjustedDestinationFolder);
        Assert.AreEqual(cSharpFolder.FullName + @"\Cargobay\Components", job.SubJobs[0].AdjustedFolder);
        Assert.AreEqual(cSharpFolder.FullName + @"\Cargobay\Interfaces", job.SubJobs[0].AdjustedDestinationFolder);
        var drive = cSharpFolder.FullName[0];
        Assert.AreEqual(drive + @":\temp", job.SubJobs[1].AdjustedFolder);
        Assert.AreEqual(@"d:\temp", job.SubJobs[1].AdjustedDestinationFolder);
    }
}