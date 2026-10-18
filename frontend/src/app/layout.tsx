import type { Metadata } from "next";
import "./globals.css";

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
    <html lang="tr" className="dark h-full">
      <body className="min-h-full flex flex-col crt-scanlines selection:bg-[#38ef7d] selection:text-black">
        {children}
      </body>
    </html>
  );
}
