using AISupportAgent.Models;
using AISupportAgent.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net.NetworkInformation;

namespace AISupportAgent.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AgentController : ControllerBase
    {
        private readonly AgentService _agentService;

        public AgentController(AgentService agentService)
        {
        _agentService= agentService;
        }

        [HttpPost("chat")]
        public async Task<IActionResult> Chat(ChatRequest request)
        {
            var result = await _agentService.ProcessMessageAsync(request.ConversationId, request.Message);
            return Ok(result);
                
        }
    }
}
