import { Alert, Badge, Button, Card, Checkbox, Group, PasswordInput, Stack, Text, TextInput, Title } from "@mantine/core";
import { notifications } from "@mantine/notifications";
import { IconLogout, IconTrash } from "@tabler/icons-react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useEffect, useState } from "react";
import { useNavigate, useSearchParams } from "react-router";
import { api, type Me, type SourceAccount } from "../api";
import { useProviders } from "../useProviders";
import { SourceIcon } from "../components/SourceBadge";

export function SettingsPage({ me }: { me: Me }) {
  const [params] = useSearchParams();
  const navigate = useNavigate();
  const providers = useProviders();

  // result of the YouTube/Spotify login redirect
  useEffect(() => {
    const error = params.get("error");
    const connected = params.get("connected");
    if (error) notifications.show({ color: "red", title: "Not connected", message: error, autoClose: false });
    if (connected) notifications.show({ color: "green", message: `${providers.label(connected)} connected.` });
    if (error || connected) navigate("/settings", { replace: true });
  }, [params, navigate, providers]);

  return (
    <Stack maw={640}>
      <Title order={2}>Settings</Title>

      <Card withBorder>
        <Stack>
          <Title order={4}>Music services</Title>
          {providers.list.map((a) => <AccountRow key={a.source} account={a} />)}
        </Stack>
      </Card>

      <PasswordCard />
      {me.isAdmin && <UsersCard me={me} />}

      <Group justify="space-between">
        <Text c="dimmed" size="sm">Logged in as {me.userName}</Text>
        <Button variant="default" leftSection={<IconLogout size={18} />}
          onClick={async () => { await api.logout(); window.location.href = "/"; }}>
          Log out
        </Button>
      </Group>
    </Stack>
  );
}

function AccountRow({ account: a }: { account: SourceAccount }) {
  const queryClient = useQueryClient();
  const disconnect = useMutation({
    mutationFn: () => api.disconnect(a.source),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["sources"] }),
  });
  const connect = () => { window.location.href = api.connectUrl(a.source); };

  return (
    <Group justify="space-between" wrap="nowrap">
      <Group gap="sm" wrap="nowrap" style={{ minWidth: 0 }}>
        <SourceIcon source={a.source} size={24} />
        <Stack gap={0} style={{ minWidth: 0 }}>
          <Text fw={500}>{a.name}</Text>
          <Text size="sm" c="dimmed" truncate>
            {!a.configured ? "Not set up on this server (see README)" : a.connected ? a.displayName ?? "Connected" : "Not connected"}
          </Text>
        </Stack>
      </Group>
      {a.configured && (
        a.needsReconnect ? <Button color="orange" onClick={connect}>Reconnect</Button>
        : a.connected ? <Button variant="default" onClick={() => disconnect.mutate()} loading={disconnect.isPending}>Disconnect</Button>
        : <Button onClick={connect}>Connect</Button>
      )}
    </Group>
  );
}

function PasswordCard() {
  const [current, setCurrent] = useState("");
  const [next, setNext] = useState("");
  const change = useMutation({
    mutationFn: () => api.changePassword(current, next),
    onSuccess: () => { setCurrent(""); setNext(""); notifications.show({ color: "green", message: "Password changed." }); },
  });
  return (
    <Card withBorder>
      <form onSubmit={(e) => { e.preventDefault(); change.mutate(); }}>
        <Stack>
          <Title order={4}>Change password</Title>
          <PasswordInput label="Current password" value={current} onChange={(e) => setCurrent(e.currentTarget.value)} autoComplete="current-password" required />
          <PasswordInput label="New password" description="At least 10 characters." value={next} onChange={(e) => setNext(e.currentTarget.value)} autoComplete="new-password" minLength={10} required />
          <Button type="submit" loading={change.isPending} style={{ alignSelf: "start" }}>Change password</Button>
        </Stack>
      </form>
    </Card>
  );
}

function UsersCard({ me }: { me: Me }) {
  const queryClient = useQueryClient();
  const users = useQuery({ queryKey: ["users"], queryFn: api.users });
  const [userName, setUserName] = useState("");
  const [password, setPassword] = useState("");
  const [isAdmin, setIsAdmin] = useState(false);
  const refresh = () => queryClient.invalidateQueries({ queryKey: ["users"] });
  const add = useMutation({
    mutationFn: () => api.addUser(userName.trim(), password, isAdmin),
    onSuccess: () => { setUserName(""); setPassword(""); setIsAdmin(false); refresh(); },
    meta: { silent: true },
  });
  const remove = useMutation({ mutationFn: api.deleteUser, onSuccess: refresh });

  return (
    <Card withBorder>
      <Stack>
        <Title order={4}>Users</Title>
        {users.data?.map((u) => (
          <Group key={u.id} justify="space-between">
            <Group gap="xs"><Text>{u.userName}</Text>{u.isAdmin && <Badge variant="light">admin</Badge>}</Group>
            {u.userName !== me.userName && (
              <Button variant="subtle" color="red" size="xs" leftSection={<IconTrash size={14} />}
                onClick={() => window.confirm(`Delete ${u.userName} and all their playlists?`) && remove.mutate(u.id)}>
                Delete
              </Button>
            )}
          </Group>
        ))}
        <form onSubmit={(e) => { e.preventDefault(); add.mutate(); }}>
          <Stack gap="xs">
            <Text fw={500} size="sm">Add a user</Text>
            <Group grow align="start">
              <TextInput placeholder="Username" aria-label="Username" value={userName} onChange={(e) => setUserName(e.currentTarget.value)} required />
              <PasswordInput placeholder="Password (10+ characters)" aria-label="Password" value={password} onChange={(e) => setPassword(e.currentTarget.value)} autoComplete="new-password" minLength={10} required />
            </Group>
            <Group justify="space-between">
              <Checkbox label="Admin" checked={isAdmin} onChange={(e) => setIsAdmin(e.currentTarget.checked)} />
              <Button type="submit" loading={add.isPending}>Add user</Button>
            </Group>
            {add.error && <Alert color="red">{add.error.message}</Alert>}
          </Stack>
        </form>
      </Stack>
    </Card>
  );
}
