# Spec 010 remediation — RED baseline

Captured before any remediation edit.
Command: `python3 scripts/tests/spec-consistency.py`
Exit: 1

```text
spec-consistency: FAIL (110 of 245 checks)
01. test-md-template.md promotion table must contain exactly the six C-REL-15 transitions; found [('any', 'partial'), ('machine-pass', 'partial'), ('machine-pass', 'verified'), ('partial', 'machine-pass'), ('partial', 'partial'), ('partial', 'partial'), ('verified', 'machine-pass'), ('verified', 'partial')]
02. test-md-template.md still contains the illegal verified -> machine-pass transition
03. test-md-template.md must state that testMdStatus and gate3Status are the same closed value
04. exit-codes.md must enumerate every code 0-22 as its own row; missing [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17]
05. exit-codes.md inheritance rows must name the spec-008 EX_* constants
06. exit-codes.md must contain a '## Helper subcommands' section
07. exit-codes.md helper section must document `validate-scenario`
08. exit-codes.md helper section must document `evaluate`
09. exit-codes.md helper section must document `extract`
10. exit-codes.md helper section must document `build-manifest`
11. evidence-bundle.schema.json must conditionally require commands, materials, and seeds for honestyLevel R/E
12. evidence-bundle.schema.json attempts must allow [] for pre-attempt failures (minItems: 0)
13. evidence-bundle.schema.json environmentOverrides must allow gamePath and milestoneSave
14. evidence-bundle.schema.json must use the canonical `buildSkipped`, not `skipBuild`
15. contracts/claims.schema.json is missing
16. spec.md C-BND-01 must list claimsFile, artifacts, and conditional commands/materials/seeds
17. data-model.md steps[].inputs row must document the insertWaste conditional requirement
18. data-model.md steps[].inputs row must document the itemId conditional requirement
19. data-model.md steps[].inputs row must document the count conditional requirement
20. data-model.md steps[].inputs row must document the setSoilHealth conditional requirement
21. data-model.md steps[].inputs row must document the soilHealth conditional requirement
22. data-model.md steps[].inputs row must document the runMacro conditional requirement
23. data-model.md steps[].inputs row must document the macroFile conditional requirement
24. never-quarantine criticalPathScenario ('004-playable-compost-loop', 'compost-growth') does not resolve to a committed scenario JSON
25. never-quarantine criticalPathScenario ('009-end-to-end-game-verification', 'save-load-round-trip') does not resolve to a committed scenario JSON
26. promotion-triggers.json must use {schemaVersion, components[], transitions[]}
27. promotion-triggers.json transitions[] must start empty and be appended by the runner
28. tasks.md T005 must mention `build-manifest`
29. tasks.md T013 must mention `claims.schema.json`
30. tasks.md T028 must mention `testMdPath`
31. tasks.md T028 must mention `marker`
32. tasks.md is missing required remediation task T034b
33. tasks.md T034b must mention `root`
34. tasks.md is missing required remediation task T034c
35. tasks.md T034c must mention `C-SEC-32`
36. tasks.md is missing required remediation task T061b
37. tasks.md T061b must mention `suite.sh`
38. tasks.md is missing required remediation task T061c
39. tasks.md T061c must mention `run-all`
40. tasks.md is missing required remediation task T061d
41. tasks.md T061d must mention `C-SCN-36`
42. tasks.md T064 must mention `deployment`
43. tasks.md T067 must mention `xdotool`
44. tasks.md T067 must mention `stdin`
45. tasks.md T071 must mention `commands`
46. tasks.md T071 must mention `materials`
47. tasks.md T071 must mention `seeds`
48. tasks.md T075 must mention `idempotent`
49. tasks.md T075 must mention `QUARANTINE_OWNER`
50. tasks.md T076 must mention `Wilson`
51. tasks.md T076 must mention `RAFT`
52. tasks.md T082 must mention `commands`
53. tasks.md T082 must mention `materials`
54. tasks.md T082 must mention `seeds`
55. tasks.md is missing required remediation task T094b
56. tasks.md T094b must mention `quit`
57. tasks.md T099 must mention `--quarantine-owner`
58. tasks.md is missing required remediation task T100b
59. tasks.md T100b must mention `C-REL-25`
60. tasks.md T104 must mention `T103`
61. tasks.md T110 must mention `zero`
62. tasks.md T110 must mention `runner-source`
63. tasks.md is missing required remediation task T122b
64. tasks.md T122b must mention `security-aggregation`
65. tasks.md is missing required remediation task T125b
66. tasks.md T125b must mention `doc-link`
67. tasks.md T136 must mention `VS-09b`
68. tasks.md T136 must mention `VS-09c`
69. tasks.md T137 must mention `gamepath.lock`
70. tasks.md T146 must mention `T141`
71. tasks.md T146 must mention `T145`
72. T110 must not implement scripts/lib/suite.sh; suite discovery moved to Phase 2 T061b
73. tasks.md still contains the removed --mods-path flag
74. tasks.md must cover .github/CODEOWNERS and security-aggregation
75. tasks.md must cover the FR-021 doc-link check
76. tasks.md must cover C-SEC-32, xdotool/stdin, and C-REL-25 research capture
77. specs/010-headless-verification-runner/spec.md still names the non-existent LivingRoots/ModConstants.cs
78. spec.md FR-025 must name LivingRoots/Constants.cs as the constants owner
79. spec.md A16 must list the four helper verbs including build-manifest
80. tasks.md T005 must scaffold four helper verbs including build-manifest
81. spec.md NFR-003 flag semantics must document --quarantine-owner
82. spec.md FR-007 must document the authoritative required-field set member rngSeed
83. spec.md FR-007 must document the authoritative required-field set member `time`
84. spec.md FR-007 must document the authoritative required-field set member schema `required` arrays
85. spec.md status vocabulary paragraph must include active
86. spec.md status vocabulary paragraph must include retired
87. spec.md status vocabulary paragraph must include not-evaluated
88. quickstart.md still assigns the twelve VS cases to T115; use T136
89. quickstart.md must assign the twelve VS cases to T136
90. quickstart.md must use buildSkipped, not skipBuild
91. traceability.md must add a Link status column
92. traceability.md must mark every CHK row as mapped or demonstrated
93. traceability.md still contains zero-padded C-* citations (e.g. C-REL-029)
94. .github/CODEOWNERS is missing
95. .github/workflows/security-aggregation.yml is missing
96. VERIFICATION_PROTOCOL.md must cross-link spec 010 FR-021
97. specs/009-end-to-end-game-verification/checklists/gate3-verification-contract.md must cross-link spec 010 FR-021
98. specs/010-headless-verification-runner/checklists/concurrency.md must carry the requirements-text-only banner
99. specs/010-headless-verification-runner/checklists/concurrency.md banner must point at T149 for implementation attestation
100. specs/010-headless-verification-runner/checklists/reliability.md must carry the requirements-text-only banner
101. specs/010-headless-verification-runner/checklists/reliability.md banner must point at T149 for implementation attestation
102. specs/010-headless-verification-runner/checklists/requirements.md must carry the requirements-text-only banner
103. specs/010-headless-verification-runner/checklists/requirements.md banner must point at T149 for implementation attestation
104. specs/010-headless-verification-runner/checklists/scenario-contract.md must carry the requirements-text-only banner
105. specs/010-headless-verification-runner/checklists/scenario-contract.md banner must point at T149 for implementation attestation
106. specs/010-headless-verification-runner/checklists/security.md must carry the requirements-text-only banner
107. specs/010-headless-verification-runner/checklists/security.md banner must point at T149 for implementation attestation
108. spec 004 scenario compost-growth-hardening.scenario.json is missing
109. GOVERNANCE-ANNEX.md deferred-items table is missing: Interactive game-store authentication
110. GOVERNANCE-ANNEX.md deferred-items table is missing: Public GitHub-hosted CI execution with game files
```
