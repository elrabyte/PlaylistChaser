import React, { useEffect, useState } from "react";
import Grid from "@mui/material/Grid2";
import { PlaylistCard } from "./PlaylistCard";

import Container from "@mui/material/Container";
import { useApi } from "./api/ApiContext";
import { Playlist } from "./api/api-client";
import { Box, Button, TextField } from "@mui/material";
import { Add } from "@mui/icons-material";
import Playlists from "./Playlists";

type AddPlaylistProps = {
  addPlaylist: (url: string) => void;
};
export const AddPlaylist = ({ addPlaylist }: AddPlaylistProps) => {
  const [url, setUrl] = useState<string>("");
  const [disabled, setDisabled] = useState<boolean>(true);

  useEffect(() => {
    if (!url || url.length === 0) {
      setDisabled(true);
      return;
    }
    setDisabled(false);
  }, [url]);

  const submit = () => {
    setUrl("");
    addPlaylist(url);
  };

  return (
    <Grid container spacing={2}>
      <TextField
        label="YT-Playlist Url"
        type="url"
        value={url}
        onChange={(e) => {
          setUrl(e.target.value);
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
