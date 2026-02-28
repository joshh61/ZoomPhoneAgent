# Blu J Zoom Phone Assistant
### AI-Powered Zoom Phone Management for Region One ESC

**Prepared by:** Josue Raudy — Help Desk Technician Intern, Division of Technology
**Date:** February 27, 2026
**For:** Dante Fox, Director of Network, Data Center and Help Desk

---

## Problem Statement

Region One ESC operates a Zoom Phone system serving **472+ users** across **11 sites** (Edinburg, Brownsville, Laredo, McAllen), with **38 call queues**, **410+ phone numbers**, and **500+ desk phones**. Managing this system requires frequent lookups, reporting, and configuration tasks that are currently performed manually through the Zoom admin portal.

David, who primarily manages the Zoom Phone system, handles a high volume of daily requests including user lookups, extension searches, call queue management, device troubleshooting, and call log reporting. These tasks are repetitive and time-consuming when done through the traditional admin dashboard.

## Proposed Solution

The **Blu J Zoom Phone Assistant** is an AI-powered chat interface that allows Help Desk staff to query the Zoom Phone system using plain English. Instead of navigating through multiple admin portal screens, staff can simply type a question and receive an immediate answer.

### Example Interactions

| Staff Types... | Agent Returns... |
|---|---|
| "Who has extension 6245?" | Dante Fox, dfox@esc1.net, Edinburg site, Online |
| "Show me all call queues" | Full list of 38 queues with extensions, sites, and status |
| "What phones are offline?" | List of devices currently offline with assigned user and site |
| "Show missed calls from last week" | Call log filtered by date and result type |
| "What numbers are available?" | List of unassigned phone numbers ready for use |
| "Who's in the HR queue?" | Members of the Human Resources call queue with receive-call status |

### Integration with Blu J Command Center

The assistant is designed to integrate directly into the existing **Blu J Command Center** (Google Sites hub) as a new tab alongside the current User Console and Devices Console. It follows the same embedded iframe architecture that Andy built for the existing tools.

```
Blu J Command Center
├── Home
├── User Console        ← existing (Andy)
├── Devices Console     ← existing (Andy)
├── VZ MDM             ← existing (Andy)
└── Zoom Phone         ← NEW (this project)
```

---

## Current Scope (Phase 1 — Read-Only MVP)

Phase 1 is **read-only**. The assistant can look up and display information but **cannot make any changes** to the Zoom Phone system. This ensures zero risk to the phone system while demonstrating the value of the tool.

### Capabilities

| Feature | Description |
|---|---|
| **User Lookup** | Search by name, email, or extension. View user profile, site, phone status. |
| **Phone Number Search** | Look up who a number is assigned to. List available/unassigned numbers. |
| **Call Queue Info** | List all queues, view members, check queue status and configuration. |
| **Auto Receptionist Info** | List IVR systems, view routing configurations. |
| **Extension Search** | Look up any extension to find what it belongs to (user, queue, IVR). |
| **Call Log Reporting** | Pull call history filtered by date, direction, and result (answered, missed, voicemail). |
| **Voicemail Status** | Check voicemail counts and status for specific users. |
| **Device Management** | List desk phones, check online/offline status, find unassigned devices. |

### What Phase 1 Does NOT Do
- Add, remove, or modify users
- Assign or unassign phone numbers
- Change call queue memberships
- Modify auto receptionist settings
- Provision or deprovision devices

These write operations are planned for Phase 2.

---

## Technical Architecture

```
┌─────────────────────────────────────────────────────────┐
│  Blu J Command Center (Google Sites)                    │
│  ┌──────────────────────────────────────┐               │
│  │  Zoom Phone Tab (iframe)             │               │
│  │  ┌──────────────────────────────┐    │               │
│  │  │  Chat UI                     │    │               │
│  │  │  (HTML/CSS/JavaScript)       │    │               │
│  │  └──────────┬───────────────────┘    │               │
│  └─────────────┼────────────────────────┘               │
└────────────────┼────────────────────────────────────────┘
                 │ HTTPS
    ┌────────────▼────────────────┐
    │  ASP.NET Core Web App       │
    │  (Hosted on Azure/Cloud)    │
    │                             │
    │  ┌───────────────────────┐  │
    │  │  Microsoft Semantic   │  │
    │  │  Kernel (AI Agent)    │  │
    │  └───────┬───────────────┘  │
    │          │                  │
    │  ┌───────▼───────────────┐  │
    │  │  Zoom Phone Plugins   │  │
    │  │  (8 read-only tools)  │  │
    │  └───────┬───────────────┘  │
    └──────────┼──────────────────┘
               │ HTTPS (OAuth 2.0)
    ┌──────────▼──────────────────┐
    │  Zoom Phone REST API        │
    │  api.zoom.us/v2/phone/*     │
    └─────────────────────────────┘
```

### Technology Stack

| Component | Technology | Why |
|---|---|---|
| Backend | ASP.NET Core (.NET 10) | Matches Region One's internal development stack |
| AI Framework | Microsoft Semantic Kernel | Microsoft's official AI agent SDK, designed for tool-calling agents |
| LLM | OpenAI GPT-4o | Industry-leading language model for function calling and natural language |
| Zoom Integration | Server-to-Server OAuth + REST API | Secure, no user login required, admin-level access |
| Frontend | HTML/CSS/JavaScript | Lightweight chat UI, embeddable via iframe in Google Sites |

---

## What's Needed to Deploy

The application is **fully built and compiled**. The following items are needed from the team to make it operational:

### From Andy (Sys Admin)

| # | Item | Details | Effort |
|---|---|---|---|
| 1 | **Zoom Server-to-Server OAuth App** | Create in the Zoom Marketplace (marketplace.zoom.us) under Develop > Build App > Server to Server OAuth App | ~15 minutes |
| 2 | **API Scopes** | Enable the following scopes on the app: `phone:read:admin`, `phone_call_log:read:admin`, `phone_recording:read:admin`, `phone_voicemail:read:admin` | Selected during app creation |
| 3 | **App Activation** | Activate the app (requires account admin) | One click |
| 4 | **Credentials** | Provide the Account ID, Client ID, and Client Secret from the activated app — these go into the app's configuration file | Copy/paste 3 values |
| 5 | **Hosting** | Deploy the .NET app to a hosting environment (Azure App Service, Google Cloud Run, or similar) | ~30 minutes |
| 6 | **Google Sites Integration** | Add a new "Zoom Phone" page to the Blu J Command Center with an iframe pointing to the hosted app | ~5 minutes |

### From Dante (Director Approval)

| # | Item | Details |
|---|---|---|
| 7 | **Project approval** | Authorization to proceed with deployment and API access |
| 8 | **OpenAI API key** | An API key from platform.openai.com for the AI model — requires a funded account |
| 9 | **Data handling confirmation** | Confirm that sending Zoom Phone operational data (employee names, extensions, call logs) to OpenAI's API is acceptable under Region One's data policies. Note: OpenAI's API does not use submitted data for model training. |

### Configuration Summary

The entire application is configured through a single file (`appsettings.json`) with **4 values**:

```json
{
  "Zoom": {
    "AccountId": "[from Andy - step 4]",
    "ClientId": "[from Andy - step 4]",
    "ClientSecret": "[from Andy - step 4]"
  },
  "OpenAI": {
    "ApiKey": "[from Dante - step 8]"
  }
}
```

No code changes are required. Fill in the 4 values and deploy.

---

## Security Considerations

| Concern | How It's Addressed |
|---|---|
| **Read-only access** | Phase 1 uses only GET requests. The agent cannot modify the phone system. |
| **Authentication** | Uses Zoom's Server-to-Server OAuth (industry standard). Credentials are stored server-side, never exposed to the browser. |
| **Access control** | The Blu J Command Center is restricted to esc1.net Google Workspace users. Only authorized staff can access the tool. |
| **Data in transit** | All communication uses HTTPS encryption. |
| **OpenAI data policy** | OpenAI's API data usage policy states that API inputs are not used to train models. No Zoom data is stored by OpenAI. |
| **No student data** | The Zoom Phone system contains only employee/staff operational data (names, extensions, call logs). No student PII is involved. |

---

## Roadmap

### Phase 1 — Read-Only MVP (Current)
**Status: Built, awaiting deployment**
- All 8 read-only features implemented
- Chat UI with Blu J Command Center branding
- Embeddable in Google Sites via iframe

### Phase 2 — Write Access
**Timeline: After Phase 1 approval and testing**
- Add/remove users from the Zoom Phone system
- Assign and unassign phone numbers
- Manage call queue memberships (add/remove agents)
- Update auto receptionist configurations
- Provision desk phones for new employees
- All write operations will require **explicit confirmation** before executing

### Phase 3 — Analytics and Automation
**Timeline: Future enhancement**
- Automated daily/weekly call volume reports
- Call queue performance dashboards
- Proactive alerts (e.g., "Queue wait time exceeded 5 minutes")
- Integration with SolarWinds Service Desk for ticket creation
- Webhook-driven notifications for missed calls and voicemail alerts

### Phase 4 — Multi-System Agent
**Timeline: Long-term vision**
- Extend the AI agent to manage other IT systems (Active Directory, Intune, SCCM)
- Unified IT operations assistant within the Blu J Command Center
- Voice interface for hands-free operation during Help Desk calls

---

## About This Project

This project was initiated as part of an ongoing conversation between Josue Raudy and Dante Fox about bringing AI automation to Region One ESC's IT operations. It builds on the existing Blu J Command Center infrastructure created by Andy, following the same Google Sites embedded application pattern.

The application is built with C# and ASP.NET Core to align with Region One's internal development stack, and uses Microsoft Semantic Kernel — Microsoft's official AI orchestration framework — for intelligent tool selection and natural language understanding.

**Repository:** [GitHub link will be added after push]
