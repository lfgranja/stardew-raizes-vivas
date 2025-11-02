# Labels and Milestones Documentation

## Overview
This document describes the labeling system and milestone organization implemented for the Living Roots project to facilitate effective project management and issue tracking.

## Labels System

### Primary Type Labels
- **`epic`**: Major feature areas that encompass multiple user stories
- **`user-story`**: Specific functionality expressed from the player's perspective
- **`feature`**: New functionality additions
- **`bug`**: Issues with existing functionality

### Domain-Specific Labels
- **`domain: soil-health`**: Issues related to soil health mechanics
- **`domain: composting`**: Issues related to composting mechanics
- **`domain: polyculture`**: Issues related to polyculture mechanics

### Standard GitHub Labels
- **`documentation`**: Improvements or additions to documentation
- **`enhancement`**: New feature or request
- **`help wanted`**: Extra attention is needed
- **`good first issue`**: Good for newcomers
- **`question`**: Further information is requested

### Priority/Estimation Labels
- **`Review effort 1/5`**: Estimation of development effort required

## Milestone System

### Early Game (Year 1) - Learning the Fundamentals
Focus: Introduction to core agroecological concepts
- Soil Health mechanics
- Composting system
- Soil Cover (Mulch)
- Simple Polyculture ("Three Sisters")
- New NPC Elena
- Heirloom Seeds
- Rainwater Harvesting

### Mid Game (Years 2-3) - Building the System
Focus: Integration of farming systems
- Crop Rotation
- Green Manure
- Integrated Animal Management
- Beneficial Insects and Beekeeping
- Living Pharmacy
- Bio-construction

### End Game (Year 4+) - Sovereignty and Community
Focus: Community integration and final goals
- Integrated Crop-Livestock-Forestry (ICLF)
- Greywater Recycling
- Short Supply Chains (Stardew Fair)
- Community Supported Agriculture (CSA)
- Collective Kitchen
- Food Sovereignty (Final Victory)

## Label Usage Guidelines

### For Epics
- Apply the `epic` label to all issues that represent major feature areas
- Add relevant `domain:` labels to indicate which game system the epic affects
- Include a description that mentions all related user stories

### For User Stories
- Apply the `user-story` label to all granular functionality issues
- Add relevant `domain:` labels to indicate which game system the story affects
- Reference the parent epic in the issue description

### For Bugs and Features
- Use `bug` for issues with existing functionality
- Use `enhancement` for new functionality requests
- Add domain-specific labels when relevant
- Add `help wanted` if you need assistance with the issue
- Add `good first issue` for beginner-friendly tasks

## Milestone Assignment Guidelines

### Early Game (Year 1) Milestone
- Core foundational mechanics
- Features that teach basic agroecological principles
- Items that should be implemented first for player learning
- Prerequisites for more advanced features

### Mid Game (Years 2-3) Milestone
- Features that build upon early game systems
- Integration between different game mechanics
- More complex agroecological practices
- Features that become relevant as player progresses

### End Game (Year 4+) Milestone
- Advanced features requiring early and mid-game systems
- Community-oriented features
- The final "victory" condition of the mod
- Features representing food sovereignty achievement

## Best Practices

1. **Consistent Labeling**: Always apply the appropriate type label (epic, user-story, etc.)
2. **Domain Classification**: Use domain-specific labels to enable filtering and organization
3. **Milestone Relevance**: Assign issues to milestones that match their complexity and progression
4. **Parent-Child Relationship**: Clearly reference parent epics in user story descriptions
5. **Regular Review**: Periodically review milestone assignments as the project evolves
6. **Prioritization**: Use labels in combination with issue assignment to prioritize work

## Benefits of This System

1. **Clear Organization**: Issues are categorized by type, domain, and development phase
2. **Effective Filtering**: Team members can easily filter issues by label or milestone
3. **Progress Tracking**: Milestones provide clear progress indicators for different game phases
4. **Domain Understanding**: Domain labels help identify which systems are affected
5. **Agile Process**: Enables sprint planning around milestones and priorities
6. **Community Contribution**: Clear labels make it easier for contributors to find relevant issues