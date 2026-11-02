using System;
using System.Collections.Generic;
using System.Text;

namespace RIMAPI.Core
{
    /// <summary>
    /// Rewrites the head of an incoming request before the HTTP listener parses
    /// it (fox ticket #4233).
    /// </summary>
    /// <remarks>
    /// Mono's HttpListener answers "411 Length Required" to a POST or a PUT that
    /// carries neither Content-Length nor Transfer-Encoding, and it answers
    /// before any of our code runs, so such a request never reaches the router:
    /// `curl -X POST .../set-default?id=5` failed while `curl -X POST -d ''`
    /// worked. The listener also matches the request's Host against its
    /// prefixes, so a request that names the public port is refused once the
    /// listener sits on a private one. Both are corrected here, on the head
    /// alone; the body and the answer stream through untouched.
    /// </remarks>
    public static class RequestFraming
    {
        /// <summary>
        /// ISO-8859-1: every header byte is one char, so a head that round-trips
        /// through this encoding is the same head, byte for byte.
        /// </summary>
        private static readonly Encoding HeadEncoding = Encoding.GetEncoding(28591);

        /// <summary>
        /// The methods the listener insists on a length for. Measured on the 1.6
        /// build (2026-11-02): POST and PUT answer 411, DELETE and PATCH do not.
        /// </summary>
        private static readonly string[] MethodsNeedingLength = { "POST", "PUT" };

        public static bool NeedsLength(string method)
        {
            if (string.IsNullOrEmpty(method))
                return false;
            foreach (string candidate in MethodsNeedingLength)
            {
                if (string.Equals(candidate, method, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// The head to hand the listener: Host pointed at the listener's own
        /// address, a Content-Length where the method needs one, and
        /// "Connection: close" so one connection carries one request.
        /// </summary>
        /// <param name="head">The request head, up to and including its blank line.</param>
        /// <param name="upstreamHost">Host[:port] the listener is bound to, e.g. "127.0.0.1:8767".</param>
        public static byte[] Normalize(byte[] head, string upstreamHost)
        {
            if (head == null || head.Length == 0)
                throw new ArgumentException("a request head is needed", nameof(head));
            if (string.IsNullOrEmpty(upstreamHost))
                throw new ArgumentException("the listener's host is needed", nameof(upstreamHost));

            string text = HeadEncoding.GetString(head);
            string headText = TrimHeadEnd(text);
            string[] lines = headText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            if (lines.Length == 0 || lines[0].Length == 0)
                return head; // not a request head we understand: let the listener answer it

            string requestLine = lines[0];
            string[] requestParts = requestLine.Split(' ');
            string method = requestParts.Length > 0 ? requestParts[0] : string.Empty;

            var output = new StringBuilder(head.Length + 64);
            output.Append(requestLine).Append("\r\n");
            // The listener dispatches on the Host it was given, and this one is
            // private to the process, so the request carries it either way.
            output.Append("Host: ").Append(upstreamHost).Append("\r\n");

            bool hasLength = false;
            bool hasTransferEncoding = false;
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i];
                if (line.Length == 0)
                    continue;

                int colon = line.IndexOf(':');
                if (colon > 0)
                {
                    string name = line.Substring(0, colon);
                    if (name.Equals("Host", StringComparison.OrdinalIgnoreCase))
                        continue; // written above, in the listener's own form
                    if (name.Equals("Connection", StringComparison.OrdinalIgnoreCase))
                        continue; // written below, as close
                    if (name.Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
                        hasLength = true;
                    else if (name.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase))
                        hasTransferEncoding = true;
                }

                output.Append(line).Append("\r\n");
            }

            if (NeedsLength(method) && !hasLength && !hasTransferEncoding)
                output.Append("Content-Length: 0\r\n");
            output.Append("Connection: close\r\n");
            output.Append("\r\n");

            return HeadEncoding.GetBytes(output.ToString());
        }

        /// <summary>The head without its closing blank line, whatever line ending it used.</summary>
        private static string TrimHeadEnd(string text)
        {
            int end = text.IndexOf("\r\n\r\n", StringComparison.Ordinal);
            if (end >= 0)
                return text.Substring(0, end);
            end = text.IndexOf("\n\n", StringComparison.Ordinal);
            if (end >= 0)
                return text.Substring(0, end);
            return text.TrimEnd('\r', '\n');
        }

        /// <summary>The index just past the head's blank line, or -1 while it is incomplete.</summary>
        public static int HeadEnd(byte[] buffer, int length)
        {
            for (int i = 0; i + 3 < length; i++)
            {
                if (
                    buffer[i] == '\r'
                    && buffer[i + 1] == '\n'
                    && buffer[i + 2] == '\r'
                    && buffer[i + 3] == '\n'
                )
                    return i + 4;
            }
            for (int i = 0; i + 1 < length; i++)
            {
                if (buffer[i] == '\n' && buffer[i + 1] == '\n')
                    return i + 2;
            }
            return -1;
        }
    }
}
