import React, { useState } from "react";
import Playlists from "./Playlist/Playlists";
import { Navigation } from "./Navigation";
import { ApiProvider } from "./api/ApiContext";
import { Container } from "@mui/material";
import TabContext from "@mui/lab/TabContext";
import TabPanel from "@mui/lab/TabPanel";
import { Account } from "./Account/Account";

function App() {
  const [currentTab, setCurrentTab] = useState<string>("0");

  return (
    <TabContext value={currentTab}>
      <Navigation setCurrentTab={setCurrentTab} />
      <ApiProvider>
        <Container>
          <TabPanel value="0">
            <Playlists />
          </TabPanel>
          <TabPanel value="1">
            <Account />
          </TabPanel>
        </Container>
      </ApiProvider>
    </TabContext>
  );
}

export default App;
