/**
 * Copyright (C) 2019-2020 Tristan
 * For conditions of distribution and use, see copyright notice in the COPYING file.
 */
using System;
using System.Collections.Generic;

abstract class Test {

	public string Identifier { get; protected set; }
	public string Name { get; protected set; }

	public Test(string id, string name) {
		Identifier = id;
		Name = name;
	}

	public abstract bool Run(HttpClient client);

}

namespace Tests {

	/* Method Section */
	namespace MethodSection {

		class Get : Test {
			
			public Get() : base("1.1", "GET '/'") {}
			
			public override bool Run(HttpClient client) {
				try {
					client.Request("/");
				} catch (Exception e) {
					Console.Write("== Failed ==\n\t{0}\n", e.ToString());
					return false;
				}
				
				return true;
			}
		}

		class Options : Test {

			public Options() : base("1.2", "OPTIONS") {}

			public override bool Run(HttpClient client) {
				try {
					HttpResponse response = client.Request("*", null, null, "OPTIONS");

					/*foreach (string name in response.Headers.Names) {
						Console.Write("\tHeader> '{0}' => '{1}'\n", name, response.Headers[name]);
					}*/

					return response.StatusCode < 400;
				} catch (Exception e) {
					Console.Write("== Failed ==\n\t{0}\n", e.ToString());
					return false;
				}
			}
		}
	}

}

class TestManager {

	public static void RunTests(HttpClient client) {
		List<Test> tests = new List<Test>();
		tests.Add(new Tests.MethodSection.Get());
		tests.Add(new Tests.MethodSection.Options());

		foreach (Test test in tests) {
			Console.Write("> \x1b[37mTest {0} \x1b[0m[\x1b[35m{1}\x1b[0m] ", test.Identifier, test.Name);
			if (!test.Run(client)) {
				Console.WriteLine("\x1b[31mfailed\x1b[0m.");
			} else {
				Console.WriteLine("\x1b[32mpassed\x1b[0m.");
			}
		}
	}
}
