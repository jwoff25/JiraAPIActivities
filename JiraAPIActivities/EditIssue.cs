using System;
using System.Text;
using System.Activities;
using System.ComponentModel;
using System.IO;
using System.Net.Http;

namespace JiraAPI.Activities
{
    public class EditIssue: CodeActivity
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
        [Description("Path for JSON file with updated issue data in it. (string)")]
        public InArgument<string> JsonFilePath { get; set; }

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
            string jsonfilepath = JsonFilePath.Get(context);
            string issueid = IssueID.Get(context);

            // Read in JSON file
            string content;
            try
            {
                content = File.ReadAllText(jsonfilepath);
            }
            catch (Exception e)
            {
                throw new Exception("Could not read file. Check JsonFilePath parameter. " + e.Message);
            }
            var payload = new StringContent(content, Encoding.UTF8, "application/json");


            // API Call begins here
            try
            {
                // Make PUT call with issueid and content
                HttpResponseMessage putResponse = client.PutAsync(url + "/rest/api/2/issue/" + issueid, payload).Result;
                // Throw error if status code is negative
                if (!putResponse.IsSuccessStatusCode)
                {
                    throw new Exception("Response status code: " + ((int)putResponse.StatusCode).ToString() + " " + putResponse.StatusCode);
                }
                Console.WriteLine(putResponse.Content.ReadAsStringAsync().Result);
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
