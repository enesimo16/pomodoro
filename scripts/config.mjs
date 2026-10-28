import fs from 'fs';
import path from 'path';

export function requireAdminKey() {
    const key = process.env.ADMIN_KEY;
    if (!key) {
        console.error('ADMIN_KEY ortam degiskeni tanimli degil. Ornek: ADMIN_KEY=<anahtar> node <betik>.mjs');
        process.exit(1);
    }
    return key;
}

export const SCREENSHOT_DIR = process.env.SCREENSHOT_DIR || './artifacts';

export function screenshotPath(fileName) {
    fs.mkdirSync(SCREENSHOT_DIR, { recursive: true });
    return path.join(SCREENSHOT_DIR, fileName);
}
