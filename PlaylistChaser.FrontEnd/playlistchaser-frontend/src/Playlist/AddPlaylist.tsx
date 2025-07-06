import React, { useEffect, useState } from "react";
import { useApi } from "../api/ApiContext";
import { Box, Button, TextField } from "@mui/material";
import { Add } from "@mui/icons-material";
import { useAccount } from "../api/AccountContext";
import { SourceId } from "../api/api-client";

type AddPlaylistProps = {
  sourceId: SourceId;
  addPlaylist: (sourceId: SourceId, url: string) => void;
};
export const AddPlaylist = ({ sourceId, addPlaylist }: AddPlaylistProps) => {
  const api = useApi();
  const accountContext = useAccount();
  const [url, setUrl] = useState<string>("");
  const [isValid, setIsValid] = useState<boolean | undefined>();
  const [disabled, setDisabled] = useState<boolean>(false);
  const [authenticated, setAuthenticated] = useState<boolean>(false);

  const errorMessage = "not a valid url";
  const helperMessage =
    "only following urls: https://open.spotify.com/playlist/1FG7wsm7OaAKar8Ojn8wNo";

  const validateUrl = () => {
    if (!url || url.length === 0) {
      setIsValid(undefined);
      return;
    }
    api.validatePlaylistUrl(sourceId, url).then((isValid) => {
      setIsValid(isValid);
    });
  };

  useEffect(() => {
    if (!isValid) return;
    setDisabled(false);
  }, [isValid]);

  useEffect(() => {
    console.log("url", url);
    setDisabled(true);
    setIsValid(undefined);
    validateUrl();
  }, [url]);

  const submit = () => {
    setUrl("");
    addPlaylist(sourceId, url);
  };

  useEffect(() => {}, []);

  return (
    <Box sx={{ display: "flex", alignItems: "start", columnGap: "10px" }}>
      {/* {accountContext.isAuthenticated(sourceId).then((authenticated) => (
        <></>
      ))} */}

      {authenticated ? (
        <>
          <TextField
            fullWidth
            error={isValid === false}
            label="Spotify Playlist-Url"
            type="url"
            helperText={isValid == false ? errorMessage : helperMessage}
            value={url}
            onInput={(e) => {
              setUrl(
                (e.target as HTMLInputElement | HTMLTextAreaElement).value
              );
            }}
            onChange={(e) => {
              setUrl(e.target.value);
            }}
            onBlur={() => {
              validateUrl();
            }}
          />
          <Button
            size="small"
            disabled={disabled}
            variant="contained"
            endIcon={<Add />}
            onClick={submit}
          >
            Add Playlist
          </Button>
        </>
      ) : (
        <></>
      )}
    </Box>
  );
};
