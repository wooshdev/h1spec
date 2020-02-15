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
					client.Request(Settings.GeneralPath);
					/* All responses are allowed (for now) */
					return TestResult.Passed;
				} catch (Exception e) {
					return new TestResult(TestStatus.Error, e.Message);
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
					return new TestResult(TestStatus.Error, e.Message);
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
					HttpResponse response = client.Request(Settings.GeneralPath, null, null, "GET", "ABCD/1.1");

					if (response.StatusCode == 505) /* 505 HTTP Version Not Supported */
						return TestResult.Passed;
					if (response.StatusCode == 400) /* 400 Bad Request */
						return Result400;
					else
						return new TestResult(TestStatus.NonConformance, "The server has sent the status \"" + HttpStatuses.Format(response.StatusCode) + "\", when \"" + HttpStatuses.Format(400) + "\" or the better \"" + HttpStatuses.Format(505) + "\" status was expected.");

				} catch (Exception e) {
					return new TestResult(TestStatus.Error, e.Message);
				}
			}
		}

		class FutureVersion : Test {

			private static string Status500Formatted = HttpStatuses.Format(505);
			private static TestResult Result400 = new TestResult(TestStatus.NonConformance, "The server has sent a status of \"" + HttpStatuses.Format(400) + "\", but a better fitting status is \"" + Status500Formatted + "\".");
			public FutureVersion() : base("2.2", "Sending an newer (future) protocol version") {}
			
			public override TestResult Run(HttpClient client) {
				try {
					HttpResponse response = client.Request(Settings.GeneralPath, null, null, "GET", "HTTP/1.2");

					if (response.StatusCode == 505) /* 505 HTTP Version Not Supported */
						return TestResult.Passed;
					if (response.StatusCode == 400) /* 400 Bad Request */
						return Result400;
					if (response.StatusCode >= 300 && response.StatusCode <= 399)
						return new TestResult(TestStatus.NonConformance, "The server has sent the status \"" + HttpStatuses.Format(response.StatusCode) + "\", to mitigate this problem, when it could've used the '" + Status500Formatted + "\" status.");
					return new TestResult(TestStatus.NonConformance, "The server has sent the status \"" + HttpStatuses.Format(response.StatusCode) + "\", when \"" + HttpStatuses.Format(400) + "\" or the better \"" + Status500Formatted + "\" status was expected.");

				} catch (Exception e) {
					return new TestResult(TestStatus.Error, e.Message);
				}
			}
		}
	}

	namespace Flexibility {
		class HeaderWithOptionalSpaces : Test {

			private static TestResult FailedResultMultipleSpaces = new TestResult(TestStatus.NonConformance, "The server doesn't allow multiple spaces before the header-value. (Not conforming to RFC 7230 Section 3.2)");
			private static TestResult FailedResultNoOWS = new TestResult(TestStatus.NonConformance, "The server requires tabs/spaces after (header-name + ':'), white spaces are optional. (Not conforming to RFC 7230 Section 3.2)");
			private static TestResult FailedResultTabs = new TestResult(TestStatus.NonConformance, "The server doesn't allow tabs before the header-value. (Not conforming to RFC 7230 Section 3.2)");
			public HeaderWithOptionalSpaces() : base("3.1", "OWS before header value") {}
			
			public override TestResult Run(HttpClient client) {
				try {
					Dictionary<string, string> headers = new Dictionary<string, string> {
						{ "Connection", "     keep-alive" }
					};
					HttpResponse response = client.Request(Settings.GeneralPath, headers);

					if (response.StatusCode == 400)
						return FailedResultMultipleSpaces;

					client.Reconnect();
					response = client.Request(System.Text.Encoding.UTF8.GetBytes("GET " + Settings.GeneralPath + " HTTP/1.1\r\nAccept:*/*\r\nHost:" + client.HostName + "\r\nConnection:close\r\n\r\n"), "GET");
					if (response.StatusCode == 400)
						return FailedResultNoOWS;

					client.Reconnect();
					response = client.Request(System.Text.Encoding.UTF8.GetBytes("GET " + Settings.GeneralPath + " HTTP/1.1\r\nAccept:\t*/*\r\nHost:\t" + client.HostName + "\r\nConnection:\tclose\r\n\r\n"), "GET");
					if (response.StatusCode == 400)
						return FailedResultTabs;

					return TestResult.Passed;
				} catch (Exception e) {
					return new TestResult(TestStatus.Error, e.Message);
				}
			}
		}
	}
}

class TestManager {

	private static string FormatTimeSpan(TimeSpan span) {
		string result = "";
		if (span.Days > 0)
			result = span.Days + " days(!) ";
		if (span.Hours > 0)
			result += span.Hours + " hours ";
		if (span.Minutes > 0)
			result += span.Minutes + " minutes ";
		if (span.Seconds > 0)
			result += span.Seconds + " seconds ";
		if (span.Milliseconds > 0)
			result += span.Milliseconds + " ms";
		return result;
	}
	
	public static void RunTests(HttpClient client) {
		List<Test> tests = new List<Test>();
		tests.Add(new Tests.Methods.Get());
		tests.Add(new Tests.Methods.Options());
		tests.Add(new Tests.MalformedRequests.InvalidVersion());
		tests.Add(new Tests.MalformedRequests.FutureVersion());
		tests.Add(new Tests.Flexibility.HeaderWithOptionalSpaces());

		uint score = 0;
		DateTime startTime = DateTime.Now;

		foreach (Test test in tests) {
			Console.Write("> \x1b[37mTest {0} \x1b[0m[\x1b[35m{1}\x1b[0m] ", test.Identifier, test.Name);
			TestResult result = test.Run(client);
			if (result.Status != TestStatus.Passed) {
				Console.WriteLine("\x1b[31mfailed\x1b[0m.\n\t>>> ({0}) {1}", result.Status, result.Message);
				score += 1;
			} else {
				Console.WriteLine("\x1b[32mpassed\x1b[0m.");
			}
			client.Reconnect();
		}

		Console.Write("\n\x1b[35m==================================\n\x1b\x1b[34mTime:           \x1b[33m{0}\n\x1b[34mTest Count:     \x1b[33m{1}\n\x1b[34mPassed Tests:   \x1b[33m{2}\n\x1b[34mScore:          \x1b[33m{3}%\x1b[0m\n", FormatTimeSpan(DateTime.Now-startTime), tests.Count, score, ((int)(((double)score / tests.Count) * 10000))/100.0);
	}
}
