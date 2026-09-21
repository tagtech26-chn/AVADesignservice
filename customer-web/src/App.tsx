import { useEffect, useRef, useState } from "react";
import "./App.css";
import StartProjectPage from "./pages/StartProjectPage";

const services = [
  {
    title: "Architecture & Design",
    description:
      "Planning, floor plans, elevations, interiors, 3D views and working drawings.",
    icon: "⌂",
    video: "/media/architecture-design.mp4",
  },
  {
    title: "Tiles & Flooring",
    description:
      "Tile selection, quantity calculation, layout planning, supply and installation.",
    icon: "▦",
    video: "/public/media/Tiles & Flooring.mp4",
  },
  {
    title: "Interior Design",
    description:
      "Interior planning, furniture layouts, kitchens, wardrobes and complete space design.",
    icon: "◇",
    video: "/public/media/Interior Design.mp4",
  },
  {
    title: "Civil & Renovation",
    description:
      "Civil modifications, partitions, preparation, renovation and related site work.",
    icon: "▤",
    video: "/media/civil-renovation.mp4",
  },
  {
    title: "Electrical & Plumbing",
    description:
      "Planning and execution support for electrical, lighting, plumbing and utility requirements.",
    icon: "⚡",
    video: "/media/electrical-plumbing.mp4",
  },
  {
    title: "Furniture & Fit-out",
    description:
      "Custom furniture, workstations, partitions, ceilings, wall finishes and fit-out services.",
    icon: "□",
    video: "/media/furniture-fit-out.mp4",
  },
];

const processSteps = [
  {
    number: "01",
    title: "Tell Us About Your Space",
    text: "Share your requirement, location, drawings, photos and the services you need.",
  },
  {
    number: "02",
    title: "Site & Requirement Assessment",
    text: "We understand your space, requirements, measurements and execution needs.",
  },
  {
    number: "03",
    title: "Design & Proposal",
    text: "Our team develops the required design and prepares the appropriate proposal.",
  },
  {
    number: "04",
    title: "Execution & Coordination",
    text: "We coordinate the selected services, materials, resources and execution.",
  },
  {
    number: "05",
    title: "Inspection & Handover",
    text: "Work is reviewed, completed and handed over with the required documentation.",
  },
];

function App() {
  const [showStartProject, setShowStartProject] = useState(false);
  const [showIntro, setShowIntro] = useState(true);
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false);
  const introVideoRef = useRef<HTMLVideoElement>(null);

  useEffect(() => {
    if (!showIntro) return;
    const video = introVideoRef.current;
    if (!video) return;
    video.currentTime = 0;
    const play = async () => {
      try {
        await video.play();
      } catch {
        video.muted = true;
        try { await video.play(); } catch { /* user can tap play */ }
      }
    };
    void play();
  }, [showIntro]);

  if (showIntro) {
    return (
      <div className="intro-gate">
        <video
          ref={introVideoRef}
          className="intro-gate-video"
          src="/media/intro-furniture-fit.mp4"
          autoPlay
          playsInline
          muted
          preload="auto"
          onEnded={() => setShowIntro(false)}
          onError={() => setShowIntro(false)}
        />
        <div className="intro-gate-shade" />
        {/* <img className="intro-gate-logo" src="/media/ava-logo.png" alt="AVA Surfaces" /> */}
        {/* <button className="intro-skip" type="button" onClick={() => setShowIntro(false)}>Skip intro →</button> */}
      </div>
    );
  }

  /*
   * ==========================================
   * CUSTOMER PROJECT ENQUIRY PAGE
   * ==========================================
   */

  if (showStartProject) {
    return (
      <div className="customer-site">

        <header className="site-header">

          <div className="site-container header-inner">

            <button
              className="logo"
              onClick={() => setShowStartProject(false)}
              type="button"
            >
              <div className="logo-mark"><img src="/media/ava-surfaces-logo.jpeg" alt="AVA Surfaces" /></div>

              <div className="logo-text">
                <strong>AVA Design</strong>
                <span>SERVICES</span>
              </div>
            </button>

            <button
              className="header-back-button"
              onClick={() => setShowStartProject(false)}
              type="button"
            >
              ← Back to Website
            </button>

          </div>

        </header>

        <StartProjectPage />

      </div>
    );
  }

  /*
   * ==========================================
   * MAIN CUSTOMER WEBSITE
   * ==========================================
   */

  return (
    <div className="customer-site">

      {/* ==========================================
          HEADER
      ========================================== */}

      <header className="site-header">

        <div className="site-container header-inner">

          <button
            className="logo logo-button"
            onClick={() => setShowStartProject(false)}
            type="button"
          >
            <div className="logo-mark"><img src="/media/ava-surfaces-logo.jpeg" alt="AVA Surfaces" /></div>

            <div className="logo-text">
              <strong>AVA Design</strong>
              <span>SERVICES</span>
            </div>
          </button>

          <nav className={`main-nav ${mobileMenuOpen ? "mobile-open" : ""}`}>

            <a href="#home" onClick={() => setMobileMenuOpen(false)}>
              Home
            </a>

            <a href="#services" onClick={() => setMobileMenuOpen(false)}>
              Services
            </a>

            <a href="#how-it-works" onClick={() => setMobileMenuOpen(false)}>
              How It Works
            </a>

            <a href="#projects" onClick={() => setMobileMenuOpen(false)}>
              Projects
            </a>

            <a href="#about" onClick={() => setMobileMenuOpen(false)}>
              About
            </a>

            <a href="#contact" onClick={() => setMobileMenuOpen(false)}>
              Contact
            </a>

          </nav>

          <button
            className="mobile-menu-button"
            type="button"
            aria-label="Toggle navigation"
            aria-expanded={mobileMenuOpen}
            onClick={() => setMobileMenuOpen((open) => !open)}
          >
            <span />
            <span />
            <span />
          </button>

          <button
            className="header-button"
            onClick={() => setShowStartProject(true)}
            type="button"
          >
            Start Your Project
          </button>

        </div>

      </header>

      <section className="header-video-banner" aria-label="AVA Design showcase">
        <video
          src="/media/header-video.mp4"
          autoPlay
          muted
          loop
          playsInline
          preload="auto"
        />
      </section>

      {/* ==========================================
          HERO
      ========================================== */}

      <main>

        <section
          className="hero"
          id="home"
        >

          <div className="hero-background"></div>

          <div className="site-container hero-inner">

            <div className="hero-content">

              <div className="eyebrow">
                COMPLETE SPACE DESIGN & EXECUTION
              </div>

              <h1>
                From Concept
                <br />
                <span>to Completion.</span>
              </h1>

              <p>
                Design your home, office, showroom or commercial
                space with one coordinated team — from planning
                and design to materials, resources and execution.
              </p>

              <div className="hero-actions">

                <button
                  className="hero-primary"
                  onClick={() => setShowStartProject(true)}
                  type="button"
                >
                  Start Your Project
                  <span>→</span>
                </button>

                <a
                  className="hero-secondary"
                  href="#services"
                >
                  Explore Services
                </a>

              </div>

              <div className="hero-note">
                <span>✓</span>
                Design • Supply • Resources • Coordination
              </div>

            </div>

            <div className="hero-visual">

              <div className="visual-card main-visual">

                <div className="visual-label">
                  SPACE DESIGN
                </div>

                <div className="visual-room">

                  <div className="room-ceiling"></div>

                  <div className="room-wall left-wall"></div>

                  <div className="room-wall back-wall">

                    <div className="window"></div>

                    <div className="art-frame"><img src="/media/ava-logo.png" alt="AVA Surfaces" /></div>

                  </div>

                  <div className="room-floor"></div>

                  <div className="room-sofa"></div>

                  <div className="room-table"></div>

                  <div className="room-rug"></div>

                  <div className="plant"></div>

                </div>

                <div className="visual-caption">
                  Designed around your space
                </div>

              </div>

            </div>

          </div>

        </section>

        {/* ==========================================
            INTRO
        ========================================== */}

        <section className="intro-section">

          <div className="site-container intro-grid">

            <div>

              <div className="section-eyebrow">
                ONE PLACE. MANY REQUIREMENTS.
              </div>

              <h2>
                Your space doesn't need
                <span> ten different conversations.</span>
              </h2>

            </div>

            <div>

              <p>
                Whether you are building a new office, renovating
                your home, setting up a showroom or simply looking
                for the right tiles and interiors, AVA Design Services
                brings the required design, materials, resources and
                coordination together.
              </p>

              <p>
                Choose only the services you need. We don't force
                you into a complete construction package.
              </p>

            </div>

          </div>

        </section>

        {/* ==========================================
            SERVICES
        ========================================== */}

        <section
          className="services-section"
          id="services"
        >

          <div className="site-container">

            <div className="section-heading">

              <div>

                <div className="section-eyebrow">
                  WHAT WE DO
                </div>

                <h2>
                  Services for every stage
                  <br />
                  of your space.
                </h2>

              </div>

              <p>
                Select individual services or combine them
                according to your project requirements.
              </p>

            </div>

            <div className="service-grid">

              {services.map((service) => (

                <article
                  className="service-card"
                  key={service.title}
                >

                  {service.video ? (
                    <div className="service-video-wrap">
                      <video
                        className="service-video"
                        src={service.video}
                        autoPlay
                        muted
                        loop
                        playsInline
                        preload="metadata"
                      />
                      <div className="service-video-overlay" />
                    </div>
                  ) : (
                    <div className="service-icon">
                      {service.icon}
                    </div>
                  )}

                  <h3>
                    {service.title}
                  </h3>

                  <p>
                    {service.description}
                  </p>

                  <button
                    className="service-link-button"
                    type="button"
                    onClick={() =>
                      setShowStartProject(true)
                    }
                  >
                    Explore service
                    <span>→</span>
                  </button>

                </article>

              ))}

            </div>

          </div>

        </section>

        {/* ==========================================
            HOW IT WORKS
        ========================================== */}

        <section
          className="process-section"
          id="how-it-works"
        >

          <div className="site-container">

            <div className="section-heading centered">

              <div className="section-eyebrow">
                HOW IT WORKS
              </div>

              <h2>
                A clear path from
                <span> idea to handover.</span>
              </h2>

              <p>
                We keep the process structured so you know
                what happens at every stage.
              </p>

            </div>

            <div className="process-grid">

              {processSteps.map((step) => (

                <div
                  className="process-card"
                  key={step.number}
                >

                  <div className="process-number">
                    {step.number}
                  </div>

                  <h3>
                    {step.title}
                  </h3>

                  <p>
                    {step.text}
                  </p>

                </div>

              ))}

            </div>

          </div>

        </section>

        {/* ==========================================
            PROJECT TYPES
        ========================================== */}

        <section
          className="project-types-section"
          id="projects"
        >

          <div className="site-container project-types-inner">

            <div>

              <div className="section-eyebrow">
                SPACES WE WORK WITH
              </div>

              <h2>
                Designed for the way
                <br />
                <span>you use your space.</span>
              </h2>

              <p>
                From homes and offices to showrooms, clinics,
                institutions and commercial spaces.
              </p>

              <button
                className="outline-button outline-button-action"
                onClick={() => setShowStartProject(true)}
                type="button"
              >
                Discuss Your Space →
              </button>

            </div>

            <div className="space-showcase">
              <video
                className="space-showcase-video"
                src="/media/space-types.mp4"
                autoPlay
                muted
                loop
                playsInline
                preload="metadata"
              />
            </div>

          </div>

        </section>

        {/* ==========================================
            ABOUT
        ========================================== */}

        <section
          className="about-section"
          id="about"
        >

          <div className="site-container about-grid">

            <div className="about-visual">

              <div className="about-box about-logo-box">
                <video src="/media/ava-surfaces-paper.mp4" autoPlay muted loop playsInline preload="metadata" aria-label="AVA Surfaces presentation" />
              </div>

            </div>

            <div className="about-content">

              <div className="section-eyebrow">
                ABOUT AVA DESIGN SERVICES
              </div>

              <h2>
                Design is the beginning.
                <span> Coordination makes it real.</span>
              </h2>

              <p>
                We bring together space planning, architecture,
                interiors, materials and execution resources so
                customers can manage their project through one
                coordinated platform.
              </p>

              <p>
                Some services are provided directly by our team.
                Materials can be supplied where required, while
                execution resources and specialist vendors can be
                arranged according to the project.
              </p>

              <div className="about-points">

                <div>
                  <span>✓</span>
                  Design-led approach
                </div>

                <div>
                  <span>✓</span>
                  Select only what you need
                </div>

                <div>
                  <span>✓</span>
                  Coordinated execution
                </div>

              </div>

            </div>

          </div>

        </section>

        {/* ==========================================
            START PROJECT
        ========================================== */}

        <section
          className="start-section"
          id="start-project"
        >

          <div className="site-container start-inner">

            <div>

              <div className="section-eyebrow">
                READY TO START?
              </div>

              <h2>
                Tell us about
                <br />
                <span>your space.</span>
              </h2>

              <p>
                Share a few details about your project.
                Our team will understand your requirements
                and get back to you.
              </p>

            </div>

            <div className="start-card">

              <div className="start-card-icon">
                +
              </div>

              <h3>
                Start Your Project
              </h3>

              <p>
                Tell us what you are planning,
                what services you need and how
                we can help.
              </p>

              <button
                className="start-button"
                onClick={() => setShowStartProject(true)}
                type="button"
              >
                Start Project Enquiry →
              </button>

              <small>
                No obligation. We'll discuss your
                requirements before proceeding.
              </small>

            </div>

          </div>

        </section>

        {/* ==========================================
            CONTACT
        ========================================== */}

        <section
          className="contact-section"
          id="contact"
        >

          <div className="site-container contact-inner">

            <div>

              <div className="section-eyebrow">
                GET IN TOUCH
              </div>

              <h2>
                Let's discuss
                <br />
                your project.
              </h2>

            </div>

            <div className="contact-details">

              <div>
                <span>CALL</span>
                <strong>
                  +91 XXXXX XXXXX
                </strong>
              </div>

              <div>
                <span>EMAIL</span>
                <strong>
                  hello@avadesignservices.com
                </strong>
              </div>

              <div>
                <span>LOCATION</span>
                <strong>
                  Puducherry, India
                </strong>
              </div>

            </div>

          </div>

        </section>

      </main>

      {/* ==========================================
          FOOTER
      ========================================== */}

      <footer className="site-footer">

        <div className="site-container footer-inner">

          <div className="footer-brand">

            <div className="logo-mark"><img src="/media/ava-surfaces-logo.jpeg" alt="AVA Surfaces" /></div>

            <div>

              <strong>
                AVA Design Services
              </strong>

              <span>
                Complete Space Design & Execution
              </span>

            </div>

          </div>

          <div className="footer-copy">
            © {new Date().getFullYear()} AVA Design Services.
            All rights reserved.
          </div>

        </div>

      </footer>

    </div>
  );
}

export default App;