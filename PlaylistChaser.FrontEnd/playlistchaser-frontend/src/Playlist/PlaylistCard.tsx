import Card from "@mui/material/Card";
import CardContent from "@mui/material/CardContent";
import CardMedia from "@mui/material/CardMedia";
import Typography from "@mui/material/Typography";
import { Playlist, PlaylistTypes } from "../api/api-client";
import { SxProps, Theme } from "@mui/material/styles";
import CardActionArea from "@mui/material/CardActionArea";
import IconButton from "@mui/material/IconButton";

import { Delete, Height, MoreVert, Sync } from "@mui/icons-material";

import CardHeader from "@mui/material/CardHeader";
import { Box, Button, Fade, Menu, MenuItem, Paper, Stack } from "@mui/material";
import { useRef, useState } from "react";
import { useApi } from "../api/ApiContext";
import { ConfirmDialog } from "../components/ConfirmDialog";

type PlaylistCardProps = {
  playlist: Playlist;
  isSelected: boolean;
  onClick: (playlistId: number) => void;
  deletePlaylist: (playlistId: number) => void;
  repullPlaylist: (playlistId: number) => void;
};
export const PlaylistCard = ({
  playlist,
  isSelected,
  onClick,
  deletePlaylist,
  repullPlaylist,
}: PlaylistCardProps) => {
  const [hovering, setHovering] = useState<boolean>(false);
  const { getThumbnailUrl } = useApi();

  const [showConfirmDeleteDialog, setShowConfirmDeleteDialog] = useState(false);

  const playlistTypeStyle = () => {
    const combined: SxProps<Theme> = {
      backgroundColor: "red",
    };
    const simple: SxProps<Theme> = {};

    if (playlist.playlistTypeId === PlaylistTypes._2) return combined;
    return simple;
  };

  const selectedStyle = () => {
    const selected: SxProps<Theme> = {
      backgroundColor: "blue",
    };
    if (isSelected) return selected;
    return {};
  };

  const cardBackground: SxProps<Theme> = {
    backgroundImage: `url(${getThumbnailUrl(playlist.id!)})`,
    position: "absolute",
    top: "0",
    left: "0",
    bottom: "0",
    right: "0",

    backgroundSize: "cover",
    backgroundPosition: "center",
    filter: "brightness(50%) blur(1px)",
    zIndex: "-1",
  };

  const baseStyle: SxProps<Theme> = {
    minHeight: "150px",
    backgroundColor: "transparent",
    display: "grid",
  };
  const cardStyle: SxProps<Theme> = {
    ...baseStyle,
    ...playlistTypeStyle(),
    ...selectedStyle(),
  };

  const cardHeaderBase: SxProps<Theme> = {
    alignSelf: "start",
    backgroundImage:
      "linear-gradient(to bottom, rgba(0, 0, 0, 0.7), rgba(0, 0, 0, 0))",
    p: 1,
  };
  const cardHeader: SxProps<Theme> = {
    ...cardHeaderBase,
  };

  const cardFooterBaseStyle: SxProps<Theme> = {
    alignSelf: "end",
    backgroundImage:
      "linear-gradient(to bottom, rgba(0, 0, 0, 0), rgba(0, 0, 0, 0.7))",
  };
  const cardFooterStyle: SxProps<Theme> = {
    ...cardFooterBaseStyle,
  };
  return (
    <>
      <CardActionArea
        sx={{ overflow: "hidden" }}
        onClick={() => {
          onClick(playlist.id!);
        }}
        onMouseEnter={(e) => {
          setHovering(true);
        }}
        onMouseLeave={() => {
          setHovering(false);
        }}
      >
        <Box sx={cardBackground} />
        <Paper sx={cardStyle} square={false}>
          <Stack direction={"column"} sx={cardHeader}>
            <Typography sx={{ color: "text.secondary", fontSize: 14 }}>
              {playlist.channelName}
            </Typography>
            <Typography sx={{ fontSize: 20 }}>{playlist.name}</Typography>
          </Stack>

          <Stack direction={"row"} spacing={1} sx={cardFooterStyle}>
            <Stack style={{ width: "50%" }} direction={"row"}>
              <Fade in={hovering}>
                <IconButton
                  aria-label="sync"
                  onClick={(e) => {
                    e.stopPropagation();
                    e.preventDefault();
                    repullPlaylist(playlist.id!);
                  }}
                >
                  <Sync />
                </IconButton>
              </Fade>
            </Stack>
            <Stack style={{ width: "50%" }} direction={"row-reverse"}>
              <Fade in={hovering}>
                <IconButton
                  color="error"
                  aria-label="delete"
                  onClick={(e) => {
                    e.stopPropagation();
                    e.preventDefault();
                    setShowConfirmDeleteDialog(true);
                  }}
                >
                  <Delete />
                </IconButton>
              </Fade>
            </Stack>
          </Stack>
        </Paper>
      </CardActionArea>
      <ConfirmDialog
        open={showConfirmDeleteDialog}
        setOpen={setShowConfirmDeleteDialog}
        handleConfirm={() => {
          deletePlaylist(playlist.id!);
        }}
        title={`Are you sure you want to delete the playlist '${playlist.name}' ?`}
      />
    </>
  );
};
