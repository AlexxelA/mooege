/*
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
using System.Collections.Generic;
using Mooege.Core.GS.Actors;
using Mooege.Core.GS.Common.Types.Math;
using Mooege.Net.GS.Message.Definitions.ACD;
using Mooege.Core.GS.Ticker.Helpers;

namespace Mooege.Core.GS.Powers.Implementations
{
    [ImplementsPowerSNO(Skills.Skills.Barbarian.FuryGenerators.Bash)]
    public class BarbarianBash : PowerImplementation
    {
        public override IEnumerable<TickTimer> Run()
        {
            yield return WaitSeconds(0.20f); // wait for swing animation

            User.PlayEffectGroup(18662);

            if (CanHitMeleeTarget(Target))
            {
                GeneratePrimaryResource(6f);
                
                if (Rand.NextDouble() < 0.20)
                    Knockback(Target, 4f);

                WeaponDamage(Target, 1.45f, DamageType.Physical);
            }

            yield break;
        }
    }

    [ImplementsPowerSNO(Skills.Skills.Barbarian.FuryGenerators.Cleave)]
    public class BarbarianCleave : PowerImplementation
    {
        public override IEnumerable<TickTimer> Run()
        {
            yield return WaitSeconds(0.25f); // wait for swing animation

            //Cleave have a different animation based on the swing side wich is cointain in the Animpreplay data of the target message
            if (this.Message.Field6.Field0 == 1)            
                User.PlayEffectGroup(18671);            
            else            
                User.PlayEffectGroup(18672);            

            GeneratePrimaryResource(5f);

            foreach (Actor actor in GetTargetsInRange(User.Position, 12f))
            {
                if (PowerMath.PointInBeam(actor.Position, User.Position, PowerMath.ProjectAndTranslate2D(User.Position, TargetPosition, User.Position, 15f), 15f))
                {
                    WeaponDamage(actor, 1.20f, DamageType.Physical, true);
                }
            }

            yield break;
        }
    }

    [ImplementsPowerSNO(Skills.Skills.Barbarian.FuryGenerators.GroundStomp)]
    public class BarbarianGroundStomp : PowerImplementation
    {
        public override IEnumerable<TickTimer> Run()
        {
            yield return WaitSeconds(0.200f); // wait for swing animation

            User.PlayEffectGroup(18685);

            bool hitAnything = false;

            foreach (Actor actor in GetTargetsInRange(User.Position, 17f))
            {
                WeaponDamage(actor, 0.7f, DamageType.Physical, true);
                Stunt(actor);
                hitAnything = true;
            }

            if(hitAnything)
                GeneratePrimaryResource(15f);

            //FIXME
            //Add power cooldown

            yield break;
        }
    }

    [ImplementsPowerSNO(Skills.Skills.Barbarian.FuryGenerators.LeapAttack)]
    public class BarbarianLeap : PowerImplementation
    {
        public override IEnumerable<TickTimer> Run()
        {
            User.TranslateArc(TargetPosition, 69792, 1.48324f);

            // wait for leap to hit
            yield return WaitSeconds(0.60f);

            // ground smash effect
            User.PlayEffectGroup(18688);

            bool hitAnything = false;
            foreach (Actor actor in GetTargetsInRange(TargetPosition, 8f))
            {
                hitAnything = true;
                WeaponDamage(actor, 0.70f, DamageType.Physical);
            }

            if (hitAnything)
                GeneratePrimaryResource(15f);

            //FIXME
            //Add power cooldown

            yield break;
        }
    }

    [ImplementsPowerSNO(Skills.Skills.Barbarian.FuryGenerators.Frenzy)]
    public class BarbarianFrenzy : PowerImplementation
    {
        public override IEnumerable<TickTimer> Run()
        {
            /*
             * //Cast owner to player
            Mooege.Core.GS.Player.Player hero = owner as Mooege.Core.GS.Player.Player;

            //This power request a valid target in range
            if (target == null || PowerUtils.isInMeleeRange(hero.Position, target.Position)) { yield break; }

            //FIXME frenzy stack should be stored in class specific attribute
            //Max stack reach ?
            if(hero.PowerManager.frenzyStack >= 5) { yield break; }  
        
            //First stack we need to add the stack buff
            if (hero.PowerManager.frenzyStack == 0) 
            { 
                hero.ActorAttribute.setAttribute(GameAttribute.Power_Buff_0_Visual_Effect_None, new GameAttributeValue(true), Skills.Skills.Barbarian.FuryGenerators.Frenzy);
            }

            //Increase player attack speed
            hero.increaseAttackSeep(0.15f);
            
            //Increase current FrenzyStack
            hero.PowerManager.frenzyStack++;

            //Regen 3 fury
            hero.regenResources(3f);

            //Send dmg
            target.ReceiveDamage(10f, FloatingNumberMessage.FloatType.White);
            
            //Add timer of 4 sec before removal of the effect
            yield return 4000;

            //Decrease attack speed
            hero.decreaseAttackSeep(0.15f);
            
            //Decrase frenzy stack
            hero.PowerManager.frenzyStack--;
            
            //Remove frenzy effect if frenzy buff is gone
            if(hero.PowerManager.frenzyStack == 0) 
            {
                hero.ActorAttribute.setAttribute(GameAttribute.Power_Buff_0_Visual_Effect_None, new GameAttributeValue(false), Skills.Skills.Barbarian.FuryGenerators.Frenzy);
            }
            */

            if (User.frenzyStack < 5)
            {

            }

            if (CanHitMeleeTarget(Target))
            {
                GeneratePrimaryResource(3f);

                WeaponDamage(Target, 1f, DamageType.Physical);
            }

            yield break;
        }
    }

    [ImplementsPowerSNO(Skills.Skills.Barbarian.FurySpenders.Whirlwind)]
    public class BarbarianWhirlwind : PowerImplementation
    {
        public override IEnumerable<TickTimer> Run()
        {
            //UsePrimaryResource(14f);

            //User.AddBuff(new WhirlWindEffectBuff(WaitSeconds(0.250f)));

            foreach (Actor target in GetTargetsInRange(User.Position, 9f))
            {
                WeaponDamage(target, 0.44f, DamageType.Physical);
            }

            yield break;
        }
    }

    [ImplementsPowerSNO(Skills.Skills.Barbarian.FuryGenerators.AncientSpear)]
    public class BarbarianAncientSpear : PowerImplementation
    {
        public override IEnumerable<TickTimer> Run()
        {
            //StartCooldown(WaitSeconds(10f));
            
            var projectile = new PowerProjectile(User.World, 74636, User.Position, TargetPosition, 2f, 500f, 1f, 3f, 5f, 0f);

            User.AddRopeEffect(79402, projectile);

            projectile.OnHit = () =>
            {
                GeneratePrimaryResource(15f);

                var inFrontOfUser = PowerMath.ProjectAndTranslate2D(User.Position, projectile.hittedActor.Position,
                    User.Position, 5f);

                _setupReturnProjectile(projectile.hittedActor.Position, 5f);

                // GET OVER HERE
                projectile.hittedActor.TranslateNormal(inFrontOfUser, 2f);
                WeaponDamage(projectile.hittedActor, 1.00f, DamageType.Physical);

                projectile.Destroy();
            };

            projectile.OnTimeout = () =>
            {
                _setupReturnProjectile(projectile.getCurrentPosition(), 0f);
            };

            yield break;
        }

        private void _setupReturnProjectile(Vector3D spawnPosition, float heightOffset)
        {
            var return_proj = new PowerProjectile(User.World, 79400, spawnPosition,
                User.Position, 2f, 500f, 1f, 3f, heightOffset, 0f);

            User.AddRopeEffect(79402, return_proj);

            return_proj.OnUpdate = () =>
            {
                if (PowerMath.Distance(return_proj.getCurrentPosition(), User.Position) < 15f) // TODO: make this tick based distance?
                {
                    return_proj.Destroy();
                    return false;
                }
                return true;
            };
        }
    }
}
