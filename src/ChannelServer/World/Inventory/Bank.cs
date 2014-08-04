// Copyright (c) Aura development team - Licensed under GNU GPL
// For more information, see licence.txt in the main folder

using System;
using System.Collections.Generic;
using System.Linq;
using Aura.Channel.Network.Sending;
using Aura.Channel.World.Entities;
using Aura.Data.Database;
using Aura.Shared.Mabi.Const;
using Aura.Shared.Util;
using Aura.Channel.Database;

namespace Aura.Channel.World
{
	/// <summary>
	/// Inventory for players
	/// </summary>
	/// <remarks>
	/// TODO: I'm dirty and unsafe, clean me up.
	/// </remarks>
	public class Bank
	{
		private const int DefaultWidth = 12;
		private const int DefaultHeight = 8;
		private const int MaxWidth = 32;
		private const int MaxHeight = 32;

		private Creature _creature;
		private Dictionary<Pocket, InventoryPocket> _pockets;
		public long Id { get; set; }
		public long LastOpened { get; set; }
		public byte Assistant { get; set; }
		public int Gold { get; set; }
		public int Height { get; set; }
		public int Width { get; set; }
		public string Location { get; protected set; }
		public int Exists { get; protected set;  }

		public Bank(Creature creature, string location)
		{
			_creature = creature;
			_pockets = new Dictionary<Pocket, InventoryPocket>();
			LastOpened = DateTime.Now.Ticks;
			Assistant = 0;
			Gold = 0;
			Height = DefaultHeight;
			Width = DefaultWidth;
			Location = location;

			// Bank reference needs to be stored in client so it can
			// be close packet is received.
			creature.Client.OpenBank = this;

			// Is this relevant for bank?
			//this.Add(new InventoryPocketStack(Pocket.Temporary));
			//this.Add(new InventoryPocketStack(Pocket.Quests));
			//this.Add(new InventoryPocketSingle(Pocket.Cursor));
			
			Exists = ChannelDb.Instance.LoadBank(creature);
		}

		/// <summary>
		/// These pockets aren't checked by the Count() method
		/// </summary>
		public readonly IEnumerable<Pocket> InvisiblePockets = new[]
		{
			Pocket.Temporary
		};

		/// <summary>
		/// List of all items in this inventory.
		/// </summary>
		public IEnumerable<Item> Items
		{
			get
			{
				return _pockets.Values.SelectMany(pocket => pocket.Items.Where(a => a != null));
			}
		}

		/// <summary>
		/// List of all items sitting in equipment pockets in this inventory.
		/// </summary>
		public IEnumerable<Item> Equipment
		{
			get
			{
				return _pockets.Values.Where(a => a.Pocket.IsEquip()).SelectMany(pocket => pocket.Items.Where(a => a != null));
			}
		}

		/// <summary>
		/// List of all items in equipment slots, minus hair and face.
		/// </summary>
		public IEnumerable<Item> ActualEquipment
		{
			get
			{
				return _pockets.Values.Where(a => a.Pocket.IsEquip() && a.Pocket != Pocket.Hair && a.Pocket != Pocket.Face)
					.SelectMany(pocket => pocket.Items.Where(a => a != null));
			}
		}

		/// <summary>
		/// Adds pocket to inventory.
		/// </summary>
		/// <param name="inventoryPocket"></param>
		public void Add(InventoryPocket inventoryPocket)
		{
			if (_pockets.ContainsKey(inventoryPocket.Pocket))
				Log.Warning("Replacing pocket '{0}' in '{1}'s inventory.", inventoryPocket.Pocket, _creature);

			_pockets[inventoryPocket.Pocket] = inventoryPocket;
		}

		/// <summary>
		/// Adds main inventories (inv, personal, VIP). Call after creature's
		/// defaults (RaceInfo) have been loaded.
		/// </summary>
		public void AddMainInventory()
		{
			if (_creature.RaceData == null)
				Log.Warning("Race for creature '{0}' ({1}) not loaded before initializing main inventory.", _creature.Name, _creature.EntityIdHex);

			Width = (_creature.RaceData != null ? 24 : DefaultWidth);
			if (Width > MaxWidth)
			{
				Width = MaxWidth;
				Log.Warning("AddMainInventory: Width exceeds max, using {0} instead.", MaxWidth);
			}

			Height = (_creature.RaceData != null ? 8 : DefaultHeight);
			if (Height > MaxHeight)
			{
				Height = MaxHeight;
				Log.Warning("AddMainInventory: Height exceeds max, using {0} instead.", MaxHeight);
			}

			// TODO: Race check
			this.Add(new InventoryPocketNormal(Pocket.Inventory, Width, Height));
		}

		/// <summary>
		/// Returns true if pocket exists in this inventory.
		/// </summary>
		/// <param name="pocket"></param>
		/// <returns></returns>
		public bool Has(Pocket pocket)
		{
			return _pockets.ContainsKey(pocket);
		}

		/// <summary>
		/// Returns item with the id, or null.
		/// </summary>
		/// <param name="entityId"></param>
		/// <returns></returns>
		public Item GetItem(long entityId)
		{
			return _pockets.Values.Select(pocket => pocket.GetItem(entityId)).FirstOrDefault(item => item != null);
		}

		/// <summary>
		/// Returns item at the location, or null.
		/// </summary>
		/// <param name="pocket"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <returns></returns>
		public Item GetItemAt(Pocket pocket, int x, int y)
		{
			if (!this.Has(pocket))
				return null;

			return _pockets[pocket].GetItemAt(x, y);
		}

		/// <summary>
		/// Returns a free pocket id to be used for item bags.
		/// </summary>
		/// <returns></returns>
		public Pocket GetFreePocketId()
		{
			for (var i = Pocket.ItemBags; i < Pocket.ItemBagsMax; ++i)
			{
				if (!_pockets.ContainsKey(i))
					return i;
			}

			return Pocket.None;
		}

		/// <summary>
		/// Adds pocket for item and updates item's linked pocket.
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>
		public bool AddBagPocket(Item item)
		{
			var freePocket = this.GetFreePocketId();
			if (freePocket == Pocket.None)
				return false;

			item.OptionInfo.LinkedPocketId = freePocket;

			this.Add(new InventoryPocketNormal(freePocket, item.Data.BagWidth, item.Data.BagHeight));

			return true;
		}

		/// <summary>
		/// Returns list of all items in pocket. Returns null if the pocket
		/// doesn't exist.
		/// </summary>
		/// <param name="pocket"></param>
		/// <returns></returns>
		public List<Item> GetAllItemsFrom(Pocket pocket)
		{
			if (!_pockets.ContainsKey(pocket))
				return null;

			return _pockets[pocket].Items.Where(a => a != null).ToList();
		}

		/// <summary>
		/// Removes pocket from inventory.
		/// </summary>
		/// <param name="pocket"></param>
		/// <returns></returns>
		public bool Remove(Pocket pocket)
		{
			if (pocket == Pocket.None || !_pockets.ContainsKey(pocket))
				return false;

			_pockets.Remove(pocket);

			return true;
		}

		// Handlers
		// ------------------------------------------------------------------

		/// <summary>
		/// Used from MoveItem handler.
		/// </summary>
		/// <remarks>
		/// The item is the one that's interacted with, the one picked up
		/// when taking it, the one being put into a packet when it's one
		/// the cursor. Colliding items switch places with it.
		/// </remarks>
		/// <param name="item">Item to move</param>
		/// <param name="target">Pocket to move it to</param>
		/// <param name="targetX"></param>
		/// <param name="targetY"></param>
		/// <returns></returns>
		public bool Move(Item item, Pocket target, byte targetX, byte targetY)
		{
			if (!this.Has(target))
				return false;

			var source = item.Info.Pocket;
			var amount = item.Info.Amount;

			Item collidingItem = null;
			if (!_pockets[target].TryAdd(item, targetX, targetY, out collidingItem))
				return false;

			// If amount differs (item was added to stack)
			if (collidingItem != null && item.Info.Amount != amount)
			{
				Send.ItemAmount(_creature, collidingItem);

				// Left overs, update
				if (item.Info.Amount > 0)
				{
					Send.ItemAmount(_creature, item);
				}
				// All in, remove from cursor.
				else
				{
					_pockets[item.Info.Pocket].Remove(item);
					Send.ItemRemove(_creature, item);
				}
			}
			else
			{
				// Remove the item from the source pocket
				_pockets[source].Remove(item);

				// Toss it in, it should be the cursor.
				if (collidingItem != null)
					_pockets[source].Add(collidingItem);

				Send.ItemMoveInfo(_creature, item, source, collidingItem);
			}

			return true;
		}

		// Adding
		// ------------------------------------------------------------------

		// TODO: Add central "Add" method that all others use, for central stuff
		//   like adding bag pockets. This wil require a GetFreePosition
		//   method in the pockets.

		/// <summary>
		/// Tries to add item to pocket. Returns false if the pocket
		/// doesn't exist or there was no space.
		/// </summary>
		public bool Add(Item item, Pocket pocket)
		{
			if (!_pockets.ContainsKey(pocket))
				return false;

			var success = _pockets[pocket].Add(item);
			if (success)
			{
				Send.ItemNew(_creature, item);

				// Add bag pocket if it doesn't already exist.
				if (item.OptionInfo.LinkedPocketId != Pocket.None && !this.Has(item.OptionInfo.LinkedPocketId))
					this.AddBagPocket(item);
			}

			return success;
		}

		/// <summary>
		/// Tries to add item to pocket. Returns false if the pocket
		/// doesn't exist or there was no space.
		/// </summary>
		public bool Add(int itemId, Pocket pocket)
		{
			var item = new Item(itemId);

			if (!this.Add(item, pocket))
				return false;

			return true;
		}

		/// <summary>
		/// Adds item to pocket at the position it currently has.
		/// Returns false if pocket doesn't exist.
		/// </summary>
		public bool InitAdd(Item item)
		{
			if (!_pockets.ContainsKey(item.Info.Pocket))
				return false;

			_pockets[item.Info.Pocket].AddUnsafe(item);

			return true;
		}

		/// <summary>
		/// Tries to add item to one of the main inventories, using the temp
		/// inv as fallback (if specified to do so). Returns false if
		/// there was no space.
		/// </summary>
		public bool Add(Item item, bool tempFallback)
		{
			var success = this.TryAutoAdd(item, tempFallback);

			// Inform about new item
			if (success)
			{
				Send.ItemNew(_creature, item);

				// Add bag pocket if it doesn't already exist.
				if (item.OptionInfo.LinkedPocketId != Pocket.None && !this.Has(item.OptionInfo.LinkedPocketId))
					this.AddBagPocket(item);
			}

			return success;
		}

		/// <summary>
		/// Tries to add item to one of the main inventories,
		/// using temp as fallback. Unlike "Add" the item will be filled
		/// into stacks first, if possible, before calling Add.
		/// </summary>
		/// <param name="item"></param>
		/// <param name="tempFallback"></param>
		/// <returns></returns>
		public bool Insert(Item item, bool tempFallback)
		{
			if (item.Data.StackType == StackType.Stackable)
			{
				// Try stacks/sacs first
				List<Item> changed;
				lock (_pockets)
				{
					// Main inv
					_pockets[Pocket.Inventory].FillStacks(item, out changed);
					this.UpdateChangedItems(changed);

					// Bags
					for (var i = Pocket.ItemBags; i <= Pocket.ItemBagsMax; ++i)
					{
						if (item.Info.Amount == 0)
							break;

						if (_pockets.ContainsKey(i))
						{
							_pockets[i].FillStacks(item, out changed);
							this.UpdateChangedItems(changed);
						}
					}

					// Add new item stacks as long as needed.
					while (item.Info.Amount > item.Data.StackMax)
					{
						var newStackItem = new Item(item);
						newStackItem.Info.Amount = item.Data.StackMax;

						// Break if no new items can be added (no space left)
						if (!this.TryAutoAdd(newStackItem, false))
							break;

						Send.ItemNew(_creature, newStackItem);
						item.Info.Amount -= item.Data.StackMax;
					}
				}

				if (item.Info.Amount == 0)
					return true;
			}

			return this.Add(item, tempFallback);
		}

		/// <summary>
		/// Adds a new item to the inventory.
		/// </summary>
		/// <remarks>
		/// For stackables and sacs the amount is capped at stack max.
		/// - If item is stackable, it will be added to existing stacks first.
		///   New stacks are added afterwards, if necessary.
		/// - If item is a sac it's simply added as one item.
		/// - If it's a normal item, it's added times the amount.
		/// </remarks>
		/// <param name="itemId"></param>
		/// <param name="amount"></param>
		/// <returns></returns>
		public bool Add(int itemId, int amount = 1)
		{
			var newItem = new Item(itemId);
			newItem.Amount = amount;

			if (newItem.Data.StackType == StackType.Stackable)
			{
				// Insert new stacks till amount is 0.
				int stackMax = newItem.Data.StackMax;
				do
				{
					var stackAmount = Math.Min(stackMax, amount);

					var stackItem = new Item(itemId);
					stackItem.Amount = stackAmount;

					var result = this.Insert(stackItem, true);
					if (!result)
						return false;

					amount -= stackAmount;
				}
				while (amount > 0);
			}
			else if (newItem.Data.StackType == StackType.Sac)
			{
				// Add sac item with amount once
				return this.Add(newItem, true);
			}
			else
			{
				// Add item x times
				for (int i = 0; i < amount; ++i)
				{
					if (!this.Add(new Item(itemId), true))
						return false;
				}
				return true;
			}

			return false;
		}

		/// <summary>
		/// Tries to add item to one of the main invs or bags,
		/// wherever free space is available. Returns whether it was successful.
		/// </summary>
		/// <param name="item"></param>
		/// <param name="tempFallback">Use temp inventory if all others are full?</param>
		/// <returns></returns>
		public bool TryAutoAdd(Item item, bool tempFallback)
		{
			var success = false;

			lock (_pockets)
			{
				// Try main inv
				if (_pockets.ContainsKey(Pocket.Inventory))
					success = _pockets[Pocket.Inventory].Add(item);

				// Try bags
				for (var i = Pocket.ItemBags; i <= Pocket.ItemBagsMax; ++i)
				{
					if (success)
						break;

					if (_pockets.ContainsKey(i))
						success = _pockets[i].Add(item);
				}

				// Try temp
				if (!success && tempFallback)
					success = _pockets[Pocket.Temporary].Add(item);
			}

			return success;
		}

		// Removing
		// ------------------------------------------------------------------

		/// <summary>
		/// Removes item from inventory, if it is in it, and sends update packets.
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>
		public bool Remove(Item item)
		{
			lock (_pockets)
			{
				if (_pockets.Values.Any(pocket => pocket.Remove(item)))
				{
					Send.ItemRemove(_creature, item);

					// Remove bag pocket
					if (item.OptionInfo.LinkedPocketId != Pocket.None)
					{
						this.Remove(item.OptionInfo.LinkedPocketId);
						item.OptionInfo.LinkedPocketId = Pocket.None;
					}

					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Removes the amount of items with the id from the inventory.
		/// Returns true if the specified amount was removed.
		/// </summary>
		/// <remarks>
		/// Does not check amount before removing.
		/// </remarks>
		/// <param name="itemId"></param>
		/// <param name="amount"></param>
		/// <returns></returns>
		public bool Remove(int itemId, int amount = 1)
		{
			if (amount < 0)
				amount = 0;

			var changed = new List<Item>();

			lock (_pockets)
			{
				foreach (var pocket in _pockets.Values)
				{
					amount -= pocket.Remove(itemId, amount, ref changed);

					if (amount == 0)
						break;
				}
			}

			this.UpdateChangedItems(changed);

			return (amount == 0);
		}

		/// <summary>
		/// Reduces item's amount and sends the necessary update packets.
		/// Also removes the item, if it's not a sack and its amount reaches 0.
		/// </summary>
		/// <param name="item"></param>
		/// <param name="amount"></param>
		/// <returns></returns>
		public bool Decrement(Item item, ushort amount = 1)
		{
			if (!this.Has(item) || item.Info.Amount == 0 || item.Info.Amount < amount)
				return false;

			item.Info.Amount -= amount;

			if (item.Info.Amount > 0 || item.Data.StackType == StackType.Sac)
			{
				Send.ItemAmount(_creature, item);
			}
			else
			{
				this.Remove(item);
				Send.ItemRemove(_creature, item);
			}

			return true;
		}

		// Checks
		// ------------------------------------------------------------------

		/// <summary>
		/// Returns true uf the item exists in this inventory.
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>
		public bool Has(Item item)
		{
			lock (_pockets)
				return _pockets.Values.Any(pocket => pocket.Has(item));
		}

		/// <summary>
		/// Returns the amount of items with this id in the inventory.
		/// </summary>
		/// <param name="itemId"></param>
		/// <returns></returns>
		public int Count(int itemId)
		{
			lock (_pockets)
				return _pockets.Values.Where(a => !InvisiblePockets.Contains(a.Pocket))
					.Sum(pocket => pocket.CountItem(itemId));
		}

		/// <summary>
		/// Returns the number of items in the given pocket.
		/// Returns -1 if the pocket doesn't exist.
		/// </summary>
		/// <param name="pocket"></param>
		/// <returns></returns>
		public int CountItemsInPocket(Pocket pocket)
		{
			if (!_pockets.ContainsKey(pocket))
				return -1;

			return _pockets[pocket].Count;
		}

		/// <summary>
		/// Returns whether inventory contains the item in this amount.
		/// </summary>
		/// <param name="itemId"></param>
		/// <param name="amount"></param>
		/// <returns></returns>
		public bool Has(int itemId, int amount = 1)
		{
			return (this.Count(itemId) >= amount);
		}

		// Helpers
		// ------------------------------------------------------------------

		/// <summary>
		/// Sends amount update or remove packets for all items, depending on
		/// their amount.
		/// </summary>
		/// <param name="items"></param>
		private void UpdateChangedItems(IEnumerable<Item> items)
		{
			if (items == null)
				return;

			foreach (var item in items)
			{
				if (item.Info.Amount > 0 || item.Data.StackType == StackType.Sac)
					Send.ItemAmount(_creature, item);
				else
					Send.ItemRemove(_creature, item);
			}
		}

		/// <summary>
		/// Converts bank location to a displayable string.
		/// </summary>
		/// <param name="location"></param>
		public string LocDisplayName(string location)
		{
			switch (location)
			{
				case "TirChonaillBank":
					return "Tir Chonaill Bank";

				case "DunbartonBank":
					return "Dunbarton Bank";

				case "IriaConnousBank":
					return "Connous Filia Bank";

				default:
					return "";
			}
		}
	}
}
