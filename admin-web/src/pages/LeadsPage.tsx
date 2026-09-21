import { useEffect, useMemo, useState } from "react";
import { api } from "../services/api";
import type { Lead, LeadService, LeadStatus } from "../services/api";

interface LeadsPageProps {
  onNewLead: () => void;
}

function LeadsPage({ onNewLead }: LeadsPageProps) {
  const [leads, setLeads] = useState<Lead[]>([]);
  const [leadStatuses, setLeadStatuses] = useState<LeadStatus[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  const [search, setSearch] = useState("");
  const [statusFilter, setStatusFilter] = useState("ALL");

  const [selectedLead, setSelectedLead] = useState<Lead | null>(null);
  const [selectedStatus, setSelectedStatus] = useState("");
  const [savingStatus, setSavingStatus] = useState(false);
  const [detailError, setDetailError] = useState("");
  const [detailLoading, setDetailLoading] = useState(false);

  useEffect(() => {
    loadLeads();
    loadLeadStatuses();
  }, []);

  async function loadLeads() {
    try {
      setLoading(true);
      setError("");

      const data = await api.getLeads();
      setLeads(data);

      if (selectedLead) {
        const refreshed = data.find(
          (lead) => lead.leadId === selectedLead.leadId
        );
        if (refreshed) {
          setSelectedLead(refreshed);
          setSelectedStatus(refreshed.status);
        }
      }
    } catch (err) {
      console.error(err);
      setError("Unable to load leads from API.");
    } finally {
      setLoading(false);
    }
  }

  async function loadLeadStatuses() {
    try {
      const data = await api.getLeadStatuses();
      setLeadStatuses(data);
    } catch (err) {
      console.error(err);
      // Statuses are useful for the dropdown, but the existing lead list
      // remains usable even if this separate request fails.
    }
  }

  async function openLeadDetails(lead: Lead) {
    setSelectedLead(lead);
    setSelectedStatus(lead.status);
    setDetailError("");

    try {
      setDetailLoading(true);
      const fullLead = await api.getLead(lead.leadId);
      setSelectedLead(fullLead);
      setSelectedStatus(fullLead.status);
    } catch (err) {
      console.error(err);
      // The list response already contains the main lead information,
      // so keep the selected lead open if the detail request fails.
    } finally {
      setDetailLoading(false);
    }
  }

  function closeLeadDetails() {
    if (savingStatus) return;
    setSelectedLead(null);
    setSelectedStatus("");
    setDetailError("");
  }

  async function updateSelectedLeadStatus() {
    if (!selectedLead || !selectedStatus) return;

    if (selectedStatus === selectedLead.status) {
      return;
    }

    try {
      setSavingStatus(true);
      setDetailError("");

      const updated = await api.updateLeadStatus(
        selectedLead.leadId,
        selectedStatus
      );

      setLeads((current) =>
        current.map((lead) =>
          lead.leadId === updated.leadId
            ? { ...lead, status: updated.statusCode, updatedAt: updated.updatedAt }
            : lead
        )
      );

      setSelectedLead((current) =>
        current
          ? {
              ...current,
              status: updated.statusCode,
              updatedAt: updated.updatedAt,
            }
          : current
      );
    } catch (err) {
      console.error(err);
      setDetailError(
        err instanceof Error
          ? err.message
          : "Unable to update lead status."
      );
    } finally {
      setSavingStatus(false);
    }
  }

  const statuses = useMemo(() => {
    if (leadStatuses.length > 0) {
      return [
        "ALL",
        ...leadStatuses.map((status) => status.statusCode),
      ];
    }

    const values = leads
      .map((lead) => lead.status)
      .filter(Boolean);

    return ["ALL", ...Array.from(new Set(values))];
  }, [leadStatuses, leads]);

  const filteredLeads = useMemo(() => {
    const searchText = search.trim().toLowerCase();

    return leads.filter((lead) => {
      const matchesSearch =
        !searchText ||
        lead.leadCode.toLowerCase().includes(searchText) ||
        lead.customerName.toLowerCase().includes(searchText) ||
        (lead.companyName || "").toLowerCase().includes(searchText) ||
        (lead.mobile || "").toLowerCase().includes(searchText) ||
        (lead.location || "").toLowerCase().includes(searchText);

      const matchesStatus =
        statusFilter === "ALL" ||
        lead.status === statusFilter;

      return matchesSearch && matchesStatus;
    });
  }, [leads, search, statusFilter]);

  function formatDate(value?: string | null) {
    if (!value) return "-";

    return new Date(value).toLocaleDateString("en-IN", {
      day: "2-digit",
      month: "short",
      year: "numeric",
    });
  }

  function formatDateTime(value?: string | null) {
    if (!value) return "-";

    return new Date(value).toLocaleString("en-IN", {
      day: "2-digit",
      month: "short",
      year: "numeric",
      hour: "2-digit",
      minute: "2-digit",
    });
  }

  function displayStatus(status: string) {
    return status.replaceAll("_", " ");
  }

  function getStatusClass(status: string) {
    switch (status.toUpperCase()) {
      case "NEW":
        return "status-new";

      case "CONTACTED":
        return "status-contacted";

      case "SITE_VISIT_REQUIRED":
      case "SITE_VISIT_SCHEDULED":
        return "status-site";

      case "QUALIFIED":
        return "status-qualified";

      case "QUOTATION_PREPARATION":
      case "QUOTATION_SENT":
        return "status-quotation";

      case "CONVERTED":
      case "WON":
        return "status-converted";

      case "LOST":
        return "status-lost";

      default:
        return "status-default";
    }
  }

  function renderService(service: LeadService) {
    return (
      <div
        key={service.leadServiceId}
        style={{
          padding: "12px 14px",
          border: "1px solid #eee8f5",
          borderRadius: 10,
          background: "#fbfaff",
        }}
      >
        <div
          style={{
            display: "flex",
            justifyContent: "space-between",
            gap: 12,
            alignItems: "flex-start",
          }}
        >
          <div>
            <strong style={{ color: "#251b35", fontSize: 13 }}>
              {service.serviceName}
            </strong>

            {service.optionName && (
              <div
                style={{
                  marginTop: 4,
                  color: "#6d28d9",
                  fontSize: 11,
                  fontWeight: 700,
                }}
              >
                {service.optionName}
              </div>
            )}
          </div>

          {service.optionCode && (
            <span
              style={{
                fontSize: 10,
                fontWeight: 800,
                color: "#7c6f88",
                background: "#f1edf7",
                borderRadius: 6,
                padding: "4px 7px",
                whiteSpace: "nowrap",
              }}
            >
              {service.optionCode}
            </span>
          )}
        </div>

        {service.requirement && (
          <p
            style={{
              margin: "8px 0 0",
              color: "#74697d",
              fontSize: 11,
              lineHeight: 1.5,
            }}
          >
            {service.requirement}
          </p>
        )}
      </div>
    );
  }

  return (
    <div className="module-page">
      <div className="module-header">
        <div>
          <h2>Leads</h2>
          <p>
            Manage customer enquiries and sales opportunities.
          </p>
        </div>

        <button
          className="primary-button module-action"
          onClick={onNewLead}
        >
          + New Lead
        </button>
      </div>

      <div className="lead-summary">
        <div className="summary-card">
          <span>Total Leads</span>
          <strong>{leads.length}</strong>
        </div>

        <div className="summary-card">
          <span>New</span>
          <strong>
            {
              leads.filter(
                (lead) =>
                  lead.status.toUpperCase() === "NEW"
              ).length
            }
          </strong>
        </div>

        <div className="summary-card">
          <span>Qualified</span>
          <strong>
            {
              leads.filter(
                (lead) =>
                  lead.status.toUpperCase() === "QUALIFIED"
              ).length
            }
          </strong>
        </div>

        <div className="summary-card">
          <span>Converted</span>
          <strong>
            {
              leads.filter((lead) =>
                ["CONVERTED", "WON"].includes(
                  lead.status.toUpperCase()
                )
              ).length
            }
          </strong>
        </div>
      </div>

      <div className="lead-toolbar">
        <div className="search-box">
          <span>⌕</span>

          <input
            type="text"
            placeholder="Search by customer, company, lead code..."
            value={search}
            onChange={(event) =>
              setSearch(event.target.value)
            }
          />
        </div>

        <select
          value={statusFilter}
          onChange={(event) =>
            setStatusFilter(event.target.value)
          }
        >
          {statuses.map((status) => (
            <option key={status} value={status}>
              {status === "ALL"
                ? "All Statuses"
                : displayStatus(status)}
            </option>
          ))}
        </select>

        <button
          className="secondary-button"
          onClick={loadLeads}
        >
          ↻ Refresh
        </button>
      </div>

      <div className="leads-panel">
        {loading ? (
          <div className="page-empty">
            <div className="empty-icon">◌</div>
            <h3>Loading leads...</h3>
            <p>Reading leads from the database.</p>
          </div>
        ) : error ? (
          <div className="page-empty">
            <div className="empty-icon">!</div>
            <h3>{error}</h3>

            <button
              className="secondary-button"
              onClick={loadLeads}
            >
              Try Again
            </button>
          </div>
        ) : filteredLeads.length === 0 ? (
          <div className="page-empty">
            <div className="empty-icon">◉</div>
            <h3>No leads found</h3>
            <p>
              Try changing the search or status filter.
            </p>
          </div>
        ) : (
          <div className="table-container">
            <table className="data-table">
              <thead>
                <tr>
                  <th>Lead</th>
                  <th>Customer</th>
                  <th>Company</th>
                  <th>Location</th>
                  <th>Status</th>
                  <th>Created</th>
                  <th>Action</th>
                </tr>
              </thead>

              <tbody>
                {filteredLeads.map((lead) => (
                  <tr key={lead.leadId}>
                    <td>
                      <strong className="lead-code">
                        {lead.leadCode}
                      </strong>
                    </td>

                    <td>
                      <div className="table-customer">
                        <div className="table-avatar">
                          {lead.customerName
                            .charAt(0)
                            .toUpperCase()}
                        </div>

                        <div>
                          <strong>
                            {lead.customerName}
                          </strong>

                          <span>
                            {lead.mobile ||
                              lead.email ||
                              "-"}
                          </span>
                        </div>
                      </div>
                    </td>

                    <td>{lead.companyName || "-"}</td>

                    <td>{lead.location || "-"}</td>

                    <td>
                      <span
                        className={`lead-status-badge ${getStatusClass(
                          lead.status
                        )}`}
                      >
                        {displayStatus(lead.status)}
                      </span>
                    </td>

                    <td>{formatDate(lead.createdAt)}</td>

                    <td>
                      <button
                        className="table-action"
                        onClick={() => openLeadDetails(lead)}
                      >
                        View
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {selectedLead && (
        <div
          role="presentation"
          onClick={closeLeadDetails}
          style={{
            position: "fixed",
            inset: 0,
            zIndex: 1000,
            background: "rgba(24, 16, 34, 0.48)",
            display: "flex",
            justifyContent: "flex-end",
          }}
        >
          <aside
            role="dialog"
            aria-modal="true"
            aria-label="Lead details"
            onClick={(event) => event.stopPropagation()}
            style={{
              width: "min(560px, 94vw)",
              height: "100%",
              overflowY: "auto",
              background: "#fff",
              boxShadow: "-12px 0 40px rgba(34, 20, 52, 0.18)",
              padding: "28px",
            }}
          >
            <div
              style={{
                display: "flex",
                justifyContent: "space-between",
                alignItems: "flex-start",
                gap: 16,
                marginBottom: 24,
              }}
            >
              <div>
                <div
                  style={{
                    fontSize: 10,
                    color: "#806f91",
                    fontWeight: 800,
                    letterSpacing: "0.12em",
                    textTransform: "uppercase",
                  }}
                >
                  Lead Details
                </div>

                <h2
                  style={{
                    margin: "7px 0 5px",
                    color: "#20152c",
                    fontSize: 23,
                  }}
                >
                  {selectedLead.customerName}
                </h2>

                <div
                  style={{
                    color: "#775f88",
                    fontSize: 11,
                    fontWeight: 700,
                  }}
                >
                  {selectedLead.leadCode}
                </div>
              </div>

              <button
                type="button"
                onClick={closeLeadDetails}
                disabled={savingStatus}
                style={{
                  width: 36,
                  height: 36,
                  border: "1px solid #e5deeb",
                  borderRadius: 9,
                  background: "#fff",
                  cursor: savingStatus ? "not-allowed" : "pointer",
                  color: "#65576e",
                  fontSize: 18,
                }}
              >
                ×
              </button>
            </div>

            {detailLoading && (
              <div
                style={{
                  marginBottom: 16,
                  padding: "10px 12px",
                  borderRadius: 8,
                  background: "#f7f3fb",
                  color: "#6d28d9",
                  fontSize: 11,
                  fontWeight: 700,
                }}
              >
                Loading latest lead details...
              </div>
            )}

            {detailError && (
              <div
                style={{
                  marginBottom: 16,
                  padding: "11px 13px",
                  borderRadius: 8,
                  background: "#fff3f3",
                  border: "1px solid #f3cccc",
                  color: "#a33b3b",
                  fontSize: 11,
                  lineHeight: 1.5,
                }}
              >
                {detailError}
              </div>
            )}

            <div
              style={{
                display: "grid",
                gridTemplateColumns: "1fr 1fr",
                gap: 12,
                marginBottom: 22,
              }}
            >
              {[
                ["Company", selectedLead.companyName || "-"],
                ["Mobile", selectedLead.mobile || "-"],
                ["Email", selectedLead.email || "-"],
                ["Location", selectedLead.location || "-"],
                ["Project Type", selectedLead.projectType || "-"],
                [
                  "Approx. Area",
                  selectedLead.approximateArea
                    ? `${selectedLead.approximateArea} ${
                        selectedLead.areaUnit || ""
                      }`.trim()
                    : "-",
                ],
                ["Lead Source", selectedLead.leadSource || "-"],
                ["Created", formatDateTime(selectedLead.createdAt)],
              ].map(([label, value]) => (
                <div
                  key={label}
                  style={{
                    padding: "12px 14px",
                    border: "1px solid #eee8f5",
                    borderRadius: 10,
                    background: "#fcfbfe",
                  }}
                >
                  <div
                    style={{
                      color: "#95879e",
                      fontSize: 9,
                      fontWeight: 800,
                      textTransform: "uppercase",
                      letterSpacing: "0.08em",
                      marginBottom: 5,
                    }}
                  >
                    {label}
                  </div>

                  <div
                    style={{
                      color: "#302438",
                      fontSize: 12,
                      fontWeight: 700,
                      lineHeight: 1.4,
                      wordBreak: "break-word",
                    }}
                  >
                    {value}
                  </div>
                </div>
              ))}
            </div>

            <section style={{ marginBottom: 22 }}>
              <div
                style={{
                  fontSize: 12,
                  fontWeight: 800,
                  color: "#261a31",
                  marginBottom: 9,
                }}
              >
                Requirement
              </div>

              <div
                style={{
                  padding: "13px 14px",
                  borderRadius: 10,
                  background: "#f9f7fc",
                  color: "#64596c",
                  fontSize: 12,
                  lineHeight: 1.6,
                  whiteSpace: "pre-wrap",
                }}
              >
                {selectedLead.requirement ||
                  "No overall requirement provided."}
              </div>
            </section>

            <section style={{ marginBottom: 24 }}>
              <div
                style={{
                  fontSize: 12,
                  fontWeight: 800,
                  color: "#261a31",
                  marginBottom: 9,
                }}
              >
                Selected Services
              </div>

              {selectedLead.services &&
              selectedLead.services.length > 0 ? (
                <div
                  style={{
                    display: "grid",
                    gap: 8,
                  }}
                >
                  {selectedLead.services.map(renderService)}
                </div>
              ) : (
                <div
                  style={{
                    padding: "13px 14px",
                    borderRadius: 10,
                    background: "#f9f7fc",
                    color: "#766b7d",
                    fontSize: 11,
                  }}
                >
                  No services were selected.
                </div>
              )}
            </section>

            <section
              style={{
                borderTop: "1px solid #eee8f5",
                paddingTop: 20,
              }}
            >
              <div
                style={{
                  display: "flex",
                  justifyContent: "space-between",
                  alignItems: "center",
                  gap: 12,
                  marginBottom: 10,
                }}
              >
                <div>
                  <div
                    style={{
                      fontSize: 12,
                      fontWeight: 800,
                      color: "#261a31",
                    }}
                  >
                    Lead Status
                  </div>

                  <div
                    style={{
                      marginTop: 4,
                      color: "#8b7d94",
                      fontSize: 10,
                    }}
                  >
                    Update the enquiry status as the sales process moves
                    forward.
                  </div>
                </div>

                <span
                  className={`lead-status-badge ${getStatusClass(
                    selectedLead.status
                  )}`}
                >
                  {displayStatus(selectedLead.status)}
                </span>
              </div>

              <div
                style={{
                  display: "flex",
                  gap: 10,
                  alignItems: "center",
                }}
              >
                <select
                  value={selectedStatus}
                  onChange={(event) =>
                    setSelectedStatus(event.target.value)
                  }
                  disabled={savingStatus}
                  style={{
                    flex: 1,
                    minHeight: 42,
                    border: "1px solid #ddd5e6",
                    borderRadius: 9,
                    padding: "0 12px",
                    background: "#fff",
                    color: "#302438",
                    fontFamily: "inherit",
                    fontSize: 12,
                    fontWeight: 700,
                  }}
                >
                  {leadStatuses.length > 0
                    ? leadStatuses.map((status) => (
                        <option
                          key={status.leadStatusId}
                          value={status.statusCode}
                        >
                          {status.statusName}
                        </option>
                      ))
                    : statuses
                        .filter((status) => status !== "ALL")
                        .map((status) => (
                          <option key={status} value={status}>
                            {displayStatus(status)}
                          </option>
                        ))}
                </select>

                <button
                  type="button"
                  className="primary-button"
                  onClick={updateSelectedLeadStatus}
                  disabled={
                    savingStatus ||
                    selectedStatus === selectedLead.status
                  }
                  style={{
                    minHeight: 42,
                    minWidth: 130,
                    cursor:
                      savingStatus ||
                      selectedStatus === selectedLead.status
                        ? "not-allowed"
                        : "pointer",
                    opacity:
                      savingStatus ||
                      selectedStatus === selectedLead.status
                        ? 0.55
                        : 1,
                  }}
                >
                  {savingStatus
                    ? "Saving..."
                    : "Update Status"}
                </button>
              </div>
            </section>
          </aside>
        </div>
      )}
    </div>
  );
}

export default LeadsPage;
