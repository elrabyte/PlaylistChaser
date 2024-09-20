import React, { useState } from "react";
import Box from "@mui/material/Box";
import Tabs from "@mui/material/Tabs";
import Tab from "@mui/material/Tab";

export type NavigationProps = {
  currentTab: number;
  setCurrentTab: (currentTab: number) => void;
};

export const Navigation = ({ currentTab, setCurrentTab }: NavigationProps) => {
  return (
    <Box sx={{ borderBottom: 1, borderColor: "divider" }}>
      <Tabs
        value={currentTab}
        onChange={(e, value) => {
          setCurrentTab(value);
        }}
      >
        <Tab label="Playlists" />
      </Tabs>
    </Box>
  );
};
