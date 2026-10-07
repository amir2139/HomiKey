// Tests the web converter against the cases shared with HomiKey.Core.
// Run from the repository root: node web/test.mjs
import { readFileSync } from "node:fs";
import { fileURLToPath } from "node:url";
import { dirname, join } from "node:path";
import { createConverter, detectDirection, US_UNSHIFTED, US_SHIFTED, LAYOUTS } from "./converter.js";
import { strings } from "./i18n.js";

const here = dirname(fileURLToPath(import.meta.url));
const { cases } = JSON.parse(readFileSync(join(here, "..", "tests", "vectors.json"), "utf8"));

let failures = 0;
const check = (ok, message) => {
  if (!ok) {
    failures++;
    console.error("FAIL:", message);
  }
};
const show = (s) => JSON.stringify(s);

for (const c of cases) {
  const converter = createConverter({ layout: c.layout });
  const actual =
    c.direction === "auto" ? converter.convert(c.input).text
      : c.direction === "latinToPersian" ? converter.latinToPersian(c.input)
        : converter.persianToLatin(c.input);
  check(actual === c.expected, `${c.layout} ${c.direction} ${show(c.input)}: expected ${show(c.expected)}, got ${show(actual)}`);
}

for (const layout of Object.keys(LAYOUTS)) {
  const converter = createConverter({ layout });
  for (const key of US_UNSHIFTED + US_SHIFTED) {
    const back = converter.persianToLatin(converter.latinToPersian(key));
    check(back === key, `${layout}: key ${show(key)} does not round-trip (got ${show(back)})`);
  }
}

const noDigits = createConverter({ convertDigits: false });
check(noDigits.latinToPersian("sghl 123") === "سلام 123", "digits kept Latin when disabled");
check(noDigits.persianToLatin("اثممخ ۱۲۳") === "hello ۱۲۳", "digits kept Persian when disabled");
check(detectDirection("") === "latinToPersian", "empty text defaults to latinToPersian");

const enKeys = Object.keys(strings.en).sort();
const faKeys = Object.keys(strings.fa).sort();
check(JSON.stringify(enKeys) === JSON.stringify(faKeys), "web i18n: en and fa have different keys");

const total = cases.length;
if (failures > 0) {
  console.error(`${failures} failure(s).`);
  process.exit(1);
}
console.log(`web converter: ${total} shared cases and layout round-trips passed.`);
