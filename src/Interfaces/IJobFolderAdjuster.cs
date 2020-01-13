using Aspenlaub.Net.GitHub.CSharp.Cargobay.Entities;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Cargobay.Interfaces {
    public interface IJobFolderAdjuster {
        void AdjustJobAndSubFolders(Job job, IErrorsAndInfos errorsAndInfos);
    }
}
