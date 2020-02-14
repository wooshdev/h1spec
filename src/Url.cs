/**
 * Copyright (C) 2019-2020 Tristan
 * For conditions of distribution and use, see copyright notice in the COPYING file.
 */
using System;
using System.Text;

public class UrlException : Exception {

	public UrlException(string source, string message) : base("Invalid URL \"" + source + "\": " + message) {
	}
}

/**
 * A strict URL parser (RFC 3986 et. al.). I am using this for the sake of not having 
 * to depend on STL and allowing me to have more flexibility.
 */
public class Url {

	private string source;
	
	public string Scheme;
	/**
	 * RFC 1738 Defines a seperate username and password, 
	 * RFC 3986 doesn't, and since the latter is newer, 
	 * I'll folow that spec.
	 */
	public string UserInfo;
	public string Host;
	public ushort Port;
	
	public string Path;
	public string Query;
	public string Fragment;
	public string CombinedPath { get => Path ?? "/" + (Query ?? "") + (Fragment ?? ""); }
	
	public Url(string input) {
		source = input;
		
		int numberSign = input.IndexOf('#');
		if (numberSign != -1) {
			Fragment = input.Substring(numberSign + 1);
			input = input.Substring(0, numberSign);
			// Check fragment syntax
		}
		
		int questionMark = input.IndexOf('?');
		if (questionMark != -1) {
			Query = input.Substring(questionMark + 1);
			input = input.Substring(0, questionMark);
			// Check query syntax
		}
		
		int colon = input.IndexOf(':');
		if (colon == -1 || colon == 0) {
			throw new UrlException(input, "the scheme part wasn't found! (This is required, see RFC 3986 Section 1)");
		}
		
		Scheme = input.Substring(0, colon);
		input = input.Substring(colon+1);
		
		if (input.Length == 0) return;
		
		if (input.StartsWith("//")) {
			/* host        = IP-literal / IPv4address / reg-name */
			input = input.Substring(2);
			
			int slash = input.IndexOf('/');

			if (slash != -1) {
				Path = input.Substring(slash);
				input = input.Substring(0, slash);
			} else {
				Path = "";
			}

			int at = input.IndexOf('@');
			if (at != -1) {
				UserInfo = input.Substring(0, at);
				CheckUserInfo();
				input = input.Substring(at + 1);
			}
			
			Host = input;
			if (input[0] == '[') {
				/* IP-literal */
				Console.WriteLine("[TODO] String literals aren't supported yet (IPv6 and IPvFuture)");
			} else {
				/* a IPv4address or a reg-name */
				/*	IPv4address = dec-octet "." dec-octet "." dec-octet "." dec-octet*/
				bool isRegName = false;
				string[] parts = input.Split('.');
				if (parts.Length == 4) {
					/* it can still be a reg-name */
					for (int j = 0; j < 4 && !isRegName; j++) {
						if (!ConformsToDecOctet(parts[j]))
							isRegName = true;
					}
				}
				
				if (isRegName) {
					/* reg-name = *( unreserved / pct-encoded / sub-delims ) */
					string value = CheckString(input, false);
					if (value != null) {
						throw new UrlException(source, "invalid host '" + input + "'; " + value + " (See RFC 3986 Section 3.2.1)");
					}
				}
			}
		} else {
			Path = input;
		}
		
		source = null;
	}
	
	private string CheckString(string input, bool colon) {
		if (input.Length > 0) {
			for (int i = 0; i < input.Length; i++) {
				uint c = input[i];
				if ((!colon || c != ':') && !IsUnreserved(c) && !IsSubDelim(c)) {
					string character = "" + ((char) c);
					if (i + 2 < input.Length) {
						character += (char) input[i+1];
						character += (char) input[i+2];
						/* maybe a pct-encoded? 
						 * pct-encoded   = "%" HEXDIG HEXDIG
						 */
						if (c == '%' && ABNF.IsHexDig(input[i+1]) && ABNF.IsHexDig(input[i+2])) {
							i += 2;
							continue;
						} else {
							Console.Write("Values: {0} {1} {2}\n", c == '%', ABNF.IsHexDig(UserInfo[i+1]), ABNF.IsHexDig(UserInfo[i+2]));
						}
					}
					return "character(s) '" + character + "' (position=" + i + ") is invalid!";
				}
			}
		}
		return null;
	}
	
	private void CheckUserInfo() {
		/* userinfo    = *( unreserved / pct-encoded / sub-delims / ":" ) */
		string value = CheckString(UserInfo, true);
		if (value != null) {
			throw new UrlException(source, "invalid user-info, " + value + " (See RFC 3986 Section 3.2.1)");
		}
	}
	
	private bool ConformsToDecOctet(string s) {
		/* dec-octet    = DIGIT                 ; 0-9
						/ %x31-39 DIGIT         ; 10-99
						/ "1" 2DIGIT            ; 100-199
						/ "2" %x30-34 DIGIT     ; 200-249
						/ "25" %x30-35          ; 250-255 */
		if (s.Length == 0)
			return false;
		
		if (s.Length == 1)
			return ABNF.IsDigit(s[0]);
		
		if (s.Length == 2)
			return s[0] >= 0x31 && s[0] <= 0x39 && ABNF.IsDigit(s[1]);
		
		if (s.Length == 3)
			if (s[0] == '1')
				return ABNF.IsDigit(s[1]) && ABNF.IsDigit(s[2]);
			else if (s[0] == '2')
				if (s[1] == '5')
					return s[2] >= 0x30 && s[2] <= 0x35;
				else
					return s[1] >= 0x30 && s[1] <= 0x34 && ABNF.IsDigit(s[2]);
		
		
		return false;
	}

	
	
	/* unreserved  = ALPHA / DIGIT / "-" / "." / "_" / "~" */
	private bool IsUnreserved(uint c) => ABNF.IsAlpha(c) || ABNF.IsDigit(c) || c == '-' || c == '.' || c == '_' || c == '~';
	
	/* sub-delims  = "!" / "$" / "&" / "'" / "(" / ")"
				   / "*" / "+" / "," / ";" / "=" */
	private bool IsSubDelim(uint c) => c == '!' || c == '$' || c == '&' || c == '\'' || c == '(' || c == ')' 
									|| c == '*' || c == '+' || c == ',' || c == ';' || c == '=';

	private void CheckScheme() {
		/* scheme        = ALPHA *( ALPHA / DIGIT / "+" / "-" / "." ) */
		for (int i = 0; i < Scheme.Length; i++) {
			uint c = Scheme[i];
			if (i == 0) {
				if (!ABNF.IsAlpha(c)) {
					throw new UrlException(source, "schemes must start with an alphabethical character! (See RFC 3986 Section 3.1 and RFC 2234 Section 6.1)");
				}
			} else if (!ABNF.IsAlpha(c) && !ABNF.IsDigit(c) && c != '+' && c != '-' && c != '.') {
				throw new UrlException(source, "invalid scheme! (See RFC 3986 Section 3.1 and RFC 2234 Section 6.1)");
			}
		}
	}
}
