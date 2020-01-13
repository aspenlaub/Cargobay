using Aspenlaub.Net.GitHub.CSharp.Cargobay.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Components;
using Autofac;

namespace Aspenlaub.Net.GitHub.CSharp.Cargobay.Components {
    public static class CargobayContainerBuilder {
        public static ContainerBuilder UseCargobay(this ContainerBuilder builder) {
            builder.UsePegh(new DummyCsArgumentPrompter());
            builder.RegisterType<JobFolderAdjuster>().As<IJobFolderAdjuster>();
            return builder;
        }
    }
}
