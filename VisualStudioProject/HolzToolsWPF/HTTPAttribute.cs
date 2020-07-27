using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HolzTools
{
    public class HTTPAttribute
    {
        public HTTPAttribute(string attributeName, string attributeValue)
        {
            AttributeName = attributeName;
            AttributeValue = attributeValue;
        }

        public string AttributeName { get; set; }
        public string AttributeValue { get; set; }
    }
}
