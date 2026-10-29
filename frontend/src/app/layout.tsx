import type { Metadata } from "next";
import { Space_Mono, VT323 } from "next/font/google";
import "./globals.css";

const spaceMono = Space_Mono({
  subsets: ["latin", "latin-ext"],
  weight: ["400", "700"],
  style: ["normal", "italic"],
  variable: "--font-space-mono",
  display: "swap",
});

const vt323 = VT323({
  subsets: ["latin", "latin-ext"],
  weight: "400",
  variable: "--font-vt323",
  display: "swap",
});

export const metadata: Metadata = {
  title: "Bokodoro | Minimalist Retro Pixel Pomodoro & Deep Work",
  description: "Dikkat dağınıklığını yok eden retro piksel çalışma odası, 4 kanallı akustik ses kalkanı ve Habbo tarzı sessiz ortak çalışma alanı.",
};

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <html lang="tr" className={`dark h-full ${spaceMono.variable} ${vt323.variable}`}>
      <body className="min-h-full flex flex-col crt-scanlines selection:bg-[#38ef7d] selection:text-black">
        {children}
      </body>
    </html>
  );
}
