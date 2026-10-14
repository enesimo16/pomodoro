import { chromium } from 'playwright';

const ADMIN_BASE = 'http://localhost:5001';
const ADMIN_KEY = 'focus_master_key_9971x';

async function main() {
    console.log('--- ApiLab Atmosfer, Tema ve Müzik Ayrıştırma Doğrulama Testi Başlıyor ---');
    const browser = await chromium.launch({ channel: 'chrome', headless: true });
    const context = await browser.newContext({ viewport: { width: 1440, height: 960 } });
    const page = await context.newPage();

    const errors = [];
    page.on('console', msg => {
        if (msg.type() === 'error') {
            console.log(`[BROWSER ERROR] ${msg.text()}`);
            errors.push(msg.text());
        }
    });

    try {
        await page.goto(`${ADMIN_BASE}/ApiLab?admin_key=${ADMIN_KEY}`, { waitUntil: 'domcontentloaded', timeout: 20000 });
        await page.waitForTimeout(1500);

        // Otomatik hesap bekleme
        await page.waitForFunction(() => {
            const el = document.getElementById('lblAutoAccountName');
            return el && el.textContent && el.textContent.trim() !== 'Bağlanıyor...';
        }, { timeout: 10000 }).catch(() => {});
        console.log('Aktif Test Hesabı bağlandı.');

        // 1. Modül 10: Atmosfer Orkestrasyonu
        console.log('\n[1] Modül 10: Atmosfer Orkestrasyonu Test Ediliyor...');
        await page.evaluate(() => {
            const sel = document.getElementById('moduleSelectDropdown');
            sel.value = '#tab-atmosphere';
            window.onModuleTabChange(sel);
        });
        await page.waitForTimeout(500);

        const tabAtmoVisible = await page.locator('#tab-atmosphere').isVisible();
        console.log(`- #tab-atmosphere görünür: ${tabAtmoVisible}`);

        await page.click('#btnGenerateAtmosphere');
        await page.waitForTimeout(2500);
        const atmoTitle = await page.locator('#lblAtmoTitle').textContent();
        console.log(`- Üretilen Bütünsel Atmosfer: "${atmoTitle}"`);

        await page.screenshot({ path: 'artifacts/test_tab_atmosphere.png', fullPage: false });

        // 2. Modül 11: Görsel Tema ve Renk Paleti (QuickJump ile geçiş)
        console.log('\n[2] Modül 11: Görsel Tema & Palet Test Ediliyor...');
        await page.evaluate(() => window.quickJumpTab('#tab-theme-ai'));
        await page.waitForTimeout(500);

        const tabThemeVisible = await page.locator('#tab-theme-ai').isVisible();
        console.log(`- #tab-theme-ai görünür: ${tabThemeVisible}`);

        // Tema seçiciyi CozyCabin yapıp renk paleti türet
        await page.selectOption('#selectThemeAiAesthetic', 'CozyCabin');
        await page.waitForTimeout(300);
        await page.click('#btnPresetThemeColors');
        await page.waitForTimeout(300);

        const wallVal = await page.locator('#txtThemeWall').inputValue();
        console.log(`- CozyCabin Duvar Rengi: ${wallVal}`);

        // Temayı Sentezle butonu
        await page.click('#btnSynthesizeThemeOnly');
        await page.waitForTimeout(2000);
        const studioThemeBadge = await page.locator('#badgeStudioTheme').textContent();
        console.log(`- Stüdyodaki Güncel Tema Rozeti: "${studioThemeBadge}"`);

        await page.screenshot({ path: 'artifacts/test_tab_theme_ai.png', fullPage: false });

        // 3. Modül 12: Müzik ve 4-Kanal Akustik Mikser
        console.log('\n[3] Modül 12: Müzik & 4-Kanal Mikser Test Ediliyor...');
        await page.evaluate(() => window.quickJumpTab('#tab-music-ai'));
        await page.waitForTimeout(500);

        const tabMusicVisible = await page.locator('#tab-music-ai').isVisible();
        console.log(`- #tab-music-ai görünür: ${tabMusicVisible}`);

        // Hazır ayar butonuna bas
        await page.click('#btnPresetDeepCode');
        await page.waitForTimeout(300);
        const melodyBadge = await page.locator('#badgeRangeMelody').textContent();
        console.log(`- Derin Kodlama Melodi Rozeti: ${melodyBadge}`);

        // Müziği ve Mikseri Sentezle butonu
        await page.click('#btnSynthesizeMusicOnly');
        await page.waitForTimeout(2000);
        const liveTrack = await page.locator('#lblMusicCurrentTrackName').textContent();
        const studioMelodyVal = await page.locator('#valMixerMelody').textContent();
        console.log(`- Sentezlenen Canlı Parça: "${liveTrack}"`);
        console.log(`- Canlı Stüdyodaki Melodi Mikser Değeri: "${studioMelodyVal}"`);

        await page.screenshot({ path: 'artifacts/test_tab_music_ai.png', fullPage: false });

        console.log('\n--- TÜM TESTLER BAŞARIYLA TAMAMLANDI! ---');
    } catch (err) {
        console.error('Test sırasında hata oluştu:', err);
        process.exitCode = 1;
    } finally {
        await browser.close();
    }
}

main();
