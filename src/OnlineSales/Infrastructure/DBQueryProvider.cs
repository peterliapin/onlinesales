﻿// <copyright file="DBQueryProvider.cs" company="WavePoint Co. Ltd.">
// Licensed under the MIT license. See LICENSE file in the samples root for full license information.
// </copyright>

using System.Data;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using OnlineSales.DataAnnotations;
using OnlineSales.Entities;
using OnlineSales.Helpers;
using OnlineSales.Interfaces;

namespace OnlineSales.Infrastructure
{
    public class DBQueryProvider<T> : IQueryProvider<T>
        where T : BaseEntityWithId
    {
        private readonly QueryModelBuilder<T> queryBuilder;
        private IQueryable<T> query;

        public DBQueryProvider(IQueryable<T> query, QueryModelBuilder<T> queryBuilder)
        {
            this.query = query;
            this.queryBuilder = queryBuilder;
        }

        public async Task<QueryResult<T>> GetResult()
        {
            AddWhereCommands();
            AddSearchCommands();

            var totalCount = query.Count();
            IList<T>? records;

            AddIncludeCommands();
            AddOrderCommands();
            AddSkipCommand();
            AddLimitCommand();
            if (queryBuilder.SelectData.IsSelect)
            {
                records = await GetSelectResult();
            }
            else
            {
                records = await query.ToListAsync();
            }

            return new QueryResult<T>(records, totalCount);
        }

        private void AddIncludeCommands()
        {
            foreach (var data in queryBuilder.IncludeData)
            {
                query = query.Include(data.Name);
            }
        }

        private void AddOrderCommands()
        {
            if (queryBuilder.OrderData.Count == 0)
            {
                query = query.OrderBy(t => t.Id);
            }
            else
            {
                var moreThanOne = false;
                foreach (var orderCmd in queryBuilder.OrderData)
                {
                    var expressionParameter = Expression.Parameter(typeof(T));
                    var orderPropertyType = orderCmd.Property.PropertyType;
                    var orderPropertyExpression = Expression.Property(expressionParameter, orderCmd.Property.Name);
                    var orderDelegateType = typeof(Func<,>).MakeGenericType(typeof(T), orderPropertyType);
                    var orderLambda = Expression.Lambda(orderDelegateType, orderPropertyExpression, expressionParameter);
                    var methodName = string.Empty;

                    if (orderCmd.Ascending)
                    {
                        methodName = moreThanOne ? "ThenBy" : "OrderBy";
                    }
                    else
                    {
                        methodName = moreThanOne ? "ThenByDescending" : "OrderByDescending";
                    }

                    moreThanOne = true;

                    var orderMethod = typeof(Queryable).GetMethods().First(
                                                                        m => m.Name == methodName &&
                                                                        m.GetGenericArguments().Length == 2 &&
                                                                        m.GetParameters().Length == 2).MakeGenericMethod(typeof(T), orderPropertyType);
                    query = (IOrderedQueryable<T>)orderMethod.Invoke(null, new object?[] { query, orderLambda })!;
                }
            }
        }

        private void AddSkipCommand()
        {
            if (queryBuilder.Skip > 0)
            {
                query = query.Skip(queryBuilder.Skip);
            }
        }

        private void AddLimitCommand()
        {
            if (queryBuilder.Limit > 0)
            {
                query = query.Take(queryBuilder.Limit);
            }
        }

        private void AddSearchCommands()
        {
            foreach (var cmdValue in queryBuilder.SearchData)
            {
                var props = typeof(T).GetProperties().Where(p => p.IsDefined(typeof(SearchableAttribute), false));

                Expression orExpression = Expression.Constant(false);
                var paramExpr = Expression.Parameter(typeof(T), "entity");
                var containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) });

                foreach (var prop in props)
                {
                    if (prop != null)
                    {
                        var n = prop.Name;
                        var me = Expression.Property(paramExpr, n);
                        Expression containsExpression;
                        if (prop.PropertyType == typeof(string))
                        {
                            containsExpression = Expression.Call(me, containsMethod!, Expression.Constant(cmdValue));
                        }
                        else
                        {
                            var pt = prop.PropertyType;
                            Console.WriteLine(pt);
                            var toStringMethod = prop.PropertyType.GetMethod("ToString", new Type[0]);
                            var ce = Expression.Call(me, toStringMethod!);
                            containsExpression = Expression.Call(ce, containsMethod!, Expression.Constant(cmdValue));
                        }

                        orExpression = Expression.Or(orExpression, containsExpression);
                    }
                }

                if (!ExpressionEqualityComparer.Instance.Equals(orExpression, Expression.Constant(false)))
                {
                    var predicate = Expression.Lambda<Func<T, bool>>(orExpression, paramExpr);
                    query = query.Where(predicate);
                }
            }
        }

        private void AddWhereCommands()
        {
            var commands = queryBuilder.WhereData;
            if (commands.Count > 0)
            {
                var expressionParameter = Expression.Parameter(typeof(T));
                Expression andExpression = Expression.Constant(true);
                var andExpressionExist = false;
                Expression orExpression = Expression.Constant(false);
                var errorList = new List<QueryException>();

                foreach (var cmds in commands)
                {
                    try
                    {
                        if (cmds.OrOperation)
                        {
                            foreach (var cmd in cmds.Data)
                            {
                                var expression = ParseWhereCommand(expressionParameter, cmd);
                                orExpression = Expression.Or(orExpression, expression);
                            }
                        }
                        else
                        {
                            foreach (var cmd in cmds.Data)
                            {
                                var expression = ParseWhereCommand(expressionParameter, cmd);
                                andExpression = Expression.And(andExpression, expression);
                                andExpressionExist = true;
                            }
                        }
                    }
                    catch (QueryException e)
                    {
                        errorList.Add(e);
                    }
                }

                if (errorList.Any())
                {
                    throw new QueryException(errorList);
                }

                if (!andExpressionExist)
                {
                    andExpression = Expression.Constant(false);
                }

                var resExpression = Expression.Or(andExpression, orExpression);
                query = query.Where(Expression.Lambda<Func<T, bool>>(resExpression, expressionParameter));
            }
        }

        private Expression ParseWhereCommand(ParameterExpression expressionParameter, QueryModelBuilder<T>.WhereUnitData cmd)
        {
            Expression outputExpression;
            var parameterPropertyExpression = Expression.Property(expressionParameter, cmd.Property.Name);

            Expression CreateEqualExpression(QueryModelBuilder<T>.WhereUnitData cmd, Expression parameter)
            {
                Expression orExpression = Expression.Constant(false);
                var stringValues = cmd.ParseStringValues();
                var parsedValues = cmd.ParseValues(stringValues);

                foreach (var value in parsedValues)
                {
                    if (value == null && !cmd.IsNullableProperty())
                    {
                        return Expression.Constant(false);
                    }
                    else
                    {
                        var valueParameterExpression = Expression.Constant(value, cmd.Property.PropertyType);
                        var eqExpression = Expression.Equal(parameter, valueParameterExpression);
                        orExpression = Expression.Or(orExpression, eqExpression);
                    }
                }

                return orExpression;
            }

            Expression CreateNEqualExpression(QueryModelBuilder<T>.WhereUnitData cmd, Expression parameter)
            {
                var expression = CreateEqualExpression(cmd, parameter);
                return Expression.Not(expression);
            }

            Expression? CreateCompareExpression(QueryModelBuilder<T>.WhereUnitData cmd, Expression parameter)
            {
                Expression? res = null;
                var parsedValue = cmd.ParseValues(new string[] { cmd.StringValue })[0];

                Expression value = Expression.Constant(parsedValue, cmd.Property.PropertyType);
                var pEx = parameter;
                var vEx = value;

                if (cmd.Property.PropertyType == typeof(string))
                {
                    var compareMethod = cmd.Property.PropertyType.GetMethod("CompareTo", new[] { typeof(string) });
                    pEx = Expression.Call(parameter, compareMethod!, value);
                    vEx = Expression.Constant(0);
                }

                if (cmd.Operation == WOperand.GreaterThan)
                {
                    res = Expression.GreaterThan(pEx, vEx);
                }
                else if (cmd.Operation == WOperand.GreaterThanOrEqualTo)
                {
                    res = Expression.GreaterThanOrEqual(pEx, vEx);
                }
                else if (cmd.Operation == WOperand.LessThan)
                {
                    res = Expression.LessThan(pEx, vEx);
                }
                else if (cmd.Operation == WOperand.LessThanOrEqualTo)
                {
                    res = Expression.LessThanOrEqual(pEx, vEx);
                }

                return res;
            }

            Expression? CreateLikeExpression(QueryModelBuilder<T>.WhereUnitData cmd, Expression parameter)
            {
                var parsedValue = cmd.ParseValues(new string[] { cmd.StringValue })[0];

                Expression value = Expression.Constant(parsedValue, cmd.Property.PropertyType);
                Expression? res = null;

                var matchOperation = typeof(Regex).GetMethod("IsMatch", BindingFlags.Static | BindingFlags.Public, new[] { typeof(string), typeof(string), typeof(RegexOptions) });
                var trueConstant = Expression.Constant(true);
                var falseConstant = Expression.Constant(false);
                var regexOptionExpression = Expression.Constant(RegexOptions.Compiled);

                if (cmd.Operation == WOperand.Like)
                {
                    res = Expression.Equal(Expression.Call(matchOperation!, parameter, value, regexOptionExpression), trueConstant);
                }
                else if (cmd.Operation == WOperand.NLike)
                {
                    res = Expression.Equal(Expression.Call(matchOperation!, parameter, value, regexOptionExpression), falseConstant);
                }

                return res;
            }

            Expression? CreateContainExpression(QueryModelBuilder<T>.WhereUnitData cmd, Expression parameter)
            {
                Expression? res = null;

                var matchOperation = typeof(Regex).GetMethod("IsMatch", BindingFlags.Static | BindingFlags.Public, new[] { typeof(string), typeof(string), typeof(RegexOptions) });
                var trueConstant = Expression.Constant(true);
                var falseConstant = Expression.Constant(false);
                var regexOptionExpression = Expression.Constant(RegexOptions.Compiled);

                var data = cmd.ParseContainValue(cmd.StringValue);
                var sb = new StringBuilder();

                sb.Append('^');
                foreach (var d in data)
                {
                    if (d.Item1 == QueryModelBuilder<T>.WhereUnitData.ContainsType.MatchAll)
                    {
                        sb.Append("(.*)");
                    }
                    else if (d.Item1 == QueryModelBuilder<T>.WhereUnitData.ContainsType.Substring)
                    {
                        sb.Append(Regex.Escape(d.Item2));
                    }
                }

                sb.Append('$');

                var valueParameterExpression = Expression.Constant(sb.ToString(), typeof(string));

                if (cmd.Operation == WOperand.Contains)
                {
                    res = Expression.Equal(Expression.Call(matchOperation!, parameter, valueParameterExpression, regexOptionExpression), trueConstant);
                }
                else if (cmd.Operation == WOperand.NContains)
                {
                    res = Expression.Equal(Expression.Call(matchOperation!, parameter, valueParameterExpression, regexOptionExpression), falseConstant);
                }

                return res;
            }

            try
            {
                switch (cmd.Operation)
                {
                    case WOperand.Equal:
                        outputExpression = CreateEqualExpression(cmd, parameterPropertyExpression);
                        break;
                    case WOperand.NotEqual:
                        outputExpression = CreateNEqualExpression(cmd, parameterPropertyExpression);
                        break;
                    case WOperand.GreaterThan:
                    case WOperand.GreaterThanOrEqualTo:
                    case WOperand.LessThan:
                    case WOperand.LessThanOrEqualTo:
                        outputExpression = CreateCompareExpression(cmd, parameterPropertyExpression)!;
                        break;
                    case WOperand.Like:
                    case WOperand.NLike:
                        outputExpression = CreateLikeExpression(cmd, parameterPropertyExpression)!;
                        break;
                    case WOperand.Contains:
                    case WOperand.NContains:
                        outputExpression = CreateContainExpression(cmd, parameterPropertyExpression)!;
                        break;
                    default:
                        throw new QueryException(cmd.Cmd.Source, $"No such operand '{cmd.Operation}'");
                }
            }
            catch (Exception ex)
            {
                throw new QueryException(cmd.Cmd.Source, ex.Message);
            }

            return outputExpression;
        }

        private async Task<IList<T>?> GetSelectResult()
        {
            if (queryBuilder.SelectData.SelectedProperties.Any())
            {
                var expressionParameter = Expression.Parameter(typeof(T));
                var outputType = TypeHelper.CompileTypeForSelectStatement(queryBuilder.SelectData.SelectedProperties.ToArray());
                var delegateType = typeof(Func<,>).MakeGenericType(typeof(T), outputType);
                var createOutputTypeExpression = Expression.New(outputType);

                var expressionSelectedProperties = queryBuilder.SelectData.SelectedProperties.Select(p =>
                {
                    var bindProp = outputType.GetProperty(p.Name);
                    var exprProp = Expression.Property(expressionParameter, p);
                    return Expression.Bind(bindProp!, exprProp);
                }).ToArray();
                var expressionCreateArray = Expression.MemberInit(createOutputTypeExpression, expressionSelectedProperties);
                dynamic lambda = Expression.Lambda(delegateType, expressionCreateArray, expressionParameter);

                var queryMethod = typeof(Queryable).GetMethods().FirstOrDefault(m => m.Name == "Select" && m.GetParameters()[1].ParameterType.GetGenericArguments()[0].GetGenericArguments().Length == 2)!.MakeGenericMethod(typeof(T), outputType);

                var toArrayAsyncMethod = typeof(EntityFrameworkQueryableExtensions).GetMethod("ToArrayAsync")!.MakeGenericMethod(outputType);

                var selectQueryable = queryMethod!.Invoke(query, new object[] { query, lambda });

                var outputTypeTaskResultProp = typeof(Task<>).MakeGenericType(outputType.MakeArrayType()).GetProperty("Result");

                var selectResult = (Task)toArrayAsyncMethod.Invoke(selectQueryable, new object?[] { selectQueryable!, null })!;
                await selectResult;
                var taskResult = outputTypeTaskResultProp!.GetValue(selectResult);
                return taskResult as IList<T>;
            }
            else
            {
                return null;
            }
        }
    }
}