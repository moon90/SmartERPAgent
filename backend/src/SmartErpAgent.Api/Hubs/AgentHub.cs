using Microsoft.AspNetCore.SignalR;
using SmartErpAgent.Application.Common.Interfaces;

namespace SmartErpAgent.Api.Hubs;

/// <summary>
/// Real-time SignalR Hub providing bi-directional communication between the Angular frontend
/// and the Semantic Kernel AI agent orchestrator.
/// </summary>
public class AgentHub : SmartErpAgent.Application.Hubs.AgentHub
{
    private readonly IAgentOrchestrator _agentOrchestrator;
    private readonly ILogger<AgentHub> _logger;

    public AgentHub(
        IAgentOrchestrator agentOrchestrator,
        ILogger<AgentHub> logger)
    {
        _agentOrchestrator = agentOrchestrator;
        _logger = logger;
    }

    /// <summary>
    /// Invoked by the client to dispatch an inquiry to the agent copilot.
    /// Intermediate planner thought processes are streamed via "ReceiveThoughtProcess",
    /// and the final response is returned and broadcast to the caller via "ReceiveFinalResponse".
    /// </summary>
    /// <param name="prompt">The natural language prompt submitted by the user.</param>
    public async Task<string> SendPrompt(string prompt)
    {
        _logger.LogInformation("AgentHub received prompt from connection {ConnectionId}: {Prompt}",
            Context?.ConnectionId ?? "unknown", prompt);

        if (string.IsNullOrWhiteSpace(prompt))
        {
            await Clients.Caller.SendAsync("ReceiveThoughtProcess", "Empty prompt rejected.");
            await Clients.Caller.SendAsync("ReceiveFinalResponse", "Prompt cannot be empty.");
            return "Prompt cannot be empty.";
        }

        try
        {
            var finalResponse = await _agentOrchestrator.ExecutePromptAsync(prompt);
            await Clients.Caller.SendAsync("ReceiveFinalResponse", finalResponse);
            return finalResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error orchestrating agent prompt in AgentHub.");
            var errorNotice = $"An error occurred while orchestrating the agent: {ex.Message}";
            await Clients.Caller.SendAsync("ReceiveThoughtProcess", $"Execution failed: {ex.Message}");
            await Clients.Caller.SendAsync("ReceiveFinalResponse", errorNotice);
            return errorNotice;
        }
    }
}

/// <summary>
/// Bridges the application-layer AgentHub context to the concrete API presentation hub context.
/// </summary>
public class AgentHubContextBridge : IHubContext<SmartErpAgent.Application.Hubs.AgentHub>
{
    private readonly IHubContext<AgentHub> _apiHubContext;

    public AgentHubContextBridge(IHubContext<AgentHub> apiHubContext)
    {
        _apiHubContext = apiHubContext;
    }

    public IHubClients Clients => _apiHubContext.Clients;
    public IGroupManager Groups => _apiHubContext.Groups;
}
