const THEME_KEY = "gotvet-theme-color";
const DEFAULT_THEME = "#0f6b4c";
const THEME_PRESETS = ["#0F6B4C", "#123A5F", "#C47B17", "#1F4E79", "#8B2942", "#333333"];

function hexToRgb(hex) {
  const value = hex.replace("#", "");
  const normalized = value.length === 3
    ? value.split("").map((part) => part + part).join("")
    : value;
  const number = parseInt(normalized, 16);
  return { r: (number >> 16) & 255, g: (number >> 8) & 255, b: number & 255 };
}

function rgbToHex({ r, g, b }) {
  return "#" + [r, g, b].map((channel) => channel.toString(16).padStart(2, "0")).join("");
}

function rgbToHsl({ r, g, b }) {
  r /= 255; g /= 255; b /= 255;
  const max = Math.max(r, g, b);
  const min = Math.min(r, g, b);
  const l = (max + min) / 2;
  if (max === min) {
    return { h: 0, s: 0, l };
  }
  const d = max - min;
  const s = l > 0.5 ? d / (2 - max - min) : d / (max + min);
  let h = 0;
  if (max === r) h = ((g - b) / d + (g < b ? 6 : 0));
  else if (max === g) h = (b - r) / d + 2;
  else h = (r - g) / d + 4;
  return { h: h * 60, s, l };
}

function hslToRgb(h, s, l) {
  const hueToRgb = (p, q, t) => {
    if (t < 0) t += 1;
    if (t > 1) t -= 1;
    if (t < 1 / 6) return p + (q - p) * 6 * t;
    if (t < 1 / 2) return q;
    if (t < 2 / 3) return p + (q - p) * (2 / 3 - t) * 6;
    return p;
  };
  if (s <= 0) {
    const grey = Math.round(l * 255);
    return { r: grey, g: grey, b: grey };
  }
  h = ((h % 360) + 360) % 360 / 360;
  const q = l < 0.5 ? l * (1 + s) : l + s - l * s;
  const p = 2 * l - q;
  return {
    r: Math.round(hueToRgb(p, q, h + 1 / 3) * 255),
    g: Math.round(hueToRgb(p, q, h) * 255),
    b: Math.round(hueToRgb(p, q, h - 1 / 3) * 255)
  };
}

function relativeLuminance({ r, g, b }) {
  const linear = (channel) => {
    const value = channel / 255;
    return value <= 0.04045 ? value / 12.92 : Math.pow((value + 0.055) / 1.055, 2.4);
  };
  return 0.2126 * linear(r) + 0.7152 * linear(g) + 0.0722 * linear(b);
}

function clamp(value, min, max) {
  return Math.min(max, Math.max(min, value));
}

function applyTheme(hex) {
  const rgb = hexToRgb(hex);
  const hsl = rgbToHsl(rgb);
  const dark = hslToRgb(hsl.h, hsl.s, clamp(hsl.l * 0.72, 0, 0.45));
  const accent = hslToRgb(hsl.h, clamp(hsl.s * 0.85 + 0.15, 0.45, 1), clamp(hsl.l + 0.22, 0.45, 0.72));
  const onPrimary = relativeLuminance(rgb) > 0.45 ? "#12263a" : "#ffffff";
  const onAccent = relativeLuminance(accent) > 0.45 ? "#12263a" : "#ffffff";
  const muted = onPrimary === "#ffffff" ? "rgba(255,255,255,0.85)" : "rgba(18,38,58,0.75)";
  const root = document.documentElement;
  root.style.setProperty("--green", rgbToHex(rgb));
  root.style.setProperty("--green-dark", rgbToHex(dark));
  root.style.setProperty("--gold", rgbToHex(accent));
  root.style.setProperty("--header-fg", onPrimary);
  root.style.setProperty("--header-muted", muted);
  root.style.setProperty("--on-gold", onAccent);
  const picker = document.getElementById("theme-color");
  if (picker) {
    picker.value = rgbToHex(rgb);
  }
}

function saveTheme(hex) {
  localStorage.setItem(THEME_KEY, hex);
  applyTheme(hex);
}

function currentTheme() {
  return localStorage.getItem(THEME_KEY) || DEFAULT_THEME;
}

applyTheme(currentTheme());

document.addEventListener("DOMContentLoaded", () => {
  const picker = document.getElementById("theme-color");
  const reset = document.getElementById("theme-reset");
  const presets = document.getElementById("theme-presets");
  picker?.addEventListener("input", (event) => saveTheme(event.target.value));
  reset?.addEventListener("click", () => {
    localStorage.removeItem(THEME_KEY);
    applyTheme(DEFAULT_THEME);
  });
  THEME_PRESETS.forEach((hex) => {
    const button = document.createElement("button");
    button.type = "button";
    button.className = "theme-swatch";
    button.style.background = hex;
    button.setAttribute("aria-label", `Use colour ${hex}`);
    button.addEventListener("click", () => saveTheme(hex));
    presets?.appendChild(button);
  });
});
