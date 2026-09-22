import { useEffect, useMemo, useState } from "react";
import type { FormEvent } from "react";
import type {
  AdminUser,
  Lead,
  SiteVisit,
} from "../services/api";
import { api } from "../services/api";
import "./SiteVisitsPage.css";

function SiteVisitsPage() {
  const [siteVisits, setSiteVisits] = useState<SiteVisit[]>([]);
  const [leads, setLeads] = useState<Lead[]>([]);
  const [users, setUsers] = useState<AdminUser[]>([]);

  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");
  const [formError, setFormError] = useState("");
  const [showForm, setShowForm] = useState(false);
  const [selectedVisit, setSelectedVisit] = useState<SiteVisit | null>(null);
  const [showDetails, setShowDetails] = useState(false);

  const [leadId, setLeadId] = useState("");
  const [assignedToUserId, setAssignedToUserId] = useState("");
  const [scheduledAt, setScheduledAt] = useState("");
  const [customerNotes, setCustomerNotes] = useState("");
  const [internalNotes, setInternalNotes] = useState("");

  const [siteCondition, setSiteCondition] = useState("");
  const [measurements, setMeasurements] = useState("");
  const [detailsCustomerNotes, setDetailsCustomerNotes] = useState("");
  const [detailsInternalNotes, setDetailsInternalNotes] = useState("");
  const [detailError, setDetailError] = useState("");

  useEffect(() => {
    loadPage();
  }, []);

  async function loadPage() {
    try {
      setLoading(true);
      setError("");

      const [visits, leadData, userData] = await Promise.all([
        api.getSiteVisits(),
        api.getLeads(),
        api.getUsers(),
      ]);

      setSiteVisits(visits);
      setLeads(leadData);
      setUsers(userData.filter((user) => user.isActive));

      const defaultUser = userData.find(
        (user) => user.isActive && user.userName.toLowerCase() === "admin"
      );

      if (defaultUser && !assignedToUserId) {
        setAssignedToUserId(String(defaultUser.userId));
      }
    } catch (err) {
      console.error(err);
      setError(
        err instanceof Error ? err.message : "Unable to load site visits."
      );
    } finally {
      setLoading(false);
    }
  }

  const eligibleLeads = useMemo(() => {
    const leadIdsWithVisit = new Set(
      siteVisits
        .filter((visit) => visit.leadId)
        .map((visit) => Number(visit.leadId))
    );

    return leads.filter(
      (lead) =>
        ["CONTACTED", "SITE_VISIT_REQUIRED", "SITE_VISIT", "SITE_VISIT_SCHEDULED"].includes(
          lead.status.toUpperCase()
        ) && !leadIdsWithVisit.has(lead.leadId)
    );
  }, [leads, siteVisits]);

  function openScheduleForm() {
    setFormError("");
    setShowForm(true);

    if (!leadId && eligibleLeads.length > 0) {
      setLeadId(String(eligibleLeads[0].leadId));
    }

    if (!scheduledAt) {
      const next = new Date(Date.now() + 60 * 60 * 1000);
      next.setSeconds(0, 0);
      setScheduledAt(toDateTimeLocal(next));
    }
  }

  function closeScheduleForm() {
    if (saving) return;
    setShowForm(false);
    setFormError("");
  }

  function toDateTimeLocal(date: Date) {
    const pad = (value: number) => String(value).padStart(2, "0");
    return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(
      date.getDate()
    )}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
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

  function statusClass(status: string) {
    switch (status.toUpperCase()) {
      case "SCHEDULED":
        return "status-site";
      case "CONFIRMED":
        return "status-qualified";
      case "IN_PROGRESS":
        return "status-quotation";
      case "COMPLETED":
        return "status-converted";
      case "CANCELLED":
        return "status-lost";
      case "RESCHEDULED":
        return "status-quotation";
      default:
        return "status-default";
    }
  }

  function leadForVisit(visit: SiteVisit) {
    if (visit.leadId) {
      return leads.find((lead) => lead.leadId === visit.leadId);
    }
    return undefined;
  }

  function openVisitDetails(visit: SiteVisit) {
    setSelectedVisit(visit);
    setDetailError("");
    setSiteCondition(visit.siteCondition || "");
    setMeasurements(visit.measurements || "");
    setDetailsCustomerNotes(visit.customerNotes || "");
    setDetailsInternalNotes(visit.internalNotes || "");
    setShowDetails(true);
  }

  function closeVisitDetails() {
    if (saving) return;
    setShowDetails(false);
    setSelectedVisit(null);
    setDetailError("");
  }

  async function submitSiteVisit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setFormError("");

    if (!leadId) {
      setFormError("Please select a lead.");
      return;
    }

    if (!scheduledAt) {
      setFormError("Please select the site visit date and time.");
      return;
    }

    try {
      setSaving(true);

      await api.createSiteVisit({
        leadId: Number(leadId),
        assignedToUserId: assignedToUserId ? Number(assignedToUserId) : null,
        scheduledAt: new Date(scheduledAt).toISOString(),
        customerNotes: customerNotes.trim() || null,
        internalNotes: internalNotes.trim() || null,
      });

      setShowForm(false);
      setLeadId("");
      setScheduledAt("");
      setCustomerNotes("");
      setInternalNotes("");
      await loadPage();
    } catch (err) {
      console.error(err);
      setFormError(
        err instanceof Error ? err.message : "Unable to schedule the site visit."
      );
    } finally {
      setSaving(false);
    }
  }

  async function changeVisitStatus(status: string) {
    if (!selectedVisit) return;

    try {
      setSaving(true);
      setDetailError("");
      const updated = await api.updateSiteVisitStatus(
        selectedVisit.siteVisitId,
        status
      );
      setSelectedVisit(updated);
      setSiteVisits((current) =>
        current.map((visit) =>
          visit.siteVisitId === updated.siteVisitId ? updated : visit
        )
      );
    } catch (err) {
      console.error(err);
      setDetailError(
        err instanceof Error ? err.message : "Unable to update visit status."
      );
    } finally {
      setSaving(false);
    }
  }

  async function saveVisitDetails(completeAfterSave = false) {
    if (!selectedVisit) return;

    try {
      setSaving(true);
      setDetailError("");

      const updated = await api.updateSiteVisitDetails(
        selectedVisit.siteVisitId,
        {
          siteCondition: siteCondition.trim() || null,
          measurements: measurements.trim() || null,
          customerNotes: detailsCustomerNotes.trim() || null,
          internalNotes: detailsInternalNotes.trim() || null,
        }
      );

      setSelectedVisit(updated);
      setSiteVisits((current) =>
        current.map((visit) =>
          visit.siteVisitId === updated.siteVisitId ? updated : visit
        )
      );

      if (completeAfterSave) {
        const completed = await api.updateSiteVisitStatus(
          updated.siteVisitId,
          "COMPLETED"
        );
        setSelectedVisit(completed);
        setSiteVisits((current) =>
          current.map((visit) =>
            visit.siteVisitId === completed.siteVisitId ? completed : visit
          )
        );
      }
    } catch (err) {
      console.error(err);
      setDetailError(
        err instanceof Error ? err.message : "Unable to save site visit details."
      );
    } finally {
      setSaving(false);
    }
  }

  const selectedLead = selectedVisit ? leadForVisit(selectedVisit) : undefined;
  const selectedStatus = selectedVisit?.status.toUpperCase() || "";

  return (
    <div className="module-page">
      <div className="module-header">
        <div>
          <h2>Site Visits</h2>
          <p>Schedule and manage site inspections linked to customer leads.</p>
        </div>

        <button className="primary-button module-action" onClick={openScheduleForm}>
          + Schedule Site Visit
        </button>
      </div>

      <div className="lead-summary">
        <div className="summary-card">
          <span>Total Visits</span>
          <strong>{siteVisits.length}</strong>
        </div>
        <div className="summary-card">
          <span>Scheduled</span>
          <strong>
            {siteVisits.filter((visit) =>
              ["SCHEDULED", "CONFIRMED", "RESCHEDULED"].includes(
                visit.status.toUpperCase()
              )
            ).length}
          </strong>
        </div>
        <div className="summary-card">
          <span>Completed</span>
          <strong>
            {siteVisits.filter(
              (visit) => visit.status.toUpperCase() === "COMPLETED"
            ).length}
          </strong>
        </div>
        <div className="summary-card">
          <span>Pending Leads</span>
          <strong>{eligibleLeads.length}</strong>
        </div>
      </div>

      {error && <div className="site-error-banner">{error}</div>}

      <div className="leads-panel">
        {loading ? (
          <div className="page-empty">
            <div className="empty-icon">◌</div>
            <h3>Loading site visits...</h3>
            <p>Reading site visits from the API.</p>
          </div>
        ) : siteVisits.length === 0 ? (
          <div className="page-empty">
            <div className="empty-icon">⌖</div>
            <h3>No site visits scheduled</h3>
            <p>Select a customer enquiry for a site visit.</p>
            <button
              className="primary-button"
              onClick={openScheduleForm}
              disabled={eligibleLeads.length === 0}
            >
              + Schedule Site Visit
            </button>
          </div>
        ) : (
          <div className="table-container">
            <table className="data-table">
              <thead>
                <tr>
                  <th>Lead</th>
                  <th>Customer</th>
                  <th>Location</th>
                  <th>Visit Date</th>
                  <th>Assigned To</th>
                  <th>Status</th>
                  <th>Action</th>
                </tr>
              </thead>
              <tbody>
                {siteVisits.map((visit) => {
                  const lead = leadForVisit(visit);
                  return (
                    <tr key={visit.siteVisitId}>
                      <td>
                        <strong className="lead-code">
                          {visit.leadCode || lead?.leadCode || `Visit #${visit.siteVisitId}`}
                        </strong>
                      </td>
                      <td>
                        <div className="table-customer">
                          <div className="table-avatar">
                            {(visit.customerName || lead?.customerName || "?")
                              .charAt(0)
                              .toUpperCase()}
                          </div>
                          <div>
                            <strong>{visit.customerName || lead?.customerName || "-"}</strong>
                            <span>{visit.companyName || lead?.companyName || "-"}</span>
                          </div>
                        </div>
                      </td>
                      <td>{visit.location || lead?.location || "-"}</td>
                      <td>{formatDateTime(visit.scheduledAt)}</td>
                      <td>
                        {visit.assignedToUserName ||
                          users.find((user) => user.userId === visit.assignedToUserId)?.userName ||
                          "-"}
                      </td>
                      <td>
                        <span className={`lead-status-badge ${statusClass(visit.status)}`}>
                          {displayStatus(visit.status)}
                        </span>
                      </td>
                      <td>
                        <button
                          type="button"
                          className="site-view-button"
                          onClick={() => openVisitDetails(visit)}
                        >
                          {visit.status.toUpperCase() === "COMPLETED" ? "View" : "Open"}
                        </button>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {showForm && (
        <div className="site-overlay" onClick={closeScheduleForm}>
          <aside className="site-drawer" onClick={(event) => event.stopPropagation()}>
            <div className="site-drawer-header">
              <div>
                <div className="site-drawer-kicker">SITE VISIT</div>
                <h2>Schedule Site Visit</h2>
                <p>Schedule the initial site inspection against an existing customer enquiry.</p>
              </div>
              <button type="button" className="site-close-button" onClick={closeScheduleForm} disabled={saving}>×</button>
            </div>

            {formError && <div className="site-error-banner">{formError}</div>}

            {eligibleLeads.length === 0 ? (
              <div className="site-empty-note">There are no CONTACTED / SITE VISIT leads currently available for scheduling.</div>
            ) : (
              <form onSubmit={submitSiteVisit}>
                <div className="site-form-grid">
                  <label>
                    <span className="site-form-label">Customer Lead</span>
                    <select value={leadId} onChange={(event) => setLeadId(event.target.value)} disabled={saving} className="site-form-control" required>
                      <option value="">Select customer enquiry</option>
                      {eligibleLeads.map((lead) => (
                        <option key={lead.leadId} value={lead.leadId}>
                          {lead.leadCode} — {lead.customerName}{lead.companyName ? ` — ${lead.companyName}` : ""}
                        </option>
                      ))}
                    </select>
                  </label>

                  {leadId && (() => {
                    const lead = leads.find((item) => item.leadId === Number(leadId));
                    if (!lead) return null;
                    return (
                      <div className="site-lead-summary">
                        <div><div className="site-meta-label">Location</div><div className="site-meta-value">{lead.location || "-"}</div></div>
                        <div><div className="site-meta-label">Project Type</div><div className="site-meta-value">{lead.projectType || "-"}</div></div>
                        <div><div className="site-meta-label">Area</div><div className="site-meta-value">{lead.approximateArea ? `${lead.approximateArea} ${lead.areaUnit || ""}`.trim() : "-"}</div></div>
                        <div><div className="site-meta-label">Mobile</div><div className="site-meta-value">{lead.mobile || "-"}</div></div>
                      </div>
                    );
                  })()}

                  <label>
                    <span className="site-form-label">Visit Date & Time</span>
                    <input type="datetime-local" value={scheduledAt} onChange={(event) => setScheduledAt(event.target.value)} disabled={saving} className="site-form-control" required />
                  </label>
                  <label>
                    <span className="site-form-label">Assigned To</span>
                    <select value={assignedToUserId} onChange={(event) => setAssignedToUserId(event.target.value)} disabled={saving} className="site-form-control">
                      <option value="">Unassigned</option>
                      {users.map((user) => <option key={user.userId} value={user.userId}>{user.userName}{user.email ? ` — ${user.email}` : ""}</option>)}
                    </select>
                  </label>
                  <label>
                    <span className="site-form-label">Customer Notes</span>
                    <textarea value={customerNotes} onChange={(event) => setCustomerNotes(event.target.value)} disabled={saving} className="site-form-control site-form-textarea" placeholder="Notes relevant to the customer/site visit" rows={4} />
                  </label>
                  <label>
                    <span className="site-form-label">Internal Notes</span>
                    <textarea value={internalNotes} onChange={(event) => setInternalNotes(event.target.value)} disabled={saving} className="site-form-control site-form-textarea" placeholder="Internal instructions for the assigned team member" rows={4} />
                  </label>
                  <div className="site-form-actions">
                    <button type="button" className="secondary-button" onClick={closeScheduleForm} disabled={saving}>Cancel</button>
                    <button type="submit" className="primary-button" disabled={saving}>{saving ? "Scheduling..." : "Schedule Site Visit"}</button>
                  </div>
                </div>
              </form>
            )}
          </aside>
        </div>
      )}

      {showDetails && selectedVisit && (
        <div className="site-overlay" onClick={closeVisitDetails}>
          <aside className="site-drawer site-drawer-wide" onClick={(event) => event.stopPropagation()}>
            <div className="site-drawer-header">
              <div>
                <div className="site-drawer-kicker">SITE VISIT #{selectedVisit.siteVisitId}</div>
                <h2>{selectedVisit.customerName || selectedLead?.customerName || "Site Visit"}</h2>
                <p>{selectedVisit.companyName || selectedLead?.companyName || ""} · {selectedVisit.location || selectedLead?.location || "Location not available"}</p>
              </div>
              <button type="button" className="site-close-button" onClick={closeVisitDetails} disabled={saving}>×</button>
            </div>

            {detailError && <div className="site-error-banner">{detailError}</div>}

            <div className="site-detail-top">
              <div><span>Status</span><strong className={`lead-status-badge ${statusClass(selectedVisit.status)}`}>{displayStatus(selectedVisit.status)}</strong></div>
              <div><span>Scheduled</span><strong>{formatDateTime(selectedVisit.scheduledAt)}</strong></div>
              <div><span>Assigned To</span><strong>{selectedVisit.assignedToUserName || "-"}</strong></div>
              <div><span>Completed</span><strong>{formatDateTime(selectedVisit.completedAt)}</strong></div>
            </div>

            <div className="site-detail-section">
              <div className="site-section-title">Visit Information</div>
              <div className="site-detail-grid">
                <div><div className="site-meta-label">Lead</div><div className="site-meta-value">{selectedVisit.leadCode || selectedLead?.leadCode || "-"}</div></div>
                <div><div className="site-meta-label">Mobile</div><div className="site-meta-value">{selectedLead?.mobile || "-"}</div></div>
                <div><div className="site-meta-label">Project Type</div><div className="site-meta-value">{selectedLead?.projectType || "-"}</div></div>
                <div><div className="site-meta-label">Approx. Area</div><div className="site-meta-value">{selectedLead?.approximateArea ? `${selectedLead.approximateArea} ${selectedLead.areaUnit || ""}`.trim() : "-"}</div></div>
              </div>
            </div>

            <div className="site-detail-section">
              <div className="site-section-title">Site Assessment</div>
              <div className="site-form-grid">
                <label>
                  <span className="site-form-label">Site Condition</span>
                  <textarea value={siteCondition} onChange={(event) => setSiteCondition(event.target.value)} disabled={saving || selectedStatus === "COMPLETED"} className="site-form-control site-form-textarea" placeholder="Describe the existing site condition, access, readiness, constraints, etc." rows={5} />
                </label>
                <label>
                  <span className="site-form-label">Measurements</span>
                  <textarea value={measurements} onChange={(event) => setMeasurements(event.target.value)} disabled={saving || selectedStatus === "COMPLETED"} className="site-form-control site-form-textarea" placeholder="Record room sizes, wall lengths, floor areas, levels and other measurements." rows={5} />
                </label>
                <label>
                  <span className="site-form-label">Customer Notes</span>
                  <textarea value={detailsCustomerNotes} onChange={(event) => setDetailsCustomerNotes(event.target.value)} disabled={saving || selectedStatus === "COMPLETED"} className="site-form-control site-form-textarea" placeholder="Customer requirements discussed during the visit." rows={4} />
                </label>
                <label>
                  <span className="site-form-label">Internal Notes</span>
                  <textarea value={detailsInternalNotes} onChange={(event) => setDetailsInternalNotes(event.target.value)} disabled={saving || selectedStatus === "COMPLETED"} className="site-form-control site-form-textarea" placeholder="Internal observations, risks, follow-up actions and resource notes." rows={4} />
                </label>
              </div>
            </div>

            <div className="site-form-actions site-detail-actions">
              {selectedStatus === "SCHEDULED" || selectedStatus === "CONFIRMED" || selectedStatus === "RESCHEDULED" ? (
                <>
                  <button type="button" className="secondary-button" onClick={() => changeVisitStatus("CANCELLED")} disabled={saving}>Cancel Visit</button>
                  <button type="button" className="secondary-button" onClick={() => changeVisitStatus("IN_PROGRESS")} disabled={saving}>Start Visit</button>
                  <button type="button" className="primary-button" onClick={() => saveVisitDetails(true)} disabled={saving}>{saving ? "Saving..." : "Save & Complete Visit"}</button>
                </>
              ) : selectedStatus === "IN_PROGRESS" ? (
                <>
                  <button type="button" className="secondary-button" onClick={() => saveVisitDetails(false)} disabled={saving}>{saving ? "Saving..." : "Save Details"}</button>
                  <button type="button" className="primary-button" onClick={() => saveVisitDetails(true)} disabled={saving}>{saving ? "Completing..." : "Complete Site Visit"}</button>
                </>
              ) : (
                <button type="button" className="secondary-button" onClick={closeVisitDetails}>Close</button>
              )}
            </div>
          </aside>
        </div>
      )}
    </div>
  );
}

export default SiteVisitsPage;
