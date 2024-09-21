import React, { useEffect, useState } from "react";
import Grid from "@mui/material/Grid2";
import { PlaylistCard } from "./PlaylistCard";

import Container from "@mui/material/Container";
import { useApi } from "../api/ApiContext";
import { Playlist } from "../api/api-client";
import { Box, Button, TextField } from "@mui/material";
import { Add } from "@mui/icons-material";
import Playlists from "./Playlists";

type AddPlaylistProps = {
  addPlaylist: (url: string) => void;
};
export const AddPlaylist = ({ addPlaylist }: AddPlaylistProps) => {
  const api = useApi();
  const [url, setUrl] = useState<string>("");
  const [isValid, setIsValid] = useState<boolean | undefined>();
  const [disabled, setDisabled] = useState<boolean>(false);

  const errorMessage = "not a valid url";
  const helperMessage =
    "only following urls: https://open.spotify.com/playlist/1FG7wsm7OaAKar8Ojn8wNo";

  const validateUrl = () => {
    if (!url || url.length === 0) {
      setIsValid(undefined);
      return;
    }
    api.validatePlaylistUrl(url).then((isValid) => {
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
    addPlaylist(url);
  };

  return (
    <Grid container spacing={2}>
      <TextField
        fullWidth
        error={isValid === false}
        label="Spotify Playlist-Url"
        type="url"
        helperText={isValid == false ? errorMessage : helperMessage}
        value={url}
        onInput={(e) => {
          setUrl((e.target as HTMLInputElement | HTMLTextAreaElement).value);
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
    </Grid>
  );
};
