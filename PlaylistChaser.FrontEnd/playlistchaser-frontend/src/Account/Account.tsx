import { LockOpen, Lock } from "@mui/icons-material";
import { Box, Button, Paper, Stack, SvgIcon } from "@mui/material";
import { useAccount } from "../api/AccountContext";
import { SourceId } from "../api/api-client";
import { useEffect, useState } from "react";

export const Account = () => {
  const accountContext = useAccount();

  const [isAuthenticated, setIsAuthenticated] = useState<
    Record<SourceId, boolean>
  >({ Spotify: false, Youtube: false });

  useEffect(() => {
    Object.values(SourceId).map((sourceId) => {
      accountContext.isAuthenticated(sourceId).then((isAuthenticated) => {
        setIsAuthenticated((prev) => ({
          ...prev,
          [sourceId]: isAuthenticated,
        }));
      });
    });
  }, []);

  return (
    <Box>
      <Stack spacing={2} direction={"column"}>
        {Object.values(SourceId).map((sourceId) => {
          return (
            <Paper>
              <Stack direction={"row"}>
                <SvgIcon titleAccess="Spotify Icon" />

                {isAuthenticated[sourceId] && (
                  <Button variant="outlined" disabled startIcon={<LockOpen />}>
                    Authenticated
                  </Button>
                )}
                {!isAuthenticated[sourceId] && (
                  <Button
                    variant="outlined"
                    startIcon={<Lock />}
                    onClick={() => {
                      accountContext.getToken(sourceId);
                    }}
                  >
                    Login to {sourceId}
                  </Button>
                )}
                {isAuthenticated[sourceId] && (
                  <Button
                    variant="outlined"
                    startIcon={<Lock />}
                    onClick={() => {
                      accountContext.refreshToken(sourceId);
                    }}
                  >
                    Refresh Token
                  </Button>
                )}
              </Stack>
            </Paper>
          );
        })}
      </Stack>
    </Box>
  );
};
