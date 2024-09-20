import React, {
  createContext,
  useContext,
  ReactNode,
  useMemo,
  useState,
  useEffect,
} from "react";
import { Client, Playlist } from "./api-client"; // Import your NSwag generated client
import { ShowError } from "../components/Toast";

// Define the shape of the API context
interface ApiContextProps {
  getPlaylists: () => Promise<Playlist[]>;
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
        .playlistAll()
        .then((playlists) => {
          return playlists;
        })
        .catch((error) => {
          setErrorMessage(error.toString());
          return Promise.resolve([]);
        });
    };

    value = {
      getPlaylists,
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
