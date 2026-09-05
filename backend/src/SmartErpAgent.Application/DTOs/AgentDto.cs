namespace SmartErpAgent.Application.DTOs;

public record AgentPromptRequestDto(
    string Prompt,
    Guid? TenantId = null,
    string? SessionId = null,
    Dictionary<string, object>? ContextParameters = null
);

public record AgentRequestDto(
    string Prompt,
    string? SessionId = null
);

public record AgentResponseDto(
    string Content,
    string? ThoughtProcess = null,
    List<AgentActionExecutedDto>? ExecutedActions = null,
    string Status = "Success"
);

public record AgentActionExecutedDto(
    string PluginName,
    string FunctionName,
    string ArgumentsJson,
    string ResultJson
);
