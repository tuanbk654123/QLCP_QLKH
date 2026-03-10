import React from 'react';
import ReactDOM from 'react-dom/client';
import './config/api';
import App from './App';

if (process.env.REACT_APP_USE_MOCK_API === 'true') {
  require('./mockapi');
}

const root = ReactDOM.createRoot(document.getElementById('root'));
root.render(
  <React.StrictMode>
    <App />
  </React.StrictMode>
);

