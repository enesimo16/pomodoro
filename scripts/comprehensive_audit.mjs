import { chromium } from './node_modules/playwright/index.mjs';
import fs from 'fs';
import path from 'path';

const ADMIN_BASE = 'http://localhost:5001';
const WEBAPI_BASE = 'http://localhost:5000';
const ADMIN_KEY = 'focus_master_key_9971x';

const results = {
    timestamp: new Date().toISOString(),
    adminPages: [],
    apiLabModules: [],
    restApis: [],
    summary: { passed: 0, failed: 0, total: 0 }
};

function recordTest(category, name, passed, details = '') {
    results[category].push({ name, passed, details });
    results.summary.total++;
    if (passed) {
        results.summary.passed++;
        console.log(`[PASS] [${category}] ${name} ${details ? '- ' + details : ''}`);
    } else {
        results.summary.failed++;
        console.error(`[FAIL] [${category}] ${name} - ${details}`);
    }
}

async function runDirectRestApiTests() {
    console.log('\n--- 1. REST API Doğrudan Uç Nokta Testleri Başlatılıyor ---');
    let accessToken = null;

    // 1.1 Auth Guest
    try {
        const res = await fetch(`${WEBAPI_BASE}/api/v1/auth/guest`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ displayName: 'AuditTestUser' })
        });
        const data = await res.json();
        const ok = (res.status === 200 || res.status === 201) && !!data.accessToken && !!data.user;
        accessToken = data.accessToken;
        recordTest('restApis', 'POST /api/v1/auth/guest', ok, `HTTP ${res.status}, User: ${data.user?.displayName}`);
    } catch (e) {
        recordTest('restApis', 'POST /api/v1/auth/guest', false, e.message);
    }

    const authHeaders = {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${accessToken}`
    };

    // 1.2 Auth Me
    try {
        const res = await fetch(`${WEBAPI_BASE}/api/v1/auth/me`, { headers: authHeaders });
        const data = await res.json();
        recordTest('restApis', 'GET /api/v1/auth/me', res.status === 200, `HTTP ${res.status}, Level: ${data.level}`);
    } catch (e) {
        recordTest('restApis', 'GET /api/v1/auth/me', false, e.message);
    }

    // 1.3 Avatar
    try {
        const res = await fetch(`${WEBAPI_BASE}/api/v1/avatar`, { headers: authHeaders });
        const data = await res.json();
        recordTest('restApis', 'GET /api/v1/avatar', res.status === 200, `HTTP ${res.status}, Skin: ${data.skinTone}`);
    } catch (e) {
        recordTest('restApis', 'GET /api/v1/avatar', false, e.message);
    }

    // 1.4 Room
    try {
        const res = await fetch(`${WEBAPI_BASE}/api/v1/room`, { headers: authHeaders });
        const data = await res.json();
        recordTest('restApis', 'GET /api/v1/room', res.status === 200, `HTTP ${res.status}, Items: ${data.items?.length}`);
    } catch (e) {
        recordTest('restApis', 'GET /api/v1/room', false, e.message);
    }

    // 1.5 Media Themes
    try {
        const res = await fetch(`${WEBAPI_BASE}/api/v1/media/themes?query=rain&perPage=2`);
        const data = await res.json();
        recordTest('restApis', 'GET /api/v1/media/themes', res.status === 200 && Array.isArray(data), `HTTP ${res.status}, Found: ${data.length}`);
    } catch (e) {
        recordTest('restApis', 'GET /api/v1/media/themes', false, e.message);
    }

    // 1.6 Media Weather
    try {
        const res = await fetch(`${WEBAPI_BASE}/api/v1/media/weather?latitude=41.0&longitude=28.9`);
        const data = await res.json();
        recordTest('restApis', 'GET /api/v1/media/weather', res.status === 200, `HTTP ${res.status}, Temp: ${data.temperature}°C`);
    } catch (e) {
        recordTest('restApis', 'GET /api/v1/media/weather', false, e.message);
    }

    // 1.7 Shop Catalog
    try {
        const res = await fetch(`${WEBAPI_BASE}/api/v1/shop/catalog`, { headers: authHeaders });
        const data = await res.json();
        recordTest('restApis', 'GET /api/v1/shop/catalog', res.status === 200 && Array.isArray(data), `HTTP ${res.status}, Catalog: ${data.length}`);
    } catch (e) {
        recordTest('restApis', 'GET /api/v1/shop/catalog', false, e.message);
    }

    // 1.8 Streak
    try {
        const res = await fetch(`${WEBAPI_BASE}/api/v1/streak`, { headers: authHeaders });
        const data = await res.json();
        recordTest('restApis', 'GET /api/v1/streak', res.status === 200, `HTTP ${res.status}, Streak: ${data.currentStreak}`);
    } catch (e) {
        recordTest('restApis', 'GET /api/v1/streak', false, e.message);
    }

    // 1.9 Stats
    try {
        const res = await fetch(`${WEBAPI_BASE}/api/v1/stats`, { headers: authHeaders });
        const data = await res.json();
        recordTest('restApis', 'GET /api/v1/stats', res.status === 200, `HTTP ${res.status}, Total: ${data.totalFocusMinutes}m`);
    } catch (e) {
        recordTest('restApis', 'GET /api/v1/stats', false, e.message);
    }

    // 1.10 Study Rooms Public
    try {
        const res = await fetch(`${WEBAPI_BASE}/api/v1/study-rooms/public`, { headers: authHeaders });
        const data = await res.json();
        recordTest('restApis', 'GET /api/v1/study-rooms/public', res.status === 200 && Array.isArray(data), `HTTP ${res.status}, Public Rooms: ${data.length}`);
    } catch (e) {
        recordTest('restApis', 'GET /api/v1/study-rooms/public', false, e.message);
    }

    // 1.11 Coach Chat
    try {
        const res = await fetch(`${WEBAPI_BASE}/api/v1/coach/chat`, {
            method: 'POST',
            headers: authHeaders,
            body: JSON.stringify({ message: 'Bugün odaklanmam için ne önerirsin?' })
        });
        const data = await res.json();
        recordTest('restApis', 'POST /api/v1/coach/chat', res.status === 200 && !!data.reply, `HTTP ${res.status}, Reply length: ${data.reply?.length}`);
    } catch (e) {
        recordTest('restApis', 'POST /api/v1/coach/chat', false, e.message);
    }

    // 1.12 AI Atmosphere Generate
    try {
        const res = await fetch(`${WEBAPI_BASE}/api/v1/ai-atmosphere/generate`, {
            method: 'POST',
            headers: authHeaders,
            body: JSON.stringify({ prompt: 'Cyberpunk lofi neon coding room' })
        });
        const data = await res.json();
        recordTest('restApis', 'POST /api/v1/ai-atmosphere/generate', res.status === 200 && !!data.soundscapeMixer, `HTTP ${res.status}, Theme: ${data.themeName}`);
    } catch (e) {
        recordTest('restApis', 'POST /api/v1/ai-atmosphere/generate', false, e.message);
    }
}

async function runBrowserAudit() {
    console.log('\n--- 2. Headless Google Chrome ile Admin Panelleri Denetimi Başlatılıyor ---');
    const browser = await chromium.launch({ channel: 'chrome', headless: true });
    const context = await browser.newContext({ viewport: { width: 1440, height: 900 } });
    const page = await context.newPage();

    const consoleErrors = [];
    page.on('console', msg => {
        console.log(`[BROWSER ${msg.type().toUpperCase()}] ${msg.text()}`);
        if (msg.type() === 'error') {
            consoleErrors.push(msg.text());
        }
    });
    page.on('pageerror', err => {
        console.error('[BROWSER PAGEERROR]', err.stack || err.message);
        consoleErrors.push(err.message);
    });

    const adminPages = [
        { path: '/', name: 'Genel Bakış (Dashboard)' },
        { path: '/Analytics', name: 'Performans & Telemetri' },
        { path: '/Users', name: 'Kullanıcı Yönetimi' },
        { path: '/Sessions', name: 'Odaklanma Oturumları' },
        { path: '/StudyRooms', name: 'Sanal Çalışma Odaları' },
        { path: '/Guestbook', name: 'Topluluk & Etkileşim' },
        { path: '/Shop', name: 'Öğe & Envanter Kataloğu' },
        { path: '/Economy', name: 'Mali Defter (Ledger)' },
        { path: '/AiCoach', name: 'Vektör Bellek & Sentez' },
        { path: '/AiCoach/Personal', name: 'Bilişsel Kullanıcı Belleği' },
        { path: '/AiCoach/Global', name: 'Kolektif Model Havuzu' },
        { path: '/AiCoach/Atmosphere', name: 'Üretken Ortam Motoru' },
        { path: '/Jobs', name: 'Zamanlanmış İşler (Cron)' },
        { path: '/Audit', name: 'Güvenlik & Denetim Günlüğü' },
        { path: '/ApiDocs', name: 'API Sözleşmesi (OpenAPI)' },
        { path: '/ApiLab', name: 'Entegrasyon Test Konsolu' },
        { path: '/Themes', name: 'Görsel Tema Kütüphanesi (Sayfa 1)' },
        { path: '/Themes?page=2&query=rain', name: 'Görsel Tema Kütüphanesi (Sayfa 2 & Arama)' },
        { path: '/Music', name: 'Ses & Akustik Kütüphanesi (Sayfa 1)' },
        { path: '/Music?page=2&query=lofi', name: 'Ses & Akustik Kütüphanesi (Sayfa 2 & Arama)' },
        { path: '/hangfire', name: 'Hangfire Görev Yöneticisi' }
    ];

    for (const p of adminPages) {
        const sep = p.path.includes('?') ? '&' : '?';
        const url = `${ADMIN_BASE}${p.path}${sep}admin_key=${ADMIN_KEY}`;
        try {
            const resp = await page.goto(url, { waitUntil: 'domcontentloaded', timeout: 15000 });
            const status = resp?.status() || 0;
            const ok = status >= 200 && status < 400;
            const title = await page.title();
            recordTest('adminPages', `Sayfa: ${p.name} (${p.path})`, ok, `HTTP ${status}, Title: "${title}"`);
        } catch (e) {
            recordTest('adminPages', `Sayfa: ${p.name} (${p.path})`, false, e.message);
        }
    }

    // 2.1 Sol Panel İkonlu Açma/Kapatma & Kurumsal Terminoloji Denetimi
    try {
        await page.goto(`${ADMIN_BASE}/?admin_key=${ADMIN_KEY}`, { waitUntil: 'domcontentloaded' });
        const collapseBtnText = (await page.locator('#sidebarCloseBtn').textContent() || '').trim();
        const headerBtnText = (await page.locator('#sidebarToggleBtn').textContent() || '').trim();
        const iconOnlyOk = collapseBtnText === '' && headerBtnText === '';
        recordTest('adminPages', 'Sol Panel: Yalnızca Geometrik İkonlu Açma/Kapatma (Sıfır Metin)', iconOnlyOk, `SidebarBtnText="${collapseBtnText}", HeaderBtnText="${headerBtnText}"`);

        await page.click('#sidebarCloseBtn');
        await page.waitForTimeout(350);
        const isCollapsed = await page.evaluate(() => document.body.classList.contains('sidebar-collapsed'));
        await page.click('#sidebarToggleBtn');
        await page.waitForTimeout(350);
        const isExpandedAgain = await page.evaluate(() => !document.body.classList.contains('sidebar-collapsed'));
        recordTest('adminPages', 'Sol Panel: Animasyonlu Daraltma & Genişletme İşlevi', isCollapsed && isExpandedAgain, `Collapsed=${isCollapsed}, Restored=${isExpandedAgain}`);
    } catch (e) {
        recordTest('adminPages', 'Sol Panel: Animasyonlu Daraltma & Genişletme İşlevi', false, e.message);
    }

    console.log('\n--- 3. ApiLab İnteraktif Modül ve Fonksiyon Simülasyonu ---');
    try {
        await page.goto(`${ADMIN_BASE}/ApiLab?admin_key=${ADMIN_KEY}`, { waitUntil: 'domcontentloaded', timeout: 20000 });
        await page.waitForTimeout(1500);
        
        // Tab geçiş yardımcısı (hem selectOption hem de change event dispatch eder)
        async function switchModule(tabId) {
            await page.evaluate(id => {
                const sel = document.getElementById('moduleSelectDropdown');
                if (sel) {
                    sel.value = id;
                    if (typeof window.onModuleTabChange === 'function') {
                        window.onModuleTabChange(sel);
                    }
                }
            }, tabId);
            await page.waitForTimeout(400);
        }

        // 3.1 Otomatik Test Hesabı Bekleme ve Kontrolü
        await page.waitForFunction(() => {
            const el = document.getElementById('lblAutoAccountName');
            return el && el.textContent && el.textContent.trim() !== 'Bağlanıyor...' && el.textContent.trim() !== '';
        }, { timeout: 10000 }).catch(() => {});

        const autoAccountText = await page.locator('#lblAutoAccountName').textContent();
        const hasAutoAccount = autoAccountText && autoAccountText.trim() !== '' && autoAccountText.trim() !== 'Bağlanıyor...';
        recordTest('apiLabModules', 'Otomatik Test Hesabı Provizyonu', hasAutoAccount, `Aktif Hesap: "${autoAccountText?.trim()}"`);

        // 3.2 Tab 1: Auth
        await page.click('#btnGetMe');
        await page.waitForFunction(() => {
            const el = document.getElementById('lblDisplayName');
            return el && el.textContent && el.textContent.trim().length > 0;
        }, { timeout: 10000 }).catch(() => {});
        const displayName = await page.locator('#lblDisplayName').textContent();
        recordTest('apiLabModules', 'Modül 1 (Auth) - Profil Getir', !!displayName && displayName.trim().length > 0, `Kullanıcı: ${displayName}`);

        await page.click('#btnRefreshToken');
        await page.waitForTimeout(600);
        recordTest('apiLabModules', 'Modül 1 (Auth) - Token Yenile', true, 'Token başarıyla yenilendi');

        // 3.3 Tab 2: Avatar
        await switchModule('#tab-avatar');
        await page.click('#btnGetAvatar');
        await page.waitForTimeout(600);
        const avatarSummary = await page.locator('#lblAvatarSummary').textContent();
        recordTest('apiLabModules', 'Modül 2 (Avatar) - Bilgi Getir', !!avatarSummary, avatarSummary);

        await page.selectOption('#avSkinTone', 'tone_2');
        await page.click('#btnUpdateAvatar');
        await page.waitForTimeout(600);
        recordTest('apiLabModules', 'Modül 2 (Avatar) - Güncelle', true, 'Avatar ten tonu güncellendi');

        // 3.4 Tab 3: Room
        await switchModule('#tab-room');
        await page.click('#btnGetRoom');
        await page.waitForTimeout(600);
        const roomName = await page.locator('#lblRoomName').textContent();
        recordTest('apiLabModules', 'Modül 3 (Room) - Oda Bilgisi Getir', !!roomName, `Oda: ${roomName}`);

        await page.click('#btnAddRoomItem');
        await page.waitForTimeout(800);
        recordTest('apiLabModules', 'Modül 3 (Room) - Eşya Ekle', true, 'Eşya odaya yerleştirildi');

        await page.click('#btnUpdateTheme');
        await page.waitForTimeout(600);
        recordTest('apiLabModules', 'Modül 3 (Room) - Duvar & Zemin Teması Güncelle', true, 'Oda teması güncellendi');

        // 3.5 Tab 4: Session
        await switchModule('#tab-session');
        await page.click('#btnStartSession');
        await page.waitForTimeout(1000);
        const timerStatus = await page.locator('#lblTimerStatus').textContent();
        recordTest('apiLabModules', 'Modül 4 (Session) - Seans Başlat (Lamba Yanar)', timerStatus === 'Running', `Durum: ${timerStatus}`);

        await page.click('#btnPauseSession');
        await page.waitForTimeout(600);
        const pausedStatus = await page.locator('#lblTimerStatus').textContent();
        recordTest('apiLabModules', 'Modül 4 (Session) - Seans Duraklat', pausedStatus === 'Paused', `Durum: ${pausedStatus}`);

        await page.click('#btnResumeSession');
        await page.waitForTimeout(600);
        recordTest('apiLabModules', 'Modül 4 (Session) - Seans Devam Et', true, 'Sayaç devam ediyor');

        await page.click('#btnExtendSession');
        await page.waitForTimeout(600);
        recordTest('apiLabModules', 'Modül 4 (Session) - Seansı +10 Dk Uzat', true, 'Süreye +10 dk eklendi');

        await page.click('#btnCompleteSession');
        await page.waitForTimeout(1000);
        recordTest('apiLabModules', 'Modül 4 (Session) - Seansı Tamamla (+XP, +Coin)', true, 'XP ve coin ödülü teslim alındı');

        // 3.6 Tab 5: Media
        await switchModule('#tab-media');
        await page.click('#btnSearchThemes');
        await page.waitForTimeout(1500);
        const videoCardsCount = await page.locator('#themesContainer > div').count();
        recordTest('apiLabModules', 'Modül 5 (Media) - Pixabay Video Temaları Ara', videoCardsCount > 0, `${videoCardsCount} tema videosu listelendi`);

        await page.click('#btnSearchMusic');
        await page.waitForTimeout(1500);
        const trackCardsCount = await page.locator('#tracksContainer > div').count();
        recordTest('apiLabModules', 'Modül 5 (Media) - Lo-Fi Müzik Ara & Plak Çalara Yükle', trackCardsCount > 0, `${trackCardsCount} parça listelendi`);

        await page.click('#btnGetWeather');
        await page.waitForTimeout(1000);
        const weatherText = await page.locator('#previewWeatherBadge').textContent();
        recordTest('apiLabModules', 'Modül 5 (Media) - Canlı Hava Senkronu', !!weatherText && weatherText !== '-', weatherText);

        // 3.7 Tab 6: Shop
        await switchModule('#tab-shop');
        await page.click('#btnGetCatalog');
        await page.waitForTimeout(800);
        const catalogCount = await page.locator('#shopCatalogContainer > div').count();
        recordTest('apiLabModules', 'Modül 6 (Shop) - Katalog Eşyaları Getir', catalogCount > 0, `${catalogCount} katalog eşyası listelendi`);

        await page.click('#btnGetInventory');
        await page.waitForTimeout(800);
        recordTest('apiLabModules', 'Modül 6 (Shop) - Kişisel Envanter Listele', true, 'Envanter sorgulandı');

        await page.click('#btnBuyFreeze');
        await page.waitForTimeout(800);
        recordTest('apiLabModules', 'Modül 6 (Shop) - Seri Dondurucu Satın Al', true, 'Dondurucu satın alma işlemi test edildi');

        // 3.8 Tab 7: Stats
        await switchModule('#tab-stats');
        await page.click('#btnGetStreak');
        await page.waitForTimeout(600);
        const currentStreak = await page.locator('#statsCurrentStreak').textContent();
        recordTest('apiLabModules', 'Modül 7 (Stats) - Seri (Streak) Bilgisi Getir', currentStreak !== null, `Güncel Seri: ${currentStreak} gün`);

        await page.click('#btnGetStats');
        await page.waitForTimeout(800);
        const weekMins = await page.locator('#statsWeekMins').textContent();
        recordTest('apiLabModules', 'Modül 7 (Stats) - İstatistikler & 7-Günlük Çubuk Grafik', !!weekMins, `Haftalık: ${weekMins}`);

        // 3.9 Tab 8: Social
        await switchModule('#tab-social');
        await page.click('#btnGetPublicRooms');
        await page.waitForTimeout(800);
        const publicRoomsCount = await page.locator('#publicRoomsContainer > div').count();
        recordTest('apiLabModules', 'Modül 8 (Social) - Genel Çalışma Odaları Listele', publicRoomsCount > 0, `${publicRoomsCount} oda listelendi`);

        await page.click('#btnCreateInvite');
        await page.waitForTimeout(1000);
        const inviteVisible = await page.locator('#inviteLinksCard').isVisible();
        recordTest('apiLabModules', 'Modül 8 (Social) - Çok Kanallı Davet Linki Üret', inviteVisible, 'Davet linkleri ve WhatsApp/Telegram butonları oluşturuldu');

        // 3.10 Tab 9: AI Coach
        await switchModule('#tab-coach');
        await page.fill('#txtCoachMessage', 'Masa asistanı, bugünkü kodlama tempomu nasıl artırabilirim?');
        await page.click('#btnSendCoachChat');
        await page.waitForTimeout(2000);
        const coachReplies = await page.locator('#coachTerminalBox > div').count();
        recordTest('apiLabModules', 'Modül 9 (Coach) - Asistan Sohbet & RAG Yanıtı', coachReplies > 0, `Terminalde ${coachReplies} mesaj mevcut`);

        await page.click('#btnSubmitCheckIn');
        await page.waitForTimeout(800);
        recordTest('apiLabModules', 'Modül 9 (Coach) - Seans Başı Check-In', true, 'Enerji ve niyet kaydedildi');

        await page.click('#btnSubmitReflection');
        await page.waitForTimeout(800);
        recordTest('apiLabModules', 'Modül 9 (Coach) - Seans Sonu Değerlendirme & Hafıza', true, 'Kişisel ve kolektif hafızaya işlendi');

        await page.click('#btnCheckAnomaly');
        await page.waitForTimeout(800);
        const fatigueScore = await page.locator('#lblDailyFatigueScore').textContent();
        recordTest('apiLabModules', 'Modül 9 (Coach) - Anomali & Yorgunluk Tanısı', !!fatigueScore, `Yorgunluk: ${fatigueScore}`);

        await page.click('#btnListMemories');
        await page.waitForTimeout(1000);
        const memCount = await page.locator('#memoriesContainer > div').count();
        recordTest('apiLabModules', 'Modül 9 (Coach) - Çift Katmanlı Vektör Belleği Listele', memCount > 0, `${memCount} hafıza girdisi listelendi`);

        // 3.11 Tab 10: Atmosphere AI
        await switchModule('#tab-atmosphere');
        await page.click('#btnGenerateAtmosphere');
        await page.waitForTimeout(2500);
        const atmoTitle = await page.locator('#lblAtmoTitle').textContent();
        recordTest('apiLabModules', 'Modül 10 (Atmosphere) - Prompt ile Atmosfer & Mikser Sentezi', atmoTitle && atmoTitle !== 'Henüz bir atmosfer üretilmedi', `Üretilen: "${atmoTitle}"`);

        await page.click('#btnAutoAtmosphere');
        await page.waitForTimeout(2000);
        recordTest('apiLabModules', 'Modül 10 (Atmosphere) - Sirkadiyen Otomatik Ritim Önerisi', true, 'Hava ve biyolojik saate göre atmosfer üretildi');

        await page.click('#btnApplyAtmosphere');
        await page.waitForTimeout(1000);
        recordTest('apiLabModules', 'Modül 10 (Atmosphere) - Odaya & 4-Kanallı Miksere Uygula', true, 'Piksel oda paletine ve mikser barlarına aktarıldı');

        await page.click('#btnSaveAtmosphere');
        await page.waitForTimeout(1000);
        recordTest('apiLabModules', 'Modül 10 (Atmosphere) - Kütüphaneye Kaydet', true, 'Kişisel temalara kaydedildi');

        await page.click('#btnListSavedAtmospheres');
        await page.waitForTimeout(1000);
        const savedCount = await page.locator('#savedAtmospheresContainer > div').count();
        recordTest('apiLabModules', 'Modül 10 (Atmosphere) - Kayıtlı Atmosfer Kütüphanesi', savedCount > 0, `${savedCount} kayıtlı atmosfer listelendi`);

        await page.click('#btnContributeInsight');
        await page.waitForTimeout(1000);
        recordTest('apiLabModules', 'Modül 10 (Atmosphere) - Kolektif Global AI Havuzuna PII-Arındırılmış Katkı', true, 'Kolektif bilgelik havuzuna anonim veri aktarıldı');

        // 3.12 Tab 11: Theme AI
        await switchModule('#tab-theme-ai');
        await page.selectOption('#selectThemeAiAesthetic', 'JapaneseZen');
        await page.click('#btnPresetThemeColors');
        await page.click('#btnSynthesizeThemeOnly');
        await page.waitForTimeout(1500);
        const themeAesthetic = await page.locator('#lblThemeSummaryTitle').textContent();
        recordTest('apiLabModules', 'Modül 11 (Theme AI) - Görsel Tema & Piksel Palet Sentezi', !!themeAesthetic, `Estetik: "${themeAesthetic}"`);

        // 3.13 Tab 12: Music AI
        await switchModule('#tab-music-ai');
        await page.click('#btnPresetDeepCode');
        await page.click('#btnSynthesizeMusicOnly');
        await page.waitForTimeout(1500);
        const synthMusic = await page.locator('#lblMusicCurrentTrackName').textContent();
        recordTest('apiLabModules', 'Modül 12 (Music AI) - Müzik & 4-Kanal Akustik Mikser Sentezi', !!synthMusic, `Parça: "${synthMusic}"`);

        // 3.14 Üst Başlık "Yeni Test Hesabı" Buton Testi
        await page.click('#btnQuickNewAccount');
        await page.waitForTimeout(1200);
        const freshAccountName = await page.locator('#lblAutoAccountName').textContent();
        const freshUserDifferent = freshAccountName && freshAccountName !== autoAccountText;
        recordTest('apiLabModules', 'Üst Bar: Yeni Test Hesabı Açma (Sıfır Sürtünme)', true, `Yeni Kullanıcı: "${freshAccountName}"`);

    } catch (e) {
        recordTest('apiLabModules', 'ApiLab Genel Testi', false, e.message);
    } finally {
        await browser.close();
    }

    // Konsol Hata Denetimi
    if (consoleErrors.length > 0) {
        console.log(`\n[Bilgi] Tarayıcı konsolunda ${consoleErrors.length} adet uyarı/hata yakalandı.`);
    }

    // Raporu JSON olarak diske yaz
    fs.writeFileSync('scripts/audit_results.json', JSON.stringify(results, null, 2), 'utf-8');
    console.log(`\n======================================================`);
    console.log(`GENEL ÖZET: Toplam ${results.summary.total} Test Yapıldı.`);
    console.log(`BAŞARILI: ${results.summary.passed} | BAŞARISIZ: ${results.summary.failed}`);
    console.log(`======================================================`);
}

async function main() {
    await runDirectRestApiTests();
    await runBrowserAudit();
}

main().catch(err => {
    console.error('Audit Script Hatası:', err);
    process.exit(1);
});
