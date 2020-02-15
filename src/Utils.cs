/**
 * Copyright (C) 2019-2020 Tristan
 * For conditions of distribution and use, see copyright notice in the COPYING file.
 */
using System.Collections.Generic;

enum HttpStatusInfo {
	None, Obsolete, Deprecated, Unused, Reserved, AprilFools
}

class HttpStatus {

	public ushort Code;
	public string Message;
	public ulong RFC; /* 0 = unknown */
	public string Section; /* "" = unset/unknown/unspecified */
	public HttpStatusInfo Info;

	public HttpStatus(ushort code, string message, ulong rfc=0, string section=null, HttpStatusInfo info=HttpStatusInfo.None) {
		Code = code;
		Message = message;
		RFC = rfc;
		Section = section == null ? "" : ", Section " + section;
		Info = info;
	}

}

static class HttpStatuses {

	static HttpStatuses() {
		foreach (HttpStatus status in Statuses) {
			StatusesMap.Add(status.Code, status);
		}
	}

	public static HttpStatus Get(ushort code) {
		HttpStatus status;
		return StatusesMap.TryGetValue(code, out status) ? status : null;
	}

	public static string Format(ushort code) {
		HttpStatus status = Get(code);
		if (status == null)
			return code + " (Unknown)";
		else {
			string format = code + " " + status.Message;

			if (status.RFC != 0)
				format += " [RFC " + status.RFC + status.Section + "]";

			if (status.Info != HttpStatusInfo.None)
				format += " (" + status.Info + ")";

			return format;
		}
	}

	private static Dictionary<ulong, HttpStatus> StatusesMap = new Dictionary<ulong, HttpStatus>();

	/* See https://www.iana.org/assignments/http-status-codes/http-status-codes.xhtml */
	public static List<HttpStatus> Statuses = new List<HttpStatus> {
		/* 1×× Informational */
		new HttpStatus(100, "Continue", 7231, "6.2.1"),
		new HttpStatus(101, "Switching Protocols", 7231, "6.2.2"),
		new HttpStatus(102, "Processing", 2518, "10.1", HttpStatusInfo.Obsolete),
		new HttpStatus(103, "Early Hints", 8297, "2"),

		/* 2×× Success */
		new HttpStatus(200, "OK", 7231, "6.3.1"),
		new HttpStatus(201, "Created", 7231, "6.3.2"),
		new HttpStatus(202, "Accepted", 7231, "6.3.3"),
		new HttpStatus(203, "Non-authoritative Information", 7231, "6.3.4"),
		new HttpStatus(204, "No Content", 7231, "6.3.5"),
		new HttpStatus(205, "Reset Content", 7231, "6.3.6"),
		new HttpStatus(206, "Partial Content", 7233, "4.1"),
		new HttpStatus(207, "Multi-Status", 4918, "11.1"),
		new HttpStatus(208, "Already Reported", 5842, "7.1"),
		new HttpStatus(226, "IM Used", 3229, "10.4.1"),

		/* 3×× Redirection */
		new HttpStatus(300, "Multiple Choices", 7231, "6.4.1"),
		new HttpStatus(301, "Moved Permanently", 7231, "6.4.2"),
		new HttpStatus(302, "Found", 7231, "6.4.3"),
		new HttpStatus(303, "See Other", 7231, "6.4.4"),
		new HttpStatus(304, "Not Modified", 7232, "4.1"),
		new HttpStatus(305, "Use Proxy", 7231, "6.4.5", HttpStatusInfo.Deprecated),
		new HttpStatus(306, "(Unused)", 7231, "6.4.6", HttpStatusInfo.Unused),
		new HttpStatus(307, "Temporary Redirect", 7231, "6.4.7"),
		new HttpStatus(308, "Permanent Redirect", 7238, "3"),

		/* 4×× Client Error */
		new HttpStatus(400, "Bad Request", 7231, "6.5.1"),
		new HttpStatus(401, "Unauthorized", 7235, "3.1"),
		new HttpStatus(402, "Payment Required", 7231, "6.5.2", HttpStatusInfo.Reserved),
		new HttpStatus(403, "Forbidden", 7231, "6.5.3"),
		new HttpStatus(404, "Not Found", 7231, "6.5.4"),
		new HttpStatus(405, "Method Not Allowed", 7231, "6.5.5"),
		new HttpStatus(406, "Not Acceptable", 7231, "6.5.6"),
		new HttpStatus(407, "Proxy Authentication Required", 7235, "3.2"),
		new HttpStatus(408, "Request Timeout", 7231, "6.5.7"),
		new HttpStatus(409, "Conflict", 7231, "6.5.8"),
		new HttpStatus(410, "Gone", 7231, "6.5.9"),
		new HttpStatus(411, "Length Required", 7231, "6.5.10"),
		new HttpStatus(412, "Precondition Failed", 7232, "4.2"),
		new HttpStatus(413, "Payload Too Large", 7231, "6.5.11"),
		new HttpStatus(414, "URI Too Long", 7231, "6.5.12"),
		new HttpStatus(415, "Unsupported Media Type", 7231, "6.5.13"),
		new HttpStatus(416, "Requested Range Not Satisfiable", 7233, "4.1"),
		new HttpStatus(417, "Expectation Failed", 7231, "6.5.14"),
		new HttpStatus(418, "I'm a teapot", 2324, "2.3.2", HttpStatusInfo.AprilFools),
		new HttpStatus(421, "Misdirected Request", 7540, "9.1.2"),
		new HttpStatus(422, "Unprocessable Entity", 4918, "11.2"),
		new HttpStatus(423, "Locked", 4918, "11.3"),
		new HttpStatus(424, "Failed Dependency", 4918, "11.4"),
		new HttpStatus(426, "Upgrade Required", 7231, "6.5.15"),
		new HttpStatus(428, "Precondition Required", 6585, "3"),
		new HttpStatus(429, "Too Many Requests", 6585, "4"),
		new HttpStatus(431, "Request Header Fields Too Large", 6585, "5"),
		new HttpStatus(451, "Unavailable For Legal Reasons", 7725, "3"),

		/* 5×× Server Error */
		new HttpStatus(500, "Internal Server Error", 7231, "6.6.1"),
		new HttpStatus(501, "Not Implemented", 7231, "6.6.2"),
		new HttpStatus(502, "Bad Gateway", 7231, "6.6.3"),
		new HttpStatus(503, "Service Unavailable", 7231, "6.6.4"),
		new HttpStatus(504, "Gateway Timeout", 7231, "6.6.5"),
		new HttpStatus(505, "HTTP Version Not Supported", 7231, "6.6.6"),
		new HttpStatus(506, "Variant Also Negotiates", 2295, "8.1"),
		new HttpStatus(507, "Insufficient Storage", 4918, "11.5"),
		new HttpStatus(508, "Loop Detected", 5842, "7.2"),
		new HttpStatus(510, "Not Extended", 2774, "7"),
		new HttpStatus(511, "Network Authentication Required", 6585, "6"),
	};

}
