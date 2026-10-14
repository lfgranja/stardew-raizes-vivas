using System.IO;
using System.Text.RegularExpressions;

namespace LivingRoots.Tests
{
    /// <summary>
    /// T210 (F5, partial, FR-015): production sources must never write raw
    /// exception stack-trace detail into the mod's output — failures are
    /// reported as <c>GetType().FullName</c> + <c>HResult</c> at
    /// <c>LogLevel.Trace</c> only. T097 removed the offending log sites and
    /// T200 claimed a source-scan guard, but the guard itself never existed,
    /// so the invariant was unprotected against regression. This test is the
    /// guard: a universal property over the production file tree — for every
    /// <c>.cs</c> file under <c>LivingRoots/</c> (build output excluded), no
    /// member access resolves a <c>StackTrace</c>. The generated domain is the
    /// real source set, so the assertion quantifies over it; a regression
    /// reintroducing <c>ex.StackTrace</c> fails here. Observed state at
    /// authoring: GREEN — the remediation already landed (verified zero
    /// occurrences across <c>LivingRoots/</c>); the red-first intent is
    /// satisfied by the fact that the guard did not exist to fail, and its
    /// value is preventing reintroduction.
    /// </summary>
    public class ProductionSourceGuardTests
    {
        [Fact]
        public void ProductionSources_ContainNoStackTraceAccess()
        {
            // Arrange — scan the real production tree, never build output.
            var productionRoot = Path.Combine(LocateRepoRoot(), "LivingRoots");
            Assert.True(Directory.Exists(productionRoot), $"Production root not found at {productionRoot}.");

            var violations = new List<string>();
            var scanned = 0;

            // Act — quantify over every production source file.
            foreach (var file in Directory.EnumerateFiles(productionRoot, "*.cs", SearchOption.AllDirectories))
            {
                if (IsBuildOutput(file))
                {
                    continue;
                }

                scanned++;
                var content = File.ReadAllText(file);
                if (Regex.IsMatch(content, @"\b\w+\.StackTrace\b"))
                {
                    violations.Add(Path.GetRelativePath(productionRoot, file));
                }
            }

            // Assert — the guard must never be vacuous, and no production file
            // may reach into an exception's stack trace.
            Assert.True(scanned > 0, "No production sources were scanned — the guard is vacuous.");
            Assert.True(
                violations.Count == 0,
                $"FR-015 violation — these production files access StackTrace: {string.Join(", ", violations)}.");
        }

        /// <summary>
        /// T233 (FR-015): exception detail is emitted ONLY at LogLevel.Trace.
        /// No ex.Message, no ex.StackTrace, and no interpolated exception
        /// detail (ex.GetType, HResult) at Warn/Error/Alert. Fails today:
        /// CompostingBinService.cs:362 and SoilHealthService.cs:52 emit
        /// FullName+HResult at Warn; the :45 guard only regexes StackTrace.
        /// </summary>
        [Fact]
        public void ProductionSources_ExceptionDetailOnlyAtTrace()
        {
            // Arrange — scan the real production tree, never build output.
            var productionRoot = Path.Combine(LocateRepoRoot(), "LivingRoots");
            Assert.True(Directory.Exists(productionRoot), $"Production root not found at {productionRoot}.");

            var messageViolations = new List<string>();
            var stackTraceViolations = new List<string>();
            var warnDetailViolations = new List<string>();
            var scanned = 0;

            // Act — quantify over every production source file.
            foreach (var file in Directory.EnumerateFiles(productionRoot, "*.cs", SearchOption.AllDirectories))
            {
                if (IsBuildOutput(file))
                {
                    continue;
                }

                scanned++;
                var content = File.ReadAllText(file);
                var relative = Path.GetRelativePath(productionRoot, file);

                if (Regex.IsMatch(content, @"\bex\.Message\b"))
                {
                    messageViolations.Add(relative);
                }

                if (Regex.IsMatch(content, @"\b\w+\.StackTrace\b"))
                {
                    stackTraceViolations.Add(relative);
                }

                var logMatches = Regex.Matches(content, @"\.Log\s*\([^;]*?LogLevel\.(Warn|Error|Alert)", RegexOptions.Singleline);
                foreach (Match match in logMatches)
                {
                    var callText = match.Value;
                    if (callText.Contains("ex.") || callText.Contains("GetType()") || callText.Contains("HResult"))
                    {
                        warnDetailViolations.Add(relative);
                        break;
                    }
                }
            }

            // Assert — the guard must never be vacuous; detail lives at Trace only.
            Assert.True(scanned > 0, "No production sources were scanned — the guard is vacuous.");
            Assert.True(
                messageViolations.Count == 0,
                $"FR-015 violation — these production files interpolate ex.Message: {string.Join(", ", messageViolations)}.");
            Assert.True(
                stackTraceViolations.Count == 0,
                $"FR-015 violation — these production files access StackTrace: {string.Join(", ", stackTraceViolations)}.");
            Assert.True(
                warnDetailViolations.Count == 0,
                $"FR-015 violation — these production files emit exception detail at Warn/Error/Alert (expected Trace only; today SoilHealthService.cs:52 and CompostingBinService.cs:362): {string.Join(", ", warnDetailViolations)}.");
        }

        /// <summary>
        /// T233 (FR-015): SaveIdProvider reports failures as GetType().FullName,
        /// never GetType().Name. Fails today: SaveIdProvider.cs:61 uses Name.
        /// </summary>
        [Fact]
        public void ProductionSources_SaveIdProviderUsesFullName()
        {
            // Arrange — read the provider source.
            var productionRoot = Path.Combine(LocateRepoRoot(), "LivingRoots");
            var providerPath = Path.Combine(productionRoot, "Services", "SaveIdProvider.cs");
            Assert.True(File.Exists(providerPath), $"SaveIdProvider.cs not found at {providerPath}.");

            // Act.
            var content = File.ReadAllText(providerPath);

            // Assert — FullName present, bare Name absent.
            Assert.True(
                content.Contains("GetType().FullName"),
                "LivingRoots/Services/SaveIdProvider.cs:61 uses GetType().Name today; expected GetType().FullName.");
            var withoutFullName = content.Replace("GetType().FullName", string.Empty);
            Assert.True(
                !withoutFullName.Contains("GetType().Name"),
                "LivingRoots/Services/SaveIdProvider.cs:61 must not use GetType().Name; use GetType().FullName.");
        }

        /// <summary>
        /// T233 (FR-012): tile-key parsing is invariant-culture with part-count
        /// validation (compare SoilHealthService.cs:866-867 TryParse with
        /// InvariantCulture). Fails today: CompostingBinService.cs:510-516 uses
        /// culture-sensitive int.Parse without a count check.
        /// </summary>
        [Fact]
        public void ProductionSources_TileKeyParsingIsInvariantCultureWithCountCheck()
        {
            // Arrange — read the bin service source.
            var productionRoot = Path.Combine(LocateRepoRoot(), "LivingRoots");
            var servicePath = Path.Combine(productionRoot, "Services", "CompostingBinService.cs");
            Assert.True(File.Exists(servicePath), $"CompostingBinService.cs not found at {servicePath}.");

            // Act — isolate the ParseTileKey window.
            var content = File.ReadAllText(servicePath);
            var methodIndex = content.IndexOf("ParseTileKey", StringComparison.Ordinal);
            Assert.True(methodIndex >= 0, "ParseTileKey not found in LivingRoots/Services/CompostingBinService.cs.");
            var methodWindow = content.Substring(methodIndex, Math.Min(800, content.Length - methodIndex));

            // Assert — invariant culture, TryParse, and a part-count check.
            Assert.True(
                methodWindow.Contains("InvariantCulture"),
                "LivingRoots/Services/CompostingBinService.cs:510-516 parses tile keys with culture-sensitive int.Parse today; expected InvariantCulture (compare SoilHealthService.cs:866-867).");
            Assert.True(
                methodWindow.Contains("TryParse"),
                "LivingRoots/Services/CompostingBinService.cs:510-516 uses int.Parse today; expected TryParse with invariant culture.");
            Assert.True(
                methodWindow.Contains("parts.Length") || methodWindow.Contains("Length != 2") || methodWindow.Contains("Length == 2"),
                "LivingRoots/Services/CompostingBinService.cs:510-516 lacks part-count validation today; expected a parts.Length count check.");
        }

        /// <summary>
        /// T236 (Constitution III): no _monitor.Log call or sound playback
        /// appears inside a lock (_lock) block in CompostingBinService.cs.
        /// Simple brace-aware scan: track lock (_lock) depth and flag Log /
        /// playSound lines inside. Fails today: direct violations in AddWaste,
        /// CollectCompost, ProcessDayStart, OnObjectRemoved, and the second
        /// PruneOrphanedLocations lock.
        /// </summary>
        [Fact]
        public void CompostingBinService_NoLogOrSoundInsideLock()
        {
            // Arrange — read the bin service lines.
            var productionRoot = Path.Combine(LocateRepoRoot(), "LivingRoots");
            var servicePath = Path.Combine(productionRoot, "Services", "CompostingBinService.cs");
            Assert.True(File.Exists(servicePath), $"CompostingBinService.cs not found at {servicePath}.");

            // Act — brace-aware scan over lock (_lock) depth.
            var lines = File.ReadAllLines(servicePath);
            var violations = new List<string>();
            int depth = 0;
            var lockDepths = new List<int>();
            bool pendingLock = false;
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (line.Contains("lock (_lock)"))
                {
                    pendingLock = true;
                }

                int opens = CountChar(line, '{');
                int closes = CountChar(line, '}');
                if (pendingLock && opens > 0)
                {
                    lockDepths.Add(depth + 1);
                    pendingLock = false;
                }

                depth = depth + opens - closes;
                while (lockDepths.Count > 0 && depth < lockDepths[lockDepths.Count - 1])
                {
                    lockDepths.RemoveAt(lockDepths.Count - 1);
                }

                bool insideLock = lockDepths.Count > 0;
                if (insideLock && (line.Contains("_monitor.Log") || line.Contains("playSound")))
                {
                    violations.Add("CompostingBinService.cs:" + (i + 1).ToString() + ":" + line.Trim());
                }
            }

            // Assert — zero Log/playSound lines inside the lock.
            Assert.True(
                violations.Count == 0,
                $"Constitution III violation — _monitor.Log or sound playback inside lock (_lock) in LivingRoots/Services/CompostingBinService.cs (today e.g. :107-108, :190, :205-207, :240-241, :252, :263, :281, :441-443): {string.Join(" | ", violations)}.");
        }

        /// <summary>
        /// T236 (Constitution III): PruneOrphanedLocations acquires _lock exactly
        /// once. Fails today: double lock at :407 and :436.
        /// </summary>
        [Fact]
        public void CompostingBinService_PruneOrphanedLocationsAcquiresLockOnce()
        {
            // Arrange — read the bin service lines.
            var productionRoot = Path.Combine(LocateRepoRoot(), "LivingRoots");
            var servicePath = Path.Combine(productionRoot, "Services", "CompostingBinService.cs");
            Assert.True(File.Exists(servicePath), $"CompostingBinService.cs not found at {servicePath}.");

            // Act — bound the method, then count lock (_lock) inside it.
            var lines = File.ReadAllLines(servicePath);
            int methodStart = -1;
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains("private void PruneOrphanedLocations"))
                {
                    methodStart = i;
                    break;
                }
            }

            Assert.True(methodStart >= 0, "PruneOrphanedLocations not found in LivingRoots/Services/CompostingBinService.cs.");
            int depth = 0;
            bool seenOpen = false;
            int lockCount = 0;
            for (int i = methodStart; i < lines.Length; i++)
            {
                string line = lines[i];
                int opens = CountChar(line, '{');
                int closes = CountChar(line, '}');
                if (!seenOpen)
                {
                    if (opens > 0)
                    {
                        seenOpen = true;
                        if (line.Contains("lock (_lock)"))
                        {
                            lockCount++;
                        }

                        depth = depth + opens - closes;
                    }
                }
                else
                {
                    if (line.Contains("lock (_lock)"))
                    {
                        lockCount++;
                    }

                    depth = depth + opens - closes;
                    if (depth <= 0)
                    {
                        break;
                    }
                }
            }

            // Assert — exactly one acquisition.
            Assert.True(
                lockCount == 1,
                $"LivingRoots/Services/CompostingBinService.cs:407/:436 acquires _lock {lockCount} times in PruneOrphanedLocations today; expected exactly once.");
        }

        /// <summary>
        /// T237: ModController.cs:1733-1736 claims a bin day guard that
        /// CompostingBinService.cs:224-269 does not implement. Assert the
        /// corrected statement so the test fails today and passes once T264
        /// corrects the comment.
        /// </summary>
        [Fact]
        public void FalseComment_ModControllerDayGuard_MatchesCode()
        {
            // Arrange.
            var productionRoot = Path.Combine(LocateRepoRoot(), "LivingRoots");
            var controllerPath = Path.Combine(productionRoot, "Controllers", "ModController.cs");
            Assert.True(File.Exists(controllerPath), $"ModController.cs not found at {controllerPath}.");

            // Act.
            var controllerContent = File.ReadAllText(controllerPath);
            string corrected = "no per-day guard exists";
            string falsePhrase = "exactly-once-per-day, per-location guard";
            bool hasCorrected = controllerContent.Contains(corrected);
            bool hasFalse = controllerContent.Contains(falsePhrase);

            // Assert — corrected present, or false removed.
            Assert.True(
                hasCorrected || !hasFalse,
                "ModController.cs:1733-1736 claims a bin day guard (T056 in CompostingBinService) that CompostingBinService.cs:224-269 does not implement. Expected corrected statement containing '" + corrected + "' once T264 corrects the comment.");
        }

        /// <summary>
        /// T237: CompostingBinService.cs:390-392 states the orphan-prune rule
        /// backwards vs :419 (claims prune only when location unknown; code
        /// retains when gameLocation == null). Assert the corrected statement.
        /// </summary>
        [Fact]
        public void FalseComment_OrphanPruneRule_MatchesCode()
        {
            // Arrange.
            var productionRoot = Path.Combine(LocateRepoRoot(), "LivingRoots");
            var servicePath = Path.Combine(productionRoot, "Services", "CompostingBinService.cs");
            Assert.True(File.Exists(servicePath), $"CompostingBinService.cs not found at {servicePath}.");

            // Act.
            var content = File.ReadAllText(servicePath);
            string corrected = "prune retains when location unresolved";
            string falsePhrase = "BOTH that the location is unknown";
            bool hasCorrected = content.Contains(corrected);
            bool hasFalse = content.Contains(falsePhrase);

            // Assert.
            Assert.True(
                hasCorrected || !hasFalse,
                "LivingRoots/Services/CompostingBinService.cs:390-392 states the orphan-prune rule backwards vs :419 (claims prune only when location unknown; code at :419 retains when gameLocation == null). Expected corrected statement containing '" + corrected + "' once T264 corrects the comment.");
        }

        /// <summary>
        /// T237: ICompostingBinService.cs:25 claims CollectCompost returns the
        /// maturation level; the code always returns 1 (exactly-one batch
        /// yield, CompostingBinService.cs:168). Assert the corrected statement.
        /// </summary>
        [Fact]
        public void FalseComment_CollectCompostMaturation_MatchesCode()
        {
            // Arrange.
            var productionRoot = Path.Combine(LocateRepoRoot(), "LivingRoots");
            var interfacePath = Path.Combine(productionRoot, "Domain", "Interfaces", "ICompostingBinService.cs");
            Assert.True(File.Exists(interfacePath), $"ICompostingBinService.cs not found at {interfacePath}.");

            // Act.
            var content = File.ReadAllText(interfacePath);
            string corrected = "exactly-one batch yield";
            string falsePhrase = "equal to maturation level";
            bool hasCorrected = content.Contains(corrected);
            bool hasFalse = content.Contains(falsePhrase);

            // Assert.
            Assert.True(
                hasCorrected || !hasFalse,
                "LivingRoots/Domain/Interfaces/ICompostingBinService.cs:25 claims CollectCompost returns maturation level; code always returns 1 (exactly-one batch yield). Expected corrected statement containing '" + corrected + "' once T264 corrects the comment.");
        }

        /// <summary>
        /// T237: ICompostingBinService.cs:36-37 omits that ProcessDayStart has
        /// no per-day dedup guard (ModController.cs:1733-1736 wrongly claims
        /// one). Assert the corrected disclaimer.
        /// </summary>
        [Fact]
        public void FalseComment_ProcessDayStartGuard_MatchesCode()
        {
            // Arrange.
            var productionRoot = Path.Combine(LocateRepoRoot(), "LivingRoots");
            var interfacePath = Path.Combine(productionRoot, "Domain", "Interfaces", "ICompostingBinService.cs");
            Assert.True(File.Exists(interfacePath), $"ICompostingBinService.cs not found at {interfacePath}.");

            // Act.
            var content = File.ReadAllText(interfacePath);
            string corrected = "no per-day guard exists";
            bool hasCorrected = content.Contains(corrected);

            // Assert.
            Assert.True(
                hasCorrected,
                "LivingRoots/Domain/Interfaces/ICompostingBinService.cs:36-37 must disclose that ProcessDayStart carries no per-day dedup guard (CompostingBinService.cs:224-269 reprocesses on repeat calls). Expected corrected statement containing '" + corrected + "' once T264 corrects the comment.");
        }

        /// <summary>
        /// T237: ICompostingBinService.cs:43-44 claims OnObjectRemoved drops
        /// items on the ground; the code only clears state
        /// (CompostingBinService.cs:271-283, ground-drop lives in
        /// CreateDislodgedBinItem/ModController). Assert the corrected statement.
        /// </summary>
        [Fact]
        public void FalseComment_OnObjectRemovedGroundDrop_MatchesCode()
        {
            // Arrange.
            var productionRoot = Path.Combine(LocateRepoRoot(), "LivingRoots");
            var interfacePath = Path.Combine(productionRoot, "Domain", "Interfaces", "ICompostingBinService.cs");
            Assert.True(File.Exists(interfacePath), $"ICompostingBinService.cs not found at {interfacePath}.");

            // Act.
            var content = File.ReadAllText(interfacePath);
            string corrected = "does not drop items on the ground";
            string falsePhrase = "Drops items on ground";
            bool hasCorrected = content.Contains(corrected);
            bool hasFalse = content.Contains(falsePhrase);

            // Assert.
            Assert.True(
                hasCorrected || !hasFalse,
                "LivingRoots/Domain/Interfaces/ICompostingBinService.cs:43-44 claims OnObjectRemoved drops items on ground; code only clears state. Expected corrected statement containing '" + corrected + "' once T264 corrects the comment.");
        }

        /// <summary>
        /// T237: Constants.cs:98-100 claims the refusal keys off the
        /// Data/Machines ShakeDuration field; the registration at
        /// ModEntry.cs:376-432 never sets ShakeDuration (sets DaysUntilReady
        /// only) and the refusal uses worldObject.shakeTimer. Assert the
        /// corrected statement.
        /// </summary>
        [Fact]
        public void FalseComment_InvalidDropInShake_MatchesCode()
        {
            // Arrange.
            var productionRoot = Path.Combine(LocateRepoRoot(), "LivingRoots");
            var constantsPath = Path.Combine(productionRoot, "Constants.cs");
            Assert.True(File.Exists(constantsPath), $"Constants.cs not found at {constantsPath}.");

            // Act.
            var content = File.ReadAllText(constantsPath);
            string corrected = "shakeTimer";
            string falsePhrase = "ShakeDuration";
            bool hasCorrected = content.Contains(corrected);
            bool hasFalse = content.Contains(falsePhrase);

            // Assert — corrected present, or false removed.
            Assert.True(
                hasCorrected || !hasFalse,
                "LivingRoots/Constants.cs:98-100 claims a Data/Machines ShakeDuration the registration at ModEntry.cs:376-432 never sets (registration sets DaysUntilReady only; refusal uses worldObject.shakeTimer). Expected corrected statement containing '" + corrected + "' (or removal of ShakeDuration) once T264 corrects the comment.");
        }

        /// <summary>
        /// T238: every constant declared in Constants.cs has at least one
        /// production reference. Written as "no unreferenced constant survives"
        /// so it passes whether T265 deletes the dead names or wires them up.
        /// Fails today: CompostCraftingSound, CompostStackMax,
        /// BinInvalidDropInShakeSeconds (and others) have zero references.
        /// </summary>
        [Fact]
        public void ProductionSources_NoUnreferencedConstantsSurvive()
        {
            // Arrange — parse declarations from Constants.cs.
            var productionRoot = Path.Combine(LocateRepoRoot(), "LivingRoots");
            var constantsPath = Path.Combine(productionRoot, "Constants.cs");
            Assert.True(File.Exists(constantsPath), $"Constants.cs not found at {constantsPath}.");
            var constantsContent = File.ReadAllText(constantsPath);
            var nameMatches = Regex.Matches(constantsContent, @"public\s+(?:const|static\s+readonly)\s+\w+(?:<[^;]+>)?\s+(\w+)\s*(?:=|;)");
            var declared = new List<string>();
            foreach (Match match in nameMatches)
            {
                declared.Add(match.Groups[1].Value);
            }

            Assert.True(declared.Count > 0, "No constants parsed from LivingRoots/Constants.cs — the guard is vacuous.");

            // Act — gather every other production source text.
            var productionTexts = new List<string>();
            foreach (var file in Directory.EnumerateFiles(productionRoot, "*.cs", SearchOption.AllDirectories))
            {
                if (IsBuildOutput(file))
                {
                    continue;
                }

                if (string.Equals(Path.GetFullPath(file), Path.GetFullPath(constantsPath), StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                productionTexts.Add(File.ReadAllText(file));
            }

            var unreferenced = new List<string>();
            foreach (var name in declared)
            {
                bool referenced = false;
                string pattern = @"\b" + Regex.Escape(name) + @"\b";
                foreach (var text in productionTexts)
                {
                    if (Regex.IsMatch(text, pattern))
                    {
                        referenced = true;
                        break;
                    }
                }

                if (!referenced)
                {
                    unreferenced.Add(name);
                }
            }

            // Assert — zero unreferenced constants survive.
            Assert.True(
                unreferenced.Count == 0,
                $"Unreferenced constants survive in LivingRoots/Constants.cs (expected zero; today includes CompostCraftingSound, CompostStackMax, BinInvalidDropInShakeSeconds): {string.Join(", ", unreferenced)}.");
        }

        /// <summary>
        /// T238: OrganicWasteValidator.MachineWasteTags is either consumed in
        /// production or removed. Passes in either end-state (consumed, or
        /// declaration gone). Fails today: declared at
        /// OrganicWasteValidator.cs:30 with zero production consumers
        /// (ModEntry.cs builds RequiredTags from the two tags directly).
        /// </summary>
        [Fact]
        public void ProductionSources_MachineWasteTagsConsumedOrRemoved()
        {
            // Arrange.
            var productionRoot = Path.Combine(LocateRepoRoot(), "LivingRoots");
            var validatorPath = Path.Combine(productionRoot, "Domain", "Services", "OrganicWasteValidator.cs");
            Assert.True(File.Exists(validatorPath), $"OrganicWasteValidator.cs not found at {validatorPath}.");

            // Act — removed end-state passes vacuously.
            var validatorContent = File.ReadAllText(validatorPath);
            bool declared = validatorContent.Contains("MachineWasteTags");
            if (!declared)
            {
                return;
            }

            bool consumed = false;
            foreach (var file in Directory.EnumerateFiles(productionRoot, "*.cs", SearchOption.AllDirectories))
            {
                if (IsBuildOutput(file))
                {
                    continue;
                }

                if (string.Equals(Path.GetFullPath(file), Path.GetFullPath(validatorPath), StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var text = File.ReadAllText(file);
                if (Regex.IsMatch(text, @"\bMachineWasteTags\b"))
                {
                    consumed = true;
                    break;
                }
            }

            // Assert — declared implies consumed.
            Assert.True(
                consumed,
                "LivingRoots/Domain/Services/OrganicWasteValidator.cs:30 declares MachineWasteTags but no production file consumes it today (ModEntry.cs:376-432 builds RequiredTags from CompostableItemTag/NotCompostableTag directly). Expected: consumed in production or removed (T265 may delete it).");
        }

        /// <summary>
        /// T244: no *.bak file exists anywhere under LivingRoots/
        /// (bin/obj excluded). Fails today: LivingRoots/ModEntry.cs.bak exists.
        /// </summary>
        [Fact]
        public void ProductionSources_ContainNoBakFiles()
        {
            // Arrange — enumerate the source tree, never build output.
            var productionRoot = Path.Combine(LocateRepoRoot(), "LivingRoots");
            Assert.True(Directory.Exists(productionRoot), $"Production root not found at {productionRoot}.");

            // Act.
            var bakFiles = new List<string>();
            var scanned = 0;
            foreach (var file in Directory.EnumerateFiles(productionRoot, "*", SearchOption.AllDirectories))
            {
                if (IsBuildOutput(file))
                {
                    continue;
                }

                scanned++;
                if (file.EndsWith(".bak", StringComparison.OrdinalIgnoreCase))
                {
                    bakFiles.Add(Path.GetRelativePath(productionRoot, file));
                }
            }

            // Assert — the guard must never be vacuous, and zero *.bak survive.
            Assert.True(scanned > 0, "No production files were scanned — the guard is vacuous.");
            Assert.True(
                bakFiles.Count == 0,
                $"*.bak files survive under LivingRoots/ (expected zero; today includes ModEntry.cs.bak): {string.Join(", ", bakFiles)}.");
        }

        /// <summary>
        /// True when the path belongs to build output (<c>bin/</c> or
        /// <c>obj/</c>) on either path separator, so generated artifacts are
        /// never treated as production sources.
        /// </summary>
        private static bool IsBuildOutput(string path)
        {
            foreach (var segment in path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
            {
                if (segment == "bin" || segment == "obj")
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Counts occurrences of a character in a line, so the lock-depth scan
        /// stays allocation-free and lambda-free.
        /// </summary>
        private static int CountChar(string text, char value)
        {
            int count = 0;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == value)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Walks up from the test output directory to the directory containing
        /// <c>Stardew-LivingRoots.sln</c> (the ArchitecturalPlanTests pattern),
        /// so the guard reads the source tree regardless of the runner's
        /// working directory.
        /// </summary>
        private static string LocateRepoRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Stardew-LivingRoots.sln")))
            {
                directory = directory.Parent;
            }

            var root = directory?.FullName;
            Assert.True(root != null, "Could not locate Stardew-LivingRoots.sln above the test output directory.");
            return root!;
        }
    }
}
