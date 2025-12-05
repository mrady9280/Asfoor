namespace Asfoo.Models;

public class ChatFileAttachment
{
    public string Name { get; set; } = "";
    public string ContentType { get; set; } = "";
    public byte[] Data { get; set; } = Array.Empty<byte>();
}