const API_BASE_URL = "http://localhost:5048/api";

export interface LeadService {
  leadServiceId: number;
  serviceId: number;
  serviceName: string;
  serviceOptionId?: number | null;
  optionCode?: string | null;
  optionName?: string | null;
  requirement?: string | null;
  createdAt?: string | null;
}

export interface LeadStatus {
  leadStatusId: number;
  statusCode: string;
  statusName: string;
  description?: string | null;
  displayOrder?: number | null;
}

export interface Lead {
  leadId: number;
  leadCode: string;
  customerId?: number | null;
  projectId?: number | null;
  leadSource?: string | null;
  customerName: string;
  companyName?: string | null;
  email?: string | null;
  mobile?: string | null;
  projectTypeId?: number | null;
  projectType?: string | null;
  location?: string | null;
  approximateArea?: number | null;
  areaUnit?: string | null;
  requirement?: string | null;
  status: string;
  assignedToUserId?: number | null;
  createdAt: string;
  updatedAt?: string | null;
  services?: LeadService[];
}

export interface Project {
  projectId: number;
  projectCode?: string | null;
  projectName?: string | null;
  customerId?: number | null;
  leadId?: number | null;
  status: string;
  createdAt?: string;
}

export interface Quotation {
  quotationId: number;
  quotationNumber: string;
  leadId?: number | null;
  leadCode?: string | null;
  customerName?: string | null;
  projectId?: number | null;
  revisionNumber?: number | null;
  quotationDate?: string | null;
  validUntil?: string | null;
  subTotal?: number | null;
  discountAmount?: number | null;
  taxAmount?: number | null;
  grandTotal?: number | null;
  status: string;
  notes?: string | null;
  createdByUserId?: number | null;
  createdByUserName?: string | null;
  sentAt?: string | null;
  approvedAt?: string | null;
  createdAt?: string | null;
  updatedAt?: string | null;
  itemCount?: number;
}

export interface CreateQuotationItemRequest {
  projectServiceId?: number | null;
  itemType: string;
  description: string;
  quantity?: number | null;
  unit?: string | null;
  unitPrice: number;
  discountAmount: number;
  taxPercent: number;
  notes?: string | null;
  displayOrder: number;
}

export interface CreateQuotationRequest {
  leadId?: number | null;
  projectId?: number | null;
  quotationDate?: string | null;
  validUntil?: string | null;
  discountAmount?: number;
  taxAmount?: number;
  notes?: string | null;
  createdByUserId?: number | null;
  items: CreateQuotationItemRequest[];
}

export interface ProjectTask {
  projectTaskId: number;
  projectId: number;
  projectStageId?: number | null;
  projectServiceId?: number | null;
  taskName: string;
  description?: string | null;
  status: string;
  progressPercent?: number | null;
  priority?: string | null;
  startDate?: string | null;
  dueDate?: string | null;
  completedDate?: string | null;
  dependsOnProjectTaskId?: number | null;
}


export interface SiteVisit {
  siteVisitId: number;
  leadId?: number | null;
  projectId?: number | null;
  leadCode?: string | null;
  customerName?: string | null;
  companyName?: string | null;
  location?: string | null;
  assignedToUserId?: number | null;
  assignedToUserName?: string | null;
  scheduledAt?: string | null;
  completedAt?: string | null;
  status: string;
  siteCondition?: string | null;
  measurements?: string | null;
  customerNotes?: string | null;
  internalNotes?: string | null;
  createdAt?: string | null;
  updatedAt?: string | null;
}

export interface AdminUser {
  userId: number;
  userName: string;
  email?: string | null;
  mobile?: string | null;
  isActive: boolean;
}

export interface CreateSiteVisitRequest {
  leadId: number;
  assignedToUserId?: number | null;
  scheduledAt: string;
  customerNotes?: string | null;
  internalNotes?: string | null;
}

export interface UpdateLeadStatusResponse {
  leadId: number;
  leadCode: string;
  previousStatus?: string;
  statusCode: string;
  statusName?: string;
  updatedAt: string;
}

async function apiGetCollection<T>(
  endpoint: string
): Promise<T[]> {
  const response = await fetch(
    `${API_BASE_URL}${endpoint}`
  );

  if (!response.ok) {
    throw new Error(
      `API request failed: ${response.status} ${response.statusText}`
    );
  }

  const data = await response.json();

  // API returns a normal array
  if (Array.isArray(data)) {
    return data;
  }

  // Some ASP.NET endpoints return:
  // { value: [...], Count: ... }
  if (data && Array.isArray(data.value)) {
    return data.value;
  }

  throw new Error(
    `Unexpected API response format from ${endpoint}`
  );
}

async function apiGet<T>(endpoint: string): Promise<T> {
  const response = await fetch(
    `${API_BASE_URL}${endpoint}`
  );

  if (!response.ok) {
    const message = await response.text();

    throw new Error(
      message ||
        `API request failed: ${response.status} ${response.statusText}`
    );
  }

  return response.json();
}

export const api = {
  getLeads: () =>
    apiGetCollection<Lead>("/Leads"),

  getLead: (leadId: number) =>
    apiGet<Lead>(`/Leads/${leadId}`),

  getLeadStatuses: () =>
    apiGetCollection<LeadStatus>("/LeadStatuses"),

  updateLeadStatus: async (
    leadId: number,
    statusCode: string
  ): Promise<UpdateLeadStatusResponse> => {
    const response = await fetch(
      `${API_BASE_URL}/Leads/${leadId}/status`,
      {
        method: "PUT",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          statusCode,
        }),
      }
    );

    if (!response.ok) {
      const message = await response.text();

      throw new Error(
        message ||
          `Unable to update lead status (${response.status}).`
      );
    }

    return response.json();
  },


  getSiteVisits: () =>
    apiGetCollection<SiteVisit>("/SiteVisits"),

  getUsers: () =>
    apiGetCollection<AdminUser>("/Users"),

  createSiteVisit: async (
    request: CreateSiteVisitRequest
  ): Promise<SiteVisit> => {
    const response = await fetch(
      `${API_BASE_URL}/SiteVisits`,
      {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify(request),
      }
    );

    if (!response.ok) {
      const message = await response.text();
      throw new Error(
        message ||
          `Unable to schedule site visit (${response.status}).`
      );
    }

    return response.json();
  },


  updateSiteVisitStatus: async (
    siteVisitId: number,
    status: string
  ): Promise<SiteVisit> => {
    const response = await fetch(
      `${API_BASE_URL}/SiteVisits/${siteVisitId}/status`,
      {
        method: "PUT",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({ status }),
      }
    );

    if (!response.ok) {
      const message = await response.text();
      throw new Error(
        message || `Unable to update site visit status (${response.status}).`
      );
    }

    return response.json();
  },

  updateSiteVisitDetails: async (
    siteVisitId: number,
    request: {
      siteCondition?: string | null;
      measurements?: string | null;
      customerNotes?: string | null;
      internalNotes?: string | null;
    }
  ): Promise<SiteVisit> => {
    const response = await fetch(
      `${API_BASE_URL}/SiteVisits/${siteVisitId}/details`,
      {
        method: "PUT",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify(request),
      }
    );

    if (!response.ok) {
      const message = await response.text();
      throw new Error(
        message || `Unable to save site visit details (${response.status}).`
      );
    }

    return response.json();
  },

  getProjects: () =>
    apiGetCollection<Project>("/Projects"),

  getQuotations: () =>
    apiGetCollection<Quotation>("/Quotations"),

  createQuotation: async (
    request: CreateQuotationRequest
  ): Promise<Quotation> => {
    const response = await fetch(`${API_BASE_URL}/Quotations`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      const message = await response.text();
      throw new Error(
        message || `Unable to create quotation (${response.status}).`
      );
    }

    return response.json();
  },

  updateQuotationStatus: async (
    quotationId: number,
    status: string
  ) => {
    const response = await fetch(
      `${API_BASE_URL}/Quotations/${quotationId}/status`,
      {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ status }),
      }
    );

    if (!response.ok) {
      const message = await response.text();
      throw new Error(
        message ||
          `Unable to update quotation status (${response.status}).`
      );
    }

    return response.json();
  },

  getProjectTasks: (projectId: number) =>
    apiGetCollection<ProjectTask>(
      `/ProjectTasks/project/${projectId}`
    ),

  getServices: () =>
    apiGetCollection<unknown>("/Services"),

  getProjectTypes: () =>
    apiGetCollection<unknown>("/ProjectTypes"),
};
