using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using CriticalCommonLib;
using CriticalCommonLib.Services.Ui;
using Dalamud.Logging;
using InventoryTools.Logic;

namespace InventoryTools.GameUi
{
    public class RetainerListOverlay : AtkRetainerList, IAtkOverlayState 
    {
        public override bool ShouldDraw { get; set; }

        public override unsafe bool Draw()
        {
            var atkUnitBase = AtkUnitBase;
            if (atkUnitBase != null && HasState)
            {
                this.SetNames(RetainerNames, RetainerColors);
                return true;
            }

            return false;
        }
        
        public unsafe void Clear()
        {
            var atkUnitBase = AtkUnitBase;
            if (atkUnitBase != null)
            {
                this.SetNames(RetainerNames, new Dictionary<ulong, Vector4>());
            }
        }

        public override void Setup()
        {
            
        }


        public Dictionary<ulong, string> RetainerNames = new Dictionary<ulong, string>();
        public Dictionary<ulong, Vector4> RetainerColors = new Dictionary<ulong, Vector4>();
        

        public bool HasState { get; set; }
        public bool NeedsStateRefresh { get; set; }

        public void UpdateState(FilterState? newState)
        {
            if (PluginService.CharacterMonitor.ActiveCharacter == 0)
            {
                return;
            }
            if (AtkUnitBase != null && newState != null)
            {
                HasState = true;
                var filterResult = newState.Value.FilterResult;
                var filterConfiguration = newState.Value.FilterConfiguration;
                var currentCharacterId = Service.ClientState.LocalContentId;
                if (newState.Value.ShouldHighlight && filterResult.HasValue)
                {
                    if (filterResult.Value.AllItems.Count != 0)
                    {
                        var allItems = filterResult.Value.AllItems;
                        Dictionary<ulong, HashSet<uint>> hasItems = new ();
                        Dictionary<ulong, int> characterTotals = new();

                        foreach (var character in PluginService.CharacterMonitor.GetRetainerCharacters(PluginService
                            .CharacterMonitor.ActiveCharacter))
                        {
                            hasItems[character.Key] = new HashSet<uint>();
                            characterTotals[character.Key] = 0;
                        }
                        foreach (var item in PluginService.InventoryMonitor.AllItems)
                        {
                            if (hasItems.ContainsKey(item.RetainerId))
                            {
                                if (!hasItems[item.RetainerId].Contains(item.ItemId))
                                {
                                    hasItems[item.RetainerId].Add(item.ItemId);
                                }
                            }
                        }

                        
                        foreach (var item in allItems)
                        {
                            foreach (var character in hasItems)
                            {
                                if (character.Value.Contains(item.RowId))
                                {
                                    characterTotals[character.Key]++;
                                }
                            }
                        }

                        var finalTotals = characterTotals.Where(c => c.Value != 0).ToList();
                        RetainerNames = finalTotals.ToDictionary(c => c.Key, GenerateNewName);
                        RetainerColors = finalTotals.ToDictionary(c => c.Key,
                            c => filterConfiguration.RetainerListColor ??
                                 PluginLogic.PluginConfiguration.RetainerListColor);
                        Draw();
                        return;

                    }
                    var filteredList = filterResult.Value.SortedItems;
                    if (filterConfiguration.FilterType == FilterType.SortingFilter)
                    {
                        var grouping = filteredList.Where(c =>
                                (c.SourceRetainerId == currentCharacterId || PluginService.CharacterMonitor.BelongsToActiveCharacter(c.SourceRetainerId)) && c.DestinationRetainerId != null)
                            .GroupBy(c => c.DestinationRetainerId!.Value).Where(c => c.Any()).ToList();
                        RetainerColors = grouping.ToDictionary(c => c.Key,
                            c => filterConfiguration.RetainerListColor ??
                                 PluginLogic.PluginConfiguration.RetainerListColor);
                        RetainerNames = grouping.ToDictionary(c => c.Key, GenerateNewName);
                        Draw();
                        return;
                    }
                    else
                    {
                        var grouping = filteredList.GroupBy(c => c.SourceRetainerId).Where(c => c.Any()).ToList();
                        RetainerNames = grouping.ToDictionary(c => c.Key, GenerateNewName);
                        RetainerColors = grouping.ToDictionary(c => c.Key,
                            c => filterConfiguration.RetainerListColor ??
                                 PluginLogic.PluginConfiguration.RetainerListColor);

                        Draw();
                        return;
                    }
                }
            }
            HasState = false;
            RetainerNames = PluginService.CharacterMonitor.Characters.Where(c => c.Value.CharacterId == PluginService.CharacterMonitor.ActiveCharacter).ToDictionary(c => c.Key, c => c.Value.Name);
            Clear();
        }
        
        private unsafe string GenerateNewName(IGrouping<ulong, SortingResult> c)
        {
            if (PluginService.CharacterMonitor.Characters.ContainsKey(c.Key))
            {
                return PluginService.CharacterMonitor.Characters[c.Key].Name + " (" + c.Count() + ")";
            }
            return "Unknown "  + "(" + c.Count() + ")";
        }
        
        private unsafe string GenerateNewName(KeyValuePair<ulong, int> c)
        {
            if (PluginService.CharacterMonitor.Characters.ContainsKey(c.Key))
            {
                return PluginService.CharacterMonitor.Characters[c.Key].Name + " (" + c.Value + ")";
            }
            return "Unknown "  + "(" + c.Value + ")";
        }
    }
}