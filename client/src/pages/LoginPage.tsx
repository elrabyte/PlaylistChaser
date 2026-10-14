import { Alert, Button, Center, Paper, PasswordInput, Stack, TextInput, Title } from "@mantine/core";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { api } from "../api";

export function LoginPage() {
  const queryClient = useQueryClient();
  const [userName, setUserName] = useState("");
  const [password, setPassword] = useState("");
  const login = useMutation({
    mutationFn: () => api.login(userName, password),
    onSuccess: (me) => queryClient.setQueryData(["me"], me),
    meta: { silent: true },
  });

  return (
    <Center h="100dvh" p="md">
      <Paper withBorder p="xl" w={360} maw="100%">
        <form onSubmit={(e) => { e.preventDefault(); login.mutate(); }}>
          <Stack>
            <Title order={2}>PlaylistChaser</Title>
            {login.error && <Alert color="red">{login.error.message}</Alert>}
            <TextInput label="Username" value={userName} onChange={(e) => setUserName(e.currentTarget.value)} autoComplete="username" required autoFocus />
            <PasswordInput label="Password" value={password} onChange={(e) => setPassword(e.currentTarget.value)} autoComplete="current-password" required />
            <Button type="submit" loading={login.isPending}>Log in</Button>
          </Stack>
        </form>
      </Paper>
    </Center>
  );
}
