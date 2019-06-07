using System;
using System.Text;
using System.Activities;
using System.ComponentModel;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using JiraAPI.Properties;

namespace JiraAPI.Activities
{
    [LocalizedDisplayName(nameof(Resources.AddComment))]
    public class AddComment : CodeActivity
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
        [LocalizedDisplayName(nameof(Resources.Body))]
        [LocalizedDescription(nameof(Resources.BodyDesc))]
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
            string issueid = IssueKey.Get(context);

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
