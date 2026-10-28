import { chromium } from './node_modules/playwright/index.mjs';
import { requireAdminKey, screenshotPath } from './config.mjs';

const ADMIN_KEY = requireAdminKey();

(async () => {
    const browser = await chromium.launch({ channel: 'chrome', headless: true });
    const context = await browser.newContext({ viewport: { width: 1440, height: 900 } });
    const page = await context.newPage();

    console.log('Navigating to root to authenticate...');
    await page.goto('http://localhost:5001/?admin_key=' + ADMIN_KEY, { waitUntil: 'domcontentloaded' });
    await page.waitForTimeout(500);

    console.log('Navigating to ApiLab...');
    await page.goto('http://localhost:5001/ApiLab?admin_key=' + ADMIN_KEY, { waitUntil: 'domcontentloaded' });
    await page.waitForTimeout(1000);

    // Otomatik test hesabının bağlanmasını bekle
    await page.waitForFunction(() => {
        const el = document.getElementById('lblAutoAccountName');
        return el && el.textContent && el.textContent.trim() !== 'Bağlanıyor...' && el.textContent.trim() !== '';
    }, { timeout: 10000 }).catch(() => {});

    // Profil getir ve token yenile butonlarına tıkla (renkli log üretmek için)
    await page.click('#btnGetMe');
    await page.waitForTimeout(600);
    await page.click('#btnRefreshToken');
    await page.waitForTimeout(600);

    // 1. Ekran Görüntüsü: Sol panel beyaz yazılar ve Gizle butonu (Açıkken)
    await page.screenshot({
        path: screenshotPath('polaris_sidebar_whiter_text.png'),
        fullPage: false
    });
    console.log('Saved polaris_sidebar_whiter_text.png');

    // 2. Ekran Görüntüsü: Okunabilir Canlı İstek Terminali
    const terminalElement = await page.$('#terminalLog');
    if (terminalElement) {
        await terminalElement.scrollIntoViewIfNeeded();
        await page.screenshot({
            path: screenshotPath('polaris_terminal_readable.png'),
            fullPage: false
        });
        console.log('Saved polaris_terminal_readable.png');
    }

    // 3. Ekran Görüntüsü: Gizle butonuna tıkla -> Sidebar Daraltılsın
    console.log('Clicking #sidebarCloseBtn to collapse sidebar...');
    await page.click('#sidebarCloseBtn');
    await page.waitForTimeout(400);

    await page.screenshot({
        path: screenshotPath('polaris_sidebar_collapsed.png'),
        fullPage: false
    });
    console.log('Saved polaris_sidebar_collapsed.png');

    // 4. Ekran Görüntüsü: Üst bardaki Menü butonuna tıkla -> Sidebar Tekrar Açılsın
    console.log('Clicking #sidebarToggleBtn to restore sidebar...');
    await page.click('#sidebarToggleBtn');
    await page.waitForTimeout(400);

    await page.screenshot({
        path: screenshotPath('polaris_sidebar_restored.png'),
        fullPage: false
    });
    console.log('Saved polaris_sidebar_restored.png');

    await browser.close();
    console.log('All screenshots captured successfully!');
})();
