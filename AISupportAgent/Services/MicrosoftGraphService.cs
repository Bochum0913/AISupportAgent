using Azure.Identity;
using Microsoft.Graph;

namespace AISupportAgent.Services
{
    public class MicrosoftGraphService
    {
        private readonly IConfiguration _configuration;
        private readonly GraphServiceClient _graphClient;
        private readonly ILogger<MicrosoftGraphService> _logger;

        public MicrosoftGraphService(
            IConfiguration configuration,
            ILogger<MicrosoftGraphService> logger)
        {
            _configuration = configuration;
            _logger = logger;

            string tenantId =
                _configuration["MicrosoftGraph:TenantId"] ?? "";

            string clientId =
                _configuration["MicrosoftGraph:ClientId"] ?? "";

            string clientSecret =
                _configuration["MicrosoftGraph:ClientSecret"] ?? "";

            ClientSecretCredential credential = new(
                tenantId,
                clientId,
                clientSecret
            );

            _graphClient = new GraphServiceClient(credential);
        }

        public async Task<string> ListUsersAsync()
        {
            try
            {
                var response = await _graphClient.Users.GetAsync();

                if (response?.Value == null || response.Value.Count == 0)
                {
                    return "No users were found in Microsoft Entra ID.";
                }

                string result = "";

                foreach (var user in response.Value)
                {
                    result +=
                        $"Name: {user.DisplayName}\n" +
                        $"Email: {user.Mail ?? user.UserPrincipalName}\n" +
                        $"User ID: {user.Id}\n\n";
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Microsoft Graph error while listing Entra ID users"
                );

                return
                    "Unable to retrieve Microsoft Entra ID users " +
                    "because Microsoft Graph returned an error.";
            }
        }

        public async Task<string> CreateGroupAsync(string groupName)
        {
            if (string.IsNullOrWhiteSpace(groupName))
            {
                return "Group name cannot be empty.";
            }

            try
            {
                string mailNickname = groupName
                    .Replace(" ", "")
                    .Replace("-", "")
                    .ToLower();

                var group = new Microsoft.Graph.Models.Group
                {
                    DisplayName = groupName,
                    MailEnabled = false,
                    MailNickname = mailNickname,
                    SecurityEnabled = true
                };

                var createdGroup =
                    await _graphClient.Groups.PostAsync(group);

                if (createdGroup == null)
                {
                    return "Failed to create Microsoft Entra ID group.";
                }

                return
                    $"Microsoft Entra ID group created successfully.\n" +
                    $"Group Name: {createdGroup.DisplayName}\n" +
                    $"Group ID: {createdGroup.Id}";
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Microsoft Graph error while creating Entra ID group"
                );

                return
                    "Unable to create the Microsoft Entra ID group " +
                    "because Microsoft Graph returned an error.";
            }
        }

        public async Task<string> AddUserToGroupAsync(
            string userId,
            string groupId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return "User ID cannot be empty.";
            }

            if (string.IsNullOrWhiteSpace(groupId))
            {
                return "Group ID cannot be empty.";
            }

            try
            {
                var requestBody =
                    new Microsoft.Graph.Models.ReferenceCreate
                    {
                        OdataId =
                            $"https://graph.microsoft.com/v1.0/" +
                            $"directoryObjects/{userId}"
                    };

                await _graphClient
                    .Groups[groupId]
                    .Members
                    .Ref
                    .PostAsync(requestBody);

                return
                    $"User added to Microsoft Entra ID group successfully.\n" +
                    $"User ID: {userId}\n" +
                    $"Group ID: {groupId}";
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Microsoft Graph error while adding user to Entra ID group"
                );

                return
                    "Unable to add the user to the Microsoft Entra ID group " +
                    "because Microsoft Graph returned an error.";
            }
        }

        public async Task<string> AddUserToGroupByNameAsync(
            string userName,
            string groupName)
        {
            if (string.IsNullOrWhiteSpace(userName))
            {
                return "User name cannot be empty.";
            }

            if (string.IsNullOrWhiteSpace(groupName))
            {
                return "Group name cannot be empty.";
            }

            try
            {
                var users =
                    await _graphClient.Users.GetAsync(
                        requestConfiguration =>
                        {
                            requestConfiguration.QueryParameters.Filter =
                                $"displayName eq " +
                                $"'{userName.Replace("'", "''")}'";
                        });

                if (users?.Value == null ||
                    users.Value.Count == 0)
                {
                    return
                        $"User '{userName}' was not found " +
                        "in Microsoft Entra ID.";
                }

                if (users.Value.Count > 1)
                {
                    return
                        $"Multiple users named '{userName}' were found. " +
                        "Please provide a more specific user.";
                }

                var user = users.Value[0];

                var groups =
                    await _graphClient.Groups.GetAsync(
                        requestConfiguration =>
                        {
                            requestConfiguration.QueryParameters.Filter =
                                $"displayName eq " +
                                $"'{groupName.Replace("'", "''")}'";
                        });

                if (groups?.Value == null ||
                    groups.Value.Count == 0)
                {
                    return
                        $"Group '{groupName}' was not found " +
                        "in Microsoft Entra ID.";
                }

                if (groups.Value.Count > 1)
                {
                    return
                        $"Multiple groups named '{groupName}' were found. " +
                        "Please provide a more specific group.";
                }

                var group = groups.Value[0];

                if (string.IsNullOrWhiteSpace(user.Id))
                {
                    return
                        $"User '{userName}' does not have a valid User ID.";
                }

                if (string.IsNullOrWhiteSpace(group.Id))
                {
                    return
                        $"Group '{groupName}' does not have a valid Group ID.";
                }

                return await AddUserToGroupAsync(
                    user.Id,
                    group.Id
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Microsoft Graph error while resolving user and group"
                );

                return
                    "Unable to add the user to the Microsoft Entra ID group " +
                    "because Microsoft Graph returned an error.";
            }
        }

        public async Task<string> ListGroupsAsync()
        {
            try
            {
                var response =
                    await _graphClient.Groups.GetAsync();

                if (response?.Value == null ||
                    response.Value.Count == 0)
                {
                    return
                        "No groups were found in Microsoft Entra ID.";
                }

                string result = "";

                foreach (var group in response.Value)
                {
                    result +=
                        $"Group Name: {group.DisplayName}\n" +
                        $"Group ID: {group.Id}\n" +
                        $"Description: " +
                        $"{group.Description ?? "No description"}\n\n";
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Microsoft Graph error while listing Entra ID groups"
                );

                return
                    "Unable to retrieve Microsoft Entra ID groups " +
                    "because Microsoft Graph returned an error.";
            }
        }

        public async Task<string> ListGroupMembersAsync(
            string groupName)
        {
            if (string.IsNullOrWhiteSpace(groupName))
            {
                return "Group name cannot be empty.";
            }

            try
            {
                var groupsResponse =
                    await _graphClient.Groups.GetAsync(
                        requestConfiguration =>
                        {
                            requestConfiguration.QueryParameters.Filter =
                                $"displayName eq " +
                                $"'{groupName.Replace("'", "''")}'";
                        });

                var group =
                    groupsResponse?.Value?.FirstOrDefault();

                if (group == null ||
                    string.IsNullOrWhiteSpace(group.Id))
                {
                    return
                        $"Microsoft Entra ID group '{groupName}' " +
                        "was not found.";
                }

                var membersResponse =
                    await _graphClient
                        .Groups[group.Id]
                        .Members
                        .GetAsync();

                if (membersResponse?.Value == null ||
                    membersResponse.Value.Count == 0)
                {
                    return
                        $"The Microsoft Entra ID group " +
                        $"'{groupName}' has no members.";
                }

                string result =
                    $"Members of group: {group.DisplayName}\n\n";

                foreach (var member in membersResponse.Value)
                {
                    if (member is Microsoft.Graph.Models.User user)
                    {
                        result +=
                            $"Name: {user.DisplayName}\n" +
                            $"Email: " +
                            $"{user.Mail ?? user.UserPrincipalName}\n" +
                            $"User ID: {user.Id}\n\n";
                    }
                    else
                    {
                        result +=
                            $"Member: {member.Id}\n" +
                            $"Type: " +
                            $"{member.OdataType ?? "Unknown"}\n\n";
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Microsoft Graph error while listing members of Entra ID group"
                );

                return
                    "Unable to retrieve the Microsoft Entra ID group members " +
                    "because Microsoft Graph returned an error.";
            }
        }
    }
}