using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RevolutionWebApiNativeApp
{
    public partial class WebBrowserForm : Form
    {
        // Seeded from system clock.
        private static Random _random = new Random();

        // Used to detect XSRF attacks.
        private Int32 _state;

        // Input data.
        private String _clientId;
        private String _redirectUri;
        private String _webApiScopeId;
        private String _authorizationUri;

        // The results of an authorization request (for an authorization code).
        private AuthorizationRequestResult _result = AuthorizationRequestResult.NoResponse;
        private String _authorizationCode;
        private String _errorCode;
        private String _errorDescription;


        // Constructor.
        public WebBrowserForm()
        {
            InitializeComponent();
        }

        // Call this method before showing the form to provide the following data:-
        //   - the application's client id
        //   - the redirect URI (for the Authorization Code flow)
        //   - the Revolution Web API's scope identifier
        //   - the OAuth2 Server's authorization endpoint URI
        public void SetData(String clientId, String redirectUri, String webApiScopeId, String authorizationUri)
        {
            _clientId = clientId;
            _redirectUri = redirectUri;
            _webApiScopeId = webApiScopeId;
            _authorizationUri = authorizationUri;
        }

        // Call this method when the form closes (with an OK result) to get the response to the authorization request.
        //
        // For a Success result, a non-empty authorization code is passed back.
        //
        // For a Failed result, a non-empty error code is passed back.  This error code has *not* been checked
        // to see if it's one of the recognised error codes.  An error description is also passed back, but it
        // could be empty.
        //
        public AuthorizationRequestResult GetResponse(out String authorizationCode, out String errorCode,
            out String errorDescription)
        {
            authorizationCode = _authorizationCode;
            errorCode = _errorCode;
            errorDescription = _errorDescription;

            return _result;
        }

        // Called when the form is loaded.
        private void WebBrowserForm_Load(object sender, EventArgs e)
        {
            // Make an authorization request to the authorization server by redirecting the web browser control
            // to the authorization endpoint.

            _state = _random.Next();

            var uri = String.Format(CultureInfo.InvariantCulture,
                "{0}?response_type=code&client_id={1}&redirect_uri={2}&scope={3}&state={4}",
                _authorizationUri,
                Uri.EscapeDataString(_clientId),
                Uri.EscapeDataString(_redirectUri),
                Uri.EscapeDataString(_webApiScopeId),
                _state);

            webBrowser1.Url = new Uri(uri);
        }

        // Called as soon as the current document in the web browser control is fully loaded.
        private void webBrowser1_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            const String PathOfSuccessPage = "/OAuth2/AuthCodeRequestSuccess";
            const String PathOfFailedPage = "/OAuth2/AuthCodeRequestFailed";


            // If the authorization server has redirected to its Success page...
            if (String.Equals(e.Url.AbsolutePath, PathOfSuccessPage, StringComparison.OrdinalIgnoreCase))
            {
                ProcessAuthServerRedirect(true);

                webBrowser1.Url = new Uri("about:blank");
                DialogResult = System.Windows.Forms.DialogResult.OK;
                return;
            }

            // If the authorization server has redirected to its Failed page...
            if (String.Equals(e.Url.AbsolutePath, PathOfFailedPage, StringComparison.OrdinalIgnoreCase))
            {
                ProcessAuthServerRedirect(false);

                webBrowser1.Url = new Uri("about:blank");
                DialogResult = System.Windows.Forms.DialogResult.OK;
                return;
            }
        }

        // Processes an auth server redirection to either its Success page ('success' = true) or to its Failed page
        // ('success' = false).  As a result, sets up the following fields:-
        //    _result
        //    _authorizationCode
        //    _errorCode
        //    _errorDescription
        private void ProcessAuthServerRedirect(Boolean success)
        {
            // Get the title.
            var title = webBrowser1.DocumentTitle;
            if (String.IsNullOrWhiteSpace(title))
            {
                _result = AuthorizationRequestResult.InvalidResponse;
                return;
            }


            // On the Success page, we expect the title to be:
            //    Success code=<authorization code> state=<state>
            // We do not expect the authorization code or the state to contain spaces.
            if (success)
            {
                // Split the title into 3 parts.
                var parts = title.Split();
                if (parts.Length != 3)
                {
                    _result = AuthorizationRequestResult.InvalidResponse;
                    return;
                }

                // Check that the first part = "Success".
                if (parts[0] != "Success")
                {
                    _result = AuthorizationRequestResult.InvalidResponse;
                    return;
                }

                // Check that the second part starts with "code=".
                if (!parts[1].StartsWith("code=", StringComparison.Ordinal))
                {
                    _result = AuthorizationRequestResult.InvalidResponse;
                    return;
                }

                // Extract the authorization code.
                _authorizationCode = parts[1].Substring("code=".Length);
                if (_authorizationCode.Length == 0)
                {
                    _result = AuthorizationRequestResult.InvalidResponse;
                    return;
                }

                // Check that the third part starts with "state=".
                if (!parts[2].StartsWith("state=", StringComparison.Ordinal))
                {
                    _result = AuthorizationRequestResult.InvalidResponse;
                    return;
                }

                // Extract the state.
                var stateText = parts[2].Substring("state=".Length);

                // We'll say that an XSRF attack took place if the state is missing or if it doesn't match
                // the state that we sent.
                if (stateText != _state.ToString(CultureInfo.InvariantCulture))
                {
                    _result = AuthorizationRequestResult.XsrfDetected;
                    return;
                }

                // Success.
                _result = AuthorizationRequestResult.Success;
            }

            // On the Failed page, we expect the title to be:
            //    Failed error=<error code> error_description="<error description>" state=<state>
            // We do not expect the error code or the state to contain spaces.  The error description
            // may contain spaces.
            else
            {
                // Split the title into 4 or more parts.
                var parts = title.Split();
                if (parts.Length < 4)
                {
                    _result = AuthorizationRequestResult.InvalidResponse;
                    return;
                }

                // Check that the first part = "Failed".
                if (parts[0] != "Failed")
                {
                    _result = AuthorizationRequestResult.InvalidResponse;
                    return;
                }

                // Check that the second part starts with "error=".
                if (!parts[1].StartsWith("error=", StringComparison.Ordinal))
                {
                    _result = AuthorizationRequestResult.InvalidResponse;
                    return;
                }

                // Extract the error code.
                _errorCode = parts[1].Substring("error=".Length);
                if (_errorCode.Length == 0)
                {
                    _result = AuthorizationRequestResult.InvalidResponse;
                    return;
                }

                // Check that the third part starts with "error_description="".
                if (!parts[2].StartsWith("error_description=\"", StringComparison.Ordinal))
                {
                    _result = AuthorizationRequestResult.InvalidResponse;
                    return;
                }

                // Extract the error description (can be empty).
                var index1 = title.IndexOf("error_description=\"", StringComparison.Ordinal);
                index1 += "error_description=\"".Length;
                var index2 = title.LastIndexOf('"');
                if (index2 < index1)
                {
                    _result = AuthorizationRequestResult.InvalidResponse;
                    return;
                }
                _errorDescription = title.Substring(index1, index2 - index1);

                // Check that the last part starts with "state=".
                if (!parts[parts.Length - 1].StartsWith("state=", StringComparison.Ordinal))
                {
                    _result = AuthorizationRequestResult.InvalidResponse;
                    return;
                }

                // Extract the state.
                var stateText = parts[parts.Length - 1].Substring("state=".Length);

                // We'll say that an XSRF attack took place if the state is missing or if it doesn't match
                // the state that we sent.
                if (stateText != _state.ToString(CultureInfo.InvariantCulture))
                {
                    _result = AuthorizationRequestResult.XsrfDetected;
                    return;
                }

                // Failed.
                _result = AuthorizationRequestResult.Failed;
            }
        }
    }
}
