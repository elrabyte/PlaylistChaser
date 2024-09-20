import React, { useEffect, useState } from "react";
import Grid from "@mui/material/Grid2";
import { PlaylistCard } from "./PlaylistCard";

import Container from "@mui/material/Container";
import { useApi } from "./api/ApiContext";
import { Playlist } from "./api/api-client";

const Playlists = () => {
  const { getPlaylists } = useApi();
  const [playlists, setPlaylists] = useState<Playlist[]>([]);
  const [selectedPlaylistIds, setSelectedPlaylistIds] = useState<number[]>([]);

  useEffect(() => {
    getPlaylists().then((playlists) => {
      setPlaylists(playlists);
    });
  }, []);

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
    <Container>
      <Grid container spacing={2}>
        {playlists.map((playlist) => {
          const isSelected = selectedPlaylistIds.indexOf(playlist.id!) > -1;
          return (
            <Grid size={2}>
              <PlaylistCard
                playlist={playlist}
                isSelected={isSelected}
                setIsSelected={setSelectedPlaylistId}
              />
            </Grid>
          );
        })}
      </Grid>
    </Container>
  );
};

export default Playlists;
