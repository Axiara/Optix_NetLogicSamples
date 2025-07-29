#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.UI;
using FTOptix.NativeUI;
using FTOptix.HMIProject;
using FTOptix.CoreBase;
using FTOptix.NetLogic;
using FTOptix.WebUI;
using FTOptix.Alarm;
using FTOptix.Recipe;
using FTOptix.EventLogger;
using FTOptix.DataLogger;
using FTOptix.SQLiteStore;
using FTOptix.Store;
using FTOptix.Report;
using FTOptix.RAEtherNetIP;
using FTOptix.System;
using FTOptix.Retentivity;
using FTOptix.CommunicationDriver;
using FTOptix.SerialPort;
using FTOptix.Core;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net;
using System.Net.Mail;
#endregion

public class OAuthEmailSenderGraph : BaseNetLogic
{

    // For Graph, ensure the scope is set to "https://graph.microsoft.com/Mail.Send"
    private static string tokenEndpoint;
    private static string clientId;
    private static string clientSecret;
    private static string scope;
    private static string grantType;
    private static string emailSenderAddress;

    // UI feedback variables (project nodes)
    private IUAVariable emailSendingStatus;    // Expected to hold a Boolean
    private IUAVariable emailSendingMessage;   // Expected to hold a string


    public override void Start()
    {
        // Capture configuration from project variables once at startup.
        tokenEndpoint = (string)LogicObject.GetVariable("TokenEndpoint").Value;
        clientId = (string)LogicObject.GetVariable("ClientId").Value;
        clientSecret = (string)LogicObject.GetVariable("ClientSecret").Value;
        scope = (string)LogicObject.GetVariable("Scope").Value; // e.g., "https://graph.microsoft.com/Mail.Send"
        grantType = (string)LogicObject.GetVariable("GrantType").Value;
        emailSenderAddress = (string)LogicObject.GetVariable("SenderEmailAddress").Value;

        // Retrieve UI feedback variables.
        emailSendingStatus = LogicObject.GetVariable("EmailSendingStatus");
        emailSendingMessage = LogicObject.GetVariable("EmailSendingMessage");

        // Initialize UI feedback.
        emailSendingStatus.Value = false;
        emailSendingMessage.Value = "";
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
    }

    [ExportMethod]
    public async void SendEmail(string toAddress, string subject, string body)
    {
        // Capture the dynamic attachment value at the start of the method.
        var attachment = GetVariableValue("Attachment").Value;

        // Get UI feedback variables.
        var statusVar = emailSendingStatus;
        var messageVar = emailSendingMessage;

        // Update UI to show initial status.
        statusVar.Value = false;
        messageVar.Value = "Sending email...";

        // Validate input parameters.
        if (string.IsNullOrEmpty(toAddress) || string.IsNullOrEmpty(subject) || string.IsNullOrEmpty(body))
        {
            messageVar.Value = "Missing email parameters.";
            Log.Error("OAuthEmailSender", "Missing parameters in SendEmail method!");
            return;
        }

        try
        {
            // Retrieve the OAuth access token.
            string accessToken = await GetOAuthToken(tokenEndpoint, clientId, clientSecret, scope, grantType);
            if (string.IsNullOrEmpty(accessToken))
            {
                messageVar.Value = "Failed to retrieve access token.";
                Log.Error("OAuthEmailSender", "Failed to retrieve access token.");
                return;
            }

            // Build a single payload that can optionally contain an attachment
            var attachments = new List<object>();

            // Assuming currentAttachmentPath is your file path string
            if (!string.IsNullOrEmpty(attachment) /*&& File.Exists(currentAttachmentPath)*/)
            {
                var attachmentUri = new ResourceUri(attachment);
                byte[] fileBytes = File.ReadAllBytes(attachmentUri.Uri);
                string base64Content = Convert.ToBase64String(fileBytes);
                string contentType = GetMimeType(attachmentUri.Uri);

                var attachmentDict = new Dictionary<string, object>
                    {
                        { "@odata.type", "#microsoft.graph.fileAttachment" },
                        { "name", Path.GetFileName(attachmentUri.Uri) },
                        { "contentType", contentType },
                        { "contentBytes", base64Content }
                    };

                attachments.Add(attachmentDict);
            }

            var messagePayload = new
            {
                message = new
                {
                    subject = subject,
                    body = new
                    {
                        contentType = "Text",
                        content = body
                    },
                    toRecipients = new[]
                    {
            new { emailAddress = new { address = toAddress } }
        },
                    // If no attachments, this will be an empty list
                    attachments = attachments
                },
                saveToSentItems = true
            };

            // Convert to JSON once
            string jsonPayload = JsonConvert.SerializeObject(messagePayload);

            // Graph API endpoint to send mail:
            string graphEndpoint = $"https://graph.microsoft.com/v1.0/users/{emailSenderAddress}/sendMail";

            // Now send the JSON payload to Graph
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                var content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");
                var response = await client.PostAsync(graphEndpoint, content);

                if (!response.IsSuccessStatusCode)
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    messageVar.Value = $"Graph API error: {response.StatusCode} {errorContent}";
                    return;
                }
            }

            // If we reach here, sending was successful
            statusVar.Value = true;
            messageVar.Value = $"Email sent successfully to {toAddress}";
            Log.Info("OAuthEmailSender", $"Email sent successfully to {toAddress}");
        }

        catch (Exception ex)
        {
            // Update UI feedback on error.
            statusVar.Value = false;
            messageVar.Value = $"Error sending email: {ex.Message}";
            Log.Error("OAuthEmailSender", $"Error sending email: {ex.Message}");
        }
        
    }

    private async Task<string> GetOAuthToken(string tokenEndpoint, string clientId, string clientSecret, string scope, string grantType)
    {
        using (var client = new HttpClient())
        {
            var values = new Dictionary<string, string>
        {
            { "client_id", clientId },
            { "client_secret", clientSecret },
            { "scope", scope },
            { "grant_type", grantType }
        };

            var content = new FormUrlEncodedContent(values);
            var response = await client.PostAsync(tokenEndpoint, content);
            if (!response.IsSuccessStatusCode)
            {
                Log.Error("OAuthEmailSender", $"Token request failed with status {response.StatusCode}");
                return null;
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var tokenData = JObject.Parse(jsonResponse);
            return tokenData["access_token"]?.ToString();
        }
    }

    private IUAVariable GetVariableValue(string variableName)
    {
        var variable = LogicObject.GetVariable(variableName);
        if (variable == null)
        {
            Log.Error($"{variableName} not found");
            return null;
        }
        return variable;
    }

    private string GetMimeType(string filePath)
    {
        // Get the file extension in lower case
        string extension = Path.GetExtension(filePath).ToLowerInvariant();
        switch (extension)
        {
            case ".txt":
                return "text/plain";
            case ".html":
            case ".htm":
                return "text/html";
            case ".jpg":
            case ".jpeg":
                return "image/jpeg";
            case ".png":
                return "image/png";
            case ".gif":
                return "image/gif";
            case ".pdf":
                return "application/pdf";
            case ".doc":
                return "application/msword";
            case ".docx":
                return "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
            // add more mappings as needed
            default:
                return "application/octet-stream"; // fallback default
        }
    }

}
