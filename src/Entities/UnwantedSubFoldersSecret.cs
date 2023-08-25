using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Cargobay.Entities {
    public class UnwantedSubFoldersSecret : ISecret<UnwantedSubFolders> {
        private UnwantedSubFolders _DefaultJobs;
        public UnwantedSubFolders DefaultValue => _DefaultJobs ??= new UnwantedSubFolders();

        public string Guid => "A68A07D9-BDDD-4438-9350-3AA8BAB390DD";
    }
}
