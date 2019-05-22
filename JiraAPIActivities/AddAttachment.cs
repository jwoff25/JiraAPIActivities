using System;
using System.IO;
using System.Activities;
using System.ComponentModel;
using System.Net.Http;
using System.Net.Http.Headers;

namespace JiraAPI.Activities
{
    public class AddAttachment : CodeActivity
    {
        [Category("Input")]
        [RequiredArgument]
        [Description("Full URL for target Jira site starting with http(s)://. Note: Do not include the API endpoint. (string)")]
        public InArgument<string> URL { get; set; }

        [Category("Input")]
        [RequiredArgument]
        [Description("Username for Atlassian account. (string)")]
        public InArgument<string> Username { get; set; }

        [Category("Input")]
        [RequiredArgument]
        [Description("API Key for Atlassian account. (string)")]
        public InArgument<string> ApiKey { get; set; }

        [Category("Input")]
        [RequiredArgument]
        [Description("ID of target issue. (string)")]
        public InArgument<string> IssueID { get; set; }

        [Category("Input")]
        [RequiredArgument]
        [Description("File path for attachment. (string)")]
        public InArgument<string> AttachmentPath { get; set; }

        protected override void Execute(CodeActivityContext context)
        {
            Console.WriteLine("Starting AddAttachment subroutine.");
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
            string issueid = IssueID.Get(context);

            // Get Mime Type of attachment
            string mimeType;
            try
            {
                string[] uriSegments = new Uri(attachmentPath).Segments; //doesn't work for relative paths
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
                string[] path = new Uri(attachmentPath).Segments; // Get file name
                payload.Add(content, "file", path[path.Length - 1]);
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
