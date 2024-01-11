using MediaBrowser.Common;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Tasks;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using MediaBrowser.Controller.Entities;

namespace ClassificationMapper
{
    public class RestoreTask : IScheduledTask
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

        public RestoreTask(ILibraryManager libraryManager,
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
            _logger.Info("ClassificationMapper - RestoreTask Started");

            InternalItemsQuery query = new InternalItemsQuery();
            query.IncludeItemTypes = new string[] { "Movie", "Series" };
            //query.ExcludeItemTypes = new string[] { "Folder", "CollectionFolder", "UserView", "Season", "Trailer", "Playlist" };

            BaseItem[] results = _libraryManager.GetItemList(query);
            List<BaseItem> updated_items = new List<BaseItem>();
            foreach (BaseItem item in results)
            {
                string backup_classification = null;
                string[] tags = item.Tags;
                foreach(string tag in tags)
                {
                    if(tag.StartsWith("OC-"))
                    {
                        backup_classification = tag;
                        break;
                    }
                }
                if(backup_classification != null)
                {
                    item.RemoveTag(backup_classification);
                    backup_classification = backup_classification.Substring(3);
                    _logger.Info("ClassificationMapper - Restoring (" + item.Name + ") from:" + item.OfficialRating + " to:" + backup_classification);
                    item.OfficialRating = backup_classification;
                    updated_items.Add(item);
                }
            }

            _logger.Info("ClassificationMapper - Saving Items : " + updated_items.Count);
            _itemRepository.SaveItems(updated_items, cancellationToken);

            _logger.Info("ClassificationMapper - RestoreTask Complete");
        }

        public string Category
        {
            get { return "Classification Mapper"; }
        }

        public string Key
        {
            get { return "ClassificationMapperRestore"; }
        }

        public string Description
        {
            get { return "Run classification restore from tags"; }
        }

        public string Name
        {
            get { return "Restore Classifications"; }
        }

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            return new TaskTriggerInfo[0];
        }

    }
}
