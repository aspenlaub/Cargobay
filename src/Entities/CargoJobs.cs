using System.Collections.Generic;
using System.Xml.Serialization;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Cargobay.Entities {
    [XmlRoot("CargoJobs", Namespace = "http://www.aspenlaub.net")]
    public class CargoJobs : List<Job>, ISecretResult<CargoJobs> {
        public CargoJobs Clone() {
            var clone = new CargoJobs();
            clone.AddRange(this);
            return clone;
        }
    }
}
