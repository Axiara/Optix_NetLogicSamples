# FactoryTalk Optix Samples

A growing collection of **FactoryTalk Optix** NetLogic examples for common HMI/SCADA tasks. Each sample lives in its own folder with a focused, production‑ready pattern you can adapt into your projects.

> **What’s inside right now**
>
> 1. **GraphEmailSender/** – Send email from Optix using **Microsoft Graph** with OAuth 2.0 **client‑credentials** (app‑only) and optional file attachments.
> 2. **UDTArrayToModel/** – Expose a PLC **UDT array** to Optix UI controls by generating a pointer‑based **Model** that DataGrids/ListBoxes/ComboBoxes can bind to directly.

---

## Repository structure

```
/
├─ GraphEmailSender/      # NetLogic for app-only OAuth + Graph sendMail (+ attachment)
├─ UDTArrayToModel/       # NetLogic that wraps a PLC UDT array as a UI Model via NodePointers
├─ LICENSE                # MIT license
└─ README.md              # This file
```

---

## Samples at a glance

### 1) GraphEmailSender — App‑only Microsoft Graph email

**What it does**

* Requests an access token from your tenant’s token endpoint using **client credentials** and the `.default` scope.
* Calls `POST /v1.0/users/{senderEmail}/sendMail` with a JSON payload (subject, body, recipients) and **inline fileAttachment** support.
* Pushes **status** back to HMI variables for operator feedback.
  See the official Graph docs for the `sendMail` action and payload details. ([Microsoft Learn][1], [Microsoft Learn][2])

**When to use it**

* You need **unattended** email (no interactive sign‑in).
* You want to send from a licensed service account mailbox and keep the logic inside Optix.

**Setup (high level)**

* In Entra ID (Azure AD), register an app; add **Microsoft Graph → Application permissions → Mail.Send**; **grant admin consent**; create a **client secret**.
* Provide the HMI with: **Tenant ID**, **Client ID**, **Client Secret**, **Token Endpoint** (`.../{tenantID}/oauth2/v2.0/token`), **Scope** (`https://graph.microsoft.com/.default`), **Grant Type** (`client_credentials`), and **Sender Email**.
* The sample posts to the Graph endpoint `https://graph.microsoft.com/v1.0/users/{senderEmail}/sendMail`. ([Microsoft Learn][1])

> **Security tip**: Consider **Application Access Policies** (Exchange Online) to restrict an app’s mailbox reach to a defined set of mailboxes (least privilege). ([Microsoft Learn][3])

---

### 2) UDTArrayToModel — PLC UDT array → Optix UI Model

**What it does**

* Creates a lightweight **Grid** object at runtime and fills it with **NodePointers** to each element of a PLC UDT array.
* Sets a `GridModel` variable to the new object, so Optix DataGrid/ListBox/ComboBox controls can bind to it as their **Model** (no value copying; read/write is fast and bidirectional).
* Cleans up on stop to avoid ownership conflicts.

**When to use it**

* Your UI control expects a **Model** node but you only have a raw PLC array.
* You want updates to write directly back to PLC tags via pointers.

**Why this pattern**
Optix controls iterate child nodes under a **Model**; a plain array doesn’t expose the right structure for UI binding. Wrapping with NodePointers keeps it light and keeps the source of truth in the PLC. See NetLogic and DataGrid/model concepts in the official help. ([Rockwell Automation][4], [Rockwell Automation][5])
For creating NodePointers and dynamic links, see the specific how‑to in the Optix docs. ([Rockwell Automation][6])

---

## Prerequisites

* **FactoryTalk Optix Studio** and runtime compatible with C# NetLogic. ([Rockwell Automation][5])
* For GraphEmailSender: an Exchange Online **licensed** mailbox for the sender, and a properly consented app registration with **Mail.Send (Application)**. ([Microsoft Learn][1])

---

## Quick start

1. Clone the repo.
2. Open the desired sample project/folder in **FactoryTalk Optix Studio**.
3. Wire up the required variables (see each sample’s README).
4. Run in the emulator or deploy to your target.

> Need help formatting Markdown or structuring READMEs? GitHub’s docs cover basics and best practices. ([GitHub Docs][7], [GitHub Docs][8], [GitHub Docs][9])

---

## Contributing

Issues and PRs are welcome—especially additional Optix NetLogic patterns (e.g., dynamic column generation, alarm grid utilities, data export helpers). Please include a concise example and a brief README for each new folder/sample.

---

## License

This repository is licensed under the **GNU General Public License v3.0** (see `LICENSE`).

---

