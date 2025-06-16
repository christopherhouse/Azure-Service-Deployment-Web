import React from 'react';
import './FileUpload.css';

interface FileUploadProps {
  onTemplateFileChange: (file: File | null) => void;
  onParametersFileChange: (file: File | null) => void;
  templateFile: File | null;
  parametersFile: File | null;
}

export const FileUpload: React.FC<FileUploadProps> = ({
  onTemplateFileChange,
  onParametersFileChange,
  templateFile,
  parametersFile,
}) => {
  const handleTemplateFileChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0] || null;
    onTemplateFileChange(file);
  };

  const handleParametersFileChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0] || null;
    onParametersFileChange(file);
  };

  return (
    <div className="file-upload-container">
      <h3>📁 Upload Bicep Files</h3>
      
      <div className="file-input-group">
        <label htmlFor="template-file" className="file-label">
          📄 Bicep Template (.bicep)
        </label>
        <input
          id="template-file"
          type="file"
          accept=".bicep,.json"
          onChange={handleTemplateFileChange}
          className="file-input"
        />
        {templateFile && (
          <div className="file-selected">
            ✅ {templateFile.name}
          </div>
        )}
      </div>

      <div className="file-input-group">
        <label htmlFor="parameters-file" className="file-label">
          ⚙️ Parameters File (.bicepparam or .json)
        </label>
        <input
          id="parameters-file"
          type="file"
          accept=".bicepparam,.json"
          onChange={handleParametersFileChange}
          className="file-input"
        />
        {parametersFile && (
          <div className="file-selected">
            ✅ {parametersFile.name}
          </div>
        )}
      </div>
    </div>
  );
};