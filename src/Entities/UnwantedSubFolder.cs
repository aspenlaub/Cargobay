using Aspenlaub.Net.GitHub.CSharp.Cargobay.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;
using System.Xml.Serialization;

namespace Aspenlaub.Net.GitHub.CSharp.Cargobay.Entities;

public class UnwantedSubFolder : IGuid, ISetGuid, IUnwantedSubFolder {

    [XmlAttribute("guid")]
    public string Guid { get; set; }

    [XmlAttribute("subfolder")]
    public string SubFolder { get; set; }
}