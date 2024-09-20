import { Snackbar, Alert, SnackbarCloseReason } from "@mui/material";
import { useEffect, useState } from "react";

type ToastProps = {
  message: string;
  open: boolean;
  setOpen: (open: boolean) => void;
};
export const ShowError = ({ message, open, setOpen }: ToastProps) => {
  const handleClose = (
    event?: React.SyntheticEvent | Event,
    reason?: SnackbarCloseReason
  ) => {
    if (reason === "clickaway") {
      return;
    }

    setOpen(false);
  };

  return (
    <Snackbar
      open={open}
      autoHideDuration={6000}
      onClose={handleClose}
      anchorOrigin={{ vertical: "bottom", horizontal: "right" }}
    >
      <Alert
        onClose={handleClose}
        severity="error"
        variant="filled"
        sx={{ width: "100%" }}
      >
        {message}
      </Alert>
    </Snackbar>
  );
};
