using SmartErpAgent.Application.DTOs;

namespace SmartErpAgent.Application.Common.Interfaces;

public interface IAgentOrchestrator
{
    /// <summary>
    /// Executes a natural language prompt and returns the synthesized response text.
    /// </summary>
    Task<string> ExecutePromptAsync(string prompt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a structured agent prompt request and returns detailed execution metadata.
    /// </summary>
    Task<AgentResponseDto> ProcessPromptAsync(AgentPromptRequestDto request, CancellationToken cancellationToken = default);
}
