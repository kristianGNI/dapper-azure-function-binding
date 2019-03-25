﻿using Microsoft.Azure.WebJobs.Description;
using System;
using System.ComponentModel.DataAnnotations;

namespace Dapper.Azure.WebJobs.Extensions.SqlServer
{
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
    [Binding]
    public sealed class DapperAttribute : Attribute
    {
        [Required(ErrorMessage = "Connection string is required, please add your connection in the application settings")]
        [ConnectionString]
        public string SqlConnection { get; set; }
        [Required(ErrorMessage = "Sql is required, please add it as a string or as reference to a .sql file")]
        [AutoResolve]
        public string Sql { get; set; }
        [AutoResolve]
        public string parameters { get; set; }
    }
}
