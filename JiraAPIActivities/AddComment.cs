using System;
using System.Text;
using System.Activities;
using System.ComponentModel;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace JiraAPI.Activities
{
    public class AddComment : CodeActivity
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
        [Description("Comment body. (string)")]
        public InArgument<string> Body { get; set; }

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
            string body = Body.Get(context);
            string issueid = IssueID.Get(context);

            // Convert body message into JSON format
            JObject payloadJSON;
            StringContent payload;
            try
            {
                payloadJSON = new JObject(new JProperty("body", body));
                payload = new StringContent(JsonConvert.SerializeObject(payloadJSON), Encoding.UTF8, "application/json");
            }
            catch (Exception e)
            {
                throw new Exception("Unable to convert comment into JSON format. Please check the body parameter. " + e.Message);
            }

            // API Call begins here
            try
            {
                // Make POST call with issueid and content
                HttpResponseMessage postResponse = client.PostAsync(url + "/rest/api/2/issue/" + issueid + "/comment", payload).Result;
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
