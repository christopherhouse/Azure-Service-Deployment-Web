import axios from 'axios';
import { msalInstance } from './authConfig';
import { apiScopes } from './authConfig';

const API_BASE_URL = process.env.REACT_APP_API_BASE_URL || 'https://localhost:7000';

// Create axios instance with interceptors for authentication
const apiClient = axios.create({
  baseURL: API_BASE_URL,
});

// Request interceptor to add authentication token
apiClient.interceptors.request.use(async (config) => {
  try {
    const accounts = msalInstance.getAllAccounts();
    if (accounts.length > 0) {
      const request = {
        scopes: apiScopes.templateLibrary,
        account: accounts[0],
      };
      const response = await msalInstance.acquireTokenSilent(request);
      config.headers.Authorization = `Bearer ${response.accessToken}`;
    }
  } catch (error) {
    console.error('Error acquiring token:', error);
  }
  return config;
});

// Response interceptor for error handling
apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      // Handle unauthorized - redirect to login
      msalInstance.loginRedirect();
    }
    return Promise.reject(error);
  }
);

export interface TemplateDto {
  id: string;
  name: string;
  description: string;
  category: string;
  templateContent: string;
  parametersContent?: string;
  tags: string[];
  tenantId: string;
  createdBy: string;
  createdAt: string;
  modifiedAt: string;
  version: number;
  isPublic: boolean;
}

export interface CreateTemplateRequest {
  name: string;
  description: string;
  category: string;
  templateContent: string;
  parametersContent?: string;
  tags: string[];
  isPublic: boolean;
}

export interface UpdateTemplateRequest {
  name: string;
  description: string;
  category: string;
  templateContent: string;
  parametersContent?: string;
  tags: string[];
  isPublic: boolean;
}

export interface GetTemplatesOptions {
  category?: string;
  search?: string;
  page?: number;
  pageSize?: number;
}

export const templateLibraryService = {
  async getTemplates(options: GetTemplatesOptions = {}) {
    const params = new URLSearchParams();
    if (options.category) params.append('category', options.category);
    if (options.search) params.append('search', options.search);
    if (options.page) params.append('page', options.page.toString());
    if (options.pageSize) params.append('pageSize', options.pageSize.toString());

    const response = await apiClient.get(`/api/templates?${params}`);
    return response.data;
  },

  async getTemplate(id: string): Promise<TemplateDto> {
    const response = await apiClient.get(`/api/templates/${id}`);
    return response.data;
  },

  async createTemplate(template: CreateTemplateRequest): Promise<TemplateDto> {
    const response = await apiClient.post('/api/templates', template);
    return response.data;
  },

  async updateTemplate(id: string, template: UpdateTemplateRequest): Promise<TemplateDto> {
    const response = await apiClient.put(`/api/templates/${id}`, template);
    return response.data;
  },

  async deleteTemplate(id: string): Promise<void> {
    await apiClient.delete(`/api/templates/${id}`);
  },

  async searchTemplates(query: string, page: number = 1, pageSize: number = 20) {
    const params = new URLSearchParams({
      query,
      page: page.toString(),
      pageSize: pageSize.toString()
    });

    const response = await apiClient.get(`/api/templates/search?${params}`);
    return response.data;
  },
};