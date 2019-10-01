using FirebaseAdmin;
using FirebaseAdmin.Auth;
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Web.Http;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Collections.Generic;
using Google.Apis.Auth.OAuth2;
using System.Net.Http;

namespace ProjectBrowser.Backend
{
    public static class HttpTriggers
    {
        public static bool GetUseAuth() {
            return bool.TryParse(Environment.GetEnvironmentVariable("UseFirebaseAuth"), out bool output) && output;
        }

        [FunctionName("build-version-get")]
        public static IActionResult BuildVersionGet([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "build")] HttpRequest req)
        {
            return new JsonResult(new { BuildId = Environment.GetEnvironmentVariable("BuildId") });
        }

        private static async Task<IActionResult> SearchAsync(HttpRequest req, ILogger log, string indexName)
        {
            Models.Search search = null;
            try {
                search = JsonConvert.DeserializeObject<Models.Search>(await new StreamReader(req.Body).ReadToEndAsync());
            }
            catch {
                return new BadRequestResult();
            }
            
            HttpClient client = new HttpClient();

            string searchServiceName = Environment.GetEnvironmentVariable("AzureSearchServiceName");
            string apiKey = Environment.GetEnvironmentVariable("AzureSearchServiceQueryApiKey");

            if (string.IsNullOrEmpty(searchServiceName) || string.IsNullOrEmpty(apiKey))
            {
                return new InternalServerErrorResult();
            }

            string responseBody = string.Empty;

            try
            {
                string query = search.SearchString;
                if (string.IsNullOrWhiteSpace(query)) {
                    query = "*";
                }

                var url = $"https://{searchServiceName}.search.windows.net/indexes/{indexName}/docs?api-version=2019-05-06&search={Uri.EscapeUriString(query)}&api-key={apiKey}";
                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                responseBody = await response.Content.ReadAsStringAsync();
            }  
            catch (Exception ex)
            {
                log.LogError(ex, "Failed to execute proejct search.");
                return new BadRequestResult();
            }

            try
            {
                dynamic obj = JsonConvert.DeserializeObject(responseBody);
                return new OkObjectResult(obj);
            }
            catch
            {
                return new InternalServerErrorResult();
            }
        }

        [FunctionName("project-search")]
        public static async Task<IActionResult> ProjectSearchAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "project")] HttpRequest req,
            ILogger log)
        {
            return await SearchAsync(req, log, "project-search-index");
        }

        [FunctionName("event-search")]
        public static async Task<IActionResult> EventSearchAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "project")] HttpRequest req,
            ILogger log)
        {
            return await SearchAsync(req, log, "event-search-index");
        }

        private static async Task<IActionResult> DocumentGetAsync<T>(HttpRequest req, ILogger log, CloudBlockBlob doc) where T : Models.IDoc
        {
            if (!await doc.ExistsAsync()) {
                return new NotFoundResult();
            }

            string json = await doc.DownloadTextAsync();

            T docObj;

            try {
                docObj = JsonConvert.DeserializeObject<T>(json);
            }
            catch (JsonException ex) {
                log.LogError(ex, $"doc-get failed to deserialize json for blob: {doc.Name}.");
                return new InternalServerErrorResult();
            }

            return new JsonResult(docObj);
        }

        [FunctionName("project-get")]
        public static async Task<IActionResult> ProjectGetAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "project/{projectId}")] HttpRequest req,
            ILogger log,
            [Blob("project/{projectId}", FileAccess.Read)] CloudBlockBlob project)
        {
            return await DocumentGetAsync<Models.Project>(req, log, project);
        }

        [FunctionName("event-get")]
        public static async Task<IActionResult> EventGetAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "event/{eventId}")] HttpRequest req,
            ILogger log,
            [Blob("event/{eventId}", FileAccess.Read)] CloudBlockBlob eventBlob)
        {
            return await DocumentGetAsync<Models.PublicEvent>(req, log, eventBlob);
        }

        private static async Task<string> GetUidAsync(HttpRequest req, ExecutionContext context, ILogger log) {
            string token = req.Headers["Authorization"];
            if (string.IsNullOrEmpty(token)) {
                return null;
            }
            if (FirebaseApp.DefaultInstance == null) {
                string credentialsFilePath = Path.GetFullPath(Path.Combine(context.FunctionDirectory, "..\\google-creds.json"));
                log.LogWarning($"Loading Google credentials from path: {credentialsFilePath}");
                var app = FirebaseApp.Create(new AppOptions { Credential = GoogleCredential.FromFile(credentialsFilePath)});
            }

            FirebaseToken t;

            try {
                t = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(token);
            }
            catch (Exception ex) {
                log.LogWarning(ex, "Firebase failed to verify token.");
                return null;
            }

            if (t == null) {
                return null;
            }

            string uid = t.Uid;

            if (string.IsNullOrWhiteSpace(uid)) {
                return null;
            }

            return uid;
        }

        private static async void RunSearchIndexerAsync(string indexerName) {
            HttpClient client = new HttpClient();

            string searchServiceName = Environment.GetEnvironmentVariable("AzureSearchServiceName");
            string apiKey = Environment.GetEnvironmentVariable("AzureSearchServiceAdminApiKey");

            if (string.IsNullOrEmpty(searchServiceName) || string.IsNullOrEmpty(apiKey))
            {
                return;
            }

            try
            {
                client.DefaultRequestHeaders.Add("api-key", apiKey);
                var url = $"https://{searchServiceName}.search.windows.net/indexers/{indexerName}/run?api-version=2019-05-06";
                HttpResponseMessage response = await client.PostAsync(url, null);
                response.EnsureSuccessStatusCode();
            }  
            catch
            {
                return;
            }
        }

        private async static Task<IActionResult> DocumentPutAsync<T>(HttpRequest req, ILogger log, CloudBlockBlob doc, string docId, ExecutionContext context) where T : Models.IDoc
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            T inDocObj;
            try {
                inDocObj = JsonConvert.DeserializeObject<T>(requestBody);
            }
            catch {
                return new BadRequestResult();
            }

            if (string.IsNullOrEmpty(inDocObj.Id)) {
                inDocObj.Id = docId;
            }

            if (await doc.ExistsAsync()) { // Update

                string json = await doc.DownloadTextAsync();

                T docObj;

                try {
                    docObj = JsonConvert.DeserializeObject<T>(json);
                }
                catch (JsonException ex) {
                    log.LogError(ex, $"document put failed to deserialize json for blob: {doc.Name}.");
                    return new InternalServerErrorResult();
                }

                string uid = string.Empty;

                if (GetUseAuth()) {
                    uid = await GetUidAsync(req, context, log);

                    if (uid == null) {
                        return new UnauthorizedResult();
                    }

                    if (!docObj.ManagerIds.Contains(uid)) {
                        return new UnauthorizedResult();
                    }
                }

                if (docObj.Equivalent(inDocObj)) {
                    return new OkResult();
                }

                if (docId != inDocObj.Id) {
                    return new BadRequestResult();
                }

                if (!inDocObj.Validate()) {
                    return new BadRequestResult();
                }

                await doc.UploadTextAsync(JsonConvert.SerializeObject(inDocObj));

                return new OkResult();
            }
            else { // New Document
                if (docId != inDocObj.Id) {
                    return new BadRequestResult();
                }

                if (GetUseAuth()) {
                    string uid = await GetUidAsync(req, context, log);
                    if (uid == null) {
                        return new UnauthorizedResult();
                    }

                    if (inDocObj.ManagerIds == null) {
                        inDocObj.ManagerIds = new List<string>();
                    }

                    if (!inDocObj.ManagerIds.Contains(uid)) {
                        inDocObj.ManagerIds.Add(uid);
                    }
                }

                if (!inDocObj.Validate()) {
                    return new BadRequestResult();
                }

                await doc.UploadTextAsync(JsonConvert.SerializeObject(inDocObj));

                return new OkResult();
            }
        }

        [FunctionName("project-put")]
        public static async Task<IActionResult> ProjectPutAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "project/{projectId}")] HttpRequest req,
            ILogger log,
            [Blob("project/{projectId}", FileAccess.ReadWrite)] CloudBlockBlob project,
            string projectId,
            ExecutionContext context)
        {
            var result = await DocumentPutAsync<Models.Project>(req, log, project, projectId, context);
            if (result is OkResult) {
                RunSearchIndexerAsync("projectindexer");
            }
            return result;
        }

        [FunctionName("event-put")]
        public static async Task<IActionResult> EventPutAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "event/{eventId}")] HttpRequest req,
            ILogger log,
            [Blob("event/{eventId}", FileAccess.ReadWrite)] CloudBlockBlob eventBlob,
            string eventId,
            ExecutionContext context)
        {
            var result = await DocumentPutAsync<Models.PublicEvent>(req, log, eventBlob, eventId, context);
            if (result is OkResult) {
                RunSearchIndexerAsync("eventindexer");
            }
            return result;
        }
    }
}
