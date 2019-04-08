using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Xml
{
    /// <summary>
    /// Represents an Xml tag that may have attributes and a value but 
    /// has no child nodes.  (e.g. Not a tree)
    /// </summary>
    public class SimpleXmlTag
    {
        /// <summary>
        /// Name of this Tag
        /// </summary>
        public string TagName
        {
            get;
            set;
        }
        
        /// <summary>
        /// True if there are no attributes and no value.
        /// </summary>
        public bool IsEmpty
        {
            get
            {
                return Attributes.Keys.Count <= 0 && string.IsNullOrEmpty(Value);
            }
        }

        /// <summary>
        /// Pass "" to get this.Value,
        /// otherwise returns the value of the requested attribute.
        /// If the requested attribute does not exist, returns "".
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string this[string key]
        {
            get
            {
                if (string.IsNullOrEmpty(key)) return this.Value;
                if (this.Attributes.ContainsKey(key))
                    return Attributes[key];
                else return "";
            }
        }

        /// <summary>
        /// Attributes of this Xml tag.
        /// </summary>
        public Dictionary<string, string> Attributes
        {
            get;
            set;
        }

        /// <summary>
        /// Value contained in this Xml tag. May be empty.
        /// </summary>
        public string Value
        {
            get;
            set;
        }

        /// <summary>
        /// Returns one line standard format XmlTag.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string str = "<" + TagName;
            foreach(string key in Attributes.Keys)
            {
                str += string.Format(" {0}=\"{1}\"", key, Attributes[key]);
            }
            if (string.IsNullOrEmpty(Value))
            {
                str += "/>";
            }
            else
            {
                str += string.Format(">{0}</{1}>", Value, TagName);
            }
            return str;
        }

        /// <summary>
        /// Initialize an empty SimpleXmlTag.
        /// </summary>
        public SimpleXmlTag()
        {
            Attributes = new Dictionary<string, string>();
        }

        /// <summary>
        /// Initialize a SimpleXML Tag with no children.
        /// Read a tag of the format <tag attr="something" />
        /// or <tag attr="something">stuff</tag>
        /// </summary>
        /// <param name="xml">Xml string with no child nodes.</param>
        public SimpleXmlTag(string xml)
        {
            bool selfclosed = XmlTree.SelfClosingTag.IsMatch(xml);

            Attributes = new Dictionary<string,string>();

            Regex tagOpen = new Regex("<(?<tag>([a-zA-Z_]+[a-zA-Z_\\d]*))(\\s+[a-zA-Z_]+[a-zA-Z_\\d]*=[\"']+[^\"]*[\"']+)*(\\s*/)?>");
            string opening = tagOpen.Match(xml).Value;
            xml = xml.Replace(opening, "");
            opening = opening.Replace("<", "").Replace(">", "");

            if (selfclosed)
                opening = opening.Replace("/", "");

            string[] parts = Regex.Split(opening, @"[\s]+");
            TagName = parts[0];

            bool quoteOpen = false;
            string combined = "";

            List<string> fixedParts = new List<string>();

            for(int i = 0; i < parts.Length; i++)
            {
                string part = parts[i];
                if (part.Where(ch => ch == '\"').Count() % 2 == 1)
                {
                    if(!quoteOpen)
                    {
                        combined = part;
                        quoteOpen = true;
                    }
                    else
                    {
                        combined = string.Format("{0} {1}", combined, part);
                        fixedParts.Add(combined);
                        quoteOpen = false;
                    }
                }
                else if(quoteOpen)
                {
                    combined = string.Format("{0} {1}", combined, part);
                }
                else
                {
                    fixedParts.Add(part);
                }
            }

            parts = fixedParts.ToArray();

            for(int i = 1; i < parts.Length; i++)
            {
                if (parts[i].Equals("")) continue;
                string[] attr = parts[i].Split('=');
                Attributes.Add(attr[0], attr[1].Replace("'", "").Replace("\"", ""));
            }
            if (selfclosed) Value = "";
            else Value = xml.Replace(string.Format("</{0}>", TagName), "");
        }
    }
}
