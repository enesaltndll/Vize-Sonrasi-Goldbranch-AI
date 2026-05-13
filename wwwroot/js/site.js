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
      type: "SYSTEM_NOTIFICATION",
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

  // 🎙️ JARVIS Voice Command Controller (Advanced HUD Version)
  GB.initJarvis = function () {
    const micBtn = document.getElementById('globalVoiceCmdBtn');
    const hud = document.getElementById('jarvisHUD');
    const transcriptEl = document.getElementById('jarvisTranscript');
    const errorEl = document.getElementById('jarvisError');
    const volMeter = document.querySelector('.jarvis-volume-meter');
    
    if (!micBtn || !hud) return;
    
    const SpeechRecognition = window.SpeechRecognition || window.webkitSpeechRecognition;
    if (!SpeechRecognition) {
      micBtn.style.display = 'none';
      return;
    }

    const recognition = new SpeechRecognition();
    recognition.lang = 'tr-TR';
    recognition.continuous = false;
    recognition.interimResults = true;

    let isListening = false;
    let volumeInterval = null;

    micBtn.addEventListener('click', (e) => {
      e.preventDefault();
      if (isListening) {
        recognition.stop();
        return;
      }
      recognition.start();
    });

    recognition.onstart = () => {
      isListening = true;
      hud.style.display = 'flex';
      setTimeout(() => hud.classList.add('active'), 10);
      micBtn.classList.add('pulse-active');
      transcriptEl.innerText = "Sistemi dinliyorum...";
      
      // Simulate volume meter
      let offset = 283;
      volumeInterval = setInterval(() => {
        const rand = Math.random() * 100;
        offset = 283 - (rand * 2.83);
        volMeter.style.strokeDashoffset = offset;
      }, 100);
    };

    recognition.onresult = (event) => {
      const transcript = event.results[0][0].transcript.toLowerCase();
      transcriptEl.innerText = transcript;
      
      if (event.results[0].isFinal) {
        setTimeout(() => GB.handleVoiceCommand(transcript), 800);
      }
    };

    recognition.onend = () => {
      isListening = false;
      clearInterval(volumeInterval);
      volMeter.style.strokeDashoffset = 283;
      
      setTimeout(() => {
        hud.classList.remove('active');
        setTimeout(() => {
            hud.style.display = 'none';
            micBtn.classList.remove('pulse-active');
        }, 400);
      }, 2000);
    };

    recognition.onerror = (event) => {
      isListening = false;
      clearInterval(volumeInterval);
      errorEl.classList.add('show');
      transcriptEl.innerText = "Bağlantı hatası!";
      setTimeout(() => errorEl.classList.remove('show'), 2000);
      console.error('Speech recognition error:', event.error);
    };
  };

  GB.handleVoiceCommand = function (cmd) {
    console.log("JARVIS Command:", cmd);
    const transcriptEl = document.getElementById('jarvisTranscript');
    
    if (cmd.includes("görev") && (cmd.includes("ekle") || cmd.includes("oluştur"))) {
      transcriptEl.innerHTML = '<span class="text-success">KOMUT ONAYLANDI: GÖREV OLUŞTURULUYOR</span>';
      setTimeout(() => window.location.href = '/Task/Create', 1000);
    } else if (cmd.includes("dashboard") || cmd.includes("radar") || cmd.includes("panel")) {
      transcriptEl.innerHTML = '<span class="text-success">KOMUT ONAYLANDI: RADAR MERKEZİ</span>';
      setTimeout(() => window.location.href = '/Task/Dashboard', 1000);
    } else if (cmd.includes("list") || cmd.includes("görevler")) {
      transcriptEl.innerHTML = '<span class="text-success">KOMUT ONAYLANDI: GÖREV LİSTESİ</span>';
      setTimeout(() => window.location.href = '/Task/Index', 1000);
    } else if (cmd.includes("kanban")) {
      transcriptEl.innerHTML = '<span class="text-success">KOMUT ONAYLANDI: KANBAN BOARD</span>';
      setTimeout(() => window.location.href = '/Task/Kanban', 1000);
    } else if (cmd.includes("araştırma") || cmd.includes("kod")) {
      transcriptEl.innerHTML = '<span class="text-success">KOMUT ONAYLANDI: CODELAB ARAŞTIRMA</span>';
      setTimeout(() => window.location.href = '/Ai/Research', 1000);
    } else if (cmd.includes("çıkış") || cmd.includes("kapat")) {
       transcriptEl.innerHTML = '<span class="text-danger">OTURUM KAPATILIYOR</span>';
       setTimeout(() => window.location.href = '/Auth/Logout', 1000);
    } else {
      document.getElementById('jarvisError').classList.add('show');
      setTimeout(() => document.getElementById('jarvisError').classList.remove('show'), 2000);
    }
  };

  // 📈 Dynamic Ticker Update
  GB.updateTicker = function () {
    const ticker = document.getElementById('gbTickerContent');
    if (!ticker) return;
    
    fetch('/Task/GetLiveStats')
      .then(res => res.json())
      .then(data => {
        const timestamp = data.timestamp;
        const loadColor = data.load > 80 ? '#ef4444' : (data.load > 50 ? '#fbbf24' : '#10b981');
        
        const content = `
          <span class="gb-ticker-item"><i class="fa-solid fa-microchip me-1"></i> SYSTEM LOAD: <span style="color:${loadColor}; font-weight:bold;">%${data.load}</span></span>
          <span class="gb-ticker-item"><i class="fa-solid fa-tasks me-1"></i> TOTAL TASKS: <span class="text-warning">${data.total}</span></span>
          <span class="gb-ticker-item"><i class="fa-solid fa-check-double me-1"></i> COMPLETED: <span class="text-success">${data.completed}</span></span>
          <span class="gb-ticker-item"><i class="fa-solid fa-clock me-1"></i> SYNC: <span class="text-info">${timestamp}</span></span>
          <span class="gb-ticker-item"><i class="fa-solid fa-shield-halved me-1"></i> FIREWALL: <span class="text-success">ACTIVE</span></span>
        `;
        ticker.innerHTML = content + content; // Double for seamless loop if using CSS marquee
      })
      .catch(() => {
        ticker.innerHTML = '<span class="gb-ticker-item text-danger">SYSTEM OFFLINE</span>';
      });
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
    GB.particlesActive = true;
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
      if (!GB.particlesActive) { 
        ctx.clearRect(0, 0, w, h); 
        requestAnimationFrame(animate); 
        return; 
      }
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

  // ─── COMMAND PALETTE (Ctrl+K) ─────────────────────────────
  GB.initCommandPalette = function () {
    const palette = document.getElementById('gbCommandPalette');
    const input = document.getElementById('gbCmdInput');
    const results = document.getElementById('gbCmdResults');
    if (!palette || !input) return;

    const commands = [
      { icon: 'fa-rocket', label: 'Dashboard / Radar Merkezi', url: '/Task/Dashboard', tags: 'radar panel ana' },
      { icon: 'fa-columns', label: 'Görev Listesi (Sprint Board)', url: '/Task/Index', tags: 'görev task sprint kanban' },
      { icon: 'fa-plus', label: 'Yeni Görev Oluştur', url: '/Task/Create', tags: 'ekle yeni görev oluştur' },
      { icon: 'fa-comments', label: 'Chat / Mesajlaşma', url: '/Chat/Index', tags: 'mesaj sohbet chat' },
      { icon: 'fa-video', label: 'Canlı Toplantı (Conference)', url: '/Conference/Index', tags: 'toplantı video konferans meeting' },
      { icon: 'fa-brain', label: 'AI Ajan Merkezi', url: '/Ai/Breakdown', tags: 'ai yapay zeka ajan breakdown' },
      { icon: 'fa-code', label: 'CodeLab Araştırma', url: '/Ai/Research', tags: 'kod araştırma research codelab' },
      { icon: 'fa-user-astronaut', label: 'Profil & Rozetler', url: '/Profile/Index', tags: 'profil rozet badge avatar' },
      { icon: 'fa-gem', label: 'SaaS Planlar (Billing)', url: '/Billing/Index', tags: 'plan ödeme fatura billing saas' },
      { icon: 'fa-chart-line', label: 'İş Raporları (Z-Report)', url: '/Admin/WorkReports', tags: 'rapor z-report analiz' },
      { icon: 'fa-trophy', label: 'Performans Ligi', url: '/Admin/Leaderboard', tags: 'liderlik sıralama performans' },
      { icon: 'fa-fire-flame-curved', label: 'Tükenmişlik Haritası', url: '/Admin/BurnoutMap', tags: 'stres burnout tükenmişlik' },
      { icon: 'fa-palette', label: 'Tema Değiştir', action: 'theme', tags: 'tema renk dark theme' },
      { icon: 'fa-power-off', label: 'Oturumu Kapat', url: '/Auth/Logout', tags: 'çıkış logout kapat' },
    ];

    let isOpen = false;

    const openPalette = () => {
      isOpen = true;
      palette.style.display = 'flex';
      setTimeout(() => palette.classList.add('active'), 10);
      input.value = '';
      input.focus();
      renderResults('');
    };

    const closePalette = () => {
      isOpen = false;
      palette.classList.remove('active');
      setTimeout(() => { palette.style.display = 'none'; }, 300);
    };

    const renderResults = (query) => {
      const q = query.toLowerCase().trim();
      const filtered = q === '' ? commands : commands.filter(c =>
        c.label.toLowerCase().includes(q) || c.tags.includes(q)
      );

      results.innerHTML = filtered.map((c, i) => `
        <div class="gb-cmd-item ${i === 0 ? 'active' : ''}" data-url="${c.url || ''}" data-action="${c.action || ''}">
          <i class="fa-solid ${c.icon} gb-cmd-item-icon"></i>
          <span>${c.label}</span>
          ${i === 0 ? '<kbd class="gb-cmd-kbd">Enter</kbd>' : ''}
        </div>
      `).join('');

      results.querySelectorAll('.gb-cmd-item').forEach(item => {
        item.addEventListener('click', () => executeItem(item));
      });
    };

    const executeItem = (item) => {
      const action = item.getAttribute('data-action');
      const url = item.getAttribute('data-url');
      if (action === 'theme') {
        closePalette();
        const offcanvas = new bootstrap.Offcanvas(document.getElementById('themeOffcanvas'));
        offcanvas.show();
      } else if (url) {
        closePalette();
        window.location.href = url;
      }
    };

    // Keyboard shortcut: Ctrl+K
    document.addEventListener('keydown', (e) => {
      if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
        e.preventDefault();
        isOpen ? closePalette() : openPalette();
      }
      if (e.key === 'Escape' && isOpen) closePalette();
      if (e.key === 'Enter' && isOpen) {
        const active = results.querySelector('.gb-cmd-item.active');
        if (active) executeItem(active);
      }
      if ((e.key === 'ArrowDown' || e.key === 'ArrowUp') && isOpen) {
        e.preventDefault();
        const items = Array.from(results.querySelectorAll('.gb-cmd-item'));
        const currentIdx = items.findIndex(el => el.classList.contains('active'));
        items.forEach(el => el.classList.remove('active'));
        let nextIdx = e.key === 'ArrowDown' ? currentIdx + 1 : currentIdx - 1;
        if (nextIdx >= items.length) nextIdx = 0;
        if (nextIdx < 0) nextIdx = items.length - 1;
        items[nextIdx].classList.add('active');
        items[nextIdx].scrollIntoView({ block: 'nearest' });
      }
    });

    input.addEventListener('input', () => renderResults(input.value));
    palette.querySelector('.gb-cmd-backdrop').addEventListener('click', closePalette);
  };

  // ─── AMBIENT SOUND PLAYER ─────────────────────────────────
  GB.initAmbientPlayer = function () {
    const btn = document.getElementById('gbAmbientBtn');
    const panel = document.getElementById('gbAmbientPanel');
    if (!btn || !panel) return;

    const sounds = [
      { name: '🌧️ Yağmur', url: 'https://cdn.pixabay.com/audio/2022/03/24/audio_67d83dcba2.mp3' },
      { name: '🔥 Şömine', url: 'https://cdn.pixabay.com/audio/2024/11/26/audio_83a0f0d814.mp3' },
      { name: '🌊 Okyanus', url: 'https://cdn.pixabay.com/audio/2022/08/31/audio_419263ef33.mp3' },
    ];

    let currentAudio = null;
    let currentIdx = -1;
    let isOpen = false;

    btn.addEventListener('click', (e) => {
      e.preventDefault();
      isOpen = !isOpen;
      panel.style.display = isOpen ? 'block' : 'none';
    });

    panel.innerHTML = `
      <div class="gb-ambient-header">
        <i class="fa-solid fa-headphones me-2"></i>Ortam Sesi
        <button class="gb-ambient-close" id="gbAmbientClose">&times;</button>
      </div>
      <div class="gb-ambient-list">
        ${sounds.map((s, i) => `
          <button class="gb-ambient-item" data-idx="${i}">
            <span>${s.name}</span>
            <i class="fa-solid fa-play gb-ambient-play-icon"></i>
          </button>
        `).join('')}
      </div>
      <div class="gb-ambient-vol">
        <i class="fa-solid fa-volume-low"></i>
        <input type="range" id="gbAmbientVol" min="0" max="100" value="40" class="gb-ambient-slider">
        <i class="fa-solid fa-volume-high"></i>
      </div>
    `;

    panel.querySelector('#gbAmbientClose').addEventListener('click', () => {
      isOpen = false;
      panel.style.display = 'none';
    });

    panel.querySelectorAll('.gb-ambient-item').forEach(item => {
      item.addEventListener('click', () => {
        const idx = parseInt(item.getAttribute('data-idx'));
        if (currentIdx === idx && currentAudio && !currentAudio.paused) {
          currentAudio.pause();
          item.querySelector('.gb-ambient-play-icon').className = 'fa-solid fa-play gb-ambient-play-icon';
          btn.classList.remove('playing');
          return;
        }
        if (currentAudio) currentAudio.pause();
        panel.querySelectorAll('.gb-ambient-play-icon').forEach(ic => ic.className = 'fa-solid fa-play gb-ambient-play-icon');
        
        currentAudio = new Audio(sounds[idx].url);
        currentAudio.loop = true;
        currentAudio.volume = (panel.querySelector('#gbAmbientVol').value || 40) / 100;
        currentAudio.play();
        currentIdx = idx;
        item.querySelector('.gb-ambient-play-icon').className = 'fa-solid fa-pause gb-ambient-play-icon';
        btn.classList.add('playing');
      });
    });

    panel.querySelector('#gbAmbientVol').addEventListener('input', (e) => {
      if (currentAudio) currentAudio.volume = e.target.value / 100;
    });
  };

  // ─── POMODORO FOCUS TIMER ─────────────────────────────────
  GB.initPomodoro = function () {
    const btn = document.getElementById('gbPomodoroBtn');
    const panel = document.getElementById('gbPomodoroPanel');
    if (!btn || !panel) return;

    let timeLeft = 25 * 60; // 25 min
    let interval = null;
    let isRunning = false;
    let isOpen = false;

    const formatTime = (s) => {
      const m = Math.floor(s / 60);
      const sec = s % 60;
      return `${m < 10 ? '0' + m : m}:${sec < 10 ? '0' + sec : sec}`;
    };

    panel.innerHTML = `
      <div class="gb-pomo-header">
        <i class="fa-solid fa-hourglass-half me-2 text-warning"></i>Focus Timer
      </div>
      <div class="gb-pomo-display" id="gbPomoDisplay">25:00</div>
      <div class="gb-pomo-controls">
        <button class="gb-pomo-ctrl" id="gbPomoStart"><i class="fa-solid fa-play"></i></button>
        <button class="gb-pomo-ctrl" id="gbPomoPause"><i class="fa-solid fa-pause"></i></button>
        <button class="gb-pomo-ctrl" id="gbPomoReset"><i class="fa-solid fa-rotate-right"></i></button>
      </div>
      <div class="gb-pomo-presets">
        <button class="gb-pomo-preset active" data-min="25">25m</button>
        <button class="gb-pomo-preset" data-min="15">15m</button>
        <button class="gb-pomo-preset" data-min="5">5m</button>
        <button class="gb-pomo-preset" data-min="45">45m</button>
      </div>
    `;

    const display = panel.querySelector('#gbPomoDisplay');

    btn.addEventListener('click', (e) => {
      e.preventDefault();
      isOpen = !isOpen;
      panel.style.display = isOpen ? 'block' : 'none';
    });

    panel.querySelector('#gbPomoStart').addEventListener('click', () => {
      if (isRunning) return;
      isRunning = true;
      btn.classList.add('pomo-active');
      interval = setInterval(() => {
        timeLeft--;
        display.textContent = formatTime(timeLeft);
        if (timeLeft <= 0) {
          clearInterval(interval);
          isRunning = false;
          btn.classList.remove('pomo-active');
          GB.toast({ title: '🍅 Pomodoro Bitti!', text: 'Mola zamanı geldi, bir nefes al!', icon: 'success', timer: 8000 });
          if (window.speechSynthesis) {
            const msg = new SpeechSynthesisUtterance('Pomodoro süresi doldu! Mola zamanı.');
            msg.lang = 'tr-TR';
            window.speechSynthesis.speak(msg);
          }
          timeLeft = 25 * 60;
          display.textContent = formatTime(timeLeft);
        }
      }, 1000);
    });

    panel.querySelector('#gbPomoPause').addEventListener('click', () => {
      clearInterval(interval);
      isRunning = false;
    });

    panel.querySelector('#gbPomoReset').addEventListener('click', () => {
      clearInterval(interval);
      isRunning = false;
      btn.classList.remove('pomo-active');
      timeLeft = parseInt(panel.querySelector('.gb-pomo-preset.active')?.getAttribute('data-min') || '25') * 60;
      display.textContent = formatTime(timeLeft);
    });

    panel.querySelectorAll('.gb-pomo-preset').forEach(p => {
      p.addEventListener('click', () => {
        panel.querySelectorAll('.gb-pomo-preset').forEach(x => x.classList.remove('active'));
        p.classList.add('active');
        clearInterval(interval);
        isRunning = false;
        btn.classList.remove('pomo-active');
        timeLeft = parseInt(p.getAttribute('data-min')) * 60;
        display.textContent = formatTime(timeLeft);
      });
    });
  };

  // ─── 🌌 AURORA BOREALIS BACKGROUND ────────────────────────
  GB.initAurora = function () {
    if (window.innerWidth < 768) return;
    const existing = document.getElementById('gbAuroraCanvas');
    if (existing) return;

    const canvas = document.createElement('canvas');
    canvas.id = 'gbAuroraCanvas';
    canvas.style.cssText = 'position:fixed;top:0;left:0;width:100%;height:100%;pointer-events:none;z-index:-2;opacity:0.35;';
    document.body.prepend(canvas);

    const ctx = canvas.getContext('2d');
    let w, h, time = 0;

    const resize = () => { w = canvas.width = window.innerWidth; h = canvas.height = window.innerHeight; };
    resize();
    window.addEventListener('resize', resize);

    const auroraColors = [
      { r: 34, g: 211, b: 238 },
      { r: 167, g: 139, b: 250 },
      { r: 251, g: 191, b: 36 },
      { r: 16, g: 185, b: 129 },
    ];

    const drawAurora = () => {
      ctx.clearRect(0, 0, w, h);
      time += 0.003;

      for (let i = 0; i < auroraColors.length; i++) {
        const c = auroraColors[i];
        const yBase = h * 0.15 + i * 60;
        
        ctx.beginPath();
        ctx.moveTo(0, h);

        for (let x = 0; x <= w; x += 4) {
          const wave1 = Math.sin(x * 0.003 + time * (1 + i * 0.3) + i * 1.5) * 80;
          const wave2 = Math.sin(x * 0.007 + time * 0.8 + i * 2.5) * 40;
          const wave3 = Math.cos(x * 0.002 + time * 1.2 + i) * 50;
          const y = yBase + wave1 + wave2 + wave3;
          ctx.lineTo(x, y);
        }

        ctx.lineTo(w, h);
        ctx.closePath();

        const grad = ctx.createLinearGradient(0, yBase - 100, 0, yBase + 150);
        grad.addColorStop(0, 'rgba(' + c.r + ',' + c.g + ',' + c.b + ',0)');
        grad.addColorStop(0.4, 'rgba(' + c.r + ',' + c.g + ',' + c.b + ',0.06)');
        grad.addColorStop(0.7, 'rgba(' + c.r + ',' + c.g + ',' + c.b + ',0.03)');
        grad.addColorStop(1, 'rgba(' + c.r + ',' + c.g + ',' + c.b + ',0)');
        ctx.fillStyle = grad;
        ctx.fill();
      }

      requestAnimationFrame(drawAurora);
    };
    drawAurora();
  };

  // ─── ✨ SPARKLE MOUSE TRAIL ───────────────────────────────
  GB.initSparkleTrail = function () {
    if (window.innerWidth < 768) return;

    const sparkleContainer = document.createElement('div');
    sparkleContainer.id = 'gbSparkleContainer';
    sparkleContainer.style.cssText = 'position:fixed;top:0;left:0;width:100%;height:100%;pointer-events:none;z-index:9997;overflow:hidden;';
    document.body.appendChild(sparkleContainer);

    let lastX = 0, lastY = 0, frame = 0;
    const colors = ['#fbbf24', '#22d3ee', '#a78bfa', '#fb7185', '#34d399'];

    document.addEventListener('mousemove', function (e) {
      frame++;
      if (frame % 3 !== 0) return;

      const dx = e.clientX - lastX;
      const dy = e.clientY - lastY;
      const speed = Math.sqrt(dx * dx + dy * dy);
      lastX = e.clientX;
      lastY = e.clientY;

      if (speed < 5) return;

      const sparkle = document.createElement('div');
      const size = Math.random() * 6 + 2;
      const color = colors[Math.floor(Math.random() * colors.length)];

      sparkle.style.cssText = 'position:fixed;left:' + e.clientX + 'px;top:' + e.clientY + 'px;width:' + size + 'px;height:' + size + 'px;background:' + color + ';border-radius:50%;pointer-events:none;box-shadow:0 0 ' + (size * 2) + 'px ' + color + ';opacity:1;transition:all ' + (0.6 + Math.random() * 0.8) + 's cubic-bezier(0.25,0.46,0.45,0.94);z-index:9997;';
      sparkleContainer.appendChild(sparkle);

      requestAnimationFrame(function () {
        sparkle.style.opacity = '0';
        sparkle.style.transform = 'translate(' + ((Math.random() - 0.5) * 80) + 'px, ' + ((Math.random() - 0.5) * 80 + 30) + 'px) scale(0)';
      });

      setTimeout(function () { sparkle.remove(); }, 1500);
    });
  };

  // ─── 🎮 KONAMI CODE EASTER EGG ───────────────────────────
  GB.initKonamiCode = function () {
    var konamiSequence = ['ArrowUp','ArrowUp','ArrowDown','ArrowDown','ArrowLeft','ArrowRight','ArrowLeft','ArrowRight','b','a'];
    var konamiIndex = 0;

    document.addEventListener('keydown', function (e) {
      if (e.key === konamiSequence[konamiIndex]) {
        konamiIndex++;
        if (konamiIndex === konamiSequence.length) {
          konamiIndex = 0;
          GB.triggerMatrixRain();
        }
      } else {
        konamiIndex = 0;
      }
    });
  };

  GB.triggerMatrixRain = function () {
    var overlay = document.createElement('div');
    overlay.id = 'gbMatrixOverlay';
    overlay.style.cssText = 'position:fixed;top:0;left:0;width:100%;height:100%;z-index:99999;background:#000;cursor:pointer;';

    var cvs = document.createElement('canvas');
    overlay.appendChild(cvs);
    document.body.appendChild(overlay);

    var ctxM = cvs.getContext('2d');
    cvs.width = window.innerWidth;
    cvs.height = window.innerHeight;

    var fontSize = 14;
    var columns = Math.floor(cvs.width / fontSize);
    var drops = [];
    for (var i = 0; i < columns; i++) drops[i] = 1;
    var chars = 'GOLDBRANCH0123456789ABCDEF';

    var drawMatrix = function () {
      ctxM.fillStyle = 'rgba(0, 0, 0, 0.05)';
      ctxM.fillRect(0, 0, cvs.width, cvs.height);
      ctxM.font = fontSize + 'px JetBrains Mono, monospace';

      for (var i = 0; i < drops.length; i++) {
        var text = chars[Math.floor(Math.random() * chars.length)];
        ctxM.fillStyle = Math.random() > 0.95 ? '#22d3ee' : (Math.random() > 0.9 ? '#fff' : '#fbbf24');
        ctxM.fillText(text, i * fontSize, drops[i] * fontSize);
        if (drops[i] * fontSize > cvs.height && Math.random() > 0.975) drops[i] = 0;
        drops[i]++;
      }
    };

    setTimeout(function () {
      var msg = document.createElement('div');
      msg.style.cssText = 'position:absolute;top:50%;left:50%;transform:translate(-50%,-50%);text-align:center;z-index:2;';
      msg.innerHTML = '<div style="font-family:JetBrains Mono,monospace;font-size:3rem;font-weight:900;color:#fbbf24;text-shadow:0 0 40px #fbbf24;letter-spacing:8px;">GOLDBRANCH AI</div>' +
        '<div style="font-family:JetBrains Mono,monospace;font-size:0.9rem;color:#22d3ee;margin-top:16px;letter-spacing:4px;opacity:0.8;">SECRET MODE ACTIVATED</div>' +
        '<div style="font-family:Inter,sans-serif;font-size:0.8rem;color:#6e7681;margin-top:24px;">Kapatmak icin tikla</div>';
      overlay.appendChild(msg);
    }, 1500);

    var matrixInterval = setInterval(drawMatrix, 33);

    if (window.speechSynthesis) {
      var u = new SpeechSynthesisUtterance('Secret mode activated. Welcome to the Matrix.');
      u.lang = 'en-US';
      u.rate = 0.9;
      window.speechSynthesis.speak(u);
    }

    overlay.addEventListener('click', function () {
      clearInterval(matrixInterval);
      overlay.style.transition = 'opacity 0.5s';
      overlay.style.opacity = '0';
      setTimeout(function () { overlay.remove(); }, 500);
    });
  };

  // ─── 📡 LIVE ACTIVITY FEED WIDGET ─────────────────────────
  GB.initActivityFeed = function () {
    var container = document.getElementById('gbActivityFeed');
    if (!container) return;

    var fetchActivity = function () {
      fetch('/Task/GetLiveStats')
        .then(function (res) { return res.json(); })
        .then(function (data) {
          var now = new Date();
          var timeStr = now.toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit', second: '2-digit' });

          var events = [
            { icon: 'fa-layer-group', color: '#22d3ee', text: 'Toplam ' + data.total + ' gorev izleniyor', time: timeStr },
            { icon: 'fa-check-double', color: '#34d399', text: data.completed + ' gorev tamamlandi', time: timeStr },
            { icon: 'fa-bolt', color: '#fbbf24', text: data.active + ' aktif gorev devam ediyor', time: timeStr },
            { icon: 'fa-gauge-high', color: data.load > 70 ? '#ef4444' : (data.load > 40 ? '#fb923c' : '#10b981'), text: 'Sistem yuku %' + data.load, time: timeStr },
          ];

          container.innerHTML = events.map(function (e) {
            return '<div class="gb-feed-item">' +
              '<div class="gb-feed-dot" style="background:' + e.color + ';box-shadow:0 0 8px ' + e.color + ';"></div>' +
              '<div class="gb-feed-line"></div>' +
              '<div class="gb-feed-content">' +
              '<div class="gb-feed-icon" style="color:' + e.color + '"><i class="fa-solid ' + e.icon + '"></i></div>' +
              '<div class="gb-feed-text">' + e.text + '</div>' +
              '<div class="gb-feed-time">' + e.time + '</div>' +
              '</div></div>';
          }).join('');
        })
        .catch(function (err) { console.warn('Activity feed error:', err); });
    };

    fetchActivity();
    setInterval(fetchActivity, 60000);
  };

  // ─── 🖱️ CUSTOM CONTEXT MENU (SAĞ TIK) ─────────────────────
  GB.initContextMenu = function () {
    const menu = document.getElementById('gbContextMenu');
    if (!menu) return;

    document.addEventListener('contextmenu', (e) => {
      // Input veya textarea ise orijinal menüyü göster (kopyala/yapıştır için)
      if (e.target.tagName === 'INPUT' || e.target.tagName === 'TEXTAREA' || e.target.isContentEditable) return;
      
      e.preventDefault();
      
      let x = e.clientX;
      let y = e.clientY;
      
      // Ekran dışına taşmasını engelle
      if (x + 240 > window.innerWidth) x = window.innerWidth - 240;
      if (y + 200 > window.innerHeight) y = window.innerHeight - 200;

      menu.style.left = x + 'px';
      menu.style.top = y + 'px';
      menu.style.display = 'block';
      
      // Animasyon tetikleme
      menu.style.animation = 'none';
      menu.offsetHeight; // Reflow
      menu.style.animation = null;
    });

    document.addEventListener('click', (e) => {
      if (!menu.contains(e.target)) {
        menu.style.display = 'none';
      }
    });
    
    // Menü içindeki bir öğeye tıklandığında da menüyü kapat
    menu.querySelectorAll('.gb-cm-item').forEach(item => {
        item.addEventListener('click', () => {
            menu.style.display = 'none';
        });
    });
  };

  // ─── 🎵 WEB AUDIO SYNTHESIZER (UI SOUNDS) ────────────────
  GB.initAudioSynth = function() {
    const AudioContext = window.AudioContext || window.webkitAudioContext;
    if (!AudioContext) return;
    
    // Only init after first user interaction to bypass autoplay policies
    let actx = null;
    let isMuted = localStorage.getItem('gb-mute') === 'true';

    const initCtx = () => {
        if (!actx) actx = new AudioContext();
        if (actx.state === 'suspended') actx.resume();
        document.removeEventListener('click', initCtx);
        document.removeEventListener('keydown', initCtx);
    };
    document.addEventListener('click', initCtx);
    document.addEventListener('keydown', initCtx);

    const playSound = (freq, type, duration, vol) => {
        if (isMuted || !actx) return;
        const osc = actx.createOscillator();
        const gain = actx.createGain();
        osc.type = type;
        osc.frequency.setValueAtTime(freq, actx.currentTime);
        // Envelope
        gain.gain.setValueAtTime(0, actx.currentTime);
        gain.gain.linearRampToValueAtTime(vol, actx.currentTime + 0.02);
        gain.gain.exponentialRampToValueAtTime(0.001, actx.currentTime + duration);

        osc.connect(gain);
        gain.connect(actx.destination);
        osc.start();
        osc.stop(actx.currentTime + duration);
    };

    GB.playHoverSound = () => playSound(440, 'sine', 0.1, 0.05);
    GB.playClickSound = () => playSound(880, 'square', 0.15, 0.05);
    GB.playErrorSound = () => playSound(150, 'sawtooth', 0.3, 0.1);

    // Bind sounds to elements
    document.addEventListener('mouseenter', (e) => {
        if(e.target && e.target.classList && (e.target.classList.contains('btn') || e.target.classList.contains('gb-cm-item'))) {
            GB.playHoverSound();
        }
    }, true);

    document.addEventListener('mousedown', (e) => {
        if(e.target && e.target.classList && (e.target.classList.contains('btn') || e.target.classList.contains('gb-cm-item'))) {
            GB.playClickSound();
        }
    }, true);
  };

  // ─── ✨ HOLOGRAPHIC CARD GLARE (UI) ────────────────────────
  GB.initHologramCards = function() {
      document.querySelectorAll('.gb-holo-card, .gb-glass-card').forEach(card => {
          card.classList.add('gb-holo-card'); // Ensure class exists
          card.addEventListener('mousemove', e => {
              const rect = card.getBoundingClientRect();
              const x = ((e.clientX - rect.left) / rect.width) * 100;
              const y = ((e.clientY - rect.top) / rect.height) * 100;
              card.style.setProperty('--mouseX', x + '%');
              card.style.setProperty('--mouseY', y + '%');
          });
      });
  };

  // ─── MAIN INITIALIZATION ───────────────────────────────────
  GB.domReady(function () {
    GB.injectDynamicCSS();
    GB.initTheme();
    GB.initParticles();
    GB.initAurora();
    GB.initSparkleTrail();
    GB.initUIEffects();
    GB.initElegantTouches();
    GB.initJarvis();
    GB.initCommandPalette();
    GB.initAmbientPlayer();
    GB.initPomodoro();
    GB.initKonamiCode();
    GB.initActivityFeed();
    GB.initContextMenu();
    GB.initAudioSynth();
    GB.initHologramCards();

    // Start Ticker
    GB.updateTicker();
    setInterval(GB.updateTicker, 60000); // Update every minute

    // 🛡️ Quantum Scanner Loader Hide
    try {
      const scanner = GB.qs("#gbQuantumScanner");
      if (scanner) {
          setTimeout(() => {
              scanner.classList.add("hide");
              setTimeout(() => scanner.remove(), 700); // Cleanup after anim
          }, 600); // Slight delay for dramatic effect
      }
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

    // ==========================================
    // PWA & OFFLINE RESILIENCE LOGIC
    // ==========================================

    // 1. Install App Prompt
    let deferredPrompt;
    window.addEventListener('beforeinstallprompt', (e) => {
      // Prevent the mini-infobar from appearing on mobile
      e.preventDefault();
      // Stash the event so it can be triggered later.
      deferredPrompt = e;
      
      // Update UI notify the user they can install the PWA
      const installContainer = document.getElementById('gbInstallContainer');
      const installBtn = document.getElementById('gbInstallAppBtn');
      if (installContainer && installBtn) {
        installContainer.classList.remove('d-none');
        installContainer.classList.add('d-flex'); // show the button
        
        installBtn.addEventListener('click', async () => {
          // Show the install prompt
          deferredPrompt.prompt();
          // Wait for the user to respond to the prompt
          const { outcome } = await deferredPrompt.userChoice;
          if (outcome === 'accepted') {
            console.log('User accepted the install prompt');
          }
          deferredPrompt = null;
          installContainer.classList.add('d-none'); // Hide it again
        });
      }
    });
    
    // 2. Offline/Online Status Detectors
    window.addEventListener('offline', () => {
        GB.toast({ title: 'Bağlantı Koptu', text: 'Çevrimdışı moda geçildi, bazı özellikler kullanılamayabilir.', icon: 'warning', timer: 5000 });
        document.body.style.filter = "grayscale(20%)"; // Visual indicator
    });

    window.addEventListener('online', () => {
        GB.toast({ title: 'Bağlantı Geldi', text: 'Tekrar çevrimiçisiniz, sistem senkronize ediliyor...', icon: 'success' });
        document.body.style.filter = "none";
    });


    // Global JS Error Handler
    window.onerror = function(msg, source, line, col) {
      console.error('[GoldBranch Hata Yakalayici]', { msg, source, line, col });
      return false;
    };
  });
})();
