import React, { useState } from "react";
import Playlists from "./Playlists";
import { Navigation } from "./Navigation";
import { ApiProvider } from "./api/ApiContext";
import { Container } from "@mui/material";

function App() {
  const [currentTab, setCurrentTab] = useState<number>(0);

  return (
    <>
      <Navigation currentTab={currentTab} setCurrentTab={setCurrentTab} />
      <ApiProvider>
        <Container>
          <Playlists />
        </Container>
      </ApiProvider>
    </>
  );
}

export default App;
