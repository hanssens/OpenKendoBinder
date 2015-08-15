using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic;
using OpenKendoBinder.Containers;
using OpenKendoBinder.Containers.Json;
using OpenKendoBinder.Extensions;

namespace OpenKendoBinder
{
    public class DataSourceResponse<TModel>
    {
        private const string _TEntity = "TEntity__";
        private readonly IQueryable<TModel> _query;

        public object Groups { get; set; }
        public IEnumerable<TModel> Data { get; set; }
        public object Aggregates { get; set; }
        public int Total { get; set; }

        public DataSourceResponse(BaseDataSourceRequest request, IEnumerable<TModel> list) : this(request, list.AsQueryable()) { }

        public DataSourceResponse(BaseDataSourceRequest request, IQueryable<TModel> query) : this(request, query, null) { }

        public DataSourceResponse(BaseDataSourceRequest request, IQueryable<TModel> query, IEnumerable<string> includes = null)
        {
            IList<string> includesAsList = null;
            if (includes != null)
            {
                includesAsList = includes.ToList();
            }

            var tempQuery = request.FilterObjectWrapper != null ? ApplyFiltering(query, request.FilterObjectWrapper) : query;
            Total = tempQuery.Count();

            if (request.AggregateObjects != null)
            {
                Aggregates = ApplyAggregates(tempQuery, includesAsList, request);
            }

            if (request.GroupObjects != null)
            {
                Groups = ApplyGroupingAndSorting(tempQuery, includesAsList, request);

                _query = null;
                Data = null;
            }
            else
            {
                tempQuery = ApplySorting(tempQuery, request.SortObjects);

                // Paging
                if (request.Skip.HasValue && request.Skip > 0)
                {
                    tempQuery = tempQuery.Skip(request.Skip.Value);
                }
                if (request.Take.HasValue && request.Take > 0)
                {
                    tempQuery = tempQuery.Take(request.Take.Value);
                }

                _query = tempQuery;

                Data = _query.ToList();
                Groups = null;
            }
        }

        protected DataSourceResponse(IEnumerable<TModel> list, int totalCount)
        {
            Data = list;
            Total = totalCount;
        }

        public IQueryable<TModel> AsQueryable()
        {
            return _query;
        }

        private IQueryable<TModel> ApplyFiltering(IQueryable<TModel> query, FilterObjectWrapper filter)
        {
            var filtering = GetFiltering(filter);
            return filtering != null ? query.Where(filtering) : query;
        }

        private IQueryable<TModel> ApplySorting(IQueryable<TModel> query, IEnumerable<SortObject> sortObjects)
        {
            var sorting = GetSorting(sortObjects) ?? query.ElementType.FirstSortableProperty();
            return query.OrderBy(sorting);
        }

        protected object ApplyAggregates(IQueryable<TModel> query, IList<string> includes, BaseDataSourceRequest request)
        {
            // In case of average, sum, min or max: convert it to sum(TEntity__.XXX) as sum_XXX
            // In case of count, convert it to count() as count_XXX
            var convertedAggregateObjects = request.AggregateObjects
                .Select(a => a.GetLinqAggregate())
                .Distinct()
                .ToList();

            // new (new (sum(TEntity__.EmployeeNumber) as sum__Number) as Aggregates)
            string aggregatesExpression = string.Format("new (new ({0}) as Aggregates)", string.Join(", ", convertedAggregateObjects));

            string includesX = string.Empty;

            if (includes != null && includes.Any())
            {
                includesX = ", " + string.Join(", ", includes.Select(i => "it." + i + " as " + _TEntity + i.Replace(".", "_")));
            }

            // Execute the Dynamic Linq "Select" to get TEntity__ (and includes if needed). Also add a fake __Key__ property to allow grouping
            // Example : new ("__Key__" as __Key__, it AS TModel__, it.Company as TEntity__Company, it.Company.MainCompany as TEntity__Company_MainCompany, it.Country as TEntity__Country)
            var selectTModelQuery = query.Select(string.Format("new (\"__Key__\" as __Key__, it AS " + _TEntity + "{0})", includesX));

            // Group by the __Key__ to allow aggregates to be calculated
            var groupByQuery = selectTModelQuery.GroupBy("__Key__");

            // Execute the Dynamic Linq "Select" to add the aggregates
            // Example : new (new (Sum(TEntity__.Id) as sum__Id, Min(TEntity__.Id) as min__Id, Max(TModel__.Id) as max__Id, Count() as count__Id, Average(TEntity__.Id) as average__Id) as Aggregates)
            var aggregatesQuery = groupByQuery.Select(aggregatesExpression);

            // Try to get first result, cast to DynamicClass as use helper method to convert this to correct response
            var aggregates = (aggregatesQuery.FirstOrDefault() as DynamicClass).GetAggregatesAsDictionary();

            return aggregates;
        }

        protected IEnumerable<DataSourceGroup> ApplyGroupingAndSorting(IQueryable<TModel> query, IList<string> includes, BaseDataSourceRequest request)
        {
            bool hasAggregates = request.GroupObjects.Any(g => g.AggregateObjects.Any());
            string aggregatesExpression = string.Empty;

            if (hasAggregates)
            {
                // In case of sum, min or max: convert it to sum(TModel__.XXX) as sum_XXX
                // In case of count, convert it to count() as count_XXX
                var convertedAggregateObjects = request.GroupObjects
                    .SelectMany(g => g.AggregateObjects)
                    .Select(a => a.GetLinqAggregate())
                    .Distinct()
                    .ToList();

                // , new (sum(TEntity__.EmployeeNumber) as sum__Number) as Aggregates
                aggregatesExpression = string.Format(", new ({0}) as Aggregates", string.Join(", ", convertedAggregateObjects));
            }

            var sort = request.SortObjects != null ? request.SortObjects.ToList() : new List<SortObject>();
            bool hasSortObjects = sort.Any();

            // List[0] = LastName as Last
            // @hanssens: in case of a navigation property, e.g. 'Company.Name', we should concat it appropriately
            var groupByFields = request.GroupObjects.Select(s => s.Field.Contains(".")
                ? s.Field + " as " + s.Field.Replace(".", string.Empty)
                : s.Field
            ).ToList();

            // new (new (LastName as Last) as GroupByFields)
            var groupByExpressionX = string.Format("new (new ({0}) as GroupByFields)", string.Join(",", groupByFields));

            // new (Key.GroupByFields, it as Grouping, new (sum(TEntity__.EmployeeNumber) as sum__TEntity___EmployeeNumber) as Aggregates)
            var selectExpressionBeforeOrderByX = string.Format("new (Key.GroupByFields, it as Grouping {0})", aggregatesExpression);
            var groupSort = string.Join(",", request.GroupObjects.ToList().Select(s => string.Format("{0} {1}", s.Field, s.Direction)));

            // Adam Downs moved sort to items vs group
            var orderByFieldsExpression = hasSortObjects ?
                string.Join(",", sort.Select(s => string.Format("{0} {1}", s.Field, s.Direction))) :
                request.GroupObjects.First().Field;

            // new (GroupByFields, Grouping, Aggregates)
            var selectExpressionAfterOrderByX = string.Format("new (GroupByFields, Grouping{0})", hasAggregates ? ", Aggregates" : string.Empty);

            // 
            string includesX = string.Empty;
            if (includes != null && includes.Any())
            {
                includesX = ", " + string.Join(", ", includes.Select(i => "it." + i + " as " + _TEntity + i.Replace(".", "_")));
            }

            var limitedQuery = query.OrderBy(string.Join(",", new[] { groupSort, orderByFieldsExpression }));

            // Execute the Dynamic Linq for Paging
            if (request.Skip.HasValue && request.Skip > 0)
            {
                limitedQuery = limitedQuery.Skip(request.Skip.Value);
            }
            if (request.Take.HasValue && request.Take > 0)
            {
                limitedQuery = limitedQuery.Take(request.Take.Value);
            }

            // Execute the Dynamic Linq "GroupBy"
            var groupByQuery = limitedQuery.GroupBy(groupByExpressionX, string.Format("new (it AS {1}{0})", includesX, _TEntity));

            // Execute the Dynamic Linq "Select"
            var selectQuery = groupByQuery.Select(selectExpressionBeforeOrderByX);

            // Execute the Dynamic Linq "OrderBy"
            var orderByQueryString = string.Join(",", request.GroupObjects
                .Select(s => string.Format("GroupByFields.{0} {1}",
                    // @hanssens: fix for navigation properties (e.g. 'Company.Name')
                    s.Field.Contains(".") ? s.Field.Replace(".", string.Empty) : s.Field,
                    s.Direction))
                    .ToList());

            var orderByQuery = selectQuery.OrderBy(orderByQueryString);

            // Execute the Dynamic Linq "Select" to get back the TModel objects
            var tempQuery = orderByQuery.Select(selectExpressionAfterOrderByX, typeof(TModel));

            // Create a valid List<DataSourceGroup> object
            var list = new List<DataSourceGroup>();
            foreach (DynamicClass item in tempQuery)
            {
                var grouping = item.GetPropertyValue<IGrouping<object, object>>("Grouping");
                var groupByDictionary = item.GetPropertyValue("GroupByFields").ToDictionary();
                var aggregates = item.GetAggregatesAsDictionary();

                Process(request.GroupObjects, groupByDictionary, grouping, aggregates, list);
            }

            return list;
        }

        private void Process(IEnumerable<GroupObject> groupByFields, IDictionary<string, object> values, IEnumerable<object> grouping, object aggregates, List<DataSourceGroup> kendoGroups)
        {
            var groupObjects = groupByFields as IList<GroupObject> ?? groupByFields.ToList();
            bool isLast = groupObjects.Count() == 1;

            var groupObject = groupObjects.First();

            var kendoGroup = new DataSourceGroup
            {
                // @hanssens: hotfix for navigation properties
                field = groupObject.Field.Replace(".", string.Empty),
                aggregates = aggregates,
                value = values[groupObject.Field.Replace(".", string.Empty)],
                hasSubgroups = !isLast,
            };

            if (isLast)
            {
                var entities = grouping.Select<TModel>(_TEntity).AsQueryable();
                kendoGroup.items = entities.ToList();
            }
            else
            {
                var newGroupByFields = new List<GroupObject>(groupObjects);
                newGroupByFields.Remove(groupObject);

                var newList = new List<DataSourceGroup>();
                Process(newGroupByFields.ToArray(), values, grouping, aggregates, newList);
                kendoGroup.items = newList;
            }

            kendoGroups.Add(kendoGroup);
        }

        protected string GetSorting(IEnumerable<SortObject> sortObjects)
        {
            if (sortObjects == null)
            {
                return null;
            }

            var expression = string.Join(",", sortObjects.Select(s => s.Field + " " + s.Direction));
            return expression.Length > 1 ? expression : null;
        }

        protected string GetFiltering(FilterObjectWrapper filter)
        {
            var finalExpression = string.Empty;

            foreach (var filterObject in filter.FilterObjects)
            {
                filterObject.Field1 = filterObject.Field1;
                filterObject.Field2 = filterObject.Field2;

                if (finalExpression.Length > 0)
                {
                    finalExpression += " " + filter.LogicToken + " ";
                }

                if (filterObject.IsConjugate)
                {
                    var expression1 = filterObject.GetExpression1<TModel>();
                    var expression2 = filterObject.GetExpression2<TModel>();
                    var combined = string.Format("({0} {1} {2})", expression1, filterObject.LogicToken, expression2);
                    finalExpression += combined;
                }
                else
                {
                    var expression = filterObject.GetExpression1<TModel>();
                    finalExpression += expression;
                }
            }

            return finalExpression.Length == 0 ? "true" : finalExpression;
        }

    }

}