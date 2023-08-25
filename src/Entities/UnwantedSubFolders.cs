using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Aspenlaub.Net.GitHub.CSharp.Cargobay.Entities;

[XmlRoot("UnwantedSubFolders", Namespace = "http://www.aspenlaub.net")]
public class UnwantedSubFolders : List<UnwantedSubFolder>, ISecretResult<UnwantedSubFolders> {
    public UnwantedSubFolders Clone() {
        var clone = new UnwantedSubFolders();
        clone.AddRange(this);
        return clone;
    }
}