using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GunGame.DataSaving
{
    public abstract class ConfigObject
    {
        public abstract string FileName { get; }
        public abstract DataWrapper Wrapper { get; }
    }
}
