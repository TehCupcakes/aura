// Copyright (c) Aura development team - Licensed under GNU GPL
// For more information, see license file in the main folder

using System;
using Aura.Channel.World.Entities;
using Aura.Shared.Mabi.Const;
using Aura.Shared.Network;
using Aura.Shared.Util;
using Aura.Channel.World.Entities.Creatures;
using System.Globalization;
using Aura.Channel.World;

namespace Aura.Channel.Network.Sending
{
	public static partial class Send
	{
		/// <summary>
		/// Sends OpenBank to creature's client.
		/// </summary>
		/// <param name="creature"></param>
		/// <param name="item"></param>
		/// <param name="source"></param>
		public static void OpenBank(Creature creature, string accountName, Bank bank, string location)
		{
			if (bank == null)
				bank = new Bank(creature);

			var packet = new Packet(Op.OpenBank, creature.EntityId);
			packet.PutByte(1); // ?
			packet.PutByte(bank.assistant);
			packet.PutLong(bank.lastOpened);
			packet.PutByte(0); // ?
			packet.PutString(accountName);
			packet.PutString(location);
			packet.PutString(bank.LocDisplayName(location));
			packet.PutInt(bank.gold);
			packet.PutInt((int)bank.exists);
			if ((int)bank.exists == 1)
			{
				packet.PutString(creature.Name);
				packet.PutByte(bank.assistant);
				packet.PutInt(12); // Default width of bank inventory (= 12 without Inventory Plus)
				packet.PutInt(bank.height);
				packet.PutInt(bank.width);
			}

			creature.Client.Send(packet);
		}
	}
}