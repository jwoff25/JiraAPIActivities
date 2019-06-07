using Newtonsoft.Json;
using System;
using System.Activities;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json.Linq;
using JiraAPI.Properties;

namespace JiraAPI.Activities
{
    [LocalizedDisplayName(nameof(Resources.CreateIssue))]
    public class CreateIssue : CodeActivity<string>
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
        [LocalizedDisplayName(nameof(Resources.JsonFilePath))]
        [LocalizedDescription(nameof(Resources.JsonCreateDesc))]
        public InArgument<string> JsonFilePath { get; set; }

        [LocalizedCategory(nameof(Resources.Output))]
        [LocalizedDisplayName(nameof(Resources.Result))]
        [LocalizedDescription(nameof(Resources.ResultKeyDesc))]
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
