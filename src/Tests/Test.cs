/**
 * Copyright (C) 2019-2020 Tristan
 * For conditions of distribution and use, see copyright notice in the COPYING file.
 */
using System;
using System.Collections.Generic;

public enum TestStatus {
	Passed, NonConformance, NotSupported, Unexpected, Error
}

public class TestResult {

	public static TestResult Passed { get; } = new TestResult(TestStatus.Passed, null);
	
	public TestStatus Status { get; }
	public string Message { get; }

	public TestResult(TestStatus status, string message) {
		Status = status;
		Message = message;
	}

	public TestResult() : this(TestStatus.Passed, null) {}
}

abstract class Test {

	public string Identifier { get; protected set; }
	public string Name { get; protected set; }

	public Test(string id, string name) {
		Identifier = id;
		Name = name;
	}

	public abstract TestResult Run(HttpClient client);

}

namespace Tests {

	/* Method Section */
	namespace Methods {
		class Get : Test {
			
			public Get() : base("1.1", "Testing method GET") {}
			
			public override TestResult Run(HttpClient client) {
				try {
					client.Request("/");
					/* All responses are allowed (for now) */
					return TestResult.Passed;
				} catch (Exception e) {
					return new TestResult(TestStatus.Error, e.ToString());
				}
			}
		}

		class Options : Test {
			
			public Options() : base("1.2", "Testing method OPTIONS") {}

			public override TestResult Run(HttpClient client) {
				try {
					HttpResponse response = client.Request("*", null, null, "OPTIONS");

					/*foreach (string name in response.Headers.Names) {
						Console.Write("\tHeader> '{0}' => '{1}'\n", name, response.Headers[name]);
					}*/

					return response.StatusCode < 400 ? TestResult.Passed : new TestResult(TestStatus.NotSupported, "The server doesn't support the OPTIONS header, as the server sent response status: " + HttpStatuses.Format(response.StatusCode));
				} catch (Exception e) {
					Console.Write("== Failed ==\n\t{0}\n", e.ToString());
					return new TestResult(TestStatus.Error, e.ToString());
				}
			}
		}
	}

	namespace MalformedRequests {
		class InvalidVersion : Test {

			private static TestResult Result400 = new TestResult(TestStatus.NonConformance, "The server has sent a status of \"" + HttpStatuses.Format(400) + "\", but a better fitting status is \"" + HttpStatuses.Format(505) + "\".");
			public InvalidVersion() : base("2.1", "Sending an invalid protocol version") {}
			
			public override TestResult Run(HttpClient client) {
				try {
					HttpResponse response = client.Request("/", null, null, "GET", "ABCD/1.1");

					if (response.StatusCode == 505) /* 505 HTTP Version Not Supported */
						return TestResult.Passed;
					if (response.StatusCode == 400) /* 400 Bad Request */
						return Result400;
					else
						return new TestResult(TestStatus.NonConformance, "The server has sent the status \"" + HttpStatuses.Format(response.StatusCode) + "\", when \"" + HttpStatuses.Format(400) + "\" or the better \"" + HttpStatuses.Format(505) + "\" status was expected.");

				} catch (Exception e) {
					Console.WriteLine(e.ToString());
					return new TestResult(TestStatus.Error, e.ToString());
				}
			}
		}
	}

}

class TestManager {

	public static void RunTests(HttpClient client) {
		List<Test> tests = new List<Test>();
		tests.Add(new Tests.MalformedRequests.InvalidVersion());
		tests.Add(new Tests.Methods.Get());
		tests.Add(new Tests.Methods.Options());

		foreach (Test test in tests) {
			Console.Write("> \x1b[37mTest {0} \x1b[0m[\x1b[35m{1}\x1b[0m] ", test.Identifier, test.Name);
			TestResult result = test.Run(client);
			if (result.Status != TestStatus.Passed) {
				Console.WriteLine("\x1b[31mfailed\x1b[0m.\n\t>>> ({0}) {1}", result.Status, result.Message);
			} else {
				Console.WriteLine("\x1b[32mpassed\x1b[0m.");
			}
		}
	}
}
