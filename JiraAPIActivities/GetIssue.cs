using System;
using System.Activities;
using System.ComponentModel;
using System.Net.Http;
using Newtonsoft.Json;

namespace JiraAPI.Activities
{
    public class GetIssue : CodeActivity<string>
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

        [Category("Output")]
        [Description("Issue data returned as JSON file. (string)")]
        public new OutArgument<string> Result { get => base.Result; set => base.Result = value; }

        protected override string Execute(CodeActivityContext context)
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

            // Authenticate to server with AuthKey 
            try
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authKey);
            }
            catch (Exception e)
            {
                throw new Exception("Authorization failed. Check Username/ApiKey. " + e.Message);
            }

            // API Call begins here
            HttpResponseMessage response;
            string result;
            try
            {
                string issueid = IssueID.Get(context);
                response = client.GetAsync(url + "/rest/api/2/issue/" + issueid).Result;
                // Throw error if status code is negative
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception("Response status code: " + ((int)response.StatusCode).ToString() + " " + response.StatusCode);
                }
                result = response.Content.ReadAsStringAsync().Result;
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
            // Return JSON serialized string
            return JsonConvert.SerializeObject(JsonConvert.DeserializeObject(result), Formatting.Indented);
        }
    }
}
