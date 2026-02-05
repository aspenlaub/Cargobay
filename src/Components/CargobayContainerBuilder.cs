using Aspenlaub.Net.GitHub.CSharp.Cargobay.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Cargobay.Jobz;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Components;
using Autofac;

namespace Aspenlaub.Net.GitHub.CSharp.Cargobay.Components;

public static class CargobayContainerBuilder {
    public static ContainerBuilder UseCargobay(this ContainerBuilder builder) {
        builder.UsePegh("Cargobay");
        builder.RegisterType<JobFolderAdjuster>().As<IJobFolderAdjuster>();
        builder.RegisterType<JobRunner>().As<IJobRunner>();
        builder.RegisterType<SubJobRunner>().As<ISubJobRunner>();
        builder.RegisterType<SubJobDetailRunner>().As<ISubJobDetailRunner>();
        return builder;
    }
}