import { LockOpen, Lock, LockClock } from "@mui/icons-material";
import { useApi } from "../api/ApiContext";
import {
  Box,
  Button,
  Divider,
  IconButton,
  Paper,
  Stack,
  SvgIcon,
  TextField,
  ToggleButton,
} from "@mui/material";
import { useEffect, useState } from "react";
import { get } from "https";

export const Account = () => {
  const api = useApi();
  const [hasAccessToken, setHasAccessToken] = useState<boolean>();
  const [accessTokenExpired, setAccessTokenExpired] = useState<boolean>();

  useEffect(() => {
    checkHasAccesstoken();
  }, []);

  const checkHasAccesstoken = async () => {
    const hasAccessToken = await api.checkHasAccesstoken();
    setHasAccessToken(hasAccessToken);
  };
  useEffect(() => {
    if (hasAccessToken && !accessTokenExpired) {
      checkAccesstokenExpired();
    } else if (hasAccessToken && accessTokenExpired) {
      getToken();
    }
  }, [hasAccessToken]);
  const getToken = async () => {
    const returnUrl = await api.getLoginUrl();
    console.log("returnUrl", returnUrl);
    window.location.assign(returnUrl);
  };

  const checkAccesstokenExpired = async () => {
    const accessTokenExpired = await api.checkAccesstokenExpired();
    setAccessTokenExpired(accessTokenExpired);
  };
  useEffect(() => {
    if (accessTokenExpired) {
      refreshToken();
    }
  }, [accessTokenExpired]);
  const refreshToken = async () => {
    await api.refreshAccesstoken();
    setAccessTokenExpired(false);
  };

  const isAuthenticated = () => {
    return hasAccessToken && accessTokenExpired == false;
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
            {hasAccessToken == false && (
              <Button
                variant="outlined"
                startIcon={<Lock />}
                onClick={() => {
                  getToken();
                }}
              >
                Login to Spotify
              </Button>
            )}
            {hasAccessToken && accessTokenExpired && (
              <Button
                variant="outlined"
                startIcon={<Lock />}
                onClick={() => {
                  refreshToken();
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
