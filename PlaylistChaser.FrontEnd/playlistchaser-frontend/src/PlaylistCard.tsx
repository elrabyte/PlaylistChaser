import Card from "@mui/material/Card";
import CardContent from "@mui/material/CardContent";
import CardMedia from "@mui/material/CardMedia";
import Typography from "@mui/material/Typography";
import { Playlist, PlaylistTypes } from "./api/api-client";
import { SxProps, Theme } from "@mui/material/styles";
import CardActionArea from "@mui/material/CardActionArea";
import IconButton, { IconButtonProps } from "@mui/material/IconButton";

import { Delete, Edit, MoreVert } from "@mui/icons-material";

import CardHeader from "@mui/material/CardHeader";
import { Menu, MenuItem } from "@mui/material";
import { useRef, useState } from "react";

type PlaylistCardProps = {
  playlist: Playlist;
  isSelected: boolean;
  setIsSelected: (playlistId: number) => void;
  deletePlaylist: (playlistId: number) => void;
};
export const PlaylistCard = ({
  playlist,
  isSelected,
  setIsSelected,
  deletePlaylist,
}: PlaylistCardProps) => {
  const [showOptions, setShowOptions] = useState<boolean>(false);
  const optionsButton = useRef(null);

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

  const cardStyle = { ...playlistTypeStyle(), ...selectedStyle() };

  return (
    <>
      <Menu
        anchorEl={optionsButton?.current}
        open={showOptions}
        onClose={() => {
          setShowOptions(false);
        }}
      >
        <MenuItem
          onClick={() => {
            setShowOptions(false);
            deletePlaylist(playlist.id!);
          }}
        >
          <Delete />
          Delete
        </MenuItem>
      </Menu>
      <Card variant="outlined" sx={cardStyle}>
        <CardActionArea
          onClick={() => {
            setIsSelected(playlist.id!);
          }}
        >
          <CardHeader
            action={
              <IconButton
                aria-label="settings"
                ref={optionsButton}
                onClick={(e) => {
                  e.stopPropagation();
                  e.preventDefault();
                  setShowOptions(true);
                }}
              >
                <MoreVert />
              </IconButton>
            }
          />
          <CardMedia
            component="img"
            alt="thumbnail"
            image={playlist.thumbnail?.fileContents?.toString()}
          />
          <CardContent>
            <Typography gutterBottom variant="h5" component="div">
              {playlist.name}
            </Typography>
            <Typography variant="body2" sx={{ color: "text.secondary" }}>
              {playlist.description}
            </Typography>
          </CardContent>
        </CardActionArea>
      </Card>
    </>
  );
};
