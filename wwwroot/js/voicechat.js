(function() {
    let lastLogo = null;
    let academicCounter = 0;
    
    // Global download data store (survives innerHTML copy)
    window._gbDownloads = window._gbDownloads || {};
    let dlCounter = 0;
    
    // UI Elements
    const msgArea = document.getElementById('vcMessages');
    const input = document.getElementById('vcInput');
    const micBtn = document.getElementById('vcMicBtn');
    const micSelect = document.getElementById('vcMicSelect');
    const status = document.getElementById('vcStatus');
    
    // Hidden Values
    const txtReady = document.getElementById('txtReady') ? document.getElementById('txtReady').value : 'Ready';
    const txtThinking = document.getElementById('txtThinking') ? document.getElementById('txtThinking').value : 'Thinking...';
    const txtListening = document.getElementById('txtListening') ? document.getElementById('txtListening').value : 'Listening...';

    // Mic Vars
    let stream = null;
    let isListening = false;
    let recognition = null;
    const SpeechRec = window.SpeechRecognition || window.webkitSpeechRecognition;

    const logoInput = document.getElementById('acLogo');
    if (logoInput) {
        logoInput.onchange = function(e) {
            const file = e.target.files[0];
            if(!file) return;
            const reader = new FileReader();
            reader.onload = (evt) => { lastLogo = evt.target.result; };
            reader.readAsDataURL(file);
        };
    }

    window.openAcademicForm = () => {
        const overlay = document.getElementById('academicFormOverlay');
        overlay.classList.remove('d-none');
        overlay.classList.add('d-flex');
    };
    window.closeAcademicForm = () => {
        const overlay = document.getElementById('academicFormOverlay');
        overlay.classList.add('d-none');
        overlay.classList.remove('d-flex');
    };

    window.loadMemory = async function() {
        const container = document.getElementById('memoryListContainer');
        if(!container) return;
        container.innerHTML = '<div class="text-center py-5"><i class="fa-solid fa-circle-notch fa-spin text-info fa-2x"></i></div>';
        
        try {
            const res = await fetch('/Ai/GetMemoryLogs');
            const data = await res.json();
            if (data.success && data.logs) {
                if (data.logs.length === 0) {
                    container.innerHTML = '<div class="text-center text-muted py-5"><i class="fa-solid fa-ghost fa-2x mb-2 opacity-50"></i><p>Henüz hafızada proje yok.</p></div>';
                    return;
                }
                
                container.innerHTML = '';
                data.logs.forEach(log => {
                    const icon = log.isProject ? 'fa-graduation-cap text-warning' : 'fa-list-check text-info';
                    const div = document.createElement('div');
                    div.className = 'p-3 mb-2 rounded-3 border position-relative';
                    div.style.cssText = 'background: rgba(255,255,255,0.02); border-color: rgba(255,255,255,0.1)!important; cursor: pointer; transition: 0.2s;';
                    div.onmouseover = () => div.style.background = 'rgba(255,255,255,0.08)';
                    div.onmouseout = () => div.style.background = 'rgba(255,255,255,0.02)';
                    div.onclick = () => {
                        // Close offcanvas if possible
                        try {
                            const bsOffcanvas = bootstrap.Offcanvas.getInstance(document.getElementById('memoryOffcanvas'));
                            if(bsOffcanvas) bsOffcanvas.hide();
                        } catch(e) {}

                        addMsg('user', '<i class="fa-solid fa-clock-rotate-left me-1 text-info"></i> <span class="opacity-75">[Hafızadan Çağrıldı]</span> ' + log.title, true);
                        
                        const preview = document.createElement('div');
                        if (log.isProject) {
                            preview.innerHTML = '<div class="a4-preview-card"><h4>' + log.title.replace('[PROJE] ', '') + '</h4><hr>'+ formatText(log.content) +'</div>';
                            const dlId = 'dl_' + (dlCounter++);
                            window._gbDownloads[dlId] = { uni: "Projeler", dept: "Kayıt", topic: log.title, content: log.content };
                            preview.innerHTML += '<button class="btn btn-sm btn-warning mt-2" onclick="window._gbDownloadById(\'' + dlId + '\')"><i class="fa-solid fa-download"></i> İndir</button>';
                        } else {
                            preview.innerHTML = formatText(log.content);
                        }
                        
                        addMsg('ai', preview.innerHTML, true);
                    };
                    
                    div.innerHTML = `
                        <div class="d-flex align-items-center gap-2 mb-2 pr-4">
                            <i class="fa-solid ${icon}"></i>
                            <div class="fw-bold text-truncate" style="font-size: 0.85rem; max-width: 80%; color: #e4e4e7;">${log.title}</div>
                        </div>
                        <div class="text-muted d-flex justify-content-between align-items-center" style="font-size: 0.75rem;">
                            <span><i class="fa-regular fa-clock me-1"></i> ${log.date}</span>
                        </div>
                        <button class="btn btn-sm btn-outline-danger position-absolute" style="top: 10px; right: 10px; padding: 2px 6px;" title="Projeyi Sil" onclick="deleteMemoryLog(event, ${log.id}, this.parentElement)">
                            <i class="fa-solid fa-trash-can"></i>
                        </button>
                    `;
                    container.appendChild(div);
                });
            } else {
                container.innerHTML = '<div class="text-danger small px-2">Veri alınamadı.</div>';
            }
        } catch(e) {
            container.innerHTML = '<div class="text-danger small px-2">Bağlantı hatası: ' + e.message + '</div>';
        }
    };

    window.deleteMemoryLog = async function(event, id, element) {
        event.stopPropagation();
        if (!confirm('Bu projeyi hafızadan KALICI olarak silmek istediğinize emin misiniz?')) return;
        
        try {
            const res = await fetch('/Ai/DeleteMemoryLog', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ Id: id })
            });
            
            if (!res.ok) {
                const text = await res.text();
                alert('Sunucu hatası: ' + res.status + ' - Lütfen PROJEYİ YENİDEN BAŞLATIN (Ctrl+R / C# decompile gerekli). Detay: ' + text.substring(0, 50));
                return;
            }

            const data = await res.json();
            if (data.success) {
                element.style.transition = "0.3s";
                element.style.opacity = "0";
                element.style.transform = "translateX(20px)";
                setTimeout(() => element.remove(), 300);
            } else {
                alert(data.message || 'Silme başarısız!');
            }
        } catch(e) {
            alert('Ağ VEYA JSON Hatası: ' + e.message + '\n\n LÜTFEN Visual Studio üzerinden projeyi durdurup tekrar BAŞLAT (F5) yapın.');
        }
    };

    window.clearAllMemory = async function() {
        if (!confirm('TÜM HAFIZA KALICI OLARAK SİLİNECEK! Tüm geçmiş projelerinizi çöpe atmak istediğinize emin misiniz?')) return;
        
        try {
            const res = await fetch('/Ai/ClearMemoryLogs', { method: 'POST' });
            if (!res.ok) {
                alert('Sunucu hatası: Lütfen projenizi baştan başlatın (Yeniden derleme).');
                return;
            }
            const data = await res.json();
            if (data.success) {
                document.getElementById('memoryListContainer').innerHTML = '<div class="text-center text-muted py-5"><i class="fa-solid fa-ghost fa-2x mb-2 opacity-50"></i><p>Tüm hafıza tertemiz!</p></div>';
            } else {
                alert('Hata: ' + data.message);
            }
        } catch(e) {
            alert('Bağlantı Hatası: Lütfen projeyi (Visual Studio üzerinden) durdurup F5 ile tekrar başlatın.');
        }
    };

    window.loadMics = async function() {
        try {
            if (!stream) {
                stream = await navigator.mediaDevices.getUserMedia({ audio: { echoCancellation: false } });
            }
            const devices = await navigator.mediaDevices.enumerateDevices();
            const inputs = devices.filter(d => d.kind === 'audioinput');
            micSelect.innerHTML = '';
            inputs.forEach((d, i) => {
                const opt = document.createElement('option');
                opt.value = d.deviceId;
                opt.text = d.label || (d.deviceId === 'default' ? 'Varsayılan Cihaz' : 'Mikrofon ' + (i+1));
                micSelect.appendChild(opt);
            });
        } catch (e) {
            if(micSelect) micSelect.innerHTML = '<option value="">Erişim Yok</option>';
        }
    };
    if(micSelect) window.loadMics();

    function addMsg(type, content, isHtml = false) {
        const tmplId = type === 'user' ? 'tmplUserMsg' : (type === 'ai' ? 'tmplAiMsg' : 'tmplErrorMsg');
        const tmpl = document.getElementById(tmplId);
        if(!tmpl) return;
        const clone = tmpl.content.cloneNode(true);
        const target = clone.querySelector('.vc-user-msg') || clone.querySelector('.vc-ai-msg');
        if (isHtml) target.innerHTML = content;
        else target.textContent = content;
        msgArea.appendChild(clone);
        msgArea.scrollTop = msgArea.scrollHeight;
    }

    function addThinking() {
        const tmpl = document.getElementById('tmplThinkRow');
        const clone = tmpl.content.cloneNode(true);
        msgArea.appendChild(clone);
        msgArea.scrollTop = msgArea.scrollHeight;
        return msgArea.lastElementChild;
    }

    function formatText(text) {
        let h = text;
        h = h.replace(/```[\s\S]*?```/g, m => {
            const code = m.replace(/```\w*\n?/g, '').replace(/```/g, '');
            return '<pre>' + code.replace(/[&<>"']/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":"&#39;"}[c])) + '</pre>';
        });
        h = h.replace(/### (.*?)\n/g, '<h3>$1</h3>');
        h = h.replace(/## (.*?)\n/g, '<h2>$1</h2>');
        h = h.replace(/# (.*?)\n/g, '<h1>$1</h1>');
        h = h.replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>');
        h = h.replace(/\n\n/g, '</p><p>');
        h = h.replace(/\n/g, '<br>');
        return '<p>' + h + '</p>';
    }

    window.submitAcademicForm = async function() {
        const topic = document.getElementById('acTopic').value;
        const uni = document.getElementById('acUni').value;
        const dept = document.getElementById('acDept').value;
        if(!topic) return;

        closeAcademicForm();
        addMsg('user', '[Akademik Proje] ' + topic);
        const think = addThinking();
        document.getElementById('acSubmitBtn').disabled = true;

        try {
            const res = await fetch('/Ai/GenerateAcademicHomework', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ Topic: topic, University: uni, Department: dept })
            });
            const data = await res.json();
            think.remove();
            if (data.success) {
                const id = academicCounter++;
                const preview = document.createElement('div');
                preview.innerHTML = '<div class="a4-preview-card"><h4>' + uni + '</h4><hr>'+ formatText(data.content) +'</div>';
                const dlId = 'dl_' + (dlCounter++);
                window._gbDownloads[dlId] = { uni, dept, topic, content: data.content };
                preview.innerHTML += '<button class="btn btn-sm btn-warning mt-2" onclick="window._gbDownloadById(\'' + dlId + '\')"><i class="fa-solid fa-download"></i> İndir</button>';
                addMsg('ai', preview.innerHTML, true);
            }
        } catch(e) { think.remove(); addMsg('error', 'Hata!'); }
        document.getElementById('acSubmitBtn').disabled = false;
    };

    // Global download trigger
    window._gbDownloadById = function(dlId) {
        const d = window._gbDownloads[dlId];
        if (!d) { alert('İndirme verisi bulunamadı!'); return; }
        downloadWordUI(d.uni, d.dept, d.topic, d.content);
    };

    function downloadWordUI(uni, dept, topic, content) {
        const title = topic || "Sistem Raporu";
        const htmlContent = `
<html xmlns:o="urn:schemas-microsoft-com:office:office" xmlns:w="urn:schemas-microsoft-com:office:word" xmlns="http://www.w3.org/TR/REC-html40">
<head>
    <meta charset="utf-8">
    <title>${title}</title>
    <style>
        body { font-family: 'Calibri', 'Inter', sans-serif; }
        h1 { color: #2c3e50; }
        h3 { color: #555555; font-style: italic; }
        p { line-height: 1.5; }
    </style>
</head>
<body>
    <h1>${uni}</h1>
    <h3>${dept}</h3>
    <i>Konu: ${title}</i>
    <hr>
    ${formatText(content)}
</body>
</html>`;
        
        // Use application/vnd.ms-word.document or standard msword with explicit BOM and UTF-8
        const blob = new Blob(['\ufeff', htmlContent], { type: 'application/msword;charset=utf-8' });
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        
        a.style.display = 'none';
        a.href = url;
        a.download = 'Proje.doc';
        
        document.body.appendChild(a);
        a.click();
        
        setTimeout(() => {
            document.body.removeChild(a);
            window.URL.revokeObjectURL(url);
        }, 100);
    }

    window.sendQuestion = async function(txt) {
        if(!txt || !txt.trim()) return;
        addMsg('user', txt);
        if(input) input.value = '';
        const think = addThinking();
        if(status) status.textContent = txtThinking;

        try {
            const res = await fetch('/Ai/AskResearch', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ question: txt })
            });
            const data = await res.json();
            think.remove();
            if (data.success) {
                const preview = document.createElement('div');
                preview.innerHTML = formatText(data.answer);
                
                const shortQ = txt.length > 40 ? txt.substring(0, 40) + "..." : txt;
                const dlId = 'dl_' + (dlCounter++);
                window._gbDownloads[dlId] = { uni: "Araştırma Raporu", dept: shortQ, topic: "AI", content: data.answer };
                preview.innerHTML += '<button class="btn btn-sm btn-info mt-2 text-dark fw-bold" onclick="window._gbDownloadById(\'' + dlId + '\')"><i class="fa-solid fa-download"></i> Raporu İndir</button>';
                
                addMsg('ai', preview.innerHTML, true);
                if(status) status.textContent = txtReady;
            }
        } catch(e) { think.remove(); if(status) status.textContent = 'Bağlantı Hatası'; }
    };

    if(document.getElementById('vcSendBtn')) {
        document.getElementById('vcSendBtn').onclick = () => sendQuestion(input.value);
    }
    if(input) {
        input.onkeydown = (e) => { if(e.key === 'Enter') sendQuestion(input.value); };
    }

    if (SpeechRec) {
        recognition = new SpeechRec();
        
        // Dil tespiti (Cookie veya HTML lang üzerinden)
        const getLang = () => {
             const cookieValue = document.cookie.split('; ').find(row => row.startsWith('PreferredLanguage='))?.split('=')[1];
             return cookieValue === 'en' ? 'en-US' : 'tr-TR';
        };

        recognition.lang = getLang();
        recognition.onresult = (e) => {
            const t = e.results[0][0].transcript;
            stopListen();
            sendQuestion(t);
        };
        recognition.onend = () => { if(isListening) stopListen(); };
        recognition.onerror = (e) => {
            console.error("Speech Error:", e.error);
            const isTr = recognition.lang === 'tr-TR';
            const errMap = {
                'no-speech': isTr ? 'Ses algılanmadı.' : 'No speech detected.',
                'audio-capture': isTr ? 'Mikrofon bulunamadı.' : 'Microphone not found.',
                'not-allowed': isTr ? 'Mikrofon izni yok!' : 'No mic permission!',
                'network': isTr ? 'Ağ hatası.' : 'Network error.'
            };
            if(status) status.textContent = errMap[e.error] || (isTr ? 'Hata: ' : 'Error: ') + e.error;
            stopListen();
        };

        function stopListen() {
            recognition.stop();
            isListening = false;
            micBtn.classList.remove('vc-recording');
            const micIcon = document.getElementById('vcMicIcon');
            if(micIcon) micIcon.className = 'fa-solid fa-microphone';
            if(status && status.textContent.indexOf('Hata') === -1) status.textContent = txtReady;
        }

        if(micBtn) {
            micBtn.onclick = async () => {
                if (isListening) { stopListen(); return; }
                try {
                    // Update lang just before start
                    recognition.lang = getLang();
                    if (!stream) stream = await navigator.mediaDevices.getUserMedia({ audio: true });
                    recognition.start();
                    isListening = true;
                    micBtn.classList.add('vc-recording');
                    const micIcon = document.getElementById('vcMicIcon');
                    if(micIcon) micIcon.className = 'fa-solid fa-stop';
                    if(status) status.textContent = txtListening;
                } catch(e) { 
                    if(status) status.textContent = (getLang() === 'tr-TR') ? 'Erişim Hatası' : 'Access Error'; 
                }
            };
        }
    }
})();
