import React, {
  createContext,
  useContext,
  ReactNode,
  useState,
  useEffect,
} from "react";
import { Client } from "./api-client";
import { ShowError } from "../components/Toast";

// Define the shape of the API context
interface AccountContextProps {
  refreshAccesstoken: () => Promise<void>;
  hasAccessToken: boolean | undefined;
  accessTokenExpired: boolean | undefined;
  getToken: () => Promise<void>;
  refreshToken: () => Promise<void>;
  isAuthenticated: () => boolean;
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

  const [hasAccessToken, setHasAccessToken] = useState<boolean>();
  const [accessTokenExpired, setAccessTokenExpired] = useState<boolean>();

  const isAuthenticated = (): boolean => {
    return hasAccessToken === true && accessTokenExpired === false;
  };

  let client: Client;
  let value: AccountContextProps | undefined;

  useEffect(() => {
    checkHasAccesstoken();
  }, []);

  useEffect(() => {
    if (!errorMessage) return;
    console.error(errorMessage);
    setShowErrorMessage(true);
  }, [errorMessage]);

  useEffect(() => {
    if (hasAccessToken && !accessTokenExpired) {
      checkAccesstokenExpired();
    } else if (hasAccessToken && accessTokenExpired) {
      getToken();
    }
    console.log("hasAccessToken", hasAccessToken);
  }, [hasAccessToken]);

  useEffect(() => {
    if (accessTokenExpired) {
      refreshToken();
    }
    console.log("accessTokenExpired", accessTokenExpired);
  }, [accessTokenExpired]);

  try {
    client = new Client(baseUrl);
  } catch (error) {
    setErrorMessage("Couldn't initiate api client");
  }

  function checkAccesstokenExpired() {
    return client
      .checkAccesstokenExpired()
      .then((accessTokenExpired) => {
        setAccessTokenExpired(accessTokenExpired);
      })
      .catch((error) => {
        setErrorMessage(error.toString());
        return false;
      });
  }

  async function getToken() {
    const returnUrl = await getLoginUrl();
    console.log("returnUrl", returnUrl);
    window.location.assign(returnUrl);
  }
  const checkHasAccesstoken = () => {
    return client
      .checkHasAccesstoken()
      .then((hasAccessToken) => {
        setHasAccessToken(hasAccessToken);
      })
      .catch((error) => {
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

  async function refreshToken() {
    await refreshAccesstoken();
    setAccessTokenExpired(false);
  }

  value = {
    hasAccessToken,
    accessTokenExpired,
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
