import Backdrop from "@mui/material/Backdrop";
import { useApi } from "../api/ApiContext";
import {
  Button,
  CircularProgress,
  Paper,
  Stack,
  Typography,
} from "@mui/material";
import { useEffect, useState } from "react";
import { Playlist, PlaylistPopulated } from "../api/api-client";

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
  const playlist = playlistDetail?.playlist;

  return (
    <Backdrop
      sx={(theme) => ({ color: "#fff", zIndex: theme.zIndex.drawer + 1 })}
      open
      onClick={handleClose}
    >
      <Paper square={false} sx={{ width: "60%", height: "80%" }}>
        {playlist && (
          <Stack direction={"column"}>
            <Typography sx={{ color: "text.secondary", fontSize: 14 }}>
              {playlist.channelName}
            </Typography>
            <Typography sx={{ fontSize: 20 }}>{playlist.name}</Typography>
          </Stack>
        )}
      </Paper>
    </Backdrop>
  );
};
