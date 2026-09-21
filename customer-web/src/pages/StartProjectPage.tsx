import { useEffect, useState } from "react";
import "./StartProjectPage.css";

const API_BASE = "http://localhost:5048/api";

interface ProjectType {
  projectTypeId: number;
  projectTypeName: string;
}

interface ServiceSubdivision {
  serviceOptionId: number;
  optionCode: string;
  optionName: string;
}

interface CatalogService {
  serviceId: number;
  serviceName: string;
  subdivisions: ServiceSubdivision[];
}

interface ServiceType {
  serviceTypeId: number;
  typeCode: string;
  typeName: string;
  services: CatalogService[];
}

interface SelectedService {
  serviceId: number;
  serviceOptionId?: number;
  requirement?: string;
}

interface FormData {
  customerName: string;
  companyName: string;
  email: string;
  mobile: string;
  projectTypeId: string;
  location: string;
  approximateArea: string;
  areaUnit: string;
  requirement: string;
}

const initialForm: FormData = {
  customerName: "",
  companyName: "",
  email: "",
  mobile: "",
  projectTypeId: "",
  location: "",
  approximateArea: "",
  areaUnit: "sq.ft",
  requirement: "",
};

export default function StartProjectPage() {
  const [projectTypes, setProjectTypes] = useState<ProjectType[]>([]);
  const [serviceTypes, setServiceTypes] = useState<ServiceType[]>([]);
  const [activeServiceTypeId, setActiveServiceTypeId] = useState<number | null>(null);

  const [form, setForm] = useState<FormData>(initialForm);

  const [selectedServices, setSelectedServices] = useState<
    SelectedService[]
  >([]);

  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);

  const [error, setError] = useState("");
  const [success, setSuccess] = useState<{
    leadId: number;
    leadCode: string;
  } | null>(null);

  useEffect(() => {
    loadMasterData();
  }, []);

  async function loadMasterData() {
    try {
      setLoading(true);
      setError("");

      const [projectTypesResponse, catalogResponse] =
        await Promise.all([
          fetch(`${API_BASE}/ProjectTypes`),
          fetch(`${API_BASE}/ServiceCatalog`),
        ]);

      if (!projectTypesResponse.ok) {
        throw new Error("Unable to load project types.");
      }

      if (!catalogResponse.ok) {
        throw new Error("Unable to load service catalogue.");
      }

      const projectTypesData = await projectTypesResponse.json();
      const catalogData = await catalogResponse.json();

      setProjectTypes(
        Array.isArray(projectTypesData)
          ? projectTypesData
          : projectTypesData.value ?? []
      );

      const loadedCatalog: ServiceType[] = Array.isArray(catalogData)
        ? catalogData
        : catalogData.value ?? [];

      // Coordination records are internal workflow records. They remain in
      // the database but are not presented as normal customer-selectable
      // services or subdivisions.
      //
      // The database also contains legacy coordination services under the
      // DESIGN type (for example Project Coordination, Material Coordination,
      // Resource Coordination and Quality Coordination). Filter those here
      // rather than changing the underlying catalogue data.
      const customerCatalog: ServiceType[] = loadedCatalog
        .filter(
          (type) => type.typeCode?.toUpperCase() !== "COORDINATION"
        )
        .map((type) => ({
          ...type,
          services: (type.services ?? [])
            .filter(
              (service) =>
                !service.serviceName
                  ?.toUpperCase()
                  .includes("COORDINATION")
            )
            .map((service) => ({
              ...service,
              subdivisions: (service.subdivisions ?? []).filter(
                (option) =>
                  option.optionCode?.toUpperCase() !== "COORDINATION"
              ),
            })),
        }))
        .filter((type) => type.services.length > 0);

      setServiceTypes(customerCatalog);
      setActiveServiceTypeId(customerCatalog[0]?.serviceTypeId ?? null);
    } catch (err) {
      setError(
        err instanceof Error
          ? err.message
          : "Unable to load project information."
      );
    } finally {
      setLoading(false);
    }
  }

  function updateField(
    field: keyof FormData,
    value: string
  ) {
    setForm((current) => ({
      ...current,
      [field]: value,
    }));
  }

  function toggleService(service: CatalogService) {
    setSelectedServices((current) => {
      const exists = current.some(
        (item) => item.serviceId === service.serviceId
      );

      if (exists) {
        return current.filter(
          (item) => item.serviceId !== service.serviceId
        );
      }

      // Most customer-facing services have one subdivision. Select it
      // automatically so the customer does not have to repeat an internal
      // classification choice. If a service has multiple subdivisions,
      // the customer can choose one after selecting the service.
      const defaultSubdivision =
        service.subdivisions?.length === 1
          ? service.subdivisions[0]
          : undefined;

      return [
        ...current,
        {
          serviceId: service.serviceId,
          serviceOptionId: defaultSubdivision?.serviceOptionId,
        },
      ];
    });
  }

  function selectOption(serviceId: number, serviceOptionId: number) {
    setSelectedServices((current) =>
      current.map((item) =>
        item.serviceId === serviceId
          ? { ...item, serviceOptionId: serviceOptionId || undefined }
          : item
      )
    );
  }

  function updateServiceRequirement(serviceId: number, requirement: string) {
    setSelectedServices((current) =>
      current.map((item) =>
        item.serviceId === serviceId ? { ...item, requirement } : item
      )
    );
  }

  function getSelectedService(serviceId: number) {
    return selectedServices.find((item) => item.serviceId === serviceId);
  }

  function getActiveServices() {
    return (
      serviceTypes.find((type) => type.serviceTypeId === activeServiceTypeId)
        ?.services ?? []
    );
  }

  async function submitEnquiry(
    event: React.FormEvent<HTMLFormElement>
  ) {
    event.preventDefault();

    setError("");
    setSuccess(null);

    if (!form.customerName.trim()) {
      setError("Please enter your name.");
      return;
    }

    if (!form.mobile.trim()) {
      setError("Please enter your mobile number.");
      return;
    }

    if (!form.location.trim()) {
      setError("Please enter the project location.");
      return;
    }

    if (!form.projectTypeId) {
      setError("Please select the project type.");
      return;
    }

    if (selectedServices.length === 0) {
      setError(
        "Please select at least one service you are interested in."
      );
      return;
    }

    try {
      setSubmitting(true);

      const payload = {
        leadSource: "CUSTOMER_WEBSITE",
        customerName: form.customerName.trim(),
        companyName: form.companyName.trim() || null,
        email: form.email.trim() || null,
        mobile: form.mobile.trim(),
        projectTypeId: Number(form.projectTypeId),
        location: form.location.trim(),
        approximateArea: form.approximateArea
          ? Number(form.approximateArea)
          : null,
        areaUnit: form.areaUnit || null,
        requirement: form.requirement.trim() || null,

        services: selectedServices.map((service) => ({
          serviceId: service.serviceId,
          serviceOptionId:
            service.serviceOptionId ?? null,
          requirement:
            service.requirement?.trim() || null,
        })),
      };

      const response = await fetch(`${API_BASE}/Leads`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify(payload),
      });

      const responseText = await response.text();

      if (!response.ok) {
        throw new Error(
          responseText ||
            "Unable to submit your enquiry."
        );
      }

      const result = JSON.parse(responseText);

      setSuccess({
        leadId: result.leadId,
        leadCode: result.leadCode,
      });

      setForm(initialForm);
      setSelectedServices([]);

      window.scrollTo({
        top: 0,
        behavior: "smooth",
      });
    } catch (err) {
      setError(
        err instanceof Error
          ? err.message
          : "Unable to submit your enquiry."
      );
    } finally {
      setSubmitting(false);
    }
  }

  if (loading) {
    return (
      <div className="start-page-loading">
        <div className="start-page-spinner"></div>
        <p>Loading project options...</p>
      </div>
    );
  }

  return (
    <div className="start-project-page">

      <section className="start-project-hero">

        <div className="start-project-container">

          <div className="start-project-eyebrow">
            START YOUR PROJECT
          </div>

          <h1>
            Tell us about
            <span> your space.</span>
          </h1>

          <p>
            Share a few details about your project.
            You don't need to know everything yet —
            we'll help you through the next steps.
          </p>

        </div>

      </section>

      <section className="start-project-content">

        <div className="start-project-container">

          {success && (
            <div className="success-message">

              <div className="success-icon">
                ✓
              </div>

              <div>
                <strong>
                  Thank you. Your enquiry has been received.
                </strong>

                <p>
                  Your enquiry reference is{" "}
                  <b>{success.leadCode}</b>.
                  Our team will review your requirements
                  and contact you.
                </p>
              </div>

            </div>
          )}

          {error && (
            <div className="error-message">
              <strong>Please check the following:</strong>
              <span>{error}</span>
            </div>
          )}

          {!success && (
            <form
              className="project-form"
              onSubmit={submitEnquiry}
            >

              {/* ====================================
                  CUSTOMER DETAILS
              ==================================== */}

              <div className="form-section">

                <div className="form-section-heading">
                  <span>01</span>

                  <div>
                    <h2>Your Details</h2>
                    <p>
                      Tell us how we can contact you.
                    </p>
                  </div>
                </div>

                <div className="form-grid">

                  <div className="form-field">

                    <label>
                      Your Name <b>*</b>
                    </label>

                    <input
                      type="text"
                      value={form.customerName}
                      onChange={(e) =>
                        updateField(
                          "customerName",
                          e.target.value
                        )
                      }
                      placeholder="Enter your name"
                      required
                    />

                  </div>

                  <div className="form-field">

                    <label>
                      Company / Organisation
                    </label>

                    <input
                      type="text"
                      value={form.companyName}
                      onChange={(e) =>
                        updateField(
                          "companyName",
                          e.target.value
                        )
                      }
                      placeholder="Optional"
                    />

                  </div>

                  <div className="form-field">

                    <label>
                      Mobile Number <b>*</b>
                    </label>

                    <input
                      type="tel"
                      value={form.mobile}
                      onChange={(e) =>
                        updateField(
                          "mobile",
                          e.target.value
                        )
                      }
                      placeholder="+91"
                      required
                    />

                  </div>

                  <div className="form-field">

                    <label>
                      Email
                    </label>

                    <input
                      type="email"
                      value={form.email}
                      onChange={(e) =>
                        updateField(
                          "email",
                          e.target.value
                        )
                      }
                      placeholder="you@example.com"
                    />

                  </div>

                </div>

              </div>

              {/* ====================================
                  PROJECT DETAILS
              ==================================== */}

              <div className="form-section">

                <div className="form-section-heading">
                  <span>02</span>

                  <div>
                    <h2>About Your Space</h2>
                    <p>
                      Tell us what kind of project
                      you are planning.
                    </p>
                  </div>
                </div>

                <div className="form-grid">

                  <div className="form-field">

                    <label>
                      Project Type <b>*</b>
                    </label>

                    <select
                      value={form.projectTypeId}
                      onChange={(e) =>
                        updateField(
                          "projectTypeId",
                          e.target.value
                        )
                      }
                      required
                    >

                      <option value="">
                        Select project type
                      </option>

                      {projectTypes.map((type) => (
                        <option
                          key={type.projectTypeId}
                          value={type.projectTypeId}
                        >
                          {type.projectTypeName}
                        </option>
                      ))}

                    </select>

                  </div>

                  <div className="form-field">

                    <label>
                      Project Location <b>*</b>
                    </label>

                    <input
                      type="text"
                      value={form.location}
                      onChange={(e) =>
                        updateField(
                          "location",
                          e.target.value
                        )
                      }
                      placeholder="City / Area"
                      required
                    />

                  </div>

                  <div className="form-field">

                    <label>
                      Approximate Area
                    </label>

                    <div className="area-input">

                      <input
                        type="number"
                        min="0"
                        value={form.approximateArea}
                        onChange={(e) =>
                          updateField(
                            "approximateArea",
                            e.target.value
                          )
                        }
                        placeholder="e.g. 2500"
                      />

                      <select
                        value={form.areaUnit}
                        onChange={(e) =>
                          updateField(
                            "areaUnit",
                            e.target.value
                          )
                        }
                      >
                        <option value="sq.ft">
                          sq.ft
                        </option>

                        <option value="sq.m">
                          sq.m
                        </option>

                        <option value="cent">
                          cent
                        </option>

                        <option value="acre">
                          acre
                        </option>
                      </select>

                    </div>

                  </div>

                </div>

              </div>

              {/* ====================================
                  SERVICES
              ==================================== */}

              <div className="form-section">
                <div className="form-section-heading">
                  <span>03</span>
                  <div>
                    <h2>What Do You Need?</h2>
                    <p>
                      Choose a service type, select the services you need,
                      and tell us anything specific about each one.
                    </p>
                  </div>
                </div>

                <div className="service-type-tabs">
                  {serviceTypes.map((type) => (
                    <button
                      key={type.serviceTypeId}
                      type="button"
                      className={
                        activeServiceTypeId === type.serviceTypeId
                          ? "active"
                          : ""
                      }
                      onClick={() => setActiveServiceTypeId(type.serviceTypeId)}
                    >
                      {type.typeName}
                    </button>
                  ))}
                </div>

                <div className="service-selection-help">
                  <span>Step 1</span>
                  <p>Select one or more services you need. Selected services will open additional details below.</p>
                </div>

                <div className="customer-service-grid">
                  {getActiveServices().map((service) => {
                    const selected = getSelectedService(service.serviceId);
                    const subdivisions = service.subdivisions ?? [];
                    const selectedSubdivision = subdivisions.find(
                      (option) =>
                        option.serviceOptionId === selected?.serviceOptionId
                    );

                    return (
                      <div
                        className={`customer-service-card ${
                          selected ? "selected" : ""
                        }`}
                        key={service.serviceId}
                      >
                        <button
                          type="button"
                          className="service-select-button"
                          onClick={() => toggleService(service)}
                          aria-pressed={Boolean(selected)}
                        >
                          <span className="service-check">
                            {selected ? "✓" : ""}
                          </span>
                          <span className="service-name">
                            {service.serviceName}
                          </span>
                        </button>

                        {selected && (
                          <div className="service-details">
                            <div className="service-detail-heading">
                              <span>Step 2</span>
                              <strong>Service subdivision</strong>
                            </div>

                            {subdivisions.length === 1 ? (
                              <div className="service-subdivision-display">
                                {selectedSubdivision?.optionName ??
                                  subdivisions[0].optionName}
                              </div>
                            ) : subdivisions.length > 1 ? (
                              <select
                                className="service-subdivision-select"
                                aria-label={`Subdivision for ${service.serviceName}`}
                                value={selected.serviceOptionId ?? ""}
                                onChange={(e) =>
                                  selectOption(
                                    service.serviceId,
                                    Number(e.target.value)
                                  )
                                }
                              >
                                <option value="">
                                  Select a subdivision
                                </option>
                                {subdivisions.map((option) => (
                                  <option
                                    key={option.serviceOptionId}
                                    value={option.serviceOptionId}
                                  >
                                    {option.optionName}
                                  </option>
                                ))}
                              </select>
                            ) : (
                              <div className="service-subdivision-display muted">
                                We will discuss the appropriate service
                                arrangement with you.
                              </div>
                            )}

                            <div className="service-detail-heading requirement-heading">
                              <span>Step 3</span>
                              <strong>Specific requirement</strong>
                            </div>

                            <textarea
                              className="service-requirement-input"
                              value={selected.requirement ?? ""}
                              onChange={(e) =>
                                updateServiceRequirement(
                                  service.serviceId,
                                  e.target.value
                                )
                              }
                              placeholder={`Tell us anything specific about ${service.serviceName.toLowerCase()}...`}
                              rows={3}
                            />
                          </div>
                        )}
                      </div>
                    );
                  })}
                </div>

                {selectedServices.length > 0 && (
                  <div className="selected-services-summary">
                    <strong>{selectedServices.length} service(s) selected</strong>
                    <span> You can select services from more than one category.</span>
                  </div>
                )}
              </div>

              {/* ====================================
                  REQUIREMENT
              ==================================== */}

              <div className="form-section">

                <div className="form-section-heading">
                  <span>04</span>

                  <div>
                    <h2>Tell Us More</h2>
                    <p>
                      Describe what you have in mind.
                    </p>
                  </div>
                </div>

                <div className="form-field">

                  <label>
                    Project Requirement
                  </label>

                  <textarea
                    value={form.requirement}
                    onChange={(e) =>
                      updateField(
                        "requirement",
                        e.target.value
                      )
                    }
                    rows={7}
                    placeholder="Tell us about your project, what you want to achieve, any existing drawings, renovation requirements, preferred materials, timeline, or anything else that may help us understand your requirement."
                  />

                </div>

              </div>

              {/* ====================================
                  SUBMIT
              ==================================== */}

              <div className="form-submit-area">

                <div>

                  <strong>
                    Ready to start?
                  </strong>

                  <p>
                    Submit your enquiry and our team
                    will contact you to discuss the next step.
                  </p>

                </div>

                <button
                  type="submit"
                  className="project-submit-button"
                  disabled={submitting}
                >
                  {submitting
                    ? "Submitting..."
                    : "Submit Project Enquiry →"}
                </button>

              </div>

            </form>
          )}

        </div>

      </section>

    </div>
  );
}