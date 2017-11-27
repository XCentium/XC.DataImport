using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace XC.Foundation.DataImport.Models.Tree
{
    // This is a model for angular-tree-component
    public class TreeNode
    {
        public string id { get; set; }
        public string name { get; set; }
        public List<TreeNode> children { get; set; }
        public bool hasChildren { get; set; }
        public bool isExpanded { get; set; }
        public string mediaUrl { get; set; }
        public bool showCheckbox { get; set; }
        public string longId { get; set; }
        public string path { get; set; }
    }
}