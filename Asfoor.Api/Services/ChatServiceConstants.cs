namespace Asfoor.Api.Services;

/// <summary>
/// Constants used by the ChatService for configuration and agent instructions.
/// </summary>
public static class ChatServiceConstants
{
    #region Configuration Keys
    
    /// <summary>
    /// Configuration key for the image processing model.
    /// </summary>
    public const string ImageModelConfigKey = "imageModel";
    
    /// <summary>
    /// Configuration key for the chat model.
    /// </summary>
    public const string ChatModelConfigKey = "chatModel";
    
    #endregion

    #region User Context
    
    /// <summary>
    /// Default user ID for context and memory storage.
    /// </summary>
    public const string DefaultUserId = "mrady_context_memory";
    
    #endregion

    #region Agent Instructions
    
    /// <summary>
    /// Instructions for the memory extraction agent.
    /// </summary>
    public const string MemoryAgentInstructions = 
        "Please examine the user’s message and identify any personal information or belongings or memories that are not already known to us. ";
    
    /// <summary>
    /// Instructions for the main chat agent with search capabilities.
    /// </summary>
    public const string ChatAgentInstructions = 
        "You are my personal assistant who answers questions my questions." +
        "Use only simple markdown to format your responses." +
        "Use search tool to find relevant information if applicable and try to rephrase the question to search by keywords in case you couldn't find result" +
        "Also if you got a question about image use the image tool to answer it" +
        ".When you do this, end your reply with citations in the special XML format:" +
        "<citation filename='string'>exact quote here</citation>" +
        "Always include the citation in your response if there are results." +
        "The quote must be max 5 words, taken word-for-word from the search result, and is the basis for why the citation is relevant." +
        "Don't refer to the presence of citations; just emit these tags right at the end, with no surrounding text. try to use the tools as much as possible.";
    
    /// <summary>
    /// Name of the chat agent.
    /// </summary>
    public const string ChatAgentName = "Asfor";
    
    #endregion

    #region Search Configuration
    
    /// <summary>
    /// Maximum number of search results to return.
    /// </summary>
    public const int MaxSearchResults = 5;
    
    #endregion

    #region Reasoning Configuration
    
    /// <summary>
    /// Default reasoning effort level for chat agents.
    /// </summary>
    public const string DefaultReasoningEffortLevel = "Medium";
    
    #endregion
}
