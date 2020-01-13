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

namespace Aspenlaub.Net.GitHub.CSharp.Cargobay.Test {
    [TestClass]
    public class CargoJobCollectionTest {
        private readonly IContainer vContainer;

        public CargoJobCollectionTest() {
            vContainer = new ContainerBuilder().UseCargobay().Build();
        }

        [TestMethod]
        public void FoldersAreInPlace() {
            using (new CargoJobCollectionTestExecutionContext()) {
                var errorsAndInfos = new ErrorsAndInfos();
                var error = CargoHelper.CheckFolder(vContainer.Resolve<IFolderResolver>().Resolve(@"$(MainUserFolder)\Cargo.Samples", errorsAndInfos).FullName + "\\", true, false);
                Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsToString());
                Assert.IsTrue(error.Length != 0, "Backslash at end allowed");
                error = CargoHelper.CheckFolder(vContainer.Resolve<IFolderResolver>().Resolve(@"$(MainUserFolder)\Cargo.Samples", errorsAndInfos).FullName + "\\", true, false);
                Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsToString());
                Assert.IsTrue(error.Length != 0, "Double backslash allowed");
                error = CargoHelper.CheckFolder(vContainer.Resolve<IFolderResolver>().Resolve(@"$(MainUserFolder)", errorsAndInfos).FullName, true, false);
                Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsToString());
                Assert.IsTrue(error.Length != 0, "Path outside playground is allowed");
            }
        }

        [TestMethod]
        public void CanSaveSimpleJobCollection() {
            using var context = new CargoJobCollectionTestExecutionContext();
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
            var adjuster = vContainer.Resolve<IJobFolderAdjuster>();
            adjuster.AdjustJobAndSubFolders(job, errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsToString());
            jobs.Add(job);
            jobs.Add(job);
            var destFolder = context.SampleRootFolder + @"\Log\";
            const string destFile = "SaveSimple.xml";
            File.Delete(destFolder + destFile);
            Assert.IsTrue(jobs.Save(vContainer.Resolve<IXmlSerializer>(), destFolder + destFile));
            errorsAndInfos = new ErrorsAndInfos();
            var jobsRev = JobsExtensions.Load(vContainer.Resolve<IXmlDeserializer>(), vContainer.Resolve<IJobFolderAdjuster>(), destFolder, destFile, errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsToString());
            Assert.IsTrue(jobsRev.Count == 2);
            File.Delete(destFolder + destFile);
        }

        [TestMethod]
        public void CanLoadAndSaveSample() {
            using var context = new CargoJobCollectionTestExecutionContext();
            var sampleRootFolder = context.SampleRootFolder;
            var sourceFolder = sampleRootFolder + @"\";
            const string sourceFile = "CargoJobs1.xml";
            var destFile = sampleRootFolder + @"\Log\CargoJobs1.xml";
            File.Delete(destFile);
            var errorsAndInfos = new ErrorsAndInfos();
            var cargoJobs = JobsExtensions.Load(vContainer.Resolve<IXmlDeserializer>(), vContainer.Resolve<IJobFolderAdjuster>(), sourceFolder, sourceFile, errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsToString());
            Assert.IsTrue(cargoJobs.Count == 4, "Four jobs expected, read " + cargoJobs.Count);
            Assert.IsTrue(cargoJobs[0].SubJobs.Count > 20);
            Assert.IsTrue(cargoJobs[0].SubJobs[0].LogicalFolder.Length > 5);
            cargoJobs.Save(vContainer.Resolve<IXmlSerializer>(), destFile);
            var sourceContents = RemoveVersionNumber(File.ReadAllText(sourceFolder + sourceFile, Encoding.UTF8));
            Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsToString());
            const string search = "xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"";
            const string replace = "xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"";
            var destinationContents = File.ReadAllText(destFile, Encoding.UTF8).Replace(search, replace);
            int i;
            for (i = 0; i < sourceContents.Length && sourceContents[i] == destinationContents[i]; i++) { }

            Assert.AreEqual(sourceContents.Substring(i), destinationContents.Substring(i), "Source and destination file differ");
            File.Delete(destFile);
        }

        private void InitCase456(CargoJobCollectionTestExecutionContext context, out string sampleRootFolder, out string sampleFileSystemRootFolder, out List<Job> cargoJobs, string addFileName) {
            sampleRootFolder = context.SampleRootFolder;
            sampleFileSystemRootFolder = context.SampleFileSystemRootFolder;
            if (addFileName.Length != 0) {
                addFileName = sampleFileSystemRootFolder + addFileName;
                File.WriteAllText(addFileName, @"This is a file that was added.", Encoding.UTF8);
            }
            var sourceFolder = sampleRootFolder + @"\";
            const string sourceFile = "CargoJobs2.xml";
            var errorsAndInfos = new ErrorsAndInfos();
            var deserializedJobs = JobsExtensions.Load(vContainer.Resolve<IXmlDeserializer>(), vContainer.Resolve<IJobFolderAdjuster>(), sourceFolder, sourceFile, errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsToString());
            cargoJobs = deserializedJobs;
        }


        private Dictionary<string, Login> AccessCodes() {
            var accessCodes = new Dictionary<string, Login> {[@"ftp://ftp.localhost"] = new Login { Identification = "guest", Password = "guest" }};
            return accessCodes;
        }

        private void RunJob(List<Job> cargoJobs, string name, DateTime currentDate, IJobRunner runner, ISubJobRunner subRunner, ISubJobDetailRunner detailRunner, CrypticKey crypticKey, Dictionary<string, Login> accessCodes) {
            var nextJob = cargoJobs.Find(x => x.Name == name);
            Assert.IsNotNull(nextJob, "Job '" + name + "' not found");
            Assert.IsTrue(runner.Run(nextJob, currentDate, new FakeCommandExecutionContext(), subRunner, detailRunner, crypticKey, accessCodes), "Job '" + name + "' could not be processed");
        }

        [TestMethod]
        public void CanProcessFirstDay() {
            using var context = new CargoJobCollectionTestExecutionContext();
            InitCase456(context, out _, out var sampleFileSystemRootFolder, out var cargoJobs, "");
            var webZipFile = sampleFileSystemRootFolder + @"\Traveller\Webdev\webseiten100825.7zip";
            Assert.IsFalse(File.Exists(webZipFile), "Web zip file exists.");
            var crypticKeyProvider = new FakeCrypticKeyProvider();
            var crypticKey = crypticKeyProvider.GetCrypticKey("", "");
            var accessCodes = AccessCodes();
            RunFirstDay(cargoJobs, new JobRunner(), new SubJobRunner(), new SubJobDetailRunner(), crypticKey, accessCodes);
            Assert.IsTrue(File.Exists(webZipFile), "Web zip file does not exist.");
        }

        private void RunFirstDay(List<Job> cargoJobs, IJobRunner runner, ISubJobRunner subRunner, ISubJobDetailRunner detailRunner, CrypticKey crypticKey, Dictionary<string, Login> accessCodes) {
            var currentDate = new DateTime(2010, 8, 25);
            RunJob(cargoJobs, "CleanUpWeb", currentDate, runner, subRunner, detailRunner, crypticKey, accessCodes);
            RunJob(cargoJobs, "ZipWamp", currentDate, runner, subRunner, detailRunner, crypticKey, accessCodes);
            RunJob(cargoJobs, "ArchiveNessies", currentDate, runner, subRunner, detailRunner, crypticKey, accessCodes);
            RunJob(cargoJobs, "CleanUpNessies", currentDate, runner, subRunner, detailRunner, crypticKey, accessCodes);
        }

        [TestMethod]
        public void CanProcessSecondDay() {
            using var context = new CargoJobCollectionTestExecutionContext();
            var runner = new JobRunner();
            var subRunner = new SubJobRunner();
            var detailRunner = new SubJobDetailRunner();
            var crypticKeyProvider = new FakeCrypticKeyProvider();
            var crypticKey = crypticKeyProvider.GetCrypticKey("", "");
            var accessCodes = AccessCodes();
            PrepareSecondDay(context, runner, subRunner, detailRunner, crypticKey, accessCodes, out _, out var sampleFileSystemRootFolder, out var cargoJobs);
            InitCase456(context, out _, out sampleFileSystemRootFolder, out cargoJobs, @"\Traveller\Wamp\tank.php");
            var webZipFile = sampleFileSystemRootFolder + @"\Traveller\Webdev\webseiten100825.7zip";
            Assert.IsTrue(File.Exists(webZipFile), "Preceding test case failed.");
            webZipFile = sampleFileSystemRootFolder + @"\Traveller\Webdev\webseiten100826.7zip";
            var uploadWebZipFile = sampleFileSystemRootFolder + @"\Traveller\Wamp\download\webseiten100825.7zip";
            Assert.IsFalse(File.Exists(webZipFile), "Web zip file exists.");
            Assert.IsFalse(File.Exists(uploadWebZipFile), "Uploaded web zip file exists.");
            RunSecondDay(cargoJobs, runner, subRunner, detailRunner, crypticKey, accessCodes);
            Assert.IsTrue(File.Exists(webZipFile), "Web zip file does not exist.");
            Assert.IsTrue(File.Exists(uploadWebZipFile), "Uploaded web zip file does not exist.");
        }

        private void PrepareSecondDay(CargoJobCollectionTestExecutionContext context, IJobRunner runner, ISubJobRunner subRunner, ISubJobDetailRunner detailRunner, CrypticKey crypticKey, Dictionary<string, Login> accessCodes, out string sampleRootFolder, out string sampleFileSystemRootFolder, out List<Job> cargoJobs) {
            InitCase456(context, out sampleRootFolder, out sampleFileSystemRootFolder, out cargoJobs, "");
            RunFirstDay(cargoJobs, runner, subRunner, detailRunner, crypticKey, accessCodes);
        }

        private void RunSecondDay(List<Job> cargoJobs, IJobRunner runner, ISubJobRunner subRunner, ISubJobDetailRunner detailRunner, CrypticKey crypticKey, Dictionary<string, Login> accessCodes) {
            var currentDate = new DateTime(2010, 8, 26);
            RunJob(cargoJobs, "ZipWamp", currentDate, runner, subRunner, detailRunner, crypticKey, accessCodes);
            RunJob(cargoJobs, "UploadZip", currentDate, runner, subRunner, detailRunner, crypticKey, accessCodes);
        }

        [TestMethod]
        public void CanProcessThirdDay() {
            using var context = new CargoJobCollectionTestExecutionContext();
            var runner = new JobRunner();
            var subRunner = new SubJobRunner();
            var detailRunner = new SubJobDetailRunner();
            var crypticKeyProvider = new FakeCrypticKeyProvider();
            var crypticKey = crypticKeyProvider.GetCrypticKey("", "");
            var accessCodes = AccessCodes();
            PrepareThirdDay(context, runner, subRunner, detailRunner, crypticKey, accessCodes, out _, out var sampleFileSystemRootFolder, out var cargoJobs);
            File.Delete(context.SampleFileSystemRootFolder + @"\Traveller\Wamp\download\webseiten100825.7zip");
            File.Delete(context.SampleFileSystemRootFolder + @"\Traveller\Wamp\download\webseiten100826.7zip");
            InitCase456(context, out _, out sampleFileSystemRootFolder, out cargoJobs, "");
            var webZipFile = sampleFileSystemRootFolder + @"\Traveller\Webdev\webseiten100826.7zip";
            Assert.IsTrue(File.Exists(webZipFile), "Preceding test case failed.");
            webZipFile = sampleFileSystemRootFolder + @"\Traveller\Webdev\webseiten100827.7zip";
            RunThirdDay(cargoJobs, runner, subRunner, detailRunner, crypticKey, accessCodes);
            Assert.IsFalse(File.Exists(webZipFile), "Web zip file exists.");
        }

        private void PrepareThirdDay(CargoJobCollectionTestExecutionContext context, IJobRunner runner, ISubJobRunner subRunner, ISubJobDetailRunner detailRunner, CrypticKey crypticKey, Dictionary<string, Login> accessCodes, out string sampleRootFolder, out string sampleFileSystemRootFolder, out List<Job> cargoJobs) {
            PrepareSecondDay(context, runner, subRunner, detailRunner, crypticKey, accessCodes, out sampleRootFolder, out sampleFileSystemRootFolder, out cargoJobs);
            InitCase456(context, out sampleRootFolder, out sampleFileSystemRootFolder, out cargoJobs, @"\Traveller\Wamp\tank.php");
            RunSecondDay(cargoJobs, runner, subRunner, detailRunner, crypticKey, accessCodes);
        }

        private void RunThirdDay(List<Job> cargoJobs, IJobRunner runner, ISubJobRunner subRunner, ISubJobDetailRunner detailRunner, CrypticKey crypticKey, Dictionary<string, Login> accessCodes) {
            var currentDate = new DateTime(2010, 8, 27);
            RunJob(cargoJobs, "ZipWamp", currentDate, runner, subRunner, detailRunner, crypticKey, accessCodes);
        }

        [TestMethod]
        public void CanProcessFourthDay() {
            using var context = new CargoJobCollectionTestExecutionContext();
            var runner = new JobRunner();
            var subRunner = new SubJobRunner();
            var detailRunner = new SubJobDetailRunner();
            var crypticKeyProvider = new FakeCrypticKeyProvider();
            var crypticKey = crypticKeyProvider.GetCrypticKey("", "");
            var accessCodes = AccessCodes();
            PrepareFourthDay(context, runner, subRunner, detailRunner, crypticKey, accessCodes, out _, out var sampleFileSystemRootFolder, out var cargoJobs);
            InitCase456(context, out _, out sampleFileSystemRootFolder, out cargoJobs, "");
            var downloadedWebZipFile = sampleFileSystemRootFolder + @"\Traveller\Download\webseiten100825.7zip";
            Assert.IsFalse(File.Exists(downloadedWebZipFile), "Downloaded web zip exists.");
            var currentDate = new DateTime(2010, 8, 28);
            RunJob(cargoJobs, "ZipWamp", currentDate, runner, subRunner, detailRunner, crypticKey, accessCodes);
            RunJob(cargoJobs, "UploadZip", currentDate, runner, subRunner, detailRunner, crypticKey, accessCodes);
            RunJob(cargoJobs, "DownloadZip", currentDate, runner, subRunner, detailRunner, crypticKey, accessCodes);
            Assert.IsTrue(File.Exists(downloadedWebZipFile), "Downloaded web zip does not exist.");
        }

        private void PrepareFourthDay(CargoJobCollectionTestExecutionContext context, IJobRunner runner, ISubJobRunner subRunner, ISubJobDetailRunner detailRunner, CrypticKey crypticKey, Dictionary<string, Login> accessCodes, out string sampleRootFolder, out string sampleFileSystemRootFolder, out List<Job> cargoJobs) {
            PrepareThirdDay(context, runner, subRunner, detailRunner, crypticKey, accessCodes, out sampleRootFolder, out sampleFileSystemRootFolder, out cargoJobs);
            InitCase456(context, out sampleRootFolder, out sampleFileSystemRootFolder, out cargoJobs, "");
            RunThirdDay(cargoJobs, runner, subRunner, detailRunner, crypticKey, accessCodes);
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
            var secretRepository = vContainer.Resolve<ISecretRepository>();
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

    internal class CargoJobCollectionTestExecutionContext : IDisposable {
        private static readonly IContainer Container = new ContainerBuilder().UsePegh(new DummyCsArgumentPrompter()).Build();

        internal string SampleRootFolder {
            get {
                var errorsAndInfos = new ErrorsAndInfos();
                var result = Container.Resolve<IFolderResolver>().Resolve(@"$(GitHub)\Cargobay\src\Samples", errorsAndInfos).FullName;
                Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsToString());
                return result;
            }
        }

        internal string SampleFileSystemRootFolder => SampleRootFolder + @"\FileSystem";

        internal CargoJobCollectionTestExecutionContext() {
            ResetFileSystem(true);
        }

        internal void ResetFileSystem(bool initialize) {
            var fileSystemRootFolder = SampleFileSystemRootFolder;
            ResetFileSystem(fileSystemRootFolder + '\\');
            if (!initialize) { return; }

            const string initialContents = "This is a test file in its initial state.";

            var folder = fileSystemRootFolder + @"\Traveller\Nessies\In Arbeit\";
            WriteAllText(folder, "cargo.mxi", initialContents);
            WriteAllText(folder, "cargo.mxt", initialContents);
            WriteAllText(folder, "cargo.mxd", initialContents);
            WriteAllText(folder, "cargo.001", initialContents);
            WriteAllText(folder, "cargo.002", initialContents);
            folder = fileSystemRootFolder + @"\Traveller\Wamp\";
            WriteAllText(folder, "cargo.php", initialContents);
            folder = fileSystemRootFolder + @"\Traveller\Wamp\temp\";
            WriteAllText(folder, "cargo.css", initialContents);
            WriteAllText(folder, "cargo.jpg", initialContents);
            folder = fileSystemRootFolder + @"\Traveller\Wamp\mid\";
            WriteAllText(folder, "cargo.mid", initialContents);
        }

        internal void WriteAllText(string folder, string fileName, string contents) {
            CheckFolder(folder);
            File.WriteAllText(folder + fileName, contents, Encoding.UTF8);
        }

        internal void ResetFileSystem(string folder) {
            CheckFolder(folder);
            var dirInfo = new DirectoryInfo(folder);
            foreach (var subDirInfo in dirInfo.GetDirectories()) {
                ResetFileSystem(subDirInfo.FullName + '\\');
            }
            foreach (var fileInfo in dirInfo.GetFiles("*.*")) {
                File.Delete(fileInfo.FullName);
            }
        }

        internal void CheckFolder(string folder) {
            var error = CargoHelper.CheckFolder(folder, true, false);
            Assert.IsTrue(error.Length == 0, error);
        }

        public void Dispose() {
            ResetFileSystem(false);
        }
    }
}