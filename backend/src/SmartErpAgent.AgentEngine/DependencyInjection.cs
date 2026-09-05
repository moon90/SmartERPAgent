using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartErpAgent.AgentEngine.Plugins;
using SmartErpAgent.AgentEngine.Services;
using SmartErpAgent.Application.Common.Interfaces;

namespace SmartErpAgent.AgentEngine;

public static class AgentEngineServiceCollectionExtensions
{
    /// <summary>
    /// Cleanly registers Semantic Kernel plugins and orchestrator services into the DI container.
    /// </summary>
    public static IServiceCollection AddAgentEngine(
        this IServiceCollection services,
        IConfiguration? configuration = null)
    {
        services.AddScoped<InvoiceAgentPlugin>();
        services.AddScoped<InventoryAgentPlugin>();
        services.AddScoped<IAgentOrchestrator, SemanticKernelAgentOrchestrator>();

        return services;
    }
}
