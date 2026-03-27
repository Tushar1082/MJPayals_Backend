using System.Xml.Serialization;

using DotnetBoilerplate.Models.Entities;

namespace DotnetBoilerplate.Models.Helpers;

public static class PolyDetailXmlHelper
{
    public static string ToXml(List<PolyDetail> details)
    {
        var serializer = new XmlSerializer(
            typeof(List<PolyDetail>),
            new XmlRootAttribute("PolyDetails")
        );

        using var writer = new StringWriter();
        serializer.Serialize(writer, details);
        return writer.ToString();
    }

    public static List<PolyDetail>? FromXml(string? xml)
    {
        if (string.IsNullOrWhiteSpace(xml))
            return null; // ✅ VERY IMPORTANT

        var serializer = new XmlSerializer(
            typeof(List<PolyDetail>),
            new XmlRootAttribute("PolyDetails")
        );

        using var reader = new StringReader(xml);
        return (List<PolyDetail>)serializer.Deserialize(reader)!;
    }

}
