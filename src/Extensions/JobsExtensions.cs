using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Cargobay.Entities;
using Aspenlaub.Net.GitHub.CSharp.Cargobay.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Skladasu.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Cargobay.Extensions;

public static class JobsExtensions {
    public static async Task<CargoJobs> LoadAsync(IXmlDeserializer xmlDeserializer, IJobFolderAdjuster jobFolderAdjuster, string folder, string utf8FileName, IErrorsAndInfos errorsAndInfos) {
        var fileName = ReplaceInvalidCharacters(utf8FileName);
        if (!File.Exists(folder + fileName)) { return new CargoJobs(); }

        var jobs = xmlDeserializer.Deserialize<CargoJobs>(await File.ReadAllTextAsync(folder + fileName, Encoding.UTF8));
        foreach (var job in jobs) {
            await jobFolderAdjuster.AdjustJobAndSubFoldersAsync(job, errorsAndInfos);
        }
        return jobs;
    }

    private static string ReplaceInvalidCharacters(string utf8FileName) {
        return Path.GetInvalidFileNameChars().Aggregate(utf8FileName, (current, c) => current.Replace(c, '_'));
    }

    public static bool Save(this CargoJobs jobs, IXmlSerializer xmlSerializer, string fileName) {
        if (fileName == null || File.Exists(fileName)) { return false; }

        var xml = xmlSerializer.Serialize(jobs);
        File.WriteAllText(fileName, xml, Encoding.UTF8);
        return true;
    }
}