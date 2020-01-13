using System;
using System.Collections.Generic;
using Aspenlaub.Net.GitHub.CSharp.Cargobay.Entities;
using Aspenlaub.Net.GitHub.CSharp.Vishizhukel.Interfaces.Application;

namespace Aspenlaub.Net.GitHub.CSharp.Cargobay.Interfaces {
    public interface ISubJobDetailRunner {
        void Preview(SubJobDetail subJobDetail, IApplicationCommandExecutionContext context);
        bool Run(SubJobDetail subJobDetail, DateTime today, Job job, SubJob subJob, IApplicationCommandExecutionContext context, CrypticKey crypticKey, Dictionary<string, Login> accessCodes);
    }
}