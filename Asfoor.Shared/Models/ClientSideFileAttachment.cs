using Microsoft.AspNetCore.Components.Forms;

namespace Asfoor.Shared.Models;

public class ClientSideFileAttachment
{
    public string Name { get; set; } = "";
    public long Size { get; set; }
    public string ContentType { get; set; } = "";
    public required IBrowserFile BrowserFile { get; set; }
}
