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

using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.IO;

namespace ClassificationMapper
{
    public class Plugin : BasePlugin<BasePluginConfiguration>, IHasThumbImage, IHasWebPages
    {
        public static Plugin Instance { get; private set; }
        public static string PluginName = "Classification Mapper";
        private Guid _id = new Guid("219b4d67-3c8b-4371-8453-0c64696b3d3c");

        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer) : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
        }

        public Stream GetThumbImage()
        {
            var type = GetType();
            return type.Assembly.GetManifestResourceStream(type.Namespace + ".Images.thumb.png");
        }

        public ImageFormat ThumbImageFormat
        {
            get
            {
                return ImageFormat.Png;
            }
        }

        public override string Description
        {
            get
            {
                return "Maps classification ratings to correct values";
            }
        }

        public override string Name
        {
            get { return PluginName; }
        }

        public override Guid Id
        {
            get { return _id; }
        }

        public IEnumerable<PluginPageInfo> GetPages()
        {
            return new[]
            {
                new PluginPageInfo
                {
                    Name = "classification_mapping_options",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.options.html",
                    EnableInMainMenu = true
                },
                new PluginPageInfo
                {
                    Name = "classification_mapping_options.js",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.options.js"
                }
            };
        }

    }
}
