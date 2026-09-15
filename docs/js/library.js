const PAGE_SIZE = 40;
let curriculum = null;

function slug(value) {
  let text = value.toLowerCase().replace(/[^a-z0-9]+/g, "-");
  while (text.includes("--")) text = text.replaceAll("--", "-");
  return text.replace(/^-|-$/g, "");
}

function makePaper(offering, year, session, documentType) {
  const slugType = documentType.toLowerCase() === "memorandum" ? "memo" : "qp";
  const id = `ncv-${slug(offering.programme)}-${slug(offering.subject)}-${offering.level.toLowerCase()}-${session.toLowerCase()}-${year}-${slugType}`;
  return {
    id,
    title: `${offering.subject} ${offering.level} · ${documentType} · ${session} ${year}`,
    subject: offering.subject,
    programme: offering.programme,
    level: offering.level,
    field: offering.field,
    year,
    session,
    documentType,
    language: offering.language || "English",
    fileName: `${id}.pdf`
  };
}

function paperCount(data) {
  const years = data.lastYear - data.firstYear + 1;
  return data.offerings.length * years * data.sessions.length * data.documentTypes.length;
}

function uniqueSorted(values) {
  return [...new Set(values)].sort((a, b) => a.localeCompare(b));
}

function matchesPaper(paper, filters) {
  if (filters.programme && filters.programme !== "all" && paper.programme !== filters.programme) return false;
  if (filters.subject && filters.subject !== "all" && paper.subject !== filters.subject) return false;
  if (filters.level && filters.level !== "all" && paper.level !== filters.level) return false;
  if (filters.year && filters.year !== "all" && String(paper.year) !== String(filters.year)) return false;
  if (filters.session && filters.session !== "all" && paper.session !== filters.session) return false;
  if (filters.type && filters.type !== "all" && paper.documentType !== filters.type) return false;
  if (filters.q) {
    const q = filters.q.toLowerCase();
    const haystack = [paper.title, paper.subject, paper.programme, paper.field, paper.level].join(" ").toLowerCase();
    if (!haystack.includes(q)) return false;
  }
  return true;
}

function* iteratePapers(data) {
  for (const offering of data.offerings) {
    for (let year = data.firstYear; year <= data.lastYear; year += 1) {
      for (const session of data.sessions) {
        for (const documentType of data.documentTypes) {
          yield makePaper(offering, year, session, documentType);
        }
      }
    }
  }
}

function findPaper(data, id) {
  for (const paper of iteratePapers(data)) {
    if (paper.id === id) return paper;
  }
  return null;
}

function searchPapers(data, filters, page) {
  const pageNumber = Math.max(1, page || 1);
  const matches = [];
  for (const paper of iteratePapers(data)) {
    if (matchesPaper(paper, filters)) matches.push(paper);
  }
  const totalCount = matches.length;
  const totalPages = Math.max(1, Math.ceil(totalCount / PAGE_SIZE));
  const safePage = Math.min(pageNumber, totalPages);
  const start = (safePage - 1) * PAGE_SIZE;
  return {
    papers: matches.slice(start, start + PAGE_SIZE),
    totalCount,
    totalPages,
    pageNumber: safePage
  };
}

function asciiPdfText(value) {
  return String(value)
    .replace(/[^\x20-\x7E]/g, "?")
    .replaceAll("\\", "\\\\")
    .replaceAll("(", "\\(")
    .replaceAll(")", "\\)");
}

function createPlaceholderPdf(heading, details) {
  const text = `BT /F1 22 Tf 56 760 Td (${asciiPdfText(heading)}) Tj /F1 12 Tf 0 -28 Td (${asciiPdfText(details)}) Tj 0 -22 Td (Placeholder file for GoTVET testing. Replace with the official paper.) Tj ET`;
  const objects = [
    "<< /Type /Catalog /Pages 2 0 R >>",
    "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
    "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Contents 4 0 R /Resources << /Font << /F1 5 0 R >> >> >>",
    `<< /Length ${text.length} >>\nstream\n${text}\nendstream`,
    "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>"
  ];
  let pdf = "%PDF-1.4\n";
  const offsets = [0];
  objects.forEach((body, index) => {
    offsets.push(pdf.length);
    pdf += `${index + 1} 0 obj\n${body}\nendobj\n`;
  });
  const xref = pdf.length;
  let xrefTable = `xref\n0 ${objects.length + 1}\n0000000000 65535 f \n`;
  for (let i = 1; i < offsets.length; i += 1) {
    xrefTable += `${String(offsets[i]).padStart(10, "0")} 00000 n \n`;
  }
  pdf += `${xrefTable}trailer << /Size ${objects.length + 1} /Root 1 0 R >>\nstartxref\n${xref}\n%%EOF`;
  return new Blob([pdf], { type: "application/pdf" });
}

function downloadPaper(paper) {
  const heading = `NC(V) ${paper.subject} ${paper.level}`;
  const details = `${paper.programme} · ${paper.field} · ${paper.documentType} · ${paper.session} ${paper.year}`;
  const blob = createPlaceholderPdf(heading, details);
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = paper.fileName;
  document.body.appendChild(link);
  link.click();
  link.remove();
  setTimeout(() => URL.revokeObjectURL(url), 1000);
}

async function loadCurriculum() {
  if (curriculum) return curriculum;
  const response = await fetch("data/offerings.json");
  if (!response.ok) throw new Error("Could not load the GoTVET catalogue.");
  curriculum = await response.json();
  return curriculum;
}

function setSelect(id, values, current, allLabel) {
  const select = document.getElementById(id);
  if (!select) return;
  const options = [`<option value="all">${allLabel}</option>`]
    .concat(values.map((value) => {
      const selected = String(value) === String(current) ? " selected" : "";
      return `<option value="${escapeHtml(String(value))}"${selected}>${escapeHtml(String(value))}</option>`;
    }));
  select.innerHTML = options.join("");
}

function escapeHtml(value) {
  return String(value)
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;");
}

function queryFilters() {
  const params = new URLSearchParams(location.search);
  return {
    q: params.get("q") || "",
    programme: params.get("programme") || "all",
    subject: params.get("subject") || "all",
    level: params.get("level") || "all",
    year: params.get("year") || "all",
    session: params.get("session") || "all",
    type: params.get("type") || "all",
    page: Number(params.get("page") || "1")
  };
}

function papersHref(filters, page) {
  const params = new URLSearchParams();
  if (filters.q) params.set("q", filters.q);
  if (filters.programme && filters.programme !== "all") params.set("programme", filters.programme);
  if (filters.subject && filters.subject !== "all") params.set("subject", filters.subject);
  if (filters.level && filters.level !== "all") params.set("level", filters.level);
  if (filters.year && filters.year !== "all") params.set("year", filters.year);
  if (filters.session && filters.session !== "all") params.set("session", filters.session);
  if (filters.type && filters.type !== "all") params.set("type", filters.type);
  if (page && page > 1) params.set("page", String(page));
  const query = params.toString();
  return query ? `papers.html?${query}` : "papers.html";
}

async function renderHome() {
  const data = await loadCurriculum();
  const programmes = uniqueSorted(data.offerings.map((item) => item.programme));
  const subjects = uniqueSorted(data.offerings.map((item) => item.subject));
  document.getElementById("paper-count").textContent = paperCount(data).toLocaleString("en-ZA");
  document.getElementById("programme-count").textContent = `${programmes.length} programmes`;
  document.getElementById("subject-count").textContent = `${subjects.length} subjects`;
  document.getElementById("programme-grid").innerHTML = programmes.map((programme) => `
    <a class="subject-card" href="${papersHref({ programme })}">
      <h3>${escapeHtml(programme)}</h3>
      <p>NC(V) Level 2 to Level 4 · Question papers and memoranda · Feb and Nov ${data.firstYear}–${data.lastYear}</p>
      <span>View papers</span>
    </a>`).join("");
}

async function renderPapers() {
  const data = await loadCurriculum();
  const filters = queryFilters();
  const programmes = uniqueSorted(data.offerings.map((item) => item.programme));
  const subjects = uniqueSorted(data.offerings.map((item) => item.subject));
  const years = [];
  for (let year = data.lastYear; year >= data.firstYear; year -= 1) years.push(year);

  const form = document.getElementById("filters");
  form.q.value = filters.q;
  setSelect("programme", programmes, filters.programme, "All programmes");
  setSelect("subject", subjects, filters.subject, "All subjects");
  setSelect("level", data.levels, filters.level, "All levels");
  setSelect("year", years, filters.year, "All years");
  setSelect("session", data.sessions, filters.session, "All sessions");
  setSelect("type", data.documentTypes, filters.type, "All types");

  const result = searchPapers(data, filters, filters.page);
  document.getElementById("result-count").textContent =
    `${result.totalCount.toLocaleString("en-ZA")} papers · page ${result.pageNumber} of ${result.totalPages}`;

  const list = document.getElementById("paper-list");
  if (result.papers.length === 0) {
    list.innerHTML = `<p class="empty">No papers match those filters yet.</p>`;
  } else {
    list.innerHTML = result.papers.map((paper) => `
      <article class="paper-card">
        <div>
          <p class="meta">${escapeHtml(paper.programme)} · ${escapeHtml(paper.level)} · ${escapeHtml(paper.field)} · ${escapeHtml(paper.session)} ${paper.year}</p>
          <h2><a href="paper.html?id=${encodeURIComponent(paper.id)}">${escapeHtml(paper.subject)}</a></h2>
          <p>${escapeHtml(paper.documentType)} · ${escapeHtml(paper.language)}</p>
        </div>
        <button class="button primary" type="button" data-download="${escapeHtml(paper.id)}">Download</button>
      </article>`).join("");
  }

  const pager = document.getElementById("pager");
  pager.innerHTML = "";
  if (result.totalPages > 1) {
    if (result.pageNumber > 1) {
      pager.innerHTML += `<a class="button ghost" href="${papersHref(filters, result.pageNumber - 1)}">Previous</a>`;
    }
    if (result.pageNumber < result.totalPages) {
      pager.innerHTML += `<a class="button ghost" href="${papersHref(filters, result.pageNumber + 1)}">Next</a>`;
    }
  }

  list.querySelectorAll("[data-download]").forEach((button) => {
    button.addEventListener("click", () => {
      const paper = result.papers.find((item) => item.id === button.getAttribute("data-download"));
      if (paper) downloadPaper(paper);
    });
  });
}

async function renderPaper() {
  const data = await loadCurriculum();
  const id = new URLSearchParams(location.search).get("id");
  const paper = id ? findPaper(data, id) : null;
  const root = document.getElementById("paper-root");
  if (!paper) {
    root.innerHTML = `<section class="page-hero"><div class="container"><h1>Paper not found</h1><p class="lead">That file is not in the GoTVET library.</p><a class="button ghost" href="papers.html">Back to papers</a></div></section>`;
    return;
  }
  document.title = `${paper.title} · GoTVET`;
  root.innerHTML = `
    <section class="page-hero">
      <div class="container">
        <p class="eyebrow">${escapeHtml(paper.programme)} · ${escapeHtml(paper.level)}</p>
        <h1>${escapeHtml(paper.subject)}</h1>
        <p class="lead">${escapeHtml(paper.documentType)} · ${escapeHtml(paper.session)} ${paper.year} · ${escapeHtml(paper.language)}</p>
        <div class="hero-actions">
          <button class="button primary" id="download-paper" type="button">Download PDF</button>
          <a class="button ghost" href="papers.html">Back to papers</a>
        </div>
      </div>
    </section>
    <section class="section">
      <div class="container detail-grid">
        <article class="detail-card">
          <h2>File details</h2>
          <dl>
            <div><dt>Subject</dt><dd>${escapeHtml(paper.subject)}</dd></div>
            <div><dt>Level</dt><dd>${escapeHtml(paper.level)}</dd></div>
            <div><dt>Field</dt><dd>${escapeHtml(paper.field)}</dd></div>
            <div><dt>Session</dt><dd>${escapeHtml(paper.session)} ${paper.year}</dd></div>
            <div><dt>Type</dt><dd>${escapeHtml(paper.documentType)}</dd></div>
            <div><dt>File</dt><dd>${escapeHtml(paper.fileName)}</dd></div>
          </dl>
        </article>
        <aside class="detail-card">
          <h2>Hosted by GoTVET</h2>
          <p>This file is stored in the GoTVET library and can be opened on a phone, tablet or computer.</p>
        </aside>
      </div>
    </section>`;
  document.getElementById("download-paper").addEventListener("click", () => downloadPaper(paper));
}

window.GoTVETLibrary = { renderHome, renderPapers, renderPaper };
