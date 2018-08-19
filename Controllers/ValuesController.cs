using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.PowerBI.Api.V2;
using Microsoft.PowerBI.Api.V2.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using PowerBIEmbedded_AppOwnsData.Models;
using Microsoft.Rest;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace WebApplication2.Controllers
{
    [RoutePrefix("api/token")]
    public class ValuesController : ApiController
    {
        // GET api/values
        private static readonly string Username = "l.hussain@matellio.com";
        private static readonly string Password = "lhussain@12";
        private static readonly string AuthorityUrl = "https://login.windows.net/common/oauth2/authorize/";
        private static readonly string ResourceUrl = "https://analysis.windows.net/powerbi/api";
        private static readonly string ApplicationId = "225bc539-a3da-49c5-901f-9738ebcd89e9";
        private static readonly string ApiUrl = "https://api.powerbi.com/";
        private static readonly string WorkspaceId = "d8207756-c2e0-496e-9e89-bbdd168af87d";
        private static readonly string ReportId = "6781cce6-129c-4f25-8010-6a975fb0b60a";

        // GET api/values
        [HttpGet]
        public async Task<IHttpActionResult> Get(string username, string roles)
        {
            var result = new EmbedConfig();
            try
            {
                result = new EmbedConfig { Username = username, Roles = roles };
                var error = GetWebConfigErrors();
                if (error != null)
                {
                    result.ErrorMessage = error;
                    return Ok(AirFusion.WindEdition.API.ResponseResult<EmbedConfig>.GetResult(result));
                }
                var credential = new UserPasswordCredential(Username, Password);
                // Authenticate using created credentials
                var authenticationContext = new AuthenticationContext(AuthorityUrl);
                var authenticationResult = await authenticationContext.AcquireTokenAsync(ResourceUrl, ApplicationId, credential);

                if (authenticationResult == null)
                {
                    result.ErrorMessage = "Authentication Failed.";
                    return Ok(AirFusion.WindEdition.API.ResponseResult<EmbedConfig>.GetResult(result));
                }
                var tokenCredentials = new TokenCredentials(authenticationResult.AccessToken, "Bearer");
                using (var client = new PowerBIClient(new Uri(ApiUrl), tokenCredentials))
                {
                    // Get a list of reports.
                    var reports = await client.Reports.GetReportsInGroupAsync(WorkspaceId);

                    // No reports retrieved for the given workspace.
                    if (reports.Value.Count() == 0)
                    {
                        result.ErrorMessage = "No reports were found in the workspace";
                        return Ok(AirFusion.WindEdition.API.ResponseResult<EmbedConfig>.GetResult(result));
                    }

                    Report report;
                    if (string.IsNullOrWhiteSpace(ReportId))
                    {
                        // Get the first report in the workspace.
                        report = reports.Value.FirstOrDefault();
                    }
                    else
                    {
                        report = reports.Value.FirstOrDefault(r => r.Id == ReportId);
                    }

                    if (report == null)
                    {
                        result.ErrorMessage = "No report with the given ID was found in the workspace. Make sure ReportId is valid.";
                        return Ok(AirFusion.WindEdition.API.ResponseResult<EmbedConfig>.GetResult(result));
                    }

                    var datasets = await client.Datasets.GetDatasetByIdInGroupAsync(WorkspaceId, report.DatasetId);
                    result.IsEffectiveIdentityRequired = datasets.IsEffectiveIdentityRequired;
                    result.IsEffectiveIdentityRolesRequired = datasets.IsEffectiveIdentityRolesRequired;
                    GenerateTokenRequest generateTokenRequestParameters;
                    // This is how you create embed token with effective identities
                    if (!string.IsNullOrWhiteSpace(username))
                    {
                        var rls = new EffectiveIdentity(username, new List<string> { report.DatasetId });
                        if (!string.IsNullOrWhiteSpace(roles))
                        {
                            var rolesList = new List<string>();
                            rolesList.AddRange(roles.Split(','));
                            rls.Roles = rolesList;
                        }
                        // Generate Embed Token with effective identities.
                        generateTokenRequestParameters = new GenerateTokenRequest(accessLevel: "view", identities: new List<EffectiveIdentity> { rls });
                    }
                    else
                    {
                        // Generate Embed Token for reports without effective identities.
                        generateTokenRequestParameters = new GenerateTokenRequest(accessLevel: "view");
                    }

                    var tokenResponse = await client.Reports.GenerateTokenInGroupAsync(WorkspaceId, report.Id, generateTokenRequestParameters);

                    if (tokenResponse == null)
                    {
                        result.ErrorMessage = "Failed to generate embed token.";
                        return Ok(AirFusion.WindEdition.API.ResponseResult<EmbedConfig>.GetResult(result));
                    }

                    // Generate Embed Configuration.
                    result.EmbedToken = tokenResponse;
                    result.EmbedUrl = report.EmbedUrl;
                    result.Id = report.Id;
                    var json = new JavaScriptSerializer().Serialize(result);
                    return Ok(AirFusion.WindEdition.API.ResponseResult<EmbedConfig>.GetResult(result));
                }

            }
            catch (Exception ex)
            {
                result.ErrorMessage = "Error Occured";
            }

            return Ok(AirFusion.WindEdition.API.ResponseResult<EmbedConfig>.GetResult(result));
        }

        // GET api/values/5
        public string Get(int id)
        {
            return "value";
        }

        private string GetWebConfigErrors()
        {
            // Application Id must have a value.
            if (string.IsNullOrWhiteSpace(ApplicationId))
            {
                return "ApplicationId is empty. please register your application as Native app in https://dev.powerbi.com/apps and fill client Id in web.config.";
            }

            // Application Id must be a Guid object.
            Guid result;
            if (!Guid.TryParse(ApplicationId, out result))
            {
                return "ApplicationId must be a Guid object. please register your application as Native app in https://dev.powerbi.com/apps and fill application Id in web.config.";
            }

            // Workspace Id must have a value.
            if (string.IsNullOrWhiteSpace(WorkspaceId))
            {
                return "WorkspaceId is empty. Please select a group you own and fill its Id in web.config";
            }

            // Workspace Id must be a Guid object.
            if (!Guid.TryParse(WorkspaceId, out result))
            {
                return "WorkspaceId must be a Guid object. Please select a workspace you own and fill its Id in web.config";
            }

            // Username must have a value.
            if (string.IsNullOrWhiteSpace(Username))
            {
                return "Username is empty. Please fill Power BI username in web.config";
            }

            // Password must have a value.
            if (string.IsNullOrWhiteSpace(Password))
            {
                return "Password is empty. Please fill password of Power BI username in web.config";
            }

            return null;
        }
    }
}
