import React, { useState } from "react";
import { 
  Button, 
  Card, 
  CardBody, 
  Row, 
  Col, 
  Table, 
  Badge, 
  Modal, 
  ModalHeader, 
  ModalBody, 
  ModalFooter,
  Form,
  FormGroup,
  Label,
  Input,
  Container
} from "reactstrap";

const PackageDealsAdmin = () => {
  const [packages, setPackages] = useState([
    { 
      id: 1, 
      name: "SBB EON Basic", 
      price: 100.0,
      category: "Basic",
      active: true
    },
    { 
      id: 2, 
      name: "SBB EON Premium", 
      price: 150.0,
      category: "Premium",
      active: true
    },
    { 
      id: 3, 
      name: "SBB Internet Only", 
      price: 80.0,
      category: "Internet",
      active: false
    },
  ]);

  const [showModal, setShowModal] = useState(false);
  const [editingPackage, setEditingPackage] = useState(null);
  const [formData, setFormData] = useState({
    name: '',
    price: '',
    category: '',
    active: true
  });

  const handleSubmit = () => {
    if (editingPackage) {
      // Update existing package
      setPackages(prev => prev.map(pkg => 
        pkg.id === editingPackage.id 
          ? { ...pkg, ...formData, price: parseFloat(formData.price) }
          : pkg
      ));
    } else {
      // Add new package
      const newPackage = {
        id: Date.now(),
        ...formData,
        price: parseFloat(formData.price)
      };
      setPackages(prev => [...prev, newPackage]);
    }
    
    setShowModal(false);
    setEditingPackage(null);
    setFormData({ name: '', price: '', category: '', active: true });
  };

  const onEdit = (pkg) => {
    setEditingPackage(pkg);
    setFormData({
      name: pkg.name,
      price: pkg.price.toString(),
      category: pkg.category,
      active: pkg.active
    });
    setShowModal(true);
  };

  const onDelete = (pkg) => {
    if (window.confirm('Are you sure you want to delete this package?')) {
      setPackages(prev => prev.filter(p => p.id !== pkg.id));
    }
  };

  const onAdd = () => {
    setEditingPackage(null);
    setFormData({ name: '', price: '', category: '', active: true });
    setShowModal(true);
  };

  const getStatusBadge = (active) => {
    return active ? 
      <Badge color="success">✓ Active</Badge> : 
      <Badge color="danger">✗ Inactive</Badge>;
  };

  return (
    <div className="bg-light min-vh-100">
      <Container className="py-4">
        {/* Header */}
        <div className="text-center mb-5">
          <div className="text-primary mb-3" style={{ fontSize: "48px", fontWeight: "bold" }}>🏠</div>
          <h1 className="display-4 text-primary">
            📦 Admin Panel
          </h1>
          <p className="lead text-muted">
            ⚙️ Telecommunications package management
          </p>
        </div>

        {/* Stats Cards */}
        <Row className="mb-4">
          <Col md={3}>
            <Card className="text-center border-0 shadow-sm">
              <CardBody className="bg-primary text-white">
                <div className="mb-2" style={{ fontSize: "32px", fontWeight: "bold" }}>📦</div>
                <h3>{packages.length}</h3>
                <p className="mb-0">Total Packages</p>
              </CardBody>
            </Card>
          </Col>
          <Col md={3}>
            <Card className="text-center border-0 shadow-sm">
              <CardBody className="bg-success text-white">
                <div className="mb-2" style={{ fontSize: "32px", fontWeight: "bold" }}>✓</div>
                <h3>{packages.filter(p => p.active).length}</h3>
                <p className="mb-0">Active Packages</p>
              </CardBody>
            </Card>
          </Col>
          <Col md={3}>
            <Card className="text-center border-0 shadow-sm">
              <CardBody className="bg-info text-white">
                <div className="mb-2" style={{ fontSize: "32px", fontWeight: "bold" }}>👥</div>
                <h3>25</h3>
                <p className="mb-0">Users</p>
              </CardBody>
            </Card>
          </Col>
          <Col md={3}>
            <Card className="text-center border-0 shadow-sm">
              <CardBody className="bg-warning text-white">
                <div className="mb-2" style={{ fontSize: "32px", fontWeight: "bold" }}>📊</div>
                <h3>€{packages.reduce((sum, p) => sum + p.price, 0)}</h3>
                <p className="mb-0">Total Revenue</p>
              </CardBody>
            </Card>
          </Col>
        </Row>

        {/* Action Buttons */}
        <div className="d-flex justify-content-between align-items-center mb-4">
          <div>
            <h4><span className="text-primary">📦</span> Packages</h4>
          </div>
          <div>
            <Button color="primary" className="me-2" onClick={onAdd}>
              ➕ Add Package
            </Button>
            <Button color="secondary" className="me-2">
              🔍 Search
            </Button>
            <Button color="info">
              🔧 Filter
            </Button>
          </div>
        </div>

        {/* Packages Table */}
        <Card className="shadow-sm">
          <CardBody>
            <Table responsive striped hover>
              <thead className="table-dark">
                <tr>
                  <th><span className="me-2">📦</span>ID</th>
                  <th><span className="me-2">📦</span>Name</th>
                  <th><span className="me-2">⭐</span>Category</th>
                  <th><span className="me-2">💖</span>Price (€)</th>
                  <th><span className="me-2">ℹ️</span>Status</th>
                  <th><span className="me-2">⚙️</span>Actions</th>
                </tr>
              </thead>
              <tbody>
                {packages.map((pkg) => (
                  <tr key={pkg.id}>
                    <td><Badge color="secondary">{pkg.id}</Badge></td>
                    <td>
                      <strong>{pkg.name}</strong>
                    </td>
                    <td>
                      <Badge color="info">{pkg.category}</Badge>
                    </td>
                    <td>
                      <span className="fw-bold text-success">€{pkg.price}</span>
                    </td>
                    <td>{getStatusBadge(pkg.active)}</td>
                    <td>
                      <Button color="info" size="sm" className="me-1" title="View">
                        👁️
                      </Button>
                      <Button color="warning" size="sm" className="me-1" onClick={() => onEdit(pkg)} title="Edit">
                        ✏️
                      </Button>
                      <Button color="danger" size="sm" onClick={() => onDelete(pkg)} title="Delete">
                        🗑️
                      </Button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </Table>
          </CardBody>
        </Card>

        {/* Quick Actions */}
        <Row className="mt-4">
          <Col md={6}>
            <Card className="border-0 shadow-sm">
              <CardBody className="text-center">
                <div className="text-primary mb-3" style={{ fontSize: "32px", fontWeight: "bold" }}>⬇️</div>
                <h5>Download Report</h5>
                <Button color="outline-primary">
                  ⬇️ Excel
                </Button>
              </CardBody>
            </Card>
          </Col>
          <Col md={6}>
            <Card className="border-0 shadow-sm">
              <CardBody className="text-center">
                <div className="text-success mb-3" style={{ fontSize: "32px", fontWeight: "bold" }}>🖨️</div>
                <h5>Print</h5>
                <Button color="outline-success">
                  🖨️ PDF
                </Button>
              </CardBody>
            </Card>
          </Col>
        </Row>

        {/* Modal */}
        <Modal isOpen={showModal} toggle={() => setShowModal(false)}>
          <ModalHeader toggle={() => setShowModal(false)}>
            ✏️ {editingPackage ? 'Edit Package' : 'Add New Package'}
          </ModalHeader>
          <ModalBody>
            <Form>
              <FormGroup>
                <Label><span className="me-2">📦</span>Package Name</Label>
                <Input
                  type="text"
                  value={formData.name}
                  onChange={(e) => setFormData({...formData, name: e.target.value})}
                  placeholder="Enter package name"
                />
              </FormGroup>
              <Row>
                <Col md={6}>
                  <FormGroup>
                    <Label><span className="me-2">⭐</span>Category</Label>
                    <Input
                      type="select"
                      value={formData.category}
                      onChange={(e) => setFormData({...formData, category: e.target.value})}
                    >
                      <option value="">Select category</option>
                      <option value="Basic">Basic</option>
                      <option value="Premium">Premium</option>
                      <option value="Internet">Internet</option>
                    </Input>
                  </FormGroup>
                </Col>
                <Col md={6}>
                  <FormGroup>
                    <Label><span className="me-2">💖</span>Price (€)</Label>
                    <Input
                      type="number"
                      value={formData.price}
                      onChange={(e) => setFormData({...formData, price: e.target.value})}
                      placeholder="0.00"
                    />
                  </FormGroup>
                </Col>
              </Row>
              <FormGroup>
                <Label><span className="me-2">✓</span>Status</Label>
                <Input
                  type="select"
                  value={formData.active ? 'true' : 'false'}
                  onChange={(e) => setFormData({...formData, active: e.target.value === 'true'})}
                >
                  <option value="true">Active</option>
                  <option value="false">Inactive</option>
                </Input>
              </FormGroup>
            </Form>
          </ModalBody>
          <ModalFooter>
            <Button color="secondary" onClick={() => setShowModal(false)}>
              ✗ Cancel
            </Button>
            <Button color="primary" onClick={handleSubmit}>
              ✓ {editingPackage ? 'Edit' : 'Add'}
            </Button>
          </ModalFooter>
        </Modal>
      </Container>
    </div>
  );
};

export default PackageDealsAdmin;
