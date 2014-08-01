// Copyright (c) Aura development team - Licensed under GNU GPL
// For more information, see license file in the main folder

using Aura.Channel.Network.Sending;
using Aura.Channel.Skills.Base;
using Aura.Channel.World.Entities;
using Aura.Shared.Mabi;
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
	public class FirstAid : StandardTargetHandler
	{
		public override void Prepare(Creature creature, Skill skill, int castTime, Packet packet)
		{
			// Get entityId of bandage if supplied
			var dict = new MabiDictionary();
			Item bandage = null;
			dict.Parse(packet.GetString());
			bandage = creature.Inventory.GetItem(dict.GetLong("ITEMID"));

			Send.SkillFlashEffect(creature);
			Send.SkillPrepare(creature, skill.Info.Id, castTime);

			creature.Skills.ActiveSkill = skill;

			// Update item and send skill complete from Complete
			creature.Skills.Callback(SkillId.FirstAid, () =>
			{
				if (bandage == null)
					bandage = this.GetBandage(creature);
				if (bandage == null)
				{
					Send.SkillPrepareSilentCancel(creature, skill.Info.Id);
					return;
				}

				// Use bandage
				creature.Inventory.Decrement(bandage);
			});
		}

		public override void Ready(Creature creature, Skill skill, Packet packet)
		{
			Send.SkillReady(creature, skill.Info.Id);
			Send.SkillStackSet(creature, skill.Info.Id, 1, 1);
		}

		public override TargetResult Use(Creature creature, Skill skill, long targetEntityId)
		{
			// Check target
			var target = creature.Region.GetCreature(targetEntityId);

			// Can't heal a dead man (UNTESTED) or a non-friendly creature
			if (target == null || target.Has(CreatureStates.Dead) || (!target.Has(CreatureStates.GoodNpc) && !(target.IsPlayer)))
				return TargetResult.InvalidTarget;

			// Check range
			var targetPosition = target.GetPosition();
			if (!creature.GetPosition().InRange(targetPosition, creature.AttackRangeFor(target)))
				return TargetResult.OutOfRange;

			// Stop movement
			creature.StopMove();			

			// Calculate injuries to be healed (% of LifeMax)
			var heal = this.GetHeal(skill, target);

			// Use skill
			Send.SkillUseFirstAid(creature, skill.Info.Id, targetEntityId);

			// Update skill stack
			Send.SkillStackUpdate(creature, skill.Info.Id, 0, 1, 0);

			// Use bandage
			creature.Skills.Callback(SkillId.FirstAid);

			// Heal wounds
			target.HealInjuries((int)heal);

			// Healing effect
			Send.HealEffect(creature, target);

			return TargetResult.Okay;
		}

		protected Item GetBandage(Creature creature)
		{
			Item item = null;

			// First aid always uses highest grade bandage the creature has
			item = creature.Inventory.GetItem((int)60119);
			if (item == null) item = creature.Inventory.GetItem((int)60049);
			if (item == null) item = creature.Inventory.GetItem((int)60048);
			if (item == null) item = creature.Inventory.GetItem((int)60047);
			if (item == null) item = creature.Inventory.GetItem((int)60005);

			return item;
		}

		protected float GetHeal(Skill skill, Creature target)
		{
			// No wound healing effect for novice; just train skill
			if (skill.Info.Rank == SkillRank.Novice)
			{
				skill.Train(1); // Use First Aid.
				return 0;
			}

			// Get base heal value (% of target's LifeMax)
			Random r = new Random(DateTime.Now.Millisecond);
			float result = r.Next((int)skill.RankData.Var1, (int)skill.RankData.Var2);

			// Higher quality bandages are more effective
			// TODO: Figure out what these values are

			// Less effective on standing characters
			if (!target.Has(CreatureStates.SitDown))
				result = result / 2f; // 50% effective

			// Get the int value of injuries to be healed
			result = (int)(result / 100f * target.LifeMax);

			// Can't heal target more than they are injured
			if (result > target.Injuries)
				result = (int)target.Injuries;

			return result;
		}
	}
}
