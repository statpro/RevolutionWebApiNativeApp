
# Introduction #

This is a lightweight sample that shows how a Native application - in this case a Windows Forms application that runs on the Windows desktop - can get user data from the StatPro Revolution Web API.  This involves talking to the StatPro Revolution OAuth2 Server to get:-
* the user's consent for access to the Web API
* an access token

The sample is written in C# 5 and .NET 4.5.  The main technique that is demonstrated is that of directing the user to the Revolution OAuth2 Server by using an embedded WebBrowser control, and then reading the result (access granted or denied) from the web page that the OAuth2 Server redirects to.

This is not production quality code.  It is intended to show certain useful techniques that are particular to a Native application.  Real applications should be coded more carefully, paying particular attention to OAuth 2.0 security issues.

*You should not expect the sample application listed here to run successfully.  It requires a genuine client identifier and client secret to be made available, and this information is only made available to you when you register your own Native client application.*


# Revolution Web API #

The StatPro Revolution Web API allows client applications to access user data from the [StatPro Revolution system](http://www.statpro.com/cloud-based-portfolio-analysis/revolution/) programmatically.

User authentication and authorization is handled by StatPro OAuth2 Server, which in the case of Native applications (as well as Server-side Web applications) uses OAuth 2.0's 'Authorization Code' flow.

To run your own Native application, you must first register it with StatPro.

For more information:-
* [Revolution Web API](http://developer.statpro.com/Revolution/WebApi/Intro)
* [Revolution OAuth2 Server](http://developer.statpro.com/Revolution/WebApi/Authorization/Overview)
* [Registering your own application](http://developer.statpro.com/Revolution/WebApi/Authorization/Registration)
* [Revolution OAuth2 application workflow](http://developer.statpro.com/Revolution/WebApi/Authorization/Workflow)
* [OAuth 2.0](http://tools.ietf.org/html/rfc6749)
* [OAuth 2.0 Native application characteristics](http://tools.ietf.org/html/rfc6749#section-9)
* [OAuth 2.0 client security considerations](http://tools.ietf.org/html/rfc6819#section-4.1)
* [Revolution Web API and OAuth2 Support](mailto:webapisupport@statpro.com)


# What the sample demonstrates #

The sample demonstrates:-
* embedding a WebBrowser control on a WinForms form
* displaying the form modally, and redirecting the browser control to the OAuth2 Server's authorization endpoint with an authorization request for access to the Revolution Web API 
* detecting when the browser control has been redirected to the OAuth2 Server's internal Success or Failed pages (for an Authorization Code request)
* parsing the result from the page's document title text
* detecting a potential cross-side request forgery (XSRF) attack
* for a successful request, swapping the authorization code for an access token and a refresh token
* requesting data from the Web API, using the access token.


# What the sample does not demonstrate #

The following techniques are not demonstrated by this simple sample application.  Nevertheless, they should be implemented by production-quality client applications:-
* getting [portfolios](http://developer.statpro.com/Revolution/WebApi/Resource/Portfolios), [analysis](http://developer.statpro.com/Revolution/WebApi/Resource/PortfolioAnalysis) and results data from the Web API
* detecting if the Web API has returned one of its [specific errors](http://developer.statpro.com/Revolution/WebApi/Intro#statusCodes)
* detecting request blockage by the Web API due to a [Fair Usage Policy violation](http://developer.statpro.com/Revolution/WebApi/FairUsagePolicy)
* detecting if the Web API has [rejected the access token because it has expired](http://developer.statpro.com/Revolution/WebApi/Authorization/Workflow#step4)
* getting a [new access token from a refresh token](http://developer.statpro.com/Revolution/WebApi/Authorization/Workflow#step5)
* [re-prompting the user for access](http://developer.statpro.com/Revolution/WebApi/Authorization/Workflow#step1) if getting an access token from a refresh token fails

Please see other samples on GitHub that cover these techniques.


# License #


THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES WITH REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY SPECIAL, DIRECT, INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE OR PERFORMANCE OF THIS SOFTWARE.

 