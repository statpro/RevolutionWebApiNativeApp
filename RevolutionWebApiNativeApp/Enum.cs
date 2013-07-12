using System;

namespace RevolutionWebApiNativeApp
{
    // Defines identifiers for the result of an authorization request (for an authorization code).
    public enum AuthorizationRequestResult
    {
        // Success - the server responded with an authorization code + state.
        Success,

        // Failed - the server responded with an error, an error description + the state that we sent.
        Failed,

        // Cross-side request forgery detected - the server didn't respond with the state value that we sent.
        XsrfDetected,

        // The response from the server was invalid (we couldn't detect success or failure).
        InvalidResponse,

        // There was no recognizable response from the server.
        NoResponse,
    }
}
