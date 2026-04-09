/* GoldBranchAI - App Shell (global JS)
   - Safe DOM init helpers
   - Fetch wrapper + toast notifications
   - Service worker messaging helper
   - ALL UI/UX effects centralized here
*/

(function () {
  "use strict";

  const GB = (window.GB = window.GB || {});

  // ─── UTILITY HELPERS ───────────────────────────────────────
  GB.domReady = function domReady(fn) {
    if (document.readyState === "loading") document.addEventListener("DOMContentLoaded", fn);
    else fn();
  };

  GB.qs = (sel, root) => (root || document).querySelector(sel);
  GB.qsa = (sel, root) => Array.from((root || document).querySelectorAll(sel));

  GB.sleep = (ms) => new Promise((r) => setTimeout(r, ms));
  GB.copyText = async function copyText(text) {
    try {
      if (navigator.clipboard && window.isSecureContext) {
        await navigator.clipboard.writeText(text);
        return true;
      }
    } catch {}

    try {
      const ta = document.createElement("textarea");
      ta.value = text;
      ta.style.position = "fixed";
      ta.style.top = "-9999px";
      document.body.appendChild(ta);
      ta.focus();
      ta.select();
      const ok = document.execCommand("copy");
      document.body.removeChild(ta);
      return ok;
    } catch {
      return false;
    }
  };

  GB.toast = function toast({ title, text, icon = "info", timer = 2800 } = {}) {
    if (typeof Swal === "undefined") return;
    Swal.fire({
      toast: true,
      position: "top-end",
      icon,
      title: title || "",
      text: text || "",
      showConfirmButton: false,
      timer,
      timerProgressBar: true,
      background: "#0d1117",
      color: "#f0f6fc",
    });
  };

  GB.fetchJson = async function fetchJson(url, options = {}) {
    const finalOptions = {
      credentials: "same-origin",
      headers: {
        "Content-Type": "application/json",
        ...(options.headers || {}),
      },
      ...options,
    };

    const res = await fetch(url, finalOptions);
    const contentType = res.headers.get("content-type") || "";
    const isJson = contentType.includes("application/json");

    if (!res.ok) {
      let msg = `HTTP ${res.status}`;
      if (isJson) {
        try {
          const j = await res.json();
          msg = j?.message || j?.error || msg;
        } catch { /* ignore */ }
      }
      throw new Error(msg);
    }

    if (isJson) return await res.json();
    return await res.text();
  };

  GB.swNotify = async function swNotify({ title, body, icon, tag } = {}) {
    if (!("serviceWorker" in navigator)) return false;
    if (!("Notification" in window)) return false;
    if (Notification.permission !== "granted") return false;

    const payload = {
      type: "WELLNESS_NOTIFICATION",
      title: title || "GoldBranch AI",
      body: body || "",
      icon: icon || "https://cdn-icons-png.flaticon.com/512/9334/9334400.png",
      tag: tag || "gb-" + Date.now(),
    };

    if (navigator.serviceWorker.controller) {
      navigator.serviceWorker.controller.postMessage(payload);
      return true;
    }

    const reg = await navigator.serviceWorker.ready;
    await reg.showNotification(payload.title, { body: payload.body, icon: payload.icon, tag: payload.tag });
    return true;
  };

  // ─── THEME MANAGEMENT ─────────────────────────────────────
  GB.initTheme = function() {
    document.querySelectorAll('.theme-btn').forEach(btn => {
      btn.addEventListener('click', (e) => {
        e.preventDefault();
        const theme = btn.getAttribute('data-theme');
        document.documentElement.setAttribute('data-theme', theme);
        localStorage.setItem('gb-theme', theme);
      });
    });
    const saved = localStorage.getItem('gb-theme');
    if (saved) document.documentElement.setAttribute('data-theme', saved);
  };

  // ─── PARTICLE BACKGROUND ──────────────────────────────────
  GB.initParticles = function() {
    const canvas = document.getElementById('particleCanvas');
    if (!canvas) return;
    const ctx = canvas.getContext('2d');
    let w, h, particles = [];

    const resize = () => { w = canvas.width = window.innerWidth; h = canvas.height = window.innerHeight; };
    resize();
    window.addEventListener('resize', resize);

    let mouse = { x: null, y: null, radius: 150 };
    window.addEventListener('mousemove', (e) => { mouse.x = e.x; mouse.y = e.y; });
    window.addEventListener('mouseout', () => { mouse.x = null; mouse.y = null; });

    class Particle {
      constructor() { this.reset(); }
      reset() {
        this.x = Math.random() * w;
        this.y = Math.random() * h;
        this.size = Math.random() * 2 + 0.5;
        this.speedX = (Math.random() - 0.5) * 0.3;
        this.speedY = (Math.random() - 0.5) * 0.3;
        this.opacity = Math.random() * 0.5 + 0.1;
        this.color = Math.random() > 0.7 ? '251,191,36' : (Math.random() > 0.5 ? '34,211,238' : '167,139,250');
      }
      update() {
        if (mouse.x != null && mouse.y != null) {
          let dx = mouse.x - this.x, dy = mouse.y - this.y;
          let dist = Math.sqrt(dx * dx + dy * dy);
          if (dist < mouse.radius) {
            const force = (mouse.radius - dist) / mouse.radius;
            this.x -= (dx / dist) * force * 5;
            this.y -= (dy / dist) * force * 5;
          }
        }
        this.x += this.speedX;
        this.y += this.speedY;
        if (this.x < 0 || this.x > w || this.y < 0 || this.y > h) this.reset();
      }
      draw() {
        ctx.beginPath();
        ctx.arc(this.x, this.y, this.size, 0, Math.PI * 2);
        ctx.fillStyle = `rgba(${this.color},${this.opacity})`;
        ctx.fill();
      }
    }

    for (let i = 0; i < 60; i++) particles.push(new Particle());

    const animate = () => {
      ctx.clearRect(0, 0, w, h);
      particles.forEach(p => { p.update(); p.draw(); });
      // Mouse-particle connections
      for (let i = 0; i < particles.length; i++) {
        if (mouse.x != null && mouse.y != null) {
          const mdx = particles[i].x - mouse.x, mdy = particles[i].y - mouse.y;
          const mDist = Math.sqrt(mdx * mdx + mdy * mdy);
          if (mDist < 180) {
            ctx.beginPath();
            ctx.strokeStyle = `rgba(34,211,238,${0.2 * (1 - mDist / 180)})`;
            ctx.lineWidth = 1.2;
            ctx.moveTo(particles[i].x, particles[i].y);
            ctx.lineTo(mouse.x, mouse.y);
            ctx.stroke();
          }
        }
        // Inter-particle connections
        for (let j = i + 1; j < particles.length; j++) {
          const dx = particles[i].x - particles[j].x, dy = particles[i].y - particles[j].y;
          const dist = Math.sqrt(dx * dx + dy * dy);
          if (dist < 120) {
            ctx.beginPath();
            ctx.strokeStyle = `rgba(251,191,36,${0.06 * (1 - dist / 120)})`;
            ctx.lineWidth = 0.5;
            ctx.moveTo(particles[i].x, particles[i].y);
            ctx.lineTo(particles[j].x, particles[j].y);
            ctx.stroke();
          }
        }
      }
      requestAnimationFrame(animate);
    };
    animate();
  };

  // ─── WELLNESS GUARDIAN ─────────────────────────────────────
  GB.initWellness = function() {
    const lockOverlay = document.getElementById('wellnessLockOverlay');
    const lockCountdown = document.getElementById('wellnessLockCountdown');
    const lockBar = document.getElementById('wellnessLockBar');
    const lockDoneBtn = document.getElementById('wellnessLockDoneBtn');

    function now() { return Date.now(); }
    function getNum(key, fallback = 0) {
      const n = parseInt(localStorage.getItem(key) || '');
      return Number.isFinite(n) ? n : fallback;
    }
    function setNum(key, value) { localStorage.setItem(key, String(value)); }

    function updateLockUI() {
      if (!lockOverlay || lockOverlay.style.display === 'none') return;
      const startAt = getNum('gb_wellness_lock_active_since', now());
      const until = getNum('gb_wellness_lock_until', now());
      const total = Math.max(1, until - startAt);
      const remaining = Math.max(0, until - now());
      const done = remaining === 0;
      const m = Math.floor(remaining / 60000).toString().padStart(2, '0');
      const s = Math.floor((remaining % 60000) / 1000).toString().padStart(2, '0');
      if (lockCountdown) lockCountdown.textContent = `${m}:${s}`;
      if (lockBar) lockBar.style.width = Math.min(100, ((total - remaining) / total) * 100) + '%';
      if (lockDoneBtn) lockDoneBtn.disabled = !done;
    }

    if (lockDoneBtn) {
      lockDoneBtn.addEventListener('click', () => {
        localStorage.removeItem('gb_wellness_lock_until');
        localStorage.removeItem('gb_wellness_lock_active_since');
        if (lockOverlay) { lockOverlay.style.display = 'none'; document.body.style.overflow = ''; }
        const t = now();
        setNum('gb_wellness_last_break_at', t);
        setNum('gb_wellness_last_eye_at', t);
        setNum('gb_wellness_last_posture_at', t);
      });
    }
    setInterval(updateLockUI, 1000);
  };

  // ─── ZEN MODE ──────────────────────────────────────────────
  GB.initZenMode = function() {
    const zenBtn = document.getElementById('zenModeBtn');
    const zenOverlay = document.getElementById('zenModeOverlay');
    const zenCloseBtn = document.getElementById('zenCloseBtn');
    if (!zenBtn || !zenOverlay) return;

    let isZen = false;
    const breathTexts = ['Nefes Al', 'Tut...', 'Nefes Ver', 'Bekle...'];
    let breathIdx = 0;
    setInterval(() => {
      const el = document.getElementById('zenBreathText');
      if (el && isZen) { breathIdx = (breathIdx + 1) % breathTexts.length; el.textContent = breathTexts[breathIdx]; }
    }, 2000);

    zenBtn.addEventListener('click', () => {
      zenOverlay.style.display = 'flex';
      setTimeout(() => { zenOverlay.style.opacity = '1'; }, 10);
      isZen = true;
      document.body.style.overflow = 'hidden';
    });
    if (zenCloseBtn) {
      zenCloseBtn.addEventListener('click', () => {
        zenOverlay.style.opacity = '0';
        setTimeout(() => { zenOverlay.style.display = 'none'; }, 1000);
        isZen = false;
        document.body.style.overflow = '';
      });
    }
  };

  // ─── ADVANCED UI EFFECTS ───────────────────────────────────
  GB.initUIEffects = function() {
    // 1. Neon Scroll Progress
    const scrollBar = document.getElementById("gbScrollProgressBar");
    if (scrollBar) {
      window.addEventListener('scroll', () => {
        const winScroll = document.body.scrollTop || document.documentElement.scrollTop;
        const height = document.documentElement.scrollHeight - document.documentElement.clientHeight;
        scrollBar.style.width = (winScroll / height) * 100 + "%";
      });
    }

    // 2. Magnetic Floating Action Bar
    const fabDock = document.getElementById('gbFabDock');
    if (fabDock && window.innerWidth > 768) {
      fabDock.style.transition = 'transform 0.2s cubic-bezier(0.25, 0.46, 0.45, 0.94)';
      fabDock.addEventListener('mousemove', e => {
        const rect = fabDock.getBoundingClientRect();
        fabDock.style.transform = `translate(${(e.clientX - rect.left - rect.width / 2) * 0.4}px, ${(e.clientY - rect.top - rect.height / 2) * 0.4}px)`;
      });
      fabDock.addEventListener('mouseleave', () => { fabDock.style.transform = 'translate(0,0)'; });
    }

    // 3. Neural Card Hologram Lighting
    document.querySelectorAll('.card.bg-dark, .compact-task, .welcome-banner').forEach(card => {
      card.classList.add('neural-card');
      card.addEventListener('mousemove', e => {
        const rect = card.getBoundingClientRect();
        card.style.setProperty('--mx', (e.clientX - rect.left) + 'px');
        card.style.setProperty('--my', (e.clientY - rect.top) + 'px');
      });
    });

    // 4. Matrix Decoder Logo
    const brandLogo = document.querySelector('.navbar-brand');
    if (brandLogo) {
      const originalHtml = brandLogo.innerHTML;
      const originalText = "GoldBranch AI";
      const letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!#$%^&*";
      let glitchInterval = null;
      brandLogo.addEventListener('mouseover', () => {
        let iteration = 0;
        clearInterval(glitchInterval);
        glitchInterval = setInterval(() => {
          const iconHtml = '<i class="fa-solid fa-leaf text-warning gb-heartbeat" style="-webkit-text-fill-color: #fbbf24;"></i> ';
          const scrambled = originalText.split("").map((letter, index) => {
            if (index < iteration) return originalText[index];
            return letters[Math.floor(Math.random() * letters.length)];
          }).join("");
          brandLogo.innerHTML = iconHtml + scrambled;
          if (iteration >= originalText.length) clearInterval(glitchInterval);
          iteration += 1 / 3;
        }, 30);
      });
      brandLogo.addEventListener('mouseleave', () => {
        clearInterval(glitchInterval);
        brandLogo.innerHTML = originalHtml;
      });
    }
  };

  // ─── ELEGANT TOUCHES ───────────────────────────────────────
  GB.initElegantTouches = function() {
    // 1. Dynamic Tab Title (Attention Grabber)
    const originalDocTitle = document.title;
    document.addEventListener("visibilitychange", () => {
      document.title = document.hidden ? "\u{1F680} Geri D\u00f6n \u015eef! Sistem Bekliyor..." : originalDocTitle;
    });

    // 2. Ambient Mouse Aura
    if (window.innerWidth > 992) {
      const aura = document.createElement('div');
      aura.style.cssText = 'position:fixed;top:0;left:0;width:400px;height:400px;background:radial-gradient(circle,rgba(251,191,36,0.08) 0%,transparent 60%);border-radius:50%;transform:translate(-50%,-50%);pointer-events:none;z-index:9999;transition:width 0.3s,height 0.3s,background 0.3s;mix-blend-mode:screen;';
      document.body.appendChild(aura);
      document.addEventListener('mousemove', (e) => { aura.style.left = e.clientX + 'px'; aura.style.top = e.clientY + 'px'; });
      document.addEventListener('mousedown', () => {
        aura.style.width = '250px'; aura.style.height = '250px';
        aura.style.background = 'radial-gradient(circle,rgba(34,211,238,0.15) 0%,transparent 60%)';
      });
      document.addEventListener('mouseup', () => {
        aura.style.width = '400px'; aura.style.height = '400px';
        aura.style.background = 'radial-gradient(circle,rgba(251,191,36,0.08) 0%,transparent 60%)';
      });
    }

    // 3. Smooth Body Fade-In
    document.body.style.animation = "bodyFadeIn 0.8s ease forwards";

    // 4. Parallax 3D Hologram Tilt for Cards
    document.querySelectorAll('.card.bg-dark, .compact-task, .welcome-banner').forEach(el => {
      el.addEventListener('mousemove', (e) => {
        if (window.innerWidth < 768) return;
        const rect = el.getBoundingClientRect();
        const x = e.clientX - rect.left - rect.width / 2;
        const y = e.clientY - rect.top - rect.height / 2;
        el.style.transform = `perspective(1000px) rotateX(${-(y / rect.height) * 10}deg) rotateY(${(x / rect.width) * 10}deg) scale3d(1.02,1.02,1.02)`;
        el.style.transition = 'none';
        el.style.zIndex = '10';
      });
      el.addEventListener('mouseleave', () => {
        el.style.transform = '';
        el.style.transition = 'transform 0.6s cubic-bezier(0.175, 0.885, 0.32, 1.275)';
        el.style.zIndex = '1';
      });
    });

    // 5. Liquid Click Ripple for Buttons
    document.querySelectorAll('.btn').forEach(btn => {
      btn.addEventListener('mousedown', function(e) {
        const rect = this.getBoundingClientRect();
        const ripple = document.createElement('span');
        ripple.style.cssText = `position:absolute;width:2px;height:2px;background:rgba(255,255,255,0.4);left:${e.clientX - rect.left}px;top:${e.clientY - rect.top}px;border-radius:50%;transform:translate(-50%,-50%);pointer-events:none;animation:liquidRipple 0.6s linear forwards;`;
        if (window.getComputedStyle(this).position === 'static') this.style.position = 'relative';
        this.style.overflow = 'hidden';
        this.appendChild(ripple);
        setTimeout(() => ripple.remove(), 600);
      });
    });

    // 6. Cinematic Scroll Reveal
    const revealObserver = new IntersectionObserver((entries) => {
      entries.forEach(entry => {
        if (entry.isIntersecting) {
          entry.target.style.opacity = '1';
          entry.target.style.transform = 'translateY(0) scale(1)';
          revealObserver.unobserve(entry.target);
        }
      });
    }, { threshold: 0.1 });

    setTimeout(() => {
      document.querySelectorAll('.card.bg-dark, .gb-section, .kanban-lane').forEach(el => {
        if (!el.classList.contains('no-reveal')) {
          el.style.opacity = '0';
          el.style.transform = 'translateY(40px) scale(0.98)';
          el.style.transition = 'opacity 0.8s ease, transform 0.8s cubic-bezier(0.165, 0.84, 0.44, 1)';
          revealObserver.observe(el);
        }
      });
    }, 100);

    // 7. Custom Futuristic Cursor Logic
    const cursorDot = document.getElementById("gbCursorDot");
    const cursorOutline = document.getElementById("gbCursorOutline");
    if (cursorDot && cursorOutline && window.innerWidth > 992) {
      window.addEventListener("mousemove", (e) => {
        const posX = e.clientX;
        const posY = e.clientY;

        cursorDot.animate({ left: `${posX}px`, top: `${posY}px` }, { duration: 0, fill: "forwards" });
        
        cursorOutline.animate({
          left: `${posX}px`,
          top: `${posY}px`
        }, { duration: 150, fill: "forwards" }); // Azaltılmış lag
      });

      // Hide cursor when leaving the window
      document.addEventListener("mouseleave", () => {
        cursorDot.style.opacity = '0';
        cursorOutline.style.opacity = '0';
      });
      document.addEventListener("mouseenter", () => {
        cursorDot.style.opacity = '1';
        cursorOutline.style.opacity = '1';
      });

      // Interactive hover scale
      document.querySelectorAll('a, button, input, textarea, select, .cursor-pointer').forEach(el => {
        el.addEventListener("mouseenter", () => {
          cursorOutline.style.transform = "translate(-50%, -50%) scale(1.5)";
          cursorOutline.style.backgroundColor = "rgba(251,191,36,0.1)";
          cursorOutline.style.borderColor = "rgba(251,191,36,0.8)";
        });
        el.addEventListener("mouseleave", () => {
          cursorOutline.style.transform = "translate(-50%, -50%) scale(1)";
          cursorOutline.style.backgroundColor = "transparent";
          cursorOutline.style.borderColor = "rgba(34,211,238,0.5)";
        });
      });
    }

    // 8. Animated Count-Up Numbers
    const counterObserver = new IntersectionObserver((entries, observer) => {
      entries.forEach(entry => {
        if (entry.isIntersecting) {
          const el = entry.target;
          const target = +el.getAttribute('data-target') || parseInt(el.innerText) || 0;
          if(target > 0) {
              const duration = 2000;
              const stepTime = Math.abs(Math.floor(duration / target));
              let current = 0;
              const timer = setInterval(() => {
                current += Math.ceil(target / 50); // fast count
                if (current >= target) {
                  el.innerText = target;
                  clearInterval(timer);
                } else {
                  el.innerText = current;
                }
              }, stepTime);
          }
          observer.unobserve(el);
        }
      });
    }, { threshold: 0.5 });
    document.querySelectorAll('.gb-counter').forEach(el => counterObserver.observe(el));
  };

  // ─── DYNAMIC CSS INJECTION ─────────────────────────────────
  GB.injectDynamicCSS = function() {
    if (document.querySelector('#bodyFadeStyle')) return;
    const at = String.fromCharCode(64); // '@' — safe from Razor
    const style = document.createElement('style');
    style.id = 'bodyFadeStyle';
    style.innerHTML =
      at + "keyframes bodyFadeIn { from { opacity: 0; transform: scale(0.99); } to { opacity: 1; transform: scale(1); } }\n" +
      at + "keyframes liquidRipple { 0% { transform: scale(0); opacity: 1; } 100% { transform: scale(200); opacity: 0; } }\n" +
      at + "keyframes gbPulse { 0% { transform: scale(1); filter: drop-shadow(0 0 2px #fbbf24); } 50% { transform: scale(1.15); filter: drop-shadow(0 0 10px #fbbf24); } 100% { transform: scale(1); filter: drop-shadow(0 0 2px #fbbf24); } }\n" +
      at + "keyframes liquidFlow { from { background-position: 0 0; } to { background-position: 200% 0; } }\n" +
      at + "keyframes neonBorderGlow { 0%,100% { border-color: rgba(251,191,36,0.3); box-shadow: 0 0 5px rgba(251,191,36,0.1); } 50% { border-color: rgba(34,211,238,0.5); box-shadow: 0 0 15px rgba(34,211,238,0.2); } }\n" +
      at + "keyframes shineSweep { 0% { left: -100%; top: -100%; } 100% { left: 200%; top: 200%; } }\n" +
      ".gb-heartbeat { display: inline-block; animation: gbPulse 2.5s infinite ease-in-out; }\n" +
      "::selection { background: #fbbf24 !important; color: #000 !important; text-shadow: none !important; }\n" +
      ".navbar-nav .nav-link { transition: all 0.3s ease; }\n" +
      ".navbar-nav .nav-link:hover { text-shadow: 0 0 8px currentColor; transform: translateY(-2px); }\n" +
      ".neural-card { position: relative; overflow: hidden; }\n" +
      ".neural-card::before { content: ''; position: absolute; top:0; left:0; width:100%; height:100%; background: radial-gradient(400px circle at var(--mx) var(--my), rgba(34,211,238,0.15), transparent 40%); z-index: 1; opacity: 0; transition: opacity 0.3s; pointer-events: none; mix-blend-mode: screen; border-radius: inherit; }\n" +
      ".neural-card:hover::before { opacity: 1; }\n" +
      ".gb-liquid-progress { background: linear-gradient(90deg, #fbbf24, #22d3ee, #a78bfa, #ec4899, #fbbf24) !important; background-size: 200% 100% !important; animation: liquidFlow 3s linear infinite !important; position: relative; overflow: hidden; border-radius: 6px; }\n" +
      ".gb-neon-border { animation: neonBorderGlow 4s ease-in-out infinite; }\n" +
      ".gb-enter { animation: bodyFadeIn 0.6s ease-out forwards; }\n" +
      "html, body { min-height: 100vh; }\n" +
      ".gb-btn-shine { position: relative; overflow: hidden; }\n" +
      ".gb-btn-shine::after { content: ''; position: absolute; top: -50%; left: -50%; width: 20%; height: 200%; background: rgba(255,255,255,0.4); transform: rotate(45deg); filter: blur(5px); opacity: 0; transition: 0s; }\n" +
      ".gb-btn-shine:hover::after { animation: shineSweep 0.8s ease forwards; opacity: 1; }\n" +
      ".form-control:focus { box-shadow: 0 0 0 3px rgba(34,211,238,0.25), 0 0 15px rgba(251,191,36,0.15) !important; border-color: rgba(34,211,238,0.6) !important; }\n" +
      ".card { transition: box-shadow 0.3s ease, border-color 0.3s ease; }\n" +
      ".card:hover { border-color: rgba(255,255,255,0.1) !important; box-shadow: 0 8px 30px rgba(0,0,0,0.4); }";
    document.head.appendChild(style);
  };

  // ─── MAIN INITIALIZATION ───────────────────────────────────
  GB.domReady(() => {
    GB.injectDynamicCSS();
    GB.initTheme();
    GB.initParticles();
    GB.initWellness();
    GB.initZenMode();
    GB.initUIEffects();
    GB.initElegantTouches();

    // Page loader: hide after interactive
    try {
      const loader = GB.qs("#pageLoader");
      if (loader) setTimeout(() => loader.classList.remove("active"), 400);
    } catch {}

    // Link loader hook
    try {
      GB.qsa('a[href]:not([target="_blank"]):not([href^="#"]):not([href^="javascript"])').forEach((link) => {
        link.addEventListener("click", () => {
          const loader = GB.qs("#pageLoader");
          if (loader) loader.classList.add("active");
        });
      });
    } catch {}

    // Soft page enter
    try {
      const main = document.querySelector("main[role='main']");
      if (main) main.classList.add("gb-enter");
    } catch {}

    // Dynamic Breadcrumb
    try {
      const path = window.location.pathname.split('/').filter(x => x);
      const bEl = GB.qs("#gbBreadcrumb");
      if (bEl) {
        let loc = path.length > 0 ? path.join(' / ').toUpperCase() : 'SYSTEM / CORE';
        loc = loc.replace('TASK / ', '');
        bEl.innerHTML = `<span class="text-warning opacity-50 me-2"><i class="fa-solid fa-terminal"></i></span> ${loc}`;
      }
    } catch {}

    // Service Worker Registration
    if ('serviceWorker' in navigator) {
      navigator.serviceWorker.register('/sw.js').catch(() => {});
    }

    // Notification Permission (first visit)
    if ('Notification' in window && Notification.permission === 'default') {
      Notification.requestPermission();
    }

    // Global JS Error Handler
    window.onerror = function(msg, source, line, col) {
      console.error('[GoldBranch Hata Yakalayici]', { msg, source, line, col });
      return false;
    };
  });
})();
