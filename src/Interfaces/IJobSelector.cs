using Aspenlaub.Net.GitHub.CSharp.Cargobay.Entities;

namespace Aspenlaub.Net.GitHub.CSharp.Cargobay.Interfaces {
    public interface IJobSelector {
        Job SelectedJob { get; }
    }
}
