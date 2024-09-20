import { createTheme, ThemeProvider } from '@mui/material/styles';
import { CssBaseline } from '@mui/material';
import ReactDOM from 'react-dom/client';
import App from './App'; 
import { deepPurple } from "@mui/material/colors";

const darkTheme = createTheme({
  palette: {
    mode: "dark",
    primary: {
      main: deepPurple[500],
    },
  },
});
const lightTheme = createTheme({
  palette: {
    mode: "light",
    primary: {
      main: deepPurple[500],
    },
  },
});

const getPreferredTheme = () => {
  return window.matchMedia('(prefers-color-scheme: dark)').matches
    ? darkTheme
    : lightTheme;
};


let currentTheme = getPreferredTheme();

window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', (e) => {
  currentTheme = e.matches ? darkTheme : lightTheme;
  root.render(
    <ThemeProvider theme={currentTheme}>
      <CssBaseline />
      <App />
    </ThemeProvider>
  );
});

const root = ReactDOM.createRoot(
  document.getElementById('root') as HTMLElement
);

root.render(
  <ThemeProvider theme={currentTheme}>
    <CssBaseline />
    <App />
  </ThemeProvider>
);
