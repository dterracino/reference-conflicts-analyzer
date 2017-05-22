﻿using ReferenceConflictAnalyser.DataStructures;
using ReferenceConflictAnalyser.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ReferenceConflictAnalyser
{
    public class GraphBuilder
    {

        public XmlDocument BuildDgml(ReferenceList referenceList)
        {
            _referenceList = referenceList;

            _doc = new XmlDocument();

            var root = AddRootElement();
            AddNodes(root);
            AddLinks(root);
            AddStyles(root);

            return _doc;
        }

        #region private members



        private const string XmlNamespace = "http://schemas.microsoft.com/vs/2009/dgml";

        private readonly Dictionary<Category, Color> _categories = new Dictionary<Category, Color>()
        {
            { Category.EntryPoint, Color.LightGreen },
            { Category.Normal , Color.White },
            { Category.ConflictResolved, Color.Khaki },
            { Category.Conflicted, Color.LightSalmon }
        };

        private ReferenceList _referenceList;
        private XmlDocument _doc;


        private XmlNode AddRootElement()
        {
            var root = _doc.AppendChild(_doc.CreateElement("DirectedGraph", XmlNamespace));
            root.Attributes.Append(AddXmlAtribute("GraphDirection", "BottomToTop"));
            root.Attributes.Append(AddXmlAtribute("Layout", "Sugiyama"));
            return root;
        }

        private void AddNodes(XmlNode parent)
        {
            var nodesElement = parent.AppendChild(_doc.CreateElement("Nodes", XmlNamespace));

            var uniqueAssemblies = new Dictionary<string, Category>();
            foreach (var assembly in _referenceList.Assemblies)
            {
                if (!uniqueAssemblies.ContainsKey(assembly.Key.Name))
                    uniqueAssemblies.Add(assembly.Key.Name, assembly.Value);
            }

            foreach (var item in uniqueAssemblies)
            {
                var nodeElement = _doc.CreateElement("Node", XmlNamespace);
                nodeElement.Attributes.Append(AddXmlAtribute("Id", item.Key.ToLower()));
                nodeElement.Attributes.Append(AddXmlAtribute("Label", item.Key));
                nodeElement.Attributes.Append(AddXmlAtribute("Category", item.Value.ToString()));
                nodesElement.AppendChild(nodeElement);
            }
        }

        private void AddLinks(XmlNode parent)
        {
            var linksElement = parent.AppendChild(_doc.CreateElement("Links", XmlNamespace));
            foreach (var reference in _referenceList.References)
            {
                var elem = _doc.CreateElement("Link", XmlNamespace);
                elem.Attributes.Append(AddXmlAtribute("Source", reference.Assembly.Name.ToLower()));
                elem.Attributes.Append(AddXmlAtribute("Target", reference.ReferencedAssembly.Name.ToLower()));
                elem.Attributes.Append(AddXmlAtribute("Label", reference.ReferencedAssembly.Version.ToString()));
                linksElement.AppendChild(elem);
            }
        }

        private XmlAttribute AddXmlAtribute(string name, string value)
        {
            var attribute = _doc.CreateAttribute(name);
            attribute.Value = value;
            return attribute;
        }

        private void AddStyles(XmlNode parent)
        {
            var stylesElement = parent.AppendChild(_doc.CreateElement("Styles", XmlNamespace));
            foreach(var category in _categories)
            {
                var styleElement = _doc.CreateElement("Style", XmlNamespace);
                styleElement.Attributes.Append(AddXmlAtribute("TargetType", "Node"));
                styleElement.Attributes.Append(AddXmlAtribute("GroupLabel", EnumHelper.GetDescription<Category>(category.Key)));
                stylesElement.AppendChild(styleElement);

                var conditionElement = _doc.CreateElement("Condition", XmlNamespace);
                conditionElement.Attributes.Append(AddXmlAtribute("Expression", string.Format("HasCategory('{0}')", category.Key)));
                styleElement.AppendChild(conditionElement);

                var setterElement = _doc.CreateElement("Setter", XmlNamespace);
                setterElement.Attributes.Append(AddXmlAtribute("Property", "Background"));
                setterElement.Attributes.Append(AddXmlAtribute("Value", ColorTranslator.ToHtml(category.Value)));
                styleElement.AppendChild(setterElement);

            }

        }

        #endregion
    }
}