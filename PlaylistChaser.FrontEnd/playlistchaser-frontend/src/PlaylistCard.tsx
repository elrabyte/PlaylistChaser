import Card from "@mui/material/Card";
import CardContent from "@mui/material/CardContent";
import CardMedia from "@mui/material/CardMedia";
import Typography from "@mui/material/Typography";
import { Playlist, PlaylistTypes } from "./api/api-client";
import { SxProps, Theme } from "@mui/material/styles";
import CardActionArea from "@mui/material/CardActionArea";
import IconButton, { IconButtonProps } from "@mui/material/IconButton";

import { MoreVert } from "@mui/icons-material";

import CardHeader from "@mui/material/CardHeader";

type PlaylistCardProps = {
  playlist: Playlist;
  isSelected: boolean;
  setIsSelected: (playlistId: number) => void;
};
export const PlaylistCard = ({
  playlist,
  isSelected,
  setIsSelected,
}: PlaylistCardProps) => {
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
    <Card variant="outlined" sx={cardStyle}>
      <CardActionArea
        onClick={() => {
          setIsSelected(playlist.id!);
        }}
      >
        <CardHeader
          action={
            <IconButton aria-label="settings">
              <MoreVert />
            </IconButton>
          }
        />
        <CardMedia
          component="img"
          alt="thumbnail"
          image={playlist.thumbnailId?.toString()}
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
  );
};
