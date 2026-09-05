using Microsoft.AspNetCore.Mvc;
using SmartErpAgent.Application.Common.Interfaces;
using SmartErpAgent.Application.DTOs;

namespace SmartErpAgent.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AgentController : ControllerBase
{
    private readonly IAgentOrchestrator _agentOrchestrator;
    private readonly ILogger<AgentController> _logger;

    public AgentController(
        IAgentOrchestrator agentOrchestrator,
        ILogger<AgentController> logger)
    {
        _agentOrchestrator = agentOrchestrator;
        _logger = logger;
    }

    [HttpPost("chat")]
    public async Task<IActionResult> Chat(
        [FromBody] AgentChatRequestDto request,
        CancellationToken cancellationToken)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Prompt))
        {
            return BadRequest(new { Message = "Prompt cannot be empty." });
        }

        _logger.LogInformation("Processing agent chat prompt: {Prompt}", request.Prompt);

        var response = await _agentOrchestrator.ExecutePromptAsync(request.Prompt, cancellationToken);
        return Ok(new { Response = response });
    }

    [HttpPost("prompt")]
    public async Task<ActionResult<AgentResponseDto>> ExecuteAgentPrompt(
        [FromBody] AgentPromptRequestDto request,
        CancellationToken cancellationToken)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Prompt))
            return BadRequest(new { Message = "Prompt cannot be empty." });

        var result = await _agentOrchestrator.ProcessPromptAsync(request, cancellationToken);
        return Ok(result);
    }
}
