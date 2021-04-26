using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Cargobay.Entities;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Cargobay.Interfaces {
    public interface IJobFolderAdjuster {
        Task AdjustJobAndSubFoldersAsync(Job job, IErrorsAndInfos errorsAndInfos);
    }
}
