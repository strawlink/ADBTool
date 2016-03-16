using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ADBTool
{
    public class Settings
    {
        public string ADBPath = string.Empty;
        public string SelectedListObject = string.Empty;

        public List<ListObject> ItemCollection = new List<ListObject>();
    }
}
