import * as React from "react";
import Button from "@mui/material/Button";
import Dialog from "@mui/material/Dialog";
import DialogActions from "@mui/material/DialogActions";
import DialogContent from "@mui/material/DialogContent";
import DialogContentText from "@mui/material/DialogContentText";
import DialogTitle from "@mui/material/DialogTitle";
import { useEffect } from "react";

type ConfirmDialogProps = {
  open: boolean;
  setOpen: (open: boolean) => void;
  title?: string;
  handleConfirm: () => void;
};
export const ConfirmDialog = ({
  open,
  setOpen,
  title,
  handleConfirm,
}: ConfirmDialogProps) => {
  const handleClose = () => {
    setOpen(false);
  };

  return (
    <Dialog
      open={open}
      onClose={handleClose}
      aria-labelledby="alert-dialog-title"
      aria-describedby="alert-dialog-description"
    >
      <DialogTitle id="alert-dialog-title">
        {title ?? "Are you sure?"}
      </DialogTitle>
      <DialogActions>
        <Button
          onClick={() => {
            setOpen(false);
          }}
        >
          Cancel
        </Button>

        <Button
          variant="contained"
          onClick={() => {
            setOpen(false);
            handleConfirm();
          }}
        >
          Confirm
        </Button>
      </DialogActions>
    </Dialog>
  );
};
