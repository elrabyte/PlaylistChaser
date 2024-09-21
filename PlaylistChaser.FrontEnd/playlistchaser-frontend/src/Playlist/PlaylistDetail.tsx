import Backdrop from "@mui/material/Backdrop";
import { useApi } from "../api/ApiContext";
import {
  Button,
  CircularProgress,
  Divider,
  Paper,
  Stack,
  Typography,
} from "@mui/material";
import { useEffect, useState } from "react";
import { Playlist, PlaylistPopulated } from "../api/api-client";
import { SongGrid } from "../Song/SongGrid";

type PlaylistDetailProps = {
  playlistId: number;
  handleClose: () => void;
};

export const PlaylistDetail = ({
  playlistId,
  handleClose,
}: PlaylistDetailProps) => {
  const api = useApi();
  console.log("PlaylistPopulated");
  const [playlistDetail, setPlaylistDetail] = useState<PlaylistPopulated>();

  useEffect(() => {
    api.getPlaylistPopulated(playlistId).then((playlistDetail) => {
      setPlaylistDetail(playlistDetail);
    });
  }, []);

  const { playlist, songs } = playlistDetail ?? {};

  return (
    <Backdrop
      sx={(theme) => ({ color: "#fff", zIndex: theme.zIndex.drawer + 1 })}
      open
      onClick={handleClose}
    >
      <Paper
        square={false}
        sx={{ width: "60%", height: "80%" }}
        onClick={(e) => {
          e.preventDefault();
          e.stopPropagation();
        }}
      >
        {playlist && (
          <>
            <Stack direction={"column"}>
              <Typography sx={{ color: "text.secondary", fontSize: 14 }}>
                {playlist.channelName}
              </Typography>
              <Typography sx={{ fontSize: 20 }}>{playlist.name}</Typography>
            </Stack>
            <Divider />
            {songs && <SongGrid songs={songs} />}
          </>
        )}
      </Paper>
    </Backdrop>
  );
};
