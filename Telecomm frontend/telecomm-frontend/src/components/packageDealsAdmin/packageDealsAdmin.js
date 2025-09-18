import React, { useState, useEffect } from "react";
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
  Container,
  Spinner,
  Alert
} from "reactstrap";
import axios from 'axios';
import { toast } from 'react-toastify';

const PackageDealsAdmin = () => {
  const [packages, setPackages] = useState([]);
  const [categories, setCategories] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  const [showModal, setShowModal] = useState(false);
  const [editingPackage, setEditingPackage] = useState(null);
  const [formData, setFormData] = useState({
    name: '',
    price: '',
    categoryId: '',
    description: '',
    active: true
  });

  useEffect(() => {
    fetchPackages();
    fetchCategories();
  }, []);

  const fetchPackages = async () => {
    try {
      setLoading(true);
      const response = await axios.get('https://localhost:5001/api/telecom/packagedeal/packages');
      setPackages(response.data);
      setError('');
    } catch (err) {
      console.error('Error fetching packages:', err);
      setError('Failed to load packages');
      toast.error('Failed to load packages');
    } finally {
      setLoading(false);
    }
  };

  const fetchCategories = async () => {
    try {
      // For now use hardcoded categories - in production you'd fetch from API
      setCategories([
        { id: 1, name: 'Internet' },
        { id: 2, name: 'Mobile' },
        { id: 3, name: 'TV' },
        { id: 4, name: 'Bundle' }
      ]);
    } catch (err) {
      console.error('Error fetching categories:', err);
      toast.error('Failed to load categories');
    }
  };

  const handleSubmit = async () => {
    try {
      if (editingPackage) {
        // Update existing package
        const updateData = {
          id: editingPackage.id,
          name: formData.name,
          description: formData.description,
          price: parseFloat(formData.price),
          categoryId: parseInt(formData.categoryId) || null,
          isActive: formData.active
        };
        
        await axios.put(`https://localhost:5001/api/telecom/packagedeal/packages/${editingPackage.id}`, updateData);
        toast.success('Package updated successfully!');
      } else {
        // Add new package
        const newPackage = {
          name: formData.name,
          description: formData.description,
          price: parseFloat(formData.price),
          categoryId: parseInt(formData.categoryId) || null,
          isActive: formData.active
        };
        
        await axios.post('https://localhost:5001/api/telecom/packagedeal/packages', newPackage);
        toast.success('Package added successfully!');
      }
      
      // Refresh the packages list
      await fetchPackages();
      
      setShowModal(false);
      setEditingPackage(null);
      setFormData({ name: '', price: '', categoryId: '', description: '', active: true });
    } catch (err) {
      console.error('Error saving package:', err);
      toast.error('Failed to save package');
    }
  };

  const onEdit = (pkg) => {
    setEditingPackage(pkg);
    setFormData({
      name: pkg.name,
      price: pkg.price.toString(),
      categoryId: pkg.categoryId ? pkg.categoryId.toString() : '',
      description: pkg.description || '',
      active: pkg.isActive
    });
    setShowModal(true);
  };

  const onDelete = async (pkg) => {
    if (window.confirm('Are you sure you want to delete this package?')) {
      try {
        await axios.delete(`https://localhost:5001/api/telecom/packagedeal/packages/${pkg.id}`);
        toast.success('Package deleted successfully!');
        await fetchPackages(); // Refresh the list
      } catch (err) {
        console.error('Error deleting package:', err);
        toast.error('Failed to delete package');
      }
    }
  };

  const onAdd = () => {
    setEditingPackage(null);
    setFormData({ name: '', price: '', categoryId: '', description: '', active: true });
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

        {/* Error Alert */}
        {error && (
          <Alert color="danger" className="mb-4">
            {error}
          </Alert>
        )}

        {/* Loading Spinner */}
        {loading && (
          <div className="text-center mb-4">
            <Spinner color="primary" />
            <p className="mt-2">Loading packages...</p>
          </div>
        )}

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
                      <Badge color="info">{pkg.category ? pkg.category.name : 'No Category'}</Badge>
                    </td>
                    <td>
                      <span className="fw-bold text-success">€{pkg.price}</span>
                    </td>
                    <td>{getStatusBadge(pkg.isActive)}</td>
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
              <FormGroup>
                <Label><span className="me-2">📝</span>Description</Label>
                <Input
                  type="textarea"
                  value={formData.description}
                  onChange={(e) => setFormData({...formData, description: e.target.value})}
                  placeholder="Enter package description"
                  rows="3"
                />
              </FormGroup>
              <Row>
                <Col md={6}>
                  <FormGroup>
                    <Label><span className="me-2">⭐</span>Category</Label>
                    <Input
                      type="select"
                      value={formData.categoryId}
                      onChange={(e) => setFormData({...formData, categoryId: e.target.value})}
                    >
                      <option value="">Select category</option>
                      {categories.map(cat => (
                        <option key={cat.id} value={cat.id}>{cat.name}</option>
                      ))}
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
                      step="0.01"
                      min="0"
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
