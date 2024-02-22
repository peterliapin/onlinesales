﻿// <copyright file="QueryProviderFactory.cs" company="WavePoint Co. Ltd.">
// Licensed under the MIT license. See LICENSE file in the samples root for full license information.
// </copyright>

using System.Web;
using Microsoft.Extensions.Options;
using Nest;
using OnlineSales.Configuration;
using OnlineSales.Data;
using OnlineSales.DataAnnotations;
using OnlineSales.Entities;
using OnlineSales.Interfaces;

namespace OnlineSales.Infrastructure
{
    public class QueryProviderFactory<T>
        where T : BaseEntityWithId, new()
    {
        protected readonly IOptions<ApiSettingsConfig> apiSettingsConfig;
        protected readonly IHttpContextHelper httpContextHelper;        
        protected readonly ElasticClient elasticClient;

        protected PgDbContext dbContext;

        public QueryProviderFactory(PgDbContext dbContext, EsDbContext esDbContext, IOptions<ApiSettingsConfig> apiSettingsConfig, IHttpContextHelper? httpContextHelper)
        {
            this.dbContext = dbContext;
            this.apiSettingsConfig = apiSettingsConfig;

            elasticClient = esDbContext.ElasticClient;

            ArgumentNullException.ThrowIfNull(httpContextHelper);
            this.httpContextHelper = httpContextHelper;
        }

        public virtual IQueryProvider<T> BuildQueryProvider(int limit = -1)
        {
            var queryCommands = QueryStringParser.Parse(httpContextHelper.Request.QueryString.HasValue ? HttpUtility.UrlDecode(httpContextHelper.Request.QueryString.ToString()) : string.Empty);

            var queryBuilder = new QueryModelBuilder<T>(queryCommands, limit == -1 ? apiSettingsConfig.Value.MaxListSize : limit, dbContext);

            var dbSet = dbContext.Set<T>();

            if (typeof(T).GetCustomAttributes(typeof(SupportsElasticAttribute), true).Any() && queryBuilder.SearchData.Count > 0)
            {
                var indexPrefix = dbContext.Configuration.GetSection("Elastic:IndexPrefix").Get<string>();
                return new MixedQueryProvider<T>(queryBuilder, dbSet!.AsQueryable<T>(), elasticClient, indexPrefix!);
            }
            else
            {
                return new DBQueryProvider<T>(dbSet!.AsQueryable<T>(), queryBuilder);
            }
        }

        public void SetDBContext(PgDbContext dbContext)
        {
            this.dbContext = dbContext;
        }
    }
}
