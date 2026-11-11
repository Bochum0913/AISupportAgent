using AISupportAgent.Models;
using AISupportAgent.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace AISupportAgent.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public sealed class AgentController : ControllerBase
{
    private readonly AgentService _agentService;

    public AgentController(AgentService agentService)
    {
        _agentService = agentService;
    }

    [HttpPost("chat")]
    public async Task<IActionResult> Chat([FromBody] ChatRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) return Forbid();
        if (request is null || string.IsNullOrWhiteSpace(request.Message) ||
            request.Message.Length > 4000 || string.IsNullOrWhiteSpace(request.ConversationId) ||
            request.ConversationId.Length > 128)
        {
            return BadRequest(new { message = "Conversation ID and a message up to 4000 characters are required." });
        }

        // Do not trust a client-supplied conversation ID as a globally unique key.
        // This keeps two administrators' conversations separate even if they choose
        // the same ConversationId. Hashing also keeps the stored key bounded.
        var key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            userId + ":" + request.ConversationId)));

        var result = await _agentService.ProcessMessageAsync("admin:" + key, request.Message);
        return Ok(result);
    }
}
