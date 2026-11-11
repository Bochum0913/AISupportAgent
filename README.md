# AISupportAgent — AI-Powered IT Help Desk

[![.NET Tests](https://github.com/Bochum0913/AISupportAgent/actions/workflows/dotnet-tests.yml/badge.svg)](https://github.com/Bochum0913/AISupportAgent/actions/workflows/dotnet-tests.yml)

**A full-stack IT support application combining AI-powered self-service troubleshooting, role-based ticket management, and human support escalation.**

Built with **ASP.NET Core 10, React, Entity Framework Core, SQLite, OpenAI API, Docker Compose, and Nginx**.

> **Portfolio frontend demo:** [SupportFlow Demo](https://supportflow-demo.pages.dev)
> **Demo source:** [SupportFlow-Demo](https://github.com/Bochum0913/SupportFlow-Demo)
> The public demo uses scripted AI interactions and browser-local data. The full-stack application in this repository runs a real ASP.NET Core backend and can call the OpenAI API when configured.

## Highlights

- **AI-first self-service:** Customers describe an IT issue and receive AI-powered troubleshooting guidance. If the issue remains unresolved, the app asks whether the customer wants to create a support ticket; it never creates one without explicit confirmation.
- **Three role-specific dashboards:** Customers manage their own tickets; Agents work on assigned tickets; Admins manage roles, assignments, and the overall ticket queue.
- **Human support workflow:** Tickets move from **Open → In Progress → Resolved**. Customers can confirm closure (**Closed**) or request more help (**Reopened**).
- **Authentication and authorization:** ASP.NET Core Identity, role-based API access, ticket ownership checks, CSRF protection, and secure cookies.
- **Traceable work:** Public replies, staff-only internal notes, status/assignment audit history, and ticket version checks.
- **Containerized local deployment:** Docker Compose, Nginx reverse proxy with local HTTPS, and persistent SQLite storage.
- **Automated testing:** xUnit unit/controller tests and HTTP authorization integration tests; CI runs using GitHub Actions on pushes and pull requests.

## Example: From AI Help to a Resolved Ticket

1. A Customer asks the AI assistant: **“I cannot connect to my company VPN.”**
2. The AI returns troubleshooting suggestions and asks **“Did this resolve your issue?”**
3. If the Customer selects **No**, the application asks whether they want to create a support ticket.
4. Only after the Customer confirms **Yes, create ticket** does the system create the ticket.
5. An Admin assigns the ticket to an Agent account.
6. The Agent investigates, updates the ticket to **In Progress**, and submits a public resolution note before marking it **Resolved**.
7. The Customer chooses **Yes, close ticket** or **No, reopen ticket**. The activity history records changes.

Customers can also skip the AI and create a ticket directly from their dashboard.

## Roles and Permissions

| Role | Main capabilities |
| --- | --- |
| **Customer** | Ask AI, create tickets, view own tickets and public replies, confirm closure or reopen a resolved ticket |
| **Agent** | View assigned tickets, update allowed states and priority, add public replies and internal notes |
| **Admin** | View all tickets, manage user roles, assign tickets to verified Agent accounts, oversee ticket progress |

The backend enforces authorization independently of which controls are visible in the frontend.

## Screenshots

### Customer Dashboard

![Customer dashboard with ticket statistics and ticket list](screenshots/dashboard-docker.png)

> The image above is an earlier customer dashboard capture. The current application has distinct Customer, Agent, and Admin dashboards. Updated screenshots of these role-specific views can be added to `screenshots/`.

## Technology Stack

| Area | Technologies |
| --- | --- |
| Frontend | React, JavaScript, Vite, CSS |
| Backend | C#, ASP.NET Core Web API, .NET 10 |
| Authentication | ASP.NET Core Identity, cookie authentication, role-based authorization, CSRF protection |
| Database | SQLite, Entity Framework Core, migrations |
| AI | OpenAI API, application-side troubleshooting and knowledge-base services |
| Deployment | Docker Compose, Nginx, local HTTPS, persistent Docker volume |
| Testing & CI | xUnit, ASP.NET Core HTTP integration tests, GitHub Actions; Playwright test setup |

## Architecture

```text
Browser (React frontend)
   |
   | HTTPS :8443 (local Docker deployment)
   v
Nginx reverse proxy
   |
   | /api
   v
ASP.NET Core Web API (.NET 10)
   |-- Auth/Identity ----> AuthDbContext ----> SQLite
   |-- Ticket APIs -----> AppDbContext ------> SQLite
   |-- AI self-service --> Troubleshooting / OpenAI integration
   `-- Role checks, audit history, comments, workflow validation
```

## Quick Start with Docker

### Requirements

- Docker Desktop (or Docker Engine with Docker Compose)
- Port **8443** available on your machine
- Optional: an OpenAI API key for live AI responses

```bash
git clone https://github.com/Bochum0913/AISupportAgent.git
cd AISupportAgent
```

To enable OpenAI responses, copy `.env.example` to `.env` in the repository root and configure the key locally:

```dotenv
OPENAI_API_KEY=your_api_key_here
```

The `.env` file must **never** be committed. Other optional integrations and bootstrap-admin settings are documented in `.env.example`.

Start the application:

```bash
docker compose up --build -d
```

Open **https://localhost:8443** in your browser. The local setup uses a self-signed HTTPS certificate, so the browser may show a certificate warning; only accept it for your own trusted localhost environment.

Register a Customer account to try the dashboard and ticket workflow. To test the Admin/Agent workflow, configure a separate initial Admin account using the optional bootstrap environment variables, restart the backend container, and promote a separate Customer test account to Agent through the Admin interface. Do not use production credentials or real user data for demos.

Stop the containers:

```bash
docker compose down
```

SQLite data is stored in a persistent Docker volume and survives ordinary restarts and `docker compose down`. **Do not run `docker compose down -v` unless you intend to delete the database volume.**

## Run Without Docker

### Requirements

- .NET 10 SDK
- Node.js and npm
- Optional OpenAI API credentials via ASP.NET Core User Secrets or environment variables

From the repository root, start the backend:

```bash
dotnet restore AISupportAgent/AISupportAgent.csproj
dotnet run --project AISupportAgent/AISupportAgent.csproj
```

In another terminal, run the frontend:

```bash
cd frontend
npm install
npm run dev
```

Open the Vite URL printed in the terminal (commonly `http://localhost:5173`). The development proxy in `frontend/vite.config.js` should point to the backend's active HTTPS URL (the repository's development configuration uses `https://localhost:7088`).

## Automated Tests

The repository contains **17 passing tests**, including ticket workflow tests and HTTP integration tests for role-based API authorization, verified locally with the .NET 10 SDK container. GitHub Actions also runs the solution test command automatically.

Run with a local .NET 10 SDK:

```bash
dotnet test AISupportAgent.slnx
```

Or run in an isolated .NET 10 SDK Docker container:

```bash
docker run --rm -v "${PWD}:/src" -w /src mcr.microsoft.com/dotnet/sdk:10.0 dotnet test AISupportAgent.slnx
```

> The command above is suitable for PowerShell. On other shells, adjust the current-directory mount syntax if needed. Test results are specific to the tested code revision; see the CI badge for current status.

CI workflow: [`.github/workflows/dotnet-tests.yml`](.github/workflows/dotnet-tests.yml).

## Public Demo vs. Full-Stack Application

| Capability | Full-stack application | Public frontend demo |
| --- | --- | --- |
| AI troubleshooting | Real OpenAI API when configured | Scripted / simulated |
| Backend | ASP.NET Core API | Browser-only simulation |
| Authentication and roles | ASP.NET Core Identity and role checks | Demo-only behavior |
| Ticket persistence | SQLite | Browser-local storage |
| Hosting | Local Docker deployment | Cloudflare Pages |

The public demo is for portfolio exploration and is **not connected** to this application's database or live API. Do not enter secrets or sensitive information into the demo.

## Security and Scope

This is a **local portfolio application, not a production-hosted help desk**. It includes authentication, role authorization, CSRF safeguards, ticket access checks, and a limited local reverse-proxy configuration. Before any public deployment, additional measures are required, including a trusted TLS certificate, rate limiting and abuse prevention, secret management, monitoring, operational backups, and a full security review. Do not commit `.env` files, API keys, or database files.

## Possible Future Improvements

- Production hosting with managed secrets, observability, and backups
- Additional security, concurrency, and end-to-end tests
- Improved ticket analytics, search, and pagination
- AI usage controls and support for a larger knowledge base
