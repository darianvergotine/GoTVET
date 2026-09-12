<div align="center">

<img src="src/GoTVET.Web/wwwroot/favicon.png" alt="GoTVET" width="96" height="96">

# GoTVET

**Free NC(V) past papers for South African TVET students.**

[![.NET](https://img.shields.io/badge/.NET-10-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](#)
[![Windows](https://img.shields.io/badge/Windows-WPF-0f6b4c?style=for-the-badge&logo=windows&logoColor=white)](#)
[![Version](https://img.shields.io/badge/version-1.2.0-d4a017?style=for-the-badge)](#)
[![Licence](https://img.shields.io/badge/free-for%20students-0a4d38?style=for-the-badge)](#)

<br>

```text
 ╔══════════════════════════════════════════════╗
 ║   G o T V E T                                ║
 ║   National Certificate (Vocational) library  ║
 ║   Levels 2 · 3 · 4   ·   2011 – 2026         ║
 ╚══════════════════════════════════════════════╝
```

</div>

---

GoTVET is a Windows desktop app with a bundled website. It helps students **find and download** National Certificate (Vocational) past papers — no login, no paywall.

The catalogue follows the DHET NC(V) subject matrix: fundamentals for every student, plus the core and optional vocational subjects for each programme. Each sitting includes a **question paper** and a **memorandum**.

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

## Highlights

|  |  |
|:--|:--|
| **Desktop + website** | Launch GoTVET and the local library opens together at `http://127.0.0.1:5088`. |
| **Search & filter** | Browse by programme, subject, level, year, or session. |
| **Downloads** | Save papers to *Documents\GoTVET\Past Papers*. |
| **Your colour** | Pick any accent colour in the app or on the website. |
| **Installer** | One Setup.exe — no Visual Studio required on the student’s PC. |

---

## Install

Use the generated installer (no admin rights):

```text
dist/GoTVET-Setup-1.2.0.exe
```

After setup, start **GoTVET**. The desktop window and the localhost website open together. Closing the app also stops the local site.

---

## Develop

```powershell
dotnet run --project src/GoTVET.Web/GoTVET.Web.csproj
dotnet run --project src/GoTVET/GoTVET.csproj
```

Build a new Setup file:

```powershell
powershell -File installer/build.ps1
```

```text
src/
├── GoTVET/          Windows WPF client
└── GoTVET.Web/      Razor catalogue + download API
```

---

<div align="center">

**Created by Darian Vergotine**

*GoTVET hosts the papers. This project is not connected to DHET servers.*

</div>
