import React, { useCallback, useEffect, useState } from "react";
import Grid from "@mui/material/Grid2";
import { PlaylistCard } from "./PlaylistCard";

import Container from "@mui/material/Container";
import { useApi } from "../api/ApiContext";
import { Playlist } from "../api/api-client";
import {
  Badge,
  Box,
  Button,
  Divider,
  IconButton,
  TextField,
  ToggleButton,
} from "@mui/material";
import { Add, Check, Delete } from "@mui/icons-material";
import { AddPlaylist } from "./AddPlaylist";
import { PlaylistDetail } from "./PlaylistDetail";

const Playlists = () => {
  const api = useApi();
  const { getPlaylists } = api;
  const [playlists, setPlaylists] = useState<Playlist[]>([]);
  const [selectedPlaylistIds, setSelectedPlaylistIds] = useState<number[]>([]);
  const [selectedPlaylistId, setSelectedPlaylistId] = useState<
    number | undefined
  >();
  const [selectionMode, setSelectionMode] = useState<boolean>(false);

  useEffect(() => {
    fetchPlaylists();
  }, []);

  const fetchPlaylists = async () => {
    const playlists = await getPlaylists();
    setPlaylists(playlists);
  };

  useEffect(() => {
    if (selectionMode) return;
    setSelectedPlaylistIds([]);
  }, [selectionMode]);

  const deletePlaylist = async (playlistId: number) => {
    console.log("deletePlaylist");
    await api.deletePlaylist(playlistId);
    let tmp = [...playlists];
    const item = tmp.filter((p) => p.id === playlistId)[0];
    const index = tmp.indexOf(item);
    tmp.splice(index, 1);
    setPlaylists(tmp);
  };
  const addPlaylist = async (url: string) => {
    console.log("addPlaylist");
    await api.addPlaylist(url);
    fetchPlaylists();
  };
  const onClick = (playlistId: number) => {
    if (selectionMode) {
      addSelectedPlaylistId(playlistId);
    } else {
      setSelectedPlaylistId(playlistId);
    }
  };

  const addSelectedPlaylistId = (playlistId: number) => {
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

  const deleteSelected = async () => {
    console.log("deleteSelected");
    await api.deletePlaylists(selectedPlaylistIds);
    setSelectedPlaylistIds([]);
    fetchPlaylists();
  };

  const repullPlaylist = (playlistId: number) => {
    api.syncFromOrigin(playlistId);
  };

  useEffect(() => {
    console.log("selectedPlaylistId", selectedPlaylistId);
  }, [selectedPlaylistId]);

  return (
    <Box>
      <Grid sx={{ p: 2 }}>
        <AddPlaylist addPlaylist={addPlaylist} />
      </Grid>
      <Grid container sx={{ p: 2 }}>
        <Grid>
          <Badge badgeContent={selectedPlaylistIds.length} color="primary">
            <ToggleButton
              color="primary"
              value="check"
              selected={selectionMode}
              onChange={() => {
                setSelectionMode(!selectionMode);
              }}
            >
              <Check />
            </ToggleButton>
          </Badge>
        </Grid>
        <Grid visibility={selectionMode ? "visible" : "hidden"}>
          <IconButton
            aria-label="delete"
            disabled={selectedPlaylistIds.length === 0}
            onClick={() => {
              deleteSelected();
            }}
          >
            <Delete />
          </IconButton>
        </Grid>
      </Grid>
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
                  onClick={onClick}
                  deletePlaylist={deletePlaylist}
                  repullPlaylist={repullPlaylist}
                />
              </Grid>
            );
          })}
        </Grid>
      </Box>
      {selectedPlaylistId && (
        <PlaylistDetail
          playlistId={selectedPlaylistId}
          handleClose={() => {
            setSelectedPlaylistId(undefined);
          }}
        />
      )}
    </Box>
  );
};

export default Playlists;
