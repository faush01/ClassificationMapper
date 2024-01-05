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

using MediaBrowser.Common;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Activity;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ClassificationMapper
{
    public class ConfigStore
    {
        private static ConfigStore instance = null;
        private static readonly object _padlock = new object();
        private static readonly string confgi_file_name = "classification_mapper_config.json";

        private ILogger _logger = null;
        private IJsonSerializer _jsonSerializer;
        private IApplicationPaths _applicationPaths;
        private IFileSystem _fileSystem;

        private ConfigStore()
        {
        }

        private ConfigStore(IApplicationHost appHost)
        {
            _jsonSerializer = (IJsonSerializer)appHost.CreateInstance(typeof(IJsonSerializer));
            _applicationPaths = (IApplicationPaths)appHost.CreateInstance(typeof(IApplicationPaths));
            _logger = (ILogger)appHost.CreateInstance(typeof(ILogger));
            _fileSystem = (IFileSystem)appHost.CreateInstance(typeof(IFileSystem));
        }

        public static ConfigStore GetInstance(IApplicationHost appHost)
        {
            lock (_padlock)
            {
                if (instance == null)
                {
                    instance = new ConfigStore(appHost);
                }
                return instance;
            }
        }

        public PluginOptions GetConfig()
        {
            PluginOptions loaded_config = new PluginOptions();
            string config_path = Path.Combine(_applicationPaths.ConfigurationDirectoryPath, confgi_file_name);
            _logger.Info("Loading plugin config from : " + config_path);

            lock (_padlock)
            {
                if (_fileSystem.FileExists(config_path))
                {
                    string loaded_config_string = _fileSystem.ReadAllText(config_path);
                    loaded_config = _jsonSerializer.DeserializeFromString<PluginOptions>(loaded_config_string);
                }
            }

            return loaded_config;
        }

        public void SaveConfig(PluginOptions config_data)
        {
            string config_path = Path.Combine(_applicationPaths.ConfigurationDirectoryPath, confgi_file_name);
            _logger.Info("Saving plugin config to : " + config_path);

            lock (_padlock)
            {
                string config_string_data = _jsonSerializer.SerializeToString(config_data);
                _logger.Info("Config data : " + config_string_data);
                _fileSystem.WriteAllText(config_path, config_string_data);
            }
        }
    }
}
