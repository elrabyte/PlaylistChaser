import React, { useState } from "react";
import Playlists from "./Playlists";
import { Navigation } from "./Navigation";
import { ApiProvider } from "./api/ApiContext";

function App() {
  const [currentTab, setCurrentTab] = useState<number>(0);

  return (
    <>
      <Navigation currentTab={currentTab} setCurrentTab={setCurrentTab} />
      <ApiProvider>
        <Playlists />
      </ApiProvider>
    </>
  );
}

export default App;
