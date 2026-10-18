# Harvest Feedback Visibility — Brief Research Findings

Task: harvest-quality feedback visibility (floating-text / immediate feedback) for agricultural/game systems.

## 1. Stardew Valley / SMAPI Mod Patterns (engineering evidence)
- **Show Item Quality** mod (NexusMods): shows star quality in HUD pop-up when harvesting crops, collecting animal products, gathering forage, catching fish, emptying machines — copied from mobile version to PC (NexusMods entry; also UIInfoSuite2Alt). Confirms floating/overlay quality indicators are a recognized quality-of-life need.
- **Crop Quality** mod (CurseForge): displays harvest quality (silver/gold/iridium stars) next to grown crops, either always visible, toggle-hotkey, or hold-to-show; also modifies quality-generation logic. Confirms players want visible quality at harvest time.
- **Harvest indicators / bubbles** mods (e.g., Aedenthorn "Crop Timer Tooltip", "Crop Quality"): floating bubbles over ready-to-harvest crops, overlay stars, tooltip predictions of harvest quality. Native-style floating bubbles above crops showing water/harvest/readiness state.
- **UIInfoSuite2Alt**: "ShowItemQualityOnPickup", "ShowHarvestQuality" toggle — predicted harvest quality on tooltips (engineering, not peer-reviewed).
- Pattern: floating text / HUD pop-up / bubble overlay at point of interaction; immediate activation (not on-demand) per INFORM framework; color/star coding communicates quality grade.

References (mod pages / repos): NexusMods (Show Item Quality, UIInfoSuite2Alt), CurseForge (Crop Quality), GitHub (autumnloveless/StardewValley-CropTimerTooltip, aedenthorn/StardewValleyMods), Spawnite docs (floating-text pattern in game engines).

## 2. Usability / Game-Design Literature (design evidence, not agronomic science)
- **Immediate progress feedback** is a required flow condition per Nakamura & Csikszentmihalyi (2002), validated experimentally (Keller & Bless 2008 for challenge; this paper tests feedback). Without it, players rely on memory / delayed verification, reducing flow and competence perception.
- **INFORM framework** (Sedig & Haworth, 2016): "immediate activation" (reaction occurs instantaneously after action) prevents effortful long-term thinking and encourages reactionary play; on-demand activation promotes reflection. For harvest verification, immediate visible feedback supports quick verification loops rather than deferred inspection.
- **Juicy feedback** (CHI 2024, Hicks et al.): amplified success-dependent feedback drives competence, curiosity, and enjoyment; too much random feedback occludes action-feedback links. Key implication: feedback must be tied to the specific harvest outcome (success-dependent) and not random decoration.
- **Usability Principles for Video Games** (Pinelle et al., 2008): "provide consistent responses to user’s actions" — unpredictable/inconsistent response breaks verification. Immediate visual feedback is the operationalization.
- **Stardew Valley usability review** (Medium/DeWitt; blog reviews): highlights visibility & feedback — icons hover above retrievable items; floating indicators for maker readiness; context-specific feedback lets players know tasks are possible/completed. Quality-star feedback fits this same pattern.
- Note: no peer-reviewed agronomy paper specifically addresses floating-text harvest feedback; evidence here is game-design / HCI engineering.

References: Nakamura & Csikszentmihalyi (2002) flow conditions; Sedig & Haworth (2016) INFORM; Hicks et al. (CHI 2024) "How does Juicy Game Feedback Motivate?"; Pinelle et al. (2008) game usability heuristics; DeWitt (Medium) usability review of Stardew.

## 3. Scientific / Agronomic Observation Practices (scientific/agronomic, not game design)
- **FAO crop assessment guide** (fao.org/4/x8234e/x8234e06.htm): emphasizes visual observation — "look at crop as whole and as single plants", compare good/poor plants, use eyes, feel, smell, taste; measurement leads to numerical descriptions of quality; structured observation sheets used each visit; representative sampling (5 separate areas, avoid edges); record management actions with observations.
- **UMN Extension — Measuring forage quality**: visual assessments should be used along with lab testing; visual assessment considers stage of maturity, color, leafiness, foreign material, odor, overall condition; cannot replace lab analysis but detects issues tests miss; sample by lot, at/near harvest, before feeding, periodically during feeding.
- **Oklahoma State — Evaluating hay quality by sight, smell, feel**: visual inspection (leafiness, odor, color, softness, purity) complements chemical analysis; evaluation must be representative (random samples, inner portion of bale); predictions made from observation (e.g., wet hay will mold).
- **Cameroon cocoa study** (s40066-015-0047-z, scientific): only 10% of farmers could perceive quality cocoa using full physical characterization (color, shell, bean, odor); 64% think quality has little impact on price; farmers focus on weight not quality; formal training improves perception. Confirms quality observation is a learned skill and that visible quality indicators (like star ratings) could help bridge the gap.

References: FAO "Section 3: Assessing and measuring your crop"; UMN Extension "Measuring forage quality"; Oklahoma State "Evaluating Hay Quality Based on Sight, Smell and Feel"; Takrama et al. (2015) "Postharvest practices and farmers' perception of cocoa bean quality in Cameroon" (DOI 10.1186/s40066-015-0047-z).

## 4. Key Takeaway / Note on Evidence Type
- The claim "harvest-quality feedback is critical for player verification" is supported strongly by **engineering/game-design literature** (flow theory, INFORM, juicy-feedback experiments, usability heuristics, existing SMAPI mods) and by **agronomic observation practice** (structured visual sampling, quality characterization by sight/feel/smell, quality perception studies in cocoa farming).
- There is no direct peer-reviewed scientific publication stating "floating text improves harvest verification"; the bridge between agronomic observation and game UI is analogical: agronomy uses immediate structured visual assessment; game design uses immediate visual feedback to close action-result loops.
- Conclusion: the design choice (floating/overlay quality indicator at harvest point) aligns with both fields — agronomic observation demands visible, representative, structured quality assessment; game usability demands consistent, immediate, action-linked feedback for verification.
