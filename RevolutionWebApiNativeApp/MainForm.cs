/*
 * This is sample code that shows how a Native (as opposed to a Server-side) client application can obtain access
 * to the Revolution Web API, and is provided on an AS-IS basis.  This isn't production quality code; it is mainly
 * intended to show the techniques involved in talking to the StatPro Revolution OAuth2 Server from a native app.
 * 
 * This sample does NOT show a lot of useful and necessary techniques:-
 *     - getting portfolios, analysis and results data from the Web API
 *     - detecting if the Web API has returned one of its specific errors
 *     - detecting request blockage by the Web API due to a Fair Usage Policy violation
 *     - detecting if the Web API has rejected the access token because it has expired
 *     - getting a new access token from a refresh token
 *     - re-prompting the user for access if getting an access token from a refresh token fails
 * All of these things and more are shown by the "StatPro Revolution Web API Explorer" repository on GitHub.
 */
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Newtonsoft.Json.Linq;

namespace RevolutionWebApiNativeApp
{
    public partial class MainForm : Form
    {
        // StatPro Revolution's OAuth2 Server's authorization endpoint.
        const String AuthServerAuthorizationUri = "https://revapiaccess.statpro.com/OAuth2/Authorization";

        // StatPro Revolution's OAuth2 Server's token endpoint.
        const String AuthServerTokenUri = "https://revapiaccess.statpro.com/OAuth2/Token";

        // This application's redirect URI, for use in the Authorization Code flow.  The value indicates that
        // the authorization server will redirect to one of its own web pages, and will include the returned
        // authorization code in both the title of the web page and in the "code" query string.
        const String AuthorizationCodeRedirectUri = "urn:ietf:wg:oauth:2.0:oob";

        // This native application's unique client identifier, obtained when registering the application with
        // StatPro for OAuth2 / Web API access.
        const String ClientId = "<your client id goes here>";

        // The Revolution Web API's scope identifier.
        const String RevolutionWebApiScopeId = "RevolutionWebApi";

        // Web API entry point.
        const String WebApiEntryPointUri = "https://revapi.statpro.com/v1";

        // Constructor.
        public MainForm()
        {
            InitializeComponent();
        }

        // Called when the form is closing.
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Don't allow the form to close while we're waiting for a response from the OAuth2 Server or
            // the Web API.
            if (!goBtn.Enabled)
                e.Cancel = true;
        }

        // Gets an access token from the OAuth2 Server, and with it gets a resource from the Web API.
        private async void goBtn_Click(object sender, EventArgs e)
        {
            goBtn.Enabled = false;

            try
            {
                var tokens = await PromptUserForAccessAsync();
                if (tokens == null)
                    return;

                var representation = await GetResourceFromWebApiAsync(tokens.Item1);
                if (representation != null)
                    MessageBox.Show(representation);

                MessageBox.Show("The code ran successfully.");
            }
            finally
            {
                goBtn.Enabled = true;
            }
        }

        // Prompts the user for access to the Revolution Web API.  If successful, the returned task will return
        // a tuple containing an access token (base64-encoded) and a refresh token.  If unsuccessful, the task will
        // return null.
        private async Task<Tuple<String, String>> PromptUserForAccessAsync()
        {
            /* Prompt the user for access, and get authorization code. */

            String errorCode;
            String errorDescription;
            String authorizationCode;
            AuthorizationRequestResult result;

            using (var form = new WebBrowserForm())
            {
                form.SetData(ClientId, AuthorizationCodeRedirectUri, RevolutionWebApiScopeId,
                    AuthServerAuthorizationUri);

                var dr = form.ShowDialog(this);
                if (dr != System.Windows.Forms.DialogResult.OK)
                    return null;

                result = form.GetResponse(out authorizationCode, out errorCode, out errorDescription);
                if (result != AuthorizationRequestResult.Success)
                {
                    // todo: consider displaying error information to the user

                    return null;
                }
            }


            /* Swap authorization code for an access token (and refresh token). */

            try
            {
                return await GetAccessTokenFromAuthorizationCodeAsync(authorizationCode);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error on requesting access token.  " + ex.ToString());

                // todo: consider displaying error information to the user

                return null;
            }
        }

        // Gets an access token (and refresh token) from the specified authorization code.
        // The return task will return a tuple whose first item contains the access token, whose second item
        // contains the refresh token.
        private async Task<Tuple<String, String>> GetAccessTokenFromAuthorizationCodeAsync(String code)
        {
            // Issue a POST request to swap the authorization code for an access token, in accordance with
            // Section 4.1.3 of http://www.ietf.org/rfc/rfc6749.txt
            HttpClient httpClient = null;
            try
            {
                httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                    "Basic",
                    Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(ClientId + ":" + GetClientSecret()))
                );
                var formUrlEncodedContent = new FormUrlEncodedContent(
                    new List<KeyValuePair<String, String>>()
                    { 
                        new KeyValuePair<String, String>("grant_type", "authorization_code"),
                        new KeyValuePair<String, String>("code", code),
                        new KeyValuePair<String, String>("redirect_uri", AuthorizationCodeRedirectUri),
                    });

                var response = await httpClient.PostAsync(AuthServerTokenUri, formUrlEncodedContent);

                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();

                // Parse the content as JSON.  Here we show the full range of information that can be extracted
                // from the response.
                var jsonObj = JObject.Parse(content);
                var accessToken = jsonObj["access_token"].ToString();
                var refreshToken = jsonObj["refresh_token"].ToString();
                var expiresIn = jsonObj["expires_in"].ToString();   // time in seconds
                var scope = jsonObj["scope"].ToString();            // should be "RevolutionWebApi"
                var tokenType = jsonObj["token_type"].ToString();   // should be "Bearer"
                var userId = jsonObj["user_id"].ToString();         // the user's unique identifier
                var userName = jsonObj["user_name"].ToString();     // the user's non-unique name

                // We're choosing to only return the access token and refresh token.
                return Tuple.Create(accessToken, refreshToken);
            }

            finally
            {
                if (httpClient != null)
                    httpClient.Dispose();
            }
        }

        // Returns a task that will return the XML representation of Revolution Web API's Service resource (or
        // null if an error occurs).
        private async Task<String> GetResourceFromWebApiAsync(String accessToken)
        {
            HttpClient httpClient = null;
            try
            {
                httpClient = new HttpClient();

                // Set the Authorization header to identify the user.  The access token is not base64-encoded here
                // because it is expected to already be base64-encodedi.
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                // Set the Accept header to indicate that we want an XML based resource representation.
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(
                        "application/xml"
                    ));

                // Send a GET request for the resource and await the response.
                var response = await httpClient.GetAsync(WebApiEntryPointUri);

                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsStringAsync();
            }

            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error on requesting data from Web API.  " + ex.ToString());
                return null;
            }

            finally
            {
                if (httpClient != null)
                    httpClient.Dispose();
            }
        }

        // Get this client application's 'client secret'.  How secret the "secret" is depends on the nature
        // of the application, how it is written, how it stores its secret, what environment it runs on, how
        // widely it is distributed, etc. etc.  Broadly speaking, publicly-available native client applications
        // cannot keep secrets, and so the client secret isn't a secret.  Nevertheless, applications shouldn't
        // publicly expose the client secret (e.g. this sample application doesn't store it in the App.config
        // file).  Also: to improve security, native client applications should keep refresh tokens hidden from
        // user view, and shouldn't persist them.
        // For further details: http://tools.ietf.org/html/rfc6819#section-4.1.1
        // The client secret is obtained upon successful registration of your app with StatPro.
        private String GetClientSecret()
        {
            return "<it's up to you>";
        }
    }
}
