﻿using System;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Security;

using Microsoft.CSharp.RuntimeBinder;

using DynamicRestProxy.PortableHttpClient;

namespace GoogleStorage
{
    /// <summary>
    /// Helper class to deal with google oauth for unit testing
    /// The first time a machine authenticates user interaction is required
    /// Subsequent unit test runs will use a stored token that will refresh with google
    /// </summary>
    public sealed class GoogleOAuth2 : IDisposable
    {
        // the set of scopes to authorize
        private string _scope;
        private dynamic _google;

        public GoogleOAuth2(string scope)
        {
            Debug.Assert(!string.IsNullOrEmpty(scope));

            _google = new DynamicRestClient("https://accounts.google.com/o/oauth2/");
            _scope = scope;
        }

        public void Dispose()
        {
            ((IDisposable)_google).Dispose();
        }

        public async Task<dynamic> StartAuthentication(string clientId)
        {
            return await StartAuthentication(clientId, CancellationToken.None);
        }

        public async Task<dynamic> StartAuthentication(string clientId, CancellationToken cancelToken)
        {
            return await _google.device.code.post(cancelToken, client_id: clientId, scope: _scope);
        }

        public async Task<dynamic> RefreshAccessToken(dynamic access, dynamic config, CancellationToken cancelToken)
        {
            SecureString clientSecret = config.ClientSecret;

            var response = await _google.token.post(cancelToken, client_id: config.ClientId, client_secret: clientSecret.ToUnsecureString(), refresh_token: access.refresh_token, grant_type: "refresh_token");

            response.refresh_token = access.refresh_token; // the new access token doesn't have a new refresh token so move our current one here for long term storage
            response.expiry = DateTime.UtcNow + TimeSpan.FromSeconds(response.expires_in);

            // replace the plain text access_token with a securestring
            response.access_token = ((string)response.access_token).ToSecureString();

            return response;
        }

        /// <summary>
        /// This authenticates against user and requires user interaction to authorize the unit test to access apis
        /// This will do the auth, put the auth code on the clipboard and then open a browser with the app auth permission page
        /// The auth code needs to be sent back to google
        /// 
        /// This should only need to be done once because the access token will be stored and refreshed for future test runs
        /// </summary>
        /// <returns></returns>
        public async Task<dynamic> WaitForConfirmation(dynamic confirmToken, string clientId, SecureString clientSecret, CancellationToken cancelToken)
        {
            long expiration = confirmToken.expires_in;
            long interval = confirmToken.interval;
            long time = interval;

            // we are using the device flow so enter the code in the browser
            // here poll google for success
            while (time < expiration)
            {
                Thread.Sleep((int)interval * 1000);
                cancelToken.ThrowIfCancellationRequested();

                dynamic tokenResponse = await _google.token.post(cancelToken, client_id: clientId, client_secret: clientSecret.ToUnsecureString(), code: confirmToken.device_code, grant_type: "http://oauth.net/grant_type/device/1.0");
                try
                {
                    if (tokenResponse.access_token != null)
                    {
                        tokenResponse.expiry = DateTime.UtcNow + TimeSpan.FromSeconds(tokenResponse.expires_in);
                        // replace the plain text access_token with a securestring
                        tokenResponse.access_token = ((string)tokenResponse.access_token).ToSecureString();
                        return tokenResponse;
                    }
                }
                catch (RuntimeBinderException)
                {
                }

                time += interval;
            }

            throw new OperationCanceledException("Authorization from user timed out");
        }
    }
}
