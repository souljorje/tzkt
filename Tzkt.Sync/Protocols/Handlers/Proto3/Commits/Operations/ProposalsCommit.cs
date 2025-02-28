﻿using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto3
{
    class ProposalsCommit : ProtocolCommit
    {
        public ProposalsCommit(ProtocolHandler protocol) : base(protocol) { }

        public virtual async Task Apply(Block block, JsonElement op, JsonElement content)
        {
            #region init
            var period = await Cache.Periods.GetAsync(content.RequiredInt32("period"));
            var sender = Cache.Accounts.GetDelegate(content.RequiredString("source"));
            
            var snapshot = await Db.VotingSnapshots
                .FirstOrDefaultAsync(x => x.Period == period.Index && x.BakerId == sender.Id)
                    ?? throw new ValidationException("Proposal sender is not on the voters list");

            var proposalOperations = new List<ProposalOperation>(4);
            foreach (var proposalHash in content.RequiredArray("proposals").EnumerateArray())
            {
                var proposal = await Cache.Proposals.GetOrCreateAsync(proposalHash.RequiredString(), period.Epoch, () => new Proposal
                {
                    Hash = proposalHash.RequiredString(),
                    Epoch = period.Epoch,
                    FirstPeriod = period.Index,
                    LastPeriod = period.Index,
                    InitiatorId = sender.Id,
                    Status = ProposalStatus.Active
                });

                var duplicated = proposalOperations.Any(x => x.Period == period.Index && x.Sender.Id == sender.Id && x.Proposal.Hash == proposal.Hash);
                if (!duplicated) duplicated = block.Proposals?.Any(x => x.Period == period.Index && x.Sender.Id == sender.Id && x.Proposal.Hash == proposal.Hash) ?? false;
                if (!duplicated) duplicated = await Db.ProposalOps.AnyAsync(x => x.Period == period.Index && x.SenderId == sender.Id && x.ProposalId == proposal.Id);

                proposalOperations.Add(new ProposalOperation
                {
                    Id = Cache.AppState.NextOperationId(),
                    Block = block,
                    Level = block.Level,
                    Timestamp = block.Timestamp,
                    OpHash = op.RequiredString("hash"),
                    Sender = sender,
                    Rolls = snapshot.Rolls,
                    Duplicated = duplicated,
                    Epoch = period.Epoch,
                    Period = period.Index,
                    Proposal = proposal
                });
            }
            #endregion

            foreach (var proposalOp in proposalOperations)
            {
                #region entities
                var proposal = proposalOp.Proposal;

                //Db.TryAttach(block);
                Db.TryAttach(period);
                Db.TryAttach(sender);
                Db.TryAttach(proposal);
                //Db.TryAttach(snapshot);
                #endregion

                #region apply operation
                if (!proposalOp.Duplicated)
                {
                    if (proposal.Upvotes == 0)
                        period.ProposalsCount++;

                    proposal.Upvotes++;
                    proposal.Rolls += proposalOp.Rolls;

                    if (proposal.Rolls > period.TopRolls)
                    {
                        period.TopUpvotes = proposal.Upvotes;
                        period.TopRolls = proposal.Rolls;
                    }

                    snapshot.Status = VoterStatus.Upvoted;
                }

                sender.ProposalsCount++;

                block.Operations |= Operations.Proposals;
                #endregion

                Db.ProposalOps.Add(proposalOp);
            }
        }

        public virtual async Task Revert(Block block, ProposalOperation proposalOp)
        {
            #region init
            proposalOp.Block ??= block;
            proposalOp.Sender ??= Cache.Accounts.GetDelegate(proposalOp.SenderId);
            proposalOp.Proposal ??= await Cache.Proposals.GetAsync(proposalOp.ProposalId);

            var snapshot = await Db.VotingSnapshots
                .FirstAsync(x => x.Period == proposalOp.Period && x.BakerId == proposalOp.Sender.Id);

            var period = await Cache.Periods.GetAsync(proposalOp.Period);
            #endregion

            #region entities
            var sender = proposalOp.Sender;
            var proposal = proposalOp.Proposal;

            //Db.TryAttach(block);
            Db.TryAttach(period);
            Db.TryAttach(sender);
            Db.TryAttach(proposal);
            //Db.TryAttach(snapshot);
            #endregion

            #region revert operation
            if (!proposalOp.Duplicated)
            {
                proposal.Upvotes--;
                proposal.Rolls -= proposalOp.Rolls;

                if (period.ProposalsCount > 1)
                {
                    var proposals = await Db.Proposals
                        .AsNoTracking()
                        .Where(x => x.Epoch == period.Epoch)
                        .ToListAsync();

                    var curr = proposals.First(x => x.Id == proposal.Id);
                    curr.Rolls -= proposalOp.Rolls;
                    curr.Upvotes--;

                    var prevMax = proposals
                        .OrderByDescending(x => x.Rolls)
                        .First();

                    period.TopUpvotes = prevMax.Upvotes;
                    period.TopRolls = prevMax.Rolls;
                }
                else
                {
                    period.TopUpvotes = proposal.Upvotes;
                    period.TopRolls = proposal.Rolls;
                }

                if (proposal.Upvotes == 0)
                    period.ProposalsCount--;

                if (!await Db.ProposalOps.AnyAsync(x => x.Period == period.Index && x.SenderId == sender.Id && x.Id < proposalOp.Id))
                    snapshot.Status = VoterStatus.None;
            }

            sender.ProposalsCount--;
            #endregion

            if (proposal.Upvotes == 0)
            {
                Db.Proposals.Remove(proposal);
                Cache.Proposals.Remove(proposal);
            }

            Db.ProposalOps.Remove(proposalOp);
        }
    }
}
