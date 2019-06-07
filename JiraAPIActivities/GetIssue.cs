using System;
using System.Linq;
using System.Activities;
using System.ComponentModel;
using System.Net.Http;
using Newtonsoft.Json;
using JiraAPI.Properties;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;

namespace JiraAPI.Activities
{
    [LocalizedDisplayName(nameof(Resources.GetIssue))]
    public class GetIssue : CodeActivity<string>
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

        [LocalizedCategory(nameof(Resources.FieldExtraction))]
        [LocalizedDisplayName(nameof(Resources.UseDefault))]
        [LocalizedDescription(nameof(Resources.UseDefaultDesc))]
        public bool UseDefault { get; set; } = true;

        [LocalizedCategory(nameof(Resources.FieldExtraction))]
        [LocalizedDisplayName(nameof(Resources.FieldList))]
        [LocalizedDescription(nameof(Resources.FieldListDesc))]
        public InArgument<string[]> FieldList { get; set; }

        [LocalizedCategory(nameof(Resources.FieldExtraction))]
        [LocalizedDisplayName(nameof(Resources.FieldJson))]
        [LocalizedDescription(nameof(Resources.FieldJsonDesc))]
        public InArgument<string> FieldJsonPath { get; set; }

        [LocalizedCategory(nameof(Resources.Output))]
        [LocalizedDisplayName(nameof(Resources.Result))]
        [LocalizedDescription(nameof(Resources.ResultJsonDesc))]
        public new OutArgument<string> Result { get => base.Result; set => base.Result = value; }

        protected override string Execute(CodeActivityContext context)
        {
            // Default Fields
            string[] defaultFields = {
                "summary", "description", "status", "priority", "fixVersions", "labels"
            };

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
                string issueid = IssueKey.Get(context);
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
            // Get JSON deserialized object
            JObject res = JsonConvert.DeserializeObject<JObject>(result);

            // See if user specified which fields to extract
            string[] fieldsToShow = null;
            // if use default is on
            if (UseDefault)
            {
                fieldsToShow = defaultFields;
            }
            string[] fieldlist = FieldList.Get(context);
            // if default is being used and user has added additional fields
            if (fieldsToShow != null && fieldlist != null)
            {
                fieldsToShow = fieldsToShow.Concat(fieldlist).ToArray();
            }
            else if (fieldlist != null)
            {
                fieldsToShow = fieldlist;
            }
            string jsonPath = FieldJsonPath.Get(context);
            // JSON first
            if (jsonPath != null)
            {
                // Try to read in the file
                string jsonString;
                try
                {
                    jsonString = File.ReadAllText(jsonPath);
                }
                catch (FileNotFoundException e)
                {
                    throw new Exception("File not found. " + e.Message);
                }
                // Read in template file and get fields from response
                JObject template = JsonConvert.DeserializeObject<JObject>(jsonString);
                JObject fields = (JObject)res["fields"];
                // Add values from response to template
                foreach (JToken j in template["fields"].Children())
                {
                    string propertyName = j.ToObject<JProperty>().Name;
                    try
                    {
                        template["fields"][propertyName] = fields[propertyName];
                    }
                    catch (Exception e)
                    {
                        throw new Exception("Unexpected error: " + e.Message);
                    }
                }
                // Return template
                return JsonConvert.SerializeObject(template, Formatting.Indented);
            }
            // If field list has been filled in or default values are being used
            else if (fieldsToShow != null)
            {
                JObject output = new JObject
                {
                    ["fields"] = new JObject()
                };
                JObject fields = (JObject)res["fields"]; // get fields from response
                // Try to extract each field from response
                foreach (string str in fieldsToShow)
                {
                    JToken target;
                    try
                    {
                        target = fields[str];
                        if (target.SelectToken("name", errorWhenNoMatch: false) != null)
                        {
                            output["fields"][str] = target["name"];
                        }
                        else
                        {
                            output["fields"][str] = target;
                        }
                    }
                    catch
                    {
                        output["fields"][str] = "Field does not exist";
                    }
                }
                return JsonConvert.SerializeObject(output, Formatting.Indented);
            }
            else
            {
                return JsonConvert.SerializeObject(res, Formatting.Indented);
            }
        }
    }
}
