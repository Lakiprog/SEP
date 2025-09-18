import axios from "axios";

export class HttpRequest {
  constructor() {
    // axios.defaults.withCredentials = true;
    this.baseConfig = {
      //   baseURL: process.env.REACT_APP_BASE_API_ENDPOINT,
      //   withCredentials: true,
      //   credentials: "include", // "same-origin",
      mode: "no-cors",
      headers: {
        Accept: "application/json",
        "Content-Type": "application/json",
      },
      cache: false,
    };

    // setupCache(axios);
  }

  getAuthHeaders() {
    const token = localStorage.getItem('token');
    if (token) {
      return {
        ...this.baseConfig.headers,
        Authorization: `Bearer ${token}`
      };
    }
    return this.baseConfig.headers;
  }

  get(requestPath, params) {
    var config = {
      ...this.baseConfig,
      method: "get",
      url: requestPath,
      params: params,
      headers: this.getAuthHeaders(),
    };
    return makeCall(config);
  }

  post(requestPath, data) {
    var config = {
      ...this.baseConfig,
      method: "post",
      url: requestPath,
      //   data: JSON.stringify(data),
      data: data,
      headers: this.getAuthHeaders(),
    };
    return makeCall(config);
  }

  delete(requestPath, data) {
    var config = {
      ...this.baseConfig,
      method: "delete",
      url: requestPath,
      data: data,
      headers: this.getAuthHeaders(),
    };
    return makeCall(config);
  }
}

const makeCall = (config) => {
  return new Promise((resolve, reject) => {
    axios(config)
      .then((result) => {
        resolve(result.data);
      })
      .catch((err) => {
        if (axios.isCancel(err)) {
          reject({
            isCancel: true,
          });
          return;
        }

        // Handle HTTP error responses
        if (err.response) {
          // Server responded with error status
          const status = err.response.status;
          const data = err.response.data;

          // Try to extract error message from response
          let errorMessage = err.message;
          if (typeof data === 'string') {
            errorMessage = data;
          } else if (data && data.message) {
            errorMessage = data.message;
          } else if (data && typeof data === 'object') {
            errorMessage = JSON.stringify(data);
          }

          reject({
            errMsg: errorMessage,
            status: status,
            response: data
          });
        } else if (err.request) {
          // Network error
          reject({ errMsg: "Network error. Please check your connection." });
        } else {
          // Other error
          reject({ errMsg: err.message });
        }
      });
  });
};

const httpRequest = new HttpRequest();
export default httpRequest;
