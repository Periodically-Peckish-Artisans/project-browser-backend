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

        [FunctionName("project-get")]
        public static async Task<IActionResult> ProjectGetAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "project/{projectId}")] HttpRequest req,
            ILogger log,
            [Blob("project/{projectId}", FileAccess.Read)] CloudBlockBlob project)
        {
            if (!await project.ExistsAsync()) {
                return new NotFoundResult();
            }

            string json = await project.DownloadTextAsync();

            Models.Project projectObj;

            try {
                projectObj = JsonConvert.DeserializeObject<Models.Project>(json);
            }
            catch (JsonException ex) {
                log.LogError(ex, $"project-get failed to deserialize json for blob: {project.Name}.");
                return new InternalServerErrorResult();
            }

            return new JsonResult(projectObj);
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

        [FunctionName("project-post")]
        public static async Task<IActionResult> ProjectPostAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "project/{projectId}")] HttpRequest req,
            ILogger log,
            [Blob("project/{projectId}", FileAccess.ReadWrite)] CloudBlockBlob project,
            string projectId,
            ExecutionContext context)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            Models.Project inProjectObj;
            try {
                inProjectObj = JsonConvert.DeserializeObject<Models.Project>(requestBody);
            }
            catch {
                return new BadRequestResult();
            }

            if (string.IsNullOrEmpty(inProjectObj.Id)) {
                inProjectObj.Id = projectId;
            }

            if (await project.ExistsAsync()) { // Update

                string json = await project.DownloadTextAsync();

                Models.Project projectObj;

                try {
                    projectObj = JsonConvert.DeserializeObject<Models.Project>(json);
                }
                catch (JsonException ex) {
                    log.LogError(ex, $"project-post failed to deserialize json for blob: {project.Name}.");
                    return new InternalServerErrorResult();
                }

                string uid = string.Empty;

                if (GetUseAuth()) {
                    uid = await GetUidAsync(req, context, log);

                    if (uid == null) {
                        return new UnauthorizedResult();
                    }

                    if (!projectObj.ProjectManagerIds.Contains(uid)) {
                        return new UnauthorizedResult();
                    }
                }

                if (projectObj.Equivalent(inProjectObj)) {
                    return new OkResult();
                }

                if (projectId != inProjectObj.Id) {
                    return new BadRequestResult();
                }

                if (!inProjectObj.Validate()) {
                    return new BadRequestResult();
                }

                await project.UploadTextAsync(JsonConvert.SerializeObject(inProjectObj));
                return new OkResult();
            }
            else { // New Project
                if (projectId != inProjectObj.Id) {
                    return new BadRequestResult();
                }

                if (GetUseAuth()) {
                    string uid = await GetUidAsync(req, context, log);
                    if (uid == null) {
                        return new UnauthorizedResult();
                    }

                    if (inProjectObj.ProjectManagerIds == null) {
                        inProjectObj.ProjectManagerIds = new List<string>();
                    }

                    if (!inProjectObj.ProjectManagerIds.Contains(uid)) {
                        inProjectObj.ProjectManagerIds.Add(uid);
                    }
                }

                if (!inProjectObj.Validate()) {
                    return new BadRequestResult();
                }

                await project.UploadTextAsync(JsonConvert.SerializeObject(inProjectObj));
                return new OkResult();
            }
        }
    }
}
