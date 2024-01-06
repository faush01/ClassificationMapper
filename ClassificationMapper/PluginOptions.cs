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

using MediaBrowser.Model.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassificationMapper
{
    public class PluginOptions
    {
        public Dictionary<string, List<string>> Mappings { get; set; } = new Dictionary<string, List<string>>();
        public bool IncludeMovies { get; set; } = true;
        public bool IncludeSeries { get; set; } = false;
        public bool IncludeCorrect { get; set; } = true;
        public bool OverrideLocked { get; set; } = false;
        public int FieldLockAction { get; set; } = 0;
    }
}
