import { chromium } from './node_modules/playwright/index.mjs';

async function snap() {
  const browser = await chromium.launch({ channel: 'chrome', headless: true });
  const page = await browser.newPage({ viewport: { width: 1440, height: 900 } });
  await page.goto('http://localhost:3002', { waitUntil: 'domcontentloaded', timeout: 10000 });
  await page.waitForTimeout(1500);
  await page.screenshot({ path: 'frontend_bokodoro_landing.png' });
  await browser.close();
  console.log('SCREENSHOT_SUCCESS');
}

snap().catch(console.error);
