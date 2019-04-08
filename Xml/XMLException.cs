using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xml
{
    public class XmlException : Exception
    {
        public XmlException(string message, Exception innerException = null)
            : base(message, innerException)
        {

        }
    }

    public class UnseparatedChildrenException : XmlException
    {

        public List<string> BrokenStrings
        {
            get;
            protected set;
        }

        /// <summary>
        /// Exception thrown when a document contains multiple tags at the root 
        /// level.  This is handled internally unless something goes wrong.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="separatedTags"></param>
        /// <param name="innerException"></param>
        public UnseparatedChildrenException(string message, List<string> separatedTags, Exception innerException = null)
            : base(message, innerException)
        {
            BrokenStrings = separatedTags;
        }
    }
}
