using System;
using System.IO;
using System.Activities;
using System.ComponentModel;
using System.Net.Http;
using System.Net.Http.Headers;
using JiraAPI.Properties;

namespace JiraAPI.Activities
{
    [LocalizedDisplayName(nameof(Resources.AddAttachment))]
    public class AddAttachment : CodeActivity
    {
        [LocalizedCategory(nameof(Resources.Input))]
        [RequiredArgument]
        [LocalizedDescription(nameof(Resources.URLDesc))]
        public InArgument<string> URL { get; set; }

        [LocalizedCategory(nameof(Resources.Input))]
        [RequiredArgument]
        [LocalizedDisplayName(nameof(Resources.Username))]
        [LocalizedDescription(nameof(Resources.UsernameDesc))]
        public InArgument<string> Username { get; set; }

        [LocalizedCategory(nameof(Resources.Input))]
        [RequiredArgument]
        [LocalizedDisplayName(nameof(Resources.ApiKey))]
        [LocalizedDescription(nameof(Resources.ApiKeyDesc))]
        public InArgument<string> ApiKey { get; set; }

        [LocalizedCategory(nameof(Resources.Input))]
        [RequiredArgument]
        [LocalizedDisplayName(nameof(Resources.IssueKey))]
        [LocalizedDescription(nameof(Resources.IssueKeyDesc))]
        public InArgument<string> IssueKey { get; set; }

        [LocalizedCategory(nameof(Resources.Input))]
        [RequiredArgument]
        [LocalizedDisplayName(nameof(Resources.AttachmentPath))]
        [LocalizedDescription(nameof(Resources.AttachmentDesc))]
        public InArgument<string> AttachmentPath { get; set; }

        protected override void Execute(CodeActivityContext context)
        {
            // Instatiate HttpClient
            HttpClient client = new HttpClient();
            string url = URL.Get(context);

            // Get Base64 Encoded Key from Username and API Key
            string username = Username.Get(context);
            string apikey = ApiKey.Get(context);
            string authKey;

            // Convert username:apikey to Base64
            try
            {
                string base64Decoded = username + ":" + apikey;
                authKey = System.Convert.ToBase64String(System.Text.ASCIIEncoding.ASCII.GetBytes(base64Decoded));
            }
            catch (Exception e)
            {
                throw new Exception("AuthKey generation failed. Check Username/ApiKey fields. " + e.Message);
            }

            // Authenticate with AuthKey
            try
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authKey);
            }
            catch (Exception e)
            {
                throw new Exception("Authorization failed. Check Username/ApiKey. " + e.Message);
            }

            // Get variables from context
            string attachmentPath = AttachmentPath.Get(context);
            string issueid = IssueKey.Get(context);

            // Get Mime Type of attachment
            string mimeType;
            string[] uriSegments;
            try
            {
                uriSegments = attachmentPath.Split('\\'); // now it works for relative paths
                mimeType = uriSegments[uriSegments.Length - 1].Split('.')[1];
            }
            catch (Exception e)
            {
                throw new Exception("Could not get MIME type. Attachment Path incorrect. " + e.Message);
            }
            

            // Add token header
            client.DefaultRequestHeaders.Add("X-Atlassian-Token", "nocheck");

            // Prepare payload
            MultipartFormDataContent payload = new MultipartFormDataContent();
            try
            {
                HttpContent content = new ByteArrayContent(File.ReadAllBytes(attachmentPath));
                content.Headers.ContentType = MediaTypeHeaderValue.Parse("image/" + mimeType);
                payload.Add(content, "file", uriSegments[uriSegments.Length - 1]);
            }
            catch (Exception e)
            {
                throw new Exception("File could not be read in. Please check the AttachmentPath parameter. " + e.Message);
            }

            // API Call begins here
            try
            {
                Console.WriteLine("API Call here");
                // Make POST call with issueid and content
                HttpResponseMessage postResponse = client.PostAsync(url + "/rest/api/2/issue/" + issueid + "/attachments", payload).Result;
                // Throw error if status code is negative
                if (!postResponse.IsSuccessStatusCode)
                {
                    throw new Exception("Response status code: " + ((int)postResponse.StatusCode).ToString() + " " + postResponse.StatusCode);
                }
                Console.WriteLine(postResponse.Content.ReadAsStringAsync().Result);
                Console.WriteLine("-- Done --");
            }
            catch (HttpRequestException e)
            {
                throw new Exception("Error sending request. " + e.Message);
            }
            catch (Exception e)
            {
                throw new Exception("API call failed. " + e.Message);
            }
        }
    }
}
