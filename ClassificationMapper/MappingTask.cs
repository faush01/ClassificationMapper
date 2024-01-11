/*
Copyright(C) 2024

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program. If not, see<http://www.gnu.org/licenses/>.
*/

using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Tasks;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Common;
using System.Linq;
using MediaBrowser.Model.Entities;
using System.Reflection;

namespace ClassificationMapper
{
    public class MappingTask : IScheduledTask
    {
        private readonly ILogger _logger;
        private readonly ILibraryManager _libraryManager;
        private readonly IFileSystem _fileSystem;
        private readonly ILibraryMonitor _libraryMonitor;
        private readonly IMediaProbeManager _mediaProbeManager;
        private readonly IItemRepository _itemRepository;
        private readonly IServerConfigurationManager _config;
        private readonly IXmlSerializer _xmlSerializer;
        private readonly IApplicationHost _appHost;

        public MappingTask(ILibraryManager libraryManager,
            ILogger logger,
            IFileSystem fileSystem,
            ILibraryMonitor libraryMonitor,
            IMediaProbeManager prob,
            IItemRepository itemRepository,
            IServerConfigurationManager config,
            IXmlSerializer xmlSerializer,
            IApplicationHost appHost)
        {
            _libraryManager = libraryManager;
            _logger = logger;
            _fileSystem = fileSystem;
            _libraryMonitor = libraryMonitor;
            _mediaProbeManager = prob;
            _itemRepository = itemRepository;
            _config = config;
            _xmlSerializer = xmlSerializer;
            _appHost = appHost;
        }

        public async Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            _logger.Info("ClassificationMapper - Task Execute");

            ConfigStore config_store = ConfigStore.GetInstance(_appHost);
            PluginOptions config = config_store.GetConfig();

            // build lookup table
            Dictionary<string, string> lookup_table = new Dictionary<string, string>();
            foreach(KeyValuePair<string, List<string>> target_mapping in config.Mappings)
            {
                foreach(string mapping in target_mapping.Value)
                {
                    lookup_table[mapping] = target_mapping.Key;
                }
            }

            // query the items
            InternalItemsQuery query = new InternalItemsQuery();
            query.IncludeItemTypes = new string[] { "Movie", "Series" };
            //query.ExcludeItemTypes = new string[] { "Folder", "CollectionFolder", "UserView", "Season", "Trailer", "Playlist" };

            BaseItem[] results = _libraryManager.GetItemList(query);
            List<BaseItem> updated_items = new List<BaseItem>();
            foreach (BaseItem item in results)
            {
                bool item_needs_saving = false;

                if (config.OverrideLocked || (!item.IsLocked && !item.IsFieldLocked(MetadataFields.OfficialRating)))
                {
                    string official_rating = item.OfficialRating;
                    if (string.IsNullOrEmpty(official_rating))
                    {
                        official_rating = "none";
                    }
                    //_logger.Info(item.Name + " - " + item.OfficialRating);
                    if (lookup_table.ContainsKey(official_rating))
                    {
                        if (config.BackupOriginal)
                        {
                            // remove existing backup of original classification
                            foreach (string tag in item.Tags)
                            {
                                if (tag.StartsWith("OC-"))
                                {
                                    item.RemoveTag(tag);
                                }
                            }
                            string oc_tag = "OC-" + official_rating;
                            item.AddTag(oc_tag);
                        }

                        _logger.Info("ClassificationMapper Mapping - " + item.Name + " - " + official_rating + " to " + lookup_table[official_rating]);
                        item.OfficialRating = lookup_table[official_rating];
                        item_needs_saving = true;
                    }
                }

                // process locking option
                //bool is_locked = item.IsFieldLocked(MetadataFields.OfficialRating);
                bool? target_lock_state = null;
                if(config.FieldLockAction == 1 && item_needs_saving)
                {
                    target_lock_state = true;
                }
                else if (config.FieldLockAction == 2 && item_needs_saving)
                {
                    target_lock_state = false;
                }
                else if(config.FieldLockAction == 3) 
                {
                    target_lock_state = true;
                }
                else if (config.FieldLockAction == 4) 
                {
                    target_lock_state = false;
                }

                if(target_lock_state != null)
                {
                    if (target_lock_state.Value == true)
                    {
                        item_needs_saving |= SetFieldLocked(MetadataFields.OfficialRating, item);
                    }
                    else if(target_lock_state.Value == false)
                    {
                        item_needs_saving |= SetFieldUnlocked(MetadataFields.OfficialRating, item);
                    }
                }

                if(item_needs_saving)
                {
                    updated_items.Add(item);
                }
            }

            _logger.Info("ClassificationMapper - Saving Items : " + updated_items.Count);
            _itemRepository.SaveItems(updated_items, cancellationToken);

            _logger.Info("ClassificationMapper - Task Complete");
        }

        public string Category
        {
            get { return "Classification Mapper"; }
        }

        public string Key
        {
            get { return "ClassificationMapper"; }
        }

        public string Description
        {
            get { return "Run classification mapping"; }
        }

        public string Name
        {
            get { return "Map Classifications"; }
        }

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            return new[]
                {
                    new TaskTriggerInfo
                    {
                        Type = TaskTriggerInfo.TriggerDaily,
                        TimeOfDayTicks = TimeSpan.FromHours(2).Ticks,
                        MaxRuntimeTicks = TimeSpan.FromHours(24).Ticks
                    }
                };
        }

        private bool SetFieldLocked(MetadataFields field, BaseItem item)
        {
            List<MetadataFields> locked = item.LockedFields.ToList<MetadataFields>();
            if (locked.IndexOf(field) == -1)
            {
                locked.Add(MetadataFields.OfficialRating);
                item.LockedFields = locked.ToArray<MetadataFields>();
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool SetFieldUnlocked(MetadataFields field, BaseItem item)
        {
            List<MetadataFields> locked = item.LockedFields.ToList<MetadataFields>();
            int index = locked.IndexOf(field);
            if (index > -1)
            {
                while (index > -1)
                {
                    locked.RemoveAt(index);
                    index = locked.IndexOf(field);
                }
                item.LockedFields = locked.ToArray<MetadataFields>();
                return true;
            }
            else
            {
                return false;
            }
        }

    }
}
