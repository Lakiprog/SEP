import * as yup from "yup";

export const CardPaymentFormValidation = yup.object().shape({
  CardNumber: yup.string().required("Card Number is a required field").matches("^[0-9]{13,19}$", "Credit card number format is wrong"),
  CardHolderName: yup.string().required("Card Holder Name is a required field"),
  ExpiryDate: yup
    .string()
    .required("Expiry Date is a required field")
    .matches("^(0[1-9]|1[0-2])/?([0-9]{4}|[0-9]{2})$", "Expiry Date must have format MM/YY"),
  CVC: yup.string().required("CVC is a required field").matches("^[0-9]{3}$", "CVC format must have format XXX"),
});
