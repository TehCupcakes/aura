// Copyright (c) Aura development team - Licensed under GNU GPL
// For more information, see license file in the main folder

using Aura.Channel.Network.Sending;
using Aura.Channel.Skills.Base;
using Aura.Channel.World.Entities;
using Aura.Shared.Mabi.Const;
using Aura.Shared.Network;
using Aura.Shared.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aura.Channel.Skills.FirstAid
{
	/// <summary>
	/// Handle for the First Aid skill.
	/// </summary>
	[Skill(SkillId.FirstAid)]
	public class FirstAid : StandardUseHandler
	{
		public override void Prepare(Creature creature, Skill skill, int castTime, Packet packet)
		{
			Send.SkillFlashEffect(creature);
			Send.SkillPrepare(creature, skill.Info.Id, castTime);

			creature.Skills.ActiveSkill = skill;
		}

		public override void Ready(Creature creature, Skill skill, Packet packet)
		{
			Send.SkillReady(creature, skill.Info.Id);
			Send.SkillStackSet(creature, skill.Info.Id, 1, 1);
		}

		public override TargetResult Use(Creature skillUser, Skill skill, long targetEntityId)
		{
			// Check target
			var target = skillUser.Region.GetCreature(targetEntityId);

			// Can't heal a dead man (UNTESTED) or a non-friendly creature
			if (target == null || target.Has(CreatureStates.Dead) || (!target.Has(CreatureStates.GoodNpc) && !(target.IsPlayer)))
				return TargetResult.InvalidTarget;

			// Check range
			var targetPosition = target.GetPosition();
			if (!skillUser.GetPosition().InRange(targetPosition, skillUser.AttackRangeFor(target)))
				return TargetResult.OutOfRange;

			// Stop movement
			skillUser.StopMove();

			// Determine which bandage to use
			Item bandage = this.GetBandage(skillUser);

			// Calculate injuries to be healed (% of LifeMax)
			var heal = this.GetHeal(skillUser, skill, target);

			// Update skill stack
			Send.SkillStackUpdate(skillUser, skill.Info.Id, 0, 1, 0);

			// Heal wounds
			// use bandage
			target.Injuries -= heal;

			Send.SkillUseFirstAid(skillUser, skill.Info.Id, targetEntityId);

			return TargetResult.Okay;
		}

		protected Item GetBandage(Creature skillUser)
		{
			// TODO: Get creature's finest bandage
			return null;
		}

		protected int GetHeal(Creature skillUser, Skill skill, Creature target)
		{
			// No wound healing effect for novice; just train skill
			if (skill.Info.Rank == SkillRank.Novice)
			{
				skill.Train(1); // Use First Aid.
				return 0;
			}

			// Get base heal value (% of target's LifeMax)
			Random r = new Random(DateTime.Now.Millisecond);
			var result = r.Next((int)skill.RankData.Var1, (int)skill.RankData.Var2);
			// Higher quality bandages are more effective
			// TODO: Figure out what these values are

			// Less effective on standing characters
			if (!target.Has(CreatureStates.SitDown))
			{
				result = result / 2; // 50% effective
			}

			// Get the int value
			result = result / 100 * (int)target.LifeMax;

			return result;
		}
	}
}
