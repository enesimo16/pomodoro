import { chromium } from './node_modules/playwright/index.mjs';

async function snap() {
  const browser = await chromium.launch({ channel: 'chrome', headless: true });
  const page = await browser.newPage({ viewport: { width: 1440, height: 900 } });
  
  // 1. Landing
  await page.goto('http://localhost:3000', { waitUntil: 'domcontentloaded', timeout: 30000 });
  await page.waitForTimeout(1000);
  await page.screenshot({ path: 'frontend_bokodoro_landing_3000.png' });

  // 2. Room
  await page.goto('http://localhost:3000/room?guest=Enes', { waitUntil: 'domcontentloaded', timeout: 30000 });
  await page.waitForTimeout(1000);
  await page.screenshot({ path: 'frontend_bokodoro_room_3000.png' });

  await browser.close();
  console.log('BOTH_SCREENSHOTS_SUCCESS');
}

snap().catch(console.error);
