﻿using System;
using System.Security;
using System.Threading.Tasks;
using System.Management.Automation;

namespace GoogleStorage
{
    public abstract class GoogleStorageAuthenticatedCmdlet : GoogleStorageCmdlet
    {
        private const string UserAgent = "GoogleStorageCmdlets/0.1";

        /// <summary>
        /// Flag indicating that no authentication is needed for the command execution
        /// i.e. the storage item being operated on is publically shared
        /// Stored authentication will be ignored
        /// </summary>
        [Parameter(Mandatory = false)]
        public SwitchParameter NoAuth { get; set; }

        protected GoogleStorageApi CreateApiWrapper()
        {
            var access_token = GetAccessToken().WaitForResult(CancellationToken);

            return new GoogleStorageApi(UserAgent, access_token, CancellationToken);
        }

        protected async Task<SecureString> GetAccessToken(bool persist = true)
        {
            if (NoAuth)
            {
                return null;
            }

            var access = GetPersistedVariableValue<dynamic>("access", d =>
                {
                    d.access_token = ((string)d.access_token).FromEncyptedString();
                    return d;
                });

            if (access == null)
            {
                throw new AccessViolationException("Access token not set. Call Grant-GoogleStorageAccess first");
            }

            if (DateTime.UtcNow >= access.expiry)
            {
                var config = GetConfig();
                using (var oauth = new GoogleOAuth2(config.ClientId, GoogleStorageApi.AuthScope))
                {
                    access = await oauth.RefreshAccessToken(access.refresh_token, config.ClientSecret, CancellationToken);

                    var storage = new PersistantStorage();
                    SetPersistedVariableValue("access", access, persist || storage.ObjectExists("access")); // re-persist access token if already saved
                }
            }

            return access.access_token;
        }
    }
}
