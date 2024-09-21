import React, {
  createContext,
  useContext,
  ReactNode,
  useState,
  useEffect,
} from "react";
import { Client, Playlist } from "./api-client"; // Import your NSwag generated client
import { ShowError } from "../components/Toast";

// Define the shape of the API context
interface ApiContextProps {
  getPlaylists: () => Promise<Playlist[]>;
  getPlaylist: (playlistId: number) => Promise<Playlist>;
  addPlaylist: (url: string) => Promise<void>;
  deletePlaylist: (playlistId: number) => Promise<void>;
  deletePlaylists: (playlistIds: number[]) => Promise<void>;
  getLoginUrl: () => Promise<string>;
  refreshAccesstoken: () => Promise<void>;
  checkAccesstokenExpired: () => Promise<boolean>;
  checkHasAccesstoken: () => Promise<boolean>;
  getThumbnailUrl: (playlistId: number) => string;
  validatePlaylistUrl: (url: string) => Promise<boolean>;
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
    const baseUrl = "http://localhost:5026";
    const client = new Client(baseUrl);
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
      return client.addPlaylist(url).catch((error) => {
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

    const checkAccesstokenExpired = () => {
      return client.checkAccesstokenExpired().catch((error) => {
        setErrorMessage(error.toString());
        return false;
      });
    };
    const checkHasAccesstoken = () => {
      return client.checkHasAccesstoken().catch((error) => {
        setErrorMessage(error.toString());
        return false;
      });
    };

    const getLoginUrl = () => {
      return client.getLoginUrl().catch((error) => {
        setErrorMessage(error.toString());
        throw Error();
      });
    };
    const refreshAccesstoken = () => {
      return client.refreshAccesstoken().catch((error) => {
        setErrorMessage(error.toString());
        throw Error();
      });
    };
    const getThumbnailUrl = (playlistId: number) => {
      return `${baseUrl}/api/Playlist/get-thumbnail/${playlistId}`;
    };

    const validatePlaylistUrl = (url: string) => {
      return client.validatePlaylistUrl(url).catch((error) => {
        setErrorMessage(error.toString());
        return false;
      });
    };

    value = {
      getPlaylists,
      getPlaylist,
      addPlaylist,
      deletePlaylist,
      deletePlaylists,
      getLoginUrl,
      refreshAccesstoken,
      checkAccesstokenExpired,
      checkHasAccesstoken,
      getThumbnailUrl,
      validatePlaylistUrl,
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
