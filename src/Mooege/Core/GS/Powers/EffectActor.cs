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

using System;
using Mooege.Core.GS.Common.Types.Math;
using Mooege.Core.GS.Objects;
using Mooege.Core.GS.Map;
using Mooege.Core.GS.Ticker.Helpers;
using Mooege.Core.GS.Actors;

namespace Mooege.Core.GS.Powers
{
    public class EffectActor : Actor, IUpdateable
    {
        public TickTimer Timeout;

        public override ActorType ActorType { get { return Actors.ActorType.ClientEffect; } }

        public EffectActor(World world, int actorSNO, Vector3D position, float angle, TickTimer timeout = null)
            : base(world, actorSNO)
        {
            RotationAmount = (float)Math.Cos(angle / 2f);
            RotationAxis = new Vector3D(0, 0, (float)Math.Sin(angle / 2f));

            // FIXME: This is hardcoded crap
            this.Field2 = 0x8; // monster=0x8, using client effect's id results in not being able to do smooth actor movements
            //this.Field2 = 0x18; // used some effects, but not needed?
            this.Field3 = 0x0;
            //this.Field7 = -1; // used by some effects, but not needed?
            this.Field8 = actorSNO;
            if (this.Scale == 0f)
                this.Scale = 1f;
            this.Position = position;
            this.GBHandle.Type = -1; this.GBHandle.GBID = -1; // TODO: use proper enum value
            
            Timeout = timeout;
            
            world.Enter(this);
        }

        public void Update(int tickCounter)
        {
            if (Timeout != null && Timeout.TimedOut)
                this.Destroy();
        }
    }
}
