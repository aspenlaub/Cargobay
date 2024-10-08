using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Cargobay.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Aspenlaub.Net.GitHub.CSharp.Cargobay.Entities;
using Aspenlaub.Net.GitHub.CSharp.Cargobay.Extensions;
using Aspenlaub.Net.GitHub.CSharp.Cargobay.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Cargobay.Jobz;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Components;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Entities;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Extensions;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;
using Autofac;

namespace Aspenlaub.Net.GitHub.CSharp.Cargobay.Test;

[TestClass]
public class CargoJobCollectionTest {
    private readonly IContainer _Container;

    public CargoJobCollectionTest() {
        _Container = new ContainerBuilder().UseCargobay().Build();
    }

    [TestMethod]
    public async Task FoldersAreInPlace() {
        await using var context = new CargoJobCollectionTestExecutionContext();
        await context.SetSampleRootFolderIfNecessaryAsync();

        var errorsAndInfos = new ErrorsAndInfos();
        var error = CargoHelper.CheckFolder((await _Container.Resolve<IFolderResolver>().ResolveAsync(@"$(MainUserFolder)\Cargo.Samples", errorsAndInfos)).FullName, true, true);
        Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsToString());
        Assert.IsFalse(string.IsNullOrEmpty(error), "Backslash at end not mandatory");
        error = CargoHelper.CheckFolder((await _Container.Resolve<IFolderResolver>().ResolveAsync(@"$(MainUserFolder)\Cargo.Samples", errorsAndInfos)).FullName + "\\\\", true, true);
        Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsToString());
        Assert.IsFalse(string.IsNullOrEmpty(error), "Double backslash allowed");
        error = CargoHelper.CheckFolder((await _Container.Resolve<IFolderResolver>().ResolveAsync(@"$(MainUserFolder)", errorsAndInfos)).FullName, true, true);
        Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsToString());
        Assert.IsFalse(string.IsNullOrEmpty(error), "Path outside playground is allowed");
    }

    [TestMethod]
    public async Task CanSaveSimpleJobCollection() {
        await using var context = new CargoJobCollectionTestExecutionContext();
        await context.SetSampleRootFolderIfNecessaryAsync();

        var jobs = new CargoJobs();
        var job = new Job {
            Name = "This is the name",
            JobType = CargoJobType.CleanUp
        };
        var subJob = new SubJob();
        var detail = new SubJobDetail { Description = "This is the detail description" };
        subJob.SubJobDetails.Add(detail);
        subJob.SubJobDetails.Add(detail);
        job.SubJobs.Add(subJob);
        job.SubJobs.Add(subJob);
        var errorsAndInfos = new ErrorsAndInfos();
        var adjuster = _Container.Resolve<IJobFolderAdjuster>();
        await adjuster.AdjustJobAndSubFoldersAsync(job, errorsAndInfos);
        Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsToString());
        jobs.Add(job);
        jobs.Add(job);
        var destFolder = context.SampleRootFolder + @"\Log\";
        const string destFile = "SaveSimple.xml";
        File.Delete(destFolder + destFile);
        Assert.IsTrue(jobs.Save(_Container.Resolve<IXmlSerializer>(), destFolder + destFile));
        errorsAndInfos = new ErrorsAndInfos();
        var jobsRev = await JobsExtensions.LoadAsync(_Container.Resolve<IXmlDeserializer>(), _Container.Resolve<IJobFolderAdjuster>(), destFolder, destFile, errorsAndInfos);
        Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsToString());
        Assert.IsTrue(jobsRev.Count == 2);
        File.Delete(destFolder + destFile);
    }

    [TestMethod]
    public async Task CanLoadAndSaveSample() {
        await using var context = new CargoJobCollectionTestExecutionContext();
        await context.SetSampleRootFolderIfNecessaryAsync();

        var sampleRootFolder = context.SampleRootFolder;
        new Folder(sampleRootFolder).SubFolder("Log").CreateIfNecessary();
        var sourceFolder = sampleRootFolder + @"\";
        const string sourceFile = "CargoJobs1.xml";
        var destFile = sampleRootFolder + @"\Log\CargoJobs1.xml";
        File.Delete(destFile);
        var errorsAndInfos = new ErrorsAndInfos();
        var cargoJobs = await JobsExtensions.LoadAsync(_Container.Resolve<IXmlDeserializer>(), _Container.Resolve<IJobFolderAdjuster>(), sourceFolder, sourceFile, errorsAndInfos);
        Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsToString());
        Assert.IsTrue(cargoJobs.Count == 4, "Four jobs expected, read " + cargoJobs.Count);
        Assert.IsTrue(cargoJobs[0].SubJobs.Count > 20);
        Assert.IsTrue(cargoJobs[0].SubJobs[0].LogicalFolder.Length > 5);
        cargoJobs.Save(_Container.Resolve<IXmlSerializer>(), destFile);
        var sourceContents = RemoveVersionNumber(await File.ReadAllTextAsync(sourceFolder + sourceFile, Encoding.UTF8));
        Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsToString());
        const string search = "xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"";
        const string replace = "xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"";
        var destinationContents = (await File.ReadAllTextAsync(destFile, Encoding.UTF8)).Replace(search, replace);
        int i;
        for (i = 0; i < sourceContents.Length && sourceContents[i] == destinationContents[i]; i++) { }

        Assert.AreEqual(sourceContents.Substring(i), destinationContents.Substring(i), "Source and destination file differ");
        File.Delete(destFile);
    }

    private async Task InitCase456Async(CargoJobCollectionTestExecutionContext context, CargoString sampleRootFolder, CargoString sampleFileSystemRootFolder, List<Job> cargoJobs, string addFileName) {
        sampleRootFolder.Value = context.SampleRootFolder;
        sampleFileSystemRootFolder.Value = context.SampleFileSystemRootFolder;
        if (addFileName.Length != 0) {
            addFileName = sampleFileSystemRootFolder.Value + addFileName;
            await File.WriteAllTextAsync(addFileName, @"This is a file that was added.", Encoding.UTF8);
        }
        var sourceFolder = sampleRootFolder.Value + @"\";
        const string sourceFile = "CargoJobs2.xml";
        var errorsAndInfos = new ErrorsAndInfos();
        var deserializedJobs = await JobsExtensions.LoadAsync(_Container.Resolve<IXmlDeserializer>(), _Container.Resolve<IJobFolderAdjuster>(), sourceFolder, sourceFile, errorsAndInfos);
        Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsToString());
        cargoJobs.Clear();
        cargoJobs.AddRange(deserializedJobs);
    }


    private Dictionary<string, Login> AccessCodes() {
        var accessCodes = new Dictionary<string, Login> {[@"ftp://ftp.localhost"] = new() { Identification = "guest", Password = "guest" }};
        return accessCodes;
    }

    private async Task RunJobAsync(List<Job> cargoJobs, string name, DateTime currentDate, IJobRunner runner, ISubJobRunner subRunner, ISubJobDetailRunner detailRunner, CrypticKey crypticKey, Dictionary<string, Login> accessCodes) {
        var nextJob = cargoJobs.Find(x => x.Name == name);
        Assert.IsNotNull(nextJob, "Job '" + name + "' not found");
        Assert.IsTrue(await runner.RunAsync(nextJob, currentDate, new FakeCommandExecutionContext(), subRunner, detailRunner, crypticKey, accessCodes), "Job '" + name + "' could not be processed");
    }

    [TestMethod]
    public async Task CanProcessFirstDay() {
        await using var context = new CargoJobCollectionTestExecutionContext();
        await context.SetSampleRootFolderIfNecessaryAsync();

        var sampleRootFolder = new CargoString();
        var sampleFileSystemRootFolder = new CargoString();
        var cargoJobs = new List<Job>();
        await InitCase456Async(context, sampleRootFolder, sampleFileSystemRootFolder, cargoJobs, "");
        new Folder(sampleFileSystemRootFolder.Value).SubFolder("Traveller").SubFolder("Webdev").CreateIfNecessary();
        var webZipFile = sampleFileSystemRootFolder.Value + @"\Traveller\Webdev\webseiten100825.7zip";
        Assert.IsFalse(File.Exists(webZipFile), "Web zip file exists.");
        var crypticKeyProvider = new FakeCrypticKeyProvider();
        var crypticKey = crypticKeyProvider.GetCrypticKey("", "");
        var accessCodes = AccessCodes();
        await RunFirstDayAsync(cargoJobs, new JobRunner(), new SubJobRunner(), new SubJobDetailRunner(_Container.Resolve<ISecretRepository>()),
            crypticKey, accessCodes);
        Assert.IsTrue(File.Exists(webZipFile), "Web zip file does not exist.");
    }

    private async Task RunFirstDayAsync(List<Job> cargoJobs, IJobRunner runner, ISubJobRunner subRunner, ISubJobDetailRunner detailRunner, CrypticKey crypticKey, Dictionary<string, Login> accessCodes) {
        var currentDate = new DateTime(2010, 8, 25);
        await RunJobAsync(cargoJobs, "CleanUpWeb", currentDate, runner, subRunner, detailRunner, crypticKey, accessCodes);
        await RunJobAsync(cargoJobs, "ZipWamp", currentDate, runner, subRunner, detailRunner, crypticKey, accessCodes);
        await RunJobAsync(cargoJobs, "ArchiveNessies", currentDate, runner, subRunner, detailRunner, crypticKey, accessCodes);
        await RunJobAsync(cargoJobs, "CleanUpNessies", currentDate, runner, subRunner, detailRunner, crypticKey, accessCodes);
    }

    [TestMethod]
    public async Task CanProcessSecondDay() {
        await using var context = new CargoJobCollectionTestExecutionContext();
        await context.SetSampleRootFolderIfNecessaryAsync();

        var runner = new JobRunner();
        var subRunner = new SubJobRunner();
        var detailRunner = new SubJobDetailRunner(_Container.Resolve<ISecretRepository>());
        var crypticKeyProvider = new FakeCrypticKeyProvider();
        var crypticKey = crypticKeyProvider.GetCrypticKey("", "");
        var accessCodes = AccessCodes();
        var sampleRootFolder = new CargoString();
        var sampleFileSystemRootFolder = new CargoString();
        var cargoJobs = new List<Job>();
        await PrepareSecondDayAsync(context, runner, subRunner, detailRunner, crypticKey, accessCodes, sampleRootFolder, sampleFileSystemRootFolder, cargoJobs);
        await InitCase456Async(context, sampleRootFolder, sampleFileSystemRootFolder, cargoJobs, @"\Traveller\Wamp\tank.php");
        var webZipFile = sampleFileSystemRootFolder.Value + @"\Traveller\Webdev\webseiten100825.7zip";
        Assert.IsTrue(File.Exists(webZipFile), "Preceding test case failed.");
        webZipFile = sampleFileSystemRootFolder.Value + @"\Traveller\Webdev\webseiten100826.7zip";
        var uploadWebZipFile = sampleFileSystemRootFolder.Value + @"\Traveller\Wamp\download\webseiten100825.7zip";
        Assert.IsFalse(File.Exists(webZipFile), "Web zip file exists.");
        Assert.IsFalse(File.Exists(uploadWebZipFile), "Uploaded web zip file exists.");
        await RunSecondDayAsync(cargoJobs, runner, subRunner, detailRunner, crypticKey, accessCodes);
        Assert.IsTrue(File.Exists(webZipFile), "Web zip file does not exist.");
        Assert.IsTrue(File.Exists(uploadWebZipFile), "Uploaded web zip file does not exist.");
    }

    private async Task PrepareSecondDayAsync(CargoJobCollectionTestExecutionContext context, IJobRunner runner, ISubJobRunner subRunner, ISubJobDetailRunner detailRunner, CrypticKey crypticKey, Dictionary<string, Login> accessCodes, CargoString sampleRootFolder, CargoString sampleFileSystemRootFolder, List<Job> cargoJobs) {
        await InitCase456Async(context, sampleRootFolder, sampleFileSystemRootFolder, cargoJobs, "");
        await RunFirstDayAsync(cargoJobs, runner, subRunner, detailRunner, crypticKey, accessCodes);
    }

    private async Task RunSecondDayAsync(List<Job> cargoJobs, IJobRunner runner, ISubJobRunner subRunner, ISubJobDetailRunner detailRunner, CrypticKey crypticKey, Dictionary<string, Login> accessCodes) {
        var currentDate = new DateTime(2010, 8, 26);
        await RunJobAsync(cargoJobs, "ZipWamp", currentDate, runner, subRunner, detailRunner, crypticKey, accessCodes);
        await RunJobAsync(cargoJobs, "UploadZip", currentDate, runner, subRunner, detailRunner, crypticKey, accessCodes);
    }

    [TestMethod]
    public async Task CanProcessThirdDay() {
        await using var context = new CargoJobCollectionTestExecutionContext();
        await context.SetSampleRootFolderIfNecessaryAsync();

        var runner = new JobRunner();
        var subRunner = new SubJobRunner();
        var detailRunner = new SubJobDetailRunner(_Container.Resolve<ISecretRepository>());
        var crypticKeyProvider = new FakeCrypticKeyProvider();
        var crypticKey = crypticKeyProvider.GetCrypticKey("", "");
        var accessCodes = AccessCodes();
        new Folder(context.SampleFileSystemRootFolder).SubFolder("Traveller").SubFolder("Wamp").SubFolder("download").CreateIfNecessary();
        var sampleRootFolder = new CargoString();
        var sampleFileSystemRootFolder = new CargoString();
        var cargoJobs = new List<Job>();
        await PrepareThirdDayAsync(context, runner, subRunner, detailRunner, crypticKey, accessCodes, sampleRootFolder, sampleFileSystemRootFolder, cargoJobs);
        File.Delete(context.SampleFileSystemRootFolder + @"\Traveller\Wamp\download\webseiten100825.7zip");
        File.Delete(context.SampleFileSystemRootFolder + @"\Traveller\Wamp\download\webseiten100826.7zip");
        await InitCase456Async(context, sampleRootFolder, sampleFileSystemRootFolder, cargoJobs, "");
        var webZipFile = sampleFileSystemRootFolder.Value + @"\Traveller\Webdev\webseiten100826.7zip";
        Assert.IsTrue(File.Exists(webZipFile), "Preceding test case failed.");
        webZipFile = sampleFileSystemRootFolder.Value + @"\Traveller\Webdev\webseiten100827.7zip";
        await RunThirdDayAsync(cargoJobs, runner, subRunner, detailRunner, crypticKey, accessCodes);
        Assert.IsFalse(File.Exists(webZipFile), "Web zip file exists.");
    }

    private async Task PrepareThirdDayAsync(CargoJobCollectionTestExecutionContext context, IJobRunner runner, ISubJobRunner subRunner, ISubJobDetailRunner detailRunner, CrypticKey crypticKey, Dictionary<string, Login> accessCodes, CargoString sampleRootFolder, CargoString sampleFileSystemRootFolder, List<Job> cargoJobs) {
        await PrepareSecondDayAsync(context, runner, subRunner, detailRunner, crypticKey, accessCodes, sampleRootFolder, sampleFileSystemRootFolder, cargoJobs);
        await InitCase456Async(context, sampleRootFolder, sampleFileSystemRootFolder, cargoJobs, @"\Traveller\Wamp\tank.php");
        await RunSecondDayAsync(cargoJobs, runner, subRunner, detailRunner, crypticKey, accessCodes);
    }

    private async Task RunThirdDayAsync(List<Job> cargoJobs, IJobRunner runner, ISubJobRunner subRunner, ISubJobDetailRunner detailRunner, CrypticKey crypticKey, Dictionary<string, Login> accessCodes) {
        var currentDate = new DateTime(2010, 8, 27);
        await RunJobAsync(cargoJobs, "ZipWamp", currentDate, runner, subRunner, detailRunner, crypticKey, accessCodes);
    }

    [TestMethod]
    public async Task CanProcessFourthDay() {
        await using var context = new CargoJobCollectionTestExecutionContext();
        await context.SetSampleRootFolderIfNecessaryAsync();

        var runner = new JobRunner();
        var subRunner = new SubJobRunner();
        var detailRunner = new SubJobDetailRunner(_Container.Resolve<ISecretRepository>());
        var crypticKeyProvider = new FakeCrypticKeyProvider();
        var crypticKey = crypticKeyProvider.GetCrypticKey("", "");
        var accessCodes = AccessCodes();
        var sampleRootFolder = new CargoString();
        var sampleFileSystemRootFolder = new CargoString();
        var cargoJobs = new List<Job>();
        await PrepareFourthDayAsync(context, runner, subRunner, detailRunner, crypticKey, accessCodes, sampleRootFolder, sampleFileSystemRootFolder, cargoJobs);
        await InitCase456Async(context, sampleRootFolder, sampleFileSystemRootFolder, cargoJobs, "");
        new Folder(sampleFileSystemRootFolder.Value).SubFolder("Traveller").SubFolder("Download").CreateIfNecessary();
        var downloadedWebZipFile = sampleFileSystemRootFolder.Value + @"\Traveller\Download\webseiten100825.7zip";
        Assert.IsFalse(File.Exists(downloadedWebZipFile), "Downloaded web zip exists.");
        var currentDate = new DateTime(2010, 8, 28);
        await RunJobAsync(cargoJobs, "ZipWamp", currentDate, runner, subRunner, detailRunner, crypticKey, accessCodes);
        await RunJobAsync(cargoJobs, "UploadZip", currentDate, runner, subRunner, detailRunner, crypticKey, accessCodes);
        await RunJobAsync(cargoJobs, "DownloadZip", currentDate, runner, subRunner, detailRunner, crypticKey, accessCodes);
        Assert.IsTrue(File.Exists(downloadedWebZipFile), "Downloaded web zip does not exist.");
    }

    private async Task PrepareFourthDayAsync(CargoJobCollectionTestExecutionContext context, IJobRunner runner, ISubJobRunner subRunner, ISubJobDetailRunner detailRunner, CrypticKey crypticKey, Dictionary<string, Login> accessCodes, CargoString sampleRootFolder, CargoString sampleFileSystemRootFolder, List<Job> cargoJobs) {
        await PrepareThirdDayAsync(context, runner, subRunner, detailRunner, crypticKey, accessCodes, sampleRootFolder, sampleFileSystemRootFolder, cargoJobs);
        await InitCase456Async(context, sampleRootFolder, sampleFileSystemRootFolder, cargoJobs, "");
        await RunThirdDayAsync(cargoJobs, runner, subRunner, detailRunner, crypticKey, accessCodes);
    }

    protected string RemoveVersionNumber(string s) {
        const string tag = "<!-- V=";
        if (!s.Contains(tag)) {
            return s;
        }

        var pos = s.IndexOf(tag, StringComparison.Ordinal);
        var endPos = s.IndexOf("-->\r\n", pos, StringComparison.Ordinal);
        s = s.Substring(0, pos) + s.Substring(endPos + 5);
        return s;
    }

    [TestMethod]
    public async Task CanGetSecretJobs() {
        var secretRepository = _Container.Resolve<ISecretRepository>();
        var secret = new CargoJobsSecret();
        var errorsAndInfos = new ErrorsAndInfos();
        var jobs = await secretRepository.GetAsync(secret, errorsAndInfos);
        Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsToString());
        Assert.IsNotNull(jobs);
        Assert.IsTrue(jobs.Count > 10, $"Only {jobs.Count} job/-s found");
        var jobWithSubJobs = jobs.FirstOrDefault(j => j.SubJobs.Count > 10);
        Assert.IsNotNull(jobWithSubJobs, "Excepted at least one job with more than 10 sub jobs");
    }
}

internal class CargoJobCollectionTestExecutionContext : IAsyncDisposable {
    private static readonly IContainer Container = new ContainerBuilder().UsePegh("Cargobay", new DummyCsArgumentPrompter()).Build();

    internal string SampleRootFolder { get; private set; }
    internal string SampleFileSystemRootFolder { get; private set; }

    internal async Task WriteAllTextAsync(string folder, string fileName, string contents) {
        await SetSampleRootFolderIfNecessaryAsync();

        CheckFolder(folder);
        await File.WriteAllTextAsync(folder + fileName, contents, Encoding.UTF8);
    }

    private async Task ResetFileSystemAsync(string folder) {
        await SetSampleRootFolderIfNecessaryAsync();

        CheckFolder(folder);
        var dirInfo = new DirectoryInfo(folder);
        foreach (var subDirInfo in dirInfo.GetDirectories()) {
            await ResetFileSystemAsync(subDirInfo.FullName + '\\');
        }
        foreach (var fileInfo in dirInfo.GetFiles("*.*")) {
            File.Delete(fileInfo.FullName);
        }
    }

    private void CheckFolder(string folder) {
        var error = CargoHelper.CheckFolder(folder, true, false);
        Assert.IsTrue(string.IsNullOrEmpty(error), error);
    }

    public async ValueTask DisposeAsync() {
        await ResetFileSystemAsync(false);
    }

    public async Task SetSampleRootFolderIfNecessaryAsync() {
        if (!string.IsNullOrEmpty(SampleRootFolder)) { return; }

        var errorsAndInfos = new ErrorsAndInfos();
        var result = (await Container.Resolve<IFolderResolver>().ResolveAsync(@"$(GitHub)\Cargobay\src\Samples", errorsAndInfos)).FullName;
        Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsToString());
        SampleRootFolder = result;
        SampleFileSystemRootFolder = result + @"\FileSystem";

        await ResetFileSystemAsync(true);
    }

    private async Task ResetFileSystemAsync(bool initialize) {
        var fileSystemRootFolder = SampleFileSystemRootFolder;
        await ResetFileSystemAsync(fileSystemRootFolder + '\\');
        if (!initialize) { return; }

        const string initialContents = "This is a test file in its initial state.";

        var folder = fileSystemRootFolder + @"\Traveller\Nessies\In Arbeit\";
        await WriteAllTextAsync(folder, "cargo.mxi", initialContents);
        await WriteAllTextAsync(folder, "cargo.mxt", initialContents);
        await WriteAllTextAsync(folder, "cargo.mxd", initialContents);
        await WriteAllTextAsync(folder, "cargo.001", initialContents);
        await WriteAllTextAsync(folder, "cargo.002", initialContents);
        folder = fileSystemRootFolder + @"\Traveller\Wamp\";
        await WriteAllTextAsync(folder, "cargo.php", initialContents);
        folder = fileSystemRootFolder + @"\Traveller\Wamp\temp\";
        await WriteAllTextAsync(folder, "cargo.css", initialContents);
        await WriteAllTextAsync(folder, "cargo.jpg", initialContents);
        folder = fileSystemRootFolder + @"\Traveller\Wamp\mid\";
        await WriteAllTextAsync(folder, "cargo.mid", initialContents);
    }

}