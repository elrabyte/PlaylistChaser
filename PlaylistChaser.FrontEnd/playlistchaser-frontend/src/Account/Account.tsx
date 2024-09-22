import { LockOpen, Lock } from "@mui/icons-material";
import { Box, Button, Paper, Stack, SvgIcon } from "@mui/material";
import { useAccount } from "../api/AccountContext";

export const Account = () => {
  const accountContext = useAccount();

  const isAuthenticated = () => {
    return (
      accountContext.hasAccessToken &&
      accountContext.accessTokenExpired == false
    );
  };

  return (
    <Box>
      <Stack spacing={2} direction={"column"}>
        <Paper>
          <Stack direction={"row"}>
            <SvgIcon titleAccess="Spotify Icon" />

            {isAuthenticated() && (
              <Button variant="outlined" disabled startIcon={<LockOpen />}>
                Authenticated
              </Button>
            )}
            {accountContext.hasAccessToken == false && (
              <Button
                variant="outlined"
                startIcon={<Lock />}
                onClick={() => {
                  accountContext.getToken();
                }}
              >
                Login to Spotify
              </Button>
            )}
            {accountContext.hasAccessToken &&
              accountContext.accessTokenExpired && (
                <Button
                  variant="outlined"
                  startIcon={<Lock />}
                  onClick={() => {
                    accountContext.refreshToken();
                  }}
                >
                  Refresh Token
                </Button>
              )}
          </Stack>
        </Paper>
      </Stack>
    </Box>
  );
};
