﻿using System;
using System.Management.Automation;


namespace GoogleStorage.Objects
{
    [Cmdlet(VerbsCommon.Get, "GoogleStorageObjectACL")]
    public class GetGoogleStorageBucketACL : GoogleStorageAuthenticatedCmdlet
    {
        /// <summary>
        /// The bucket where the object exists
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true)]
        public string Bucket { get; set; }

        /// <summary>
        /// The name of the object
        /// </summary>
        [Parameter(Mandatory = true, Position = 1, ValueFromPipelineByPropertyName = true)]
        public string ObjectName { get; set; }

        /// <summary>
        /// An entity name to retrieve the ACL for
        /// If not set all ACLs are returned
        /// </summary>
        [Parameter(Mandatory = false, Position = 2, ValueFromPipelineByPropertyName = true)]
        public string EntityName { get; set; }

        protected override void ProcessRecord()
        {
            try
            {
                using (var api = CreateApiWrapper())
                {
                    if (string.IsNullOrEmpty(EntityName))
                    {
                        dynamic acls = api.GetObjectACL(Bucket, ObjectName).WaitForResult(CancellationToken);
                        foreach (var acl in acls.items)
                        {
                            WriteDynamicObject(acl);
                        }
                    }
                    else
                    {
                        dynamic acl = api.GetObjectACL(Bucket, ObjectName, EntityName).WaitForResult(CancellationToken);
                        WriteDynamicObject(acl);
                    }
                }
            }
            catch (Exception e)
            {
                HandleException(e);
            }
        }
    }
}
