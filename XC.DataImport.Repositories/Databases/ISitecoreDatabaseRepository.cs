﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sitecore.Data;
using Sitecore.Data.Items;

namespace XC.DataImport.Repositories.Databases
{

    public interface ISitecoreDatabaseRepository
    {
        /// <summary>
        /// Gets the items.
        /// </summary>
        /// <returns></returns>
        IEnumerable<Item> GetSourceItemsForImport();

        /// <summary>
        /// Gets the database.
        /// </summary>
        /// <value>
        /// The database.
        /// </value>
        Database Database { get; }

        /// <summary>
        /// Migrates the item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="parentItem">The parent item.</param>
        void MigrateItem(Item item, Item parentItem);
    }

}
