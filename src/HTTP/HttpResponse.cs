/**
 * Copyright (C) 2019-2020 Tristan
 * For conditions of distribution and use, see copyright notice in the COPYING file.
 */
using System;
using System.Collections.Generic;

class HttpHeaders {

	public List<string> Names { get; } = new List<string>();
	public List<string> Values { get; } = new List<string>();

	public void Add(string name, string value) {
		Names.Add(name);
		Values.Add(value);
	}

	public bool ContainsKey(string name) => Names.Contains(name);
	
	public string this[string name] {
		get { 
			int index = Names.FindIndex(nameInList => nameInList.Equals(name, StringComparison.OrdinalIgnoreCase));
			if (index == -1)
				return null;
			else
				return Values[index];
		}
	}
}

class HttpResponse {

	public string Version;
	public ushort StatusCode;
	/* the ReasonPhrase SHOULD be ignored by the client (RFC 7230 Section 3.1.2.) */

	public HttpHeaders Headers { get; } = new HttpHeaders();

	public char[] Body = null;

} 
