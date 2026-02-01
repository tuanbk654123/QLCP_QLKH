import axios from 'axios';

const API_BASE_URL = ''; // Hardcode to empty string to prevent double /api/api issue
// const API_BASE_URL = process.env.REACT_APP_API_BASE_URL || '';

axios.defaults.baseURL = API_BASE_URL;

export { API_BASE_URL };
