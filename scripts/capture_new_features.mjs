import { chromium } from './node_modules/playwright/index.mjs';
import path from 'path';

const artifactsDir = 'C:/Users/enes yel/.gemini/antigravity/brain/3b108ce6-7f66-47a7-9462-d7f31dd328c3';

(async () => {
    const browser = await chromium.launch({ channel: 'chrome', headless: true });
    const context = await browser.newContext({ viewport: { width: 1440, height: 900 } });
    const page = await context.newPage();

    console.log('Navigating to root to authenticate...');
    await page.goto('http://localhost:5001/?admin_key=focus_master_key_9971x', { waitUntil: 'domcontentloaded' });
    await page.waitForTimeout(600);

    // 1. Ekran Görüntüsü: Profesyonel Sol Panel & Animasyonlu İkonlar (Zero Text)
    console.log('Capturing sidebar...');
    await page.screenshot({
        path: path.join(artifactsDir, 'polaris_sidebar_professional.png'),
        fullPage: false
    });
    console.log('Saved polaris_sidebar_professional.png');

    // 2. Ekran Görüntüsü: Görsel Temalar & Video Kütüphanesi (Kategori Sekmeleri & Canlı Sayfalama)
    console.log('Navigating to Themes...');
    await page.goto('http://localhost:5001/Themes?admin_key=focus_master_key_9971x', { waitUntil: 'domcontentloaded' });
    await page.waitForTimeout(600);
    await page.screenshot({
        path: path.join(artifactsDir, 'polaris_themes_paginated.png'),
        fullPage: false
    });
    console.log('Saved polaris_themes_paginated.png');

    // 3. Ekran Görüntüsü: Akustik Ses Kütüphanesi (Tür Sekmeleri & Sayfalama)
    console.log('Navigating to Music...');
    await page.goto('http://localhost:5001/Music?admin_key=focus_master_key_9971x', { waitUntil: 'domcontentloaded' });
    await page.waitForTimeout(600);
    await page.screenshot({
        path: path.join(artifactsDir, 'polaris_music_paginated.png'),
        fullPage: false
    });
    console.log('Saved polaris_music_paginated.png');

    await browser.close();
    console.log('All verification screenshots captured successfully!');
})();
