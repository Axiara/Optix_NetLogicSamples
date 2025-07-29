# FactoryTalk Optix – Microsoft Graph Email Sender (C# NetLogic)

Send automated email from a **FactoryTalk Optix** HMI using **Microsoft Graph** with OAuth 2.0 **client credentials**. This NetLogic class (`OAuthEmailSenderGraph`) acquires an app-only access token and posts a `sendMail` request to Microsoft Graph, optionally including a file attachment.

 **What this is:** A self‑contained C# NetLogic for FT Optix that:

 1. gets an app-only token from your tenant’s `/oauth2/v2.0/token` endpoint, and
 2. calls `POST /v1.0/users/{sender}/sendMail` with a JSON payload that supports **fileAttachment**. ([Microsoft Learn][1], [Microsoft Learn][2], [Microsoft Learn][3])

---

## Features

* **App-only (daemon) auth** – no interactive sign-in required (OAuth 2.0 client credentials grant). ([Microsoft Learn][4])
* **Graph `sendMail` action** – sends plain‑text messages and supports file attachments using `@odata.type: #microsoft.graph.fileAttachment`. ([Microsoft Learn][2], [Microsoft Learn][3], [Microsoft Learn][5])
* **Status feedback to the HMI** – writes success/error text to UA variables so you can show user-friendly messages on screens.
* **Config via project variables** – no code edits needed for tenant/app IDs, scopes, etc.

---

## How it Works (High Level)

1. **Token acquisition**
   The logic posts to:
   `https://login.microsoftonline.com/{TenantID}/oauth2/v2.0/token`
   with `grant_type=client_credentials`, `client_id`, `client_secret`, and `scope=https://graph.microsoft.com/.default`. The `.default` scope tells Entra ID (Azure AD) to include all **application permissions (app roles)** previously consented for Microsoft Graph. ([Microsoft Learn][1], [Microsoft Learn][6], [Microsoft Learn][7])

2. **Send mail**
   It calls:
   `POST https://graph.microsoft.com/v1.0/users/{senderEmail}/sendMail`
   with a JSON body containing `subject`, `body`, `toRecipients`, optional `attachments`, and `saveToSentItems: true`. ([Microsoft Learn][2])

3. **Runs inside FT Optix**
   Implemented as a **NetLogic** class that inherits `BaseNetLogic`, so you can invoke it from UI events, scripts, or methods in your FT Optix project. ([Rockwell Automation][8], [Rockwell Automation][9])

---

## Prerequisites (IT Setup)

> These steps are performed once by your IT / tenant admins.

1. **Create (or choose) a sender mailbox**

   * Use a **licensed** Exchange Online mailbox (service account or shared mailbox as appropriate). Unlicensed or non‑Exchange mailboxes won’t work with Graph mail APIs and commonly trigger errors like `MailboxNotEnabledForRESTAPI`. ([Microsoft Learn][10], [Stack Overflow][11])

2. **App registration (Entra ID)**

   * Azure Portal → Azure Active Directory → **App registrations** → **New registration**.
   * Record **Application (client) ID** and **Directory (tenant) ID**. ([Microsoft Learn][1])

3. **Grant Graph application permission**

   * App → **API permissions** → **Add a permission** → **Microsoft Graph** → **Application permissions** → **Mail.Send** → **Add**.
   * Click **Grant admin consent** for the tenant.
   * After consent, tokens requested with `.default` will include the `Mail.Send` app role. ([Microsoft Learn][6])

4. **Create a client secret**

   * App → **Certificates & secrets** → **New client secret** → copy the **secret value** securely. ([Microsoft Learn][1])

5. *(Optional but recommended)* **Restrict which mailboxes the app can access**

   * Use an **Exchange Online Application Access Policy** to constrain app-only Graph mail permissions to specific mailbox(es). This enforces least privilege for `Mail.Send`. ([Stack Overflow][12], [c7solutions.com][13], [appgovscore.com][14])

---

## Configure the FT Optix Project

Create (or map) UA variables on your Logic object and set them as follows:

| Variable name         | Example / Guidance                                                                                                    |
| --------------------- | --------------------------------------------------------------------------------------------------------------------- |
| `TokenEndpoint`       | `https://login.microsoftonline.com/<TenantID>/oauth2/v2.0/token` (tenant‑specific) ([Microsoft Learn][1])             |
| `ClientId`            | Your **Application (client) ID**                                                                                      |
| `ClientSecret`        | The client secret **value**                                                                                           |
| `Scope`               | `https://graph.microsoft.com/.default` (required for client credentials) ([Microsoft Learn][6], [Microsoft Learn][7]) |
| `GrantType`           | `client_credentials` ([Microsoft Learn][4])                                                                           |
| `SenderEmailAddress`  | The licensed service account’s SMTP address                                                                           |
| `Attachment`          | (Optional) HMI‑provided path to a file to attach                                                                      |
| `EmailSendingStatus`  | `Boolean` output (false/true)                                                                                         |
| `EmailSendingMessage` | `String` output with human‑readable status                                                                            |

> **Note:** The code uses `users/{SenderEmailAddress}/sendMail` (not `/me/sendMail`) because app‑only calls don’t have a signed‑in user context. ([Microsoft Learn][2])

---

## Using the Method

Call the exported method from FT Optix UI or logic:

```csharp
// C# call (e.g., from another NetLogic or event handler)
var logic = Owner.Get<OAuthEmailSenderGraph>("OAuthEmailSenderGraph");
logic.SendEmail("someone@contoso.com", "Subject line", "Body text");
```

* Attachments are optional. If `Attachment` contains a valid path, the code loads the file, base64‑encodes it, and adds it to the message as `#microsoft.graph.fileAttachment`. ([Microsoft Learn][3], [Microsoft Learn][5])
* Status and errors are written to `EmailSendingStatus` and `EmailSendingMessage`.

---

## Troubleshooting

* **403 Forbidden**
  Usually indicates missing app permission or missing admin consent. Re‑check **Mail.Send (Application)** and that consent is granted. Request a **new** token after changes. ([Microsoft Learn][6])

* **Token lacks `roles: ["Mail.Send"]`**
  You requested the wrong scope or didn’t have Mail.Send (Application) + admin consent. Request with `.default` and verify the `roles` claim in the access token. ([Microsoft Learn][6])

* **“MailboxNotEnabledForRESTAPI” / 401**
  The target mailbox isn’t an active Exchange Online mailbox (often unlicensed). Assign the appropriate Exchange Online license, wait for provisioning, and retry. ([Stack Overflow][11])

* **Attachments not appearing**
  Ensure the attachment JSON uses `"@odata.type": "#microsoft.graph.fileAttachment"` and sets `contentBytes` to base64. ([Microsoft Learn][3], [Microsoft Learn][5])

---

## Security Notes

* Store the **client secret** securely (e.g., encrypted configuration or secret store).
* Prefer restricting app-only Graph mail access with **Application Access Policies** so the app can send only from intended mailbox(es). ([Stack Overflow][12], [c7solutions.com][13])

---

## Code Layout (Highlights)

* **`OAuthEmailSenderGraph : BaseNetLogic`** – main class invoked from HMI. ([Rockwell Automation][8])
* Reads UA variables for config, fetches token, builds JSON with optional `fileAttachment`, and posts to Graph `sendMail`. ([Microsoft Learn][1], [Microsoft Learn][2])

---

## References

* Microsoft Graph **sendMail** (v1.0): payload, recipients, attachments, `saveToSentItems`. ([Microsoft Learn][2])
* Attachment types & `fileAttachment` model. ([Microsoft Learn][3])
* `@odata.type: #microsoft.graph.fileAttachment` when inlining attachments. ([Microsoft Learn][5])
* Client credentials flow overview. ([Microsoft Learn][4])
* App‑only access (daemon apps): token request & parameters. ([Microsoft Learn][1])
* Use `.default` scope for client credentials. ([Microsoft Learn][6], [Microsoft Learn][7])
* FactoryTalk Optix **NetLogic** concepts. ([Rockwell Automation][8], [Rockwell Automation][9])
* License requirement context & common mailbox error. ([Microsoft Learn][10], [Stack Overflow][11])
* Restrict app access to specific mailboxes (Application Access Policy). ([Stack Overflow][12], [c7solutions.com][13])

---

## Disclaimer

This sample is provided as-is. Review security, error handling, and configuration storage for your environment and follow your organization’s coding and cybersecurity standards before deploying to production.

---

## License

Add your preferred open-source license (e.g., MIT) in a `LICENSE` file.

[1]: https://learn.microsoft.com/en-us/graph/auth-v2-service?utm_source=chatgpt.com "Get access without a user - Microsoft Graph"
[2]: https://learn.microsoft.com/en-us/graph/api/user-sendmail?view=graph-rest-1.0&utm_source=chatgpt.com "user: sendMail - Microsoft Graph v1.0"
[3]: https://learn.microsoft.com/en-us/graph/api/message-post-attachments?view=graph-rest-1.0&utm_source=chatgpt.com "Add attachment - Microsoft Graph v1.0"
[4]: https://learn.microsoft.com/en-us/entra/identity-platform/v2-oauth2-client-creds-grant-flow?utm_source=chatgpt.com "OAuth 2.0 client credentials flow on the Microsoft identity platform"
[5]: https://learn.microsoft.com/en-us/answers/questions/1355625/graph-api-sendmail-the-property-contentbytes-does?utm_source=chatgpt.com "Graph API sendMail - The property 'contentBytes' does not exist on ..."
[6]: https://learn.microsoft.com/en-us/entra/identity-platform/scopes-oidc?utm_source=chatgpt.com "Scopes and permissions in the Microsoft identity platform"
[7]: https://learn.microsoft.com/en-us/entra/identity-platform/scenario-daemon-acquire-token?utm_source=chatgpt.com "Acquire tokens to call a web API using a daemon application"
[8]: https://www.rockwellautomation.com/en-id/docs/factorytalk-optix/1-03/contents-ditamap/developing-solutions/developing-projects-with-csharp/netlogic.html?utm_source=chatgpt.com "NetLogic - Rockwell Automation"
[9]: https://www.rockwellautomation.com/en-fi/docs/factorytalk-optix/1-10/contents-ditamap/developing-solutions/application-examples/netlogic-tutorial.html?utm_source=chatgpt.com "NetLogic tutorial - Rockwell Automation"
[10]: https://learn.microsoft.com/en-us/answers/questions/2275827/401-on-graph-sendmail-api?utm_source=chatgpt.com "401 on graph /sendMail API - Microsoft Q&A"
[11]: https://stackoverflow.com/questions/65426179/microsoft-graph-to-send-mail-with-client-credential-flow-application-permission?utm_source=chatgpt.com "Microsoft Graph to send mail with Client Credential Flow (application ..."
[12]: https://stackoverflow.com/questions/69080522/send-mail-via-microsoft-graph-as-application-any-user?utm_source=chatgpt.com "Send mail via Microsoft Graph as Application (Any User)"
[13]: https://c7solutions.com/2024/09/secure-access-to-mailboxes-via-graph?utm_source=chatgpt.com "Secure Access To Some Mailboxes Via Graph - Brian Reid"
[14]: https://www.appgovscore.com/blog/how-to-restrict-microsoft-graph-api-access-to-mailboxes?utm_source=chatgpt.com "How to Restrict Microsoft Graph API Access to Mailboxes"
