import React, {
  createContext,
  useContext,
  ReactNode,
  useState,
  useEffect,
  useCallback,
} from "react";
import { Client, SourceId } from "./api-client";
import { ShowError } from "../components/Toast";

// Define the shape of the API context
interface AccountContextProps {
  refreshAccesstoken: (sourceId: SourceId) => Promise<void>;
  getToken: (sourceId: SourceId) => Promise<void>;
  refreshToken: (sourceId: SourceId) => Promise<void>;
  isAuthenticated: (sourceId: SourceId) => Promise<boolean>;
}

// Create the API context
const AccountContext = createContext<AccountContextProps | undefined>(
  undefined
);

// Custom hook to use the API context
export const useAccount = () => {
  const context = useContext(AccountContext);
  if (!context) {
    throw new Error("useApi must be used within an AccountProvider");
  }
  return context;
};

const baseUrl = "http://localhost:5026";

export const AccountProvider = ({ children }: { children: ReactNode }) => {
  const [errorMessage, setErrorMessage] = useState<string>("");
  const [showErrorMessage, setShowErrorMessage] = useState<boolean>(false);

  // const [hasAccessToken, setHasAccessToken] = useState<boolean>();
  // const [accessTokenExpired, setAccessTokenExpired] = useState<boolean>();

  const isAuthenticated = async (sourceId: SourceId) => {
    return (
      (await checkHasAccesstoken(sourceId)) === true &&
      (await checkAccesstokenExpired(sourceId)) === false
    );
  };

  let client: Client;
  let value: AccountContextProps | undefined;

  useEffect(() => {
    for (let sourceId of Object.values(SourceId)) {
      checkHasAccesstoken(SourceId[sourceId]);
    }
  }, []);

  useEffect(() => {
    if (!errorMessage) return;
    console.error(errorMessage);
    setShowErrorMessage(true);
  }, [errorMessage]);

  try {
    client = new Client(baseUrl);
  } catch (error) {
    setErrorMessage("Couldn't initiate api client");
  }

  function checkAccesstokenExpired(sourceId: SourceId) {
    return client
      .checkAccesstokenExpired(SourceId[sourceId])
      .then((accessTokenExpired) => {
        return accessTokenExpired;
      })
      .catch((error) => {
        setErrorMessage(error.toString());
        return false;
      });
  }

  async function getToken(sourceId: SourceId) {
    const returnUrl = await getLoginUrl(sourceId);
    console.log("returnUrl", returnUrl);
    window.location.assign(returnUrl);
  }

  const checkHasAccesstoken = (sourceId: string) => {
    return client
      .checkHasAccesstoken(sourceId)
      .then((hasAccessToken) => {
        return hasAccessToken;
      })
      .catch((error) => {
        setErrorMessage(error.toString());
        return false;
      });
  };

  const getLoginUrl = (sourceId: SourceId) => {
    return client.getLoginUrl(SourceId[sourceId]).catch((error) => {
      setErrorMessage(error.toString());
      throw Error();
    });
  };
  const refreshAccesstoken = (sourceId: SourceId) => {
    return client.refreshAccesstoken(SourceId[sourceId]).catch((error) => {
      setErrorMessage(error.toString());
      throw Error();
    });
  };

  async function refreshToken(sourceId: SourceId) {
    await refreshAccesstoken(sourceId);
  }

  value = {
    refreshAccesstoken,
    getToken,
    isAuthenticated,
    refreshToken,
  };

  return (
    <AccountContext.Provider value={value}>
      {children}
      <ShowError
        message={errorMessage}
        open={showErrorMessage}
        setOpen={setShowErrorMessage}
      />
    </AccountContext.Provider>
  );
};
