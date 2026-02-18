using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Cargobay.Entities;
using Aspenlaub.Net.GitHub.CSharp.Skladasu.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Cargobay.Interfaces;

public interface IJobFolderAdjuster {
    Task AdjustJobAndSubFoldersAsync(Job job, IErrorsAndInfos errorsAndInfos);
}