import React, { useState } from "react";
import { Button, Card, CardBody, CardTitle, ListGroup, ListGroupItem } from "reactstrap";
import AdminNavbar from "../navbars/adminNavbar";

const PackageDealsAdmin = (props) => {
  const [packageDeals, setPackageDeals] = useState([
    { Id: 1, Name: "SBB EON", Price: 100.0 },
    { Id: 2, Name: "SBB EON + Telephone", Price: 110.0 },
    { Id: 3, Name: "SBB Telephone", Price: 10.0 },
  ]);

  const onAdd = () => {};

  const onEdit = (data) => {};

  const onDelete = (data) => {};

  return (
    <div>
      <AdminNavbar />
      <Card className="registration-form" style={{ backgroundColor: "#DEEDE6", borderColor: "black" }}>
        <CardTitle>Package Deals</CardTitle>
        <CardBody>
          <Button color="primary" style={{ marginBottom: "5px" }} onClick={onAdd}>
            Add
          </Button>
          <ListGroup>
            {packageDeals.map((packageDeal) => (
              <ListGroupItem key={packageDeal.Id} action tag="button">
                <Button color="warning" style={{ marginBottom: "5px", marginLeft: "10px" }} onClick={() => onEdit(packageDeal)}>
                  Edit
                </Button>
                <Button color="danger" style={{ marginBottom: "5px", marginLeft: "10px", marginRight: "10px" }} onClick={() => onDelete(packageDeal)}>
                  Delete
                </Button>
                {`${packageDeal.Name} = ${packageDeal.Price} Euros`}
              </ListGroupItem>
            ))}
          </ListGroup>
        </CardBody>
      </Card>
    </div>
  );
};

export default PackageDealsAdmin;
