using AISupportAgent.Models;
using OpenAI.Responses;
using System.Text.Json;

#pragma warning disable OPENAI001

namespace AISupportAgent.Services
{
    public class AgentService
    {
        private readonly IConfiguration _configuration;
        private readonly KnowledgeBaseService _knowledgeBaseService;
        private readonly SupportTicketService _supportTicketService;
        private readonly ConversationService _conversationService;
        private readonly ILogger<AgentService> _logger;
        private readonly MicrosoftGraphService _microsoftGraphService;


        public AgentService(
            IConfiguration configuration,
            KnowledgeBaseService knowledgeBaseService,
            SupportTicketService supportTicketService,
            ConversationService conversationService,
            MicrosoftGraphService microsoftGraphService,
            ILogger<AgentService> logger)
        {
            _configuration = configuration;
            _knowledgeBaseService = knowledgeBaseService;
            _supportTicketService = supportTicketService;
            _conversationService = conversationService;
            _microsoftGraphService = microsoftGraphService;
            _logger = logger;
        }

        private List<ResponseTool> CreateTools()
        {
            List<ResponseTool> tools = new();
            FunctionTool searchKnowledgeBaseTool = ResponseTool.CreateFunctionTool(
                functionName: "search_knowledge_base",
                functionDescription: "Search the company's IT support knowledge base.",
                functionParameters: BinaryData.FromBytes("""
                {
                    "type": "object",
                    "properties": {
                        "query": {
                            "type": "string",
                            "description": "The IT issue to search for, such as VPN."
                        }
                    },
                    "required": ["query"]
                }
                """u8.ToArray()),
                strictModeEnabled: false
            );
            tools.Add(searchKnowledgeBaseTool);

            FunctionTool createSupportTicketTool = ResponseTool.CreateFunctionTool(
                functionName: "create_support_ticket",
                functionDescription: "Create an IT support ticket and classify it into a category.",
                functionParameters: BinaryData.FromBytes("""
                {
                    "type": "object",
                    "properties": {
                        "issue": {
                            "type": "string",
                            "description": "The technical issue that should be included in the support ticket."
                        },
                        "category": {
                            "type": "string",
                            "description": "The category of the issue. Must be Network, Email, Account, Hardware, Software, or Other."
                        }
                    },
                    "required": ["issue", "category"]
                }
                """u8.ToArray()),
                strictModeEnabled: false
            );
            tools.Add(createSupportTicketTool);

            FunctionTool getSupportTicketTool = ResponseTool.CreateFunctionTool(
                functionName: "get_support_ticket",
                functionDescription: "Get the details and current status of an existing support ticket by ticket ID.",
                functionParameters: BinaryData.FromBytes("""
                {
                    "type": "object",
                    "properties": {
                        "ticketId": {
                            "type": "string",
                            "description": "The support ticket ID, for example INC-1002."
                        }
                    },
                    "required": ["ticketId"]
                }
                """u8.ToArray()),
                strictModeEnabled: false
            );
            tools.Add(getSupportTicketTool);

            FunctionTool updateSupportTicketTool = ResponseTool.CreateFunctionTool(
                functionName: "update_support_ticket",
                functionDescription: "Update the status of an existing support ticket.",
                functionParameters: BinaryData.FromBytes("""
                {
                    "type": "object",
                    "properties": {
                        "ticketId": {
                            "type": "string",
                            "description": "The support ticket ID, for example INC-1002."
                        },
                        "status": {
                            "type": "string",
                            "description": "The new status of the support ticket, for example Open, In Progress, or Resolved."
                        }
                    },
                    "required": ["ticketId", "status"]
                }
                """u8.ToArray()),
                strictModeEnabled: false
            );
            tools.Add(updateSupportTicketTool);

            FunctionTool addTicketCommentTool = ResponseTool.CreateFunctionTool(
                functionName: "add_ticket_comment",
                functionDescription: "Add a comment or note to an existing support ticket.",
                functionParameters: BinaryData.FromBytes("""
                {
                    "type": "object",
                    "properties": {
                        "ticketId": {
                            "type": "string",
                            "description": "The support ticket ID, for example INC-1003."
                        },
                        "content": {
                            "type": "string",
                            "description": "The comment or note to add to the support ticket."
                        }
                    },
                    "required": ["ticketId", "content"]
                }
                """u8.ToArray()),
                strictModeEnabled: false
            );
            tools.Add(addTicketCommentTool);

            FunctionTool updateTicketPriorityTool = ResponseTool.CreateFunctionTool(
                functionName: "update_ticket_priority",
                functionDescription: "Update the priority of an existing support ticket.",
                functionParameters: BinaryData.FromBytes("""
                {
                    "type": "object",
                    "properties": {
                        "ticketId": {
                            "type": "string",
                            "description": "The support ticket ID, for example INC-1003."
                        },
                        "priority": {
                            "type": "string",
                            "description": "The new priority. Must be Low, Normal, or High."
                        }
                    },
                    "required": ["ticketId", "priority"]
                }
                """u8.ToArray()),
                strictModeEnabled: false
            );
            tools.Add(updateTicketPriorityTool);

            FunctionTool listSupportTicketsTool = ResponseTool.CreateFunctionTool(
                functionName: "list_support_tickets",
                functionDescription: "List support tickets and optionally filter them by status, priority, and category.",
                functionParameters: BinaryData.FromBytes("""
                {
                    "type": "object",
                    "properties": {
                        "status": {
                            "type": ["string", "null"],
                            "description": "Optional ticket status filter, such as Open, In Progress, or Resolved."
                        },
                        "priority": {
                            "type": ["string", "null"],
                            "description": "Optional priority filter: Low, Normal, or High."
                        },
                        "category": {
                            "type": ["string", "null"],
                            "description": "Optional category filter: Network, Email, Account, Hardware, Software, or Other."
                        }
                    },
                    "required": ["status", "priority", "category"]
                }
                """u8.ToArray()),
                strictModeEnabled: false
            );
            tools.Add(listSupportTicketsTool);

            FunctionTool assignSupportTicketTool = ResponseTool.CreateFunctionTool(
                functionName: "assign_support_ticket",
                functionDescription: "Assign an existing support ticket to a technician or support agent.",
                functionParameters: BinaryData.FromBytes("""
                {
                    "type": "object",
                    "properties": {
                        "ticketId": {
                            "type": "string",
                            "description": "The support ticket ID, for example INC-1005."
                        },
                        "assignedTo": {
                            "type": "string",
                            "description": "The name of the technician or support agent to assign the ticket to."
                        }
                    },
                    "required": ["ticketId", "assignedTo"]
                }
                """u8.ToArray()),
                strictModeEnabled: false
            );
            tools.Add(assignSupportTicketTool);

            FunctionTool listEntraUsersTool = ResponseTool.CreateFunctionTool(
                functionName: "list_entra_users",
                functionDescription: "List users in the company's Microsoft Entra ID directory.",
                functionParameters: BinaryData.FromBytes("""
                {
                    "type": "object",
                    "properties": {}
                }
                """u8.ToArray()),
                strictModeEnabled: false
            );
            tools.Add(listEntraUsersTool);

            FunctionTool listEntraGroupsTool = ResponseTool.CreateFunctionTool(
                functionName: "list_entra_groups",
                functionDescription: "List security groups in the company's Microsoft Entra ID directory.",
                functionParameters: BinaryData.FromBytes("""
                {
                    "type": "object",
                    "properties": {}
                }
                """u8.ToArray()),
                strictModeEnabled: false
            );
            tools.Add(listEntraGroupsTool);

            FunctionTool listGroupMembersTool = ResponseTool.CreateFunctionTool(
                functionName: "list_group_members",
                functionDescription:
                    "List the members of a Microsoft Entra ID group by group name.",
                functionParameters: BinaryData.FromBytes("""
                {
                    "type": "object",
                    "properties": {
                        "groupName": {
                            "type": "string",
                            "description": "The exact display name of the Microsoft Entra ID group."
                        }
                    },
                    "required": ["groupName"]
                }
                """u8.ToArray()),
                strictModeEnabled: false
            );
            tools.Add(listGroupMembersTool);

            FunctionTool createEntraGroupTool = ResponseTool.CreateFunctionTool(
                functionName: "create_entra_group",
                functionDescription: "Create a new security group in the company's Microsoft Entra ID directory.",
                functionParameters: BinaryData.FromBytes("""
                {
                    "type": "object",
                    "properties": {
                        "groupName": {
                            "type": "string",
                            "description": "The name of the Microsoft Entra ID group to create."
                        }
                    },
                    "required": ["groupName"],
                    "additionalProperties": false
                }
                """u8.ToArray()),
                strictModeEnabled: false
            );
            tools.Add(createEntraGroupTool);

            FunctionTool addUserToEntraGroupTool = ResponseTool.CreateFunctionTool(
                functionName: "add_user_to_entra_group",
                functionDescription: "Add an existing Microsoft Entra ID user to an existing security group using the user's display name and the group's display name.",
                functionParameters: BinaryData.FromBytes("""
                {
                    "type": "object",
                    "properties": {
                        "userName": {
                            "type": "string",
                            "description": "The display name of the Microsoft Entra ID user."
                        },
                        "groupName": {
                            "type": "string",
                            "description": "The display name of the Microsoft Entra ID security group."
                        }
                    },
                    "required": ["userName", "groupName"],
                    "additionalProperties": false
                }
                """u8.ToArray()),
                strictModeEnabled: false
            );
            tools.Add(addUserToEntraGroupTool);

            return tools;
        }

        private bool IsWriteOperationConfirmed(string conversationId)
        {
            List<ConversationMessage> messages =
                _conversationService.GetMessages(conversationId);

            if (messages == null || messages.Count == 0)
            {
                return false;
            }

            // The current user message is the last user message in the conversation.
            ConversationMessage? lastUserMessage = messages
                .LastOrDefault(m => m.Role == "user");

            if (lastUserMessage == null)
            {
                return false;
            }

            string text = lastUserMessage.Content
                .Trim()
                .ToLowerInvariant();

            string[] confirmations =
            {
                "yes",
                "yes, proceed",
                "yes proceed",
                "confirm",
                "confirmed",
                "proceed",
                "go ahead",
                "do it"
            };

            return confirmations.Any(c =>
                text == c ||
                text == c + "." ||
                text == c + "!");
        }

        private async Task<string> ExecuteToolAsync(FunctionCallResponseItem functionCall, string conversationId)
        {
            try
            {
                _logger.LogInformation("Executing tool {ToolName}", functionCall.FunctionName);

                using JsonDocument arguments = JsonDocument.Parse(functionCall.FunctionArguments);

                string result;

                switch (functionCall.FunctionName)
                {
                    case "search_knowledge_base":
                        {
                            string query = arguments.RootElement.GetProperty("query").GetString() ?? "";

                            result = _knowledgeBaseService.Search(query);
                            break;
                        }

                    case "create_support_ticket":
                        {
                            string issue = arguments.RootElement.GetProperty("issue").GetString() ?? "";

                            string category = arguments.RootElement.GetProperty("category").GetString() ?? "";

                            result = _supportTicketService.CreateTicket(issue, category);

                            break;
                        }

                    case "get_support_ticket":
                        {
                            string ticketId = arguments.RootElement.GetProperty("ticketId").GetString() ?? "";

                            result = _supportTicketService.GetTicket(ticketId);
                            break;
                        }

                    case "update_support_ticket":
                        {
                            string ticketId = arguments.RootElement.GetProperty("ticketId").GetString() ?? "";

                            string status = arguments.RootElement.GetProperty("status").GetString() ?? "";

                            result = _supportTicketService.UpdateTicketStatus(ticketId,status);

                            break;
                        }

                    case "add_ticket_comment":
                        {
                            string ticketId = arguments.RootElement.GetProperty("ticketId").GetString() ?? "";

                            string content = arguments.RootElement.GetProperty("content").GetString() ?? "";

                            result = _supportTicketService.AddComment(ticketId,content);

                            break;
                        }

                    case "update_ticket_priority":
                        {
                            string ticketId = arguments.RootElement.GetProperty("ticketId").GetString() ?? "";

                            string priority = arguments.RootElement.GetProperty("priority").GetString() ?? "";

                            result = _supportTicketService.UpdateTicketPriority(ticketId,priority);

                            break;
                        }

                    case "list_support_tickets":
                        {
                            string? status = null;
                            string? priority = null;
                            string? category = null;

                            if (arguments.RootElement.TryGetProperty(
                                    "status",
                                    out JsonElement statusElement) &&
                                statusElement.ValueKind != JsonValueKind.Null)
                            {
                                status = statusElement.GetString();
                            }

                            if (arguments.RootElement.TryGetProperty(
                                    "priority",
                                    out JsonElement priorityElement) &&
                                priorityElement.ValueKind != JsonValueKind.Null)
                            {
                                priority = priorityElement.GetString();
                            }

                            if (arguments.RootElement.TryGetProperty(
                                    "category",
                                    out JsonElement categoryElement) &&
                                categoryElement.ValueKind != JsonValueKind.Null)
                            {
                                category = categoryElement.GetString();
                            }

                            result = _supportTicketService.ListTickets(status,priority,category);

                            break;
                        }

                    case "assign_support_ticket":
                        {
                            string ticketId = arguments.RootElement.GetProperty("ticketId").GetString() ?? "";

                            string assignedTo = arguments.RootElement.GetProperty("assignedTo").GetString() ?? "";

                            result = _supportTicketService.AssignTicket(ticketId,assignedTo);

                            break;
                        }

                    case "list_entra_users":
                        {
                            result =
                                await _microsoftGraphService.ListUsersAsync();

                            break;
                        }

                    case "list_entra_groups":
                        {
                            result =
                                await _microsoftGraphService.ListGroupsAsync();

                            break;
                        }

                    case "list_group_members":
                        {
                            string groupName = arguments.RootElement.GetProperty("groupName").GetString() ?? "";

                            result =
                                await _microsoftGraphService
                                    .ListGroupMembersAsync(groupName);

                            break;
                        }

                    case "create_entra_group":
                        {
                            // Backend safety guard:
                            // Never create an Entra ID group unless the user
                            // explicitly confirmed the operation.
                            if (!IsWriteOperationConfirmed(conversationId))
                            {
                                _logger.LogWarning(
                                    "Blocked unconfirmed write operation {ToolName}",
                                    functionCall.FunctionName
                                );

                                return
                                    "BLOCKED: The create-group operation was not " +
                                    "explicitly confirmed by the user. Do not make " +
                                    "any Microsoft Entra ID changes.";
                            }

                            string groupName = arguments.RootElement.GetProperty("groupName").GetString() ?? "";

                            result =
                                await _microsoftGraphService
                                    .CreateGroupAsync(groupName);

                            break;
                        }

                    case "add_user_to_entra_group":
                        {
                            // Backend safety guard:
                            // Never add a user to a group unless the user
                            // explicitly confirmed the operation.
                            if (!IsWriteOperationConfirmed(conversationId))
                            {
                                _logger.LogWarning(
                                    "Blocked unconfirmed write operation {ToolName}",
                                    functionCall.FunctionName
                                );

                                return
                                    "BLOCKED: The add-user-to-group operation was " +
                                    "not explicitly confirmed by the user. Do not " +
                                    "make any Microsoft Entra ID changes.";
                            }

                            string userName = arguments.RootElement.GetProperty("userName").GetString() ?? "";

                            string groupName = arguments.RootElement.GetProperty("groupName").GetString() ?? "";

                            result =
                                await _microsoftGraphService
                                    .AddUserToGroupByNameAsync(
                                        userName,
                                        groupName
                                    );

                            break;
                        }

                    default:
                        {
                            _logger.LogWarning(
                                "Unknown tool requested: {ToolName}",
                                functionCall.FunctionName
                            );

                            return "Unknown tool.";
                        }
                }

                _logger.LogInformation(
                    "Tool {ToolName} completed successfully",
                    functionCall.FunctionName
                );

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error executing tool {ToolName}",
                    functionCall.FunctionName
                );

                return "Tool execution failed due to an internal error.";
            }
        }

        public async Task<string> ProcessMessageAsync(string conversationId, string message)
        {
            string? apiKey = _configuration["OpenAI:ApiKey"];

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogError("OpenAI API key was not found.");
                return "AI service configuration is unavailable.";
            }

            _conversationService.AddMessage(
                conversationId,
                "user",
                message
            );

            List<ConversationMessage> history =
                _conversationService.GetMessages(conversationId);

            ResponsesClient client = new(apiKey);


            CreateResponseOptions options = new()
            {
                Model = "gpt-5-mini",

                Instructions = """
                    You are an AI IT support agent for a company.

                    When a user asks for help with an IT problem, always search the
                    company knowledge base using the search_knowledge_base tool before
                    providing troubleshooting instructions.

                    If the knowledge base contains a relevant article:
                    - Use the knowledge base article as the source of truth.
                    - Answer using only troubleshooting information contained in the article.
                    - Do not invent company procedures, URLs, buttons, policies, or troubleshooting steps.
                    - You may rephrase the article to make it clearer, but do not add unsupported details.

                    If the knowledge base does not contain a relevant article:
                    - Clearly tell the user that no relevant company knowledge-base article was found.
                    - You may then provide general troubleshooting suggestions, but clearly identify
                      them as general advice rather than company-specific instructions.

                    If the user asks to list or show users in the company's Microsoft Entra ID
                    directory, use the list_entra_users tool.

                    Use Microsoft Entra ID information returned by the tool as the source of truth.
                    Do not invent users or directory information that was not returned by the tool.

                    If the user asks to list or show groups in the company's Microsoft Entra ID
                    directory, use the list_entra_groups tool.

                    Use Microsoft Entra ID group information returned by the tool as the source of truth.
                    Do not invent groups, group IDs, descriptions, or other directory information
                    that was not returned by the tool.

                    If the user asks to list, show, or identify the members of a Microsoft Entra ID
                    group, use the list_group_members tool.

                    Use the exact group name provided by the user.

                    Use the group membership information returned by the tool as the source of truth.
                    Do not invent group members, user names, email addresses, user IDs, or other
                    directory information that was not returned by the tool.

                    If the user asks to create a Microsoft Entra ID security group,
                    treat this as a write operation.

                    Before using the create_entra_group tool:
                    - Clearly state the exact group name that will be created.
                    - Ask the user to confirm the operation.
                    - Do not call the create_entra_group tool yet.

                    Only use the create_entra_group tool after the user explicitly confirms
                    the pending operation.

                    When the user confirms:
                    - Use the exact group name from the previously confirmed request.
                    - Do not invent or modify the group name.

                    If the user rejects or cancels the operation:
                    - Do not call the tool.
                    - Tell the user that the operation was cancelled.

                    Do not claim that the group was created unless the create_entra_group tool
                    returns a successful result.

                    If the user asks to add an existing Microsoft Entra ID user
                    to an existing Microsoft Entra ID group, treat this as a write operation.

                    Before using the add_user_to_entra_group tool:
                    - Clearly state the user name and group name that will be modified.
                    - Ask the user to confirm the operation.
                    - Do not call the add_user_to_entra_group tool yet.

                    Only use the add_user_to_entra_group tool after the user explicitly confirms
                    the pending operation.

                    When the user confirms:
                    - Use the userName and groupName from the previously confirmed request.
                    - Do not invent user IDs or group IDs.
                    - The tool will resolve the user and group internally.

                    If the user rejects or cancels the operation:
                    - Do not call the tool.
                    - Tell the user that the operation was cancelled.

                    Do not claim that the user was added to the group unless the
                    add_user_to_entra_group tool returns a successful result.

                    If the user or group cannot be found, clearly report the result returned
                    by the tool. Do not pretend that the operation succeeded.

                    When using the add_user_to_entra_group tool:
                    - Extract the user's display name as userName.
                    - Extract the group's display name as groupName.
                    - Use the exact user name and group name provided by the user.
                    - Do not invent user IDs or group IDs.
                    - The tool will resolve the user and group internally.

                    Do not claim that the user was added to the group unless the
                    add_user_to_entra_group tool returns a successful result.

                    If the user or group cannot be found, clearly report the result returned
                    by the tool. Do not pretend that the operation succeeded.

                    If the user explicitly asks to create a support ticket, use the
                    create_support_ticket tool.

                    If the user asks about an existing support ticket, use the
                    get_support_ticket tool.

                    If the user asks to change the status of an existing support ticket,
                    use the update_support_ticket tool.

                    If the user asks to add a comment, note, or additional information
                    to an existing support ticket, use the add_ticket_comment tool.

                    If the user asks to change the priority of an existing support ticket,
                    use the update_ticket_priority tool.

                    Valid priorities are Low, Normal, and High.

                    If the user asks to assign an existing support ticket to a technician
                    or support agent, use the assign_support_ticket tool.

                    When troubleshooting does not resolve the user's issue:

                    - If the user says they already tried the recommended troubleshooting steps, do not repeatedly give the same steps.
                    - Offer to create a support ticket for further assistance.
                    - Do not create a support ticket automatically just because troubleshooting failed.
                    - Only create the support ticket after the user confirms that they want one.
                    - If the user confirms, use the create_support_ticket tool.
                    - When creating the ticket, use the conversation context to describe the unresolved issue clearly.

                    When creating a support ticket, classify the issue into exactly one of these categories:

                    - Network: VPN, Wi-Fi, network connectivity, remote access, or internet connection issues.
                    - Email: Outlook, email sending, receiving, mailbox, or email access issues.
                    - Account: Password, login, authentication, MFA, user account lockout, or problems accessing an existing user account.
                      Do not use Account for general permission requests, physical access, meeting-room access, equipment requests, or other resource-access requests.
                    - Hardware: Printers, monitors, keyboards, laptops, desktops, or other physical device issues.
                    - Software: Application errors, crashes, installation problems, or other software issues.
                    - Other: Use only when the issue does not reasonably fit any of the categories above.

                    Always provide one of these category values when using the create_support_ticket tool:
                    Network, Email, Account, Hardware, Software, or Other.

                    If the user asks to list, show, find, or filter multiple support tickets,
                    use the list_support_tickets tool.

                    Use the available filters when they are specified:
                    - status: Open, In Progress, or Resolved
                    - priority: Low, Normal, or High
                    - category: Network, Email, Account, Hardware, Software, or Other

                    If the user does not specify a filter, use null for that filter.

                    """
            };

            foreach (ResponseTool tool in CreateTools())
            {
                options.Tools.Add(tool);
            }

            foreach (ConversationMessage historyMessage in history)
            {
                if (historyMessage.Role == "user")
                {
                    options.InputItems.Add(
                        ResponseItem.CreateUserMessageItem(historyMessage.Content)
                    );
                }
                else if (historyMessage.Role == "assistant")
                {
                    options.InputItems.Add(
                        ResponseItem.CreateAssistantMessageItem(historyMessage.Content)
                    );
                }
            }

            ResponseResult response;

            int iteration = 0;
            const int maxIterations = 5;

            while (true)
            {
                iteration++;

                if (iteration > maxIterations)
                {
                    string errorMessage = "The AI agent reached the maximum number of processing iterations.";

                    _conversationService.AddMessage(
                        conversationId,
                        "assistant",
                        errorMessage
                    );

                    return errorMessage;
                }

                try
                {
                    response = await client.CreateResponseAsync(options);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Error calling OpenAI Responses API for conversation {ConversationId}",
                        conversationId
                    );

                    string errorMessage =
                        "The AI service is temporarily unavailable. Please try again later.";

                    _conversationService.AddMessage(
                        conversationId,
                        "assistant",
                        errorMessage
                    );

                    return errorMessage;
                }

                foreach (ResponseItem outputItem in response.OutputItems)
                {
                    options.InputItems.Add(outputItem);
                }

                bool toolCalled = false;

                foreach (ResponseItem item in response.OutputItems)
                {
                    if (item is FunctionCallResponseItem functionCall)
                    {
                        toolCalled = true;
                        string toolResult = await ExecuteToolAsync(functionCall, conversationId);

                        options.InputItems.Add(
                            ResponseItem.CreateFunctionCallOutputItem(
                                functionCall.CallId,
                                toolResult
                            )
                        );
                    }
                }
                if (!toolCalled)
                {
                    string finalText = response.GetOutputText();

                    _conversationService.AddMessage(
                        conversationId,
                        "assistant",
                        finalText
                    );

                    return finalText;
                }
            }      
        }                    
    }
}
