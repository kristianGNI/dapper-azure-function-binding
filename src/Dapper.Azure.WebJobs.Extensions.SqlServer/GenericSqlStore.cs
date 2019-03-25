﻿using Dapper;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;

namespace Dapper.Azure.WebJobs.Extensions.SqlServer
{
    internal class GenericSqlStore
    {
        public static async Task Execute(SqlInput input, string connectionString, string sql)
        {
            if (string.IsNullOrEmpty(connectionString)) throw new System.ArgumentNullException(nameof(connectionString));
            if (string.IsNullOrEmpty(sql)) throw new System.ArgumentNullException(nameof(sql));
            var isParameterizeSql = IsParameterizeSql(sql);
            if (isParameterizeSql)
            {
                if (input == null || input.Parameters == null)
                    throw new System.ArgumentNullException(nameof(input), "The sql statement is parameterized therefore input can't be null or empty");
            }

            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);

                using (var transaction = connection.BeginTransaction())
                {
                    if (!isParameterizeSql)
                        await connection.ExecuteAsync(sql, transaction: transaction).ConfigureAwait(false);
                    else if (!IsMultiExecution(input.Parameters))
                        await connection.ExecuteAsync(sql, input.Parameters as object, transaction: transaction).ConfigureAwait(false);
                    else
                    {
                        var parameters = input.Parameters as IEnumerable;
                        foreach (var parameter in parameters)
                        {
                            await connection.ExecuteAsync(sql, parameter, transaction: transaction).ConfigureAwait(false);
                        }
                    }
                    transaction.Commit();
                }
            }
        }
        public static async Task<T> ExecuteQuery<T>(string connectionString, string sql, string parameters)
        {
            if (string.IsNullOrEmpty(connectionString)) throw new System.ArgumentNullException(nameof(connectionString));
            if (string.IsNullOrEmpty(sql)) throw new System.ArgumentNullException(nameof(sql));

            var isParameterizeSql = IsParameterizeSql(sql);
            if (isParameterizeSql)
            {
                if (string.IsNullOrEmpty(parameters))
                    throw new System.ArgumentNullException(nameof(parameters), "The sql statement is parameterized therefore parameters can't be null or empty");
            }

            using (var connection = new SqlConnection(connectionString))
            {
                IEnumerable<dynamic> result;
                await connection.OpenAsync().ConfigureAwait(false);                
                if (isParameterizeSql)
                    result = await connection.QueryAsync(sql, GetParameters(parameters, sql));
                else
                    result = await connection.QueryAsync(sql);
                return HandleQueryResult<T>(result);
            }
        }
        private static object GetParameters(string strParameters, string sql)
        {
            if (string.IsNullOrEmpty(strParameters)) throw new System.ArgumentNullException(nameof(strParameters));
            if (string.IsNullOrEmpty(sql)) throw new System.ArgumentNullException(nameof(sql));

            object parameters = null;            
            if (Utility.IsJson(strParameters))
            {
                parameters = JsonConvert.DeserializeObject<ExpandoObject>(strParameters, new ExpandoObjectConverter());
            }
            else
            {
                var sqlParameter = Utility.GetWords(sql);
                var values = Utility.StringToDict(strParameters);
                parameters = new DynamicParameters();
                for (int i = 0; i < sqlParameter.Count(); i++)
                {
                    ((DynamicParameters)parameters).Add(sqlParameter[i], values[sqlParameter[i].Remove(0, 1)], null, ParameterDirection.Input);
                }
            }            
            return parameters;
        }
        private static bool IsParameterizeSql(string sql)
        {
            if (string.IsNullOrEmpty(sql)) throw new System.ArgumentNullException(nameof(sql));
            var sqlParameters = Utility.GetWords(sql);
            return sqlParameters.Count() > 0;
        }
        private static bool IsMultiExecution(dynamic input)
        {
            return input is IEnumerable;
        }
        private static T HandleQueryResult<T>(IEnumerable<dynamic> result)
        {
            T resultValue = default(T);
            if (result != null && result.Count() > 0) {
                var resultType = typeof(T);
                string json;

                if (Utility.IsEnumerableType(resultType))
                    json = JsonConvert.SerializeObject(result);
                else
                    json = JsonConvert.SerializeObject(result.FirstOrDefault());
                resultValue = JsonConvert.DeserializeObject<T>(json);
            }            
            return resultValue;
        }
    }
}