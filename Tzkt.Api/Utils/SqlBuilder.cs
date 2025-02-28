﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Dapper;
using Tzkt.Api.Utils;

namespace Tzkt.Api
{
    public class SqlBuilder
    {
        public DynamicParameters Params { get; private set; }
        public string Query => Builder.ToString();
        
        readonly StringBuilder Builder;
        bool Filters;
        int Counter;

        public SqlBuilder(string select)
        {
            Params = new DynamicParameters();
            Builder = new StringBuilder(select, select.Length + 80);
            Builder.AppendLine();
        }

        public SqlBuilder Filter(string expression)
        {
            AppendFilter(expression);
            return this;
        }

        public SqlBuilder Filter(AnyOfParameter anyof, Func<string, string> map)
        {
            if (anyof != null)
                AppendFilter($"({string.Join(" OR ", anyof.Fields.Select(x => $@"""{map(x)}"" = {anyof.Value}"))})");

            return this;
        }

        public SqlBuilder FilterA(AnyOfParameter anyof, Func<string, string> map)
        {
            if (anyof != null)
                AppendFilter($"({string.Join(" OR ", anyof.Fields.Select(x => $@"{map(x)} = {anyof.Value}"))})");

            return this;
        }

        public SqlBuilder Filter(string column, int value)
        {
            AppendFilter($@"""{column}"" = {value}");
            return this;
        }

        public SqlBuilder FilterA(string column, int value)
        {
            AppendFilter($"{column} = {value}");
            return this;
        }

        public SqlBuilder Filter(string column, bool? value)
        {
            if (value == null) return this;
            AppendFilter($@"""{column}"" = {value}");
            return this;
        }

        public SqlBuilder Filter(string column, AccountTypeParameter type)
        {
            if (type == null) return this;

            if (type.Eq != null)
                AppendFilter($@"""{column}"" = {type.Eq}");

            if (type.Ne != null)
                AppendFilter($@"""{column}"" != {type.Ne}");

            return this;
        }

        public SqlBuilder Filter(string column, BakingRightTypeParameter type)
        {
            if (type == null) return this;

            if (type.Eq != null)
                AppendFilter($@"""{column}"" = {type.Eq}");

            if (type.Ne != null)
                AppendFilter($@"""{column}"" != {type.Ne}");

            return this;
        }

        public SqlBuilder Filter(string column, BakingRightStatusParameter type)
        {
            if (type == null) return this;

            if (type.Eq != null)
                AppendFilter($@"""{column}"" = {type.Eq}");

            if (type.Ne != null)
                AppendFilter($@"""{column}"" != {type.Ne}");

            return this;
        }

        public SqlBuilder Filter(string column, ContractKindParameter kind)
        {
            if (kind == null) return this;

            if (kind.Eq != null)
                AppendFilter($@"""{column}"" = {kind.Eq}");

            if (kind.Ne != null)
                AppendFilter($@"""{column}"" != {kind.Ne}");

            if (kind.In != null)
                AppendFilter($@"""{column}"" = ANY ({Param(kind.In)})");

            if (kind.Ni != null && kind.Ni.Count > 0)
                AppendFilter($@"NOT (""{column}"" = ANY ({Param(kind.Ni)}))");

            return this;
        }

        public SqlBuilder Filter(string column, BigMapActionParameter action)
        {
            if (action == null) return this;

            if (action.Eq != null)
                AppendFilter($@"""{column}"" = {action.Eq}");

            if (action.Ne != null)
                AppendFilter($@"""{column}"" != {action.Ne}");

            if (action.In != null)
                AppendFilter($@"""{column}"" = ANY ({Param(action.In)})");

            if (action.Ni != null && action.Ni.Count > 0)
                AppendFilter($@"NOT (""{column}"" = ANY ({Param(action.Ni)}))");

            return this;
        }

        public SqlBuilder Filter(string column, BigMapTagsParameter tags)
        {
            if (tags == null) return this;

            if (tags.Eq != null)
                AppendFilter($@"""{column}"" = {tags.Eq}");

            if (tags.Any != null)
                AppendFilter($@"""{column}"" & {tags.Any} > 0");

            if (tags.All != null)
                AppendFilter($@"""{column}"" & {tags.All} = {tags.All}");

            return this;
        }

        public SqlBuilder Filter(string column, MigrationKindParameter kind)
        {
            if (kind == null) return this;

            if (kind.Eq != null)
                AppendFilter($@"""{column}"" = {kind.Eq}");

            if (kind.Ne != null)
                AppendFilter($@"""{column}"" != {kind.Ne}");

            if (kind.In != null)
                AppendFilter($@"""{column}"" = ANY ({Param(kind.In)})");

            if (kind.Ni != null && kind.Ni.Count > 0)
                AppendFilter($@"NOT (""{column}"" = ANY ({Param(kind.Ni)}))");

            return this;
        }

        public SqlBuilder Filter(string column, VoterStatusParameter status)
        {
            if (status == null) return this;

            if (status.Eq != null)
                AppendFilter($@"""{column}"" = {status.Eq}");

            if (status.Ne != null)
                AppendFilter($@"""{column}"" != {status.Ne}");

            if (status.In != null)
                AppendFilter($@"""{column}"" = ANY ({Param(status.In)})");

            if (status.Ni != null && status.Ni.Count > 0)
                AppendFilter($@"NOT (""{column}"" = ANY ({Param(status.Ni)}))");

            return this;
        }

        public SqlBuilder Filter(string column, OperationStatusParameter status)
        {
            if (status == null) return this;

            if (status.Eq != null)
                AppendFilter($@"""{column}"" = {status.Eq}");

            if (status.Ne != null)
                AppendFilter($@"""{column}"" != {status.Ne}");

            return this;
        }

        public SqlBuilder Filter(string column, ExpressionParameter expression)
        {
            if (expression == null) return this;

            if (expression.Eq != null)
                AppendFilter($@"""{column}"" = {Param(expression.Eq)}::character(54)");

            if (expression.Ne != null)
                AppendFilter($@"""{column}"" != {Param(expression.Ne)}::character(54)");

            if (expression.In != null)
                AppendFilter($@"""{column}"" = ANY ({Param(expression.In)})");

            if (expression.Ni != null && expression.Ni.Count > 0)
                AppendFilter($@"NOT (""{column}"" = ANY ({Param(expression.Ni)}))");

            return this;
        }

        public SqlBuilder Filter(string column, ProtocolParameter protocol)
        {
            if (protocol == null) return this;

            if (protocol.Eq != null)
                AppendFilter($@"""{column}"" = {Param(protocol.Eq)}::character(51)");

            if (protocol.Ne != null)
                AppendFilter($@"""{column}"" != {Param(protocol.Ne)}::character(51)");

            if (protocol.In != null)
                AppendFilter($@"""{column}"" = ANY ({Param(protocol.In)})");

            if (protocol.Ni != null && protocol.Ni.Count > 0)
                AppendFilter($@"NOT (""{column}"" = ANY ({Param(protocol.Ni)}))");

            return this;
        }

        public SqlBuilder FilterA(string column, ProtocolParameter protocol)
        {
            if (protocol == null) return this;

            if (protocol.Eq != null)
                AppendFilter($@"{column} = {Param(protocol.Eq)}::character(51)");

            if (protocol.Ne != null)
                AppendFilter($@"{column} != {Param(protocol.Ne)}::character(51)");

            if (protocol.In != null)
                AppendFilter($@"{column} = ANY ({Param(protocol.In)})");

            if (protocol.Ni != null && protocol.Ni.Count > 0)
                AppendFilter($@"NOT ({column} = ANY ({Param(protocol.Ni)}))");

            return this;
        }

        public SqlBuilder Filter(string column, AccountParameter account, Func<string, string> map = null)
        {
            if (account == null) return this;

            if (account.Eq != null)
                AppendFilter($@"""{column}"" = {account.Eq}");

            if (account.Ne != null && account.Ne != -1)
                AppendFilter($@"(""{column}"" IS NULL OR ""{column}"" != {account.Ne})");

            if (account.In != null)
                AppendFilter($@"""{column}"" = ANY ({Param(account.In)})");

            if (account.Ni != null && account.Ni.Count > 0)
                AppendFilter($@"(""{column}"" IS NULL OR NOT (""{column}"" = ANY ({Param(account.Ni)})))");

            if (account.Eqx != null && map != null)
                AppendFilter($@"""{column}"" = ""{map(account.Eqx)}""");

            if (account.Nex != null && map != null)
                AppendFilter($@"""{column}"" != ""{map(account.Nex)}""");

            if (account.Null != null)
            {
                AppendFilter(account.Null == true
                    ? $@"""{column}"" IS NULL"
                    : $@"""{column}"" IS NOT NULL");
            }

            return this;
        }

        public SqlBuilder FilterA(string column, AccountParameter account, Func<string, string> map = null)
        {
            if (account == null) return this;

            if (account.Eq != null)
                AppendFilter($"{column} = {account.Eq}");

            if (account.Ne != null && account.Ne != -1)
                AppendFilter($"({column} IS NULL OR {column} != {account.Ne})");

            if (account.In != null)
                AppendFilter($"{column} = ANY ({Param(account.In)})");

            if (account.Ni != null && account.Ni.Count > 0)
                AppendFilter($"({column} IS NULL OR NOT ({column} = ANY ({Param(account.Ni)})))");

            if (account.Eqx != null && map != null)
                AppendFilter($"{column} = {map(account.Eqx)}");

            if (account.Nex != null && map != null)
                AppendFilter($"{column} != {map(account.Nex)}");

            if (account.Null != null)
            {
                AppendFilter(account.Null == true
                    ? $"{column} IS NULL"
                    : $"{column} IS NOT NULL");
            }

            return this;
        }

        public SqlBuilder Filter(string column, StringParameter str, Func<string, string> map = null)
        {
            if (str == null) return this;

            if (str.Eq != null)
                AppendFilter($@"""{column}"" = {Param(str.Eq)}");

            if (str.Ne != null)
                AppendFilter($@"(""{column}"" IS NULL OR ""{column}"" != {Param(str.Ne)})");

            if (str.As != null)
                AppendFilter($@"""{column}"" LIKE {Param(str.As)}");

            if (str.Un != null)
                AppendFilter($@"NOT (""{column}"" LIKE ({Param(str.Un)}))");

            if (str.In != null)
                AppendFilter($@"""{column}"" = ANY ({Param(str.In)})");

            if (str.Ni != null)
                AppendFilter($@"(""{column}"" IS NULL OR NOT (""{column}"" = ANY ({Param(str.Ni)})))");

            if (str.Null != null)
            {
                AppendFilter(str.Null == true
                    ? $@"""{column}"" IS NULL"
                    : $@"""{column}"" IS NOT NULL");
            }

            return this;
        }

        public SqlBuilder Filter(string column, JsonParameter json, Func<string, string> map = null)
        {
            if (json == null) return this;
            
            if (json.Eq != null)
            {
                foreach (var (path, value) in json.Eq)
                {
                    AppendFilter($@"""{column}"" @> {Param(JsonPath.Merge(path, value))}::jsonb");
                    if (path.Any(x => x.Type == JsonPathType.Index))
                        AppendFilter($@"""{column}"" #> {Param(JsonPath.Select(path))} = {Param(value)}::jsonb");
                }
            }

            if (json.Ne != null)
            {
                foreach (var (path, value) in json.Ne)
                {
                    AppendFilter(path.Any(x => x.Type == JsonPathType.Any)
                        ? $@"NOT (""{column}"" @> {Param(JsonPath.Merge(path, value))}::jsonb)"
                        : $@"NOT (""{column}"" #> {Param(JsonPath.Select(path))} = {Param(value)}::jsonb)");
                }
            }

            if (json.Gt != null)
            {
                foreach (var (path, value) in json.Gt)
                {
                    var val = Param(value);
                    var fld = $@"""{column}"" #>> {Param(JsonPath.Select(path))}";
                    var len = $"greatest(length({fld}), length({val}))";
                    AppendFilter(Regex.IsMatch(value, @"^\d+$")
                        ? $@"lpad({fld}, {len}, '0') > lpad({val}, {len}, '0')"
                        : $@"{fld} > {val}");
                }
            }

            if (json.Ge != null)
            {
                foreach (var (path, value) in json.Ge)
                {
                    var val = Param(value);
                    var fld = $@"""{column}"" #>> {Param(JsonPath.Select(path))}";
                    var len = $"greatest(length({fld}), length({val}))";
                    AppendFilter(Regex.IsMatch(value, @"^\d+$")
                        ? $@"lpad({fld}, {len}, '0') >= lpad({val}, {len}, '0')"
                        : $@"{fld} >= {val}");
                }
            }

            if (json.Lt != null)
            {
                foreach (var (path, value) in json.Lt)
                {
                    var val = Param(value);
                    var fld = $@"""{column}"" #>> {Param(JsonPath.Select(path))}";
                    var len = $"greatest(length({fld}), length({val}))";
                    AppendFilter(Regex.IsMatch(value, @"^\d+$")
                        ? $@"lpad({fld}, {len}, '0') < lpad({val}, {len}, '0')"
                        : $@"{fld} < {val}");
                }
            }

            if (json.Le != null)
            {
                foreach (var (path, value) in json.Le)
                {
                    var val = Param(value);
                    var fld = $@"""{column}"" #>> {Param(JsonPath.Select(path))}";
                    var len = $"greatest(length({fld}), length({val}))";
                    AppendFilter(Regex.IsMatch(value, @"^\d+$")
                        ? $@"lpad({fld}, {len}, '0') <= lpad({val}, {len}, '0')"
                        : $@"{fld} <= {val}");
                }
            }

            if (json.As != null)
            {
                foreach (var (path, value) in json.As)
                {
                    AppendFilter($@"""{column}"" #>> {Param(JsonPath.Select(path))} LIKE {Param(value)}");
                }
            }

            if (json.Un != null)
            {
                foreach (var (path, value) in json.Un)
                {
                    AppendFilter($@"NOT (""{column}"" #>> {Param(JsonPath.Select(path))} LIKE {Param(value)})");
                }
            }

            if (json.In != null)
            {
                foreach (var (path, values) in json.In)
                {
                    var sqls = new List<string>(values.Length);
                    foreach (var value in values)
                    {
                        var sql = $@"""{column}"" @> {Param(JsonPath.Merge(path, value))}::jsonb";
                        if (path.Any(x => x.Type == JsonPathType.Index))
                            sql += $@" AND ""{column}"" #> {Param(JsonPath.Select(path))} = {Param(value)}::jsonb";
                        sqls.Add(sql);
                    }
                    AppendFilter($"({string.Join(" OR ", sqls)})");
                }
            }

            if (json.Ni != null)
            {
                foreach (var (path, values) in json.Ni)
                {
                    foreach (var value in values)
                    {
                        AppendFilter(path.Any(x => x.Type == JsonPathType.Any)
                            ? $@"NOT (""{column}"" @> {Param(JsonPath.Merge(path, value))}::jsonb)"
                            : $@"NOT (""{column}"" #> {Param(JsonPath.Select(path))} = {Param(value)}::jsonb)");
                    }
                }
            }

            if (json.Null != null)
            {
                foreach (var (path, value) in json.Null)
                {
                    if (path.Length == 0)
                    {
                        AppendFilter($@"""{column}"" IS {(value ? "" : "NOT ")}NULL");
                    }
                    else
                    {
                        if (value)
                            AppendFilter($@"""{column}"" IS NOT NULL");

                        AppendFilter($@"""{column}"" #>> {Param(JsonPath.Select(path))} IS {(value ? "" : "NOT ")}NULL");
                    }
                }
            }

            return this;
        }

        public SqlBuilder Filter(string column, Int32Parameter value, Func<string, string> map = null)
        {
            if (value == null) return this;

            if (value.Eq != null)
                AppendFilter($@"""{column}"" = {value.Eq}");

            if (value.Ne != null)
                AppendFilter($@"""{column}"" != {value.Ne}");

            if (value.Gt != null)
                AppendFilter($@"""{column}"" > {value.Gt}");

            if (value.Ge != null)
                AppendFilter($@"""{column}"" >= {value.Ge}");

            if (value.Lt != null)
                AppendFilter($@"""{column}"" < {value.Lt}");

            if (value.Le != null)
                AppendFilter($@"""{column}"" <= {value.Le}");

            if (value.In != null)
                AppendFilter($@"""{column}"" = ANY ({Param(value.In)})");

            if (value.Ni != null)
                AppendFilter($@"NOT (""{column}"" = ANY ({Param(value.Ni)}))");

            return this;
        }

        public SqlBuilder FilterA(string column, Int32Parameter value, Func<string, string> map = null)
        {
            if (value == null) return this;

            if (value.Eq != null)
                AppendFilter($@"{column} = {value.Eq}");

            if (value.Ne != null)
                AppendFilter($@"{column} != {value.Ne}");

            if (value.Gt != null)
                AppendFilter($@"{column} > {value.Gt}");

            if (value.Ge != null)
                AppendFilter($@"{column} >= {value.Ge}");

            if (value.Lt != null)
                AppendFilter($@"{column} < {value.Lt}");

            if (value.Le != null)
                AppendFilter($@"{column} <= {value.Le}");

            if (value.In != null)
                AppendFilter($@"{column} = ANY ({Param(value.In)})");

            if (value.Ni != null)
                AppendFilter($@"NOT ({column} = ANY ({Param(value.Ni)}))");

            return this;
        }

        public SqlBuilder Filter(string column, Int32NullParameter value, Func<string, string> map = null)
        {
            if (value == null) return this;

            if (value.Eq != null)
                AppendFilter($@"""{column}"" = {value.Eq}");

            if (value.Ne != null)
                AppendFilter($@"(""{column}"" IS NULL OR ""{column}"" != {value.Ne})");

            if (value.Gt != null)
                AppendFilter($@"""{column}"" > {value.Gt}");

            if (value.Ge != null)
                AppendFilter($@"""{column}"" >= {value.Ge}");

            if (value.Lt != null)
                AppendFilter($@"""{column}"" < {value.Lt}");

            if (value.Le != null)
                AppendFilter($@"""{column}"" <= {value.Le}");

            if (value.In != null)
                AppendFilter($@"""{column}"" = ANY ({Param(value.In)})");

            if (value.Ni != null)
                AppendFilter($@"(""{column}"" IS NULL OR NOT (""{column}"" = ANY ({Param(value.Ni)})))");

            if (value.Null != null)
            {
                AppendFilter(value.Null == true
                    ? $@"""{column}"" IS NULL"
                    : $@"""{column}"" IS NOT NULL");
            }

            return this;
        }

        public SqlBuilder FilterA(string column, Int32NullParameter value, Func<string, string> map = null)
        {
            if (value == null) return this;

            if (value.Eq != null)
                AppendFilter($@"{column} = {value.Eq}");

            if (value.Ne != null)
                AppendFilter($@"({column} IS NULL OR {column} != {value.Ne})");

            if (value.Gt != null)
                AppendFilter($@"{column} > {value.Gt}");

            if (value.Ge != null)
                AppendFilter($@"{column} >= {value.Ge}");

            if (value.Lt != null)
                AppendFilter($@"{column} < {value.Lt}");

            if (value.Le != null)
                AppendFilter($@"{column} <= {value.Le}");

            if (value.In != null)
                AppendFilter($@"{column} = ANY ({Param(value.In)})");

            if (value.Ni != null)
                AppendFilter($@"({column} IS NULL OR NOT ({column} = ANY ({Param(value.Ni)})))");

            if (value.Null != null)
            {
                AppendFilter(value.Null == true
                    ? $@"{column} IS NULL"
                    : $@"{column} IS NOT NULL");
            }

            return this;
        }

        public SqlBuilder Filter(string column, Int32ExParameter value, Func<string, string> map = null)
        {
            if (value == null) return this;

            if (value.Eq != null)
                AppendFilter($@"""{column}"" = {value.Eq}");

            if (value.Ne != null)
                AppendFilter($@"(""{column}"" IS NULL OR ""{column}"" != {value.Ne})");

            if (value.Gt != null)
                AppendFilter($@"""{column}"" > {value.Gt}");

            if (value.Ge != null)
                AppendFilter($@"""{column}"" >= {value.Ge}");

            if (value.Lt != null)
                AppendFilter($@"""{column}"" < {value.Lt}");

            if (value.Le != null)
                AppendFilter($@"""{column}"" <= {value.Le}");

            if (value.In != null)
                AppendFilter($@"""{column}"" = ANY ({Param(value.In)})");

            if (value.Ni != null)
                AppendFilter($@"(""{column}"" IS NULL OR NOT (""{column}"" = ANY ({Param(value.Ni)})))");

            if (value.Eqx != null && map != null)
                AppendFilter($@"""{column}"" = ""{map(value.Eqx)}""");

            if (value.Nex != null && map != null)
                AppendFilter($@"""{column}"" != ""{map(value.Nex)}""");

            if (value.Null != null)
            {
                AppendFilter(value.Null == true
                    ? $@"""{column}"" IS NULL"
                    : $@"""{column}"" IS NOT NULL");
            }

            return this;
        }

        public SqlBuilder FilterA(string column, Int32ExParameter value, Func<string, string> map = null)
        {
            if (value == null) return this;

            if (value.Eq != null)
                AppendFilter($@"{column} = {value.Eq}");

            if (value.Ne != null)
                AppendFilter($@"({column} IS NULL OR {column} != {value.Ne})");

            if (value.Gt != null)
                AppendFilter($@"{column} > {value.Gt}");

            if (value.Ge != null)
                AppendFilter($@"{column} >= {value.Ge}");

            if (value.Lt != null)
                AppendFilter($@"{column} < {value.Lt}");

            if (value.Le != null)
                AppendFilter($@"{column} <= {value.Le}");

            if (value.In != null)
                AppendFilter($@"{column} = ANY ({Param(value.In)})");

            if (value.Ni != null)
                AppendFilter($@"({column} IS NULL OR NOT ({column} = ANY ({Param(value.Ni)})))");

            if (value.Eqx != null && map != null)
                AppendFilter($@"{column} = {map(value.Eqx)}");

            if (value.Nex != null && map != null)
                AppendFilter($@"{column} != {map(value.Nex)}");

            if (value.Null != null)
            {
                AppendFilter(value.Null == true
                    ? $@"{column} IS NULL"
                    : $@"{column} IS NOT NULL");
            }

            return this;
        }

        public SqlBuilder Filter(string column, Int64Parameter value, Func<string, string> map = null)
        {
            if (value == null) return this;

            if (value.Eq != null)
                AppendFilter($@"""{column}"" = {value.Eq}");

            if (value.Ne != null)
                AppendFilter($@"""{column}"" != {value.Ne}");

            if (value.Gt != null)
                AppendFilter($@"""{column}"" > {value.Gt}");

            if (value.Ge != null)
                AppendFilter($@"""{column}"" >= {value.Ge}");

            if (value.Lt != null)
                AppendFilter($@"""{column}"" < {value.Lt}");

            if (value.Le != null)
                AppendFilter($@"""{column}"" <= {value.Le}");

            if (value.In != null)
                AppendFilter($@"""{column}"" = ANY ({Param(value.In)})");

            if (value.Ni != null)
                AppendFilter($@"NOT (""{column}"" = ANY ({Param(value.Ni)}))");

            return this;
        }

        public SqlBuilder Filter(string column, Int64ExParameter value, Func<string, string> map = null)
        {
            if (value == null) return this;

            if (value.Eq != null)
                AppendFilter($@"""{column}"" = {value.Eq}");

            if (value.Ne != null)
                AppendFilter($@"(""{column}"" IS NULL OR ""{column}"" != {value.Ne})");

            if (value.Gt != null)
                AppendFilter($@"""{column}"" > {value.Gt}");

            if (value.Ge != null)
                AppendFilter($@"""{column}"" >= {value.Ge}");

            if (value.Lt != null)
                AppendFilter($@"""{column}"" < {value.Lt}");

            if (value.Le != null)
                AppendFilter($@"""{column}"" <= {value.Le}");

            if (value.In != null)
                AppendFilter($@"""{column}"" = ANY ({Param(value.In)})");

            if (value.Ni != null)
                AppendFilter($@"(""{column}"" IS NULL OR NOT (""{column}"" = ANY ({Param(value.Ni)})))");

            if (value.Eqx != null && map != null)
                AppendFilter($@"""{column}"" = ""{map(value.Eqx)}""");

            if (value.Nex != null && map != null)
                AppendFilter($@"""{column}"" != ""{map(value.Nex)}""");

            if (value.Null != null)
            {
                AppendFilter(value.Null == true
                    ? $@"""{column}"" IS NULL"
                    : $@"""{column}"" IS NOT NULL");
            }

            return this;
        }

        public SqlBuilder Filter(string column, Int64NullParameter value, Func<string, string> map = null)
        {
            if (value == null) return this;

            if (value.Eq != null)
                AppendFilter($@"""{column}"" = {value.Eq}");

            if (value.Ne != null)
                AppendFilter($@"(""{column}"" IS NULL OR ""{column}"" != {value.Ne})");

            if (value.Gt != null)
                AppendFilter($@"""{column}"" > {value.Gt}");

            if (value.Ge != null)
                AppendFilter($@"""{column}"" >= {value.Ge}");

            if (value.Lt != null)
                AppendFilter($@"""{column}"" < {value.Lt}");

            if (value.Le != null)
                AppendFilter($@"""{column}"" <= {value.Le}");

            if (value.In != null)
                AppendFilter($@"""{column}"" = ANY ({Param(value.In)})");

            if (value.Ni != null)
                AppendFilter($@"(""{column}"" IS NULL OR NOT (""{column}"" = ANY ({Param(value.Ni)})))");

            if (value.Null != null)
            {
                AppendFilter(value.Null == true
                    ? $@"""{column}"" IS NULL"
                    : $@"""{column}"" IS NOT NULL");
            }

            return this;
        }

        public SqlBuilder Filter(string column, BoolParameter value, Func<string, string> map = null)
        {
            if (value == null) return this;

            if (value.Eq != null)
                AppendFilter($@"""{column}"" = {value.Eq}");

            if (value.Null != null)
            {
                AppendFilter(value.Null == true
                    ? $@"""{column}"" IS NULL"
                    : $@"""{column}"" IS NOT NULL");
            }

            return this;
        }

        public SqlBuilder FilterA(string column, BoolParameter value, Func<string, string> map = null)
        {
            if (value == null) return this;

            if (value.Eq != null)
                AppendFilter($@"{column} = {value.Eq}");

            if (value.Null != null)
            {
                AppendFilter(value.Null == true
                    ? $@"{column} IS NULL"
                    : $@"{column} IS NOT NULL");
            }

            return this;
        }

        public SqlBuilder Filter(string column, DateTimeParameter value, Func<string, string> map = null)
        {
            if (value == null) return this;

            if (value.Eq != null)
                AppendFilter($@"""{column}"" = {Param(value.Eq)}");

            if (value.Ne != null)
                AppendFilter($@"""{column}"" != {Param(value.Ne)}");

            if (value.Gt != null)
                AppendFilter($@"""{column}"" > {Param(value.Gt)}");

            if (value.Ge != null)
                AppendFilter($@"""{column}"" >= {Param(value.Ge)}");

            if (value.Lt != null)
                AppendFilter($@"""{column}"" < {Param(value.Lt)}");

            if (value.Le != null)
                AppendFilter($@"""{column}"" <= {Param(value.Le)}");

            if (value.In != null)
                AppendFilter($@"""{column}"" = ANY ({Param(value.In)})");

            if (value.Ni != null)
                AppendFilter($@"NOT (""{column}"" = ANY ({Param(value.Ni)}))");

            return this;
        }

        public SqlBuilder FilterA(string column, DateTimeParameter value, Func<string, string> map = null)
        {
            if (value == null) return this;
            if (value.Eq != null)
                AppendFilter($@"{column} = {Param(value.Eq)}");

            if (value.Ne != null)
                AppendFilter($@"{column} != {Param(value.Ne)}");

            if (value.Gt != null)
                AppendFilter($@"{column} > {Param(value.Gt)}");

            if (value.Ge != null)
                AppendFilter($@"{column} >= {Param(value.Ge)}");

            if (value.Lt != null)
                AppendFilter($@"{column} < {Param(value.Lt)}");

            if (value.Le != null)
                AppendFilter($@"{column} <= {Param(value.Le)}");

            if (value.In != null)
                AppendFilter($@"{column} = ANY ({Param(value.In)})");

            if (value.Ni != null)
                AppendFilter($@"NOT ({column} = ANY ({Param(value.Ni)}))");

            return this;
        }

        public SqlBuilder Filter(string column, TimestampParameter value, Func<string, string> map = null)
        {
            if (value == null) return this;

            if (value.Eq != null)
                AppendFilter($@"""{column}"" = {value.Eq}");

            if (value.Ne != null)
                AppendFilter($@"""{column}"" != {value.Ne}");

            if (value.Gt != null)
                AppendFilter($@"""{column}"" > {value.Gt}");

            if (value.Ge != null)
                AppendFilter($@"""{column}"" >= {value.Ge}");

            if (value.Lt != null)
                AppendFilter($@"""{column}"" < {value.Lt}");

            if (value.Le != null)
                AppendFilter($@"""{column}"" <= {value.Le}");

            if (value.In != null)
                AppendFilter($@"""{column}"" = ANY ({Param(value.In)})");

            if (value.Ni != null)
                AppendFilter($@"NOT (""{column}"" = ANY ({Param(value.Ni)}))");

            return this;
        }

        public SqlBuilder FilterA(string column, TimestampParameter value, Func<string, string> map = null)
        {
            if (value == null) return this;

            if (value.Eq != null)
                AppendFilter($@"{column} = {value.Eq}");

            if (value.Ne != null)
                AppendFilter($@"{column} != {value.Ne}");

            if (value.Gt != null)
                AppendFilter($@"{column} > {value.Gt}");

            if (value.Ge != null)
                AppendFilter($@"{column} >= {value.Ge}");

            if (value.Lt != null)
                AppendFilter($@"{column} < {value.Lt}");

            if (value.Le != null)
                AppendFilter($@"{column} <= {value.Le}");

            if (value.In != null)
                AppendFilter($@"{column} = ANY ({Param(value.In)})");

            if (value.Ni != null)
                AppendFilter($@"NOT ({column} = ANY ({Param(value.Ni)}))");

            return this;
        }

        public SqlBuilder Take(SortParameter sort, OffsetParameter offset, int limit, Func<string, (string, string)> map)
        {
            var sortAsc = true;
            var sortColumn = "Id";
            var cursorColumn = "Id";

            if (sort != null)
            {
                if (sort.Asc != null)
                {
                    (sortColumn, cursorColumn) = map(sort.Asc);
                }
                else if (sort.Desc != null)
                {
                    sortAsc = false;
                    (sortColumn, cursorColumn) = map(sort.Desc);
                }
            }

            if (offset?.Cr != null)
            {
                AppendFilter(sortAsc 
                    ? $@"""{cursorColumn}"" > {offset.Cr}"
                    : $@"""{cursorColumn}"" < {offset.Cr}");
            }

            if (sortColumn == "Id")
            {
                Builder.AppendLine(sortAsc
                    ? $@"ORDER BY ""Id"""
                    : $@"ORDER BY ""Id"" DESC");
            }
            else
            {
                Builder.AppendLine(sortAsc
                    ? $@"ORDER BY ""{sortColumn}"", ""Id"""
                    : $@"ORDER BY ""{sortColumn}"" DESC, ""Id"" DESC");
            }

            if (offset != null)
            {
                if (offset.El != null)
                    Builder.AppendLine($"OFFSET {offset.El}");
                else if (offset.Pg != null)
                    Builder.AppendLine($"OFFSET {offset.Pg * limit}");
            }

            Builder.AppendLine($"LIMIT {limit}");
            return this;
        }

        public SqlBuilder Take(SortParameter sort, OffsetParameter offset, int limit, Func<string, (string, string)> map, string prefix)
        {
            var sortAsc = true;
            var sortColumn = "Id";
            var cursorColumn = "Id";

            if (sort != null)
            {
                if (sort.Asc != null)
                {
                    (sortColumn, cursorColumn) = map(sort.Asc);
                }
                else if (sort.Desc != null)
                {
                    sortAsc = false;
                    (sortColumn, cursorColumn) = map(sort.Desc);
                }
            }

            if (offset?.Cr != null)
            {
                AppendFilter(sortAsc
                    ? $@"{prefix}.""{cursorColumn}"" > {offset.Cr}"
                    : $@"{prefix}.""{cursorColumn}"" < {offset.Cr}");
            }

            if (sortColumn == "Id")
            {
                Builder.AppendLine(sortAsc
                    ? $@"ORDER BY {prefix}.""Id"""
                    : $@"ORDER BY {prefix}.""Id"" DESC");
            }
            else
            {
                Builder.AppendLine(sortAsc
                    ? $@"ORDER BY {prefix}.""{sortColumn}"", {prefix}.""Id"""
                    : $@"ORDER BY {prefix}.""{sortColumn}"" DESC, {prefix}.""Id"" DESC");
            }

            if (offset != null)
            {
                if (offset.El != null)
                    Builder.AppendLine($"OFFSET {offset.El}");
                else if (offset.Pg != null)
                    Builder.AppendLine($"OFFSET {offset.Pg * limit}");
            }

            Builder.AppendLine($"LIMIT {limit}");
            return this;
        }

        public SqlBuilder Take(OffsetParameter offset, int limit)
        {
            if (offset?.Cr != null)
                AppendFilter($@"""Id"" > {offset.Cr} ");

            Builder.AppendLine($@"ORDER BY ""Id""");

            if (offset != null)
            {
                if (offset.El != null)
                    Builder.AppendLine($"OFFSET {offset.El}");
                else if (offset.Pg != null)
                    Builder.AppendLine($"OFFSET {offset.Pg * limit}");
            }

            Builder.AppendLine($"LIMIT {limit}");
            return this;
        }

        void AppendFilter(string filter)
        {
            if (!Filters)
            {
                Builder.Append("WHERE ");
                Filters = true;
            }
            else
            {
                Builder.Append("AND ");
            }

            Builder.AppendLine(filter);
        }

        string Param(object value)
        {
            var name = $"@p{Counter++}";
            Params.Add(name, value);
            return name;
        }
    }
}
