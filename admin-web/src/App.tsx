import { useEffect, useState } from "react";
import "./App.css";

import { api } from "./services/api";
import LeadsPage from "./pages/LeadsPage";
import SiteVisitsPage from "./pages/SiteVisitsPage";
import QuotationsPage from "./pages/QuotationsPage";

import type {
  Lead,
  Project,
  Quotation,
  ProjectTask,
} from "./services/api";

const menuItems = [
  { name: "Dashboard", icon: "▦" },
  { name: "Leads", icon: "◉" },
  { name: "Customers", icon: "♙" },
  { name: "Projects", icon: "▣" },
  { name: "Site Visits", icon: "⌖" },
  { name: "Designs", icon: "✎" },
  { name: "Quotations", icon: "▤" },
  { name: "Services", icon: "◇" },
  { name: "Resources", icon: "♢" },
  { name: "Materials", icon: "□" },
  { name: "Tasks", icon: "✓" },
  { name: "Progress", icon: "◔" },
  { name: "Payments", icon: "₹" },
  { name: "Documents", icon: "▱" },
  { name: "Settings", icon: "⚙" },
];

function App() {
  const [activeMenu, setActiveMenu] = useState("Dashboard");

  // ==========================================
  // LEADS
  // ==========================================

  const [leads, setLeads] = useState<Lead[]>([]);
  const [loadingLeads, setLoadingLeads] = useState(true);
  const [leadError, setLeadError] = useState("");

  // ==========================================
  // PROJECTS
  // ==========================================

  const [projects, setProjects] = useState<Project[]>([]);
  const [loadingProjects, setLoadingProjects] = useState(true);
  const [projectError, setProjectError] = useState("");

  // ==========================================
  // QUOTATIONS
  // ==========================================

  const [quotations, setQuotations] = useState<Quotation[]>([]);
  const [loadingQuotations, setLoadingQuotations] = useState(true);
  const [quotationError, setQuotationError] = useState("");

  // ==========================================
  // TASKS
  // ==========================================

  const [tasks, setTasks] = useState<ProjectTask[]>([]);
  const [loadingTasks, setLoadingTasks] = useState(true);
  const [taskError, setTaskError] = useState("");

  // ==========================================
  // INITIAL LOAD
  // ==========================================

  useEffect(() => {
    loadLeads();
    loadProjects();
    loadQuotations();
  }, []);

  // ==========================================
  // LOAD LEADS
  // ==========================================

  async function loadLeads() {
    try {
      setLoadingLeads(true);
      setLeadError("");

      const data = await api.getLeads();

      setLeads(data);
    } catch (error) {
      console.error("Failed to load leads:", error);
      setLeadError("Unable to load leads from API.");
    } finally {
      setLoadingLeads(false);
    }
  }

  // ==========================================
  // LOAD PROJECTS
  // ==========================================

  async function loadProjects() {
    try {
      setLoadingProjects(true);
      setProjectError("");

      const data = await api.getProjects();

      setProjects(data);

      await loadTasksForProjects(data);
    } catch (error) {
      console.error("Failed to load projects:", error);
      setProjectError("Unable to load projects from API.");
      setLoadingTasks(false);
    } finally {
      setLoadingProjects(false);
    }
  }

  // ==========================================
  // LOAD QUOTATIONS
  // ==========================================

  async function loadQuotations() {
    try {
      setLoadingQuotations(true);
      setQuotationError("");

      const data = await api.getQuotations();

      setQuotations(data);
    } catch (error) {
      console.error("Failed to load quotations:", error);
      setQuotationError("Unable to load quotations from API.");
    } finally {
      setLoadingQuotations(false);
    }
  }

  // ==========================================
  // LOAD TASKS
  // ==========================================

  async function loadTasksForProjects(
    projectList: Project[]
  ) {
    try {
      setLoadingTasks(true);
      setTaskError("");

      if (projectList.length === 0) {
        setTasks([]);
        setLoadingTasks(false);
        return;
      }

      const taskResults = await Promise.all(
        projectList.map(async (project) => {
          try {
            return await api.getProjectTasks(project.projectId);
          } catch (error) {
            console.error(
              `Failed to load tasks for project ${project.projectId}:`,
              error
            );

            return [];
          }
        })
      );

      setTasks(taskResults.flat());
    } catch (error) {
      console.error("Failed to load tasks:", error);
      setTaskError("Unable to load project tasks.");
    } finally {
      setLoadingTasks(false);
    }
  }

  // ==========================================
  // DASHBOARD COUNTS
  // ==========================================

  const activeProjects = projects.filter(
    (project) =>
      project.status &&
      !["COMPLETED", "CANCELLED"].includes(
        project.status.toUpperCase()
      )
  );

  const pendingQuotations = quotations.filter(
    (quotation) =>
      quotation.status &&
      [
        "DRAFT",
        "SENT",
        "CUSTOMER_REVIEW",
        "REVISION_REQUESTED",
      ].includes(quotation.status.toUpperCase())
  );

  const pendingTasks = tasks.filter(
    (task) =>
      task.status &&
      !["COMPLETED", "CANCELLED"].includes(
        task.status.toUpperCase()
      )
  );

  // ==========================================
  // RENDER
  // ==========================================

  return (
    <div className="admin-layout">

      {/* ======================================
          SIDEBAR
      ====================================== */}

      <aside className="sidebar">

        <div className="brand">

          <div className="brand-logo">
            AVA
          </div>

          <div>
            <div className="brand-title">
              AVA Design
            </div>

            <div className="brand-subtitle">
              SERVICES
            </div>
          </div>

        </div>

        <div className="sidebar-section-title">
          MAIN MENU
        </div>

        <nav className="menu">

          {menuItems.map((item) => (
            <button
              key={item.name}
              className={`menu-item ${
                activeMenu === item.name
                  ? "active"
                  : ""
              }`}
              onClick={() =>
                setActiveMenu(item.name)
              }
            >
              <span className="menu-icon">
                {item.icon}
              </span>

              <span>
                {item.name}
              </span>
            </button>
          ))}

        </nav>

        <div className="sidebar-bottom">

          <div className="admin-card">

            <div className="avatar">
              A
            </div>

            <div className="admin-info">

              <strong>
                Admin
              </strong>

              <span>
                Administrator
              </span>

            </div>

            <span className="status-dot"></span>

          </div>

        </div>

      </aside>

      {/* ======================================
          MAIN
      ====================================== */}

      <main className="main-content">

        {/* TOP BAR */}

        <header className="topbar">

          <div>

            <h1>
              {activeMenu}
            </h1>

            <p>
              AVA Design Services Administration
            </p>

          </div>

          <div className="topbar-right">

            <button className="notification-button">
              🔔
            </button>

            <div className="profile">

              <div className="profile-avatar">
                A
              </div>

              <div>

                <strong>
                  Admin
                </strong>

                <span>
                  Administrator
                </span>

              </div>

            </div>

          </div>

        </header>

        {/* ======================================
            LEADS PAGE
        ====================================== */}

        {activeMenu === "Leads" ? (

          <LeadsPage
            onNewLead={() => {
              alert(
                "New Lead form will be connected next."
              );
            }}
          />

        ) : activeMenu === "Site Visits" ? (

          <SiteVisitsPage />

        ) : activeMenu === "Quotations" ? (

          <QuotationsPage />

        ) : (

          /* ====================================
             DASHBOARD
          ==================================== */

          <section className="dashboard">

            {/* WELCOME */}

            <div className="welcome">

              <div>

                <h2>
                  Welcome to AVA Design Services
                </h2>

                <p>
                  Manage leads, projects, designs,
                  quotations and execution activities
                  from one place.
                </p>

              </div>

              <button className="primary-button">
                + New Lead
              </button>

            </div>

            {/* ==================================
                KPI CARDS
            ================================== */}

            <div className="stat-grid">

              {/* LEADS */}

              <div className="stat-card">

                <div className="stat-icon purple">
                  ◉
                </div>

                <div>

                  <span>
                    Total Leads
                  </span>

                  <strong>
                    {loadingLeads
                      ? "..."
                      : leads.length}
                  </strong>

                </div>

              </div>

              {/* PROJECTS */}

              <div className="stat-card">

                <div className="stat-icon blue">
                  ▣
                </div>

                <div>

                  <span>
                    Active Projects
                  </span>

                  <strong>
                    {loadingProjects
                      ? "..."
                      : activeProjects.length}
                  </strong>

                </div>

              </div>

              {/* QUOTATIONS */}

              <div className="stat-card">

                <div className="stat-icon green">
                  ₹
                </div>

                <div>

                  <span>
                    Pending Quotations
                  </span>

                  <strong>
                    {loadingQuotations
                      ? "..."
                      : pendingQuotations.length}
                  </strong>

                </div>

              </div>

              {/* TASKS */}

              <div className="stat-card">

                <div className="stat-icon orange">
                  ✓
                </div>

                <div>

                  <span>
                    Tasks Pending
                  </span>

                  <strong>
                    {loadingTasks
                      ? "..."
                      : pendingTasks.length}
                  </strong>

                </div>

              </div>

            </div>

            {/* ==================================
                CONTENT GRID
            ================================== */}

            <div className="content-grid">

              {/* RECENT LEADS */}

              <div className="panel">

                <div className="panel-header">

                  <div>

                    <h3>
                      Recent Leads
                    </h3>

                    <p>
                      Latest customer enquiries
                    </p>

                  </div>

                  <button
                    className="link-button"
                    onClick={() =>
                      setActiveMenu("Leads")
                    }
                  >
                    View All
                  </button>

                </div>

                {leadError ? (

                  <div className="empty-state">

                    <div className="empty-icon">
                      !
                    </div>

                    <h4>
                      {leadError}
                    </h4>

                    <p>
                      Check that the backend API
                      is running on port 5048.
                    </p>

                  </div>

                ) : loadingLeads ? (

                  <div className="empty-state">

                    <div className="empty-icon">
                      ◌
                    </div>

                    <h4>
                      Loading leads...
                    </h4>

                    <p>
                      Reading customer enquiries
                      from the database.
                    </p>

                  </div>

                ) : leads.length === 0 ? (

                  <div className="empty-state">

                    <div className="empty-icon">
                      ◉
                    </div>

                    <h4>
                      No recent leads
                    </h4>

                    <p>
                      New customer enquiries will
                      appear here.
                    </p>

                  </div>

                ) : (

                  <div className="lead-list">

                    {leads
                      .slice(0, 5)
                      .map((lead) => (

                        <div
                          className="lead-row"
                          key={lead.leadId}
                        >

                          <div className="lead-avatar">
                            {lead.customerName
                              ?.charAt(0)
                              .toUpperCase() || "L"}
                          </div>

                          <div className="lead-details">

                            <strong>
                              {lead.customerName}
                            </strong>

                            <span>
                              {lead.companyName ||
                                lead.leadCode}
                            </span>

                          </div>

                          <div className="lead-status">
                            {lead.status}
                          </div>

                        </div>

                      ))}

                  </div>

                )}

              </div>

              {/* PROJECT OVERVIEW */}

              <div className="panel">

                <div className="panel-header">

                  <div>

                    <h3>
                      Project Overview
                    </h3>

                    <p>
                      Current project status
                    </p>

                  </div>

                  <button className="link-button">
                    View All
                  </button>

                </div>

                {projectError ? (

                  <div className="empty-state">

                    <div className="empty-icon">
                      !
                    </div>

                    <h4>
                      {projectError}
                    </h4>

                    <p>
                      Check that the backend API
                      is running on port 5048.
                    </p>

                  </div>

                ) : loadingProjects ? (

                  <div className="empty-state">

                    <div className="empty-icon">
                      ◌
                    </div>

                    <h4>
                      Loading projects...
                    </h4>

                    <p>
                      Reading projects from
                      the database.
                    </p>

                  </div>

                ) : projects.length === 0 ? (

                  <div className="empty-state">

                    <div className="empty-icon">
                      ▣
                    </div>

                    <h4>
                      No projects
                    </h4>

                    <p>
                      Projects created from
                      qualified leads will appear here.
                    </p>

                  </div>

                ) : (

                  <div className="project-list">

                    {projects
                      .slice(0, 5)
                      .map((project) => (

                        <div
                          className="project-row"
                          key={project.projectId}
                        >

                          <div className="project-icon">
                            ▣
                          </div>

                          <div className="project-details">

                            <strong>
                              {project.projectName ||
                                project.projectCode ||
                                `Project #${project.projectId}`}
                            </strong>

                            <span>
                              {project.projectCode ||
                                `Project #${project.projectId}`}
                            </span>

                          </div>

                          <div className="project-status">
                            {project.status}
                          </div>

                        </div>

                      ))}

                  </div>

                )}

              </div>

            </div>

            {/* ==================================
                SYSTEM STATUS
            ================================== */}

            <div className="system-status-panel">

              <div className="system-status-header">

                <div>

                  <h3>
                    System Status
                  </h3>

                  <p>
                    Live connection status
                  </p>

                </div>

                <div className="api-online">

                  <span className="online-dot"></span>

                  API Connected

                </div>

              </div>

              <div className="system-status-grid">

                <div>

                  <span>
                    Leads
                  </span>

                  <strong>
                    {loadingLeads
                      ? "Loading..."
                      : `${leads.length} records`}
                  </strong>

                </div>

                <div>

                  <span>
                    Projects
                  </span>

                  <strong>
                    {loadingProjects
                      ? "Loading..."
                      : `${projects.length} records`}
                  </strong>

                </div>

                <div>

                  <span>
                    Quotations
                  </span>

                  <strong>
                    {loadingQuotations
                      ? "Loading..."
                      : `${quotations.length} records`}
                  </strong>

                </div>

                <div>

                  <span>
                    Tasks
                  </span>

                  <strong>
                    {loadingTasks
                      ? "Loading..."
                      : `${tasks.length} records`}
                  </strong>

                </div>

              </div>

              {(quotationError || taskError) && (

                <div className="system-warning">

                  {quotationError && (
                    <div>
                      {quotationError}
                    </div>
                  )}

                  {taskError && (
                    <div>
                      {taskError}
                    </div>
                  )}

                </div>

              )}

            </div>

          </section>

        )}

      </main>

    </div>
  );
}

export default App;