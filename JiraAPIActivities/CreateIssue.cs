using Newtonsoft.Json;
using System;
using System.Activities;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json.Linq;

namespace JiraAPI.Activities
{
    public class CreateIssue : CodeActivity<string>
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
        [Description("Path for JSON file with issue data in it. (string)")]
        public InArgument<string> JsonFilePath { get; set; }

        [Category("Output")]
        [Description("Issue ID of the newly created issue. (string)")]
        public new OutArgument<string> Result { get => base.Result; set => base.Result = value; }

        protected override string Execute(CodeActivityContext context)
        {
            // Http Client
            HttpClient client = new HttpClient();

            // Variables
            var url = URL.Get(context);
            var username = Username.Get(context);
            var apikey = ApiKey.Get(context);
            var jsonfilepath = JsonFilePath.Get(context);
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
                throw new Exception("Authorization headers failed. Check Username/ApiKey. " + e.Message);
            }

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

            // Make POST request to Jira REST API (/rest/api/2/issue/)
            try
            {
                HttpResponseMessage postResponse = client.PostAsync(url + "/rest/api/2/issue/", payload).Result;
                Console.WriteLine(postResponse.Content.ReadAsStringAsync().Result);
                // Throw error if status code is negative
                if (!postResponse.IsSuccessStatusCode)
                {
                    throw new Exception("Response status code: " + ((int)postResponse.StatusCode).ToString() + " " + postResponse.StatusCode);
                }
                Console.WriteLine("--Done--");
                // Return key of newly created ticket
                JObject responseObject = (JObject)JsonConvert.DeserializeObject(postResponse.Content.ReadAsStringAsync().Result);
                return responseObject["key"].ToString();
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
