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

  get(requestPath, params) {
    var config = {
      ...this.baseConfig,
      method: "get",
      url: requestPath,
      params: params,
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
    };
    return makeCall(config);
  }

  delete(requestPath, data) {
    var config = {
      ...this.baseConfig,
      method: "delete",
      url: requestPath,
      data: data,
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

        reject({ errMsg: err.message });
      });
  });
};

const httpRequest = new HttpRequest();
export default httpRequest;
