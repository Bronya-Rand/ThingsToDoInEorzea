using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Dalamud.Game;
using Dalamud.Game.Inventory.InventoryEventArgTypes;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel.Sheets;

namespace ThingsToDoInEorzea.Services.Browser
{
    public sealed class Job
    {
        public uint RowId { get; set; }
        public string Name { get; set; } = string.Empty;
    }
    public class JobBrowserService : LuminaBrowserService<Job, ClassJob>
    {
        private readonly IGameInventory gameInventory;
        private readonly IPlayerState playerState;

        private const int ScholarJobId = 28; // Scholar's ClassJob.RowId

        private readonly Dictionary<uint, HashSet<uint>> categoryAllowedJobsCache = [];
        private readonly HashSet<uint> ownedSoulCrystalsCache = [];
        private bool isSoulCrystalCacheValid;

        private readonly Dictionary<uint, uint> baseClassToAdvancedJob = [];
        private readonly Dictionary<uint, uint> advancedJobToBaseClass = [];

        // Mapping of ClassJobCategory.RowId to booleans indicating if a job is allowed for that category
        private static readonly Dictionary<uint, Func<ClassJobCategory, bool>> CategoryJobCheckers = new()
        {
            { 1, c => c.GLA },
            { 2, c => c.PGL },
            { 3, c => c.MRD },
            { 4, c => c.LNC },
            { 5, c => c.ARC },
            { 6, c => c.CNJ },
            { 7, c => c.THM },
            { 8, c => c.CRP },
            { 9, c => c.BSM },
            { 10, c => c.ARM },
            { 11, c => c.GSM },
            { 12, c => c.LTW },
            { 13, c => c.WVR },
            { 14, c => c.ALC },
            { 15, c => c.CUL },
            { 16, c => c.MIN },
            { 17, c => c.BTN },
            { 18, c => c.FSH },
            { 19, c => c.PLD },
            { 20, c => c.MNK },
            { 21, c => c.WAR },
            { 22, c => c.DRG },
            { 23, c => c.BRD },
            { 24, c => c.WHM },
            { 25, c => c.BLM },
            { 26, c => c.ACN },
            { 27, c => c.SMN },
            { 28, c => c.SCH },
            { 29, c => c.ROG },
            { 30, c => c.NIN },
            { 31, c => c.MCH },
            { 32, c => c.DRK },
            { 33, c => c.AST },
            { 34, c => c.SAM },
            { 35, c => c.RDM },
            { 36, c => c.BLU },
            { 37, c => c.GNB },
            { 38, c => c.DNC },
            { 39, c => c.RPR },
            { 40, c => c.SGE },
            { 41, c => c.VPR },
            { 42, c => c.PCT },
            //{ 43, c => c.BST }, for some reason Beastmaster is not listed yet?
        };

        public JobBrowserService(IDataManager dataManager, ISeStringEvaluator seStringEvaluator, IGameInventory gameInventory, IPlayerState playerState, Configuration configuration, ClientLanguage? overrideLanguage = null) : base(dataManager, seStringEvaluator, configuration, overrideLanguage)
        {
            this.gameInventory = gameInventory;
            this.playerState = playerState;

            var classJobSheet = dataManager.GetExcelSheet<ClassJob>();
            if (classJobSheet != null)
            {
                foreach (var row in classJobSheet)
                {
                    // Initialize Base Class -> Advanced Job mapping
                    // Skip base class rows or advanced jobs (where ClassJobParent.RowId == RowId)
                    if (row.RowId != 0 && row.ClassJobParent.RowId != row.RowId)
                    {
                        // Skip Scholar (28) so Arcanist maps to Summoner (27) as its primary advanced job
                        if (row.RowId == ScholarJobId) continue;

                        baseClassToAdvancedJob[row.ClassJobParent.RowId] = row.RowId;
                    }
                }
                // Create the reverse mapping for advanced jobs to their base classes
                advancedJobToBaseClass = baseClassToAdvancedJob.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);
                advancedJobToBaseClass[ScholarJobId] = 26; // SCH -> ACN
            }
            gameInventory.InventoryChanged += OnInventoryChanged;
        }

        private void OnInventoryChanged(IReadOnlyCollection<InventoryEventArgs> events)
        {
            isSoulCrystalCacheValid = false;
        }

        private unsafe void RefreshSoulCrystalCache()
        {
            var inventory = InventoryManager.Instance();
            if (inventory == null) return;

            ownedSoulCrystalsCache.Clear();
            var equippedItems = inventory->GetInventoryContainer(InventoryType.EquippedItems);
            if (equippedItems != null)
            {
                for (var i = 0; i < equippedItems->Size; i++)
                {
                    var item = equippedItems->GetInventorySlot(i);
                    if (item != null && item->GetItemId() != 0)
                        ownedSoulCrystalsCache.Add(item->GetItemId());
                }
            }

            var soulCrystalInventory = inventory->GetInventoryContainer(InventoryType.ArmorySoulCrystal);
            if (soulCrystalInventory != null)
            {
                for (var i = 0; i < soulCrystalInventory->Size; i++)
                {
                    var item = soulCrystalInventory->GetInventorySlot(i);
                    if (item != null && item->GetItemId() != 0)
                        ownedSoulCrystalsCache.Add(item->GetItemId());
                }
            }

            isSoulCrystalCacheValid = true;
        }

        /// <summary>
        /// Returns whether the player has ever played a specific job via level
        /// </summary>
        /// <param name="jobId"></param>
        /// <returns></returns>
        private bool HasEverPlayedJob(uint jobId)
        {
            var job = dataManager.GetExcelSheet<ClassJob>()?.GetRow(jobId);
            if (job == null) return false;

            var jobLevel = playerState.GetClassJobLevel(job.Value);
            return jobLevel > 0;
        }
        protected override bool TryMapRow(ClassJob row, out uint id, out Job entity)
        {
            id = row.RowId;
            var name = EvaluateSeString(row.Name);

            if (id == 0 || name.IsNullOrEmpty())
            {
                entity = default!;
                return false;
            }

            var textInfo = CultureInfo.CurrentCulture.TextInfo;
            entity = new Job
            {
                RowId = id,
                Name = textInfo.ToTitleCase(name),
            };
            return true;
        }
        public string GetJobName(uint jobId) => TryGet(jobId, out var job) ? job!.Name : $"Unknown Job ({jobId})";
        public ISet<uint> GetAllowedJobIdsForCategory(uint classJobCategoryId)
        {
            if (categoryAllowedJobsCache.TryGetValue(classJobCategoryId, out var allowedJobs))
                return allowedJobs;

            // Retrieve the ClassJobCategory row from Lumina
            var categoryRow = dataManager.GetExcelSheet<ClassJobCategory>()?.GetRow(classJobCategoryId);
            if (categoryRow == null)
            {
                var emptySet = new HashSet<uint>();
                categoryAllowedJobsCache.Add(classJobCategoryId, emptySet);
                return emptySet;
            }

            // Collect all job IDs that are allowed for this category
            var allowedJobIds = new HashSet<uint>();
            foreach (var kvp in CategoryJobCheckers)
            {
                if (kvp.Value.Invoke(categoryRow.Value))
                    allowedJobIds.Add(kvp.Key);
            }

            categoryAllowedJobsCache.Add(classJobCategoryId, allowedJobIds);
            return allowedJobIds;
        }
        public static bool MatchesJob(uint jobId, uint targetJobId)
        {
            if (jobId == targetJobId) return true;
            return false;
        }
        public bool HasSoulCrystal(uint jobRowId)
        {
            // Find soul crystal ID in Lumina
            var job = dataManager.GetExcelSheet<ClassJob>()?.GetRow(jobRowId);
            if (job == null)
                return false;

            var soulCrystalRowId = job.Value.ItemSoulCrystal.RowId;
            if (soulCrystalRowId == 0)
                return false;

            if (!isSoulCrystalCacheValid)
            {
                RefreshSoulCrystalCache();
            }

            return ownedSoulCrystalsCache.Contains(soulCrystalRowId);
        }
        public uint GetEffectiveJobId(uint classJobId)
        {
            if (classJobId == 0) return 0;

            // Case 1: classJobId is a Base Class (e.g. 2 PGL, 3 MRD, 26 ACN)
            if (baseClassToAdvancedJob.TryGetValue(classJobId, out var advancedJobId) && advancedJobId != 0)
            {
                if (HasSoulCrystal(advancedJobId))
                    return advancedJobId;
                return classJobId;
            }

            // Case 2: classJobId is an Advanced Job (e.g. 20 MNK, 21 WAR, 27 SMN, 28 SCH)
            if (advancedJobToBaseClass.TryGetValue(classJobId, out var parentBaseId))
            {
                if (HasSoulCrystal(classJobId))
                    return classJobId;

                // Player does not have the soul crystal -> fallback to base class
                return parentBaseId;
            }

            return classJobId;
        }

        /// <summary>
        /// Returns a set of allowed job IDs based on if the player has the soul crystal for a class/job.
        /// </summary>
        public HashSet<uint> GetFilteredAllowedJobIds(IEnumerable<uint> rawAllowedJobIds)
        {
            var filteredAllowedJobIds = new HashSet<uint>();

            foreach (var jobId in rawAllowedJobIds)
            {
                if (jobId == 0) continue;

                // Scholar special case: If the player has the soul crystal for Scholar (28), add it
                if (jobId == ScholarJobId)
                {
                    if (HasSoulCrystal(ScholarJobId))
                    {
                        if (configuration.HideUnusedJobs && !HasEverPlayedJob(ScholarJobId)) continue;
                        filteredAllowedJobIds.Add(ScholarJobId);
                    }
                    continue;
                }

                // Check if this jobId is an advanced job with a parent class or parent class with an advanced job.
                if (advancedJobToBaseClass.TryGetValue(jobId, out var parentId))
                {
                    var finalJob = HasSoulCrystal(jobId) ? jobId : parentId;
                    if (configuration.HideUnusedJobs && !HasEverPlayedJob(finalJob)) continue;

                    filteredAllowedJobIds.Add(finalJob);
                }
                else if (baseClassToAdvancedJob.TryGetValue(jobId, out var primaryAdvancedId))
                {
                    var finalJob = HasSoulCrystal(primaryAdvancedId) ? primaryAdvancedId : jobId;
                    if (configuration.HideUnusedJobs && !HasEverPlayedJob(finalJob)) continue;

                    filteredAllowedJobIds.Add(finalJob);
                }
                else
                {
                    // Crafters, gatherers, or jobs without base classes (DRK, MCH, AST, SAM, RDM, BLU, GNB, DNC, RPR, SGE, VPR, PCT)
                    if (configuration.HideUnusedJobs && !HasEverPlayedJob(jobId)) continue;
                    filteredAllowedJobIds.Add(jobId);
                }
            }
            return filteredAllowedJobIds;
        }
        public override void Dispose()
        {
            gameInventory.InventoryChanged -= OnInventoryChanged;
            ownedSoulCrystalsCache.Clear();
            GC.SuppressFinalize(this);
            base.Dispose();
        }
    }
}
