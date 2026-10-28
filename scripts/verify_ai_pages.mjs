import { chromium } from './node_modules/playwright/index.mjs';
import { requireAdminKey, screenshotPath } from './config.mjs';

async function verifyAiPages() {
    console.log('Testing newly created AI pages...');
    const browser = await chromium.launch({ channel: 'chrome', headless: true });
    const page = await browser.newPage({ viewport: { width: 1440, height: 900 } });
    const ADMIN_KEY = requireAdminKey();

    const urls = [
        { name: 'AI Hub Overview', path: '/AiCoach' },
        { name: 'Kişisel AI Bilgileri', path: '/AiCoach/Personal' },
        { name: 'Kolektif Global AI', path: '/AiCoach/Global' },
        { name: 'Atmosfer Sentezi', path: '/AiCoach/Atmosphere' },
        { name: 'Cron Görevleri (Geliştirici)', path: '/Jobs' }
    ];

    for (const u of urls) {
        const resp = await page.goto('http://localhost:5001' + u.path + '?admin_key=' + ADMIN_KEY, { waitUntil: 'domcontentloaded' });
        console.log('[PAGE]', u.name, '-> HTTP', resp.status(), 'Title:', await page.title());
        if (resp.status() !== 200) {
            throw new Error(`Page ${u.name} failed with status ${resp.status()}`);
        }
    }

    // Take screenshot of Personal AI
    await page.goto('http://localhost:5001/AiCoach/Personal?admin_key=' + ADMIN_KEY, { waitUntil: 'domcontentloaded' });
    await page.screenshot({ path: screenshotPath('polaris_ai_personal.png') });
    console.log('Saved polaris_ai_personal.png');

    // Take screenshot of Global AI
    await page.goto('http://localhost:5001/AiCoach/Global?admin_key=' + ADMIN_KEY, { waitUntil: 'domcontentloaded' });
    await page.screenshot({ path: screenshotPath('polaris_ai_global.png') });
    console.log('Saved polaris_ai_global.png');

    // Take screenshot of Atmosphere
    await page.goto('http://localhost:5001/AiCoach/Atmosphere?admin_key=' + ADMIN_KEY, { waitUntil: 'domcontentloaded' });
    await page.screenshot({ path: screenshotPath('polaris_ai_atmosphere.png') });
    console.log('Saved polaris_ai_atmosphere.png');

    // Zero icons check
    const svgCount = await page.$$eval('svg', els => els.length);
    const biCount = await page.$$eval('[class*="bi-"], [class*="fa-"]', els => els.length);
    console.log('Zero Icons check - SVG count:', svgCount, 'Icon fonts:', biCount);
    if (svgCount > 0 || biCount > 0) {
        throw new Error('Found disallowed icons!');
    }

    await browser.close();
    console.log('\nAll AI pages verified successfully!');
}

verifyAiPages().catch(err => {
    console.error('Test Failed:', err);
    process.exit(1);
});
