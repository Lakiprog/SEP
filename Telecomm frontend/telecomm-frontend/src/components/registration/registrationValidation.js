import * as yup from "yup";

export const RegistrationValidation = yup.object().shape({
  Username: yup.string().max(50, "Max num of characters is 50").required("Username is a required field"),
  Email: yup.string().email("Email format is wrong").required("Email is a required field"),
  Password: yup.string().max(50, "Max num of characters is 50").required("Password is a required field"),
});
