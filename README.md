# AISQL Refactoring Tool  

<img src="images/GUI1.png" alt="AISQL Optimizer GUI" width="800">

**AI Refactoring tool** is a WinForms desktop application designed to analyze, refactor, and optimize T-SQL code using Microsoft Foundry Persistent Agents. 
The goal is to assess T-SQL codebase, identify bad practices and anti-patterns, obsolete and deprecated code and provide a complete assessment and a new refactored T-SQL code which is:
- Faster
- More efficient
- More secure
- Aligned with T-SQL Best Practices
- Free of deprecated syntax


Its key strength is scale and speed: it can analyze and refactor large amounts of existing SQL code in a very short time, delivering a complete and consistent assessment on every stored procedure, function, batch you want to consider. You can connect to SQL Server on-premises, IaaS, PaaS, or just provide the code from documentation.  

**AI Refactoring tool** Read-only by design: the tool proposes changes, it never applies them. **Nothing is deployed or executed against your database automatically**: every change is reviewed and applied by you.
The intent is not to replace developers or DBAs, but to dramatically reduce the time required to review, refactor, and improve complex and large T-SQL codebases, leaving developers and DBAs in a supervisory role, with the responsibility for the final decisions and validation.

This application builds on a previous work, [published on the Azure SQL Dev Blog](https://devblogs.microsoft.com/azure-sql/ai-based-t-sql-refactoring-an-automatic-intelligent-code-optimization-with-azure-openai/). The study's repository is available [here](https://github.com/Savelor/SQLAIRefactor).
It targets **DBAs, SQL Developers, and Performance Engineers** who want practical, context-aware SQL optimizations and not just syntactic rewrites. Because the Agent works with your real schema, indexes, and target SQL Server version, the suggestions account for how your code actually runs, and scale across hundreds of objects without the copy-paste-into-a-chatbot grind.

##  Architecture
**AI Refactoring tool** is a bridge between SQL Server and Microsoft Foundry agents. It runs on a common PC, connects to SQL Server as well as Microsoft Foundry. It retrieves the database schema details and T-SQL application code, and passes this information to the Agent to get the assessment results together with the new refactored code.

<img src="images/AIFoundrySchema.png" alt="AISQL Optimizer Architecture" width="800">


##  Key Features

### ✨ AI-Driven T-SQL Refactoring
- T-SQL optimization powered by **Microsoft Foundry Agents**
- Automatic code rewrite and assessment, focusing on: performance, security, compliance with best practices, and deprecations.
- Assess and refactor large codebase (hundreds or thousands of objects) in short time, dramatically reducing effort and cost of code reviews, migrations, etc.
- **Version-aware rewrites:** the tool detects the target SQL Server version (2012 → 2025, and Azure SQL) and instructs the Agent to use only syntax supported by that version.
- The tool provides a new refactored code together with deep analysis about what has been modified and why
- The tool provides a complete assessment on each stored procedure, batch, function, trigger, with recommendations and additional evaluations.
- Persistent conversations per SQL object: every object code (stored procedure, function, batch, etc) is assessed in a separate thread. The user can discuss with AI Agent further options and analysis in a chat style interaction.
- **Note on cost:** refactoring uses Azure AI Foundry Agents, so each assessment consumes model tokens billed to your Azure subscription. However, **AI Refactoring tool** shows the number of tokens consumed in RealTime, so that the user can be aware of the ongoing effort and control costs.

### 🧠 Real Database Context
The AI agent operates with actual database metadata: the tool provides to AI Agent the proper information needed to improve the code. This allows the Agent to produce effective and actionable optimizations grounded in the current database schema details, not generic SQL advice. The following metadata are collected by the tool and provided to the Agent.
- Table schemas
- Column data types
- Current indexes detailed structure
- The user can choose the criteria to select the code to optimize: cpu, i/o, executions and elapsed time. **AI Refactoring tool** retrieves these information from SQL Server plan cache or Query Store, according to the user's choice.

## 🌳 An Easy, Practical User Interface
Everything happens in one window: no scripts to run, no context-switching. Pick what to optimize, fire the Agent, review the result side by side, and eventually continue the optimization regarding a specific item chatting with the Agent.
* **Object explorer with a purpose.** A TreeView lists your SQL objects (procedures, views, functions, triggers…) and lets you rank and select them by what actually matters: execution count, CPU consumption, elapsed time, or I/O reads — so you start from the code that hurts the most, not from a random alphabetical list.
* **See what's done at a glance.** Already-optimized objects are visually highlighted, so on large codebases you always know what's left to do.
* **Work in bulk.** Multi-select via checkboxes and send dozens or hundreds of objects to the Agent in a single batch operation.
* **The Impact Dashboard.** After a batch assessment, a dashboard summarizes every object by how many fixes were applied across **Security, Performance, Compliance, and Deprecations**. Instead of guessing where to begin, you can immediately spot the highest-impact objects and focus your review there, turning a vague "modernize the database" task into a prioritized, measurable plan. If you keep the Dashboard windows close to the main window, you can click the name of the object on the Dashboard, and inspect the corresponding source and optimized code on the main window.

  <img src="images/Dashboard.png" alt="AISQL Optimizer GUI" width="800">

## 🔐 Security & Reliability
- The application authenticates to Microsoft Foundry Agents using Microsoft Entra ID (Azure RBAC) and browser, via `InteractiveBrowserCredential`
- AI Agents **never execute SQL** against the database. The database is never connected to the Agent. In addition, the tool proposes changes only: nothing is deployed, changed or executed against the database automatically. All changes are user-controlled and auditable.

## 🎯 Target Audience

- **SQL Server DBAs**: modernize legacy code and flag deprecated syntax before it breaks on the next upgrade
- **Database Developers**: refactor stored procedures, views and functions without rewriting them by hand
- **Performance Engineers**: turn execution-stat hotspots into concrete, schema-aware optimizations
- **Teams modernizing legacy T-SQL codebases**: migrate to a newer SQL Server (or Azure SQL) with version-aware rewrites

## ⚠️ Payload size limitations

The tool applies two self-imposed size guards, expressed in **characters**, before sending anything to the agent:

- **Code per object — 768,000 characters.** If an object's source exceeds this, it is **not sent**: in batch mode the object is skipped and the run continues with the next one; for a single object the action is blocked with a warning. But a single failing object never stops a batch.
- **Schema + indexes — 768,000 characters (combined).** If the database schema (table columns + indexes) exceeds this, the schema is **omitted** and the optimization proceeds on **SQL syntax only** — the code itself is still sent. A warning is shown so you know the result was produced without schema context.

These character caps are conservative safety nets, well below the Microsoft Foundry Agent Service documented per-message limit (1,500,000 characters).

### Model context window (the real constraint)

The binding limit is not the character count but the **context window of the model** behind your agent, measured in **tokens** and specific to each model.

- Characters are not tokens: as a rough rule, **1 token ≈ 3–4 characters**. A single request combines the object's code, the database schema, and the agent's own instructions and knowledge, so the total token count can be significant.
- **Recommendation: use a model with a context window of at least 512K tokens, and preferably 1M.** This gives enough headroom to process large objects together with the schema without hitting the limit.
- If the combined input still exceeds the model's window, the service rejects the request. The tool catches this and shows a **clear, actionable message** (for example, *"the request exceeds the model's context window — reduce the selection or use a model with a larger context window"*) instead of a raw API error. In batch mode, the run continues with the remaining objects.

## 📖 Documentation
- Run the Refactoring tool: [User Operational Guide](USER_GUIDE.md).
- Setup Agents in Microsoft Foundry: [Microsoft Foundry Agent Setup](AGENT_GUIDE.md)
  
---
## 📋 Prerequisites

### Runtime (to run the tool)

- **Windows 10/11 (x64)**: Windows desktop application (WinForms, `WinExe`).
- **.NET 8 Desktop Runtime (x64)**: the project targets `net8.0-windows`, so the **Desktop** Runtime is required (Windows Forms), not just the base .NET runtime.
- **Microsoft Edge WebView2 Runtime (Evergreen)**: used to render the refactored code, the analysis, and the Dashboard. Preinstalled on current Windows 11; otherwise install it from Microsoft.

### Azure AI Foundry

- An Azure subscription with an **Azure AI Foundry project**.
- A **pre-configured agent** in that project. For detailed information see [Microsoft Foundry Agent Setup](AGENT_GUIDE.md)
- Your **Tenant ID** and the **project endpoint URL**.
- Interactive browser sign-in (via `Azure.Identity`): the signed-in account needs a role on the project that allows listing and running agents (e.g. *Azure AI Developer*).
- Outbound HTTPS access to the Foundry endpoint.

### SQL Server

- Supported: **SQL Server 2012+** (on-prem / IaaS), **Azure SQL Database**, **Azure SQL Managed Instance** with SQL authentication.
- Connectivity via `Microsoft.Data.SqlClient` 6.x. This version defaults to **encrypted connections**: for local/dev servers without a trusted certificate you may need *Trust Server Certificate* enabled (the tool exposes this option).
- Network / firewall access to the instance (for Azure, allow your client IP).
- A login with permission to read object definitions and execution statistics (see *SQL permissions* below).

### SQL permissions

> **To confirm against your own testing.** The values below are the recommended minimum; adjust to what you actually validated.

- `VIEW DEFINITION` on the target objects (to read the source T-SQL of objects).
- **Plan cache source:** `VIEW SERVER STATE` (on-prem / IaaS) or the Azure equivalent, to read the execution-statistics DMVs.
- **Query Store source:** `VIEW DATABASE STATE` on the target database to read from Query Store.
- Read access to the target database catalog views (schema and index metadata).

### Build from source

- **Visual Studio 2022 (17.8+)** or the **.NET 8 SDK**, with the **.NET desktop development** workload.
- NuGet packages (restored automatically on build):

| Package | Version | Purpose |
|---|---|---|
| Azure.AI.Projects | 2.0.1 | Foundry project / agent client |
| Azure.Identity | 1.21.0 | Interactive Azure authentication |
| Microsoft.Data.SqlClient | 6.1.1 | SQL Server connectivity |
| Microsoft.Web.WebView2 | 1.0.3485.44 | Rendering of results / Dashboard |

### Notes

- **No secrets are stored in session files.** A saved session contains the loaded objects and their optimization results plus the server and database *names*, never the password or the full connection string.
- **Data source availability.** If the *Query Store* option is offered in the UI, make sure it is actually implemented in the scan; otherwise document only the plan-cache (DMV) source.

  
## 🚀 Installation

### Run the app 

1. Download the latest `.zip` from the [Releases](../../releases) page.
2. Extract it to a folder of your choice.
3. Install the **Microsoft Edge WebView2 Runtime** if it isn't already on your machine
   (pre-installed on recent Windows): https://developer.microsoft.com/microsoft-edge/webview2/
4. Run `AISQLRefactor.exe`.

> The .NET runtime is **not** required — the release is published self-contained (Windows x64).

On first launch, open **Settings** and configure:
- **Azure AI Foundry**: project endpoint, tenant, and the agent to use (sign-in happens in your browser).
- **SQL Server**: server, database, and authentication.

### Build from source (developers)

```bash
git clone https://github.com/<user>/<repo>.git
```

1. Open the solution in **Visual Studio 2022** (.NET 8 SDK, Windows Forms workload).
2. Build the project — NuGet packages are restored automatically.

### Requirements

- Windows 10/11 (x64)
- Microsoft Edge WebView2 Runtime
- Access to an Azure AI Foundry agent
- A reachable SQL Server instance
  


