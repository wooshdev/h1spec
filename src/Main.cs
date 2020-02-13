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
		try {
			Url url = new Url(args[0]);
			Console.Write("Scheme: {0}\nUserInfo: {1}\nHost: {2}\nPath: {3}\nQuery: {4}\nFragment: {5}\n", 
						  url.Scheme ?? "(null)", 
						  url.UserInfo ?? "(null)", 
						  url.Host ?? "(null)", 
						  url.Path ?? "(null)", 
						  url.Query ?? "(null)", 
						  url.Fragment ??"(null)");
		} catch (Exception e) {
			Console.WriteLine(e.ToString());
		}
	}
}
