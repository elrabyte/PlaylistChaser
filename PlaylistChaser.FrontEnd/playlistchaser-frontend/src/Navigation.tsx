import React, { useState } from "react";
import Box from "@mui/material/Box";
import Tabs from "@mui/material/Tabs";
import Tab from "@mui/material/Tab";
import TabList from "@mui/lab/TabList";

export type NavigationProps = {
  setCurrentTab: (currentTab: string) => void;
};

export const Navigation = ({ setCurrentTab }: NavigationProps) => {
  return (
    <Box sx={{ borderBottom: 1, borderColor: "divider" }}>
      <TabList
        onChange={(e, value) => {
          setCurrentTab(value);
        }}
      >
        <Tab label="Playlists" value="0" />
        <Tab label="Account" value="1" />
      </TabList>
    </Box>
  );
};
