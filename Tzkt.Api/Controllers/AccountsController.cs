﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

using Tzkt.Api.Models;
using Tzkt.Api.Repositories;
using Tzkt.Api.Services.Cache;

namespace Tzkt.Api.Controllers
{
    [ApiController]
    [Route("v1/accounts")]
    public class AccountsController : ControllerBase
    {
        private readonly AccountRepository Accounts;
        private readonly BalanceHistoryRepository History;
        private readonly ReportRepository Reports;
        private readonly StateCache State;

        public AccountsController(AccountRepository accounts, BalanceHistoryRepository history, ReportRepository reports, StateCache state)
        {
            Accounts = accounts;
            History = history;
            Reports = reports;
            State = state;
        }

        /// <summary>
        /// Get accounts
        /// </summary>
        /// <remarks>
        /// Returns a list of accounts.
        /// </remarks>
        /// <param name="type">Filters accounts by type (`user`, `delegate`, `contract`).</param>
        /// <param name="kind">Filters accounts by contract kind (`delegator_contract` or `smart_contract`)</param>
        /// <param name="delegate">Filters accounts by delegate. Allowed fields for `.eqx` mode: none.</param>
        /// <param name="balance">Filters accounts by balance</param>
        /// <param name="staked">Filters accounts by participation in staking</param>
        /// <param name="lastActivity">Filters accounts by last activity level (where the account was updated)</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you select single field, response will be an array of values in both `.fields` and `.values` modes.</param>
        /// <param name="sort">Sorts delegators by specified field. Supported fields: `id` (default), `balance`, `firstActivity`, `lastActivity`, `numTransactions`, `numContracts`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Account>>> Get(
            AccountTypeParameter type,
            ContractKindParameter kind,
            AccountParameter @delegate,
            Int64Parameter balance,
            BoolParameter staked,
            Int32Parameter lastActivity,
            SelectParameter select,
            SortParameter sort,
            OffsetParameter offset,
            [Range(0, 10000)] int limit = 100)
        {
            #region validate
            if (@delegate != null)
            {
                if (@delegate.Eqx != null)
                    return new BadRequest($"{nameof(@delegate)}.eqx", "This parameter doesn't support .eqx mode.");

                if (@delegate.Nex != null)
                    return new BadRequest($"{nameof(@delegate)}.nex", "This parameter doesn't support .nex mode.");

                if (@delegate.Eq == -1 || @delegate.In?.Count == 0)
                    return Ok(Enumerable.Empty<Account>());
            }

            if (sort != null && !sort.Validate("id", "balance", "firstActivity", "lastActivity", "numTransactions", "numContracts"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            #region optimize
            if (kind?.Eq != null && type == null)
                type = new AccountTypeParameter { Eq = 2 };
            #endregion
            
            if (select == null)
                return Ok(await Accounts.Get(type, kind, @delegate, balance, staked, lastActivity, sort, offset, limit));

            if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    return Ok(await Accounts.Get(type, kind, @delegate, balance, staked, lastActivity, sort, offset, limit, select.Values[0]));
                else
                    return Ok(await Accounts.Get(type, kind, @delegate, balance, staked, lastActivity, sort, offset, limit, select.Values));
            }
            else
            {
                if (select.Fields.Length == 1)
                    return Ok(await Accounts.Get(type, kind, @delegate, balance, staked, lastActivity, sort, offset, limit, select.Fields[0]));
                else
                {
                    return Ok(new SelectionResponse
                    {
                        Cols = select.Fields,
                        Rows = await Accounts.Get(type, kind, @delegate, balance, staked, lastActivity, sort, offset, limit, select.Fields)
                    });
                }
            }
        }

        /// <summary>
        /// Get accounts count
        /// </summary>
        /// <remarks>
        /// Returns a number of accounts.
        /// </remarks>
        /// <param name="type">Filters accounts by type (`user`, `delegate`, `contract`).</param>
        /// <param name="kind">Filters accounts by contract kind (`delegator_contract` or `smart_contract`)</param>
        /// <param name="balance">Filters accounts by balance</param>
        /// <param name="staked">Filters accounts by participation in staking</param>
        /// <returns></returns>
        [HttpGet("count")]
        public Task<int> GetCount(
            AccountTypeParameter type,
            ContractKindParameter kind,
            Int64Parameter balance,
            BoolParameter staked)
        {
            #region optimize
            if (type == null && kind == null && balance == null && staked == null)
                return Task.FromResult(State.Current.AccountsCount);

            if (kind?.Eq != null && type == null)
                type = new AccountTypeParameter { Eq = 2 };
            #endregion

            return Accounts.GetCount(type, kind, balance, staked);
        }

        /// <summary>
        /// Get account by address
        /// </summary>
        /// <remarks>
        /// Returns an account with the specified address.
        /// </remarks>
        /// <param name="address">Account address (starting with tz or KT)</param>
        /// <param name="metadata">Include or not account metadata</param>
        /// <returns></returns>
        [HttpGet("{address}")]
        public Task<Account> GetByAddress([Required][Address] string address, bool metadata = false)
        {
            return Accounts.Get(address, metadata);
        }

        /// <summary>
        /// Get account contracts
        /// </summary>
        /// <remarks>
        /// Returns a list of contracts created by (or related to) the specified account.
        /// </remarks>
        /// <param name="address">Account address (starting with tz or KT)</param>
        /// <param name="sort">Sorts contracts by specified field. Supported fields: `id` (default, desc), `balance`, `creationLevel`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <returns></returns>
        [HttpGet("{address}/contracts")]
        public async Task<ActionResult<IEnumerable<RelatedContract>>> GetContracts(
            [Required][Address] string address,
            SortParameter sort,
            OffsetParameter offset,
            [Range(0, 10000)] int limit = 100)
        {
            #region validate
            if (sort != null && !sort.Validate("id", "balance", "creationLevel"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            return Ok(await Accounts.GetRelatedContracts(address, sort, offset, limit));
        }

        /// <summary>
        /// Get account delegators
        /// </summary>
        /// <remarks>
        /// Returns a list of accounts delegated to the specified account.
        /// </remarks>
        /// <param name="address">Account address (starting with tz)</param>
        /// <param name="type">Filters delegators by type (`user`, `delegate`, `contract`).</param>
        /// <param name="balance">Filters delegators by balance.</param>
        /// <param name="delegationLevel">Number of items to skip</param>
        /// <param name="sort">Sorts delegators by specified field. Supported fields: `delegationLevel` (default, desc), `balance`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <returns></returns>
        [HttpGet("{address}/delegators")]
        public async Task<ActionResult<IEnumerable<Delegator>>> GetDelegators(
            [Required][TzAddress] string address,
            AccountTypeParameter type,
            Int64Parameter balance,
            Int32Parameter delegationLevel,
            SortParameter sort,
            OffsetParameter offset,
            [Range(0, 10000)] int limit = 100)
        {
            #region validate
            if (sort != null && !sort.Validate("balance", "delegationLevel"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            return Ok(await Accounts.GetDelegators(address, type, balance, delegationLevel, sort, offset, limit));
        }

        /// <summary>
        /// Get account operations
        /// </summary>
        /// <remarks>
        /// Returns a list of operations related to the specified account.
        /// Note: for better flexibility this endpoint accumulates query parameters (filters) of each `/operations/{type}` endpoint,
        /// so a particular filter may affect several operation types containing this filter.
        /// For example, if you specify an `initiator` it will affect all transactions, delegations and originations,
        /// because all these types have an `initiator` field.
        /// </remarks>
        /// <param name="address">Account address (starting with tz or KT)</param>
        /// <param name="type">Comma separated list of operation types to return (`endorsement`, `ballot`, `proposal`, `activation`, `double_baking`, `double_endorsing`, `nonce_revelation`, `delegation`, `origination`, `transaction`, `reveal`, `migration`, `revelation_penalty`, `baking`). If not specified then all operation types except `endorsement` and `baking` will be returned.</param>
        /// <param name="initiator">Filters transactions, delegations and originations by initiator. Allowed fields for `.eqx` mode: none.</param>
        /// <param name="sender">Filters transactions, delegations, originations, reveals and seed nonce revelations by sender. Allowed fields for `.eqx` mode: none.</param>
        /// <param name="target">Filters transactions by target. Allowed fields for `.eqx` mode: none.</param>
        /// <param name="prevDelegate">Filters delegations by prev delegate. Allowed fields for `.eqx` mode: none.</param>
        /// <param name="newDelegate">Filters delegations by new delegate. Allowed fields for `.eqx` mode: none.</param>
        /// <param name="contractManager">Filters origination operations by manager. Allowed fields for `.eqx` mode: none.</param>
        /// <param name="contractDelegate">Filters origination operations by delegate. Allowed fields for `.eqx` mode: none.</param>
        /// <param name="originatedContract">Filters origination operations by originated contract. Allowed fields for `.eqx` mode: none.</param>
        /// <param name="accuser">Filters double baking and double endorsing by accuser. Allowed fields for `.eqx` mode: none.</param>
        /// <param name="offender">Filters double baking and double endorsing by offender. Allowed fields for `.eqx` mode: none.</param>
        /// <param name="baker">Filters seed nonce revelation operations by baker. Allowed fields for `.eqx` mode: none.</param>
        /// <param name="level">Filters operations by level.</param>
        /// <param name="timestamp">Filters operations by timestamp.</param>
        /// <param name="entrypoint">Filters transactions by entrypoint called on the target contract.</param>
        /// <param name="parameter">Filters transactions by parameter value. Note, this query parameter supports the following format: `?parameter{.path?}{.mode?}=...`,
        /// so you can specify a path to a particular field to filter by, for example: `?parameter.token_id=...` or `?parameter.sigs.0.ne=...`.</param>
        /// <param name="parameters">**DEPRECATED**. Use `entrypoint` and `parameter` instead.</param>
        /// <param name="hasInternals">Filters transactions by presence of internal operations.</param>
        /// <param name="status">Filters transactions, delegations, originations and reveals by operation status (`applied`, `failed`, `backtracked`, `skipped`).</param>
        /// <param name="sort">Sort mode (0 - ascending, 1 - descending), operations of different types can only be sorted by ID.</param>
        /// <param name="lastId">Id of the last operation received, which is used as an offset for pagination</param>
        /// <param name="limit">Number of items to return</param>
        /// <param name="micheline">Format of the parameters, storage and diffs: `0` - JSON, `1` - JSON string, `2` - raw micheline, `3` - raw micheline string</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <param name="from">**DEPRECATED**. Use `timestamp.ge=` intead.</param>
        /// <param name="to">**DEPRECATED**. Use `timestamp.lt=` intead.</param>
        /// <returns></returns>
        [HttpGet("{address}/operations")]
        public async Task<ActionResult<IEnumerable<Operation>>> GetOperations(
            [Required][Address] string address,
            string type,
            AccountParameter initiator,
            AccountParameter sender,
            AccountParameter target,
            AccountParameter prevDelegate,
            AccountParameter newDelegate,
            AccountParameter contractManager,
            AccountParameter contractDelegate,
            AccountParameter originatedContract,
            AccountParameter accuser,
            AccountParameter offender,
            AccountParameter baker,
            Int32Parameter level,
            DateTimeParameter timestamp,
            StringParameter entrypoint,
            JsonParameter parameter,
            StringParameter parameters,
            BoolParameter hasInternals,
            OperationStatusParameter status,
            SortMode sort = SortMode.Descending,
            int? lastId = null,
            [Range(0, 1000)] int limit = 100,
            MichelineFormat micheline = MichelineFormat.Json,
            Symbols quote = Symbols.None,
            DateTimeOffset? from = null,
            DateTimeOffset? to = null)
        {
            #region validate
            if (initiator != null)
            {
                if (initiator.Eqx != null)
                    return new BadRequest($"{nameof(initiator)}.eqx", "This parameter doesn't support .eqx mode.");

                if (initiator.Nex != null)
                    return new BadRequest($"{nameof(initiator)}.eqx", "This parameter doesn't support .eqx mode.");
            }

            if (sender != null)
            {
                if (sender.Eqx != null)
                    return new BadRequest($"{nameof(sender)}.eqx", "This parameter doesn't support .eqx mode.");

                if (sender.Nex != null)
                    return new BadRequest($"{nameof(sender)}.eqx", "This parameter doesn't support .eqx mode.");
            }

            if (target != null)
            {
                if (target.Eqx != null)
                    return new BadRequest($"{nameof(target)}.eqx", "This parameter doesn't support .eqx mode.");

                if (target.Nex != null)
                    return new BadRequest($"{nameof(target)}.eqx", "This parameter doesn't support .eqx mode.");
            }

            if (prevDelegate != null)
            {
                if (prevDelegate.Eqx != null)
                    return new BadRequest($"{nameof(prevDelegate)}.eqx", "This parameter doesn't support .eqx mode.");

                if (prevDelegate.Nex != null)
                    return new BadRequest($"{nameof(prevDelegate)}.nex", "This parameter doesn't support .nex mode.");
            }

            if (newDelegate != null)
            {
                if (newDelegate.Eqx != null)
                    return new BadRequest($"{nameof(newDelegate)}.eqx", "This parameter doesn't support .eqx mode.");

                if (newDelegate.Nex != null)
                    return new BadRequest($"{nameof(newDelegate)}.nex", "This parameter doesn't support .nex mode.");
            }

            if (contractManager != null)
            {
                if (contractManager.Eqx != null)
                    return new BadRequest($"{nameof(contractManager)}.eqx", "This parameter doesn't support .eqx mode.");

                if (contractManager.Nex != null)
                    return new BadRequest($"{nameof(contractManager)}.nex", "This parameter doesn't support .nex mode.");
            }

            if (contractDelegate != null)
            {
                if (contractDelegate.Eqx != null)
                    return new BadRequest($"{nameof(contractDelegate)}.eqx", "This parameter doesn't support .eqx mode.");

                if (contractDelegate.Nex != null)
                    return new BadRequest($"{nameof(contractDelegate)}.nex", "This parameter doesn't support .nex mode.");
            }

            if (originatedContract != null)
            {
                if (originatedContract.Eqx != null)
                    return new BadRequest($"{nameof(originatedContract)}.eqx", "This parameter doesn't support .eqx mode.");

                if (originatedContract.Nex != null)
                    return new BadRequest($"{nameof(originatedContract)}.nex", "This parameter doesn't support .nex mode.");
            }

            if (accuser != null)
            {
                if (accuser.Eqx != null)
                    return new BadRequest($"{nameof(accuser)}.eqx", "This parameter doesn't support .eqx mode.");

                if (accuser.Nex != null)
                    return new BadRequest($"{nameof(accuser)}.nex", "This parameter doesn't support .nex mode.");
            }

            if (offender != null)
            {
                if (offender.Eqx != null)
                    return new BadRequest($"{nameof(offender)}.eqx", "This parameter doesn't support .eqx mode.");

                if (offender.Nex != null)
                    return new BadRequest($"{nameof(offender)}.nex", "This parameter doesn't support .nex mode.");
            }

            if (baker != null)
            {
                if (baker.Eqx != null)
                    return new BadRequest($"{nameof(baker)}.eqx", "This parameter doesn't support .eqx mode.");

                if (baker.Nex != null)
                    return new BadRequest($"{nameof(baker)}.nex", "This parameter doesn't support .nex mode.");
            }
            #endregion

            var types = type != null ? new HashSet<string>(type.Split(',')) : OpTypes.DefaultSet;

            var _sort = sort == SortMode.Ascending
                ? new SortParameter { Asc = "Id" }
                : new SortParameter { Desc = "Id" };

            var _offset = lastId != null
                ? new OffsetParameter { Cr = lastId }
                : null;

            #region legacy
            if (timestamp == null && (from != null || to != null))
                timestamp = new DateTimeParameter();

            if (from != null) timestamp.Ge = from.Value.DateTime;
            if (to != null) timestamp.Lt = to.Value.DateTime;
            #endregion

            return Ok(await Accounts.GetOperations(address, types, initiator, sender, target, prevDelegate, newDelegate, contractManager, contractDelegate, originatedContract, accuser, offender, baker, level, timestamp, entrypoint, parameter, parameters, hasInternals, status, _sort, _offset, limit, micheline, quote));
        }

        /// <summary>
        /// Get account metadata
        /// </summary>
        /// <remarks>
        /// Returns metadata of the specified account (alias, logo, website, contacts, etc).
        /// </remarks>
        /// <param name="address">Account address (starting with tz or KT)</param>
        /// <returns></returns>
        [HttpGet("{address}/metadata")]
        public Task<AccountMetadata> GetMetadata([Required][Address] string address)
        {
            return Accounts.GetMetadata(address);
        }

        /// <summary>
        /// Get counter
        /// </summary>
        /// <remarks>
        /// Returns account counter
        /// </remarks>
        /// <param name="address">Account address (starting with tz or KT)</param>
        /// <returns></returns>
        [HttpGet("{address}/counter")]
        public async Task<int> GetCounter([Required][Address] string address)
        {
            var rawAccount = await Accounts.GetRawAsync(address);
            return rawAccount == null || rawAccount is RawUser && rawAccount.Balance == 0
                ? State.Current.ManagerCounter
                : rawAccount.Counter;
        }

        /// <summary>
        /// Get balance
        /// </summary>
        /// <remarks>
        /// Returns account balance
        /// </remarks>
        /// <param name="address">Account address (starting with tz or KT)</param>
        /// <returns></returns>
        [HttpGet("{address}/balance")]
        public async Task<long> GetBalance([Required][Address] string address)
        {
            return (await Accounts.GetRawAsync(address))?.Balance ?? 0;
        }

        /// <summary>
        /// Get balance at level
        /// </summary>
        /// <remarks>
        /// Returns account balance at the specified block
        /// </remarks>
        /// <param name="address">Account address (starting with tz or KT)</param>
        /// <param name="level">Block height at which you want to know account balance</param>
        /// <returns></returns>
        [HttpGet("{address}/balance_history/{level:int}")]
        public Task<long> GetBalanceAtLevel([Required][Address] string address, [Min(0)] int level)
        {
            return History.Get(address, level);
        }

        /// <summary>
        /// Get balance at date
        /// </summary>
        /// <remarks>
        /// Returns account balance at the specified datetime
        /// </remarks>
        /// <param name="address">Account address (starting with tz or KT)</param>
        /// <param name="datetime">Datetime at which you want to know account balance (e.g. `2020-01-01`, or `2019-12-30T23:42:59Z`)</param>
        /// <returns></returns>
        [HttpGet("{address}/balance_history/{datetime:DateTime}")]
        public Task<long> GetBalanceAtDate([Required][Address] string address, DateTimeOffset datetime)
        {
            return History.Get(address, datetime.DateTime);
        }

        /// <summary>
        /// Get balance history
        /// </summary>
        /// <remarks>
        /// Returns time series with historical balances (only changes, without duplicates).
        /// </remarks>
        /// <param name="address">Account address (starting with tz or KT)</param>
        /// <param name="step">Step of the time series, for example if `step = 1000` you will get balances at blocks `1000, 2000, 3000, ...`.</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you select single field, response will be an array of values in both `.fields` and `.values` modes.</param>
        /// <param name="sort">Sorts historical balances by specified field. Supported fields: `level`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("{address}/balance_history")]
        public async Task<ActionResult<IEnumerable<HistoricalBalance>>> GetBalanceHistory(
            [Required][Address] string address,
            [Min(1)] int? step,
            SelectParameter select,
            SortParameter sort,
            [Min(0)] int offset = 0,
            [Range(0, 10000)] int limit = 100,
            Symbols quote = Symbols.None)
        {
            #region validate
            if (sort != null && !sort.Validate("level"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            if (select == null)
                return Ok(await History.Get(address, step ?? 1, sort, offset, limit, quote));

            if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    return Ok(await History.Get(address, step ?? 1, sort, offset, limit, select.Values[0], quote));
                else
                    return Ok(await History.Get(address, step ?? 1, sort, offset, limit, select.Values, quote));
            }
            else
            {
                if (select.Fields.Length == 1)
                    return Ok(await History.Get(address, step ?? 1, sort, offset, limit, select.Fields[0], quote));
                else
                {
                    return Ok(new SelectionResponse
                    {
                        Cols = select.Fields,
                        Rows = await History.Get(address, step ?? 1, sort, offset, limit, select.Fields, quote)
                    });
                }
            }
        }

        /// <summary>
        /// Get account report
        /// </summary>
        /// <remarks>
        /// Exports account balance report in .csv format
        /// </remarks>
        /// <param name="address">Account address (starting with tz or KT)</param>
        /// <param name="from">Start of the datetime range to filter by (ISO 8601, e.g. 2019-11-31)</param>
        /// <param name="to">End of the datetime range to filter by (ISO 8601, e.g. 2019-12-31)</param>
        /// <param name="delimiter">Column delimiter (`comma`, `semicolon`)</param>
        /// <param name="separator">Decimal separator (`comma`, `point`)</param>
        /// <param name="currency">Currency to convert amounts to (`btc`, `eur`, `usd`, `cny`, `jpy`, `krw`, `eth`, `gbp`)</param>
        /// <param name="historical">`true` if you want to use historical prices, `false` to use current price</param>
        /// <returns></returns>
        [HttpGet("{address}/report")]
        public async Task<ActionResult> GetBalanceReport(
            [Required][Address] string address,
            DateTimeOffset? from,
            DateTimeOffset? to,
            string currency,
            bool historical = false,
            string delimiter = "comma",
            string separator = "point")
        {
            #region verify delimiter
            if (delimiter == "comma")
            {
                delimiter = ",";
            }
            else if (delimiter == "semicolon")
            {
                delimiter = ";";
            }
            else
            {
                return new BadRequest(nameof(delimiter), "Unsupported value");
            }
            #endregion

            #region verify separator
            if (separator == "comma")
            {
                separator = ",";
            }
            else if (separator == "point")
            {
                separator = ".";
            }
            else
            {
                return new BadRequest(nameof(separator), "Unsupported value");
            }
            #endregion

            #region verify symbol
            var symbol = currency switch
            {
                "btc" => 0,
                "eur" => 1,
                "usd" => 2,
                "cny" => 3,
                "jpy" => 4,
                "krw" => 5,
                "eth" => 6,
                "gbp" => 7,
                _ => -1
            };
            #endregion

            var _from = from?.DateTime ?? DateTime.MinValue;
            var _to = to?.DateTime ?? DateTime.MaxValue;

            var stream = new MemoryStream();
            var csv = new StreamWriter(stream);

            if (symbol == -1)
            {
                await Reports.Write(csv, address, _from, _to, 257_000, delimiter, separator);
            }
            else if (historical)
            {
                await Reports.WriteHistorical(csv, address, _from, _to, 257_000, delimiter, separator, symbol);
            }
            else
            {
                await Reports.Write(csv, address, _from, _to, 257_000, delimiter, separator, symbol);
            }

            stream.Seek(0, SeekOrigin.Begin);
            return new FileStreamResult(stream, "text/csv")
            {
                FileDownloadName = $"{address[..9]}..{address[^6..]}_{_from.ToShortDateString()}-{_to.ToShortDateString()}.csv"
            };
        }
    }
}
