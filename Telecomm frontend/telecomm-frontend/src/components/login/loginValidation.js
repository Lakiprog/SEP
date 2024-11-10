import * as yup from "yup";

export const LoginValidation = yup.object().shape({
  Username: yup.string().required("Username is a required field"),
  Password: yup.string().required("Password is a required field"),
});
