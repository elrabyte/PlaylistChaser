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
  const [isAuthenticated, setIsAuthenticated] = useState<boolean>(false);

  useEffect(() => {
    checkAuthenticated();
  }, []);

  const checkAuthenticated = async () => {
    const isAuthenticated = await api.checkAuthenticated();
    setIsAuthenticated(isAuthenticated);
  };

  const authenticate = async () => {
    const returnUrl = await api.getLoginUrl();
    console.log("returnUrl", returnUrl);
    window.location.assign(returnUrl);
  };

  return (
    <Box>
      <Stack spacing={2} direction={"column"}>
        <Paper>
          <Stack direction={"row"}>
            <SvgIcon titleAccess="Spotify Icon" />

            {isAuthenticated ? (
              <Button variant="outlined" disabled startIcon={<LockOpen />}>
                Authenticated
              </Button>
            ) : (
              <Button
                variant="outlined"
                startIcon={<Lock />}
                onClick={() => {
                  authenticate();
                }}
              >
                Authenticate
              </Button>
            )}
          </Stack>
        </Paper>
      </Stack>
    </Box>
  );
};
