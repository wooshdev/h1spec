/**
 * Copyright (C) 2019-2020 Tristan
 * For conditions of distribution and use, see copyright notice in the COPYING file.
 */
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text;
using System.IO;

enum HttpClientReadSection {
	Initialization, StatusLine, Headers, Body
}

class HttpStandardException : Exception {
	public string Reference;
	public string Explanation;
	
	public HttpStandardException(string explanation, string reference) : base("'" + explanation + "' (" + reference + ")") {
		Explanation = explanation;
		Reference = reference;
	}
}

/**
 * Errors on "HttpClient"s which aren't related to the standard.
 */
class HttpClientException : Exception {

	public HttpClientException(string message) : base (message) {
	}
}

/**
 * The HTTP/1.1 client to test the server with. An instance CANNOT be used concurrently, 
 * and by doing so undefined behavior is bound to occur. Multiple instances are required 
 * for parallelism.
 */
class HttpClient {

	private TcpClient tcpClient;
	private Stream stream;
	private StreamReader reader;
	public string HostName { get; private set; }

	/**
	 * Tries to establish a connection to a server.
	 *
	 * Parameters:
	 *   host
	 *     The address for TcpClient to connect to.
	 *   port
	 *     The port of the remote TCP socket.
	 *   secure
	 *     Should we wrap the stream in a SslStream (i.e. enabling TLS)?
	 *   hostname
	 *     The name of the host (used in headers).
	 * 
	 * Return Value:
	 *   null on success, otherwise an error message describing the problem this function
	 *   encountered.
	 */
	public string Connect(string host, int port, bool secure, string hostName) {
		if (tcpClient != null)
			return null;

		HostName = hostName;

		try {
			tcpClient = new TcpClient(host, port);
			stream = tcpClient.GetStream();
		} catch (Exception e) {
			try { tcpClient.Close(); } catch { }
			return "TCP Failure: " + e.Message;
		}

		if (secure) {
			SslStream secureStream;
			try {
				secureStream = new SslStream(stream, true);
			} catch (Exception e) {
				try { tcpClient.Close(); } catch { }
				return "TLS Initialization Failure: " + e.Message;
			}

			try {
				secureStream.AuthenticateAsClient(host);
			} catch (AuthenticationException e) {
				try { tcpClient.Close(); } catch { }
				return "TLS Authentication Failure: " + e.Message;
			} catch (Exception e) {
				try { tcpClient.Close(); } catch { }
				return "TLS Generic Failure: " + e.Message;
			}

			stream = secureStream;
		}

		try {
			reader = new StreamReader(stream, Encoding.UTF8, false, 1024, true);
		} catch (Exception e) {
			try { stream.Close(); } catch { }
			try { tcpClient.Close(); } catch { }
			
			return "Reader Setup Failure: " + e.Message;
		}

		return null;
	}

	/**
	 * Makes a request to the server.
	 */
	public HttpResponse Request(string path) => Request(path, null, null);
	
	/**
	 * Makes a request to the server.
	 */
	public HttpResponse Request(string path, byte[] body) => Request(path, null, body);
	
	/**
	 * Makes a request to the server.
	 */
	public HttpResponse Request(string path, Dictionary<string, string> headers) => Request(path, headers, null);
	
	/**
	 * Makes a request to the server.
	 */
	public HttpResponse Request(string path, Dictionary<string, string> headers, byte[] body, string method="GET") {
		byte[] request = Encoding.UTF8.GetBytes(method + " " + path + " HTTP/1.1\r\nAccept: */*\r\nHost: " + HostName + "\r\nConnection: keep-alive\r\n\r\n");
		stream.Write(request, 0, request.Length);
		
		return ReadResponse(method);
	}
	
	private static void ThrowErrorStatusLine(string message, string source) => throw new HttpStandardException("Invalid status code: " + message + " (source=" + (source == null ? "null" : '"' + source + '"') + ')', "RFC 7230 Section 3.1.2.");
	private static void ThrowErrorVersion(string message, string source) => throw new HttpStandardException("Invalid version: " + message + " (source=" + (source == null ? "null" : '"' + source + '"') + ')', "RFC 7230 Section 2.6.");
	private static void ThrowErrorHeaderField(string message, string source) => throw new HttpStandardException("Invalid header field: " + message + " (source=" + (source == null ? "null" : '"' + source + '"') + ')', "RFC 7230 Section 3.2.");
	
	private static void checkHTTPVersion(string version) {
		if (version.Length != 8)
			ThrowErrorVersion("Version strings are eight (8) octets of length (this one is " + version.Length + " in length)", version);
		
		if (version[0] != 0x48 || version[1] != 0x54 || version[2] != 0x54 || version[3] != 0x50 || version[4] != '/')
			ThrowErrorVersion("Version strings must start with \"HTTP/\", not with \"" + version.Substring(0, 4) + "\"", version);

		if (!ABNF.IsDigit(version[5]))
			ThrowErrorVersion("The version major must be a DIGIT as defined by 'RFC 5234 ABNF (STD 68)' and not '" + version[5] + "'!", version);

		if (version[6] != '.')
			ThrowErrorVersion("The version major and minor must be seperated with a FULL STOP ('.') and not a '" + version[6] + "'!", version);

		if (!ABNF.IsDigit(version[7]))
			ThrowErrorVersion("The version minor must be a DIGIT as defined by 'RFC 5234 ABNF (STD 68)' and not '" + version[7] + "'!", version);
	}

	private static void checkHTTPStatusCode(string code) {
		if (code.Length != 3)
			ThrowErrorStatusLine("Status codes are three (3) octets of length,  (this one is " + code.Length + " in length)", code);
		if (!ABNF.IsDigit(code[0]) || !ABNF.IsDigit(code[1]) || !ABNF.IsDigit(code[2]))
			ThrowErrorStatusLine("The status code isn't a string of three DIGITS as defined by 'RFC 5234 ABNF (STD 68)'!", code);
		if (Convert.ToUInt16("" + code[0]) > 5)
			throw new HttpStandardException("Invalid status code: status codes of 600 or higher don't exist. (source=" + (code == null ? "null" : '"' + code + '"') + ')', "RFC 7231 Section 6.");
	}

	private static string isToken(string input) {
		/**
		 *  tchar = "!" / "#" / "$" / "%" / "&" / "'" / "*" / "+" / "-" / "." /
		 *			"^" / "_" / "`" / "|" / "~" / DIGIT / ALPHA
		 *	token = 1*tchar 
		 */
		if (input.Length == 0)
			return "tokens must be at least one (1) tchar in size";
		
		foreach (char c in input)
			if (c != '!' && c != '#' && c != '$' && c != '%' && c != '&' && c != '\'' && c != '*' && c != '+' && c != '-' && c != '.' &&
				c != '^' && c != '_' && c != '`' && c != '|' && c != '~' && !ABNF.IsDigit(c) && !ABNF.IsAlpha(c)) {
				return "'" + c + "' isn't a tchar";
			}
		
		return null;
	}

	private static int countRepeatingOWSCharacters(string input) {
		/**
		 * RFC 7230 Appendix B:
		 *	 OWS  = *( SP / HTAB )
		 *		  ; optional whitespace
		 *
		 * RFC 5234 Appendix B.1:
		 *	 SP   = %x20
		 *	 HTAB = %x09
		 */
		int counter = 0;
		while (input[counter] == ' ' || input[counter] == '\t')
			counter++;
		return counter;
	}
	
	private static bool isFieldVChar(char c) {
		/*
		 * field-vchar = VCHAR / obs-text
		 * VCHAR = (ABNF)
		 * obs-text = %x80-FF
		 */
		return ABNF.IsVChar(c) || (c >= 0x80 && c <= 0xFF);
	}
	
	private void readToResponse(HttpResponse response, int length, int start) {
		int position = 0;
		int read;
		do {
			read = reader.ReadBlock(response.Body, start + position, length - position);
			if (read == -1)
				throw new IOException("Reader returned -1");
		} while ((position += read) < length);
	}

	public HttpResponse ReadResponse(string method) {
		HttpClientReadSection section = HttpClientReadSection.Initialization;
		HttpResponse response = new HttpResponse();

		try {
			section = HttpClientReadSection.StatusLine;
			{
				string statusLine = reader.ReadLine();

				if (statusLine == null)
					ThrowErrorStatusLine("EOS reached.", statusLine);

				string[] parts = statusLine.Split(new char[] { ' ' }, 3);
				if (parts.Length < 3)
					ThrowErrorStatusLine("Invalid status line: the string doesn't contain at least three parts seperated by two spaces. (value=\"" + statusLine + "\")", "RFC 7230 Section 3.1.2.");

				try {
					checkHTTPVersion(parts[0]);
					checkHTTPStatusCode(parts[1]);
					/* the reason-phrase SHOULD be ignored by the client. */
				} catch (HttpStandardException e) {
					Console.WriteLine("Invalid Status Line: \"" + statusLine + "\"");
					throw e;
				}

				response.Version = parts[0];
				response.StatusCode = Convert.ToUInt16(parts[1]);
			}

			section = HttpClientReadSection.Headers;
			{
				string line;
				while ((line = reader.ReadLine()) != null && line.Length > 0) {
					int colonIndex = line.IndexOf(':');
					if (colonIndex == -1)
						ThrowErrorHeaderField("there isn't a seperating COLON (':') character between the field-name and field-value", line);

					string fieldName = line.Substring(0, colonIndex);
					string fieldNameIsToken = isToken(fieldName);

					if (fieldNameIsToken != null)
						ThrowErrorHeaderField("the field-name isn't a token as defined by RFC 7230 Appendix B (reason: " + fieldNameIsToken + ')', fieldName);

					int owsChars = countRepeatingOWSCharacters(line.Substring(colonIndex + 1));
					string fieldValue = line.Substring(colonIndex + 1 + owsChars);

					int endIndex = -1;
					for (int i = 0; i < fieldValue.Length; i++) {
						if (!isFieldVChar(fieldValue[i]))
							if (fieldValue[i] == ' ' || fieldValue[i] == '\t') {
								if (endIndex == -1)
									endIndex = i;
							} else
								ThrowErrorHeaderField("field value character '" + fieldValue[i] + "' (position=" + i + ") isn't a VCHAR as defined by RFC 7230 Appendix B.1.", fieldValue);
						endIndex = -1;
					}

					if (endIndex > 0)
						fieldValue = fieldValue.Substring(0, endIndex);

					response.Headers.Add(fieldName, fieldValue);
				}
			}

			section = HttpClientReadSection.Body;
			{
				bool isHead = method == "HEAD";
				bool sentContentLength = response.Headers.ContainsKey("Content-Length");
				bool sentTransferEncoding = response.Headers.ContainsKey("Transfer-Encoding");
				bool sentPayloadLength = sentContentLength || sentTransferEncoding;

				if (sentPayloadLength && response.StatusCode >= 100 && response.StatusCode <= 199)
					throw new HttpStandardException("The server sent a 'Content-Length' or 'Transfer-Encoding' header on a response with a 1xx status code.", "RFC 7230 Section 3.3.2.");

				if (sentPayloadLength && response.StatusCode == 204)
					throw new HttpStandardException("The server sent a 'Content-Length' or 'Transfer-Encoding' header on a response with a 204 status code.", "RFC 7230 Section 3.3.2. and Section 3.3.3.1.");

				if (sentContentLength && response.StatusCode == 304)
					throw new HttpStandardException("The server sent a 'Content-Length' header on a response with a 304 status code.", "RFC 7230 Section 3.3.3.1");

				if (method == "OPTIONS" && !sentPayloadLength)
					throw new HttpStandardException("The server didn't send a 'Content-Length' or 'Transfer-Encoding' header on an OPTIONS-method request", "RFC 7231 Section 4.3.7.");

				if (method == "CONNECT" && sentPayloadLength && response.StatusCode >= 200 && response.StatusCode <= 200)
					throw new HttpStandardException("The server sent a 'Content-Length' or 'Transfer-Encoding' header with a status code of 2xx on an CONNECT-method request", "RFC 7231 Section 4.3.6.");

				if (sentPayloadLength) {
					if (!isHead) {
						if (sentContentLength) {
							/* TODO doesn't support Transfer-Encoding yet! */
							uint ulength = Convert.ToUInt32(response.Headers["Content-Length"]);
							if (ulength > Int32.MaxValue)
								throw new Exception("ulength > int32 max (value=" + ulength + ")");
							int length = (int) ulength;
							Console.Write("(DEBUG) Reading {0} octets.\n", length);
							response.Body = new char[length];
							readToResponse(response, length, 0);
						} else if (sentTransferEncoding) {
							response.Body = new char[0];
							string line;
							while ((line = reader.ReadLine()) != null) {
								int length = (int) UInt32.Parse(line, NumberStyles.HexNumber);
								if (length == 0) {
									/* Seek two octets (there isn't a smarter method for this) */
									reader.Read();
									reader.Read();
									break;
								}

								int start = response.Body.Length;
								Array.Resize(ref response.Body, start + length);
								readToResponse(response, length, start);
								/* Seek two octets (there isn't a smarter method for this) */
								reader.Read();
								reader.Read();
							}
						} else {
							Console.WriteLine("(DEBUG) Neither Content-Length nor Transfer-Encoding is set?");
						}
					}
				} else {
					/* see 304 (et. al.) */
					Console.WriteLine("(DEBUG) No body sent.");
				}
			}
		} catch (OutOfMemoryException) {
			throw new HttpClientException("Out of memory! (Section: " + section + ")");
		} catch (IOException) {
			throw new HttpClientException("Connection lost. (Section: " + section + ")");
		}

		return response;
	}

	/**
	 * Closes the connection.
	 *
	 * Warning: Undefined behavior occurs when:
	 * - The connection hasn't been established (i.e. Connect() hasn't been called or hasn't finished);
	 * - The call to Connect() failed;
	 * - A request is happening;
	 * - Close() has already been called and a following call to Connect() hasn't been performed or completed;
	 * - Close() is being called on another thread.
	 */
	public void Close() {
		try { reader.Close(); } catch { }
		try { stream.Close(); } catch { }
		try { tcpClient.Close(); } catch { }
		tcpClient = null;
	}

}
