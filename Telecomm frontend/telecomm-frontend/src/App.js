import "./App.css";
import { BrowserRouter, Route, RouterProvider, Routes, createBrowserRouter } from "react-router-dom";
import HomeNavbar from "./components/navbars/homeNavbar";
import Registration from "./components/registration/registration";
import Login from "./components/login/login";

function App() {
  const router = createBrowserRouter([
    {
      path: "/",
      element: <HomeNavbar />,
    },
    {
      path: "/login",
      element: <Login />,
    },
    {
      path: "/registration",
      element: <Registration />,
    },
  ]);

  return <RouterProvider router={router}></RouterProvider>;
}

export default App;
