using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Juvenile_Record_System.Helpers
{
    public static class PermitHelper
    {
        public const string ScanBiometrics = "ScanBiometrics";
        public const string Authenticate = "Authenticate";
        public const string CheckRecords = "CheckRecords";
        public const string ManageProfile = "ManageProfile";
        public const string ProcessPenalty = "ProcessPenalty";
        public const string JuvenileRecords = "JuvenileRecords";

        public static readonly string[] AllCheckpointModules = new[]
        {
            ScanBiometrics,
            Authenticate,
            CheckRecords,
            ManageProfile,
            ProcessPenalty,
            JuvenileRecords
        };

        /// <summary>
        /// Reads allowed modules from Session["AllowedPermits"] (e.g. a HashSet&lt;string&gt; or comma-separated string).
        /// Falls back to role defaults if Session["AllowedPermits"] is not set.
        /// </summary>
        public static HashSet<string> GetAllowedModules(HttpSessionStateBase session)
        {
            if (session == null)
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // 1. If explicit permits were loaded into Session (e.g., from DB/OfficerPermits)
            var stored = session["AllowedPermits"];
            if (stored is IEnumerable<string> permitList)
            {
                return new HashSet<string>(permitList.Where(s => !string.IsNullOrWhiteSpace(s)), StringComparer.OrdinalIgnoreCase);
            }
            if (stored is string csv && !string.IsNullOrWhiteSpace(csv))
            {
                return new HashSet<string>(
                    csv.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(p => p.Trim()),
                    StringComparer.OrdinalIgnoreCase);
            }

            // 2. Role-based defaults when no granular permit list is stored yet
            string role = (session["RoleName"]?.ToString() ?? string.Empty).Trim();

            if (string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(role, "Administrator", StringComparison.OrdinalIgnoreCase))
            {
                return new HashSet<string>(AllCheckpointModules, StringComparer.OrdinalIgnoreCase);
            }

            // Default Enforcement Officer access (adjust to match your exact operational rules)
            return new HashSet<string>(new[]
            {
                ScanBiometrics,
                Authenticate,
                CheckRecords,
                ManageProfile
                // ProcessPenalty and JuvenileRecords require explicit permit assignment
            }, StringComparer.OrdinalIgnoreCase);
        }

        public static bool HasPermit(HttpSessionStateBase session, string moduleKey)
        {
            if (session == null || string.IsNullOrWhiteSpace(moduleKey))
                return false;

            string role = (session["RoleName"]?.ToString() ?? string.Empty).Trim();
            if (string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(role, "Administrator", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return GetAllowedModules(session).Contains(moduleKey);
        }
    }
}