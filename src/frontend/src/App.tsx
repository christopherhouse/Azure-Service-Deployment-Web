import React from 'react';
import { Routes, Route } from 'react-router-dom';
import { AuthenticatedTemplate, UnauthenticatedTemplate } from '@azure/msal-react';
import Navbar from './components/Navbar';
import Sidebar from './components/Sidebar';
import Dashboard from './pages/Dashboard';
import TemplateLibrary from './pages/TemplateLibrary';
import TemplateEditor from './pages/TemplateEditor';
import Deployments from './pages/Deployments';
import Account from './pages/Account';
import Login from './pages/Login';

function App() {
  return (
    <div className="App">
      <AuthenticatedTemplate>
        <Navbar />
        <div className="container-fluid">
          <div className="row">
            <nav className="col-md-3 col-lg-2 d-md-block sidebar collapse">
              <Sidebar />
            </nav>
            <main className="col-md-9 ms-sm-auto col-lg-10 px-md-4 main-content">
              <Routes>
                <Route path="/" element={<Dashboard />} />
                <Route path="/templates" element={<TemplateLibrary />} />
                <Route path="/templates/new" element={<TemplateEditor />} />
                <Route path="/templates/:id/edit" element={<TemplateEditor />} />
                <Route path="/deployments" element={<Deployments />} />
                <Route path="/account" element={<Account />} />
              </Routes>
            </main>
          </div>
        </div>
      </AuthenticatedTemplate>
      
      <UnauthenticatedTemplate>
        <Login />
      </UnauthenticatedTemplate>
    </div>
  );
}

export default App;