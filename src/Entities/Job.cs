using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Xml.Serialization;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Cargobay.Entities;

public class Job : IGuid, ISetGuid {

    [XmlAttribute("guid")]
    public string Guid { get; set; }

    [XmlElement("SubJob")]
    public ObservableCollection<SubJob> SubJobs { get; set; }

    [XmlAttribute("name")]
    public string Name { get; set; }

    [XmlAttribute("description")]
    public string Description { get; set; }

    [XmlAttribute("type")]
    public CargoJobType JobType { get; set; }

    [XmlAttribute("folder"), DefaultValue("")]
    public string LogicalFolder { get; set; }

    [XmlAttribute("destinationfolder"), DefaultValue("")]
    public string LogicalDestinationFolder { get; set; }

    [XmlAttribute("machine"), DefaultValue("")]
    public string Machine { get; set; }

    [XmlAttribute("secondarymachine"), DefaultValue("")]
    public string SecondaryMachine { get; set; }

    [XmlAttribute("url"), DefaultValue("")]
    public string Url { get; set; }

    [XmlIgnore]
    public FolderAdjustmentState FolderAdjustmentState { get; set; }

    private string _PrivateAdjustedFolder;
    [XmlIgnore]
    public string AdjustedFolder {
        get {
            if (FolderAdjustmentState == FolderAdjustmentState.NotAdjusted) {
                throw new Exception("Job folder has not been adjusted");
            }
            return _PrivateAdjustedFolder;
        }
        set => _PrivateAdjustedFolder = value;
    }

    private string _PrivateAdjustedDestinationFolder;
    [XmlIgnore]
    public string AdjustedDestinationFolder {
        get {
            if (FolderAdjustmentState == FolderAdjustmentState.NotAdjusted) {
                throw new Exception("Job destination folder has not been adjusted");
            }
            return _PrivateAdjustedDestinationFolder;
        }
        set => _PrivateAdjustedDestinationFolder = value;
    }

    public Job() {
        Guid = System.Guid.NewGuid().ToString();
        Name = string.Empty;
        Description = string.Empty;
        JobType = CargoJobType.None;
        LogicalFolder = string.Empty;
        LogicalDestinationFolder = string.Empty;
        SubJobs = new ObservableCollection<SubJob>();
        Machine = string.Empty;
        SecondaryMachine = string.Empty;
        FolderAdjustmentState = FolderAdjustmentState.NotAdjusted;
        AdjustedFolder = string.Empty;
        AdjustedDestinationFolder = string.Empty;
    }

    public string SortValue() {
        var s = Name;
        s = s.Replace("Archive", "");
        s = s.Replace("CleanUp", "");
        s = s.Replace("Upload", "");
        s = s.Replace("Zip", "");
        return s;
    }
}