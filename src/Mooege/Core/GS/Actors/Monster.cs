﻿/*
 * Copyright (C) 2011 mooege project
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
 */

using System.Collections.Generic;
using System.Linq;
using Mooege.Common.Helpers;
using Mooege.Common.MPQ.FileFormats.Types;
using Mooege.Core.GS.Map;
using Mooege.Core.GS.Objects;
using Mooege.Core.GS.Players;
using Mooege.Net.GS.Message;
using Mooege.Net.GS.Message.Definitions.World;
using Mooege.Net.GS.Message.Fields;
using Mooege.Net.GS.Message.Definitions.Animation;
using Mooege.Net.GS.Message.Definitions.Effect;
using Mooege.Net.GS.Message.Definitions.Misc;
using Mooege.Common.MPQ;
using Mooege.Core.GS.Common.Types.SNO;
using Mooege.Core.GS.Actors.Helpers;

namespace Mooege.Core.GS.Actors
{
    public class Monster : Living, IUpdateable
    {
        public override ActorType ActorType { get { return ActorType.Monster; } }

        public int LoreSNOId { get; private set; }

        public Monster(World world, int snoId, Dictionary<int, TagMapEntry> tags)
            : base(world, snoId, tags)
        {
            this.Field2 = 0x8;
            this.GBHandle.Type = (int)GBHandleType.Monster; this.GBHandle.GBID = 1;
            this.Attributes[GameAttribute.TeamID] = 10;
            this.Attributes[GameAttribute.Experience_Granted] = 125;
            var monsterAsset = MPQStorage.Data.Assets[SNOGroup.Monster][SNOMonsterId];
            var monsterData = monsterAsset.Data as Mooege.Common.MPQ.FileFormats.Monster;
            if (monsterData != null)
            {
                LoreSNOId = monsterData.SNOLore;
            }
            else
            {
                LoreSNOId = -1;
            }
        }

        public override void OnTargeted(Player player, TargetMessage message)
        {
            this.Die(player);
        }


        public void Update(int tickCounter)
        {
            if (this.Brain == null)
                return;

            this.Brain.Think(tickCounter);
        }

        // FIXME: Hardcoded hell. /komiga
        public void Die(Player player)
        {
            /*var killAni = new int[]{
                    0x2cd7,
                    0x2cd4,
                    0x01b378,
                    0x2cdc,
                    0x02f2,
                    0x2ccf,
                    0x2cd0,
                    0x2cd1,
                    0x2cd2,
                    0x2cd3,
                    0x2cd5,
                    0x01b144,
                    0x2cd6,
                    0x2cd8,
                    0x2cda,
                    0x2cd9
            };*/

            this.World.BroadcastIfRevealed(new PlayEffectMessage()
            {
                ActorId = this.DynamicID,
                Effect = Effect.Hit,
                OptionalParameter = 0x2,
            }, this);

            this.World.BroadcastIfRevealed(new PlayEffectMessage()
            {
                ActorId = this.DynamicID,
                Effect = Effect.Unknown12,
            }, this);

            this.World.BroadcastIfRevealed(new PlayHitEffectMessage()
            {
                ActorID = this.DynamicID,
                HitDealer = player.DynamicID,
                Field2 = 0x2,
                Field3 = false,
            }, this);

            this.World.BroadcastIfRevealed(new FloatingNumberMessage()
            {
                ActorID = this.DynamicID,
                Number = 9001.0f,
                Type = FloatingNumberMessage.FloatType.White,
            }, this);

            this.World.BroadcastIfRevealed(new ANNDataMessage(Opcodes.ANNDataMessage13)
            {
                ActorID = this.DynamicID
            }, this);

            player.UpdateExp(this.Attributes[GameAttribute.Experience_Granted]);
            player.ExpBonusData.Update(player.GBHandle.Type, this.GBHandle.Type);

            this.World.BroadcastIfRevealed(new PlayAnimationMessage()
            {
                ActorID = this.DynamicID,
                Field1 = 0xb,
                Field2 = 0,
                tAnim = new PlayAnimationMessageSpec[1]
                {
                    new PlayAnimationMessageSpec()
                    {
                        Field0 = 0x2,
                        Field1 = AnimationSet.GetRandomDeath(),//killAni[RandomHelper.Next(killAni.Length)],
                        Field2 = 0x0,
                        Field3 = 1f
                    }
                }
            }, this);

            this.World.BroadcastIfRevealed(new ANNDataMessage(Opcodes.ANNDataMessage24)
            {
                ActorID = this.DynamicID,
            }, this);

            GameAttributeMap attribs = new GameAttributeMap();
            attribs[GameAttribute.Hitpoints_Cur] = 0f;
            attribs[GameAttribute.Could_Have_Ragdolled] = true;
            attribs[GameAttribute.Deleted_On_Server] = true;

            foreach (var msg in attribs.GetMessageList(this.DynamicID))
                this.World.BroadcastIfRevealed(msg, this);

            this.World.SpawnRandomItemDrop(player, this.Position);
            this.World.SpawnGold(player, this.Position);
            if (RandomHelper.Next(1, 100) < 20)
                this.World.SpawnHealthGlobe(player, this.Position);
            this.PlayLore();
            this.Destroy();
        }

        /// <summary>
        /// Plays lore for first death of this monster's death.
        /// </summary>
        private void PlayLore()
        {
            if (LoreSNOId != -1)
            {
                var players = this.GetPlayersInRange();
                if (players != null)
                {
                    foreach (var player in players.Where(player => !player.HasLore(LoreSNOId)))
                    {
                        player.PlayLore(LoreSNOId, false);
                    }
                }
            }
        }
                    
    }
}
