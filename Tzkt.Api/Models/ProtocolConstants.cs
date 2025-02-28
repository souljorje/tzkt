﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tzkt.Api.Models
{
    public class ProtocolConstants
    {
        /// <summary>
        /// The number of cycles where security deposit is ramping up
        /// </summary>
        public int RampUpCycles { get; set; }

        /// <summary>
        /// The number of cycles with no baking rewards
        /// </summary>
        public int NoRewardCycles { get; set; }

        /// <summary>
        /// A number of cycles in which baker's security deposit and rewards are frozen
        /// </summary>
        public int PreservedCycles { get; set; }

        /// <summary>
        /// A number of blocks the cycle contains
        /// </summary>
        public int BlocksPerCycle { get; set; }
        
        /// <summary>
        /// A number of blocks that indicates how often seed nonce hash is included in a block. Seed nonce hash presents in only one out of `blocksPerCommitment`
        /// </summary>
        public int BlocksPerCommitment { get; set; }
        
        /// <summary>
        /// A number of blocks that indicates how often a snapshot (snapshots are records of the state of rolls distributions) is taken
        /// </summary>
        public int BlocksPerSnapshot { get; set; }
        
        /// <summary>
        /// A number of block that indicates how long a voting period takes
        /// </summary>
        public int BlocksPerVoting { get; set; }

        /// <summary>
        /// Minimum amount of seconds between blocks
        /// </summary>
        public int TimeBetweenBlocks { get; set; }

        /// <summary>
        /// Number of bakers that assigned to endorse a block
        /// </summary>
        public int EndorsersPerBlock { get; set; }
        
        /// <summary>
        /// Maximum amount of gas that one operation can consume
        /// </summary>
        public int HardOperationGasLimit { get; set; }
        
        /// <summary>
        /// Maximum amount of storage that one operation can consume
        /// </summary>
        public int HardOperationStorageLimit { get; set; }
        
        /// <summary>
        /// Maximum amount of total gas usage of a single block
        /// </summary>
        public int HardBlockGasLimit { get; set; }

        /// <summary>
        /// Required number of tokens to get 1 roll (micro tez)
        /// </summary>
        public long TokensPerRoll { get; set; }
        
        /// <summary>
        /// Reward for seed nonce revelation (micro tez)
        /// </summary>
        public long RevelationReward { get; set; }

        /// <summary>
        /// Security deposit for baking (producing) a block (micro tez)
        /// </summary>
        public long BlockDeposit { get; set; }
        
        //TODO Think about it
        /// <summary>
        /// Reward for baking (producing) a block (micro tez)
        /// </summary>
        public List<long> BlockReward { get; set; }

        /// <summary>
        /// Security deposit for sending an endorsement operation (micro tez)
        /// </summary>
        public long EndorsementDeposit { get; set; }
        
        /// <summary>
        /// Reward for sending an endorsement operation (micro tez)
        /// </summary>
        public List<long> EndorsementReward { get; set; }

        /// <summary>
        /// Initial storage size of an originated (created) account (bytes)
        /// </summary>
        public int OriginationSize { get; set; }
        
        /// <summary>
        /// Cost of one storage byte in the blockchain (micro tez)
        /// </summary>
        public int ByteCost { get; set; }

        /// <summary>
        /// Percentage of the total number of rolls required to select a proposal on the proposal period
        /// </summary>
        public double ProposalQuorum { get; set; }
        
        /// <summary>
        /// The minimum value of quorum percentage on the exploration and promotion periods
        /// </summary>
        public double BallotQuorumMin { get; set; }

        /// <summary>
        /// The maximum value of quorum percentage on the exploration and promotion periods
        /// </summary>
        public double BallotQuorumMax { get; set; }

        /// <summary>
        /// Liquidity baking subsidy is 1/16th of total rewards for a block of priority 0 with all endorsements
        /// </summary>
        public int LBSubsidy { get; set; }
        /// <summary>
        /// Level after protocol activation when liquidity baking shuts off
        /// </summary>
        public int LBSunsetLevel { get; set; }
        /// <summary>
        /// 1/2 window size of 2000 blocks with precision of 1000 for integer computation
        /// </summary>
        public int LBEscapeThreshold { get; set; }
    }
}
