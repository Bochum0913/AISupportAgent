using AISupportAgent.Services;
using Microsoft.AspNetCore.Mvc;

namespace AISupportAgent.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GraphController : ControllerBase
    {
        private readonly MicrosoftGraphService _microsoftGraphService;

        public GraphController(MicrosoftGraphService microsoftGraphService)
        {
            _microsoftGraphService = microsoftGraphService;
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            string result = await _microsoftGraphService.ListUsersAsync();

            return Ok(result);
        }

        [HttpGet("groups")]
        public async Task<IActionResult> GetGroups()
        {
            string result = await _microsoftGraphService.ListGroupsAsync();

            return Ok(result);
        }

        [HttpGet("groups/{groupName}/members")]
        public async Task<IActionResult> GetGroupMembers(string groupName)
        {
            string result =
                await _microsoftGraphService.ListGroupMembersAsync(groupName);

            return Ok(result);
        }

        [HttpPost("groups")]
        public async Task<IActionResult> CreateGroup(
            [FromBody] CreateGroupRequest request)
        {
            string result =
                await _microsoftGraphService.CreateGroupAsync(request.GroupName);

            return Ok(result);
        }
    }

    public class CreateGroupRequest
    {
        public string GroupName { get; set; } = "";
    }
}