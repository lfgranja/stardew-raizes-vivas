using System;
using System.Collections.Generic;
using System.Globalization;

namespace LivingRoots.Verification.Queue
{
    /// <summary>
    /// One executable verification-queue command. Parsing is deliberately
    /// strict: the queue is a trust boundary (the runner writes it, the mod
    /// executes it), so unknown commands, bad arity, and non-numeric
    /// coordinates are rejected before any game state is touched.
    /// </summary>
    public sealed class QueueCommand
    {
        public QueueCommand(string name, IReadOnlyList<string> arguments)
        {
            Name = name;
            Arguments = arguments;
        }

        public string Name { get; }

        public IReadOnlyList<string> Arguments { get; }

        public int GetInt(int index)
        {
            return int.Parse(Arguments[index], NumberStyles.Integer, CultureInfo.InvariantCulture);
        }

        public float GetFloat(int index)
        {
            return float.Parse(Arguments[index], NumberStyles.Float, CultureInfo.InvariantCulture);
        }

        public string GetString(int index)
        {
            return Arguments[index];
        }

        public static bool TryParse(string line, out QueueCommand? command, out string error)
        {
            command = null;
            error = string.Empty;

            if (line == null)
            {
                error = "null-line";
                return false;
            }

            string trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith("#", StringComparison.Ordinal))
            {
                error = "empty";
                return false;
            }

            string[] parts = trimmed.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
            {
                error = "empty";
                return false;
            }

            string name = parts[0];
            var args = new List<string>(parts.Length - 1);
            for (int i = 1; i < parts.Length; i++)
            {
                args.Add(parts[i]);
            }

            if (!TryGetArity(name, out int min, out int max))
            {
                error = "unknown-command";
                return false;
            }

            if (args.Count < min || args.Count > max)
            {
                error = "bad-arity";
                return false;
            }

            if (!Validate(name, args, out error))
            {
                return false;
            }

            command = new QueueCommand(name, args);
            return true;
        }

        private static bool TryGetArity(string name, out int min, out int max)
        {
            switch (name)
            {
                case "lr_set_soil_health":
                case "lr_plant_crop":
                    min = 3;
                    max = 4;
                    return true;
                case "lr_place_bin":
                case "lr_apply_agrochemical":
                    min = 2;
                    max = 3;
                    return true;
                case "lr_insert_waste":
                    min = 4;
                    max = 5;
                    return true;
                case "lr_give_agrochemical":
                case "lr_advance_days":
                case "lr_dump_crop_phase":
                case "lr_dump_soil_health":
                    min = 1;
                    max = 1;
                    return true;
                case "lr_harvest_matrix":
                    min = 2;
                    max = 2;
                    return true;
                case "lr_quit":
                    min = 0;
                    max = 0;
                    return true;
                default:
                    min = 0;
                    max = 0;
                    return false;
            }
        }

        private static bool Validate(string name, List<string> args, out string error)
        {
            error = string.Empty;
            switch (name)
            {
                case "lr_set_soil_health":
                    if (!IsInt(args[0]) || !IsInt(args[1]) || !IsFloat(args[2]))
                    {
                        error = "bad-number";
                        return false;
                    }

                    if (args.Count == 4 && !IsSafeLabel(args[3]))
                    {
                        error = "bad-location";
                        return false;
                    }

                    return true;
                case "lr_place_bin":
                case "lr_apply_agrochemical":
                    if (!IsInt(args[0]) || !IsInt(args[1]))
                    {
                        error = "bad-number";
                        return false;
                    }

                    if (args.Count == 3 && !IsSafeLabel(args[2]))
                    {
                        error = "bad-location";
                        return false;
                    }

                    return true;
                case "lr_insert_waste":
                    if (!IsInt(args[0]) || !IsInt(args[1]) || !IsInt(args[3]))
                    {
                        error = "bad-number";
                        return false;
                    }

                    if (!IsSafeLabel(args[2]))
                    {
                        error = "bad-item";
                        return false;
                    }

                    if (args.Count == 5 && !IsSafeLabel(args[4]))
                    {
                        error = "bad-location";
                        return false;
                    }

                    return true;
                case "lr_give_agrochemical":
                    if (!IsInt(args[0]) || int.Parse(args[0], CultureInfo.InvariantCulture) <= 0)
                    {
                        error = "bad-count";
                        return false;
                    }

                    return true;
                case "lr_harvest_matrix":
                    if (!IsInt(args[0]) || !IsInt(args[1]))
                    {
                        error = "bad-number";
                        return false;
                    }

                    return true;
                case "lr_plant_crop":
                    if (!IsInt(args[0]) || !IsInt(args[1]))
                    {
                        error = "bad-number";
                        return false;
                    }

                    if (!IsSafeLabel(args[2]))
                    {
                        error = "bad-seed";
                        return false;
                    }

                    if (args.Count == 4 && !IsSafeLabel(args[3]))
                    {
                        error = "bad-location";
                        return false;
                    }

                    return true;
                case "lr_advance_days":
                    if (!IsInt(args[0]) || int.Parse(args[0], CultureInfo.InvariantCulture) < 0)
                    {
                        error = "bad-days";
                        return false;
                    }

                    return true;
                case "lr_dump_crop_phase":
                case "lr_dump_soil_health":
                    if (!IsSafeLabel(args[0]))
                    {
                        error = "bad-label";
                        return false;
                    }

                    return true;
                case "lr_quit":
                    return true;
                default:
                    error = "unknown-command";
                    return false;
            }
        }

        private static bool IsInt(string value)
        {
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _);
        }

        private static bool IsFloat(string value)
        {
            return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out _);
        }

        /// <summary>
        /// Labels become file-name components, so only an explicit safe set is
        /// accepted. This is the verification mod's path-traversal guard.
        /// </summary>
        public static bool IsSafeLabel(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length > 64)
            {
                return false;
            }

            foreach (char c in value)
            {
                bool safe = (c >= 'a' && c <= 'z')
                    || (c >= 'A' && c <= 'Z')
                    || (c >= '0' && c <= '9')
                    || c == '_' || c == '-' || c == '.';
                if (!safe)
                {
                    return false;
                }
            }

            return value != "." && value != "..";
        }
    }
}
