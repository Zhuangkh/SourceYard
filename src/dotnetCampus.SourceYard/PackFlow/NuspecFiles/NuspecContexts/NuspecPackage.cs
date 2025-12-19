using System.Xml.Serialization;

namespace dotnetCampus.SourceYard.PackFlow.NuspecFiles.NuspecContexts
{
    [XmlRoot("package", Namespace = "http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd")]
    public class NuspecPackage
    {
        [XmlElement("metadata")]
        public NuspecMetadata? NuspecMetadata { get; set; }
    }
}