import { createConverter } from "./converter.js";
import { strings } from "./i18n.js";

const $ = (id) => document.getElementById(id);
const input = $("input");
const output = $("output");
const detected = $("detected");
const layout = $("layout");
const digits = $("digits");
const copyButton = $("copy");

// Remembered per browser; storage may be unavailable (private mode), so every access is guarded.
const store = {
  get(key) {
    try { return localStorage.getItem(`homikey.${key}`); } catch { return null; }
  },
  set(key, value) {
    try { localStorage.setItem(`homikey.${key}`, value); } catch { /* ignore */ }
  },
};

let language = store.get("lang") ?? ((navigator.languages ?? [navigator.language]).some((l) => /^fa\b/i.test(l)) ? "fa" : "en");
let copiedTimer = 0;

function t(key) {
  return strings[language][key] ?? strings.en[key] ?? key;
}

function applyLanguage() {
  const root = document.documentElement;
  root.lang = language;
  root.dir = language === "fa" ? "rtl" : "ltr";
  document.title = t("pageTitle");
  for (const el of document.querySelectorAll("[data-i18n]")) el.textContent = t(el.dataset.i18n);
  for (const el of document.querySelectorAll("[data-i18n-placeholder]")) el.placeholder = t(el.dataset.i18nPlaceholder);
  $("lang").lang = language === "fa" ? "en" : "fa";
  update();
}

function selectedDirection() {
  return document.querySelector("input[name=direction]:checked").value;
}

function update() {
  const converter = createConverter({ layout: layout.value, convertDigits: digits.checked });
  const text = input.value;
  if (text.length === 0) {
    output.textContent = t("empty");
    output.classList.add("placeholder");
    detected.textContent = "";
    return;
  }
  const result = converter.convert(text, selectedDirection());
  output.textContent = result.text;
  output.classList.remove("placeholder");
  detected.textContent = selectedDirection() === "auto"
    ? t(result.direction === "latinToPersian" ? "detectedToPersian" : "detectedToEnglish")
    : "";
}

async function copyResult() {
  if (input.value.length === 0) return;
  try {
    await navigator.clipboard.writeText(output.textContent);
  } catch {
    const range = document.createRange();
    range.selectNodeContents(output);
    const selection = getSelection();
    selection.removeAllRanges();
    selection.addRange(range);
    document.execCommand("copy");
    selection.removeAllRanges();
  }
  copyButton.textContent = t("copied");
  clearTimeout(copiedTimer);
  copiedTimer = setTimeout(() => { copyButton.textContent = t("copy"); }, 1500);
}

input.addEventListener("input", update);
layout.addEventListener("change", () => { store.set("layout", layout.value); update(); });
digits.addEventListener("change", update);
$("direction").addEventListener("change", update);
copyButton.addEventListener("click", copyResult);
$("swap").addEventListener("click", () => {
  if (input.value.length === 0) return;
  input.value = output.textContent;
  document.querySelector("input[name=direction][value=auto]").checked = true;
  update();
  input.focus();
});
$("clear").addEventListener("click", () => {
  input.value = "";
  update();
  input.focus();
});
$("lang").addEventListener("click", () => {
  language = language === "fa" ? "en" : "fa";
  store.set("lang", language);
  applyLanguage();
});

const savedLayout = store.get("layout");
if (savedLayout === "standard" || savedLayout === "legacy") layout.value = savedLayout;
applyLanguage();
