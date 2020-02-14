/**
 * Copyright (C) 2019-2020 Tristan
 * For conditions of distribution and use, see copyright notice in the COPYING file.
 */

/**
 * This class contains functions to verify characters using the rules of ABNF.
 * See 'Augmented BNF for Syntax Specifications: ABNF' (RFC 5234)
 */
static class ABNF {

	/* ALPHA          =  %x41-5A / %x61-7A   ; A-Z / a-z */
	public static bool IsAlpha(uint c) => (c >= 0x41 && c <= 0x5A) || (c >= 0x61 && c <= 0x7A);

	/* DIGIT       =  %x30-39 */
	public static bool IsDigit(uint c) => c >= 0x30 && c <= 0x39;

	/* HEXDIG         =  DIGIT / "A" / "B" / "C" / "D" / "E" / "F" */
	public static bool IsHexDig(uint c) => IsDigit(c) || (c >= 0x41 && c <= 0x46) || (c >= 0x61 && c <= 0x66); 

}
