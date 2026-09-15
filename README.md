<div align="center">

<img src="src/GoTVET.Web/wwwroot/favicon.png" alt="GoTVET" width="96" height="96">

# GoTVET

**Free NC(V) past papers for South African TVET students.**

**Live website:** [https://darianvergotine.github.io/GoTVET/](https://darianvergotine.github.io/GoTVET/)

[![.NET](https://img.shields.io/badge/.NET-10-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](#)
[![Windows](https://img.shields.io/badge/Windows-WPF-0f6b4c?style=for-the-badge&logo=windows&logoColor=white)](#)
[![Website](https://img.shields.io/badge/website-live-d4a017?style=for-the-badge)](https://darianvergotine.github.io/GoTVET/)
[![Version](https://img.shields.io/badge/version-1.3.0-0a4d38?style=for-the-badge)](#)

</div>

---

GoTVET is a free library for **National Certificate (Vocational)** past papers. Open the website on a phone, tablet or computer — no login and no payment.

The public site is always on at **[darianvergotine.github.io/GoTVET](https://darianvergotine.github.io/GoTVET/)**. The Windows app connects to that same live library when it starts.

<table>
  <tr>
    <td align="center"><b>21</b><br>programmes</td>
    <td align="center"><b>131</b><br>subjects</td>
    <td align="center"><b>L2 · L3 · L4</b><br>levels</td>
    <td align="center"><b>Feb · Nov</b><br>sessions</td>
    <td align="center"><b>2011 – 2026</b><br>exam years</td>
  </tr>
</table>

---

## Use it

| Device | What to open |
|:--|:--|
| Phone, tablet, school PC, any browser | [https://darianvergotine.github.io/GoTVET/](https://darianvergotine.github.io/GoTVET/) |
| Windows desktop app | `dist/GoTVET-Setup-1.3.0.exe` — launches the app and opens the live website |

---

## Develop

```powershell
dotnet run --project src/GoTVET.Web/GoTVET.Web.csproj
dotnet run --project src/GoTVET/GoTVET.csproj
```

Refresh the public catalogue JSON:

```powershell
dotnet run --project src/GoTVET.Web/GoTVET.Web.csproj --no-launch-profile -- --export-offerings docs/data/offerings.json
```

```text
src/
├── GoTVET/          Windows WPF client
└── GoTVET.Web/      Razor catalogue + download API
docs/                Public website (GitHub Pages)
```

---

<div align="center">

**Created by Darian Vergotine**

*GoTVET hosts the papers. This project is not connected to DHET servers.*

</div>
