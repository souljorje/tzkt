﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Dapper;

using Tzkt.Api.Models;
using Tzkt.Api.Services.Cache;

namespace Tzkt.Api.Repositories
{
    public class ReportRepository : DbConnection
    {
        readonly AccountsCache Accounts;
        readonly QuotesCache Quotes;

        public ReportRepository(AccountsCache accounts, QuotesCache quotes, IConfiguration config) : base(config)
        {
            Accounts = accounts;
            Quotes = quotes;
        }

        public async Task Write(StreamWriter csv, string address, DateTime from, DateTime to, int limit, string delimiter, string separator)
        {
            var account = await Accounts.GetAsync(address);
            if (account == null) return;

            var sql = new StringBuilder();

            if (account.DelegationsCount > 0) UnionDelegations(sql);
            if (account.OriginationsCount > 0) UnionOriginations(sql);
            if (account.TransactionsCount > 0) UnionTransactions(sql);
            if (account.RevealsCount > 0) UnionReveals(sql);
            if (account.MigrationsCount > 0) UnionMigrations(sql);

            if (account is RawUser user)
            {
                if (user.Activated == true) UnionActivations(sql);
                if (user.RegisterConstantsCount > 0) UnionRegisterConstant(sql);
            }

            if (account is RawDelegate delegat)
            {
                if (delegat.BlocksCount > 0) UnionBaking(sql);
                if (delegat.EndorsementsCount > 0) UnionEndorsements(sql);
                if (delegat.DoubleBakingCount > 0) UnionDoubleBaking(sql);
                if (delegat.DoubleEndorsingCount > 0) UnionDoubleEndorsing(sql);
                if (delegat.NonceRevelationsCount > 0) UnionNonceRevelations(sql);
                if (delegat.RevelationPenaltiesCount > 0) UnionRevelationPenalties(sql);
            }

            if (sql.Length == 0) return;

            sql.AppendLine(@"ORDER BY ""Id""");
            sql.AppendLine(@"LIMIT @limit");

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.ToString(), new { account = account.Id, from, to, limit });

            #region write header
            csv.Write("Block level");
            csv.Write(delimiter);
            csv.Write("Datetime");
            csv.Write(delimiter);
            csv.Write("Operation");
            csv.Write(delimiter);
            if (account is RawDelegate)
            {
                csv.Write("Reward");
                csv.Write(delimiter);
                csv.Write("Loss");
                csv.Write(delimiter);
            }
            csv.Write("Received");
            csv.Write(delimiter);
            csv.Write("From address");
            csv.Write(delimiter);
            csv.Write("Sent");
            csv.Write(delimiter);
            csv.Write("Fee");
            csv.Write(delimiter);
            csv.Write("To address");
            csv.Write(delimiter);
            csv.Write("Explorer link");
            csv.Write("\n");
            #endregion

            #region write rows
            var format = new NumberFormatInfo { NumberDecimalSeparator = separator };

            foreach (var row in rows)
            {
                csv.Write(row.Level);
                csv.Write(delimiter);
                csv.Write(row.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"));
                csv.Write(delimiter);
                csv.Write(Operations[row.Type]);
                csv.Write(delimiter);
                if (account is RawDelegate)
                {
                    csv.Write(row.Reward == null ? "" : ((decimal)row.Reward / 1_000_000m).ToString(format));
                    csv.Write(delimiter);
                    csv.Write(row.Loss == null ? "" : ((decimal)-row.Loss / 1_000_000m).ToString(format));
                    csv.Write(delimiter);
                }
                csv.Write(row.Received == null ? "" : ((decimal)row.Received / 1_000_000m).ToString(format));
                csv.Write(delimiter);
                // WARN: possible NullReferenceException if chain reorgs during request execution (very unlikely)
                csv.Write(row.From == null ? "" : Accounts.Get(row.From).Address);
                csv.Write(delimiter);
                csv.Write(row.Sent == null ? "" : ((decimal)-row.Sent / 1_000_000m).ToString(format));
                csv.Write(delimiter);
                csv.Write(row.Fee == null ? "" : ((decimal)-row.Fee / 1_000_000m).ToString(format));
                csv.Write(delimiter);
                // WARN: possible NullReferenceException if chain reorgs during request execution (very unlikely)
                csv.Write(row.To == null ? "" : Accounts.Get(row.To).Address);
                csv.Write(delimiter);
                csv.Write(row.Nonce != null
                    ? $"https://tzkt.io/{row.OpHash}/{row.Counter}/{row.Nonce}"
                    : row.Counter != null
                        ? $"https://tzkt.io/{row.OpHash}/{row.Counter}"
                        : row.OpHash != null
                            ? $"https://tzkt.io/{row.OpHash}"
                            : "");

                csv.Write("\n");
            }
            #endregion

            csv.Flush();
        }

        public async Task Write(StreamWriter csv, string address, DateTime from, DateTime to, int limit, string delimiter, string separator, int symbol)
        {
            var account = await Accounts.GetAsync(address);
            if (account == null) return;

            var sql = new StringBuilder();

            if (account.DelegationsCount > 0) UnionDelegations(sql);
            if (account.OriginationsCount > 0) UnionOriginations(sql);
            if (account.TransactionsCount > 0) UnionTransactions(sql);
            if (account.RevealsCount > 0) UnionReveals(sql);
            if (account.MigrationsCount > 0) UnionMigrations(sql);

            if (account is RawUser user)
            {
                if (user.Activated == true) UnionActivations(sql);
                if (user.RegisterConstantsCount > 0) UnionRegisterConstant(sql);
            }

            if (account is RawDelegate delegat)
            {
                if (delegat.BlocksCount > 0) UnionBaking(sql);
                if (delegat.EndorsementsCount > 0) UnionEndorsements(sql);
                if (delegat.DoubleBakingCount > 0) UnionDoubleBaking(sql);
                if (delegat.DoubleEndorsingCount > 0) UnionDoubleEndorsing(sql);
                if (delegat.NonceRevelationsCount > 0) UnionNonceRevelations(sql);
                if (delegat.RevelationPenaltiesCount > 0) UnionRevelationPenalties(sql);
            }

            if (sql.Length == 0) return;

            sql.AppendLine(@"ORDER BY ""Id""");
            sql.AppendLine(@"LIMIT @limit");

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.ToString(), new { account = account.Id, from, to, limit });

            #region write header
            var symbolName = symbol switch
            {
                0 => "BTC",
                1 => "EUR",
                2 => "USD",
                3 => "CNY",
                4 => "JPY",
                5 => "KRW",
                6 => "ETH",
                7 => "GBP",
                _ => ""
            };

            csv.Write("Block level");
            csv.Write(delimiter);
            csv.Write("Datetime");
            csv.Write(delimiter);
            csv.Write("Operation");
            csv.Write(delimiter);
            if (account is RawDelegate)
            {
                csv.Write("Reward XTZ");
                csv.Write(delimiter);
                csv.Write($"Reward {symbolName}");
                csv.Write(delimiter);
                csv.Write("Loss XTZ");
                csv.Write(delimiter);
                csv.Write($"Loss {symbolName}");
                csv.Write(delimiter);
            }
            csv.Write("Received XTZ");
            csv.Write(delimiter);
            csv.Write($"Received {symbolName}");
            csv.Write(delimiter);
            csv.Write("From address");
            csv.Write(delimiter);
            csv.Write("Sent XTZ");
            csv.Write(delimiter);
            csv.Write($"Sent {symbolName}");
            csv.Write(delimiter);
            csv.Write("Fee XTZ");
            csv.Write(delimiter);
            csv.Write($"Fee {symbolName}");
            csv.Write(delimiter);
            csv.Write("To address");
            csv.Write(delimiter);
            csv.Write("Explorer link");
            csv.Write("\n");
            #endregion

            #region write rows
            var format = new NumberFormatInfo { NumberDecimalSeparator = separator };
            var price = Quotes.Get(symbol);
            
            foreach (var row in rows)
            {
                csv.Write(row.Level);
                csv.Write(delimiter);
                csv.Write(row.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"));
                csv.Write(delimiter);
                csv.Write(Operations[row.Type]);
                csv.Write(delimiter);
                if (account is RawDelegate)
                {
                    csv.Write(row.Reward == null ? "" : ((decimal)row.Reward / 1_000_000m).ToString(format));
                    csv.Write(delimiter);
                    csv.Write(row.Reward == null ? "" : ((double)row.Reward / 1_000_000d * price).ToString(format));
                    csv.Write(delimiter);
                    csv.Write(row.Loss == null ? "" : ((decimal)-row.Loss / 1_000_000m).ToString(format));
                    csv.Write(delimiter);
                    csv.Write(row.Loss == null ? "" : ((double)-row.Loss / 1_000_000d * price).ToString(format));
                    csv.Write(delimiter);
                }
                csv.Write(row.Received == null ? "" : ((decimal)row.Received / 1_000_000m).ToString(format));
                csv.Write(delimiter);
                csv.Write(row.Received == null ? "" : ((double)row.Received / 1_000_000d * price).ToString(format));
                csv.Write(delimiter);
                // WARN: possible NullReferenceException if chain reorgs during request execution (very unlikely)
                csv.Write(row.From == null ? "" : Accounts.Get(row.From).Address);
                csv.Write(delimiter);
                csv.Write(row.Sent == null ? "" : ((decimal)-row.Sent / 1_000_000m).ToString(format));
                csv.Write(delimiter);
                csv.Write(row.Sent == null ? "" : ((double)-row.Sent / 1_000_000d * price).ToString(format));
                csv.Write(delimiter);
                csv.Write(row.Fee == null ? "" : ((decimal)-row.Fee / 1_000_000m).ToString(format));
                csv.Write(delimiter);
                csv.Write(row.Fee == null ? "" : ((double)-row.Fee / 1_000_000d * price).ToString(format));
                csv.Write(delimiter);
                // WARN: possible NullReferenceException if chain reorgs during request execution (very unlikely)
                csv.Write(row.To == null ? "" : Accounts.Get(row.To).Address);
                csv.Write(delimiter);
                csv.Write(row.Nonce != null
                    ? $"https://tzkt.io/{row.OpHash}/{row.Counter}/{row.Nonce}"
                    : row.Counter != null
                        ? $"https://tzkt.io/{row.OpHash}/{row.Counter}"
                        : row.OpHash != null
                            ? $"https://tzkt.io/{row.OpHash}"
                            : "");

                csv.Write("\n");
            }
            #endregion

            csv.Flush();
        }

        public async Task WriteHistorical(StreamWriter csv, string address, DateTime from, DateTime to, int limit, string delimiter, string separator, int symbol)
        {
            var account = await Accounts.GetAsync(address);
            if (account == null) return;

            var sql = new StringBuilder();

            if (account.DelegationsCount > 0) UnionDelegations(sql);
            if (account.OriginationsCount > 0) UnionOriginations(sql);
            if (account.TransactionsCount > 0) UnionTransactions(sql);
            if (account.RevealsCount > 0) UnionReveals(sql);
            if (account.MigrationsCount > 0) UnionMigrations(sql);

            if (account is RawUser user)
            {
                if (user.Activated == true) UnionActivations(sql);
                if (user.RegisterConstantsCount > 0) UnionRegisterConstant(sql);
            }

            if (account is RawDelegate delegat)
            {
                if (delegat.BlocksCount > 0) UnionBaking(sql);
                if (delegat.EndorsementsCount > 0) UnionEndorsements(sql);
                if (delegat.DoubleBakingCount > 0) UnionDoubleBaking(sql);
                if (delegat.DoubleEndorsingCount > 0) UnionDoubleEndorsing(sql);
                if (delegat.NonceRevelationsCount > 0) UnionNonceRevelations(sql);
                if (delegat.RevelationPenaltiesCount > 0) UnionRevelationPenalties(sql);
            }

            if (sql.Length == 0) return;

            sql.AppendLine(@"ORDER BY ""Id""");
            sql.AppendLine(@"LIMIT @limit");

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.ToString(), new { account = account.Id, from, to, limit });

            #region write header
            var symbolName = symbol switch
            {
                0 => "BTC",
                1 => "EUR",
                2 => "USD",
                3 => "CNY",
                4 => "JPY",
                5 => "KRW",
                6 => "ETH",
                7 => "GBP",
                _ => ""
            };

            csv.Write("Block level");
            csv.Write(delimiter);
            csv.Write("Datetime");
            csv.Write(delimiter);
            csv.Write("Operation");
            csv.Write(delimiter);
            if (account is RawDelegate)
            {
                csv.Write("Reward XTZ");
                csv.Write(delimiter);
                csv.Write($"Reward {symbolName}");
                csv.Write(delimiter);
                csv.Write("Loss XTZ");
                csv.Write(delimiter);
                csv.Write($"Loss {symbolName}");
                csv.Write(delimiter);
            }
            csv.Write("Received XTZ");
            csv.Write(delimiter);
            csv.Write($"Received {symbolName}");
            csv.Write(delimiter);
            csv.Write("From address");
            csv.Write(delimiter);
            csv.Write("Sent XTZ");
            csv.Write(delimiter);
            csv.Write($"Sent {symbolName}");
            csv.Write(delimiter);
            csv.Write("Fee XTZ");
            csv.Write(delimiter);
            csv.Write($"Fee {symbolName}");
            csv.Write(delimiter);
            csv.Write("To address");
            csv.Write(delimiter);
            csv.Write("Explorer link");
            csv.Write("\n");
            #endregion

            #region write rows
            var format = new NumberFormatInfo { NumberDecimalSeparator = separator };

            foreach (var row in rows)
            {
                var price = Quotes.Get(symbol, (int)row.Level);

                csv.Write(row.Level);
                csv.Write(delimiter);
                csv.Write(row.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"));
                csv.Write(delimiter);
                csv.Write(Operations[row.Type]);
                csv.Write(delimiter);
                if (account is RawDelegate)
                {
                    csv.Write(row.Reward == null ? "" : ((decimal)row.Reward / 1_000_000m).ToString(format));
                    csv.Write(delimiter);
                    csv.Write(row.Reward == null ? "" : ((double)row.Reward / 1_000_000d * price).ToString(format));
                    csv.Write(delimiter);
                    csv.Write(row.Loss == null ? "" : ((decimal)-row.Loss / 1_000_000m).ToString(format));
                    csv.Write(delimiter);
                    csv.Write(row.Loss == null ? "" : ((double)-row.Loss / 1_000_000d * price).ToString(format));
                    csv.Write(delimiter);
                }
                csv.Write(row.Received == null ? "" : ((decimal)row.Received / 1_000_000m).ToString(format));
                csv.Write(delimiter);
                csv.Write(row.Received == null ? "" : ((double)row.Received / 1_000_000d * price).ToString(format));
                csv.Write(delimiter);
                // WARN: possible NullReferenceException if chain reorgs during request execution (very unlikely)
                csv.Write(row.From == null ? "" : Accounts.Get(row.From).Address);
                csv.Write(delimiter);
                csv.Write(row.Sent == null ? "" : ((decimal)-row.Sent / 1_000_000m).ToString(format));
                csv.Write(delimiter);
                csv.Write(row.Sent == null ? "" : ((double)-row.Sent / 1_000_000d * price).ToString(format));
                csv.Write(delimiter);
                csv.Write(row.Fee == null ? "" : ((decimal)-row.Fee / 1_000_000m).ToString(format));
                csv.Write(delimiter);
                csv.Write(row.Fee == null ? "" : ((double)-row.Fee / 1_000_000d * price).ToString(format));
                csv.Write(delimiter);
                // WARN: possible NullReferenceException if chain reorgs during request execution (very unlikely)
                csv.Write(row.To == null ? "" : Accounts.Get(row.To).Address);
                csv.Write(delimiter);
                csv.Write(row.Nonce != null
                    ? $"https://tzkt.io/{row.OpHash}/{row.Counter}/{row.Nonce}"
                    : row.Counter != null
                        ? $"https://tzkt.io/{row.OpHash}/{row.Counter}"
                        : row.OpHash != null
                            ? $"https://tzkt.io/{row.OpHash}"
                            : "");

                csv.Write("\n");
            }
            #endregion

            csv.Flush();
        }

        void UnionBaking(StringBuilder sql)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            sql.Append(@"0 as ""Type"", ");
            sql.Append(@"""Id"" as ""Id"", ");
            sql.Append(@"""Level"" as ""Level"", ");
            sql.Append(@"""Hash"" as ""OpHash"", ");
            sql.Append(@"null::integer as ""Counter"", ");
            sql.Append(@"null::integer as ""Nonce"", ");
            sql.Append(@"""Timestamp"" as ""Timestamp"", ");
            sql.Append(@"(""Reward"" + ""Fees"") as ""Reward"", ");
            sql.Append(@"null::integer as ""Loss"", ");
            sql.Append(@"null::integer as ""Received"", ");
            sql.Append(@"null::integer as ""From"", ");
            sql.Append(@"null::integer as ""Sent"", ");
            sql.Append(@"null::integer as ""Fee"", ");
            sql.Append(@"null::integer as ""To"" ");

            sql.Append(@"FROM ""Blocks"" ");
            sql.Append(@"WHERE ""BakerId"" = @account ");
            sql.Append(@"AND ""Timestamp"" >= @from AND ""Timestamp"" < @to ");
            sql.Append(@"AND (""Reward"" > 0 OR ""Fees"" > 0) ");

            sql.AppendLine();
        }

        void UnionEndorsements(StringBuilder sql)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            sql.Append(@"1 as ""Type"", ");
            sql.Append(@"""Id"" as ""Id"", ");
            sql.Append(@"""Level"" as ""Level"", ");
            sql.Append(@"""OpHash"" as ""OpHash"", ");
            sql.Append(@"null::integer as ""Counter"", ");
            sql.Append(@"null::integer as ""Nonce"", ");
            sql.Append(@"""Timestamp"" as ""Timestamp"", ");
            sql.Append(@"""Reward"" as ""Reward"", ");
            sql.Append(@"null::integer as ""Loss"", ");
            sql.Append(@"null::integer as ""Received"", ");
            sql.Append(@"null::integer as ""From"", ");
            sql.Append(@"null::integer as ""Sent"", ");
            sql.Append(@"null::integer as ""Fee"", ");
            sql.Append(@"null::integer as ""To"" ");

            sql.Append(@"FROM ""EndorsementOps"" ");
            sql.Append(@"WHERE ""DelegateId"" = @account ");
            sql.Append(@"AND ""Timestamp"" >= @from AND ""Timestamp"" < @to ");
            sql.Append(@"AND ""Reward"" > 0 ");

            sql.AppendLine();
        }

        void UnionActivations(StringBuilder sql)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            sql.Append(@"2 as ""Type"", ");
            sql.Append(@"""Id"" as ""Id"", ");
            sql.Append(@"""Level"" as ""Level"", ");
            sql.Append(@"""OpHash"" as ""OpHash"", ");
            sql.Append(@"null::integer as ""Counter"", ");
            sql.Append(@"null::integer as ""Nonce"", ");
            sql.Append(@"""Timestamp"" as ""Timestamp"", ");
            sql.Append(@"null::integer as ""Reward"", ");
            sql.Append(@"null::integer as ""Loss"", ");
            sql.Append(@"""Balance"" as ""Received"", ");
            sql.Append(@"null::integer as ""From"", ");
            sql.Append(@"null::integer as ""Sent"", ");
            sql.Append(@"null::integer as ""Fee"", ");
            sql.Append(@"null::integer as ""To"" ");

            sql.Append(@"FROM ""ActivationOps"" ");
            sql.Append(@"WHERE ""AccountId"" = @account ");
            sql.Append(@"AND ""Timestamp"" >= @from AND ""Timestamp"" < @to ");
            sql.Append(@"AND ""Balance"" > 0 ");

            sql.AppendLine();
        }

        void UnionDoubleBaking(StringBuilder sql)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            #region accuser
            sql.Append(@"3 as ""Type"", ");
            sql.Append(@"""Id"" as ""Id"", ");
            sql.Append(@"""Level"" as ""Level"", ");
            sql.Append(@"""OpHash"" as ""OpHash"", ");
            sql.Append(@"null::integer as ""Counter"", ");
            sql.Append(@"null::integer as ""Nonce"", ");
            sql.Append(@"""Timestamp"" as ""Timestamp"", ");
            sql.Append(@"""AccuserReward"" as ""Reward"", ");
            sql.Append(@"null::integer as ""Loss"", ");
            sql.Append(@"null::integer as ""Received"", ");
            sql.Append(@"null::integer as ""From"", ");
            sql.Append(@"null::integer as ""Sent"", ");
            sql.Append(@"null::integer as ""Fee"", ");
            sql.Append(@"null::integer as ""To"" ");

            sql.Append(@"FROM ""DoubleBakingOps"" ");
            sql.Append(@"WHERE ""AccuserId"" = @account ");
            sql.Append(@"AND ""Timestamp"" >= @from AND ""Timestamp"" < @to ");

            sql.AppendLine();
            #endregion

            sql.Append("UNION ALL SELECT ");

            #region offender
            sql.Append(@"3 as ""Type"", ");
            sql.Append(@"""Id"" as ""Id"", ");
            sql.Append(@"""Level"" as ""Level"", ");
            sql.Append(@"""OpHash"" as ""OpHash"", ");
            sql.Append(@"null::integer as ""Counter"", ");
            sql.Append(@"null::integer as ""Nonce"", ");
            sql.Append(@"""Timestamp"" as ""Timestamp"", ");
            sql.Append(@"null::integer as ""Reward"", ");
            sql.Append(@"(""OffenderLostDeposit"" + ""OffenderLostReward"" + ""OffenderLostFee"") as ""Loss"", ");
            sql.Append(@"null::integer as ""Received"", ");
            sql.Append(@"null::integer as ""From"", ");
            sql.Append(@"null::integer as ""Sent"", ");
            sql.Append(@"null::integer as ""Fee"", ");
            sql.Append(@"null::integer as ""To"" ");

            sql.Append(@"FROM ""DoubleBakingOps"" ");
            sql.Append(@"WHERE ""OffenderId"" = @account ");
            sql.Append(@"AND ""Timestamp"" >= @from AND ""Timestamp"" < @to ");

            sql.AppendLine();
            #endregion
        }

        void UnionDoubleEndorsing(StringBuilder sql)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            #region accuser
            sql.Append(@"4 as ""Type"", ");
            sql.Append(@"""Id"" as ""Id"", ");
            sql.Append(@"""Level"" as ""Level"", ");
            sql.Append(@"""OpHash"" as ""OpHash"", ");
            sql.Append(@"null::integer as ""Counter"", ");
            sql.Append(@"null::integer as ""Nonce"", ");
            sql.Append(@"""Timestamp"" as ""Timestamp"", ");
            sql.Append(@"""AccuserReward"" as ""Reward"", ");
            sql.Append(@"null::integer as ""Loss"", ");
            sql.Append(@"null::integer as ""Received"", ");
            sql.Append(@"null::integer as ""From"", ");
            sql.Append(@"null::integer as ""Sent"", ");
            sql.Append(@"null::integer as ""Fee"", ");
            sql.Append(@"null::integer as ""To"" ");

            sql.Append(@"FROM ""DoubleEndorsingOps"" ");
            sql.Append(@"WHERE ""AccuserId"" = @account ");
            sql.Append(@"AND ""Timestamp"" >= @from AND ""Timestamp"" < @to ");

            sql.AppendLine();
            #endregion

            sql.Append("UNION ALL SELECT ");

            #region offender
            sql.Append(@"4 as ""Type"", ");
            sql.Append(@"""Id"" as ""Id"", ");
            sql.Append(@"""Level"" as ""Level"", ");
            sql.Append(@"""OpHash"" as ""OpHash"", ");
            sql.Append(@"null::integer as ""Counter"", ");
            sql.Append(@"null::integer as ""Nonce"", ");
            sql.Append(@"""Timestamp"" as ""Timestamp"", ");
            sql.Append(@"null::integer as ""Reward"", ");
            sql.Append(@"(""OffenderLostDeposit"" + ""OffenderLostReward"" + ""OffenderLostFee"") as ""Loss"", ");
            sql.Append(@"null::integer as ""Received"", ");
            sql.Append(@"null::integer as ""From"", ");
            sql.Append(@"null::integer as ""Sent"", ");
            sql.Append(@"null::integer as ""Fee"", ");
            sql.Append(@"null::integer as ""To"" ");

            sql.Append(@"FROM ""DoubleEndorsingOps"" ");
            sql.Append(@"WHERE ""OffenderId"" = @account ");
            sql.Append(@"AND ""Timestamp"" >= @from AND ""Timestamp"" < @to ");

            sql.AppendLine();
            #endregion
        }

        void UnionNonceRevelations(StringBuilder sql)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            sql.Append(@"5 as ""Type"", ");
            sql.Append(@"""Id"" as ""Id"", ");
            sql.Append(@"""Level"" as ""Level"", ");
            sql.Append(@"""OpHash"" as ""OpHash"", ");
            sql.Append(@"null::integer as ""Counter"", ");
            sql.Append(@"null::integer as ""Nonce"", ");
            sql.Append(@"""Timestamp"" as ""Timestamp"", ");
            sql.Append(@"125000 as ""Reward"", "); // TODO: get reward amount from protocol constants
            sql.Append(@"null::integer as ""Loss"", ");
            sql.Append(@"null::integer as ""Received"", ");
            sql.Append(@"null::integer as ""From"", ");
            sql.Append(@"null::integer as ""Sent"", ");
            sql.Append(@"null::integer as ""Fee"", ");
            sql.Append(@"null::integer as ""To"" ");

            sql.Append(@"FROM ""NonceRevelationOps"" ");
            sql.Append(@"WHERE ""BakerId"" = @account ");
            sql.Append(@"AND ""Timestamp"" >= @from AND ""Timestamp"" < @to ");

            sql.AppendLine();
        }

        void UnionDelegations(StringBuilder sql)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            sql.Append(@"6 as ""Type"", ");
            sql.Append(@"""Id"" as ""Id"", ");
            sql.Append(@"""Level"" as ""Level"", ");
            sql.Append(@"""OpHash"" as ""OpHash"", ");
            sql.Append(@"""Counter"" as ""Counter"", ");
            sql.Append(@"""Nonce"" as ""Nonce"", ");
            sql.Append(@"""Timestamp"" as ""Timestamp"", ");
            sql.Append(@"null::integer as ""Reward"", ");
            sql.Append(@"null::integer as ""Loss"", ");
            sql.Append(@"null::integer as ""Received"", ");
            sql.Append(@"null::integer as ""From"", ");
            sql.Append(@"null::integer as ""Sent"", ");
            sql.Append(@"""BakerFee"" as ""Fee"", ");
            sql.Append(@"""DelegateId"" as ""To"" ");

            sql.Append(@"FROM ""DelegationOps"" ");
            sql.Append(@"WHERE ""SenderId"" = @account AND ""Nonce"" IS NULL ");
            sql.Append(@"AND ""Timestamp"" >= @from AND ""Timestamp"" < @to ");
            sql.Append(@"AND ""BakerFee"" > 0 ");

            sql.AppendLine();
        }

        void UnionOriginations(StringBuilder sql)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            #region originated
            sql.Append(@"7 as ""Type"", ");
            sql.Append(@"""Id"" as ""Id"", ");
            sql.Append(@"""Level"" as ""Level"", ");
            sql.Append(@"""OpHash"" as ""OpHash"", ");
            sql.Append(@"""Counter"" as ""Counter"", ");
            sql.Append(@"""Nonce"" as ""Nonce"", ");
            sql.Append(@"""Timestamp"" as ""Timestamp"", ");
            sql.Append(@"null::integer as ""Reward"", ");
            sql.Append(@"null::integer as ""Loss"", ");
            sql.Append(@"""Balance"" as ""Received"", ");
            sql.Append(@"""SenderId"" as ""From"", ");
            sql.Append(@"null::integer as ""Sent"", ");
            sql.Append(@"null::integer as ""Fee"", ");
            sql.Append(@"null::integer as ""To"" ");

            sql.Append(@"FROM ""OriginationOps"" ");
            sql.Append(@"WHERE ""ContractId"" = @account ");
            sql.Append(@"AND ""Timestamp"" >= @from AND ""Timestamp"" < @to ");
            sql.Append(@"AND ""Balance"" > 0 ");

            sql.AppendLine();
            #endregion

            sql.Append("UNION ALL SELECT ");

            #region sent
            sql.Append(@"7 as ""Type"", ");
            sql.Append(@"""Id"" as ""Id"", ");
            sql.Append(@"""Level"" as ""Level"", ");
            sql.Append(@"""OpHash"" as ""OpHash"", ");
            sql.Append(@"""Counter"" as ""Counter"", ");
            sql.Append(@"""Nonce"" as ""Nonce"", ");
            sql.Append(@"""Timestamp"" as ""Timestamp"", ");
            sql.Append(@"null::integer as ""Reward"", ");
            sql.Append(@"null::integer as ""Loss"", ");
            sql.Append(@"null::integer as ""Received"", ");
            sql.Append(@"null::integer as ""From"", ");
            sql.Append(@"""Balance"" as ""Sent"", ");
            sql.Append(@"CASE WHEN ""Nonce"" is NULL THEN (""BakerFee"" + COALESCE(""StorageFee"", 0) + COALESCE(""AllocationFee"", 0)) ELSE null::integer END as ""Fee"", ");
            sql.Append(@"""ContractId"" as ""To"" ");

            sql.Append(@"FROM ""OriginationOps"" ");
            sql.Append(@"WHERE ""SenderId"" = @account ");
            sql.Append(@"AND ""Timestamp"" >= @from AND ""Timestamp"" < @to ");
            sql.Append(@"AND ""Status"" = 1 ");
            sql.Append(@"AND (""Balance"" > 0 OR CASE WHEN ""Nonce"" is NULL THEN (""BakerFee"" + COALESCE(""StorageFee"", 0) + COALESCE(""AllocationFee"", 0)) ELSE 0 END > 0) ");

            sql.AppendLine();
            #endregion

            sql.Append("UNION ALL SELECT ");

            #region internal
            sql.Append(@"7 as ""Type"", ");
            sql.Append(@"""Id"" as ""Id"", ");
            sql.Append(@"""Level"" as ""Level"", ");
            sql.Append(@"""OpHash"" as ""OpHash"", ");
            sql.Append(@"""Counter"" as ""Counter"", ");
            sql.Append(@"""Nonce"" as ""Nonce"", ");
            sql.Append(@"""Timestamp"" as ""Timestamp"", ");
            sql.Append(@"null::integer as ""Reward"", ");
            sql.Append(@"null::integer as ""Loss"", ");
            sql.Append(@"null::integer as ""Received"", ");
            sql.Append(@"null::integer as ""From"", ");
            sql.Append(@"null::integer as ""Sent"", ");
            sql.Append(@"(COALESCE(""StorageFee"", 0) + COALESCE(""AllocationFee"", 0)) as ""Fee"", ");
            sql.Append(@"""ContractId"" as ""To"" ");

            sql.Append(@"FROM ""OriginationOps"" ");
            sql.Append(@"WHERE ""InitiatorId"" = @account ");
            sql.Append(@"AND ""Timestamp"" >= @from AND ""Timestamp"" < @to ");
            sql.Append(@"AND ""Status"" = 1 ");
            sql.Append(@"AND (COALESCE(""StorageFee"", 0) + COALESCE(""AllocationFee"", 0)) > 0 ");

            sql.AppendLine();
            #endregion

            sql.Append("UNION ALL SELECT ");

            #region failed
            sql.Append(@"7 as ""Type"", ");
            sql.Append(@"""Id"" as ""Id"", ");
            sql.Append(@"""Level"" as ""Level"", ");
            sql.Append(@"""OpHash"" as ""OpHash"", ");
            sql.Append(@"""Counter"" as ""Counter"", ");
            sql.Append(@"""Nonce"" as ""Nonce"", ");
            sql.Append(@"""Timestamp"" as ""Timestamp"", ");
            sql.Append(@"null::integer as ""Reward"", ");
            sql.Append(@"null::integer as ""Loss"", ");
            sql.Append(@"null::integer as ""Received"", ");
            sql.Append(@"null::integer as ""From"", ");
            sql.Append(@"null::integer as ""Sent"", ");
            sql.Append(@"""BakerFee"" as ""Fee"", ");
            sql.Append(@"""ContractId"" as ""To"" ");

            sql.Append(@"FROM ""OriginationOps"" ");
            sql.Append(@"WHERE ""SenderId"" = @account ");
            sql.Append(@"AND ""Timestamp"" >= @from AND ""Timestamp"" < @to ");
            sql.Append(@"AND ""Status"" != 1 AND ""Nonce"" IS NULL ");
            sql.Append(@"AND ""BakerFee"" > 0 ");

            sql.AppendLine();
            #endregion
        }

        void UnionTransactions(StringBuilder sql)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            #region received
            sql.Append(@"8 as ""Type"", ");
            sql.Append(@"""Id"" as ""Id"", ");
            sql.Append(@"""Level"" as ""Level"", ");
            sql.Append(@"""OpHash"" as ""OpHash"", ");
            sql.Append(@"""Counter"" as ""Counter"", ");
            sql.Append(@"""Nonce"" as ""Nonce"", ");
            sql.Append(@"""Timestamp"" as ""Timestamp"", ");
            sql.Append(@"null::integer as ""Reward"", ");
            sql.Append(@"null::integer as ""Loss"", ");
            sql.Append(@"""Amount"" as ""Received"", ");
            sql.Append(@"""SenderId"" as ""From"", ");
            sql.Append(@"null::integer as ""Sent"", ");
            sql.Append(@"null::integer as ""Fee"", ");
            sql.Append(@"null::integer as ""To"" ");

            sql.Append(@"FROM ""TransactionOps"" ");
            sql.Append(@"WHERE ""TargetId"" = @account ");
            sql.Append(@"AND ""Timestamp"" >= @from AND ""Timestamp"" < @to ");
            sql.Append(@"AND ""Status"" = 1 ");
            sql.Append(@"AND ""Amount"" > 0 ");

            sql.AppendLine();
            #endregion

            sql.Append("UNION ALL SELECT ");

            #region sent
            sql.Append(@"8 as ""Type"", ");
            sql.Append(@"""Id"" as ""Id"", ");
            sql.Append(@"""Level"" as ""Level"", ");
            sql.Append(@"""OpHash"" as ""OpHash"", ");
            sql.Append(@"""Counter"" as ""Counter"", ");
            sql.Append(@"""Nonce"" as ""Nonce"", ");
            sql.Append(@"""Timestamp"" as ""Timestamp"", ");
            sql.Append(@"null::integer as ""Reward"", ");
            sql.Append(@"null::integer as ""Loss"", ");
            sql.Append(@"null::integer as ""Received"", ");
            sql.Append(@"null::integer as ""From"", ");
            sql.Append(@"""Amount"" as ""Sent"", ");
            sql.Append(@"CASE WHEN ""Nonce"" is NULL THEN (""BakerFee"" + COALESCE(""StorageFee"", 0) + COALESCE(""AllocationFee"", 0)) ELSE null::integer END as ""Fee"", ");
            sql.Append(@"""TargetId"" as ""To"" ");

            sql.Append(@"FROM ""TransactionOps"" ");
            sql.Append(@"WHERE ""SenderId"" = @account ");
            sql.Append(@"AND ""Timestamp"" >= @from AND ""Timestamp"" < @to ");
            sql.Append(@"AND ""Status"" = 1 ");
            sql.Append(@"AND (""Amount"" > 0 OR CASE WHEN ""Nonce"" is NULL THEN (""BakerFee"" + COALESCE(""StorageFee"", 0) + COALESCE(""AllocationFee"", 0)) ELSE 0 END > 0) ");

            sql.AppendLine();
            #endregion

            sql.Append("UNION ALL SELECT ");

            #region internal
            sql.Append(@"8 as ""Type"", ");
            sql.Append(@"""Id"" as ""Id"", ");
            sql.Append(@"""Level"" as ""Level"", ");
            sql.Append(@"""OpHash"" as ""OpHash"", ");
            sql.Append(@"""Counter"" as ""Counter"", ");
            sql.Append(@"""Nonce"" as ""Nonce"", ");
            sql.Append(@"""Timestamp"" as ""Timestamp"", ");
            sql.Append(@"null::integer as ""Reward"", ");
            sql.Append(@"null::integer as ""Loss"", ");
            sql.Append(@"null::integer as ""Received"", ");
            sql.Append(@"null::integer as ""From"", ");
            sql.Append(@"null::integer as ""Sent"", ");
            sql.Append(@"(COALESCE(""StorageFee"", 0) + COALESCE(""AllocationFee"", 0)) as ""Fee"", ");
            sql.Append(@"""TargetId"" as ""To"" ");

            sql.Append(@"FROM ""TransactionOps"" ");
            sql.Append(@"WHERE ""InitiatorId"" = @account ");
            sql.Append(@"AND ""Timestamp"" >= @from AND ""Timestamp"" < @to ");
            sql.Append(@"AND ""Status"" = 1 ");
            sql.Append(@"AND (COALESCE(""StorageFee"", 0) + COALESCE(""AllocationFee"", 0)) > 0 ");

            sql.AppendLine();
            #endregion

            sql.Append("UNION ALL SELECT ");

            #region failed
            sql.Append(@"8 as ""Type"", ");
            sql.Append(@"""Id"" as ""Id"", ");
            sql.Append(@"""Level"" as ""Level"", ");
            sql.Append(@"""OpHash"" as ""OpHash"", ");
            sql.Append(@"""Counter"" as ""Counter"", ");
            sql.Append(@"""Nonce"" as ""Nonce"", ");
            sql.Append(@"""Timestamp"" as ""Timestamp"", ");
            sql.Append(@"null::integer as ""Reward"", ");
            sql.Append(@"null::integer as ""Loss"", ");
            sql.Append(@"null::integer as ""Received"", ");
            sql.Append(@"null::integer as ""From"", ");
            sql.Append(@"null::integer as ""Sent"", ");
            sql.Append(@"""BakerFee"" as ""Fee"", ");
            sql.Append(@"""TargetId"" as ""To"" ");

            sql.Append(@"FROM ""TransactionOps"" ");
            sql.Append(@"WHERE ""SenderId"" = @account ");
            sql.Append(@"AND ""Timestamp"" >= @from AND ""Timestamp"" < @to ");
            sql.Append(@"AND ""Status"" != 1 AND ""Nonce"" is NULL ");
            sql.Append(@"AND ""BakerFee"" > 0 ");

            sql.AppendLine();
            #endregion
        }

        void UnionReveals(StringBuilder sql)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            sql.Append(@"9 as ""Type"", ");
            sql.Append(@"""Id"" as ""Id"", ");
            sql.Append(@"""Level"" as ""Level"", ");
            sql.Append(@"""OpHash"" as ""OpHash"", ");
            sql.Append(@"""Counter"" as ""Counter"", ");
            sql.Append(@"null::integer as ""Nonce"", ");
            sql.Append(@"""Timestamp"" as ""Timestamp"", ");
            sql.Append(@"null::integer as ""Reward"", ");
            sql.Append(@"null::integer as ""Loss"", ");
            sql.Append(@"null::integer as ""Received"", ");
            sql.Append(@"null::integer as ""From"", ");
            sql.Append(@"null::integer as ""Sent"", ");
            sql.Append(@"""BakerFee"" as ""Fee"", ");
            sql.Append(@"null::integer as ""To"" ");

            sql.Append(@"FROM ""RevealOps"" ");
            sql.Append(@"WHERE ""SenderId"" = @account ");
            sql.Append(@"AND ""Timestamp"" >= @from AND ""Timestamp"" < @to ");
            sql.Append(@"AND ""BakerFee"" > 0 ");

            sql.AppendLine();
        }

        void UnionRegisterConstant(StringBuilder sql)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            sql.Append(@"18 as ""Type"", ");
            sql.Append(@"""Id"" as ""Id"", ");
            sql.Append(@"""Level"" as ""Level"", ");
            sql.Append(@"""OpHash"" as ""OpHash"", ");
            sql.Append(@"""Counter"" as ""Counter"", ");
            sql.Append(@"null::integer as ""Nonce"", ");
            sql.Append(@"""Timestamp"" as ""Timestamp"", ");
            sql.Append(@"null::integer as ""Reward"", ");
            sql.Append(@"null::integer as ""Loss"", ");
            sql.Append(@"null::integer as ""Received"", ");
            sql.Append(@"null::integer as ""From"", ");
            sql.Append(@"null::integer as ""Sent"", ");
            sql.Append(@"(""BakerFee"" + COALESCE(""StorageFee"", 0)) as ""Fee"", ");
            sql.Append(@"null::integer as ""To"" ");

            sql.Append(@"FROM ""RegisterConstantOps"" ");
            sql.Append(@"WHERE ""SenderId"" = @account ");
            sql.Append(@"AND ""Timestamp"" >= @from AND ""Timestamp"" < @to ");
            sql.Append(@"AND (""BakerFee"" > 0 OR COALESCE(""StorageFee"", 0) > 0) ");

            sql.AppendLine();
        }

        void UnionRevelationPenalties(StringBuilder sql)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            sql.Append(@"10 as ""Type"", ");
            sql.Append(@"""Id"" as ""Id"", ");
            sql.Append(@"""Level"" as ""Level"", ");
            sql.Append(@"null::character(51) as ""OpHash"", ");
            sql.Append(@"null::integer as ""Counter"", ");
            sql.Append(@"null::integer as ""Nonce"", ");
            sql.Append(@"""Timestamp"" as ""Timestamp"", ");
            sql.Append(@"null::integer as ""Reward"", "); // TODO: get reward amount from protocol constants
            sql.Append(@"(""LostReward"" + ""LostFees"") as ""Loss"", ");
            sql.Append(@"null::integer as ""Received"", ");
            sql.Append(@"null::integer as ""From"", ");
            sql.Append(@"null::integer as ""Sent"", ");
            sql.Append(@"null::integer as ""Fee"", ");
            sql.Append(@"null::integer as ""To"" ");

            sql.Append(@"FROM ""RevelationPenaltyOps"" ");
            sql.Append(@"WHERE ""BakerId"" = @account ");
            sql.Append(@"AND ""Timestamp"" >= @from AND ""Timestamp"" < @to ");

            sql.AppendLine();
        }

        void UnionMigrations(StringBuilder sql)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            sql.Append(@"(11 + ""Kind"") as ""Type"", ");
            sql.Append(@"""Id"" as ""Id"", ");
            sql.Append(@"""Level"" as ""Level"", ");
            sql.Append(@"null::character(51) as ""OpHash"", ");
            sql.Append(@"null::integer as ""Counter"", ");
            sql.Append(@"null::integer as ""Nonce"", ");
            sql.Append(@"""Timestamp"" as ""Timestamp"", ");
            sql.Append(@"null::integer as ""Reward"", ");
            sql.Append(@"null::integer as ""Loss"", ");
            sql.Append(@"""BalanceChange"" as ""Received"", ");
            sql.Append(@"null::integer as ""From"", ");
            sql.Append(@"null::integer as ""Sent"", ");
            sql.Append(@"null::integer as ""Fee"", ");
            sql.Append(@"null::integer as ""To"" ");

            sql.Append(@"FROM ""MigrationOps"" ");
            sql.Append(@"WHERE ""AccountId"" = @account ");
            sql.Append(@"AND ""Timestamp"" >= @from AND ""Timestamp"" < @to ");
            sql.Append(@"AND ""BalanceChange"" > 0 ");

            sql.AppendLine();
        }

        static readonly string[] Operations = new[]
        {
            "baking",               // 0
            "endorsement",          // 1

            "activation",           // 2
            "double baking",        // 3
            "double endorsing",     // 4
            "nonce revelation",     // 5
            
            "delegation",           // 6
            "origination",          // 7
            "transaction",          // 8
            "reveal",               // 9
            
            "revelation penalty",   // 10
            "bootstrap",            // 11
            "activate delegate",    // 12
            "airdrop",              // 13
            "proposal invoice",     // 14
            "code change",          // 15
            "implicit origination", // 16
            "subsidy",              // 17
            "register constant",    // 18
        };
    }
}
