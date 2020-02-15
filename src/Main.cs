/**
 * Copyright (C) 2019-2020 Tristan
 * For conditions of distribution and use, see copyright notice in the COPYING file.
 */
using System;

class MainClass {

	public static void Main(string[] args) {
		if (args.Length == 0) {
			Console.WriteLine("Add URL argument!");
			return;
		}
		HttpClient client = new HttpClient();
		
		try {
			Url url = new Url(args[0]);
			url.Scheme = url.Scheme.ToLower();
			
			int port;
			bool secure;
			if (url.Scheme == "http") {
				port = 80;
				secure = false;
			} else if (url.Scheme == "https") {
				port = 443;
				secure = true;
			} else {
				Console.Write("Unknown scheme: '{0}'\n", url.Scheme);
				return;
			}

			try {
				client.Connect(url.Host, port, secure, url.Host);
			} catch (Exception e) {
				Console.WriteLine("Failed to connect to client.\n\tMessage: " + e.Message);
			}
			
			TestManager.RunTests(client);
		} catch (Exception e) {
			Console.WriteLine(e.ToString());
		} finally {
			if (client != null)
				try { client.Close(); } catch { }
		}
	}
}
