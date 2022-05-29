using System;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Cargobay.Entities;

public class CargoJobsSecret : ISecret<CargoJobs> {
    private CargoJobs DefaultJobs;
    public CargoJobs DefaultValue => DefaultJobs ??= new CargoJobs {
        new() { Guid = System.Guid.NewGuid().ToString(), JobType = CargoJobType.CleanUp, Machine = Environment.MachineName }
    };

    public string Guid => "364603C7-D91E-4DD6-AD72-7113F7CDA64E";
}