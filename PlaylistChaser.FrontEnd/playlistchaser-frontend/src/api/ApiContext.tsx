import React, {
  createContext,
  useContext,
  ReactNode,
  useMemo,
  useState,
  useEffect,
} from "react";
import { AddPlaylistModel, Client, Playlist } from "./api-client"; // Import your NSwag generated client
import { ShowError } from "../components/Toast";

// Define the shape of the API context
interface ApiContextProps {
  getPlaylists: () => Promise<Playlist[]>;
  getPlaylist: (playlistId: number) => Promise<Playlist>;
  addPlaylist: (url: string) => Promise<void>;
  deletePlaylist: (playlistId: number) => Promise<void>;
  deletePlaylists: (playlistIds: number[]) => Promise<void>;
}

// Create the API context
const ApiContext = createContext<ApiContextProps | undefined>(undefined);

// Custom hook to use the API context
export const useApi = () => {
  const context = useContext(ApiContext);
  if (!context) {
    throw new Error("useApi must be used within an ApiProvider");
  }
  return context;
};

// API context provider component
export const ApiProvider = ({ children }: { children: ReactNode }) => {
  const [errorMessage, setErrorMessage] = useState<string>("");
  const [showErrorMessage, setShowErrorMessage] = useState<boolean>(false);

  useEffect(() => {
    if (!errorMessage) return;
    console.error(errorMessage);
    setShowErrorMessage(true);
  }, [errorMessage]);

  let value: ApiContextProps | undefined;
  try {
    const client = new Client("http://localhost:5026");
    const getPlaylists = () => {
      return client
        .getAllPlaylists()
        .then((playlists) => {
          return playlists;
        })
        .catch((error) => {
          setErrorMessage(error.toString());
          return Promise.resolve([]);
        });
    };
    const getPlaylist = (playlistId: number) => {
      return client
        .playlist(playlistId)
        .then((playlist) => {
          return playlist;
        })
        .catch((error) => {
          setErrorMessage(error.toString());
          throw Error(errorMessage);
        });
    };
    const addPlaylist = (url: string) => {
      return client
        .addPlaylist(new AddPlaylistModel({ playlistUrl: url }))
        .catch((error) => {
          setErrorMessage(error.toString());
        });
    };
    const deletePlaylist = (playlistId: number) => {
      return client.removePlaylist(playlistId).catch((error) => {
        setErrorMessage(error.toString());
      });
    };

    const deletePlaylists = (playlistIds: number[]) => {
      return client.removePlaylists(playlistIds).catch((error) => {
        setErrorMessage(error.toString());
      });
    };

    value = {
      getPlaylists,
      getPlaylist,
      addPlaylist,
      deletePlaylist,
      deletePlaylists,
    };
  } catch (error) {
    setErrorMessage("an unexcepted error occured");
  }

  return (
    <ApiContext.Provider value={value}>
      {children}
      <ShowError
        message={errorMessage}
        open={showErrorMessage}
        setOpen={setShowErrorMessage}
      />
    </ApiContext.Provider>
  );
};
