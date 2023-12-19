import { Route, Routes } from "react-router-dom";

import "./App.css";

import Header from "./components/Header/Header";
import Login from "./components/Login/Login";
import Footer from "./components/Footer/Footer";
import Register from "./components/Register/Register";
import Home from "./components/Home/Home";
import { AuthProvider } from "./contexts/authContext";
import { PATH } from "./core/environments/costants";
import Logout from "./components/Logout/Logout";
import Userprofile from "./components/Userprofile/Userprofile";
import Userprofileedit from "./components/Userprofile/Userprofileedit";

function App() {
  return (
    <AuthProvider>
      <>
        <Header />

        <main className="main-content">
          <Routes>
            <Route path={PATH.home} element={<Home />} />
            <Route path={PATH.login} element={<Login />} />
            <Route path={PATH.register} element={<Register />} />
            <Route path={PATH.logout} element={<Logout />} />
            <Route path={PATH.profile} element={<Userprofile />} />
            <Route path={PATH.profileedit} element={<Userprofileedit />} />
          </Routes>
        </main>

        <Footer />
      </>
    </AuthProvider>
  );
}

export default App;
