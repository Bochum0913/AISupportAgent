
# AI Support Agent

A full-stack AI-powered IT support application built with React, ASP.NET Core, and SQLite.

The application combines an AI support assistant with a ticket management dashboard, allowing users to troubleshoot issues and manage support tickets.

## Tech Stack

**Frontend**
- React
- JavaScript
- Vite
- CSS

**Backend**
- C#
- ASP.NET Core Web API
- Entity Framework Core
- SQLite
- OpenAI API

## Features

### AI Support Assistant
- AI-powered IT troubleshooting conversations
- Knowledge base lookup
- Conversation history management
- Tool/function calling for support workflows
- User confirmation before sensitive actions

### Ticket Management Dashboard
- View support tickets and dashboard statistics
- Create new support tickets
- Filter tickets by status, priority, and category
- View ticket details in a sliding side panel
- Update ticket status and priority
- Assign tickets to technicians
- Add and view ticket comments
- Persist ticket information in SQLite

## Project Structure

```text
AISupportAgent/
├── AISupportAgent/     # ASP.NET Core backend
├── frontend/           # React frontend
├── AISupportAgent.slnx
└── README.md
```

## Getting Started

### Prerequisites

- .NET SDK compatible with the backend project
- Node.js and npm
- Visual Studio 2022 or VS Code

### Backend

1. Open `AISupportAgent.slnx` in Visual Studio.
2. Configure the required application settings and API credentials.
3. Restore NuGet packages.
4. Start the ASP.NET Core application.

### Frontend

Open a terminal in the `frontend` directory:

```bash
npm install
npm run dev
```

Open the local URL displayed by Vite, usually:

http://localhost:5173

The React application communicates with the ASP.NET Core REST API.

## Screenshots

## Screenshots

### Support Ticket Dashboard
![Support Ticket Dashboard](screenshots/dashboard.png)

### Ticket Details
![Ticket Details](screenshots/ticket-details.png)

## Future Improvements

- Authentication and role-based access control
- Improved ticket search and sorting
- Automated tests
- Cloud deployment

## Automated End-to-End Testing

This project includes automated end-to-end (E2E) tests using **Playwright** and **Chromium** to validate the React frontend and ASP.NET Core backend integration.

### Test Coverage

The automated test covers the following workflow:

1. Open the AI Support Agent dashboard.
2. Create a new support ticket with a unique description.
3. Verify that the ticket appears in the dashboard.
4. Open the ticket details panel.
5. Update the ticket status to **In Progress**.
6. Reload the application.
7. Verify that the updated status persists after reloading.

### Running the Tests

Start the ASP.NET Core backend and React frontend before running the tests.

From the `frontend` directory, execute:

```bash
npx playwright test tests/ticket.spec.js --project=chromium
```

On Windows PowerShell, you can use:

```powershell
npx.cmd playwright test tests/ticket.spec.js --project=chromium
```

The tests run in Chromium and verify the application's core ticket management workflow.

**Note:** Tests currently use the local development environment and create records in the development database. A dedicated test database is recommended for future improvements.

## Author

**Matthew (Bohong) Liu**

[LinkedIn](https://www.linkedin.com/in/bohong-liu-2511a610b/) | [GitHub](https://github.com/Bochum0913)
