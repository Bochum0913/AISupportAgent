\# AI Support Agent



An AI-powered IT support assistant built with ASP.NET Core, Azure OpenAI, Microsoft Graph, Entity Framework Core, and SQLite.



The application uses AI function calling to understand user requests and execute backend tools for knowledge-base search, support-ticket management, and Microsoft Entra ID operations.



\## Features



\### AI Support Assistant

\- Natural-language IT support conversations

\- Azure OpenAI integration

\- AI function/tool calling

\- Multi-turn conversation history



\### Knowledge Base

\- Search IT support knowledge articles

\- Return relevant troubleshooting information through the AI assistant



\### Support Ticket Management

\- Create support tickets

\- Retrieve ticket details

\- Update ticket status

\- Update ticket priority

\- Add ticket comments

\- Assign tickets

\- Filter and list support tickets



\### Microsoft Entra ID Integration

\- List Entra ID users

\- List Entra ID groups

\- List members of a group

\- Create security groups

\- Add users to groups



Microsoft Graph API is used to communicate with Microsoft Entra ID.



\### Safety Controls



Write operations against Microsoft Entra ID require explicit user confirmation before execution.



Examples include:



\- Creating an Entra ID group

\- Adding a user to an Entra ID group



The backend performs an additional confirmation check before allowing these operations.



\### Logging and Error Handling

\- Structured ASP.NET Core logging

\- Tool execution logging

\- Successful operation logging

\- Exception handling for external service calls



\## Architecture



```text

User

&#x20; |

&#x20; v

AgentController

&#x20; |

&#x20; v

AgentService

&#x20; |

&#x20; v

Azure OpenAI

&#x20; |

&#x20; v

Function Calling / Tool Execution

&#x20; |

&#x20; +-------------------+--------------------+

&#x20; |                   |                    |

&#x20; v                   v                    v

Knowledge Base   Support Tickets     Microsoft Graph

&#x20; |                   |                    |

&#x20; v                   v                    v

SQLite / EF Core SQLite / EF Core   Microsoft Entra ID

```



\## Technology Stack



\- C#

\- .NET 10

\- ASP.NET Core Web API

\- Azure OpenAI

\- Microsoft Graph SDK

\- Microsoft Entra ID

\- Entity Framework Core

\- SQLite

\- Azure Identity

\- Git / GitHub



\## Project Structure



```text

AISupportAgent

|

+-- Controllers

|   +-- AgentController.cs

|   +-- GraphController.cs

|

+-- Data

|   +-- AppDbContext.cs

|

+-- Models

|   +-- ChatRequest.cs

|   +-- ConversationMessage.cs

|   +-- KnowledgeArticle.cs

|   +-- SupportTicket.cs

|   +-- SupportTicketComment.cs

|

+-- Services

|   +-- AgentService.cs

|   +-- ConversationService.cs

|   +-- KnowledgeBaseService.cs

|   +-- MicrosoftGraphService.cs

|   +-- SupportTicketService.cs

|

+-- Migrations

|

+-- Program.cs

+-- appsettings.json

```



\## Configuration



Sensitive configuration should not be committed to source control.



Create a local `appsettings.Development.json` file and configure the required credentials there.



Example:



```json

{

&#x20; "MicrosoftGraph": {

&#x20;   "TenantId": "YOUR\_TENANT\_ID",

&#x20;   "ClientId": "YOUR\_CLIENT\_ID",

&#x20;   "ClientSecret": "YOUR\_CLIENT\_SECRET"

&#x20; }

}

```



Azure OpenAI credentials should also be stored in local configuration or environment variables.



`appsettings.Development.json` is excluded from Git through `.gitignore`.



\## Running the Project



Clone the repository:



```bash

git clone https://github.com/Bochum0913/AISupportAgent.git

```



Open the solution in Visual Studio.



Restore dependencies:



```bash

dotnet restore

```



Apply Entity Framework migrations:



```bash

dotnet ef database update

```



Run the application:



```bash

dotnet run --project AISupportAgent

```



\## Example Requests



The AI assistant can process natural-language requests such as:



```text

Show me all Microsoft Entra ID groups.

```



```text

Show me the members of the AI Automation Team group.

```



```text

Create a support ticket for a VPN connection issue.

```



```text

Show me all high-priority open support tickets.

```



For sensitive Microsoft Entra ID write operations, the assistant requests confirmation before executing the change.



\## Security



Secrets such as API keys, client secrets, and local development credentials should never be committed to the repository.



The project uses `.gitignore` to exclude local development configuration and SQLite database files.



\## Future Improvements



\- Azure App Service deployment

\- Production database hosting

\- Authentication and authorization

\- Expanded Microsoft Graph operations

\- Improved AI tool orchestration

\- Automated testing

\- CI/CD with GitHub Actions



\## Author



\*\*Bohong Liu\*\*

