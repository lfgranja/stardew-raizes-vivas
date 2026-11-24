# Research: Software Specification Quality

> **Date:** 2026-09-08
> **Topic:** Best practices for writing specifications free of implementation details while remaining precise enough for implementation.
> **Sources:** IEEE 830-1998, ISO/IEC/IEEE 29148:2018, Agile/Scrum guides, software engineering references.

---

## 1. "No Implementation Details" — What vs. How

### The Core Distinction

The most fundamental separation in software specification is between **what** the system must do and **how** it does it. This is not merely a stylistic preference — it is a structural boundary that determines who can write, review, and approve a specification.

| | **What (Requirements / SRS)** | **How (Design / SDD)** |
|---|---|---|
| **Question answered** | What problem does the software solve? What must it do? | How will we build it? What technologies, structures, and techniques? |
| **Audience** | Business stakeholders, product owners, QA, developers, clients | Developers, architects, DevOps |
| **Language** | Plain language, "shall" statements, user-visible behavior | Technical: APIs, schemas, algorithms, stack choices |
| **Stability** | Relatively stable — changes signal scope evolution | Flexible — can change without altering requirements |
| **Example** | "The system shall allow users to reset their forgotten passwords through email verification." | "Password reset flow uses JWT tokens with RS256 signing, sent via SendGrid API, stored in PostgreSQL." |

**Source:** [SRS Document vs SDD Document: The Complete 2026 Guide](https://www.thirdrocktechkno.com/blog/software-design-document-vs-software-requirement-specification/) — "The difference between a Software Requirements Specification and a Software Design Document comes down to one question: what vs how."

### What Counts as an "Implementation Detail"?

An implementation detail is any constraint or prescription that dictates *how* the system achieves its behavior, rather than *what* behavior it must exhibit. Common examples:

- **Specific technologies:** "Use PostgreSQL" vs. "Store user data persistently"
- **Data structures:** "Use a Dictionary<string, List<SoilSample>>" vs. "The system shall retrieve soil health data for a given tile"
- **Algorithms:** "Use Bresenham's line algorithm" vs. "The overlay shall render a smooth gradient between health levels"
- **Internal architecture:** "Implement via microservices" vs. "The system shall support 10,000 concurrent users"
- **UI widget choice:** "Use a dropdown" vs. "The user shall select one option from a predefined list"

**Source:** [How do I determine if a software requirement is specifying implementation detail?](https://sqa.stackexchange.com/questions/50549/how-do-i-determine-if-a-software-requirement-is-specifying-implementation-detail) — "It would remove many of the requirements that still reference implementation details (e.g. specific folder paths, log filename formatting...)"

### The Continuum Problem

The boundary between "what" and "how" is not absolute — it is a continuum. One stakeholder's "what" is another's "how":

> "Someone's 'what' is someone else's 'how'." — [Requirements Versus Design: It's All Design](https://www.its-all-design.com/requirements-versus-design-its-all-design/)

This means the "what vs. how" distinction is always relative to the audience and the level of abstraction. A useful three-level model from Chris Britton (cited in the same source):

1. **Business process design** — the business problem and workflow (the highest "what")
2. **System functional design** — user-visible behavior of the system (the middle "what"/"how")
3. **System technical design** — internal workings, architecture, code (the "how")

A specification should operate at levels 1–2. Level 3 belongs in a design document.

### IEEE Perspective

**IEEE 830-1998** (superseded by ISO/IEC/IEEE 29148:2018) defines the SRS as a document that "establishes the basis for agreement between customers and the developers on what the software product is to do." It explicitly separates requirements from design:

> "The SRS is *not* a design document. It is a specification of *what* the system should do." — [IEEE 830-1998](https://standards.ieee.org/standard/830-1998.html)

**ISO/IEC/IEEE 29148:2018** (the current international standard for requirements engineering) defines the Stakeholder Requirements Specification (StRS) as capturing "stakeholder expectations with acceptance criteria and **without implementation details**."

**Source:** [ISO 29148 Explained: Requirements Engineering Standard](https://www.modernrequirements.com/blogs/iso-29148-explained/) — The StRS "captures stakeholder expectations with acceptance criteria and without implementation details."

---

## 2. "Written for Non-Technical Stakeholders" — How Technical Is Too Technical?

### The Principle

A specification written for non-technical stakeholders should be readable and reviewable by someone who cannot read code. This does not mean it must be vague — it means it must express precision through *behavior*, not through *mechanism*.

> "Instead of long technical statements, user stories explain the need in plain language. This makes them easier for stakeholders and delivery teams to discuss." — [Agile Business Consortium: What are User Stories?](https://www.agilebusiness.org/resource/what-are-user-stories/)

### Signs a Specification Is Too Technical

A specification has crossed the line into "too technical" when it contains any of the following:

| **Too Technical (SDD territory)** | **Appropriate for Specs (SRS territory)** |
|---|---|
| "Implement a REST endpoint at `/api/soil/health`" | "The system shall expose soil health data to external consumers" |
| "Use a `Dictionary<TileKey, float>` for the runtime cache" | "The system shall retrieve soil health values for any tile in constant time" |
| "Render via XNA sprite batch with a custom shader" | "The overlay shall visually distinguish at least 5 levels of soil health using color coding" |
| "Apply the `Interlocked.Increment` pattern for thread safety" | "The system shall handle concurrent access from the game loop and UI thread without data corruption" |
| "Store data as JSON in `SaveData/soil_health.json`" | "Soil health data shall persist across game saves and reloads" |
| "Use a state machine with `enum CompostState { Empty, Processing, Ready }`" | "The composting bin shall transition through three observable states: empty, processing, and ready" |

### The "Stakeholder Test"

Ask: *Can my product owner, a QA tester who doesn't code, or a business stakeholder read this requirement and tell me whether the implemented feature matches it?*

If the answer is no, the requirement is likely too technical.

### Acceptance Criteria as the Precision Layer

Acceptance criteria bridge the gap between non-technical language and implementation precision. They describe *observable behavior* without prescribing *internal mechanism*:

> "Acceptance criteria describe what the end result should be, not the process of achieving it." — [Acceptance Criteria: Purposes, Types, Examples and Best Practices](https://www.altexsoft.com/blog/acceptance-criteria-purposes-formats-and-best-practices/)

Example of well-formed acceptance criteria (Given/When/Then):

```gherkin
Scenario: View soil health overlay
  Given the player has a hoe equipped
  When the player hovers over a tilled tile
  Then the system displays a tooltip showing the tile's nitrogen, phosphorus, and potassium levels
  And the tooltip updates within 100ms of hovering
```

This is precise enough to test and implement, yet contains zero implementation details about rendering, data structures, or algorithms.

### The INVEST Criteria

User stories (and by extension, specifications) should follow the **INVEST** model:

- **I**ndependent — can be developed in any order
- **N**egotiable — details are discussed, not dictated
- **V**aluable — delivers clear user/business value
- **E**stimable — team can size the work
- **S**mall — fits within a sprint/iteration
- **T**estable — has clear pass/fail criteria

**Source:** [Agile Business Consortium: What are User Stories?](https://www.agilebusiness.org/resource/what-are-user-stories/)

---

## 3. Best Practices for Precision Without Implementation Details

### 3.1 Use "Shall" Statements for Functional Requirements

IEEE 830 mandates "shall" as the canonical keyword for functional requirements. "Shall" is a binding, testable verb. Avoid "should," "might," "may," or "ideally" — these introduce ambiguity.

> "There is a significant difference between 'The system shall log all failed login attempts' and 'The system should log failed login attempts.' The former is verifiable. The latter invites misinterpretation." — [What Is a Software Requirement Specification (SRS)?](https://builtin.com/articles/software-requirement-specification-meaning)

**Template (from IEEE 830):**
> "Upon `<event or condition>`, the `<system or module>` shall `<action>`."

Example:
> "Upon the player hovering over a tilled tile, the visualization service shall display a tooltip containing the tile's current soil health metrics."

**Source:** [Appendix C: IEEE 830 Template – Requirements Engineering](https://press.rebus.community/requirementsengineering/back-matter/appendix-c-ieee-830-template/)

### 3.2 Describe Behavior, Not Mechanism

Focus on *observable system behavior* — what the user sees, hears, or experiences. Avoid describing internal processes.

| **Mechanism (avoid)** | **Behavior (prefer)** |
|---|---|
| "The system queries the soil health dictionary" | "The system retrieves the soil health value for a given tile" |
| "The overlay renders using a sprite batch" | "The overlay displays a colored indicator on each tilled tile" |
| "The service validates input via regex" | "The system rejects invalid coordinates and displays an error message" |
| "Data is serialized to JSON" | "Soil health data persists across game sessions" |

### 3.3 Use the Given/When/Then Format for Acceptance Criteria

The Gherkin format (from Behavior-Driven Development) enforces the what/how separation by design:

- **Given** — the precondition (state of the world)
- **When** — the trigger (user action or system event)
- **Then** — the observable outcome (what the user sees)

This format naturally excludes implementation details because it is structured around user-observable states and transitions.

**Source:** [Behaviour-Driven Development | Cucumber](https://cucumber.io/docs/bdd/) — "BDD is a way for software teams to work that closes the gap between business people and technical people by focusing collaborative work around..."

### 3.4 Apply the ISO/IEC/IEEE 29148 Quality Characteristics

The standard defines 9 characteristics for individual requirements. The most relevant to the what/how separation:

| **Characteristic** | **What it means for what/how separation** |
|---|---|
| **Unambiguous** | "The wording of requirements should not be vague." A requirement that says "fast response" is vague; "respond within 200ms at p95" is unambiguous without prescribing how. |
| **Verifiable** | "Requirements should not contain subjective language like 'user-friendly'." Verifiability forces you to describe *what* you observe, not *how* it's built. |
| **Singular** | "Each requirement statement must cover exactly one action or capability." This prevents bundling behavior with implementation. |
| **Necessary** | "If removing any requirement wouldn't leave a gap in what the system must do, it is not necessary." Implementation details often fail this test. |

**Source:** [ISO 29148 Explained: Requirements Engineering Standard](https://www.modernrequirements.com/blogs/iso-29148-explained/)

### 3.5 The "So That" Test for User Stories

Every user story should have a "so that" clause that explains the value. If the "I want" clause describes a technical mechanism rather than a user capability, it has drifted into implementation:

| **Too Technical** | **Value-Focused** |
|---|---|
| "As a developer, I want to implement a soil health cache" | "As a farmer, I want to see soil health at a glance, so that I can decide where to fertilize" |
| "As a system, I want to serialize data to JSON" | "As a player, I want my soil improvements to persist, so that I don't lose progress when I quit" |

**Source:** [Agile Business Consortium: What are User Stories?](https://www.agilebusiness.org/resource/what-are-user-stories/) — "Another mistake is making stories too technical. For example, 'As a developer, I want to create a database table' may be a valid task, but it is not usually a strong user story because it does not describe user value."

### 3.6 Separate Constraints from Requirements

ISO/IEC/IEEE 29148 and IEEE 830 both recognize that some technical constraints are legitimate in a specification — but they should be clearly labeled as *constraints*, not functional requirements:

- **Functional requirement:** "The system shall display soil health data for the tile under the cursor"
- **Design constraint:** "The implementation shall use SMAPI's rendering API for overlay compatibility"
- **Implementation detail (belongs in SDD):** "The overlay shall use `SpriteBatch.Draw` with a custom `Effect` class"

Constraints are acceptable when they are externally imposed (regulatory, platform-mandated, compatibility). Implementation details are choices the developer should be free to make.

**Source:** [IEEE 830 Template — Section 2.5: Design and Implementation Constraints](https://press.rebus.community/requirementsengineering/back-matter/appendix-c-ieee-830-template/) — "Describe any items or issues that will limit the options available to the developers."

### 3.7 Use Domain Language, Not Technical Language

The specification should use the language of the problem domain (ubiquitous language in DDD terms), not the language of the solution domain:

| **Solution Domain (avoid)** | **Problem Domain (prefer)** |
|---|---|
| "Dictionary lookup" | "Retrieve soil health for a tile" |
| "State machine transition" | "The composting bin changes from processing to ready" |
| "Event-driven architecture" | "The system responds to the player's actions in real time" |
| "Cache invalidation" | "The displayed data refreshes when the underlying values change" |

This aligns with the project's DDD conventions: `SoilHealth`, `Compost`, `LivingSoil`, `HeirloomSeed`, `CompanionPlanting`.

### 3.8 The "Multiple Solutions" Test

A well-written requirement should admit more than one implementation. If a requirement can only be satisfied by one specific technical approach, it is likely over-specified:

> "For every requirement, there is more than one solution that can be listed and from which, one is selected." — [Requirements Versus Design: It's All Design](https://www.its-all-design.com/requirements-versus-design-its-all-design/)

**Test:** After writing a requirement, ask: "Could two competent developers implement this in different ways and both satisfy the requirement?" If no, the requirement is too prescriptive.

---

## Summary: The Specification Quality Checklist

A high-quality feature specification should:

- [ ] **Pass the stakeholder test** — readable by non-technical stakeholders
- [ ] **Use "shall" statements** — binding, testable language
- [ ] **Describe behavior, not mechanism** — what the system does, not how it does it
- [ ] **Admit multiple implementations** — does not prescribe a single technical solution
- [ ] **Use domain language** — ubiquitous language, not solution-domain jargon
- [ ] **Include acceptance criteria** — Given/When/Then or rule-oriented format
- [ ] **Separate constraints from requirements** — label platform/compatibility constraints explicitly
- [ ] **Pass the "so that" test** — every story has clear user/business value
- [ ] **Be verifiable** — each requirement has a clear pass/fail condition
- [ ] **Be singular** — one action or capability per requirement statement

---

## References

1. [IEEE 830-1998 - IEEE Standard for Software Requirements Specifications](https://standards.ieee.org/standard/830-1998.html)
2. [IEEE/ISO/IEC 29148-2018 - Systems and Software Engineering, Life Cycle Processes, Requirements Engineering](https://standards.ieee.org/standard/29148-2018.html)
3. [ISO 29148 Explained: Requirements Engineering Standard](https://www.modernrequirements.com/blogs/iso-29148-explained/)
4. [SRS Document vs SDD Document: The Complete 2026 Guide](https://www.thirdrocktechkno.com/blog/software-design-document-vs-software-requirement-specification/)
5. [Requirements Versus Design: It's All Design](https://www.its-all-design.com/requirements-versus-design-its-all-design/)
6. [How do I determine if a software requirement is specifying implementation detail?](https://sqa.stackexchange.com/questions/50549/how-do-i-determine-if-a-software-requirement-is-specifying-implementation-detail)
7. [Acceptance Criteria: Purposes, Types, Examples and Best Practices](https://www.altexsoft.com/blog/acceptance-criteria-purposes-formats-and-best-practices/)
8. [What Is a Software Requirement Specification (SRS)?](https://builtin.com/articles/software-requirement-specification-meaning)
9. [What are User Stories? — Agile Business Consortium](https://www.agilebusiness.org/resource/what-are-user-stories/)
10. [Behaviour-Driven Development | Cucumber](https://cucumber.io/docs/bdd/)
11. [Appendix C: IEEE 830 Template – Requirements Engineering](https://press.rebus.community/requirementsengineering/back-matter/appendix-c-ieee-830-template/)
