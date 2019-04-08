using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.IO;

namespace Xml
{
    public class XmlTree
    {
        public static readonly Regex header = new Regex(@"<\?[^\?]*\?>", RegexOptions.Singleline);
        public static readonly Regex tree = new Regex("<(?<root>([a-zA-Z_]+[a-zA-Z_\\d]*))( [a-zA-Z_]+[a-zA-Z_\\d]*=[\"']+[^\"]*[\"']+)*>.*</\\k<root>>", RegexOptions.Singleline);
        public static Regex XMLTagEx = new Regex("<(?<tag>([a-zA-Z_]+[a-zA-Z_\\d]*))( [a-zA-Z_]+[a-zA-Z_\\d]*=[\"']+[^\"]*[\"']+)*>[^<]*</\\k<tag>>");
        public static Regex SelfClosingTag = new Regex("<([a-zA-Z_]+[a-zA-Z_\\d]*)( [a-zA-Z_]+[a-zA-Z_\\d]*=[\"']+[^\"]*[\"']+)*\\s*\\/>");
        public static Regex OpeningTag = new Regex("<(?<root>([a-zA-Z_]+[a-zA-Z_\\d]*))( [a-zA-Z_]+[a-zA-Z_\\d]*=[\"']+[^\"]*[\"']+)*>");
        
        public bool IsEmpty
        {
            get
            {
                return ChildNodes.Count <= 0 && ChildTrees.Count <= 0;
            }
        }


        public SimpleXmlTag this[string tag, string attribute, string value]
        {
            get
            {
                foreach(SimpleXmlTag xmlTag in ChildNodes.Where(node => node.TagName.Equals(tag)))
                {
                    if (xmlTag[attribute].Equals(value))
                        return xmlTag;
                }

                return null;
            }
        }

        public static string MakeXMLAttributeValueSafe(string str)
        {
            Regex unsafeChars = new Regex(@"[^a-zA-Z_\d]");
            str = str.Replace(" ", "_");
            foreach (Match match in unsafeChars.Matches(str))
            {
                str = str.Replace(match.Value, "");
            }

            return str;
        }

        /// <summary>
        /// Sub-Trees of this tree.
        /// </summary>
        public List<XmlTree> ChildTrees
        {
            get;
            protected set;
        }
        
        /// <summary>
        /// All the leaves at this branch of the tree.
        /// </summary>
        public List<SimpleXmlTag> ChildNodes
        {
            get;
            protected set;
        }
        
        public object this[string key]
        {
            get
            {
                if (this.ChildTrees.Where(node => node.TagName.Equals(key)).Count() == 1)
                    return this.ChildTrees.Where(node => node.TagName.Equals(key)).Single();

                if (this.ChildNodes.Where(node => node.TagName.Equals(key)).Count() == 1)
                    return this.ChildNodes.Where(node => node.TagName.Equals(key)).Single();

                if(Attributes.ContainsKey(key))
                    return Attributes[key];

                return null;
            }
        }

        public Dictionary<string, string> Attributes
        {
            get;
            protected set;
        }

        public string TagName
        {
            get;
            protected set;
        }
        
        public override string ToString()
        {
            string xml = "<" + TagName;
            foreach (string attr in Attributes.Keys)
            {
                xml += string.Format(" {0}=\"{1}\"", attr, Attributes[attr]);
            }
            xml += ">";
            foreach(XmlTree ct in ChildTrees)
            {
                xml += "\r\n" + ct.ToString();
            }

            foreach(SimpleXmlTag tag in ChildNodes)
            {
                xml += "\r\n" + tag.ToString();
            }

            xml += "\r\n</" + TagName + ">";

            return xml;
        }

        /// <summary>
        /// Looks for parallel tags at the root of a Xml String.
        /// </summary>
        /// <param name="xml"></param>
        /// <returns></returns>
        public static List<string> GetParallelRoots(string xml)
        {
            Dictionary<string, int> openTagsCounters = new Dictionary<string, int>();
            
            List<string> roots = new List<string>();

            string root = "";
            while (!xml.Equals(""))
            {
                int firstOpenChar = xml.IndexOf('<');
                int firstCloseChar = xml.IndexOf('>');
                string tag = xml.Substring(firstOpenChar, firstCloseChar - firstOpenChar + 1);
                tag = tag.Replace("<","").Replace(">","");
                bool selfClosing = false;
                if(tag.EndsWith("/"))
                {
                    // ignore self closing tag!
                    selfClosing = true;
                }
                tag = tag.Split(' ')[0];

                if(selfClosing)
                {
                    if (!openTagsCounters.ContainsKey(tag))
                        openTagsCounters.Add(tag, 0);
                }
                else if (tag[0] != '/')
                {
                    if (openTagsCounters.ContainsKey(tag))
                    {
                        openTagsCounters[tag]++;
                    }
                    else
                    {
                        openTagsCounters.Add(tag, 1);
                    }
                }
                else
                {
                    tag = tag.Substring(1);
                    if (openTagsCounters.ContainsKey(tag))
                    {
                        openTagsCounters[tag]--;
                    }
                    else
                    {
                        // closed an unopened tag!
                        throw new XmlException("Invalid XML Structure!");
                    }
                }

                bool rootRetuned = true;

                foreach (string key in openTagsCounters.Keys)
                {
                    if (openTagsCounters[key] > 0) rootRetuned = false;
                }

                root += xml.Substring(0, firstCloseChar + 1);
                if (rootRetuned)
                {
                    roots.Add(root);
                    root = "";
                }

                xml = xml.Substring(firstCloseChar + 1);
            }

            foreach (string key in openTagsCounters.Keys)
            {
                if (openTagsCounters[key] != 0)
                    throw new XmlException("Incomplete XML Tree!");
            }

            return roots;
        }

        /// <summary>
        /// Strips \r \n and \t characters from the data.
        /// </summary>
        /// <param name="xml"></param>
        /// <returns></returns>
        protected static string StripFormatting(string xml)
        {
            xml = xml.Replace("\r", "").Replace("\n", "").Replace("\t", "");
            return xml;
        }

        /// <summary>
        /// Check to see if a given xml string is valid for using this library.
        /// </summary>
        /// <param name="xml"></param>
        /// <returns></returns>
        public static bool IsTree(string xml)
        {
            if (!tree.IsMatch(xml))
                return false;
            Dictionary<string, int> openTagsCounters = new Dictionary<string, int>();
            
            foreach(Match match in SelfClosingTag.Matches(xml))
            {
                // ignore self closing tags.
                xml = xml.Replace(match.Value, "");
            }

            while (!xml.Equals(""))
            {
                int firstOpenChar = xml.IndexOf('<');
                int firstCloseChar = xml.IndexOf('>');

                string tag = xml.Substring(firstOpenChar, firstCloseChar - firstOpenChar+1);
                tag = tag.Substring(1, tag.Length - 2);
                tag = tag.Split(' ')[0];

                if(tag[0] != '/')
                {
                    if(openTagsCounters.ContainsKey(tag))
                    {
                        openTagsCounters[tag]++;
                    }
                    else
                    {
                        openTagsCounters.Add(tag, 1);
                    }
                }
                else
                {
                    tag = tag.Substring(1);
                    if (openTagsCounters.ContainsKey(tag))
                    {
                        openTagsCounters[tag]--;
                    }
                    else
                    {
                        // closed an unopened tag!
                        return false;
                    }
                }

                xml = xml.Substring(firstCloseChar + 1);
            }

            foreach(string key in openTagsCounters.Keys)
            {
                if (openTagsCounters[key] != 0) 
                    return false;
            }

            return true;
        }
        
        /// <summary>
        /// Merge two xmltags
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public XmlTree MergeWith(XmlTree other)
        {
            bool merge = true;
            if(this.TagName.Equals(other.TagName))
            {
                foreach(string attr in this.Attributes.Keys)
                {
                    if(other.Attributes.ContainsKey(attr) && !this.Attributes[attr].Equals(other.Attributes[attr]))
                    {
                        merge = false;
                    }
                }
            }
            else
            {
                merge = false;
            }

            if(merge)
            {
                XmlTree tree = new XmlTree()
                {
                    TagName = this.TagName
                };
                foreach(string attr in this.Attributes.Keys)
                {
                    tree.Attributes.Add(attr, this.Attributes[attr]);
                }
                foreach(string attr in other.Attributes.Keys)
                {
                    if(!tree.Attributes.ContainsKey(attr))
                    {
                        tree.Attributes.Add(attr, other.Attributes[attr]);
                    }
                }

                tree.ChildNodes.AddRange(this.ChildNodes);
                tree.ChildNodes.AddRange(other.ChildNodes);
                tree.ChildTrees.AddRange(this.ChildTrees);
                tree.ChildTrees.AddRange(other.ChildTrees);

                return tree;
            }
            else if(this.TagName.Equals("merge"))
            {
                XmlTree tree = new XmlTree()
                {
                    TagName = "merge"
                };
                tree.ChildTrees.AddRange(this.ChildTrees);
                tree.ChildNodes.AddRange(this.ChildNodes);
                tree.ChildTrees.Add(other);
                return tree;
            }
            else if (other.TagName.Equals("merge"))
            {
                XmlTree tree = new XmlTree()
                {
                    TagName = "merge"
                };
                tree.ChildTrees.AddRange(other.ChildTrees);
                tree.ChildNodes.AddRange(other.ChildNodes);
                tree.ChildTrees.Add(this);
                return tree;
            } 
            else
            {
                XmlTree tree = new XmlTree()
                {
                    TagName = "merge"
                };

                tree.ChildTrees.Add(this);
                tree.ChildTrees.Add(other);
                return tree;
            }

        }

        /// <summary>
        /// Recursive search of the XMLTree for a given tag.
        /// Result:
        /// 
        /// &lt;result&gt; DATA &lt;/result&gt;
        /// 
        /// 
        /// </summary>
        /// <param name="tagname"></param>
        /// <returns></returns>
        public XmlTree Search(string tagname)
        {
            XmlTree tree = new XmlTree();
            tree.TagName = "result";
            foreach(XmlTree subtree in this.ChildTrees)
            {
                if (subtree.TagName.Equals(tagname))
                    tree.ChildTrees.Add(subtree);

                XmlTree result = subtree.Search(tagname);
                tree.ChildTrees.AddRange(result.ChildTrees);
                tree.ChildNodes.AddRange(result.ChildNodes);
            }
            foreach(SimpleXmlTag tag in this.ChildNodes)
            {
                if(tag.TagName.Equals(tagname))
                {
                    tree.ChildNodes.Add(tag);
                }
            }

            return tree;
        }

        /// <summary>
        /// Save the XMLTree to an XML file.
        /// </summary>
        /// <param name="fileName"></param>
        public void Save(string fileName, FileMode mode = FileMode.Create)
        {
            if (mode == FileMode.Create && File.Exists(fileName))
                File.Delete(fileName);

            StreamWriter writer = new StreamWriter(new FileStream(fileName, mode));
            writer.Write(this.ToString());
            writer.Flush();
            writer.Close();
        }

        /// <summary>
        /// Create an empty XMLTree.
        /// </summary>
        public XmlTree()
        {
            ChildNodes = new List<SimpleXmlTag>();
            ChildTrees = new List<XmlTree>();
            Attributes = new Dictionary<string, string>();
        }
        
        /// <summary>
        /// Build an XMLTree from an XML formatted string.
        /// </summary>
        /// <param name="xml"></param>
        /// <exception cref="XmlException"></exception>
        /// <exception cref="UnseparatedChildrenException"></exception>
        public XmlTree(string xml)
        {
            foreach (Match match in header.Matches(xml))
                xml = xml.Replace(match.Value, "");

            xml = StripFormatting(xml);
            
            if (!IsTree(xml))
                throw new XmlException(string.Format("Cannot Identify root tag in:  {0}", xml));

            ChildNodes = new List<SimpleXmlTag>();
            ChildTrees = new List<XmlTree>();
            
            string opening = OpeningTag.Match(xml).Value;
            int start = xml.IndexOf(opening);
            opening = opening.Replace("<", "").Replace(">", "");
            string[] parts = Regex.Split(opening, @"[\s]+");
            TagName = parts[0];

            // some attributes may contain spaces between quotes.
            // combine parts from this split list where a quote is left open.
            // posibly contains a bug if 
            bool quoteOpen = false;
            string combined = "";

            List<string> fixedParts = new List<string>();

            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i];
                if (part.Where(ch => ch == '\"').Count() % 2 == 1)
                {
                    if (!quoteOpen)
                    {
                        combined = part;
                        quoteOpen = true;
                    }
                    else
                    {
                        combined = string.Format("{0} {1}", combined, part);
                        fixedParts.Add(combined);
                        combined = "";
                        quoteOpen = false;
                    }
                }
                else if (quoteOpen)
                {
                    combined = string.Format("{0} {1}", combined, part);
                }
                else
                {
                    fixedParts.Add(part);
                }
            }

            if (!string.IsNullOrEmpty(combined))
                throw new XmlException("Confused Xml Attributes left unclosed quotations.");

            parts = fixedParts.ToArray();

            Regex closingTag = new Regex(string.Format("</{0}>", TagName));

            Regex sameTags = new Regex(string.Format("<{0}( [a-zA-Z_]+[a-zA-Z_\\d]*=[\"']+[^\"']*[\"']+)*>", TagName));
            if (sameTags.Matches(xml).Count > 1)
            {
                List<string> xmlstrings = GetParallelRoots(xml);
                
                if (xmlstrings.Count == 1)
                {
                    xml = xmlstrings[0];
                }
                else
                {
                    throw new UnseparatedChildrenException("XML contains multiple sister tags.", xmlstrings);
                }
            }
            xml = xml.Substring(start + opening.Length + 2);
            int end = xml.LastIndexOf(closingTag.Match(xml).Value);
            xml = xml.Substring(0, end);
            Attributes = new Dictionary<string,string>();
            for (int i = 1; i < parts.Length; i++)
            {
                string[] attr = parts[i].Split('=');
                Attributes.Add(attr[0], attr[1].Replace("'", "").Replace("\"", ""));
            }

            // Get parallel children.
            List<string> children = GetParallelRoots(xml);

            foreach(string child in children)
            {
                if (child.ToCharArray().ToList().Where(c => c == '<').Count() <= 2)
                {
                    ChildNodes.Add(new SimpleXmlTag(child));
                }
                else
                {
                    ChildTrees.Add(new XmlTree(child));
                }
            }
        }
    }
}
