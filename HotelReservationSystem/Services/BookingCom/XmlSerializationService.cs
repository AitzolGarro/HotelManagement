using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace HotelReservationSystem.Services.BookingCom;

public interface IXmlSerializationService
{
    string Serialize<T>(T obj) where T : class;
    T Deserialize<T>(string xml) where T : class;
    bool TryDeserialize<T>(string xml, out T? result) where T : class;
}

public class XmlSerializationService : IXmlSerializationService
{
    private readonly ILogger<XmlSerializationService> _logger;
    private readonly Dictionary<Type, XmlSerializer> _serializers = new();

    public XmlSerializationService(ILogger<XmlSerializationService> logger)
    {
        _logger = logger;
    }

    public string Serialize<T>(T obj) where T : class
    {
        if (obj == null)
            throw new ArgumentNullException(nameof(obj));

        try
        {
            var serializer = GetSerializer<T>();
            using var stringWriter = new StringWriter();
            using var xmlWriter = XmlWriter.Create(stringWriter, new XmlWriterSettings
            {
                Indent = true,
                Encoding = Encoding.UTF8,
                OmitXmlDeclaration = false
            });

            serializer.Serialize(xmlWriter, obj);
            var result = stringWriter.ToString();
            
            _logger.LogDebug("Successfully serialized {Type} to XML", typeof(T).Name);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to serialize {Type} to XML", typeof(T).Name);
            throw new InvalidOperationException($"Failed to serialize {typeof(T).Name} to XML", ex);
        }
    }

    public T Deserialize<T>(string xml) where T : class
    {
        if (string.IsNullOrWhiteSpace(xml))
            throw new ArgumentException("XML content cannot be null or empty", nameof(xml));

        try
        {
            var serializer = GetSerializer<T>();
            using var stringReader = new StringReader(xml);
            using var xmlReader = XmlReader.Create(stringReader);

            var result = (T)serializer.Deserialize(xmlReader)!;
            
            _logger.LogDebug("Successfully deserialized XML to {Type}", typeof(T).Name);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deserialize XML to {Type}. XML content: {Xml}", typeof(T).Name, xml);
            throw new InvalidOperationException($"Failed to deserialize XML to {typeof(T).Name}", ex);
        }
    }

    public bool TryDeserialize<T>(string xml, out T? result) where T : class
    {
        result = null;
        
        if (string.IsNullOrWhiteSpace(xml))
        {
            _logger.LogWarning("Cannot deserialize null or empty XML content");
            return false;
        }

        try
        {
            result = Deserialize<T>(xml);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize XML to {Type}", typeof(T).Name);
            return false;
        }
    }

    private XmlSerializer GetSerializer<T>()
    {
        var type = typeof(T);
        
        if (!_serializers.TryGetValue(type, out var serializer))
        {
            serializer = new XmlSerializer(type);
            _serializers[type] = serializer;
        }

        return serializer;
    }
}