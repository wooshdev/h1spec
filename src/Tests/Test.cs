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
	namespace Section1 {

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

		class HeadA : Test {
			
			public HeadA() : base("1.2", "HEAD Test A") {}
			
			public override bool Run(HttpClient client) {
				
				
				return true;
			}
		}
	}

}

class TestManager {

	public static void RunTests(HttpClient client) {
		List<Test> tests = new List<Test>();
		tests.Add(new Tests.Section1.Get());

		foreach (Test test in tests) {
			if (!test.Run(client)) {
				Console.WriteLine("> Failed {0} ({1})\n", test.Name, test.Identifier);
			} else {
				Console.Write("> Test {0} (\"{1}\") passed.\n", test.Identifier, test.Name);
			}
		}
	}
}
