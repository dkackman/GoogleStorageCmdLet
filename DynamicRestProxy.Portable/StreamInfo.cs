﻿using System;
using System.IO;

namespace DynamicRestProxy.PortableHttpClient
{
    /// <summary>
    /// Wrapper class for a stream that relates the stream to meta-data (MIME type)
    /// about the stream so meta data can be added to content headers
    /// </summary>
    public class StreamInfo : ContentInfo, IDisposable
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="stream">stream</param>
        /// <param name="mimeType">MIME type</param>
        public StreamInfo(Stream stream, string mimeType = "application/octet-stream")
            : base(stream, mimeType)
        {
        }

        /// <summary>
        /// Disposes the underlying stream when called
        /// </summary>
        public void Dispose()
        {
            if (Content != null)
            {
                ((IDisposable)Content).Dispose();
            }
        }
    }
}
