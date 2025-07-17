import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { templateLibraryService } from '../services/templateLibraryService';

interface Template {
  id: string;
  name: string;
  description: string;
  category: string;
  tags: string[];
  createdAt: string;
  modifiedAt: string;
  version: number;
  isPublic: boolean;
}

const TemplateLibrary: React.FC = () => {
  const [templates, setTemplates] = useState<Template[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedCategory, setSelectedCategory] = useState('');

  useEffect(() => {
    loadTemplates();
  }, [selectedCategory, searchQuery]);

  const loadTemplates = async () => {
    try {
      setLoading(true);
      const result = await templateLibraryService.getTemplates({
        category: selectedCategory || undefined,
        search: searchQuery || undefined,
        page: 1,
        pageSize: 20
      });
      setTemplates(result.data);
    } catch (err) {
      setError('Failed to load templates');
      console.error('Error loading templates:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleSearch = async () => {
    if (searchQuery.trim()) {
      try {
        setLoading(true);
        const result = await templateLibraryService.searchTemplates(searchQuery, 1, 20);
        setTemplates(result.data);
      } catch (err) {
        setError('Failed to search templates');
        console.error('Error searching templates:', err);
      } finally {
        setLoading(false);
      }
    } else {
      loadTemplates();
    }
  };

  const categories = ['Storage', 'Compute', 'Network', 'Security', 'Database', 'Web', 'Analytics'];

  return (
    <div className="container-fluid py-4">
      <div className="d-flex justify-content-between align-items-center mb-4">
        <h1>
          <i className="fas fa-file-code me-2"></i>
          Template Library
        </h1>
        <Link to="/templates/new" className="btn btn-primary">
          <i className="fas fa-plus me-2"></i>
          New Template
        </Link>
      </div>

      {/* Search and Filter Bar */}
      <div className="row mb-4">
        <div className="col-md-8">
          <div className="input-group">
            <input
              type="text"
              className="form-control"
              placeholder="Search templates by name, description, or content..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              onKeyPress={(e) => e.key === 'Enter' && handleSearch()}
            />
            <button 
              className="btn btn-outline-secondary" 
              type="button"
              onClick={handleSearch}
            >
              <i className="fas fa-search"></i>
            </button>
          </div>
        </div>
        <div className="col-md-4">
          <select
            className="form-select"
            value={selectedCategory}
            onChange={(e) => setSelectedCategory(e.target.value)}
          >
            <option value="">All Categories</option>
            {categories.map(category => (
              <option key={category} value={category}>{category}</option>
            ))}
          </select>
        </div>
      </div>

      {/* Error Message */}
      {error && (
        <div className="alert alert-danger" role="alert">
          <i className="fas fa-exclamation-triangle me-2"></i>
          {error}
        </div>
      )}

      {/* Loading Spinner */}
      {loading && (
        <div className="text-center py-4">
          <div className="spinner-border text-primary" role="status">
            <span className="visually-hidden">Loading...</span>
          </div>
        </div>
      )}

      {/* Templates Grid */}
      {!loading && !error && (
        <div className="row">
          {templates.length === 0 ? (
            <div className="col-12">
              <div className="text-center py-5">
                <i className="fas fa-file-code fa-3x text-muted mb-3"></i>
                <h4 className="text-muted">No templates found</h4>
                <p className="text-muted">Get started by creating your first ARM template.</p>
                <Link to="/templates/new" className="btn btn-primary">
                  Create Template
                </Link>
              </div>
            </div>
          ) : (
            templates.map(template => (
              <div key={template.id} className="col-xl-4 col-lg-6 col-md-6 mb-4">
                <div className="card template-card h-100">
                  <div className="card-body">
                    <div className="d-flex justify-content-between align-items-start mb-2">
                      <h5 className="card-title">{template.name}</h5>
                      <span className="badge bg-secondary">{template.category}</span>
                    </div>
                    <p className="card-text text-muted small">
                      {template.description}
                    </p>
                    <div className="mb-3">
                      {template.tags.map(tag => (
                        <span key={tag} className="badge bg-light text-dark me-1">
                          {tag}
                        </span>
                      ))}
                    </div>
                    <div className="d-flex justify-content-between align-items-center">
                      <small className="text-muted">
                        v{template.version} • {new Date(template.modifiedAt).toLocaleDateString()}
                      </small>
                      <div className="btn-group" role="group">
                        <Link 
                          to={`/templates/${template.id}/edit`} 
                          className="btn btn-outline-primary btn-sm"
                        >
                          <i className="fas fa-edit"></i>
                        </Link>
                        <button 
                          className="btn btn-outline-success btn-sm"
                          title="Deploy"
                        >
                          <i className="fas fa-rocket"></i>
                        </button>
                      </div>
                    </div>
                  </div>
                  {template.isPublic && (
                    <div className="card-footer">
                      <small className="text-success">
                        <i className="fas fa-globe me-1"></i>
                        Public template
                      </small>
                    </div>
                  )}
                </div>
              </div>
            ))
          )}
        </div>
      )}
    </div>
  );
};

export default TemplateLibrary;