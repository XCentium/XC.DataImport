﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace XC.Foundation.DataImport.Models.DataSources
{
    public class TargetSitecoreFormsDataSourceModel : IDataSourceModel
    {
        public string DatabaseName { get; set; }
        public string ItemPath { get; set; }
        public string TemplateId { get; set; }
        public string FullPath { get; set; }
    }
}