﻿/*
 Copyright 2014-2017, Augurk
 
 Licensed under the Apache License, Version 2.0 (the "License");
 you may not use this file except in compliance with the License.
 You may obtain a copy of the License at
 
 http://www.apache.org/licenses/LICENSE-2.0
 
 Unless required by applicable law or agreed to in writing, software
 distributed under the License is distributed on an "AS IS" BASIS,
 WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 See the License for the specific language governing permissions and
 limitations under the License.
*/

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Raven.Client;
using Raven.Client.Linq;
using Augurk.Api.Indeces;
using System;
using Raven.Abstractions.Data;

namespace Augurk.Api.Managers
{
    /// <summary>
    /// Provides methods for retrieving products from storage.
    /// </summary>
    public class ProductManager
    {
        /// <summary>
        /// Gets all available products.
        /// </summary>
        /// <returns>Returns a range of productName names.</returns>
        public async Task<IEnumerable<string>> GetProductsAsync()
        {
            using (var session = Database.DocumentStore.OpenAsyncSession())
            {
                return await session.Query<DbFeature, Features_ByTitleProductAndGroup>()
                                    .OrderBy(feature => feature.Product)
                                    .Select(feature => feature.Product)
                                    .Distinct()
                                    .ToListAsync();
            }
        }

        /// <summary>
        /// Gets the description of the provided product.
        /// </summary>
        /// <param name="productName">The name of the product for which the description should be retrieved.</param>
        /// <returns>The description of the requested product; or, null.</returns>
        public async Task<string> GetProductDescriptionAsync(string productName)
        {
            using (var session = Database.DocumentStore.OpenAsyncSession())
            {
                return await session.Query<DbProduct, Products_ByName>()
                                    .Where(product => product.Name.Equals(productName, StringComparison.OrdinalIgnoreCase))
                                    .Select(product => product.DescriptionMarkdown)
                                    .SingleOrDefaultAsync();
            }
        }

        /// <summary>
        /// Inserts or updates the provided description for the product with the provided name.
        /// </summary>
        /// <param name="productName">The name of the product for which the description should be persisted.</param>
        /// <param name="descriptionMarkdown">The description that should be persisted.</param>
        public async Task InsertOrUpdateProductDescriptionAsync(string productName, string descriptionMarkdown)
        {
            var dbProduct = new DbProduct()
            {
                Name = productName,
                DescriptionMarkdown = descriptionMarkdown
            };

            using (var session = Database.DocumentStore.OpenAsyncSession())
            {
                // Using the store method when the product already exists in the database will override it completely, this is acceptable
                await session.StoreAsync(dbProduct, dbProduct.GetIdentifier());
                await session.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Deletes the specified productName.
        /// </summary>
        /// <param name="productName">The name of the product to delete.</param>
        public async Task DeleteProductAsync(string productName)
        {
            using (var session = Database.DocumentStore.OpenAsyncSession())
            {
                await session.Advanced.DocumentStore.AsyncDatabaseCommands.DeleteByIndexAsync(
                    nameof(Features_ByTitleProductAndGroup).Replace('_', '/'),
                    new IndexQuery() {Query = $"Product:\"{productName}\"" },
                    new BulkOperationOptions() {AllowStale = true});
            }
        }

        /// <summary>
        /// Deletes a specified version of the specified productName.
        /// </summary>
        /// <param name="productName">The productName to delete.</param>
        /// <param name="version">The version of the productName to delete.</param>
        public async Task DeleteProductAsync(string productName, string version)
        {
            using (var session = Database.DocumentStore.OpenAsyncSession())
            {
                await session.Advanced.DocumentStore.AsyncDatabaseCommands.DeleteByIndexAsync(
                    nameof(Features_ByTitleProductAndGroup).Replace('_', '/'),
                    new IndexQuery() { Query = $"Product:\"{productName}\"AND Version:\"{version}\"" },
                    new BulkOperationOptions() { AllowStale = true });
            }
        }

        /// <summary>
        /// Gets all available tags for the provided <paramref name="productName">productName</paramref>.
        /// </summary>
        /// <param name="productName">Name of the productName to get the available tags for.</param>
        /// <returns>Returns a range of tags for the provided <paramref name="productName">productName</paramref>.</returns>
        public async Task<IEnumerable<string>> GetTagsAsync(string productName)
        {
            using (var session = Database.DocumentStore.OpenAsyncSession())
            {
                return await session.Query<Features_ByProductAndBranch.TaggedFeature, Features_ByProductAndBranch>()
                                    .Where(feature => feature.Product.Equals(productName, StringComparison.CurrentCultureIgnoreCase))
                                    .OrderBy(feature => feature.Tag)
                                    .Select(feature => feature.Tag)
                                    .Distinct()
                                    .Take(512)
                                    .ToListAsync();
            }
        }
    }
}
