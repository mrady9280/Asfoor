namespace Asfoo.Models;

public record CustomChatResponse(
    string Response = "",
    string ThreadString = "",
    string Usage = "",
    string Thinking = "");