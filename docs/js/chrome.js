function mountChrome(active) {
  const header = document.getElementById("site-header");
  const footer = document.getElementById("site-footer");
  if (header) {
    header.innerHTML = `
      <div class="container header-row">
        <a class="brand" href="index.html">
          <span class="brand-mark">G</span>
          <span>
            <strong>GoTVET</strong>
            <small>Free TVET past papers</small>
          </span>
        </a>
        <button class="nav-toggle" type="button" aria-expanded="false" aria-controls="site-nav">Menu</button>
        <nav id="site-nav" class="site-nav">
          <a href="index.html" class="${active === "home" ? "active" : ""}">Home</a>
          <a href="papers.html" class="${active === "papers" ? "active" : ""}">Papers</a>
          <a href="app.html" class="${active === "app" ? "active" : ""}">Desktop app</a>
          <a href="about.html" class="${active === "about" ? "active" : ""}">About</a>
        </nav>
        <div class="theme-picker" title="Change the site colour">
          <span class="theme-picker-label">Colour</span>
          <div id="theme-presets" class="theme-swatches"></div>
          <input id="theme-color" type="color" value="#0f6b4c" aria-label="Pick any colour" />
          <button id="theme-reset" class="theme-reset" type="button">Reset</button>
        </div>
      </div>`;
  }
  if (footer) {
    footer.innerHTML = `
      <div class="container footer-grid">
        <div>
          <strong>GoTVET</strong>
          <p>A free library of TVET past papers for South African students. Open this site on any phone, tablet or computer.</p>
        </div>
        <div>
          <strong>Library</strong>
          <a href="papers.html">Browse papers</a>
          <a href="app.html">Windows app</a>
          <a href="about.html">How it works</a>
        </div>
      </div>
      <p class="container fine-print">GoTVET hosts its own paper library. It is not connected to DHET servers. Created by Darian Vergotine.</p>`;
  }
  const toggle = document.querySelector(".nav-toggle");
  const nav = document.getElementById("site-nav");
  toggle?.addEventListener("click", () => {
    const open = nav.classList.toggle("open");
    toggle.setAttribute("aria-expanded", String(open));
  });
}
