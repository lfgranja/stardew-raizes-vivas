# Decision Record: Project Management Artifacts Approach

## Date
November 2, 2025

## Decision
Maintain current hybrid approach with GitHub Issues as primary project management system and markdown documentation files as supplementary reference material.

## Context
The project currently uses both GitHub Issues for task tracking and markdown files in `LivingRoots/docs/` for process documentation. A suggestion was made to relocate all project management artifacts from markdown files to GitHub Issues only, as storing project management artifacts as static markdown files was considered an anti-pattern.

## Analysis
### Current State
- 19 epics with 64 user stories already established in GitHub Issues
- Comprehensive documentation in `LivingRoots/docs/` including:
 - EPICS_USER_STORIES.md: Complete list of all epics and user stories
  - SETUP_CHECKLIST.md: Documentation of setup activities
  - PROJECT_SUMMARY.md: High-level project organization
 - LABELS_MILESTONES.md: Process documentation for labels and milestones
  - ISSUE_TEMPLATES.md: Documentation of issue templates

### Evaluation
After analyzing the pros and cons of both approaches, the current hybrid system provides value that a pure GitHub Issues approach would not:

**Markdown Files Value:**
- Historical context of project setup
- Onboarding material for new contributors
- High-level overview of entire project
- Process documentation for consistent issue creation

**GitHub Issues Value:**
- Real-time task tracking and status
- Interactive collaboration features
- Integration with development workflow
- Assignment and milestone tracking

## Decision
Maintain the current hybrid approach where:
- GitHub Issues serve as the primary system for task tracking and project management
- Markdown documentation files serve as supplementary reference material
- Documentation files focus on process, historical context, and high-level overviews rather than detailed task tracking

## Rationale
1. **Functionality**: The current system is already well-established and functional
2. **Complementary Value**: Documentation files provide different value than GitHub Issues
3. **Onboarding**: Documentation files help new contributors understand the project structure
4. **Risk**: Migration would be high-effort with significant risk of losing valuable historical context
5. **Scope**: The change is outside the scope of the current development efforts

## Future Considerations
- Implement a synchronization process to keep documentation aligned with GitHub Issues
- Use documentation files for process documentation rather than detailed task tracking
- Add cross-references between GitHub Issues and documentation files
- Regular review of documentation to ensure relevance

## Implementation
No immediate changes required. Continue using the current hybrid approach while considering future improvements to synchronization between GitHub Issues and documentation files.