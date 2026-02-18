using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Cargobay.Components;
using Aspenlaub.Net.GitHub.CSharp.Cargobay.Entities;
using Aspenlaub.Net.GitHub.CSharp.Cargobay.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Seoa.Extensions;
using Aspenlaub.Net.GitHub.CSharp.Skladasu.Entities;
using Autofac;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[assembly: DoNotParallelize]
namespace Aspenlaub.Net.GitHub.CSharp.Cargobay.Test.Components;

[TestClass]
public class JobFolderAdjusterTest {
    [TestMethod]
    public async Task CanAdjustJobFolders() {
        IContainer container = new ContainerBuilder().UseCargobay().Build();
        IJobFolderAdjuster sut = container.Resolve<IJobFolderAdjuster>();
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
        Assert.That.ThereWereNoErrors(errorsAndInfos);
        IFolderResolver resolver = container.Resolve<IFolderResolver>();
        IFolder cSharpFolder = await resolver.ResolveAsync("$(CSharp)", errorsAndInfos);
        Assert.That.ThereWereNoErrors(errorsAndInfos);
        Assert.AreEqual(FolderAdjustmentState.Adjusted, job.FolderAdjustmentState);
        Assert.AreEqual(FolderAdjustmentState.Adjusted, job.SubJobs[0].FolderAdjustmentState);
        Assert.AreEqual(FolderAdjustmentState.Adjusted, job.SubJobs[1].FolderAdjustmentState);
        Assert.AreEqual(cSharpFolder.FullName, job.AdjustedFolder);
        Assert.AreEqual(cSharpFolder.FullName + @"\Cargobay", job.AdjustedDestinationFolder);
        Assert.AreEqual(cSharpFolder.FullName + @"\Cargobay\Components", job.SubJobs[0].AdjustedFolder);
        Assert.AreEqual(cSharpFolder.FullName + @"\Cargobay\Interfaces", job.SubJobs[0].AdjustedDestinationFolder);
        char drive = cSharpFolder.FullName[0];
        Assert.AreEqual(drive + @":\temp", job.SubJobs[1].AdjustedFolder);
        Assert.AreEqual(@"d:\temp", job.SubJobs[1].AdjustedDestinationFolder);
    }
}