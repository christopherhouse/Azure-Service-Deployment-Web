import React from 'react';

const TemplateEditor: React.FC = () => {
  return (
    <div className="container-fluid py-4">
      <h1>
        <i className="fas fa-edit me-2"></i>
        Template Editor
      </h1>
      <p>Browser-based ARM template editor with Monaco (coming soon)</p>
    </div>
  );
};

export default TemplateEditor;