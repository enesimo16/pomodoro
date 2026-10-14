import { chromium } from './node_modules/playwright/index.mjs';
import fs from 'fs';
import path from 'path';

const ADMIN_BASE = 'http://localhost:5001';
const ADMIN_KEY = 'focus_master_key_9971x';

async function testPolarisSidebar() {
    console.log('Testing Shopify Polaris Sidebar & Panels...');
    const browser = await chromium.launch({ channel: 'chrome', headless: true });
    const context = await browser.newContext({ viewport: { width: 1440, height: 900 } });
    const page = await context.newPage();

    const errors = [];
    page.on('console', msg => {
        if (msg.type() === 'error') {
            errors.push(msg.text());
        }
    });

    // 1. Visit Dashboard
    console.log('\n--- 1. Testing Dashboard & Initial Sidebar State ---');
    await page.goto(`${ADMIN_BASE}/?admin_key=${ADMIN_KEY}`, { waitUntil: 'networkidle' });

    // Take screenshot of Dashboard with Polaris theme
    await page.screenshot({ path: 'C:/Users/enes yel/.gemini/antigravity/brain/3b108ce6-7f66-47a7-9462-d7f31dd328c3/polaris_dashboard.png', fullPage: false });
    console.log('Saved polaris_dashboard.png');

    // Count folders
    const folders = await page.$$('.sidebar-folder');
    console.log(`Found ${folders.length} sidebar folders.`);
    if (folders.length < 4) {
        throw new Error(`Expected at least 4 folders, found ${folders.length}`);
    }

    // 2. Test Developer folder specifically
    console.log('\n--- 2. Testing Developer Folder Dropdown ---');
    const devFolder = await page.$('.sidebar-folder[data-folder-id="developer"]');
    if (!devFolder) throw new Error('Developer folder not found!');

    const devHeader = await devFolder.$('.sidebar-folder-header');
    const devTitle = await devHeader.innerText();
    console.log('Developer header text:', devTitle);

    // Click to collapse
    await devHeader.click();
    await page.waitForTimeout(300);
    let isCollapsed = await devFolder.evaluate(el => el.classList.contains('collapsed'));
    console.log('Developer folder collapsed after click:', isCollapsed);
    if (!isCollapsed) throw new Error('Developer folder did not collapse on click!');

    // Click to expand again
    await devHeader.click();
    await page.waitForTimeout(300);
    isCollapsed = await devFolder.evaluate(el => el.classList.contains('collapsed'));
    console.log('Developer folder collapsed after 2nd click:', isCollapsed);
    if (isCollapsed) throw new Error('Developer folder did not re-expand on click!');

    // 3. Test Navigation & Automatic Folder Expansion
    console.log('\n--- 3. Testing Navigation to /ApiDocs and Auto-Folder Expansion ---');
    await page.goto(`${ADMIN_BASE}/ApiDocs?admin_key=${ADMIN_KEY}`, { waitUntil: 'networkidle' });
    
    // Check if developer folder is expanded and has-active-child
    const devFolderOnApiDocs = await page.$('.sidebar-folder[data-folder-id="developer"]');
    const hasActiveClass = await devFolderOnApiDocs.evaluate(el => el.classList.contains('has-active-child'));
    const isDevCollapsed = await devFolderOnApiDocs.evaluate(el => el.classList.contains('collapsed'));
    console.log('On /ApiDocs - Developer folder has-active-child:', hasActiveClass);
    console.log('On /ApiDocs - Developer folder is collapsed:', isDevCollapsed);
    if (!hasActiveClass || isDevCollapsed) {
        throw new Error('Developer folder should be expanded and have has-active-child on /ApiDocs!');
    }

    await page.screenshot({ path: 'C:/Users/enes yel/.gemini/antigravity/brain/3b108ce6-7f66-47a7-9462-d7f31dd328c3/polaris_apidocs.png', fullPage: false });
    console.log('Saved polaris_apidocs.png');

    // 4. Test Navigation to /Shop and Economy Folder Expansion
    console.log('\n--- 4. Testing Navigation to /Shop and Economy Folder Expansion ---');
    await page.goto(`${ADMIN_BASE}/Shop?admin_key=${ADMIN_KEY}`, { waitUntil: 'networkidle' });
    const economyFolder = await page.$('.sidebar-folder[data-folder-id="economy"]');
    const economyActive = await economyFolder.evaluate(el => el.classList.contains('has-active-child'));
    console.log('On /Shop - Economy folder has-active-child:', economyActive);
    if (!economyActive) throw new Error('Economy folder should have has-active-child on /Shop!');

    await page.screenshot({ path: 'C:/Users/enes yel/.gemini/antigravity/brain/3b108ce6-7f66-47a7-9462-d7f31dd328c3/polaris_shop.png', fullPage: false });
    console.log('Saved polaris_shop.png');

    // 5. Test Navigation to /ApiLab
    console.log('\n--- 5. Testing ApiLab Page ---');
    await page.goto(`${ADMIN_BASE}/ApiLab?admin_key=${ADMIN_KEY}`, { waitUntil: 'networkidle' });
    await page.screenshot({ path: 'C:/Users/enes yel/.gemini/antigravity/brain/3b108ce6-7f66-47a7-9462-d7f31dd328c3/polaris_apilab.png', fullPage: false });
    console.log('Saved polaris_apilab.png');

    // 6. Test Mobile Responsiveness
    console.log('\n--- 6. Testing Mobile Drawer & Sidebar Navigation ---');
    await page.setViewportSize({ width: 390, height: 844 });
    await page.goto(`${ADMIN_BASE}/?admin_key=${ADMIN_KEY}`, { waitUntil: 'networkidle' });
    
    // Open sidebar
    const toggleBtn = await page.$('#sidebarToggleBtn');
    await toggleBtn.click();
    await page.waitForTimeout(400);

    await page.screenshot({ path: 'C:/Users/enes yel/.gemini/antigravity/brain/3b108ce6-7f66-47a7-9462-d7f31dd328c3/polaris_mobile_sidebar.png', fullPage: false });
    console.log('Saved polaris_mobile_sidebar.png');

    // Verify no icon tags or emojis in the rendered HTML
    console.log('\n--- 7. Verifying Zero Icons / Emojis Constraint ---');
    const svgCount = await page.$$eval('svg', els => els.length);
    const biCount = await page.$$eval('[class*="bi-"], [class*="fa-"]', els => els.length);
    console.log('Rendered SVG count:', svgCount);
    console.log('Rendered icon font classes:', biCount);
    if (svgCount > 0 || biCount > 0) {
        throw new Error(`Found disallowed icons! SVGs: ${svgCount}, Icon fonts: ${biCount}`);
    }

    await browser.close();
    console.log('\nAll tests PASSED! Polaris design and folder dropdowns successfully verified.');
}

testPolarisSidebar().catch(err => {
    console.error('Test Failed:', err);
    process.exit(1);
});
