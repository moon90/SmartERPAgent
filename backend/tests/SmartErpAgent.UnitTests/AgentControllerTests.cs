using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using SmartErpAgent.AgentEngine;
using SmartErpAgent.Api.Controllers;
using SmartErpAgent.Application.Common.Interfaces;
using SmartErpAgent.Application.DTOs;
using SmartErpAgent.Infrastructure.Persistence;
using SmartErpAgent.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using SmartErpAgent.Api.Hubs;

namespace SmartErpAgent.UnitTests;

public class AgentControllerTests
{
    private class MockAgentOrchestrator : IAgentOrchestrator
    {
        public Task<string> ExecutePromptAsync(string prompt, CancellationToken cancellationToken = default)
        {
            return Task.FromResult($"AI Response for: {prompt}");
        }

        public Task<AgentResponseDto> ProcessPromptAsync(AgentPromptRequestDto request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new AgentResponseDto(
                Content: $"AI Response for: {request.Prompt}",
                ThoughtProcess: "Mock thought process",
                ExecutedActions: new List<AgentActionExecutedDto>()
            ));
        }
    }

    [Fact]
    public async Task Chat_ShouldReturnOkWithResponse_WhenPromptIsValid()
    {
        // Arrange
        var orchestrator = new MockAgentOrchestrator();
        var controller = new AgentController(orchestrator, NullLogger<AgentController>.Instance);
        var request = new AgentChatRequestDto
        {
            Prompt = "Do we have enough stock for SKU-123?"
        };

        // Act
        var result = await controller.Chat(request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);

        // Check anonymous object property via reflection
        var responseProp = okResult.Value.GetType().GetProperty("Response");
        Assert.NotNull(responseProp);
        var responseValue = responseProp.GetValue(okResult.Value) as string;
        Assert.Equal("AI Response for: Do we have enough stock for SKU-123?", responseValue);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Chat_ShouldReturnBadRequest_WhenPromptIsEmptyOrWhitespace(string emptyPrompt)
    {
        // Arrange
        var orchestrator = new MockAgentOrchestrator();
        var controller = new AgentController(orchestrator, NullLogger<AgentController>.Instance);
        var request = new AgentChatRequestDto
        {
            Prompt = emptyPrompt
        };

        // Act
        var result = await controller.Chat(request, CancellationToken.None);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Chat_ShouldReturnBadRequest_WhenRequestIsNull()
    {
        // Arrange
        var orchestrator = new MockAgentOrchestrator();
        var controller = new AgentController(orchestrator, NullLogger<AgentController>.Instance);

        // Act
        var result = await controller.Chat(null!, CancellationToken.None);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public void DependencyInjection_ShouldResolveAgentControllerWithOrchestrator()
    {
        // Arrange
        var services = new ServiceCollection();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var tenantContext = new TenantContext();

        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddSingleton<IApplicationDbContext>(new ApplicationDbContext(options, tenantContext));
        services.AddLogging();
        services.AddAgentEngine();
        services.AddTransient<AgentController>();

        // Act
        var provider = services.BuildServiceProvider();
        var controller = provider.GetService<AgentController>();

        // Assert
        Assert.NotNull(controller);
    }

    [Fact]
    public async Task AgentHub_SendPrompt_ShouldCallOrchestrator_AndReturnResponse()
    {
        // Arrange
        var orchestrator = new MockAgentOrchestrator();
        var hub = new AgentHub(orchestrator, NullLogger<AgentHub>.Instance);
        var testClients = new TestHubCallerClients();
        hub.Clients = testClients;

        // Act
        var result = await hub.SendPrompt("Check inventory");

        // Assert
        Assert.Equal("AI Response for: Check inventory", result);
        Assert.Contains(testClients.SentMessages, m => m.method == "ReceiveFinalResponse" && (string?)m.args[0] == "AI Response for: Check inventory");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AgentHub_SendPrompt_ShouldRejectEmptyPrompt(string emptyPrompt)
    {
        // Arrange
        var orchestrator = new MockAgentOrchestrator();
        var hub = new AgentHub(orchestrator, NullLogger<AgentHub>.Instance);
        var testClients = new TestHubCallerClients();
        hub.Clients = testClients;

        // Act
        var result = await hub.SendPrompt(emptyPrompt);

        // Assert
        Assert.Equal("Prompt cannot be empty.", result);
    }

    private class TestHubCallerClients : IHubCallerClients
    {
        public List<(string method, object?[] args)> SentMessages { get; } = new();

        public IClientProxy Caller => new TestClientProxy(SentMessages);
        public IClientProxy Others => throw new NotImplementedException();
        public IClientProxy OthersInGroup(string groupName) => throw new NotImplementedException();
        public IClientProxy All => throw new NotImplementedException();
        public IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds) => throw new NotImplementedException();
        public IClientProxy Client(string connectionId) => throw new NotImplementedException();
        public IClientProxy Clients(IReadOnlyList<string> connectionIds) => throw new NotImplementedException();
        public IClientProxy Group(string groupName) => throw new NotImplementedException();
        public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => throw new NotImplementedException();
        public IClientProxy Groups(IReadOnlyList<string> groupNames) => throw new NotImplementedException();
        public IClientProxy User(string userId) => throw new NotImplementedException();
        public IClientProxy Users(IReadOnlyList<string> userIds) => throw new NotImplementedException();
    }

    private class TestClientProxy : IClientProxy
    {
        private readonly List<(string method, object?[] args)> _sent;
        public TestClientProxy(List<(string method, object?[] args)> sent) => _sent = sent;

        public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default)
        {
            _sent.Add((method, args));
            return Task.CompletedTask;
        }
    }
}
