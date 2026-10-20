import { AppShell, Center, Group, Loader, NavLink, Text, UnstyledButton, Stack } from "@mantine/core";
import { useMediaQuery } from "@mantine/hooks";
import { IconClock, IconPlaylist, IconSettings } from "@tabler/icons-react";
import { useQuery } from "@tanstack/react-query";
import { Link, Route, Routes, useLocation } from "react-router";
import { api, ApiError } from "./api";
import { JobsIndicator } from "./components/JobsIndicator";
import { LoginPage } from "./pages/LoginPage";
import { PlaylistPage } from "./pages/PlaylistPage";
import { PlaylistsPage } from "./pages/PlaylistsPage";
import { SettingsPage } from "./pages/SettingsPage";
import { SharePage } from "./pages/SharePage";
import { TasksPage } from "./pages/TasksPage";

const NAV = [
  { to: "/", label: "Playlists", icon: IconPlaylist },
  { to: "/tasks", label: "Tasks", icon: IconClock },
  { to: "/settings", label: "Settings", icon: IconSettings },
];

export function App() {
  const me = useQuery({ queryKey: ["me"], queryFn: api.me });
  const isMobile = useMediaQuery("(max-width: 48em)");
  const { pathname } = useLocation();
  const active = (to: string) => (to === "/" ? pathname === "/" || pathname.startsWith("/playlists") : pathname.startsWith(to));

  if (me.isPending) return <Center h="100dvh"><Loader /></Center>;
  if (me.error instanceof ApiError && me.error.status === 401) return <LoginPage />;
  if (me.isError) return <Center h="100dvh"><Text c="red">Can't reach the server: {me.error.message}</Text></Center>;

  return (
    <AppShell
      header={{ height: 56 }}
      navbar={{ width: 220, breakpoint: "sm", collapsed: { mobile: true } }}
      footer={{ height: 64, collapsed: !isMobile }}
      padding="md"
    >
      <AppShell.Header>
        <Group h="100%" px="md" justify="space-between">
          <Text fw={700} size="lg" component={Link} to="/">PlaylistChaser</Text>
          <JobsIndicator />
        </Group>
      </AppShell.Header>

      <AppShell.Navbar p="sm">
        {NAV.map((n) => (
          <NavLink key={n.to} component={Link} to={n.to} label={n.label} leftSection={<n.icon size={18} />} active={active(n.to)} />
        ))}
      </AppShell.Navbar>

      <AppShell.Main>
        <Routes>
          <Route path="/" element={<PlaylistsPage />} />
          <Route path="/playlists/:id" element={<PlaylistPage />} />
          <Route path="/tasks" element={<TasksPage />} />
          <Route path="/settings" element={<SettingsPage me={me.data} />} />
          <Route path="/share" element={<SharePage />} />
          <Route path="*" element={<PlaylistsPage />} />
        </Routes>
      </AppShell.Main>

      <AppShell.Footer style={{ paddingBottom: "env(safe-area-inset-bottom)" }}>
        <Group grow h="100%" gap={0}>
          {NAV.map((n) => (
            <UnstyledButton key={n.to} component={Link} to={n.to} h="100%" c={active(n.to) ? "blue" : "dimmed"}>
              <Stack align="center" gap={2}>
                <n.icon size={22} />
                <Text size="xs">{n.label}</Text>
              </Stack>
            </UnstyledButton>
          ))}
        </Group>
      </AppShell.Footer>
    </AppShell>
  );
}
