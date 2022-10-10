using System.Diagnostics;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Newtonsoft.Json.Linq;

namespace Rock3t.Telegram.WebApi
{
    public static class OAuth2Authenticator
    {
        public static UserCredential Authenticate(string oauthClient, string oauthSecret, string[] scopes, bool saveState = true)
        {
            if (oauthClient == null || oauthSecret == null || scopes == null)
                throw new ArgumentNullException();
            try
            {
                UserCredential credentials = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    new ClientSecrets
                    {
                        ClientId = oauthClient,
                        ClientSecret = oauthSecret,
                    },
                    scopes, "user", CancellationToken.None).Result;

                if (saveState) SaveState(credentials);
                return credentials;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                throw;
            }
        }

        public static UserCredential Authenticate(string jsonPath, string[] scopes, bool saveState = true)
        {
            JObject jsonFile = JObject.Parse(File.ReadAllText(jsonPath));
            string clientId = jsonFile["installed"]?["client_id"]?.ToString();
            string clientSecret = jsonFile["installed"]?["client_secret"]?.ToString();
            return Authenticate(clientId, clientSecret, scopes);
        }

        private static UserCredential LoadExistingGoogleToken(string oauthClient, string oauthSecret)
        {
            try
            {
                GoogleAuthorizationCodeFlow flow = CreateAuthenticationFlow(oauthClient, oauthSecret);
                JObject token = JObject.Parse(File.ReadAllText("googleToken.json"));
                TokenResponse tokenResponse = new TokenResponse()
                {
                    AccessToken = token["accessToken"]?.ToString() ?? throw new ArgumentNullException(),
                    RefreshToken = token["refreshToken"]?.ToString() ?? throw new ArgumentNullException(),
                    ExpiresInSeconds = long.Parse(token["expiresInSeconds"]?.ToString() ?? throw new ArgumentNullException()),
                    IdToken = token["idToken"]?.ToString() ?? throw new ArgumentNullException(),
                    IssuedUtc = DateTime.Parse(token["issuedUtc"]?.ToString()),
                    Scope = token["scope"]?.ToString() ?? throw new ArgumentNullException(),
                    TokenType = token["tokenType"]?.ToString() ?? throw new ArgumentNullException()
                };
                UserCredential credentials = new UserCredential(flow, "user", tokenResponse);
                return credentials;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                throw;
            }
        }

        private static GoogleAuthorizationCodeFlow CreateAuthenticationFlow(string oauthClient, string oauthSecret)
        {
            var authorization = new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets
                {
                    ClientId = oauthClient,
                    ClientSecret = oauthSecret
                }
            };
            var flow = new GoogleAuthorizationCodeFlow(authorization);
            return flow;
        }

        public static UserCredential ReAuthenticate(UserCredential token, bool saveState = true)
        {
            if (token == null)
                throw new ArgumentNullException(nameof(token));
            try
            {
                GoogleWebAuthorizationBroker.ReauthorizeAsync(token, CancellationToken.None).Wait();
                if (saveState) SaveState(token);
                return token;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                throw;
            }
        }

        private static void SaveState(UserCredential credential)
        {
            JObject googleToken = new JObject()
            {
                new JProperty("accessToken", credential.Token.AccessToken),
                new JProperty("refreshToken", credential.Token.RefreshToken),
                new JProperty("expiresInSeconds", credential.Token.ExpiresInSeconds),
                new JProperty("idToken", credential.Token.IdToken),
                new JProperty("issuedUtc", credential.Token.IssuedUtc.ToString("yyyy-MM-dd hh:mm:ss.fff")),
                new JProperty("tokenType", credential.Token.TokenType),
                new JProperty("scope", credential.Token.Scope)
            };

            File.WriteAllText("googleToken.json", googleToken.ToString());
        }
    }
}