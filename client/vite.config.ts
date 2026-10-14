import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import { VitePWA } from "vite-plugin-pwa";

export default defineConfig({
  plugins: [
    react(),
    VitePWA({
      registerType: "autoUpdate",
      includeAssets: ["favicon.svg", "apple-touch-icon.png"],
      manifest: {
        name: "PlaylistChaser",
        short_name: "PlaylistChaser",
        description: "Keep your YouTube and Spotify playlists in sync.",
        theme_color: "#1c7ed6",
        background_color: "#1a1b1e",
        display: "standalone",
        start_url: "/",
        icons: [
          { src: "icon-192.png", sizes: "192x192", type: "image/png" },
          { src: "icon-512.png", sizes: "512x512", type: "image/png" },
          { src: "icon-512.png", sizes: "512x512", type: "image/png", purpose: "maskable" },
        ],
        // Android: "Share" a playlist from the YouTube/Spotify app straight into PlaylistChaser
        share_target: { action: "/share", method: "GET", params: { title: "title", text: "text", url: "url" } },
      },
      workbox: {
        // API calls and the OAuth redirects must always reach the server, never the offline cache
        navigateFallbackDenylist: [/^\/api\//],
      },
    }),
  ],
  server: { proxy: { "/api": "http://localhost:5271" } },
});
