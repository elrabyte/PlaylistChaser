import React, { useCallback, useEffect, useState } from "react";
import Grid from "@mui/material/Grid2";
import { PlaylistCard } from "./PlaylistCard";

import Container from "@mui/material/Container";
import { useApi } from "./api/ApiContext";
import { Playlist } from "./api/api-client";
import { Box, Button, Divider, TextField } from "@mui/material";
import { Add } from "@mui/icons-material";
import { AddPlaylist } from "./AddPlaylist";

const Playlists = () => {
  const api = useApi();
  const { getPlaylists } = api;
  const [playlists, setPlaylists] = useState<Playlist[]>([]);
  const [selectedPlaylistIds, setSelectedPlaylistIds] = useState<number[]>([]);

  useEffect(() => {
    fetchPlaylists();
  }, []);

  const fetchPlaylists = useCallback(async () => {
    const playlists = await getPlaylists();
    setPlaylists(playlists);
  }, []);

  const deletePlaylist = (playlistId: number) => {
    console.log("deletePlaylist");
    api.deletePlaylist(playlistId);
    let tmp = [...playlists];
    const item = tmp.filter((p) => p.id === playlistId)[0];
    const index = tmp.indexOf(item);
    tmp.splice(index, 1);
    setPlaylists(tmp);
  };
  const addPlaylist = (url: string) => {
    console.log("addPlaylist");
    api.addPlaylist(url);
    fetchPlaylists();
  };

  const setSelectedPlaylistId = (playlistId: number) => {
    let ids = [...selectedPlaylistIds];
    const index = ids.indexOf(playlistId);
    const wasSelected = index > -1;

    if (wasSelected) {
      ids.splice(index, 1);
    } else {
      ids.push(playlistId);
    }
    setSelectedPlaylistIds(ids);
  };

  return (
    <Box>
      <Box sx={{ p: 2 }}>
        <AddPlaylist addPlaylist={addPlaylist} />
      </Box>
      <Divider />
      <Box sx={{ p: 2 }}>
        <Grid container spacing={2}>
          {playlists.map((playlist) => {
            const isSelected = selectedPlaylistIds.indexOf(playlist.id!) > -1;
            return (
              <Grid size={2}>
                <PlaylistCard
                  playlist={playlist}
                  isSelected={isSelected}
                  setIsSelected={setSelectedPlaylistId}
                  deletePlaylist={deletePlaylist}
                />
              </Grid>
            );
          })}
        </Grid>
      </Box>
    </Box>
  );
};

export default Playlists;
