// HomiKey keyboard-layout converter for the browser.
// A line-for-line port of src/HomiKey.Core; both are tested against tests/vectors.json.
// No dependencies, no network access.

/** The 47 printable keys on US QWERTY, unshifted, in row order. */
export const US_UNSHIFTED = "`1234567890-=qwertyuiop[]\\asdfghjkl;'zxcvbnm,./";
/** The same keys with Shift held. */
export const US_SHIFTED = "~!@#$%^&*()_+QWERTYUIOP{}|ASDFGHJKL:\"ZXCVBNM<>?";

const ZWNJ = "\u200C";
const ZWJ = "\u200D";

/** What each Persian layout types on each US key. */
export const LAYOUTS = {
  // ISIRI 9147, the Iranian standard ("Persian (Standard)" in Windows).
  standard: {
    unshifted: [
      ZWJ, "۱", "۲", "۳", "۴", "۵", "۶", "۷", "۸", "۹", "۰", "-", "=",
      "ض", "ص", "ث", "ق", "ف", "غ", "ع", "ه", "خ", "ح", "ج", "چ", "\\",
      "ش", "س", "ی", "ب", "ل", "ا", "ت", "ن", "م", "ک", "گ",
      "ظ", "ط", "ز", "ر", "ذ", "د", "پ", "و", ".", "/",
    ],
    shifted: [
      "÷", "!", "\u066C", "\u066B", "\uFDFC", "\u066A", "×", "،", "*", ")", "(", "ـ", "+",
      "\u0652", "\u064C", "\u064D", "\u064B", "\u064F", "\u0650", "\u064E", "\u0651", "]", "[", "}", "{", "|",
      "ؤ", "ئ", "ي", "إ", "أ", "آ", "ة", "»", "«", ":", "؛",
      "ك", "\u0653", "ژ", "\u0670", ZWNJ, "\u0654", "ء", ">", "<", "؟",
    ],
  },
  // The legacy Microsoft layout ("Persian" in Windows).
  legacy: {
    unshifted: [
      ZWJ, "۱", "۲", "۳", "۴", "۵", "۶", "۷", "۸", "۹", "۰", "-", "=",
      "ض", "ص", "ث", "ق", "ف", "غ", "ع", "ه", "خ", "ح", "ج", "چ", "پ",
      "ش", "س", "ی", "ب", "ل", "ا", "ت", "ن", "م", "ک", "گ",
      "ظ", "ط", "ز", "ر", "ذ", "د", "ئ", "و", ".", "/",
    ],
    shifted: [
      "÷", "!", "@", "#", "$", "%", "^", "&", "*", ")", "(", "_", "+",
      "\u064B", "\u064C", "\u064D", "ريال", "،", "؛", ",", "]", "[", "\\", "}", "{", "|",
      "\u064E", "\u064F", "\u0650", "\u0651", "ۀ", "آ", "ـ", "«", "»", ":", "\"",
      "ة", "ي", "ژ", "ؤ", "إ", "أ", "ء", ">", "<", "؟",
    ],
  },
};

const ARABIC_INDIC_DIGITS = ["٠", "١", "٢", "٣", "٤", "٥", "٦", "٧", "٨", "٩"];

/** All [usKey, output] pairs of a layout, unshifted keys first. */
function keys(layout) {
  const pairs = [];
  for (let i = 0; i < US_UNSHIFTED.length; i++) pairs.push([US_UNSHIFTED[i], layout.unshifted[i]]);
  for (let i = 0; i < US_SHIFTED.length; i++) pairs.push([US_SHIFTED[i], layout.shifted[i]]);
  return pairs;
}

const isAsciiDigit = (c) => c >= "0" && c <= "9";
const isPersianDigit = (c) => c >= "۰" && c <= "۹";
const isAscii = (s) => /^[\x00-\x7F]*$/.test(s);

/** True for characters of the Arabic script blocks and for ZWNJ. */
export function isPersian(c) {
  const code = c.charCodeAt(0);
  return (code >= 0x0600 && code <= 0x06ff) ||
    (code >= 0x0750 && code <= 0x077f) ||
    (code >= 0xfb50 && code <= 0xfdff) ||
    (code >= 0xfe70 && code <= 0xfefc) ||
    code === 0x200c;
}

/** Counts Latin letters and Persian characters. */
export function count(text) {
  let latin = 0;
  let persian = 0;
  for (let i = 0; i < text.length; i++) {
    const c = text[i];
    if ((c >= "a" && c <= "z") || (c >= "A" && c <= "Z")) latin++;
    else if (isPersian(c)) persian++;
  }
  return { latin, persian };
}

/** "persianToLatin" when Persian characters outnumber Latin letters, otherwise "latinToPersian". */
export function detectDirection(text, whenUndecided = "latinToPersian") {
  const { latin, persian } = count(text);
  if (persian > latin) return "persianToLatin";
  if (latin > persian) return "latinToPersian";
  return whenUndecided;
}

/**
 * Builds a converter.
 * @param {{layout?: "standard" | "legacy", convertDigits?: boolean}} options
 */
export function createConverter({ layout = "standard", convertDigits = true } = {}) {
  const primary = LAYOUTS[layout];
  if (!primary) throw new Error(`Unknown layout ${layout}`);
  const other = layout === "standard" ? LAYOUTS.legacy : LAYOUTS.standard;

  // Latin character == US key, so the forward map is the layout itself.
  const latinToPersianMap = new Map();
  for (const [key, output] of keys(primary)) {
    if (!convertDigits && isAsciiDigit(key)) continue;
    if (output.length > 0) latinToPersianMap.set(key, output);
  }

  // Persian output -> US key. The selected layout wins; the other fills in characters it never types.
  const persianToLatinMap = new Map();
  const addReverse = (persianLayout, isFallback) => {
    for (const [key, output] of keys(persianLayout)) {
      if (output.length === 0) continue;
      if (isFallback && isAscii(output)) continue;
      if (!convertDigits && output.length === 1 && isPersianDigit(output)) continue;
      if (!persianToLatinMap.has(output)) persianToLatinMap.set(output, key);
    }
  };
  addReverse(primary, false);
  addReverse(other, true);
  if (convertDigits) {
    ARABIC_INDIC_DIGITS.forEach((d, i) => {
      if (!persianToLatinMap.has(d)) persianToLatinMap.set(d, String(i));
    });
  }
  const longest = Math.max(...[...persianToLatinMap.keys()].map((k) => k.length));

  /** "sghl" → "سلام" */
  function latinToPersian(text) {
    let out = "";
    for (let i = 0; i < text.length; i++) {
      const c = text[i];
      out += latinToPersianMap.get(c) ?? c;
    }
    return out;
  }

  /** "اثممخ" → "hello" */
  function persianToLatin(text) {
    let out = "";
    let i = 0;
    while (i < text.length) {
      let matched = false;
      // Longest match first: the legacy layout has a key that types the whole word "ريال".
      for (let len = Math.min(longest, text.length - i); len >= 1; len--) {
        const mapped = persianToLatinMap.get(text.substr(i, len));
        if (mapped !== undefined) {
          out += mapped;
          i += len;
          matched = true;
          break;
        }
      }
      if (!matched) {
        out += text[i];
        i++;
      }
    }
    return out;
  }

  /**
   * @param {string} text
   * @param {"auto" | "latinToPersian" | "persianToLatin"} direction
   * @returns {{text: string, direction: "latinToPersian" | "persianToLatin"}}
   */
  function convert(text, direction = "auto") {
    const d = direction === "auto" ? detectDirection(text) : direction;
    return { text: d === "latinToPersian" ? latinToPersian(text) : persianToLatin(text), direction: d };
  }

  return { latinToPersian, persianToLatin, convert };
}
