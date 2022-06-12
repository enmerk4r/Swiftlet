using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Swiftlet.DataModels.Interfaces
{
    public interface IKeyValue
    {
        string Key { get; }
        string Value { get; }

        void SetValue(string value);
        string ToJsonString();
    }
}
